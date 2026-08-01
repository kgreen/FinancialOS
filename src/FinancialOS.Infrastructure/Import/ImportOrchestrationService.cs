using FinancialOS.Core.Contracts;
using FinancialOS.Core.Knowledge.Provenance;
using FinancialOS.Core.Models;
using FinancialOS.Infrastructure.Import.Parsers;

namespace FinancialOS.Infrastructure.Import;

public sealed class ImportOrchestrationService : IImportOrchestrationService
{
    private readonly IFinancialRepository _repository;
    private readonly IEnumerable<ITransactionParser> _parsers;
    private readonly IRuleEvaluationService _ruleEvaluationService;
    private readonly ProvenanceWriter _provenanceWriter;
    private readonly EvidenceImportService _evidenceImportService;

    public ImportOrchestrationService(
        IFinancialRepository repository,
        IEnumerable<ITransactionParser> parsers,
        IRuleEvaluationService ruleEvaluationService,
        ProvenanceWriter provenanceWriter,
        EvidenceImportService evidenceImportService)
    {
        _repository = repository;
        _parsers = parsers;
        _ruleEvaluationService = ruleEvaluationService;
        _provenanceWriter = provenanceWriter;
        _evidenceImportService = evidenceImportService;
    }

    public async Task<ImportOrchestrationResult> ImportAsync(
        string fileName,
        Stream fileStream,
        Guid? institutionProfileId,
        CancellationToken cancellationToken = default)
    {
        // 1. Detect format
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var sourceType = ext switch
        {
            ".ofx" or ".qfx" => EvidenceSourceType.Ofx,
            ".csv" => EvidenceSourceType.Csv,
            _ => throw new OfxFormatException($"File format '{ext}' is not supported. Supported formats: .csv, .ofx, .qfx")
        };

        // 2. Reject unsupported extensions
        var parser = _parsers.FirstOrDefault(p => p.CanParse(fileName, sourceType))
            ?? throw new OfxFormatException($"File format '{ext}' is not supported. Supported formats: .csv, .ofx, .qfx");

        // 3. File-level validation, storage, and SHA256 deduplication
        var evidenceImport = await _evidenceImportService.ImportAsync(fileName, fileStream, cancellationToken);
        if (evidenceImport.WasDuplicate && evidenceImport.ExistingImportJob is not null)
        {
            return new ImportOrchestrationResult(
                Evidence: evidenceImport.Evidence,
                Job: evidenceImport.ExistingImportJob,
                CreatedRecords: Array.Empty<FinancialRecord>(),
                WasDuplicate: true);
        }

        var evidence = evidenceImport.Evidence;

        // 5. Resolve institution profile
        InstitutionProfile? profile = null;
        if (institutionProfileId.HasValue)
        {
            profile = await _repository.GetInstitutionProfileAsync(institutionProfileId.Value, cancellationToken);
        }

        // 6. Create ImportJob in Processing state
        var job = new ImportJob
        {
            EvidenceId = evidence.Id,
            InstitutionProfileId = profile?.Id,
            ParserType = parser.ParserType,
            Status = ImportJobStatus.Processing,
            StartedAt = DateTimeOffset.UtcNow,
        };
        job = await _repository.AddImportJobAsync(job, cancellationToken);

        // 7. Parse
        TransactionParseResult parseResult;
        try
        {
            await using var parseStream = File.OpenRead(evidence.StoragePath);
            parseResult = await parser.ParseAsync(parseStream, profile, cancellationToken);
        }
        catch (OfxFormatException ex)
        {
            job.Status = ImportJobStatus.Failed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            job.FailedRows.Add(new FailedRowEntry(0, ex.Message));
            job.FailedRowCount = 1;
            await _repository.UpdateImportJobAsync(job, cancellationToken);
            throw;
        }
        catch (CsvLayoutUndetectableException ex)
        {
            job.Status = ImportJobStatus.Failed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            job.FailedRows.Add(new FailedRowEntry(0, ex.Message));
            job.FailedRowCount = 1;
            await _repository.UpdateImportJobAsync(job, cancellationToken);
            throw;
        }

        job.ParserType = parser.ParserType;
        job.TotalRows = parseResult.TotalRowsScanned;
        job.FailedRows.AddRange(parseResult.FailedRows);
        job.FailedRowCount = parseResult.FailedRows.Count;

        // 8. Hydrate records
        var createdRecords = new List<FinancialRecord>();

        foreach (var tx in parseResult.Transactions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Cross-import OFX FITID duplicate detection (FR-020)
            if (sourceType == EvidenceSourceType.Ofx &&
                tx.ExternalReferenceId is not null &&
                await _repository.ExternalReferenceIdExistsAsync(tx.ExternalReferenceId, cancellationToken))
            {
                job.FailedRows.Add(new FailedRowEntry(tx.RowIndex, $"Cross-file duplicate: FITID '{tx.ExternalReferenceId}' already exists"));
                job.FailedRowCount++;
                continue;
            }

            var record = new FinancialRecord
            {
                EvidenceId = evidence.Id,
                ImportJobId = job.Id,
                Description = tx.Description,
                Amount = new Money(tx.Amount, "USD"),
                OccurredOn = new DateTime(tx.TransactionDate.Year, tx.TransactionDate.Month, tx.TransactionDate.Day, 0, 0, 0, DateTimeKind.Utc),
                Status = RecordStatus.Pending,
                ExternalReferenceId = tx.ExternalReferenceId,
                RowIndex = tx.RowIndex,
                ClassificationStatus = ClassificationStatus.Pending,
                Provenance = new Provenance($"import:{parser.ParserType}", job.Id.ToString())
            };

            // 9. Auto-classify via rule engine
            var evalResult = await _ruleEvaluationService.EvaluateAsync(record, cancellationToken);
            if (evalResult is not null)
            {
                record.ClassificationStatus = ClassificationStatus.Classified;
                record.ClassificationConfidence = new Confidence(evalResult.Confidence);
                record.ClassificationReasonCode = evalResult.ReasonCodes.FirstOrDefault();
                record.MerchantId = evalResult.TargetMerchantId;
                record.CategoryId = evalResult.TargetCategoryId;
            }

            record = await _repository.AddRecordAsync(record, cancellationToken);
            await _provenanceWriter.WriteImportHydrationAsync(
                financialRecordId: record.Id,
                evidenceId: evidence.Id,
                importJobId: job.Id,
                parserType: job.ParserType,
                rowIndex: tx.RowIndex,
                externalReferenceId: tx.ExternalReferenceId,
                cancellationToken: cancellationToken);
            createdRecords.Add(record);
        }

        // 10. Finalise ImportJob status
        job.ParsedCount = createdRecords.Count;
        job.FailedRowCount = job.FailedRows.Count;
        job.CompletedAt = DateTimeOffset.UtcNow;

        if (job.TotalRows == 0)
            job.Status = ImportJobStatus.Completed;
        else if (job.ParsedCount == 0)
            job.Status = ImportJobStatus.Failed;
        else if (job.FailedRowCount > 0)
            job.Status = ImportJobStatus.PartialSuccess;
        else
            job.Status = ImportJobStatus.Completed;

        await _repository.UpdateImportJobAsync(job, cancellationToken);

        return new ImportOrchestrationResult(
            Evidence: evidence,
            Job: job,
            CreatedRecords: createdRecords,
            WasDuplicate: false);
    }
}

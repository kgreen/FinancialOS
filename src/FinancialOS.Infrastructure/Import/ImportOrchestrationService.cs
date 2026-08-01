using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using FinancialOS.Infrastructure.Import.Parsers;

namespace FinancialOS.Infrastructure.Import;

public sealed class ImportOrchestrationService : IImportOrchestrationService
{
    private readonly IFinancialRepository _repository;
    private readonly IEnumerable<ITransactionParser> _parsers;
    private readonly IRuleEvaluationService _ruleEvaluationService;

    public ImportOrchestrationService(
        IFinancialRepository repository,
        IEnumerable<ITransactionParser> parsers,
        IRuleEvaluationService ruleEvaluationService)
    {
        _repository = repository;
        _parsers = parsers;
        _ruleEvaluationService = ruleEvaluationService;
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

        // 3. SHA256 duplicate check — compute hash and look up
        fileStream.Position = 0;
        var (sha256, sizeBytes) = await ComputeSha256Async(fileStream, cancellationToken);
        fileStream.Position = 0;

        var existingEvidence = await _repository.GetEvidenceBySha256Async(sha256, cancellationToken);
        if (existingEvidence is not null)
        {
            var existingJob = await _repository.GetImportJobByEvidenceIdAsync(existingEvidence.Id, cancellationToken);
            if (existingJob is not null)
            {
                return new ImportOrchestrationResult(
                    Evidence: existingEvidence,
                    Job: existingJob,
                    CreatedRecords: Array.Empty<FinancialRecord>(),
                    WasDuplicate: true);
            }
        }

        // 4. Persist evidence (skip file write if evidence already exists for this SHA256)
        FinancialEvidence evidence;
        if (existingEvidence is not null)
        {
            evidence = existingEvidence;
        }
        else
        {
            var evidenceId = Guid.NewGuid();
            var uploadsDirectory = Path.Combine(Path.GetTempPath(), "financialos", "uploads");
            Directory.CreateDirectory(uploadsDirectory);
            var destinationPath = Path.Combine(uploadsDirectory, $"{evidenceId:N}{ext}");

            fileStream.Position = 0;
            await using (var outputStream = File.Create(destinationPath))
                await fileStream.CopyToAsync(outputStream, cancellationToken);

            var newEvidence = new FinancialEvidence
            {
                Id = evidenceId,
                SourceType = sourceType,
                OriginalFileName = fileName,
                StoragePath = destinationPath,
                Sha256Hash = sha256,
                SourceMetadata = $"Imported {sizeBytes} bytes",
                UploadedAt = DateTimeOffset.UtcNow
            };
            evidence = await _repository.AddEvidenceAsync(newEvidence, cancellationToken);
        }

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
        fileStream.Position = 0;
        TransactionParseResult parseResult;
        try
        {
            parseResult = await parser.ParseAsync(fileStream, profile, cancellationToken);
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

        job.TotalRows = parseResult.TotalRowsScanned;
        job.FailedRows.AddRange(parseResult.FailedRows);
        job.FailedRowCount = parseResult.FailedRows.Count;

        // 8. Hydrate records
        var createdRecords = new List<FinancialRecord>();

        foreach (var tx in parseResult.Transactions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Cross-import OFX FITID duplicate detection (FR-020)
            if (tx.ExternalReferenceId is not null &&
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

    private static async Task<(string Sha256, long SizeBytes)> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var buffer = new byte[8192];
        var totalBytes = 0L;

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
            if (read == 0) break;
            totalBytes += read;
            sha256.TransformBlock(buffer, 0, read, null, 0);
        }
        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return (Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>()), totalBytes);
    }
}

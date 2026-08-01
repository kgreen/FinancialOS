using System.Security.Cryptography;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Infrastructure.Import;

public sealed class EvidenceImportService
{
    private readonly IFinancialRepository _repository;

    public EvidenceImportService(IFinancialRepository repository)
    {
        _repository = repository;
    }

    public async Task<EvidenceImportResult> ImportAsync(string fileName, Stream inputStream, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(inputStream);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var sourceType = ext switch
        {
            ".csv" => EvidenceSourceType.Csv,
            ".ofx" or ".qfx" => EvidenceSourceType.Ofx,
            _ => throw new EvidenceImportValidationException(
                $"File format '{ext}' is not supported. Supported formats: .csv, .ofx, .qfx",
                422)
        };

        var evidenceId = Guid.NewGuid();
        var uploadsDirectory = Path.Combine(Path.GetTempPath(), "financialos", "uploads");
        Directory.CreateDirectory(uploadsDirectory);
        var destinationPath = Path.Combine(uploadsDirectory, $"{evidenceId:N}{ext}");

        using var sha256 = SHA256.Create();
        var buffer = new byte[8192];
        var totalBytes = 0L;
        var hasNonWhitespaceContent = false;

        await using (var outputStream = File.Create(destinationPath))
        {
            while (true)
            {
                var read = await inputStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                totalBytes += read;
                sha256.TransformBlock(buffer, 0, read, null, 0);
                await outputStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);

                if (!hasNonWhitespaceContent)
                {
                    for (var i = 0; i < read; i++)
                    {
                        var b = buffer[i];
                        if (b is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n'))
                        {
                            hasNonWhitespaceContent = true;
                            break;
                        }
                    }
                }
            }
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        if (totalBytes == 0)
        {
            File.Delete(destinationPath);
            throw new EvidenceImportValidationException(
                "A non-empty file is required.",
                400);
        }

        if (!hasNonWhitespaceContent)
        {
            File.Delete(destinationPath);
            throw new EvidenceImportValidationException(
                "A non-blank file is required.",
                400);
        }

        var hash = Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>());

        var existingEvidence = await _repository.GetEvidenceBySha256Async(hash, cancellationToken);
        if (existingEvidence is not null)
        {
            File.Delete(destinationPath);
            var existingJob = await _repository.GetImportJobByEvidenceIdAsync(existingEvidence.Id, cancellationToken);
            if (existingJob is not null)
            {
                return new EvidenceImportResult(existingEvidence, existingEvidence.StoragePath, totalBytes, true, existingJob);
            }

            return new EvidenceImportResult(existingEvidence, existingEvidence.StoragePath, totalBytes, false, null);
        }

        var evidence = new FinancialEvidence
        {
            Id = evidenceId,
            SourceType = sourceType,
            OriginalFileName = fileName,
            StoragePath = destinationPath,
            Sha256Hash = hash,
            SourceMetadata = $"Imported {totalBytes} bytes",
            UploadedAt = DateTimeOffset.UtcNow
        };

        var storedEvidence = await _repository.AddEvidenceAsync(evidence, cancellationToken);
        return new EvidenceImportResult(storedEvidence, destinationPath, totalBytes, false, null);
    }
}

public sealed record EvidenceImportResult(
    FinancialEvidence Evidence,
    string StoragePath,
    long SizeBytes,
    bool WasDuplicate,
    ImportJob? ExistingImportJob);

public sealed class EvidenceImportValidationException : Exception
{
    public int StatusCode { get; }

    public EvidenceImportValidationException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}

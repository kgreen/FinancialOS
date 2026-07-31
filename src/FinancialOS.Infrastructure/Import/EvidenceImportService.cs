using System.Security.Cryptography;
using FinancialOS.Core.Models;

namespace FinancialOS.Infrastructure.Import;

public sealed class EvidenceImportService
{
    public async Task<EvidenceImportResult> ImportAsync(string fileName, Stream inputStream, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(inputStream);

        var evidenceId = Guid.NewGuid();
        var uploadsDirectory = Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(uploadsDirectory);
        var destinationPath = Path.Combine(uploadsDirectory, $"{evidenceId:N}{Path.GetExtension(fileName)}");

        await using var outputStream = File.Create(destinationPath);
        using var sha256 = SHA256.Create();
        var buffer = new byte[8192];
        var totalBytes = 0L;

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
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var hash = Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>());
        var sourceType = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".ofx" => EvidenceSourceType.Ofx,
            ".pdf" => EvidenceSourceType.Pdf,
            ".png" or ".jpg" or ".jpeg" => EvidenceSourceType.Image,
            _ => EvidenceSourceType.Csv
        };

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

        return new EvidenceImportResult(evidence, destinationPath, totalBytes);
    }
}

public sealed record EvidenceImportResult(FinancialEvidence Evidence, string StoragePath, long SizeBytes);

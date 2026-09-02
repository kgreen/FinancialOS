using FinancialOS.Core.Models;
using System.Text.Json;

namespace FinancialOS.Infrastructure.Exporters;

/// <summary>
/// Exports records as a JSON array.
/// Each object includes all record fields plus a nested provenance object.
/// </summary>
public sealed class JsonRecordExporter : IRecordExporter
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public ExportFormat Format      => ExportFormat.Json;
    public string       ContentType => "application/json; charset=utf-8";
    public string       FileExtension => ".json";

    public async Task WriteAsync(
        IAsyncEnumerable<FinancialRecord> records,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        await using var writer = new Utf8JsonWriter(destination);
        writer.WriteStartArray();

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            var obj = new
            {
                id = record.Id,
                transactionDate = record.OccurredOn.ToString("yyyy-MM-dd"),
                merchantName = record.Description,
                normalizedMerchantName = record.Description,
                amount = record.Amount.Amount,
                currency = record.Amount.Currency,
                categoryId = record.CategoryId,
                accountId = record.AccountId,
                status = record.Status.ToString(),
                provenance = new
                {
                    sourceFile = record.Provenance?.Source,
                    importedAt = (string?)null,
                    confidenceScore = record.ClassificationConfidence?.Score,
                }
            };

            JsonSerializer.Serialize(writer, obj, s_options);
            await writer.FlushAsync(cancellationToken);
        }

        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken);
    }
}

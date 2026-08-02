using CsvHelper;
using CsvHelper.Configuration;
using FinancialOS.Core.Models;
using System.Globalization;

namespace FinancialOS.Infrastructure.Exporters;

/// <summary>
/// Exports records as a plain CSV file.
/// Columns: Date, Merchant, Amount, Category, Account, Notes
/// </summary>
public sealed class CsvRecordExporter : IRecordExporter
{
    public ExportFormat Format      => ExportFormat.Csv;
    public string       ContentType => "text/csv; charset=utf-8";
    public string       FileExtension => ".csv";

    public async Task WriteAsync(
        IAsyncEnumerable<FinancialRecord> records,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,        };

        await using var writer = new StreamWriter(destination, leaveOpen: true);
        await using var csv = new CsvWriter(writer, config);

        csv.WriteField("Date");
        csv.WriteField("Merchant");
        csv.WriteField("Amount");
        csv.WriteField("Category");
        csv.WriteField("Account");
        csv.WriteField("Notes");
        await csv.NextRecordAsync();

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            csv.WriteField(record.OccurredOn.ToString("yyyy-MM-dd"));
            csv.WriteField(record.Description);
            csv.WriteField(record.Amount.Amount);
            csv.WriteField(record.CategoryId?.ToString() ?? "");
            csv.WriteField(record.AccountId?.ToString() ?? "");
            csv.WriteField("");  // Notes — not present on FinancialRecord; placeholder
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync(cancellationToken);
    }
}

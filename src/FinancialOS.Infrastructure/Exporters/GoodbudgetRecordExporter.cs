using CsvHelper;
using CsvHelper.Configuration;
using FinancialOS.Core.Models;
using System.Globalization;

namespace FinancialOS.Infrastructure.Exporters;

/// <summary>
/// Exports records in Goodbudget CSV format.
/// Columns: Date, Envelope, Account, Name, Amount, Notes
/// Date format: MM/DD/YYYY; Amount is signed (negative = spending).
/// </summary>
public sealed class GoodbudgetRecordExporter : IRecordExporter
{
    public ExportFormat Format      => ExportFormat.Goodbudget;
    public string       ContentType => "text/csv; charset=utf-8";
    public string       FileExtension => "-goodbudget.csv";

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
        csv.WriteField("Envelope");
        csv.WriteField("Account");
        csv.WriteField("Name");
        csv.WriteField("Amount");
        csv.WriteField("Notes");
        await csv.NextRecordAsync();

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            csv.WriteField(record.OccurredOn.ToString("MM/dd/yyyy"));
            csv.WriteField(record.CategoryId?.ToString() ?? "");  // Envelope = category
            csv.WriteField(record.AccountId?.ToString() ?? "");   // Account
            csv.WriteField(record.Description);                    // Name = merchant
            csv.WriteField(record.Amount.Amount.ToString("F2", CultureInfo.InvariantCulture));
            csv.WriteField("");  // Notes
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync(cancellationToken);
    }
}

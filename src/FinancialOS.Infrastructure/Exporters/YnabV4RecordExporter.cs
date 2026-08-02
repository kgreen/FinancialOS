using CsvHelper;
using CsvHelper.Configuration;
using FinancialOS.Core.Models;
using System.Globalization;

namespace FinancialOS.Infrastructure.Exporters;

/// <summary>
/// Exports records in YNAB 4 CSV format.
/// Columns: Date, Payee, Memo, Outflow, Inflow
/// Date format: MM/DD/YYYY
/// Amount &lt; 0  → Outflow = positive abs value, Inflow = empty
/// Amount &gt;= 0 → Inflow  = positive value,     Outflow = "0.00"
/// </summary>
public sealed class YnabV4RecordExporter : IRecordExporter
{
    public ExportFormat Format      => ExportFormat.Ynab4;
    public string       ContentType => "text/csv; charset=utf-8";
    public string       FileExtension => "-ynab4.csv";

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
        csv.WriteField("Payee");
        csv.WriteField("Memo");
        csv.WriteField("Outflow");
        csv.WriteField("Inflow");
        await csv.NextRecordAsync();

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            var amount = record.Amount.Amount;
            var date   = record.OccurredOn.ToString("MM/dd/yyyy");

            string outflow;
            string inflow;

            if (amount < 0)
            {
                outflow = Math.Abs(amount).ToString("F2", CultureInfo.InvariantCulture);
                inflow  = "";
            }
            else
            {
                // Amount == 0: Outflow = 0.00, Inflow empty (per contracts/exports.md)
                outflow = "0.00";
                inflow  = amount > 0
                    ? amount.ToString("F2", CultureInfo.InvariantCulture)
                    : "";
            }

            csv.WriteField(date);
            csv.WriteField(record.Description);
            csv.WriteField("");  // Memo
            csv.WriteField(outflow);
            csv.WriteField(inflow);
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync(cancellationToken);
    }
}

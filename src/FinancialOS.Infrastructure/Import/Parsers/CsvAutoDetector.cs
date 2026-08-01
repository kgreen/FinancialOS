using FinancialOS.Core.Models;

namespace FinancialOS.Infrastructure.Import.Parsers;

/// <summary>
/// Known CSV header fingerprints for common bank exports.
/// Normalizes headers to lowercase, strips punctuation, collapses whitespace before matching.
/// </summary>
public sealed class CsvAutoDetector
{
    public sealed record DetectedCsvLayout(
        string Name,
        string DateColumn,
        string AmountColumn,
        string DescriptionColumn,
        string? BalanceColumn,
        string? ReferenceColumn,
        AmountLayout AmountLayout,
        string? DebitColumn,
        string? CreditColumn,
        string? DateFormatPattern
    );

    private static string Normalize(string header)
    {
        var lower = header.ToLowerInvariant();
        // Strip all punctuation except letters, digits, spaces
        var cleaned = new string(lower.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());
        // Collapse whitespace
        return string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    // Known bank layouts: key is the set of normalized required headers
    private static readonly List<(HashSet<string> RequiredHeaders, DetectedCsvLayout Layout)> KnownLayouts = new()
    {
        // Chase Checking: "Transaction Date","Description","Category","Type","Amount","Balance"
        (
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "transaction date", "description", "amount" },
            new DetectedCsvLayout("chase-checking", "Transaction Date", "Amount", "Description", "Balance", null, AmountLayout.SingleSigned, null, null, "MM/dd/yyyy")
        ),
        // Chase Credit: "Transaction Date","Post Date","Description","Category","Type","Amount","Memo"
        (
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "transaction date", "post date", "description", "amount" },
            new DetectedCsvLayout("chase-credit", "Transaction Date", "Amount", "Description", null, null, AmountLayout.SingleSigned, null, null, "MM/dd/yyyy")
        ),
        // Ally Bank: "Date","Time","Amount","Type","Description"
        (
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "date", "time", "amount", "type", "description" },
            new DetectedCsvLayout("ally-bank", "Date", "Amount", "Description", null, null, AmountLayout.SingleSigned, null, null, "MM/dd/yyyy")
        ),
        // Citi Checking: "Status","Date","Description","Debit","Credit"
        (
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "status", "date", "description", "debit", "credit" },
            new DetectedCsvLayout("citi-checking", "Date", "", "Description", null, null, AmountLayout.SplitDebitCredit, "Debit", "Credit", null)
        ),
        // Discover: "Trans. Date","Post Date","Description","Amount","Category"
        (
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "trans date", "post date", "description", "amount", "category" },
            new DetectedCsvLayout("discover", "Trans. Date", "Amount", "Description", null, null, AmountLayout.SingleSigned, null, null, "MM/dd/yyyy")
        ),
        // BofA Checking: "Date","Description","Amount","Running Bal."
        (
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "date", "description", "amount", "running bal" },
            new DetectedCsvLayout("bofa-checking", "Date", "Amount", "Description", "Running Bal.", null, AmountLayout.SingleSigned, null, null, "MM/dd/yyyy")
        ),
        // Capital One: "Transaction Date","Posted Date","Card No.","Description","Category","Debit","Credit"
        (
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "transaction date", "posted date", "description", "debit", "credit" },
            new DetectedCsvLayout("capital-one", "Transaction Date", "", "Description", null, null, AmountLayout.SplitDebitCredit, "Debit", "Credit", "MM/dd/yyyy")
        ),
        // Generic signed: date + amount + description
        (
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "date", "amount", "description" },
            new DetectedCsvLayout("generic-signed", "Date", "Amount", "Description", null, null, AmountLayout.SingleSigned, null, null, null)
        ),
    };

    public bool TryDetect(string[] headers, out DetectedCsvLayout? layout)
    {
        var normalized = headers.Select(Normalize).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (required, candidate) in KnownLayouts)
        {
            if (required.IsSubsetOf(normalized))
            {
                layout = candidate;
                return true;
            }
        }

        layout = null;
        return false;
    }
}

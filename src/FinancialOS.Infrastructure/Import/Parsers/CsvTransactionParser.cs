using CsvHelper;
using CsvHelper.Configuration;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using System.Globalization;

namespace FinancialOS.Infrastructure.Import.Parsers;

public sealed class CsvTransactionParser : ITransactionParser
{
    private readonly CsvAutoDetector _autoDetector;

    public CsvTransactionParser(CsvAutoDetector autoDetector)
    {
        _autoDetector = autoDetector;
    }

    public ParserType ParserType { get; private set; } = ParserType.CsvAutoDetected;

    public bool CanParse(string fileName, EvidenceSourceType sourceType)
        => Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase);

    public async Task<TransactionParseResult> ParseAsync(
        Stream stream,
        InstitutionProfile? profile,
        CancellationToken cancellationToken = default)
    {
        var transactions = new List<ParsedTransaction>();
        var failedRows = new List<FailedRowEntry>();
        var seenFingerprints = new HashSet<string>(StringComparer.Ordinal);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim,
        };

        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        // Resolve column mapping
        string? dateCol, amountCol, descCol, balanceCol, referenceCol, debitCol, creditCol, dateFormat;
        AmountLayout amountLayout;

        if (profile is not null)
        {
            ParserType = ParserType.CsvConfigured;
            profile.ColumnMappings.TryGetValue("date", out dateCol);
            profile.ColumnMappings.TryGetValue("amount", out amountCol);
            profile.ColumnMappings.TryGetValue("description", out descCol);
            profile.ColumnMappings.TryGetValue("balance", out balanceCol);
            profile.ColumnMappings.TryGetValue("reference", out referenceCol);
            amountLayout = profile.AmountLayout;
            debitCol = profile.DebitColumnName;
            creditCol = profile.CreditColumnName;
            dateFormat = profile.DateFormatPattern;
        }
        else
        {
            if (!_autoDetector.TryDetect(headers, out var detected) || detected is null)
            {
                var headerList = string.Join(", ", headers.Select(h => $"[{h}]"));
                throw new CsvLayoutUndetectableException(
                    $"Could not auto-detect CSV layout. Detected headers: [{string.Join(", ", headers)}]",
                    headers);
            }

            ParserType = ParserType.CsvAutoDetected;
            dateCol = detected.DateColumn;
            amountCol = detected.AmountColumn;
            descCol = detected.DescriptionColumn;
            balanceCol = detected.BalanceColumn;
            referenceCol = detected.ReferenceColumn;
            amountLayout = detected.AmountLayout;
            debitCol = detected.DebitColumn;
            creditCol = detected.CreditColumn;
            dateFormat = detected.DateFormatPattern;
        }

        var rowIndex = 0;
        var totalRowsScanned = 0;

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            totalRowsScanned++;
            var currentRow = rowIndex++;

            // Parse date
            var dateRaw = GetField(csv, dateCol);
            if (string.IsNullOrWhiteSpace(dateRaw))
            {
                failedRows.Add(new FailedRowEntry(currentRow, "Missing required field: date"));
                continue;
            }

            if (!TryParseDate(dateRaw, dateFormat, out var transDate))
            {
                failedRows.Add(new FailedRowEntry(currentRow, $"Date is not parseable: '{dateRaw}'"));
                continue;
            }

            // Parse amount
            decimal amount;
            if (amountLayout == AmountLayout.SplitDebitCredit)
            {
                var debitRaw = GetField(csv, debitCol);
                var creditRaw = GetField(csv, creditCol);
                var hasDebit = !string.IsNullOrWhiteSpace(debitRaw) && debitRaw != "0" && debitRaw != "0.00";
                var hasCredit = !string.IsNullOrWhiteSpace(creditRaw) && creditRaw != "0" && creditRaw != "0.00";

                if (hasDebit && hasCredit)
                {
                    failedRows.Add(new FailedRowEntry(currentRow, "Both debit and credit fields are populated; ambiguous amount"));
                    continue;
                }

                if (hasDebit)
                {
                    if (!TryParseDecimal(debitRaw!, out var debitAmt))
                    {
                        failedRows.Add(new FailedRowEntry(currentRow, $"Amount is not a valid decimal: '{debitRaw}'"));
                        continue;
                    }
                    amount = -Math.Abs(debitAmt); // stored negative
                }
                else if (hasCredit)
                {
                    if (!TryParseDecimal(creditRaw!, out var creditAmt))
                    {
                        failedRows.Add(new FailedRowEntry(currentRow, $"Amount is not a valid decimal: '{creditRaw}'"));
                        continue;
                    }
                    amount = Math.Abs(creditAmt); // stored positive
                }
                else
                {
                    failedRows.Add(new FailedRowEntry(currentRow, "Missing required field: amount (both debit and credit are empty)"));
                    continue;
                }
            }
            else
            {
                var amountRaw = GetField(csv, amountCol);
                if (string.IsNullOrWhiteSpace(amountRaw))
                {
                    failedRows.Add(new FailedRowEntry(currentRow, "Missing required field: amount"));
                    continue;
                }
                if (!TryParseDecimal(amountRaw, out amount))
                {
                    failedRows.Add(new FailedRowEntry(currentRow, $"Amount is not a valid decimal: '{amountRaw}'"));
                    continue;
                }
            }

            // Parse description
            var description = GetField(csv, descCol) ?? string.Empty;

            // Parse optional fields
            decimal? balance = null;
            var balanceRaw = GetField(csv, balanceCol);
            if (!string.IsNullOrWhiteSpace(balanceRaw) && TryParseDecimal(balanceRaw, out var parsedBalance))
                balance = parsedBalance;

            var reference = GetField(csv, referenceCol);
            var rawRow = csv.Parser.RawRecord?.TrimEnd() ?? string.Empty;

            // Within-file duplicate detection (FR-019)
            var fingerprint = $"{transDate:yyyy-MM-dd}|{amount}|{description}";
            if (!seenFingerprints.Add(fingerprint))
            {
                failedRows.Add(new FailedRowEntry(currentRow, "Duplicate row within file"));
                continue;
            }

            transactions.Add(new ParsedTransaction(
                TransactionDate: transDate,
                Amount: amount,
                Description: description,
                Balance: balance,
                ExternalReferenceId: string.IsNullOrWhiteSpace(reference) ? null : reference,
                RowIndex: currentRow,
                RawRow: rawRow
            ));
        }

        return new TransactionParseResult(transactions, failedRows, totalRowsScanned);
    }

    private static string? GetField(CsvReader csv, string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName)) return null;
        try { return csv.GetField<string>(columnName); }
        catch { return null; }
    }

    private static bool TryParseDate(string raw, string? format, out DateOnly result)
    {
        // Clean common currency/text artefacts
        raw = raw.Trim();

        if (format is not null)
        {
            if (DateOnly.TryParseExact(raw, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;
        }

        // Try common formats in order
        string[] formats = { "MM/dd/yyyy", "yyyy-MM-dd", "dd-MMM-yyyy", "M/d/yyyy", "yyyy/MM/dd", "dd/MM/yyyy", "MM-dd-yyyy" };
        foreach (var f in formats)
        {
            if (DateOnly.TryParseExact(raw, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;
        }

        result = default;
        return false;
    }

    private static bool TryParseDecimal(string raw, out decimal result)
    {
        // Strip currency symbols and thousands separators
        raw = raw.Trim().Replace("$", "").Replace(",", "").Replace("(", "-").Replace(")", "");
        return decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
    }
}

/// <summary>Thrown when CsvAutoDetector cannot identify the CSV layout.</summary>
public sealed class CsvLayoutUndetectableException : Exception
{
    public string[] DetectedHeaders { get; }
    public CsvLayoutUndetectableException(string message, string[] detectedHeaders) : base(message)
    {
        DetectedHeaders = detectedHeaders;
    }
}

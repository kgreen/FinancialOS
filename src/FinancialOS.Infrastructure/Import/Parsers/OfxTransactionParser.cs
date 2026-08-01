using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Infrastructure.Import.Parsers;

public sealed class OfxTransactionParser : ITransactionParser
{
    public ParserType ParserType => ParserType.Ofx;

    public bool CanParse(string fileName, EvidenceSourceType sourceType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext == ".ofx" || ext == ".qfx";
    }

    public async Task<TransactionParseResult> ParseAsync(
        Stream stream,
        InstitutionProfile? profile,
        CancellationToken cancellationToken = default)
    {
        var content = await new StreamReader(stream, leaveOpen: true).ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
            throw new OfxFormatException("Not a recognizable OFX/QFX file");

        var trimmed = content.TrimStart();

        // Detect format
        if (trimmed.StartsWith("OFXHEADER:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("DATA:OFXSGML", StringComparison.OrdinalIgnoreCase))
        {
            return ParseSgml(content);
        }

        if (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<OFX", StringComparison.OrdinalIgnoreCase))
        {
            return ParseXml(content);
        }

        throw new OfxFormatException("Not a recognizable OFX/QFX file");
    }

    private static TransactionParseResult ParseSgml(string content)
    {
        var transactions = new List<ParsedTransaction>();
        var failedRows = new List<FailedRowEntry>();
        var seenFitids = new HashSet<string>(StringComparer.Ordinal);

        // Find all STMTTRN blocks
        var blockPattern = new Regex(@"<STMTTRN>(.*?)</STMTTRN>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var tagPattern = new Regex(@"<(\w+)>([^<\r\n]*)", RegexOptions.IgnoreCase);

        var blockMatches = blockPattern.Matches(content);
        var rowIndex = 0;

        foreach (Match blockMatch in blockMatches)
        {
            var currentRow = rowIndex++;
            var block = blockMatch.Value;
            var tagMatches = tagPattern.Matches(block);
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match tagMatch in tagMatches)
            {
                fields[tagMatch.Groups[1].Value] = tagMatch.Groups[2].Value.Trim();
            }

            var result = ExtractTransaction(fields, currentRow, block, seenFitids);
            if (result.IsError)
                failedRows.Add(new FailedRowEntry(currentRow, result.ErrorReason!));
            else
                transactions.Add(result.Transaction!);
        }

        return new TransactionParseResult(transactions, failedRows, rowIndex);
    }

    private static TransactionParseResult ParseXml(string content)
    {
        var transactions = new List<ParsedTransaction>();
        var failedRows = new List<FailedRowEntry>();
        var seenFitids = new HashSet<string>(StringComparer.Ordinal);

        XDocument doc;
        try { doc = XDocument.Parse(content); }
        catch (Exception ex) { throw new OfxFormatException($"OFX XML is not well-formed: {ex.Message}"); }

        var stmtTrns = doc.Descendants("STMTTRN").ToList();
        var rowIndex = 0;

        foreach (var element in stmtTrns)
        {
            var currentRow = rowIndex++;
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in element.Elements())
                fields[child.Name.LocalName] = child.Value.Trim();

            var raw = element.ToString();
            var result = ExtractTransaction(fields, currentRow, raw, seenFitids);
            if (result.IsError)
                failedRows.Add(new FailedRowEntry(currentRow, result.ErrorReason!));
            else
                transactions.Add(result.Transaction!);
        }

        return new TransactionParseResult(transactions, failedRows, rowIndex);
    }

    private sealed record ExtractionResult(
        ParsedTransaction? Transaction,
        bool IsError,
        string? ErrorReason
    );

    private static ExtractionResult ExtractTransaction(
        Dictionary<string, string> fields,
        int rowIndex,
        string rawBlock,
        HashSet<string> seenFitids)
    {
        // DTPOSTED → date
        if (!fields.TryGetValue("DTPOSTED", out var dtPosted) || string.IsNullOrWhiteSpace(dtPosted))
            return new ExtractionResult(null, true, "Missing required field: DTPOSTED");

        if (!TryParseOfxDate(dtPosted, out var transDate))
            return new ExtractionResult(null, true, $"Date is not parseable: '{dtPosted}'");

        // TRNAMT → amount
        if (!fields.TryGetValue("TRNAMT", out var trnaRaw) || string.IsNullOrWhiteSpace(trnaRaw))
            return new ExtractionResult(null, true, "Missing required field: TRNAMT");

        if (!decimal.TryParse(trnaRaw, NumberStyles.Number | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var amount))
            return new ExtractionResult(null, true, $"Amount is not a valid decimal: '{trnaRaw}'");

        // NAME → description, fallback to MEMO
        fields.TryGetValue("NAME", out var name);
        fields.TryGetValue("MEMO", out var memo);
        var description = (!string.IsNullOrWhiteSpace(name) ? name : memo) ?? string.Empty;

        // FITID → ExternalReferenceId
        fields.TryGetValue("FITID", out var fitid);
        var externalRef = string.IsNullOrWhiteSpace(fitid) ? null : fitid;

        // Within-file FITID duplicate detection (FR-019)
        if (externalRef is not null && !seenFitids.Add(externalRef))
            return new ExtractionResult(null, true, $"Duplicate FITID within file: {externalRef}");

        var tx = new ParsedTransaction(
            TransactionDate: transDate,
            Amount: amount,
            Description: description,
            Balance: null,
            ExternalReferenceId: externalRef,
            RowIndex: rowIndex,
            RawRow: rawBlock.Length > 2000 ? rawBlock[..2000] : rawBlock
        );

        return new ExtractionResult(tx, false, null);
    }

    private static bool TryParseOfxDate(string raw, out DateOnly result)
    {
        // OFX date formats: yyyyMMddHHmmss[.xxx][TZ] or yyyyMMdd
        raw = raw.Trim();
        // Strip timezone suffix (e.g. [+5:EST])
        var bracketIdx = raw.IndexOf('[');
        if (bracketIdx >= 0) raw = raw[..bracketIdx];

        // Try yyyyMMddHHmmss or shorter variants
        if (raw.Length >= 8)
        {
            var datePart = raw[..8];
            if (DateOnly.TryParseExact(datePart, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out result))
                return true;
        }

        result = default;
        return false;
    }
}

/// <summary>Thrown when an OFX/QFX file is not in a recognizable format.</summary>
public sealed class OfxFormatException : Exception
{
    public OfxFormatException(string message) : base(message) { }
}

using FinancialOS.Core.Models;

namespace FinancialOS.Core.Contracts;

public sealed record TransactionParseResult(
    IReadOnlyList<ParsedTransaction> Transactions,
    IReadOnlyList<FailedRowEntry> FailedRows,
    int TotalRowsScanned
);

public interface ITransactionParser
{
    ParserType ParserType { get; }
    bool CanParse(string fileName, EvidenceSourceType sourceType);
    Task<TransactionParseResult> ParseAsync(Stream stream, InstitutionProfile? profile, CancellationToken cancellationToken = default);
}

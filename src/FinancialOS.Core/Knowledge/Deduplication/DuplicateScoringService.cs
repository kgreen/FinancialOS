using System.Text.Json;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Knowledge.Deduplication;

public sealed record DuplicateScoreResult(
    decimal Confidence,
    IReadOnlyList<string> ReasonCodes,
    string SignalSnapshotJson);

public sealed class DuplicateScoringService
{
    public DuplicateScoreResult Score(FinancialRecord source, FinancialRecord candidate)
    {
        var reasonCodes = new List<string>();
        decimal score = 0m;

        var sameAccount = source.AccountId.HasValue && source.AccountId == candidate.AccountId;
        if (sameAccount)
        {
            score += 0.30m;
            reasonCodes.Add("same-account");
        }

        decimal? amountDelta = null;
        var amountMatch = false;
        var amountCurrencyMatch = string.Equals(source.Amount.Currency, candidate.Amount.Currency, StringComparison.OrdinalIgnoreCase);
        if (amountCurrencyMatch)
        {
            amountDelta = Math.Abs(source.Amount.Amount - candidate.Amount.Amount);
            amountMatch = amountDelta <= 0.01m;
        }

        if (amountMatch)
        {
            score += 0.35m;
            reasonCodes.Add("amount-match");
        }

        var dayDelta = Math.Abs((source.OccurredOn.UtcDateTime.Date - candidate.OccurredOn.UtcDateTime.Date).TotalDays);
        var nearDate = dayDelta <= 2;
        if (nearDate)
        {
            score += 0.20m;
            reasonCodes.Add("date-near");
        }

        var textSimilarity = TokenJaccardSimilarity(source.Description, candidate.Description);
        if (textSimilarity >= 0.5m)
        {
            score += 0.15m;
            reasonCodes.Add("text-similar");
        }

        var clamped = Math.Clamp(score, 0m, 1m);
        var signalSnapshot = JsonSerializer.Serialize(new
        {
            sameAccount,
            amountCurrencyMatch,
            amountDelta,
            amountMatch,
            dayDelta,
            nearDate,
            textSimilarity
        });

        return new DuplicateScoreResult(clamped, reasonCodes, signalSnapshot);
    }

    private static decimal TokenJaccardSimilarity(string left, string right)
    {
        var leftTokens = Tokenize(left);
        var rightTokens = Tokenize(right);
        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return 0m;
        }

        var intersection = leftTokens.Intersect(rightTokens, StringComparer.Ordinal).Count();
        var union = leftTokens.Union(rightTokens, StringComparer.Ordinal).Count();
        if (union == 0)
        {
            return 0m;
        }

        return decimal.Divide(intersection, union);
    }

    private static HashSet<string> Tokenize(string value) =>
        value.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
}

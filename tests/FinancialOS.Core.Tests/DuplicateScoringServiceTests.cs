using FinancialOS.Core.Knowledge.Deduplication;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Tests;

public sealed class DuplicateScoringServiceTests
{
    private static FinancialRecord MakeRecord(
        Guid accountId,
        decimal amount,
        DateTimeOffset occurredOn,
        string description) =>
        new()
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Amount = new Money(amount, "USD"),
            OccurredOn = occurredOn,
            Description = description
        };

    [Fact]
    public void Score_AllSignalsAlign_ProducesHighConfidence()
    {
        var service = new DuplicateScoringService();
        var accountId = Guid.NewGuid();
        var first = MakeRecord(accountId, 45.10m, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), "Whole Foods Market");
        var second = MakeRecord(accountId, 45.10m, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), "Whole Foods");

        var result = service.Score(first, second);

        Assert.True(result.Confidence >= 0.80m);
        Assert.Contains("same-account", result.ReasonCodes);
        Assert.Contains("amount-match", result.ReasonCodes);
        Assert.Contains("date-near", result.ReasonCodes);
        Assert.Contains("text-similar", result.ReasonCodes);
    }

    [Fact]
    public void Score_NoSignalsAlign_ProducesLowConfidence()
    {
        var service = new DuplicateScoringService();
        var first = MakeRecord(Guid.NewGuid(), 12m, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), "Coffee Shop");
        var second = MakeRecord(Guid.NewGuid(), 200m, new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), "Utility Bill");

        var result = service.Score(first, second);

        Assert.True(result.Confidence < 0.30m);
        Assert.Empty(result.ReasonCodes);
    }

    [Fact]
    public void Score_AmountAndDateMatch_WithoutTextSimilarity_StillScores()
    {
        var service = new DuplicateScoringService();
        var accountId = Guid.NewGuid();
        var first = MakeRecord(accountId, 89.99m, new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero), "Vendor A");
        var second = MakeRecord(accountId, 89.99m, new DateTimeOffset(2026, 1, 11, 0, 0, 0, TimeSpan.Zero), "Completely Different Name");

        var result = service.Score(first, second);

        Assert.True(result.Confidence >= 0.65m);
        Assert.Contains("same-account", result.ReasonCodes);
        Assert.Contains("amount-match", result.ReasonCodes);
        Assert.Contains("date-near", result.ReasonCodes);
    }

    [Fact]
    public void Score_AlwaysClampsConfidenceToRange()
    {
        var service = new DuplicateScoringService();
        var accountId = Guid.NewGuid();
        var first = MakeRecord(accountId, 10m, DateTimeOffset.UtcNow, "one");
        var second = MakeRecord(accountId, 10m, DateTimeOffset.UtcNow, "one");

        var result = service.Score(first, second);

        Assert.InRange(result.Confidence, 0m, 1m);
    }
}

using Xunit;

namespace FinancialOS.Api.Tests;

public static class KnowledgeAssertions
{
    public static void AssertDeterministicOrder<T, TKey>(IEnumerable<T> firstRun, IEnumerable<T> secondRun, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        var first = firstRun.Select(keySelector).ToList();
        var second = secondRun.Select(keySelector).ToList();
        Assert.Equal(first, second);
    }

    public static void AssertConfidenceInRange(decimal confidenceValue)
    {
        Assert.InRange(confidenceValue, 0m, 1m);
    }

    public static void AssertHasReasonCode(string? reasonCode)
    {
        Assert.False(string.IsNullOrWhiteSpace(reasonCode));
    }
}

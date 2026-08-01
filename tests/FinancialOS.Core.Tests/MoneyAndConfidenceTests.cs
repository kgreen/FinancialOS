using FinancialOS.Core.Models;

namespace FinancialOS.Core.Tests;

public sealed class MoneyAndConfidenceTests
{
    [Fact]
    public void Money_Zero_DefaultsToUsd()
    {
        var result = Money.Zero();
        Assert.Equal(0m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Confidence_ClampsToRange()
    {
        var result = new Confidence(1.5m);
        Assert.Equal(1m, result.Score);
    }
}

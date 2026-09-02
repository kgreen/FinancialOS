using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using FinancialOS.Core.Services;
using FinancialOS.Data;
using Microsoft.Extensions.Configuration;

namespace FinancialOS.Core.Tests;

public sealed class StewardshipServicesTests
{
    [Fact]
    public async Task GenerateAsync_WithMatchingGoalsAndBudgets_ReturnsProgressSnapshots()
    {
        var repository = new InMemoryFinancialRepository();
        var goal = new Goal
        {
            Name = "Emergency fund",
            TargetAmount = 25m,
            Currency = "USD"
        };
        var budget = new Budget
        {
            Name = "Groceries",
            LimitAmount = 50m,
            Currency = "USD"
        };

        await repository.AddGoalAsync(goal);
        await repository.AddBudgetAsync(budget);
        await repository.AddRecordAsync(new FinancialRecord
        {
            Description = "Groceries",
            Amount = new Money(25m, "USD"),
            OccurredOn = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero),
            AccountId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid()
        });

        var service = new StewardshipInsightService(repository);
        var insight = await service.GenerateAsync(new InsightRequest
        {
            StartDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2025, 1, 31, 0, 0, 0, TimeSpan.Zero)
        });

        Assert.Equal("OnTrack", insight.AlignmentStatus);
        Assert.Single(insight.GoalProgress);
        Assert.Single(insight.BudgetProgress);
        Assert.Equal(25m, insight.GoalProgress[0].ActualAmount);
        Assert.Equal(25m, insight.BudgetProgress[0].ActualAmount);
    }

    [Fact]
    public async Task GenerateAsync_WhenAdvisorDisabled_ReturnsFallbackRecommendation()
    {
        var repository = new InMemoryFinancialRepository();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Advisor:Enabled"] = "false"
            })
            .Build();
        var service = new AdvisorService(configuration, repository);

        var recommendation = await service.GenerateAsync(new RecommendationRequest
        {
            StartDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2025, 1, 31, 0, 0, 0, TimeSpan.Zero)
        });

        Assert.Equal("Fallback", recommendation.Status);
        Assert.Equal("Advisor unavailable", recommendation.Title);
    }
}

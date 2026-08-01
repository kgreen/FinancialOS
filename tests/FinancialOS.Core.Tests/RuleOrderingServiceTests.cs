using FinancialOS.Core.Knowledge.Rules;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Tests;

public sealed class RuleOrderingServiceTests
{
    private static ClassificationRule MakeRule(int priority, Guid? id = null, DateTimeOffset? createdAt = null, RuleStatus status = RuleStatus.Active)
    {
        return new ClassificationRule
        {
            Id = id ?? Guid.NewGuid(),
            Name = $"Rule-{priority}",
            Priority = priority,
            Status = status,
            CreatedAtUtc = createdAt ?? DateTimeOffset.UtcNow,
            ConditionJson = "{}",
            EffectiveFromUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    [Fact]
    public void OrderRules_ByPriorityDescending_HighestFirst()
    {
        var rules = new[]
        {
            MakeRule(100),
            MakeRule(900),
            MakeRule(500)
        };

        var ordered = RuleOrderingService.OrderForEvaluation(rules).ToList();

        Assert.Equal(900, ordered[0].Priority);
        Assert.Equal(500, ordered[1].Priority);
        Assert.Equal(100, ordered[2].Priority);
    }

    [Fact]
    public void OrderRules_TiedPriority_OlderRuleFirst()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var older = MakeRule(500, createdAt: baseTime);
        var newer = MakeRule(500, createdAt: baseTime.AddMinutes(5));

        var ordered = RuleOrderingService.OrderForEvaluation(new[] { newer, older }).ToList();

        Assert.Equal(older.Id, ordered[0].Id);
        Assert.Equal(newer.Id, ordered[1].Id);
    }

    [Fact]
    public void OrderRules_TiedPriorityAndCreatedAt_LowestIdFirst()
    {
        var sameTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var idA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var idB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var ruleA = MakeRule(500, id: idA, createdAt: sameTime);
        var ruleB = MakeRule(500, id: idB, createdAt: sameTime);

        var ordered = RuleOrderingService.OrderForEvaluation(new[] { ruleB, ruleA }).ToList();

        Assert.Equal(idA, ordered[0].Id);
        Assert.Equal(idB, ordered[1].Id);
    }

    [Fact]
    public void OrderRules_InactiveRulesExcluded()
    {
        var active = MakeRule(500, status: RuleStatus.Active);
        var inactive = MakeRule(900, status: RuleStatus.Inactive);

        var ordered = RuleOrderingService.OrderForEvaluation(new[] { active, inactive }).ToList();

        Assert.Single(ordered);
        Assert.Equal(active.Id, ordered[0].Id);
    }

    [Fact]
    public void OrderRules_EmptyInput_ReturnsEmpty()
    {
        var ordered = RuleOrderingService.OrderForEvaluation(Array.Empty<ClassificationRule>()).ToList();
        Assert.Empty(ordered);
    }

    [Fact]
    public void OrderRules_IsStable_SameResultOnRepeat()
    {
        var rules = new[]
        {
            MakeRule(200),
            MakeRule(800),
            MakeRule(500),
            MakeRule(800)
        };

        var first = RuleOrderingService.OrderForEvaluation(rules).Select(r => r.Id).ToList();
        var second = RuleOrderingService.OrderForEvaluation(rules).Select(r => r.Id).ToList();

        Assert.Equal(first, second);
    }
}

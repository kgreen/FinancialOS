using FinancialOS.Core.Models;

namespace FinancialOS.Core.Knowledge.Rules;

/// <summary>
/// Provides deterministic, stable ordering for classification rule evaluation.
/// Tie-breaking order: Priority desc → CreatedAtUtc asc → Id asc.
/// </summary>
public static class RuleOrderingService
{
    public static IOrderedEnumerable<ClassificationRule> OrderForEvaluation(IEnumerable<ClassificationRule> rules)
    {
        return rules
            .Where(r => r.Status == RuleStatus.Active)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAtUtc)
            .ThenBy(r => r.Id);
    }
}

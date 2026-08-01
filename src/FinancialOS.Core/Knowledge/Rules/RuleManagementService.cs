using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Knowledge.Rules;

/// <summary>
/// Manages the lifecycle of classification rules: create, activate, deactivate, reprioritize.
/// </summary>
public sealed class RuleManagementService : IRuleManagementService
{
    private readonly IFinancialRepository _repository;

    public RuleManagementService(IFinancialRepository repository)
    {
        _repository = repository;
    }

    public async Task<ClassificationRule> CreateAsync(ClassificationRule rule, CancellationToken cancellationToken = default)
    {
        rule.CreatedAtUtc = DateTimeOffset.UtcNow;
        rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        return await _repository.AddClassificationRuleAsync(rule, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassificationRule>> ListAsync(CancellationToken cancellationToken = default)
    {
        var all = await _repository.ListClassificationRulesAsync(cancellationToken);
        // Deterministic ordering for list/read API: priority desc, then created time, then id.
        return all
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAtUtc)
            .ThenBy(r => r.Id)
            .ToList();
    }

    public async Task<ClassificationRule?> UpdateAsync(ClassificationRule rule, CancellationToken cancellationToken = default)
    {
        rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        return await _repository.UpdateClassificationRuleAsync(rule, cancellationToken);
    }
}

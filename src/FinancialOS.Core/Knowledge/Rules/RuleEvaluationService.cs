using System.Text.Json;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Knowledge.Rules;

/// <summary>
/// Evaluates active classification rules against a financial record in deterministic order.
/// Condition fields supported: merchantContains, amountMin, amountMax, accountId.
/// </summary>
public sealed class RuleEvaluationService : IRuleEvaluationService
{
    private readonly IFinancialRepository _repository;

    public RuleEvaluationService(IFinancialRepository repository)
    {
        _repository = repository;
    }

    public async Task<RuleEvaluationResult?> EvaluateAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        var allRules = await _repository.ListClassificationRulesAsync(cancellationToken);
        var ordered = RuleOrderingService.OrderForEvaluation(allRules);

        foreach (var rule in ordered)
        {
            if (!IsInEffect(rule))
            {
                continue;
            }

            var (matched, reasonCodes) = EvaluateCondition(rule, record);
            if (matched)
            {
                return new RuleEvaluationResult(
                    RuleId: rule.Id,
                    RuleName: rule.Name,
                    Confidence: 1.0m,
                    ReasonCodes: reasonCodes,
                    TargetMerchantId: rule.TargetMerchantId,
                    TargetCategoryId: rule.TargetCategoryId);
            }
        }

        return null;
    }

    private static bool IsInEffect(ClassificationRule rule)
    {
        var now = DateTimeOffset.UtcNow;
        return rule.EffectiveFromUtc <= now
            && (rule.EffectiveToUtc is null || rule.EffectiveToUtc > now);
    }

    private static (bool Matched, IReadOnlyList<string> ReasonCodes) EvaluateCondition(
        ClassificationRule rule, FinancialRecord record)
    {
        if (string.IsNullOrWhiteSpace(rule.ConditionJson))
        {
            // An empty condition matches anything — caller is responsible for ensuring this is intentional.
            return (true, new[] { "condition-match", "open-condition" });
        }

        JsonElement condition;
        try
        {
            condition = JsonDocument.Parse(rule.ConditionJson).RootElement;
        }
        catch (JsonException)
        {
            return (false, Array.Empty<string>());
        }

        var reasonCodes = new List<string>();
        var allPass = true;
        var hasEvaluatedCondition = false;

        if (condition.TryGetProperty("merchantContains", out var merchantContains))
        {
            hasEvaluatedCondition = true;
            var pattern = merchantContains.GetString();
            if (string.IsNullOrWhiteSpace(pattern))
            {
                allPass = false;
            }
            else
            {
                var match = record.Description.Contains(pattern, StringComparison.OrdinalIgnoreCase);
                if (match)
                {
                    reasonCodes.Add("merchant-contains");
                }
                else
                {
                    allPass = false;
                }
            }
        }

        if (condition.TryGetProperty("amountMin", out var amountMin))
        {
            hasEvaluatedCondition = true;
            if (amountMin.TryGetDecimal(out var amountMinValue) && record.Amount.Amount >= amountMinValue)
            {
                reasonCodes.Add("amount-min-pass");
            }
            else
            {
                allPass = false;
            }
        }

        if (condition.TryGetProperty("amountMax", out var amountMax))
        {
            hasEvaluatedCondition = true;
            if (amountMax.TryGetDecimal(out var amountMaxValue) && record.Amount.Amount <= amountMaxValue)
            {
                reasonCodes.Add("amount-max-pass");
            }
            else
            {
                allPass = false;
            }
        }

        if (condition.TryGetProperty("accountId", out var accountId))
        {
            hasEvaluatedCondition = true;
            var targetAccount = accountId.GetString();
            if (!string.IsNullOrWhiteSpace(targetAccount)
                && Guid.TryParse(targetAccount, out var targetAccountId)
                && record.AccountId == targetAccountId)
            {
                reasonCodes.Add("account-match");
            }
            else
            {
                allPass = false;
            }
        }

        if (!hasEvaluatedCondition)
        {
            return (true, new[] { "condition-match", "open-condition" });
        }

        if (allPass && reasonCodes.Count > 0)
        {
            reasonCodes.Add("condition-match");
        }

        return (allPass, reasonCodes);
    }
}

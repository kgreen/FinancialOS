using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using Microsoft.Extensions.Configuration;

namespace FinancialOS.Core.Services;

public sealed class AdvisorService : IAdvisorService
{
    private readonly IConfiguration _configuration;
    private readonly IFinancialRepository _repository;

    public AdvisorService(IConfiguration configuration, IFinancialRepository repository)
    {
        _configuration = configuration;
        _repository = repository;
    }

    public async Task<AdvisorRecommendation> GenerateAsync(RecommendationRequest request, CancellationToken cancellationToken = default)
    {
        var enabledValue = _configuration["Advisor:Enabled"];
        var enabled = true;
        if (!string.IsNullOrWhiteSpace(enabledValue) && bool.TryParse(enabledValue, out var parsedEnabled))
        {
            enabled = parsedEnabled;
        }

        if (!enabled)
        {
            return new AdvisorRecommendation
            {
                Title = "Advisor unavailable",
                Summary = "Advisor output is currently disabled.",
                Status = "Fallback",
                Confidence = 0.1m,
                Rationale = "The advisor service is disabled by configuration, so a deterministic fallback is being returned.",
                Evidence = new[]
                {
                    new EvidenceReference { Type = "Configuration", Label = "Advisor enabled", Detail = "Advisor generation is disabled.", Amount = 0 }
                }
            };
        }

        var records = (await _repository.ListRecordsAsync(cancellationToken))
            .Where(item => item.OccurredOn >= request.StartDate && item.OccurredOn <= request.EndDate)
            .Where(item => request.AccountId is null || item.AccountId == request.AccountId)
            .Where(item => request.CategoryId is null || item.CategoryId == request.CategoryId)
            .ToList();

        var goals = (await _repository.ListGoalsAsync(cancellationToken)).ToList();
        var budgets = (await _repository.ListBudgetsAsync(cancellationToken)).ToList();
        if (records.Count == 0)
        {
            return new AdvisorRecommendation
            {
                Title = "Gather more activity",
                Summary = "There is not enough activity in the selected range to generate a recommendation.",
                Status = "Fallback",
                Confidence = 0.2m,
                Rationale = "No records matched the supplied filters, so the advisor returned a safe fallback message.",
                Evidence = new[]
                {
                    new EvidenceReference { Type = "Records", Label = "Matching records", Detail = "No records matched the requested range.", Amount = 0 }
                }
            };
        }

        var totalSpend = records.Sum(item => item.Amount.Amount < 0 ? -item.Amount.Amount : item.Amount.Amount);
        var overBudget = budgets.FirstOrDefault(item => totalSpend > item.LimitAmount);
        var behindGoal = goals.FirstOrDefault(item => totalSpend > item.TargetAmount);

        if (overBudget is not null)
        {
            return new AdvisorRecommendation
            {
                Title = "Reduce near-term spending",
                Summary = $"You are already above the {overBudget.Name} envelope for the selected range.",
                Status = "Suggested",
                Confidence = 0.8m,
                Rationale = $"The current period has spent {totalSpend:C}, which exceeds the budget envelope of {overBudget.LimitAmount:C}.",
                Evidence = new[]
                {
                    new EvidenceReference { Type = "Budget", Label = overBudget.Name, Detail = "Budget usage exceeded the configured limit.", Amount = totalSpend },
                    new EvidenceReference { Type = "Records", Label = "Transaction count", Detail = $"{records.Count} records matched the request.", Amount = totalSpend }
                }
            };
        }

        if (behindGoal is not null)
        {
            return new AdvisorRecommendation
            {
                Title = "Protect progress toward your goal",
                Summary = $"Your pace is ahead of the {behindGoal.Name} target and may require attention.",
                Status = "Suggested",
                Confidence = 0.7m,
                Rationale = $"The current spend of {totalSpend:C} is above the configured goal target of {behindGoal.TargetAmount:C}.",
                Evidence = new[]
                {
                    new EvidenceReference { Type = "Goal", Label = behindGoal.Name, Detail = "Goal target has been exceeded.", Amount = totalSpend },
                    new EvidenceReference { Type = "Records", Label = "Transaction count", Detail = $"{records.Count} records matched the request.", Amount = totalSpend }
                }
            };
        }

        return new AdvisorRecommendation
        {
            Title = "Maintain your current approach",
            Summary = "Your current activity is within the available planning guidance, so keep monitoring the period closely.",
            Status = "Suggested",
            Confidence = 0.6m,
            Rationale = $"The analysis found {records.Count} transactions with a total spend of {totalSpend:C} and no clear over-budget or behind-target warning.",
            Evidence = new[]
            {
                new EvidenceReference { Type = "Records", Label = "Transaction count", Detail = $"{records.Count} records matched the request.", Amount = totalSpend }
            }
        };
    }
}

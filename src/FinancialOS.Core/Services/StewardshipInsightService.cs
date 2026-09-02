using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Services;

public sealed class StewardshipInsightService : IInsightService
{
    private readonly IFinancialRepository _repository;

    public StewardshipInsightService(IFinancialRepository repository)
    {
        _repository = repository;
    }

    public async Task<StewardshipInsight> GenerateAsync(InsightRequest request, CancellationToken cancellationToken = default)
    {
        var records = (await _repository.ListRecordsAsync(cancellationToken))
            .Where(item => item.OccurredOn >= request.StartDate && item.OccurredOn <= request.EndDate)
            .Where(item => request.AccountId is null || item.AccountId == request.AccountId)
            .Where(item => request.CategoryId is null || item.CategoryId == request.CategoryId)
            .Where(item => string.Equals(item.Amount.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var goals = (await _repository.ListGoalsAsync(cancellationToken))
            .Where(item => string.Equals(item.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var budgets = (await _repository.ListBudgetsAsync(cancellationToken))
            .Where(item => string.Equals(item.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var categories = (await _repository.ListCategoriesAsync(cancellationToken)).ToDictionary(item => item.Id, item => item.Name);

        if (records.Count == 0)
        {
            return new StewardshipInsight
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Summary = "There is not enough transaction history to generate stewardship insights for this period.",
                AlignmentStatus = "InsufficientData",
                Evidence = new[]
                {
                    new EvidenceReference { Type = "Records", Label = "Transaction count", Detail = "No records matched the requested period.", Amount = 0 }
                }
            };
        }

        var totalSpend = records.Sum(item => NormalizeAmount(item.Amount.Amount));
        var topCategory = records
            .Where(item => item.CategoryId.HasValue)
            .GroupBy(item => item.CategoryId!.Value)
            .Select(group => new { CategoryId = group.Key, Amount = group.Sum(item => NormalizeAmount(item.Amount.Amount)) })
            .OrderByDescending(item => item.Amount)
            .FirstOrDefault();

        var categoryName = topCategory is null ? "No category" : categories.TryGetValue(topCategory.CategoryId, out var knownName) ? knownName : "Unknown category";
        var categoryAmount = topCategory?.Amount ?? 0m;

        var orderedRecords = records.OrderBy(item => item.OccurredOn).ToList();
        var trendDirection = orderedRecords.Count >= 2
            ? orderedRecords.First().Amount.Amount == orderedRecords.Last().Amount.Amount
                ? "Flat"
                : NormalizeAmount(orderedRecords.Last().Amount.Amount) > NormalizeAmount(orderedRecords.First().Amount.Amount)
                    ? "Up"
                    : "Down"
            : "Flat";

        var goalProgress = goals.Select(goal =>
        {
            var actual = records.Where(item => goal.CategoryId is null || item.CategoryId == goal.CategoryId)
                .Where(item => goal.AccountId is null || item.AccountId == goal.AccountId)
                .Sum(item => NormalizeAmount(item.Amount.Amount));
            var progress = goal.TargetAmount > 0 ? Math.Min(100m, (actual / goal.TargetAmount) * 100m) : 0m;
            var remaining = Math.Max(0m, goal.TargetAmount - actual);
            var status = actual >= goal.TargetAmount ? "Achieved" : progress >= 50m ? "OnTrack" : "Behind";
            return new GoalProgressSnapshot
            {
                GoalId = goal.Id,
                Name = goal.Name,
                TargetAmount = goal.TargetAmount,
                ActualAmount = actual,
                RemainingAmount = remaining,
                ProgressPercentage = progress,
                Status = status
            };
        }).ToList();

        var budgetProgress = budgets.Select(budget =>
        {
            var actual = records.Where(item => budget.CategoryId is null || item.CategoryId == budget.CategoryId)
                .Where(item => budget.AccountId is null || item.AccountId == budget.AccountId)
                .Sum(item => NormalizeAmount(item.Amount.Amount));
            var usage = budget.LimitAmount > 0 ? Math.Min(100m, (actual / budget.LimitAmount) * 100m) : 0m;
            var remaining = Math.Max(0m, budget.LimitAmount - actual);
            var status = actual > budget.LimitAmount ? "OverBudget" : "OnTrack";
            return new BudgetProgressSnapshot
            {
                BudgetId = budget.Id,
                Name = budget.Name,
                LimitAmount = budget.LimitAmount,
                ActualAmount = actual,
                RemainingAmount = remaining,
                UsagePercentage = usage,
                Status = status
            };
        }).ToList();

        var alignmentStatus = budgetProgress.Any(item => item.Status == "OverBudget") || goalProgress.Any(item => item.Status == "Behind")
            ? "Behind"
            : "OnTrack";

        var evidence = new List<EvidenceReference>
        {
            new() { Type = "Records", Label = "Recorded transactions", Detail = $"{records.Count} records found in the requested range.", Amount = totalSpend },
            new() { Type = "Category", Label = "Top category", Detail = $"{categoryName} accumulated {categoryAmount:C}.", Amount = categoryAmount }
        };

        if (goalProgress.Any())
        {
            var firstGoal = goalProgress.First();
            evidence.Add(new EvidenceReference { Type = "Goal", Label = firstGoal.Name, Detail = $"Progress is {firstGoal.ProgressPercentage:F0}%.", Amount = firstGoal.ActualAmount });
        }

        if (budgetProgress.Any())
        {
            var firstBudget = budgetProgress.First();
            evidence.Add(new EvidenceReference { Type = "Budget", Label = firstBudget.Name, Detail = $"Usage is {firstBudget.UsagePercentage:F0}%.", Amount = firstBudget.ActualAmount });
        }

        return new StewardshipInsight
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Summary = $"You spent {totalSpend:C} across {records.Count} transactions, concentrated in {categoryName}.",
            TotalSpend = totalSpend,
            RecordCount = records.Count,
            CategoryConcentration = categoryName,
            CategoryConcentrationAmount = categoryAmount,
            AlignmentStatus = alignmentStatus,
            TrendDirection = trendDirection,
            Evidence = evidence,
            GoalProgress = goalProgress,
            BudgetProgress = budgetProgress
        };
    }

    private static decimal NormalizeAmount(decimal amount) => amount < 0 ? -amount : amount;
}

namespace FinancialOS.Core.Models;

public sealed class StewardshipInsight
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal TotalSpend { get; set; }
    public int RecordCount { get; set; }
    public string CategoryConcentration { get; set; } = "Insufficient data";
    public decimal CategoryConcentrationAmount { get; set; }
    public string AlignmentStatus { get; set; } = "InsufficientData";
    public string TrendDirection { get; set; } = "Flat";
    public IReadOnlyList<EvidenceReference> Evidence { get; set; } = Array.Empty<EvidenceReference>();
    public IReadOnlyList<GoalProgressSnapshot> GoalProgress { get; set; } = Array.Empty<GoalProgressSnapshot>();
    public IReadOnlyList<BudgetProgressSnapshot> BudgetProgress { get; set; } = Array.Empty<BudgetProgressSnapshot>();
}

public sealed class GoalProgressSnapshot
{
    public Guid GoalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal ProgressPercentage { get; set; }
    public string Status { get; set; } = "OnTrack";
}

public sealed class BudgetProgressSnapshot
{
    public Guid BudgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal LimitAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal UsagePercentage { get; set; }
    public string Status { get; set; } = "OnTrack";
}

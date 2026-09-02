namespace FinancialOS.Core.Models;

public enum BudgetPeriod
{
    Monthly,
    Quarterly,
    Yearly
}

public sealed class Budget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BudgetPeriod Period { get; set; }
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset EndDate { get; set; } = DateTimeOffset.UtcNow;
    public decimal LimitAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public Guid? CategoryId { get; set; }
    public Guid? AccountId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

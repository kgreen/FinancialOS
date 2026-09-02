namespace FinancialOS.Core.Models;

public sealed class RecommendationRequest
{
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? GoalId { get; set; }
    public Guid? BudgetId { get; set; }
}

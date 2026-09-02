namespace FinancialOS.Core.Models;

public sealed class InsightRequest
{
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Currency { get; set; } = "USD";
}

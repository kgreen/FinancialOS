namespace FinancialOS.Api.QueryModels;

/// <summary>Query string parameters for GET /api/v1/classification-rules.</summary>
public sealed class RuleFilterQuery
{
    public string? RuleType   { get; set; }
    public bool?   IsEnabled  { get; set; }
    public Guid?   CategoryId { get; set; }
    public int?    Page       { get; set; } = PaginationConstants.MinPage;
    public int?    PageSize   { get; set; } = PaginationConstants.DefaultPageSize;
}

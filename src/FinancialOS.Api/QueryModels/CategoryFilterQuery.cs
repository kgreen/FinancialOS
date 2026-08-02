namespace FinancialOS.Api.QueryModels;

/// <summary>Query string parameters for GET /api/v1/categories.</summary>
public sealed class CategoryFilterQuery
{
    public string? NameSearch { get; set; }
    public Guid?   ParentId   { get; set; }
    public int?    Page       { get; set; }
    public int?    PageSize   { get; set; }
}

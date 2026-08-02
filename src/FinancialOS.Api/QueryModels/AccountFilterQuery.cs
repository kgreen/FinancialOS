namespace FinancialOS.Api.QueryModels;

/// <summary>Query string parameters for GET /api/v1/accounts.</summary>
public sealed class AccountFilterQuery
{
    public string? AccountType { get; set; }
    public bool?   IsActive    { get; set; }
    public int?    Page        { get; set; }
    public int?    PageSize    { get; set; }
}

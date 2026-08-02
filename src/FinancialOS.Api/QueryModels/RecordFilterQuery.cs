using FinancialOS.Core.Models;

namespace FinancialOS.Api.QueryModels;

/// <summary>Query string parameters for GET /api/v1/records.</summary>
public sealed class RecordFilterQuery
{
    public DateOnly? StartDate   { get; set; }
    public DateOnly? EndDate     { get; set; }
    public Guid?     AccountId   { get; set; }
    public Guid?     CategoryId  { get; set; }
    public string?   Merchant    { get; set; }
    public decimal?  MinAmount   { get; set; }
    public decimal?  MaxAmount   { get; set; }
    public int?      Page        { get; set; }
    public int?      PageSize    { get; set; }

    public FilterCriteria ToFilterCriteria() => new()
    {
        StartDate      = StartDate,
        EndDate        = EndDate,
        AccountId      = AccountId,
        CategoryId     = CategoryId,
        MerchantSearch = Merchant,
        MinAmount      = MinAmount,
        MaxAmount      = MaxAmount,
    };
}

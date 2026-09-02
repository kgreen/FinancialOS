namespace FinancialOS.Api.QueryModels;

/// <summary>Shared pagination defaults and bounds used by API query models and endpoints.</summary>
public static class PaginationConstants
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 200;
    public const int MinPage = 1;
}

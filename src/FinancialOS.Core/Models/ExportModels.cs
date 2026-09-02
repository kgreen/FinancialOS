namespace FinancialOS.Core.Models;

public enum ExportFormat
{
    Csv        = 0,
    Json       = 1,
    Ynab4      = 2,
    Goodbudget = 3
}

/// <summary>
/// Describes an export request: format, date range, and optional additional filters.
/// The <c>filters</c> JSON property name matches the API contract (contracts/exports.md).
/// </summary>
public sealed record ExportRequest
{
    public required ExportFormat  Format    { get; init; }
    public required DateOnly      StartDate { get; init; }
    public required DateOnly      EndDate   { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("filters")]
    public FilterCriteria? AdditionalFilters { get; init; }

    public IEnumerable<string> Validate()
    {
        if (EndDate < StartDate)
            yield return "EndDate must be on or after StartDate.";
        if (AdditionalFilters is not null)
            foreach (var e in AdditionalFilters.Validate())
                yield return e;
    }

    /// <summary>Merges the date range and additional filters into a single FilterCriteria.</summary>
    public FilterCriteria ToFilterCriteria() => new()
    {
        StartDate       = StartDate,
        EndDate         = EndDate,
        AccountId       = AdditionalFilters?.AccountId,
        CategoryId      = AdditionalFilters?.CategoryId,
        MerchantSearch  = AdditionalFilters?.MerchantSearch,
        MinAmount       = AdditionalFilters?.MinAmount,
        MaxAmount       = AdditionalFilters?.MaxAmount,
        SortBy          = AdditionalFilters?.SortBy,
        SortDescending  = AdditionalFilters?.SortDescending,
    };
}

/// <summary>
/// Transient result of an export operation. Carries the stream and metadata
/// needed to write the HTTP response. Never persisted.
/// </summary>
public sealed record ExportSnapshot
{
    public required Stream         Content     { get; init; }  // readable, caller owns disposal
    public required string         FileName    { get; init; }
    public required string         ContentType { get; init; }
    public required ExportFormat   Format      { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
    public required int            RecordCount { get; init; }
}

namespace FinancialOS.Core.Models;

/// <summary>
/// Immutable filter criteria applied to list queries. All fields are optional;
/// an empty FilterCriteria matches all records.
/// </summary>
public sealed record FilterCriteria
{
    public DateOnly? StartDate      { get; init; }
    public DateOnly? EndDate        { get; init; }
    public Guid?     AccountId      { get; init; }
    public Guid?     CategoryId     { get; init; }
    /// <summary>Partial, case-insensitive match against merchant name.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("merchant")]
    public string?   MerchantSearch { get; init; }
    public decimal?  MinAmount      { get; init; }
    public decimal?  MaxAmount      { get; init; }
    /// <summary>Field to sort by. Supported values: date, amount, description. Defaults to date.</summary>
    public string?   SortBy         { get; init; }
    /// <summary>When true, sorts in descending order. Defaults to true for date, false for other fields.</summary>
    public bool?     SortDescending { get; init; }

    public IEnumerable<string> Validate()
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            yield return "EndDate must be on or after StartDate.";
        if (MinAmount.HasValue && MaxAmount.HasValue && MaxAmount < MinAmount)
            yield return "MaxAmount must be greater than or equal to MinAmount.";
        if (MerchantSearch is { Length: > 200 })
            yield return "MerchantSearch must not exceed 200 characters.";
        if (SortBy is not null && SortBy is not "date" and not "amount" and not "description")
            yield return "SortBy must be one of: date, amount, description.";
    }
}

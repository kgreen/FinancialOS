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
    public string?   MerchantSearch { get; init; }
    public decimal?  MinAmount      { get; init; }
    public decimal?  MaxAmount      { get; init; }

    public IEnumerable<string> Validate()
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            yield return "EndDate must be on or after StartDate.";
        if (MinAmount.HasValue && MaxAmount.HasValue && MaxAmount < MinAmount)
            yield return "MaxAmount must be greater than or equal to MinAmount.";
        if (MerchantSearch is { Length: > 200 })
            yield return "MerchantSearch must not exceed 200 characters.";
    }
}

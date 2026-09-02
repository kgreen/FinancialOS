using FinancialOS.Core.Models;

namespace FinancialOS.Api.Validation;

public sealed record GoalCreateRequest(
    string Name,
    string? Description,
    GoalType Type,
    GoalPeriod Period,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    decimal TargetAmount,
    string? Currency,
    Guid? CategoryId,
    Guid? AccountId);

public sealed record GoalUpdateRequest(
    string? Name,
    string? Description,
    GoalType? Type,
    GoalPeriod? Period,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    decimal? TargetAmount,
    string? Currency,
    Guid? CategoryId,
    Guid? AccountId);

public sealed record BudgetCreateRequest(
    string Name,
    string? Description,
    BudgetPeriod Period,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    decimal LimitAmount,
    string? Currency,
    Guid? CategoryId,
    Guid? AccountId);

public sealed record BudgetUpdateRequest(
    string? Name,
    string? Description,
    BudgetPeriod? Period,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    decimal? LimitAmount,
    string? Currency,
    Guid? CategoryId,
    Guid? AccountId);

public sealed record InsightQuery(
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    Guid? AccountId,
    Guid? CategoryId,
    string? Currency);

public sealed record AdvisorQuery(
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    Guid? AccountId,
    Guid? CategoryId,
    Guid? GoalId,
    Guid? BudgetId);

public static class StewardshipValidation
{
    public static Dictionary<string, string[]> Validate(GoalCreateRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = new[] { "Goal name is required." };
        }

        if (request.TargetAmount <= 0m)
        {
            errors[nameof(request.TargetAmount)] = new[] { "Target amount must be greater than zero." };
        }

        if (request.StartDate >= request.EndDate)
        {
            errors[nameof(request.EndDate)] = new[] { "End date must be later than the start date." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> Validate(GoalUpdateRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Name is not null && string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = new[] { "Goal name cannot be empty." };
        }

        if (request.TargetAmount is not null && request.TargetAmount <= 0m)
        {
            errors[nameof(request.TargetAmount)] = new[] { "Target amount must be greater than zero." };
        }

        if (request.StartDate is not null && request.EndDate is not null && request.StartDate >= request.EndDate)
        {
            errors[nameof(request.EndDate)] = new[] { "End date must be later than the start date." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> Validate(BudgetCreateRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = new[] { "Budget name is required." };
        }

        if (request.LimitAmount <= 0m)
        {
            errors[nameof(request.LimitAmount)] = new[] { "Budget limit amount must be greater than zero." };
        }

        if (request.StartDate >= request.EndDate)
        {
            errors[nameof(request.EndDate)] = new[] { "End date must be later than the start date." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> Validate(BudgetUpdateRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Name is not null && string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = new[] { "Budget name cannot be empty." };
        }

        if (request.LimitAmount is not null && request.LimitAmount <= 0m)
        {
            errors[nameof(request.LimitAmount)] = new[] { "Budget limit amount must be greater than zero." };
        }

        if (request.StartDate is not null && request.EndDate is not null && request.StartDate >= request.EndDate)
        {
            errors[nameof(request.EndDate)] = new[] { "End date must be later than the start date." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> Validate(InsightQuery request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value >= request.EndDate.Value)
        {
            errors[nameof(request.EndDate)] = new[] { "End date must be later than the start date." };
        }

        return errors;
    }

    public static Dictionary<string, string[]> Validate(AdvisorQuery request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value >= request.EndDate.Value)
        {
            errors[nameof(request.EndDate)] = new[] { "End date must be later than the start date." };
        }

        return errors;
    }
}

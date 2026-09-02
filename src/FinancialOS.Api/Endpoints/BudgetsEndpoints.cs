using FinancialOS.Api.Validation;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Api.Endpoints;

public static class BudgetsEndpoints
{
    public static IEndpointRouteBuilder MapBudgetsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/budgets", async (IGoalService service, CancellationToken cancellationToken) =>
        {
            var budgets = await service.ListBudgetsAsync(cancellationToken);
            return Results.Ok(budgets.Select(ToResponse));
        });

        app.MapGet("/api/v1/budgets/{id:guid}", async (Guid id, IGoalService service, CancellationToken cancellationToken) =>
        {
            var budget = await service.GetBudgetAsync(id, cancellationToken);
            return budget is null ? Results.NotFound() : Results.Ok(ToResponse(budget));
        });

        app.MapPost("/api/v1/budgets", async (BudgetCreateRequest request, IGoalService service, CancellationToken cancellationToken) =>
        {
            var errors = StewardshipValidation.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var budget = new Budget
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Period = request.Period,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                LimitAmount = request.LimitAmount,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant(),
                CategoryId = request.CategoryId,
                AccountId = request.AccountId
            };

            var created = await service.CreateBudgetAsync(budget, cancellationToken);
            return Results.Created($"/api/v1/budgets/{created.Id}", ToResponse(created));
        });

        app.MapPut("/api/v1/budgets/{id:guid}", async (Guid id, BudgetUpdateRequest request, IGoalService service, CancellationToken cancellationToken) =>
        {
            var errors = StewardshipValidation.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var existing = await service.GetBudgetAsync(id, cancellationToken);
            if (existing is null)
            {
                return Results.NotFound();
            }

            existing.Name = request.Name?.Trim() ?? existing.Name;
            existing.Description = request.Description?.Trim() ?? existing.Description;
            existing.Period = request.Period ?? existing.Period;
            existing.StartDate = request.StartDate ?? existing.StartDate;
            existing.EndDate = request.EndDate ?? existing.EndDate;
            existing.LimitAmount = request.LimitAmount ?? existing.LimitAmount;
            existing.Currency = string.IsNullOrWhiteSpace(request.Currency) ? existing.Currency : request.Currency.Trim().ToUpperInvariant();
            existing.CategoryId = request.CategoryId ?? existing.CategoryId;
            existing.AccountId = request.AccountId ?? existing.AccountId;

            var updated = await service.UpdateBudgetAsync(existing, cancellationToken);
            return updated is null ? Results.StatusCode(500) : Results.Ok(ToResponse(updated));
        });

        app.MapDelete("/api/v1/budgets/{id:guid}", async (Guid id, IGoalService service, CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteBudgetAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }

    private static BudgetResponse ToResponse(Budget budget) => new(
        budget.Id,
        budget.Name,
        budget.Description,
        budget.Period.ToString(),
        budget.StartDate,
        budget.EndDate,
        budget.LimitAmount,
        budget.Currency,
        budget.CategoryId,
        budget.AccountId,
        budget.CreatedAt,
        budget.UpdatedAt);

    private sealed record BudgetResponse(
        Guid Id,
        string Name,
        string? Description,
        string Period,
        DateTimeOffset StartDate,
        DateTimeOffset EndDate,
        decimal LimitAmount,
        string Currency,
        Guid? CategoryId,
        Guid? AccountId,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}

using FinancialOS.Api.Validation;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Api.Endpoints;

public static class GoalsEndpoints
{
    public static IEndpointRouteBuilder MapGoalsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/goals", async (IGoalService service, CancellationToken cancellationToken) =>
        {
            var goals = await service.ListGoalsAsync(cancellationToken);
            return Results.Ok(goals.Select(ToResponse));
        });

        app.MapGet("/api/v1/goals/{id:guid}", async (Guid id, IGoalService service, CancellationToken cancellationToken) =>
        {
            var goal = await service.GetGoalAsync(id, cancellationToken);
            return goal is null ? Results.NotFound() : Results.Ok(ToResponse(goal));
        });

        app.MapPost("/api/v1/goals", async (GoalCreateRequest request, IGoalService service, CancellationToken cancellationToken) =>
        {
            var errors = StewardshipValidation.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var goal = new Goal
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Type = request.Type,
                Period = request.Period,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                TargetAmount = request.TargetAmount,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant(),
                CategoryId = request.CategoryId,
                AccountId = request.AccountId
            };

            var created = await service.CreateGoalAsync(goal, cancellationToken);
            return Results.Created($"/api/v1/goals/{created.Id}", ToResponse(created));
        });

        app.MapPut("/api/v1/goals/{id:guid}", async (Guid id, GoalUpdateRequest request, IGoalService service, CancellationToken cancellationToken) =>
        {
            var errors = StewardshipValidation.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var existing = await service.GetGoalAsync(id, cancellationToken);
            if (existing is null)
            {
                return Results.NotFound();
            }

            existing.Name = request.Name?.Trim() ?? existing.Name;
            existing.Description = request.Description?.Trim() ?? existing.Description;
            existing.Type = request.Type ?? existing.Type;
            existing.Period = request.Period ?? existing.Period;
            existing.StartDate = request.StartDate ?? existing.StartDate;
            existing.EndDate = request.EndDate ?? existing.EndDate;
            existing.TargetAmount = request.TargetAmount ?? existing.TargetAmount;
            existing.Currency = string.IsNullOrWhiteSpace(request.Currency) ? existing.Currency : request.Currency.Trim().ToUpperInvariant();
            existing.CategoryId = request.CategoryId ?? existing.CategoryId;
            existing.AccountId = request.AccountId ?? existing.AccountId;

            var updated = await service.UpdateGoalAsync(existing, cancellationToken);
            return updated is null ? Results.StatusCode(500) : Results.Ok(ToResponse(updated));
        });

        app.MapDelete("/api/v1/goals/{id:guid}", async (Guid id, IGoalService service, CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteGoalAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }

    private static GoalResponse ToResponse(Goal goal) => new(
        goal.Id,
        goal.Name,
        goal.Description,
        goal.Type.ToString(),
        goal.Period.ToString(),
        goal.StartDate,
        goal.EndDate,
        goal.TargetAmount,
        goal.Currency,
        goal.CategoryId,
        goal.AccountId,
        goal.CreatedAt,
        goal.UpdatedAt);

    private sealed record GoalResponse(
        Guid Id,
        string Name,
        string? Description,
        string Type,
        string Period,
        DateTimeOffset StartDate,
        DateTimeOffset EndDate,
        decimal TargetAmount,
        string Currency,
        Guid? CategoryId,
        Guid? AccountId,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}

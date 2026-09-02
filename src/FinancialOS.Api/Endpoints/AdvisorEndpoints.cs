using FinancialOS.Api.Validation;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Api.Endpoints;

public static class AdvisorEndpoints
{
    public static IEndpointRouteBuilder MapAdvisorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/advisor/recommendations", async (
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            Guid? accountId,
            Guid? categoryId,
            Guid? goalId,
            Guid? budgetId,
            IAdvisorService service,
            CancellationToken cancellationToken) =>
        {
            var query = new AdvisorQuery(startDate, endDate, accountId, categoryId, goalId, budgetId);
            var errors = StewardshipValidation.Validate(query);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var request = new RecommendationRequest
            {
                StartDate = startDate ?? DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTimeOffset.UtcNow,
                AccountId = accountId,
                CategoryId = categoryId,
                GoalId = goalId,
                BudgetId = budgetId
            };

            var recommendation = await service.GenerateAsync(request, cancellationToken);
            return Results.Ok(recommendation);
        });

        return app;
    }
}

using FinancialOS.Api.Validation;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Api.Endpoints;

public static class InsightsEndpoints
{
    public static IEndpointRouteBuilder MapInsightsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/insights", async (
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            Guid? accountId,
            Guid? categoryId,
            string? currency,
            IInsightService service,
            CancellationToken cancellationToken) =>
        {
            var query = new InsightQuery(startDate, endDate, accountId, categoryId, currency);
            var errors = StewardshipValidation.Validate(query);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var request = new InsightRequest
            {
                StartDate = startDate ?? DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTimeOffset.UtcNow,
                AccountId = accountId,
                CategoryId = categoryId,
                Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant()
            };

            var insight = await service.GenerateAsync(request, cancellationToken);
            return Results.Ok(insight);
        });

        return app;
    }
}

using FinancialOS.Api.Validation;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;

namespace FinancialOS.Api.Endpoints;

public static class DuplicateEndpoints
{
    public static IEndpointRouteBuilder MapDuplicateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/duplicates/evaluate", async (
            DuplicateEvaluateRequest request,
            IFinancialRepository repository,
            IDuplicateReviewService reviewService,
            CancellationToken cancellationToken) =>
        {
            var record = await repository.GetRecordAsync(request.RecordId, cancellationToken);
            if (record is null)
            {
                return Results.NotFound();
            }

            var candidate = await reviewService.EvaluateAsync(record, cancellationToken);
            if (candidate is null)
            {
                return Results.NoContent();
            }

            return Results.Ok(ToResponse(candidate));
        });

        app.MapGet("/api/v1/duplicates/candidates", async (
            string? status,
            decimal? minConfidence,
            IDuplicateReviewService reviewService,
            CancellationToken cancellationToken) =>
        {
            DuplicateCandidateStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<DuplicateCandidateStatus>(status, ignoreCase: true, out var parsed))
                {
                    return Results.BadRequest(new { error = "invalid-status" });
                }

                parsedStatus = parsed;
            }

            var candidates = await reviewService.ListAsync(parsedStatus, minConfidence, cancellationToken);
            return Results.Ok(candidates.Select(ToResponse).ToList());
        });

        app.MapPost("/api/v1/duplicates/candidates/{id:guid}/confirm", async (
            Guid id,
            HttpContext httpContext,
            IDuplicateReviewService reviewService,
            CancellationToken cancellationToken) =>
        {
            var actorId = httpContext.Items[ActorIdentityEndpointFilter.ActorContextKey]?.ToString() ?? string.Empty;
            var reviewed = await reviewService.ReviewAsync(
                id,
                DuplicateCandidateStatus.ConfirmedDuplicate,
                actorId,
                cancellationToken);

            return reviewed is null ? Results.NotFound() : Results.Ok(ToResponse(reviewed));
        }).AddEndpointFilter<ActorIdentityEndpointFilter>();

        app.MapPost("/api/v1/duplicates/candidates/{id:guid}/dismiss", async (
            Guid id,
            HttpContext httpContext,
            IDuplicateReviewService reviewService,
            CancellationToken cancellationToken) =>
        {
            var actorId = httpContext.Items[ActorIdentityEndpointFilter.ActorContextKey]?.ToString() ?? string.Empty;
            var reviewed = await reviewService.ReviewAsync(
                id,
                DuplicateCandidateStatus.Dismissed,
                actorId,
                cancellationToken);

            return reviewed is null ? Results.NotFound() : Results.Ok(ToResponse(reviewed));
        }).AddEndpointFilter<ActorIdentityEndpointFilter>();

        return app;
    }

    private static DuplicateCandidateResponse ToResponse(DuplicateCandidate candidate) =>
        new(
            candidate.Id,
            candidate.CandidateGroupKey,
            candidate.RecordId,
            candidate.MatchedRecordId,
            candidate.Confidence,
            candidate.Status.ToString(),
            candidate.ReasonCodes,
            candidate.SignalSnapshotJson,
            candidate.EvaluatedAtUtc,
            candidate.ReviewedByUserId,
            candidate.ReviewedAtUtc);
}

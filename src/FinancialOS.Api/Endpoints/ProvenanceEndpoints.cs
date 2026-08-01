using FinancialOS.Core.Contracts;
using FinancialOS.Shared.Contracts;

namespace FinancialOS.Api.Endpoints;

public static class ProvenanceEndpoints
{
    public static IEndpointRouteBuilder MapProvenanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/records/{id:guid}/provenance", async (
            Guid id,
            IFinancialRepository repository,
            CancellationToken cancellationToken) =>
        {
            var record = await repository.GetRecordAsync(id, cancellationToken);
            if (record is null)
            {
                return Results.NotFound();
            }

            var entries = await repository.ListProvenanceEntriesAsync(id, cancellationToken);
            var response = new ProvenanceTimelineResponse(
                id,
                entries
                    .OrderBy(item => item.StepSequence)
                    .ThenBy(item => item.CreatedAtUtc)
                    .Select(item => new ProvenanceEntryResponse(
                        item.Id,
                        item.FinancialRecordId,
                        item.StepType.ToString(),
                        item.StepSequence,
                        item.Source.ToString().ToLowerInvariant(),
                        item.SourceReference,
                        item.Confidence,
                        item.DecisionSummary,
                        item.ReasonCodes,
                        item.ActorId,
                        item.CorrelationId,
                        item.CreatedAtUtc))
                    .ToList());

            return Results.Ok(response);
        });

        return app;
    }
}

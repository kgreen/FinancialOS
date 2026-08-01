using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
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
            return Results.Ok(new ProvenanceTimelineResponse(
                id,
                entries.Select(ToResponse).ToList()));
        });

        return app;
    }

    private static ProvenanceEntryResponse ToResponse(ProvenanceEntry entry) =>
        new(
            entry.Id,
            entry.FinancialRecordId,
            entry.StepType.ToString(),
            entry.StepSequence,
            entry.Source.ToString().ToLowerInvariant(),
            entry.SourceReference,
            entry.Confidence,
            entry.DecisionSummary,
            entry.ReasonCodes,
            entry.ActorId,
            entry.CorrelationId,
            entry.CreatedAtUtc);
}

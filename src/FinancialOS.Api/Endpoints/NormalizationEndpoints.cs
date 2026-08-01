using FinancialOS.Api.Validation;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Knowledge.Normalization;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;

namespace FinancialOS.Api.Endpoints;

public static class NormalizationEndpoints
{
    public static IEndpointRouteBuilder MapNormalizationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/normalization/aliases", async (
            MerchantAliasCreateRequest request,
            MerchantAliasService aliasService,
            IFinancialRepository repository,
            CancellationToken cancellationToken) =>
        {
            var errors = KnowledgeRequestValidator.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var canonicalMerchant = await repository.GetCanonicalMerchantAsync(request.CanonicalMerchantId, cancellationToken);
            if (canonicalMerchant is null)
            {
                return Results.NotFound($"Canonical merchant '{request.CanonicalMerchantId}' was not found.");
            }

            var alias = new MerchantAliasMap
            {
                CanonicalMerchantId = request.CanonicalMerchantId,
                AliasRawText = request.AliasRawText,
                AliasNormalizedText = request.AliasNormalizedText,
                MatchStrategy = request.MatchStrategy,
                ConfidenceWeight = request.ConfidenceWeight,
                IsActive = request.IsActive
            };

            var created = await aliasService.CreateAliasAsync(alias, cancellationToken);

            return Results.Created($"/api/v1/normalization/aliases/{created.Id}", ToResponse(created));
        });

        app.MapGet("/api/v1/normalization/aliases", async (
            MerchantAliasService aliasService,
            CancellationToken cancellationToken) =>
        {
            var aliases = await aliasService.ListAliasesAsync(cancellationToken);
            return Results.Ok(aliases.Select(ToResponse).ToList());
        });

        app.MapPost("/api/v1/records/{id:guid}/normalize", async (
            Guid id,
            IFinancialRepository repository,
            INormalizationPipelineService pipeline,
            CancellationToken cancellationToken) =>
        {
            var record = await repository.GetRecordAsync(id, cancellationToken);
            if (record is null)
            {
                return Results.NotFound();
            }

            var decision = await pipeline.NormalizeAsync(record, cancellationToken);

            var provenance = await repository.ListProvenanceEntriesAsync(id, cancellationToken);
            var correlationId = provenance.LastOrDefault()?.CorrelationId ?? Guid.Empty;

            return Results.Ok(new NormalizeRecordResponse(
                decision.FinancialRecordId,
                decision.Status.ToString(),
                decision.CanonicalMerchantId,
                decision.CategoryId,
                decision.RuleId,
                decision.Confidence,
                decision.ReasonCodes,
                correlationId));
        });

        return app;
    }

    private static MerchantAliasResponse ToResponse(MerchantAliasMap alias) =>
        new(
            alias.Id,
            alias.CanonicalMerchantId,
            alias.AliasRawText,
            alias.AliasNormalizedText,
            alias.MatchStrategy.ToString(),
            alias.ConfidenceWeight,
            alias.IsActive,
            alias.CreatedAtUtc);
}

using FinancialOS.Api.Validation;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;

namespace FinancialOS.Api.Endpoints;

public static class RulesEndpoints
{
    public static IEndpointRouteBuilder MapRulesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/classification-rules", async (
            ClassificationRuleCreateRequest request,
            IRuleManagementService service,
            CancellationToken cancellationToken) =>
        {
            var errors = KnowledgeRequestValidator.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var rule = new ClassificationRule
            {
                Name = request.Name,
                Status = request.Status,
                Priority = request.Priority,
                Scope = request.Scope,
                ScopeReferenceId = request.ScopeReferenceId,
                ConditionJson = request.ConditionJson,
                TargetMerchantId = request.TargetMerchantId,
                TargetCategoryId = request.TargetCategoryId,
                EffectiveFromUtc = request.EffectiveFromUtc ?? DateTimeOffset.UtcNow,
                EffectiveToUtc = request.EffectiveToUtc
            };

            var created = await service.CreateAsync(rule, cancellationToken);
            var response = ToResponse(created);
            return Results.Created($"/api/v1/classification-rules/{created.Id}", response);
        });

        app.MapMethods("/api/v1/classification-rules/{id:guid}", new[] { "PATCH" }, async (
            Guid id,
            ClassificationRuleUpdateRequest request,
            IFinancialRepository repository,
            IRuleManagementService service,
            CancellationToken cancellationToken) =>
        {
            var existing = await repository.GetClassificationRuleAsync(id, cancellationToken);
            if (existing is null)
            {
                return Results.NotFound();
            }

            if (request.Status.HasValue) existing.Status = request.Status.Value;
            if (request.Priority.HasValue) existing.Priority = request.Priority.Value;
            if (request.ScopeReferenceId.HasValue) existing.ScopeReferenceId = request.ScopeReferenceId.Value;
            if (request.ConditionJson is not null) existing.ConditionJson = request.ConditionJson;
            if (request.TargetMerchantId.HasValue) existing.TargetMerchantId = request.TargetMerchantId.Value;
            if (request.TargetCategoryId.HasValue) existing.TargetCategoryId = request.TargetCategoryId.Value;
            if (request.EffectiveToUtc.HasValue) existing.EffectiveToUtc = request.EffectiveToUtc.Value;

            var updated = await service.UpdateAsync(existing, cancellationToken);
            if (updated is null)
            {
                return Results.StatusCode(500);
            }

            return Results.Ok(ToResponse(updated));
        });

        return app;
    }

    private static ClassificationRuleResponse ToResponse(ClassificationRule rule) =>
        new(
            rule.Id,
            rule.Name,
            rule.Status.ToString(),
            rule.Priority,
            rule.Scope.ToString(),
            rule.ScopeReferenceId,
            rule.ConditionJson,
            rule.TargetMerchantId,
            rule.TargetCategoryId,
            rule.EffectiveFromUtc,
            rule.EffectiveToUtc,
            rule.CreatedAtUtc,
            rule.UpdatedAtUtc);
}

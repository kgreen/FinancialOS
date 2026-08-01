using FinancialOS.Core.Contracts;
using FinancialOS.Core.Knowledge.Provenance;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Knowledge.Normalization;

/// <summary>
/// Runs the deterministic normalization + rule classification pipeline for a single record:
/// 1. Resolve merchant text via alias matching.
/// 2. Evaluate active classification rules (rules may override alias-derived merchant/category).
/// 3. Persist a single normalization decision and append provenance entries.
/// Records below the resolution confidence threshold are marked Unresolved for human review.
/// </summary>
public sealed class NormalizationPipelineService : INormalizationPipelineService
{
    public const decimal ResolutionConfidenceThreshold = 0.6m;

    private readonly IFinancialRepository _repository;
    private readonly MerchantAliasService _aliasService;
    private readonly IRuleEvaluationService _ruleEvaluationService;
    private readonly ProvenanceWriter _provenanceWriter;

    public NormalizationPipelineService(
        IFinancialRepository repository,
        MerchantAliasService aliasService,
        IRuleEvaluationService ruleEvaluationService,
        ProvenanceWriter provenanceWriter)
    {
        _repository = repository;
        _aliasService = aliasService;
        _ruleEvaluationService = ruleEvaluationService;
        _provenanceWriter = provenanceWriter;
    }

    public async Task<NormalizationDecision> NormalizeAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();
        var existingEntries = await _repository.ListProvenanceEntriesAsync(record.Id, cancellationToken);
        long stepSequence = (existingEntries.Count == 0 ? 0 : existingEntries.Max(e => e.StepSequence)) + 1;

        var aliasResult = await _aliasService.ResolveAsync(record.Description, cancellationToken);

        Guid? canonicalMerchantId = aliasResult?.CanonicalMerchantId;
        Guid? categoryId = aliasResult?.DefaultCategoryId;
        decimal confidence = aliasResult?.ConfidenceWeight ?? 0m;
        var reasonCodes = new List<string>();
        if (aliasResult is not null)
        {
            reasonCodes.Add("alias-match");
        }

        var ruleResult = await _ruleEvaluationService.EvaluateAsync(record, cancellationToken);

        Guid? ruleId = null;
        if (ruleResult is not null)
        {
            ruleId = ruleResult.RuleId;
            if (ruleResult.TargetMerchantId.HasValue)
            {
                canonicalMerchantId = ruleResult.TargetMerchantId;
            }
            if (ruleResult.TargetCategoryId.HasValue)
            {
                categoryId = ruleResult.TargetCategoryId;
            }
            confidence = Math.Max(confidence, ruleResult.Confidence);
            reasonCodes.AddRange(ruleResult.ReasonCodes);
            reasonCodes.Add("rule-priority-win");

            await _provenanceWriter.WriteRuleEvaluationAsync(record.Id, ruleResult, stepSequence++, correlationId, cancellationToken);
        }
        else
        {
            await _provenanceWriter.WriteNoRuleMatchAsync(record.Id, stepSequence++, correlationId, cancellationToken);
        }

        var hasOutcome = canonicalMerchantId.HasValue || categoryId.HasValue;
        var status = hasOutcome && confidence >= ResolutionConfidenceThreshold
            ? NormalizationDecisionStatus.Resolved
            : NormalizationDecisionStatus.Unresolved;

        if (status == NormalizationDecisionStatus.Unresolved)
        {
            reasonCodes.Add(hasOutcome ? "insufficient-confidence" : "no-match-found");
        }

        var decision = new NormalizationDecision
        {
            FinancialRecordId = record.Id,
            CanonicalMerchantId = canonicalMerchantId,
            CategoryId = categoryId,
            RuleId = ruleId,
            Confidence = Math.Clamp(confidence, 0m, 1m),
            Status = status,
            ReasonCodes = reasonCodes.Distinct().ToList()
        };

        var created = await _repository.AddNormalizationDecisionAsync(decision, cancellationToken);

        // Link any prior unsuperseded decision for this record to this new one (append-only lineage).
        var priorDecisions = await _repository.ListNormalizationDecisionsAsync(record.Id, cancellationToken);
        var priorLatest = priorDecisions
            .Where(d => d.Id != created.Id && d.SupersededByDecisionId is null)
            .OrderByDescending(d => d.CreatedAtUtc)
            .FirstOrDefault();
        if (priorLatest is not null)
        {
            await _repository.MarkNormalizationDecisionSupersededAsync(priorLatest.Id, created.Id, cancellationToken);
        }

        var sourceReference = ruleId.HasValue
            ? $"rule:{ruleId}"
            : aliasResult is not null ? $"alias:{aliasResult.AliasId}" : "normalization:unresolved";

        await _provenanceWriter.WriteNormalizationAsync(record.Id, created, sourceReference, stepSequence, correlationId, cancellationToken);

        return created;
    }
}

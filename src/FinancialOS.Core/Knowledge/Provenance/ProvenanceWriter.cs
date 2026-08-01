using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Knowledge.Provenance;

/// <summary>
/// Appends immutable provenance entries for pipeline steps.
/// Provenance entries are never modified or deleted after creation.
/// </summary>
public sealed class ProvenanceWriter
{
    private readonly IFinancialRepository _repository;

    public ProvenanceWriter(IFinancialRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProvenanceEntry> WriteRuleEvaluationAsync(
        Guid financialRecordId,
        RuleEvaluationResult result,
        long stepSequence,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var entry = ProvenanceEntry.CreateSystemEntry(
            financialRecordId: financialRecordId,
            stepType: ProvenanceStepType.RuleEvaluation,
            stepSequence: stepSequence,
            sourceReference: $"rule:{result.RuleId}",
            confidence: result.Confidence,
            decisionSummary: $"Rule '{result.RuleName}' selected",
            reasonCodes: result.ReasonCodes,
            correlationId: correlationId);

        return await _repository.AppendProvenanceEntryAsync(entry, cancellationToken);
    }

    public async Task<ProvenanceEntry> WriteNoRuleMatchAsync(
        Guid financialRecordId,
        long stepSequence,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var entry = ProvenanceEntry.CreateSystemEntry(
            financialRecordId: financialRecordId,
            stepType: ProvenanceStepType.RuleEvaluation,
            stepSequence: stepSequence,
            sourceReference: "rule:none",
            confidence: 0m,
            decisionSummary: "No active rule matched the record",
            reasonCodes: new[] { "no-rule-match" },
            correlationId: correlationId);

        return await _repository.AppendProvenanceEntryAsync(entry, cancellationToken);
    }

    public async Task<ProvenanceEntry> WriteNormalizationAsync(
        Guid financialRecordId,
        NormalizationDecision decision,
        string sourceReference,
        long stepSequence,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var entry = ProvenanceEntry.CreateSystemEntry(
            financialRecordId: financialRecordId,
            stepType: ProvenanceStepType.Normalization,
            stepSequence: stepSequence,
            sourceReference: sourceReference,
            confidence: decision.Confidence,
            decisionSummary: $"Normalization {decision.Status}",
            reasonCodes: decision.ReasonCodes,
            correlationId: correlationId);

        return await _repository.AppendProvenanceEntryAsync(entry, cancellationToken);
    }

    public async Task<IReadOnlyList<ProvenanceEntry>> GetTimelineAsync(
        Guid financialRecordId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.ListProvenanceEntriesAsync(financialRecordId, cancellationToken);
    }

    public async Task<ProvenanceEntry> WriteDuplicateDetectionAsync(
        Guid financialRecordId,
        DuplicateCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        var stepSequence = await GetNextStepSequenceAsync(financialRecordId, cancellationToken);
        var correlationId = Guid.NewGuid();
        var entry = ProvenanceEntry.CreateSystemEntry(
            financialRecordId: financialRecordId,
            stepType: ProvenanceStepType.DuplicateDetection,
            stepSequence: stepSequence,
            sourceReference: $"duplicate:{candidate.Id}",
            confidence: candidate.Confidence,
            decisionSummary: $"Duplicate candidate generated for {candidate.MatchedRecordId}",
            reasonCodes: candidate.ReasonCodes,
            correlationId: correlationId);

        return await _repository.AppendProvenanceEntryAsync(entry, cancellationToken);
    }

    public async Task<ProvenanceEntry> WriteDuplicateReviewAsync(
        Guid financialRecordId,
        DuplicateCandidate candidate,
        string actorId,
        CancellationToken cancellationToken = default)
    {
        var stepSequence = await GetNextStepSequenceAsync(financialRecordId, cancellationToken);
        var correlationId = Guid.NewGuid();
        var entry = ProvenanceEntry.CreateUserEntry(
            financialRecordId: financialRecordId,
            stepType: ProvenanceStepType.DuplicateReview,
            stepSequence: stepSequence,
            sourceReference: $"duplicate:{candidate.Id}:{candidate.Status}",
            confidence: candidate.Confidence,
            decisionSummary: $"Duplicate candidate marked as {candidate.Status}",
            reasonCodes: new[] { "human-review", candidate.Status.ToString() },
            actorId: actorId,
            correlationId: correlationId);

        return await _repository.AppendProvenanceEntryAsync(entry, cancellationToken);
    }

    private async Task<long> GetNextStepSequenceAsync(Guid financialRecordId, CancellationToken cancellationToken)
    {
        var timeline = await _repository.ListProvenanceEntriesAsync(financialRecordId, cancellationToken);
        return (timeline.Count == 0 ? 0 : timeline.Max(item => item.StepSequence)) + 1;
    }
}

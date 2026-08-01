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

    public async Task<ProvenanceEntry> WriteImportHydrationAsync(
        Guid financialRecordId,
        Guid evidenceId,
        Guid importJobId,
        ParserType parserType,
        int? rowIndex,
        string? externalReferenceId,
        CancellationToken cancellationToken = default)
    {
        return await AppendWithRetryAsync(financialRecordId, stepSequence =>
        {
            var sourceReference = $"import-job:{importJobId}";
            var detail = externalReferenceId is null
                ? $"row:{rowIndex?.ToString() ?? "n/a"}"
                : $"fitid:{externalReferenceId}";
            return ProvenanceEntry.CreateSystemEntry(
                financialRecordId: financialRecordId,
                stepType: ProvenanceStepType.ImportHydration,
                stepSequence: stepSequence,
                sourceReference: sourceReference,
                confidence: null,
                decisionSummary: $"Hydrated from evidence {evidenceId} using parser {parserType} ({detail})",
                reasonCodes: new[] { "import-hydration" },
                correlationId: importJobId);
        }, cancellationToken);
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
        return await AppendWithRetryAsync(financialRecordId, stepSequence =>
        {
            var correlationId = Guid.NewGuid();
            return ProvenanceEntry.CreateSystemEntry(
                financialRecordId: financialRecordId,
                stepType: ProvenanceStepType.DuplicateDetection,
                stepSequence: stepSequence,
                sourceReference: $"duplicate:{candidate.Id}",
                confidence: candidate.Confidence,
                decisionSummary: $"Duplicate candidate generated for {candidate.MatchedRecordId}",
                reasonCodes: candidate.ReasonCodes,
                correlationId: correlationId);
        }, cancellationToken);
    }

    public async Task<ProvenanceEntry> WriteDuplicateReviewAsync(
        Guid financialRecordId,
        DuplicateCandidate candidate,
        string actorId,
        CancellationToken cancellationToken = default)
    {
        return await AppendWithRetryAsync(financialRecordId, stepSequence =>
        {
            var correlationId = Guid.NewGuid();
            return ProvenanceEntry.CreateUserEntry(
                financialRecordId: financialRecordId,
                stepType: ProvenanceStepType.DuplicateReview,
                stepSequence: stepSequence,
                sourceReference: $"duplicate:{candidate.Id}:{candidate.Status}",
                confidence: candidate.Confidence,
                decisionSummary: $"Duplicate candidate marked as {candidate.Status}",
                reasonCodes: new[] { "human-review", ToReasonCode(candidate.Status) },
                actorId: actorId,
                correlationId: correlationId);
        }, cancellationToken);
    }

    private async Task<ProvenanceEntry> AppendWithRetryAsync(
        Guid financialRecordId,
        Func<long, ProvenanceEntry> createEntry,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var stepSequence = await GetNextStepSequenceAsync(financialRecordId, cancellationToken);
            var entry = createEntry(stepSequence);
            try
            {
                return await _repository.AppendProvenanceEntryAsync(entry, cancellationToken);
            }
            catch (Exception ex) when (attempt < 2 && IsStepSequenceConflict(ex))
            {
            }
        }

        throw new InvalidOperationException("Unable to append provenance entry after retrying step sequence allocation.");
    }

    private async Task<long> GetNextStepSequenceAsync(Guid financialRecordId, CancellationToken cancellationToken)
    {
        var maxStepSequence = await _repository.GetMaxProvenanceStepSequenceAsync(financialRecordId, cancellationToken);
        return (maxStepSequence ?? 0) + 1;
    }

    private static bool IsStepSequenceConflict(Exception exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_ProvenanceEntry_Record_StepSequence_Unique", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToReasonCode(DuplicateCandidateStatus status) =>
        status switch
        {
            DuplicateCandidateStatus.ConfirmedDuplicate => "confirmed-duplicate",
            DuplicateCandidateStatus.Dismissed => "dismissed",
            _ => status.ToString().ToLowerInvariant()
        };
}

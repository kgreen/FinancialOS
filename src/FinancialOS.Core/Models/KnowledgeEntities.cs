namespace FinancialOS.Core.Models;

public enum RuleStatus
{
    Active,
    Inactive
}

public enum RuleScope
{
    Global,
    Account,
    Institution
}

public enum AliasMatchStrategy
{
    Exact,
    Contains,
    TokenSet
}

public enum NormalizationDecisionStatus
{
    Resolved,
    Unresolved,
    Overridden
}

public enum DuplicateCandidateStatus
{
    PendingReview,
    ConfirmedDuplicate,
    Dismissed
}

public enum ProvenanceStepType
{
    ImportHydration,
    Normalization,
    RuleEvaluation,
    DuplicateDetection,
    DuplicateReview,
    ManualOverride
}

public enum ProvenanceSourceType
{
    System,
    User
}

public sealed class ClassificationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public RuleStatus Status { get; set; } = RuleStatus.Active;
    public int Priority { get; set; }
    public RuleScope Scope { get; set; } = RuleScope.Global;
    public Guid? ScopeReferenceId { get; set; }
    public string ConditionJson { get; set; } = "{}";
    public Guid? TargetMerchantId { get; set; }
    public Guid? TargetCategoryId { get; set; }
    public DateTimeOffset EffectiveFromUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EffectiveToUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CanonicalMerchant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string NormalizedKey { get; set; } = string.Empty;
    public Guid? DefaultCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class MerchantAliasMap
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CanonicalMerchantId { get; set; }
    public string AliasRawText { get; set; } = string.Empty;
    public string AliasNormalizedText { get; set; } = string.Empty;
    public AliasMatchStrategy MatchStrategy { get; set; } = AliasMatchStrategy.Exact;
    public decimal ConfidenceWeight { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class NormalizationDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FinancialRecordId { get; set; }
    public Guid? CanonicalMerchantId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? RuleId { get; set; }
    public decimal Confidence { get; set; }
    public NormalizationDecisionStatus Status { get; set; }
    public List<string> ReasonCodes { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Guid? SupersededByDecisionId { get; set; }
}

public sealed class DuplicateCandidate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CandidateGroupKey { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public Guid MatchedRecordId { get; set; }
    public decimal Confidence { get; set; }
    public DuplicateCandidateStatus Status { get; set; } = DuplicateCandidateStatus.PendingReview;
    public List<string> ReasonCodes { get; set; } = new();
    public string SignalSnapshotJson { get; set; } = "{}";
    public DateTimeOffset EvaluatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
}

public sealed class ProvenanceEntry
{
    private ProvenanceEntry()
    {
        ReasonCodes = new List<string>();
    }

    public Guid Id { get; private set; }
    public Guid FinancialRecordId { get; private set; }
    public ProvenanceStepType StepType { get; private set; }
    public long StepSequence { get; private set; }
    public ProvenanceSourceType Source { get; private set; }
    public string SourceReference { get; private set; } = string.Empty;
    public decimal? Confidence { get; private set; }
    public string DecisionSummary { get; private set; } = string.Empty;
    public List<string> ReasonCodes { get; private set; }
    public string? ActorId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ProvenanceEntry(
        Guid financialRecordId,
        ProvenanceStepType stepType,
        long stepSequence,
        ProvenanceSourceType source,
        string sourceReference,
        decimal? confidence,
        string decisionSummary,
        IEnumerable<string> reasonCodes,
        string? actorId,
        Guid correlationId,
        DateTimeOffset createdAtUtc)
    {
        if (source == ProvenanceSourceType.User && string.IsNullOrWhiteSpace(actorId))
        {
            throw new ArgumentException("ActorId is required for user provenance entries.", nameof(actorId));
        }

        if (confidence is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        }

        Id = Guid.NewGuid();
        FinancialRecordId = financialRecordId;
        StepType = stepType;
        StepSequence = stepSequence;
        Source = source;
        SourceReference = sourceReference;
        Confidence = confidence;
        DecisionSummary = decisionSummary;
        ReasonCodes = reasonCodes.Where(code => !string.IsNullOrWhiteSpace(code)).Distinct().ToList();
        ActorId = actorId;
        CorrelationId = correlationId;
        CreatedAtUtc = createdAtUtc;
    }

    public static ProvenanceEntry CreateSystemEntry(
        Guid financialRecordId,
        ProvenanceStepType stepType,
        long stepSequence,
        string sourceReference,
        decimal? confidence,
        string decisionSummary,
        IEnumerable<string> reasonCodes,
        Guid correlationId,
        DateTimeOffset? createdAtUtc = null)
    {
        return new ProvenanceEntry(
            financialRecordId,
            stepType,
            stepSequence,
            ProvenanceSourceType.System,
            sourceReference,
            confidence,
            decisionSummary,
            reasonCodes,
            actorId: null,
            correlationId,
            createdAtUtc ?? DateTimeOffset.UtcNow);
    }

    public static ProvenanceEntry CreateUserEntry(
        Guid financialRecordId,
        ProvenanceStepType stepType,
        long stepSequence,
        string sourceReference,
        decimal? confidence,
        string decisionSummary,
        IEnumerable<string> reasonCodes,
        string actorId,
        Guid correlationId,
        DateTimeOffset? createdAtUtc = null)
    {
        return new ProvenanceEntry(
            financialRecordId,
            stepType,
            stepSequence,
            ProvenanceSourceType.User,
            sourceReference,
            confidence,
            decisionSummary,
            reasonCodes,
            actorId,
            correlationId,
            createdAtUtc ?? DateTimeOffset.UtcNow);
    }
}

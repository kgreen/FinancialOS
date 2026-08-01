using System.ComponentModel.DataAnnotations;
using FinancialOS.Core.Models;

namespace FinancialOS.Shared.Contracts;

public sealed record ClassificationRuleResponse(
    Guid Id,
    string Name,
    string Status,
    int Priority,
    string Scope,
    Guid? ScopeReferenceId,
    string ConditionJson,
    Guid? TargetMerchantId,
    Guid? TargetCategoryId,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ClassificationRuleCreateRequest(
    [property: Required(AllowEmptyStrings = false)] string Name,
    RuleStatus Status,
    int Priority,
    RuleScope Scope,
    Guid? ScopeReferenceId,
    [property: Required(AllowEmptyStrings = false)] string ConditionJson,
    Guid? TargetMerchantId,
    Guid? TargetCategoryId,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc);

public sealed record ClassificationRuleUpdateRequest(
    RuleStatus? Status,
    int? Priority,
    Guid? ScopeReferenceId,
    string? ConditionJson,
    Guid? TargetMerchantId,
    Guid? TargetCategoryId,
    DateTimeOffset? EffectiveToUtc);

public sealed record CanonicalMerchantResponse(
    Guid Id,
    string DisplayName,
    string NormalizedKey,
    Guid? DefaultCategoryId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record CanonicalMerchantCreateRequest(
    [property: Required(AllowEmptyStrings = false)] string DisplayName,
    [property: Required(AllowEmptyStrings = false)] string NormalizedKey,
    Guid? DefaultCategoryId,
    bool IsActive);

public sealed record MerchantAliasCreateRequest(
    [property: Required(AllowEmptyStrings = false)] string AliasRawText,
    [property: Required(AllowEmptyStrings = false)] string AliasNormalizedText,
    Guid CanonicalMerchantId,
    AliasMatchStrategy MatchStrategy,
    decimal ConfidenceWeight,
    bool IsActive);

public sealed record MerchantAliasResponse(
    Guid Id,
    Guid CanonicalMerchantId,
    string AliasRawText,
    string AliasNormalizedText,
    string MatchStrategy,
    decimal ConfidenceWeight,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record NormalizeRecordResponse(
    Guid RecordId,
    string Status,
    Guid? CanonicalMerchantId,
    Guid? CategoryId,
    Guid? RuleId,
    decimal Confidence,
    IReadOnlyList<string> ReasonCodes,
    Guid ProvenanceCorrelationId);

public sealed record NormalizationDecisionResponse(
    Guid Id,
    Guid FinancialRecordId,
    Guid? CanonicalMerchantId,
    Guid? CategoryId,
    Guid? RuleId,
    decimal Confidence,
    string Status,
    IReadOnlyList<string> ReasonCodes,
    DateTimeOffset CreatedAtUtc,
    Guid? SupersededByDecisionId);

public sealed record DuplicateCandidateResponse(
    Guid Id,
    string CandidateGroupKey,
    Guid RecordId,
    Guid MatchedRecordId,
    decimal Confidence,
    string Status,
    IReadOnlyList<string> ReasonCodes,
    string SignalSnapshotJson,
    DateTimeOffset EvaluatedAtUtc,
    string? ReviewedByUserId,
    DateTimeOffset? ReviewedAtUtc);

public sealed record ProvenanceEntryResponse(
    Guid Id,
    Guid FinancialRecordId,
    string StepType,
    long StepSequence,
    string Source,
    string SourceReference,
    decimal? Confidence,
    string DecisionSummary,
    IReadOnlyList<string> ReasonCodes,
    string? ActorId,
    Guid CorrelationId,
    DateTimeOffset CreatedAtUtc);
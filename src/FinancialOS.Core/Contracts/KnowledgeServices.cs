using FinancialOS.Core.Models;

namespace FinancialOS.Core.Contracts;

public interface IRuleEvaluationService
{
    Task<RuleEvaluationResult?> EvaluateAsync(FinancialRecord record, CancellationToken cancellationToken = default);
}

public interface IRuleManagementService
{
    Task<ClassificationRule> CreateAsync(ClassificationRule rule, CancellationToken cancellationToken = default);
    Task<ClassificationRule?> UpdateAsync(ClassificationRule rule, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassificationRule>> ListAsync(CancellationToken cancellationToken = default);
}

public interface INormalizationPipelineService
{
    Task<NormalizationDecision> NormalizeAsync(FinancialRecord record, CancellationToken cancellationToken = default);
}

public interface IDuplicateReviewService
{
    Task<DuplicateCandidate?> EvaluateAsync(FinancialRecord record, CancellationToken cancellationToken = default);
    Task<DuplicateCandidate?> ReviewAsync(Guid id, DuplicateCandidateStatus status, string reviewedByUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DuplicateCandidate>> ListAsync(DuplicateCandidateStatus? status, decimal? minConfidence, CancellationToken cancellationToken = default);
}

public sealed record RuleEvaluationResult(
    Guid RuleId,
    string RuleName,
    decimal Confidence,
    IReadOnlyList<string> ReasonCodes,
    Guid? TargetMerchantId,
    Guid? TargetCategoryId);
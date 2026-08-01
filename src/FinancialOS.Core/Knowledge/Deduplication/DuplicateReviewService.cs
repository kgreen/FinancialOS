using FinancialOS.Core.Contracts;
using FinancialOS.Core.Knowledge.Provenance;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Knowledge.Deduplication;

public sealed class DuplicateReviewService : IDuplicateReviewService
{
    private readonly IFinancialRepository _repository;
    private readonly DuplicateScoringService _scoringService;
    private readonly ProvenanceWriter _provenanceWriter;

    public DuplicateReviewService(
        IFinancialRepository repository,
        DuplicateScoringService scoringService,
        ProvenanceWriter provenanceWriter)
    {
        _repository = repository;
        _scoringService = scoringService;
        _provenanceWriter = provenanceWriter;
    }

    public async Task<DuplicateCandidate?> EvaluateAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        var candidates = await _repository.ListPotentialDuplicateRecordsAsync(
            recordId: record.Id,
            accountId: record.AccountId,
            occurredOn: record.OccurredOn,
            cancellationToken);
        if (candidates.Count == 0)
        {
            return null;
        }

        var best = candidates
            .Select(item => new { Record = item, Score = _scoringService.Score(record, item) })
            .OrderByDescending(item => item.Score.Confidence)
            .ThenBy(item => item.Record.OccurredOn)
            .ThenBy(item => item.Record.Id)
            .First();

        var candidate = new DuplicateCandidate
        {
            CandidateGroupKey = BuildGroupKey(record.Id, best.Record.Id),
            RecordId = record.Id,
            MatchedRecordId = best.Record.Id,
            Confidence = best.Score.Confidence,
            Status = DuplicateCandidateStatus.PendingReview,
            ReasonCodes = best.Score.ReasonCodes.ToList(),
            SignalSnapshotJson = best.Score.SignalSnapshotJson,
            EvaluatedAtUtc = DateTimeOffset.UtcNow
        };

        var created = await _repository.AddDuplicateCandidateAsync(candidate, cancellationToken);
        await _provenanceWriter.WriteDuplicateDetectionAsync(
            financialRecordId: record.Id,
            candidate: created,
            cancellationToken);

        return created;
    }

    public async Task<DuplicateCandidate?> ReviewAsync(
        Guid id,
        DuplicateCandidateStatus status,
        string reviewedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reviewedByUserId))
        {
            throw new ArgumentException("ReviewedByUserId is required.", nameof(reviewedByUserId));
        }

        if (status is DuplicateCandidateStatus.PendingReview)
        {
            throw new ArgumentException("Review status must be ConfirmedDuplicate or Dismissed.", nameof(status));
        }

        var candidate = await _repository.GetDuplicateCandidateAsync(id, cancellationToken);
        if (candidate is null)
        {
            return null;
        }

        candidate.Status = status;
        candidate.ReviewedByUserId = reviewedByUserId;
        candidate.ReviewedAtUtc = DateTimeOffset.UtcNow;

        var updated = await _repository.UpdateDuplicateCandidateAsync(candidate, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        await _provenanceWriter.WriteDuplicateReviewAsync(
            financialRecordId: updated.RecordId,
            candidate: updated,
            actorId: reviewedByUserId,
            cancellationToken);

        return updated;
    }

    public async Task<IReadOnlyList<DuplicateCandidate>> ListAsync(
        DuplicateCandidateStatus? status,
        decimal? minConfidence,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListDuplicateCandidatesAsync(status, minConfidence, cancellationToken);
        return items
            .OrderByDescending(item => item.EvaluatedAtUtc)
            .ThenBy(item => item.Id)
            .ToList();
    }

    private static string BuildGroupKey(Guid first, Guid second)
    {
        var left = first.CompareTo(second) <= 0 ? first : second;
        var right = first.CompareTo(second) <= 0 ? second : first;
        return $"{left:N}:{right:N}";
    }

    private static List<FinancialRecord> ScopeCandidates(FinancialRecord record, List<FinancialRecord> candidates)
    {
        var scoped = candidates.AsEnumerable();

        if (record.AccountId.HasValue)
        {
            scoped = scoped.Where(item => item.AccountId == record.AccountId);
        }

        var windowStart = record.OccurredOn.AddDays(-30);
        var windowEnd = record.OccurredOn.AddDays(30);
        scoped = scoped.Where(item => item.OccurredOn >= windowStart && item.OccurredOn <= windowEnd);

        return scoped.ToList();
    }
}

namespace FinancialOS.Shared.Contracts;

public sealed record EvidenceUploadResponse(Guid Id, string Status, string SourceType, string FileName, string StoragePath, string Sha256Hash, long SizeBytes);

public sealed record EvidenceResponse(Guid Id, string SourceType, string FileName, string StoragePath, string Sha256Hash, long SizeBytes, DateTimeOffset UploadedAt);

public sealed record RecordListResponse(IReadOnlyList<RecordResponse> Items, int Page, int PageSize);

public sealed record RecordResponse(
    Guid Id,
    Guid? EvidenceId,
    Guid? AccountId,
    Guid? MerchantId,
    Guid? CategoryId,
    string Description,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredOn,
    string Status,
    decimal? ConfidenceValue,
    string? RuleName);

public sealed record RecordClassificationRequest(Guid? CategoryId, Guid? MerchantId, decimal Confidence, string? RuleName, string? Notes);

public sealed record ReferenceItemResponse(Guid Id, string Name, string Type);

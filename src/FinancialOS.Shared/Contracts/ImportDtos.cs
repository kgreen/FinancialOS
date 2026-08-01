// Import DTOs — populated in Phase 2
namespace FinancialOS.Shared.Contracts;

public sealed record CreateInstitutionProfileRequest(
    string Name,
    Dictionary<string, string> ColumnMappings,
    string AmountLayout,
    string? DebitColumnName,
    string? CreditColumnName,
    string? DateFormatPattern
);

public sealed record UpdateInstitutionProfileRequest(
    string Name,
    Dictionary<string, string> ColumnMappings,
    string AmountLayout,
    string? DebitColumnName,
    string? CreditColumnName,
    string? DateFormatPattern
);

public sealed record InstitutionProfileResponse(
    Guid Id,
    string Name,
    Dictionary<string, string> ColumnMappings,
    string AmountLayout,
    string? DebitColumnName,
    string? CreditColumnName,
    string? DateFormatPattern,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record ImportJobResponse(
    Guid Id,
    Guid EvidenceId,
    Guid? InstitutionProfileId,
    string ParserType,
    string Status,
    int TotalRows,
    int ParsedCount,
    int FailedRowCount,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<FailedRowDto> FailedRows
);

public sealed record FailedRowDto(int RowIndex, string Reason);

public sealed record ImportRecordSummary(
    Guid Id,
    string Date,
    decimal Amount,
    string Currency,
    string Description,
    string ClassificationStatus,
    decimal? ClassificationConfidence,
    string? ClassificationReasonCode
);

public sealed record EvidenceImportResponse(
    Guid EvidenceId,
    Guid ImportJobId,
    string Status,
    string ParserType,
    int ParsedTransactionCount,
    int FailedRowCount,
    IReadOnlyList<ImportRecordSummary> Records
);

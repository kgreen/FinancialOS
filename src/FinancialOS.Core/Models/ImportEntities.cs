namespace FinancialOS.Core.Models;

public enum AmountLayout
{
    SingleSigned,
    SplitDebitCredit
}

public enum ImportJobStatus
{
    Pending,
    Processing,
    Completed,
    PartialSuccess,
    Failed
}

public enum ParserType
{
    CsvConfigured,
    CsvAutoDetected,
    Ofx
}

public enum ClassificationStatus
{
    Pending,
    Classified
}

/// <summary>Transient DTO — never persisted. Produced by ITransactionParser.</summary>
public sealed record ParsedTransaction(
    DateOnly TransactionDate,
    decimal Amount,
    string Description,
    decimal? Balance,
    string? ExternalReferenceId,
    int RowIndex,
    string RawRow
);

/// <summary>Serialized as JSON within ImportJob.FailedRows.</summary>
public sealed record FailedRowEntry(int RowIndex, string Reason);

/// <summary>Describes how to parse a CSV from a specific bank.</summary>
public sealed class InstitutionProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> ColumnMappings { get; set; } = new();
    public AmountLayout AmountLayout { get; set; } = AmountLayout.SingleSigned;
    public string? DebitColumnName { get; set; }
    public string? CreditColumnName { get; set; }
    public string? DateFormatPattern { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; set; } = false;
}

/// <summary>Tracks one parsing execution.</summary>
public sealed class ImportJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EvidenceId { get; set; }
    public Guid? InstitutionProfileId { get; set; }
    public ParserType ParserType { get; set; }
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Pending;
    public int TotalRows { get; set; }
    public int ParsedCount { get; set; }
    public int FailedRowCount { get; set; }
    public List<FailedRowEntry> FailedRows { get; set; } = new();
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

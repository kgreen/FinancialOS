namespace FinancialOS.Core.Models;

public enum EvidenceSourceType
{
    Csv,
    Ofx,
    Pdf,
    Image
}

public enum RecordStatus
{
    Pending,
    Normalized,
    Reviewed,
    Ignored
}

public sealed record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency = "USD") => new(0m, currency);
}

public sealed record Confidence(decimal Score)
{
    public decimal Score { get; init; } = Math.Clamp(Score, 0m, 1m);
}

public sealed record Provenance(string Source, string RuleName, string? AlgorithmVersion = null);

public sealed class FinancialEvidence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public EvidenceSourceType SourceType { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public string? SourceMetadata { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class FinancialRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? EvidenceId { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? MerchantId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public Money Amount { get; set; } = Money.Zero();
    public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.UtcNow;
    public RecordStatus Status { get; set; } = RecordStatus.Pending;
    public Confidence? ClassificationConfidence { get; set; }
    public Provenance? Provenance { get; set; }
}

public sealed class FinancialAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
}

public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}

public sealed class Merchant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}

public sealed class Rule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string MatchExpression { get; set; } = string.Empty;
}

public sealed class PlanningScenario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? TargetAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Guid> RelatedRecordIds { get; set; } = new();
}

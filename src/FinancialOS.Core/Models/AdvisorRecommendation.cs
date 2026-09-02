namespace FinancialOS.Core.Models;

public sealed class AdvisorRecommendation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Status { get; set; } = "Suggested";
    public decimal Confidence { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public IReadOnlyList<EvidenceReference> Evidence { get; set; } = Array.Empty<EvidenceReference>();
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}

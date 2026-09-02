namespace FinancialOS.Core.Models;

public sealed class EvidenceReference
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}

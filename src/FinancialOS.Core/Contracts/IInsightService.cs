using FinancialOS.Core.Models;

namespace FinancialOS.Core.Contracts;

public interface IInsightService
{
    Task<StewardshipInsight> GenerateAsync(InsightRequest request, CancellationToken cancellationToken = default);
}

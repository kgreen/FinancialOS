using FinancialOS.Core.Models;

namespace FinancialOS.Core.Contracts;

public interface IAdvisorService
{
    Task<AdvisorRecommendation> GenerateAsync(RecommendationRequest request, CancellationToken cancellationToken = default);
}

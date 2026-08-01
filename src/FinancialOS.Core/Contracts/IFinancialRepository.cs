using FinancialOS.Core.Models;

namespace FinancialOS.Core.Contracts;

public interface IFinancialRepository
{
    Task<FinancialEvidence> AddEvidenceAsync(FinancialEvidence evidence, CancellationToken cancellationToken = default);
    Task<FinancialEvidence?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialEvidence>> ListEvidenceAsync(CancellationToken cancellationToken = default);

    Task<FinancialRecord> AddRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default);
    Task<FinancialRecord?> GetRecordAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialRecord>> ListRecordsAsync(CancellationToken cancellationToken = default);
    Task<FinancialRecord?> UpdateRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialAccount>> ListAccountsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Merchant>> ListMerchantsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rule>> ListRulesAsync(CancellationToken cancellationToken = default);

    Task<PlanningScenario> AddPlanningScenarioAsync(PlanningScenario scenario, CancellationToken cancellationToken = default);
    Task<PlanningScenario?> GetPlanningScenarioAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanningScenario>> ListPlanningScenariosAsync(CancellationToken cancellationToken = default);
}

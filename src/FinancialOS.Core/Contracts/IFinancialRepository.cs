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

    Task<ClassificationRule> AddClassificationRuleAsync(ClassificationRule rule, CancellationToken cancellationToken = default);
    Task<ClassificationRule?> GetClassificationRuleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassificationRule>> ListClassificationRulesAsync(CancellationToken cancellationToken = default);
    Task<ClassificationRule?> UpdateClassificationRuleAsync(ClassificationRule rule, CancellationToken cancellationToken = default);

    Task<CanonicalMerchant> AddCanonicalMerchantAsync(CanonicalMerchant merchant, CancellationToken cancellationToken = default);
    Task<CanonicalMerchant?> GetCanonicalMerchantAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CanonicalMerchant>> ListCanonicalMerchantsAsync(CancellationToken cancellationToken = default);

    Task<MerchantAliasMap> AddMerchantAliasAsync(MerchantAliasMap alias, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MerchantAliasMap>> ListMerchantAliasesAsync(CancellationToken cancellationToken = default);

    Task<NormalizationDecision> AddNormalizationDecisionAsync(NormalizationDecision decision, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NormalizationDecision>> ListNormalizationDecisionsAsync(Guid financialRecordId, CancellationToken cancellationToken = default);
    Task<NormalizationDecision?> MarkNormalizationDecisionSupersededAsync(Guid decisionId, Guid supersededByDecisionId, CancellationToken cancellationToken = default);

    Task<DuplicateCandidate> AddDuplicateCandidateAsync(DuplicateCandidate candidate, CancellationToken cancellationToken = default);
    Task<DuplicateCandidate?> GetDuplicateCandidateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DuplicateCandidate>> ListDuplicateCandidatesAsync(CancellationToken cancellationToken = default);
    Task<DuplicateCandidate?> UpdateDuplicateCandidateAsync(DuplicateCandidate candidate, CancellationToken cancellationToken = default);

    Task<long?> GetMaxProvenanceStepSequenceAsync(Guid financialRecordId, CancellationToken cancellationToken = default);
    Task<ProvenanceEntry> AppendProvenanceEntryAsync(ProvenanceEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProvenanceEntry>> ListProvenanceEntriesAsync(Guid financialRecordId, CancellationToken cancellationToken = default);
}

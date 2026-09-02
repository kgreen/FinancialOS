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
    Task<IReadOnlyList<FinancialRecord>> ListPotentialDuplicateRecordsAsync(
        Guid recordId,
        Guid? accountId,
        DateTimeOffset occurredOn,
        CancellationToken cancellationToken = default);
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
    Task<IReadOnlyList<DuplicateCandidate>> ListDuplicateCandidatesAsync(
        DuplicateCandidateStatus? status,
        decimal? minConfidence,
        CancellationToken cancellationToken = default);
    Task<DuplicateCandidate?> UpdateDuplicateCandidateAsync(DuplicateCandidate candidate, CancellationToken cancellationToken = default);

    Task<long?> GetMaxProvenanceStepSequenceAsync(Guid financialRecordId, CancellationToken cancellationToken = default);
    Task<ProvenanceEntry> AppendProvenanceEntryAsync(ProvenanceEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProvenanceEntry>> ListProvenanceEntriesAsync(Guid financialRecordId, CancellationToken cancellationToken = default);

    // spec 003 — evidence duplicate detection
    Task<FinancialEvidence?> GetEvidenceBySha256Async(string sha256, CancellationToken cancellationToken = default);

    // spec 003 — ImportJob CRUD
    Task<ImportJob> AddImportJobAsync(ImportJob job, CancellationToken cancellationToken = default);
    Task<ImportJob?> GetImportJobAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ImportJob?> UpdateImportJobAsync(ImportJob job, CancellationToken cancellationToken = default);
    Task<ImportJob?> GetImportJobByEvidenceIdAsync(Guid evidenceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportJob>> ListImportJobsAsync(CancellationToken cancellationToken = default);

    // spec 003 — InstitutionProfile CRUD
    Task<InstitutionProfile> AddInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default);
    Task<InstitutionProfile?> GetInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstitutionProfile>> ListInstitutionProfilesAsync(CancellationToken cancellationToken = default);
    Task<InstitutionProfile?> UpdateInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default);
    /// <summary>Soft-deletes the profile. Returns false if referenced by any ImportJob.</summary>
    Task<bool> DeleteInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default);

    // spec 003 — duplicate detection & job record listing
    Task<bool> ExternalReferenceIdExistsAsync(string externalReferenceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialRecord>> ListRecordsByImportJobAsync(Guid importJobId, CancellationToken cancellationToken = default);

    // spec 004 — paged + filtered queries
    Task<PagedResult<FinancialRecord>> GetRecordsPagedAsync(FilterCriteria filter, int page, int pageSize, CancellationToken cancellationToken = default);
    IAsyncEnumerable<FinancialRecord> StreamRecordsAsync(FilterCriteria filter, CancellationToken cancellationToken = default);
    Task<PagedResult<FinancialAccount>> GetAccountsPagedAsync(string? accountType, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<Category>> GetCategoriesPagedAsync(string? nameSearch, Guid? parentId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ClassificationRule>> GetRulesPagedAsync(string? ruleType, bool? isEnabled, Guid? categoryId, int page, int pageSize, CancellationToken cancellationToken = default);
}

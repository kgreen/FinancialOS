using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialOS.Data;

public sealed class EfFinancialRepository : IFinancialRepository
{
    private readonly FinancialOsDbContext _dbContext;

    public EfFinancialRepository(FinancialOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FinancialEvidence> AddEvidenceAsync(FinancialEvidence evidence, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Evidence
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Sha256Hash == evidence.Sha256Hash, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        evidence.Id = evidence.Id == Guid.Empty ? Guid.NewGuid() : evidence.Id;
        _dbContext.Evidence.Add(evidence);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return evidence;
    }

    public async Task<FinancialEvidence?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Evidence.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialEvidence>> ListEvidenceAsync(CancellationToken cancellationToken = default)
    {
        var evidence = await _dbContext.Evidence.AsNoTracking().ToListAsync(cancellationToken);
        return evidence.OrderByDescending(item => item.UploadedAt).ToList();
    }

    public async Task<FinancialRecord> AddRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
        _dbContext.Records.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<FinancialRecord?> GetRecordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Records.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialRecord>> ListRecordsAsync(CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Records.AsNoTracking().ToListAsync(cancellationToken);
        return records.OrderByDescending(item => item.OccurredOn).ToList();
    }

    public async Task<IReadOnlyList<FinancialRecord>> ListPotentialDuplicateRecordsAsync(
        Guid recordId,
        Guid? accountId,
        DateTimeOffset occurredOn,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Records.AsNoTracking()
            .Where(item => item.Id != recordId);

        if (accountId.HasValue)
        {
            var accountValue = accountId.Value;
            query = query.Where(item => item.AccountId == accountValue);
        }

        var records = await query.ToListAsync(cancellationToken);
        return records.OrderBy(item => item.OccurredOn).ThenBy(item => item.Id).ToList();
    }

    public async Task<FinancialRecord?> UpdateRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        _dbContext.Records.Update(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<IReadOnlyList<FinancialAccount>> ListAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Merchant>> ListMerchantsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Merchants.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Rule>> ListRulesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Rules.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<PlanningScenario> AddPlanningScenarioAsync(PlanningScenario scenario, CancellationToken cancellationToken = default)
    {
        scenario.Id = scenario.Id == Guid.Empty ? Guid.NewGuid() : scenario.Id;
        _dbContext.PlanningScenarios.Add(scenario);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return scenario;
    }

    public async Task<PlanningScenario?> GetPlanningScenarioAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlanningScenarios.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PlanningScenario>> ListPlanningScenariosAsync(CancellationToken cancellationToken = default)
    {
        var scenarios = await _dbContext.PlanningScenarios.AsNoTracking().ToListAsync(cancellationToken);
        return scenarios.OrderByDescending(item => item.CreatedAt).ToList();
    }

    public async Task<ClassificationRule> AddClassificationRuleAsync(ClassificationRule rule, CancellationToken cancellationToken = default)
    {
        rule.Id = rule.Id == Guid.Empty ? Guid.NewGuid() : rule.Id;
        rule.CreatedAtUtc = rule.CreatedAtUtc == default ? DateTimeOffset.UtcNow : rule.CreatedAtUtc;
        rule.UpdatedAtUtc = rule.UpdatedAtUtc == default ? rule.CreatedAtUtc : rule.UpdatedAtUtc;
        _dbContext.ClassificationRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<ClassificationRule?> GetClassificationRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClassificationRules.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassificationRule>> ListClassificationRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _dbContext.ClassificationRules.AsNoTracking().ToListAsync(cancellationToken);
        return rules.OrderByDescending(item => item.Priority).ThenBy(item => item.CreatedAtUtc).ToList();
    }

    public async Task<ClassificationRule?> UpdateClassificationRuleAsync(ClassificationRule rule, CancellationToken cancellationToken = default)
    {
        rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        _dbContext.ClassificationRules.Update(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<CanonicalMerchant> AddCanonicalMerchantAsync(CanonicalMerchant merchant, CancellationToken cancellationToken = default)
    {
        merchant.Id = merchant.Id == Guid.Empty ? Guid.NewGuid() : merchant.Id;
        merchant.CreatedAtUtc = merchant.CreatedAtUtc == default ? DateTimeOffset.UtcNow : merchant.CreatedAtUtc;
        _dbContext.CanonicalMerchants.Add(merchant);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return merchant;
    }

    public async Task<CanonicalMerchant?> GetCanonicalMerchantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CanonicalMerchants.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CanonicalMerchant>> ListCanonicalMerchantsAsync(CancellationToken cancellationToken = default)
    {
        var merchants = await _dbContext.CanonicalMerchants.AsNoTracking().ToListAsync(cancellationToken);
        return merchants.OrderBy(item => item.DisplayName).ToList();
    }

    public async Task<MerchantAliasMap> AddMerchantAliasAsync(MerchantAliasMap alias, CancellationToken cancellationToken = default)
    {
        alias.Id = alias.Id == Guid.Empty ? Guid.NewGuid() : alias.Id;
        alias.CreatedAtUtc = alias.CreatedAtUtc == default ? DateTimeOffset.UtcNow : alias.CreatedAtUtc;
        _dbContext.MerchantAliases.Add(alias);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return alias;
    }

    public async Task<IReadOnlyList<MerchantAliasMap>> ListMerchantAliasesAsync(CancellationToken cancellationToken = default)
    {
        var aliases = await _dbContext.MerchantAliases.AsNoTracking().ToListAsync(cancellationToken);
        return aliases.OrderBy(item => item.AliasNormalizedText).ToList();
    }

    public async Task<NormalizationDecision> AddNormalizationDecisionAsync(NormalizationDecision decision, CancellationToken cancellationToken = default)
    {
        decision.Id = decision.Id == Guid.Empty ? Guid.NewGuid() : decision.Id;
        decision.CreatedAtUtc = decision.CreatedAtUtc == default ? DateTimeOffset.UtcNow : decision.CreatedAtUtc;
        _dbContext.NormalizationDecisions.Add(decision);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return decision;
    }

    public async Task<IReadOnlyList<NormalizationDecision>> ListNormalizationDecisionsAsync(Guid financialRecordId, CancellationToken cancellationToken = default)
    {
        var decisions = await _dbContext.NormalizationDecisions.AsNoTracking()
            .Where(item => item.FinancialRecordId == financialRecordId)
            .ToListAsync(cancellationToken);
        return decisions.OrderBy(item => item.CreatedAtUtc).ToList();
    }

    public async Task<NormalizationDecision?> MarkNormalizationDecisionSupersededAsync(Guid decisionId, Guid supersededByDecisionId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.NormalizationDecisions.FirstOrDefaultAsync(item => item.Id == decisionId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.SupersededByDecisionId = supersededByDecisionId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<DuplicateCandidate> AddDuplicateCandidateAsync(DuplicateCandidate candidate, CancellationToken cancellationToken = default)
    {
        candidate.Id = candidate.Id == Guid.Empty ? Guid.NewGuid() : candidate.Id;
        candidate.EvaluatedAtUtc = candidate.EvaluatedAtUtc == default ? DateTimeOffset.UtcNow : candidate.EvaluatedAtUtc;
        _dbContext.DuplicateCandidates.Add(candidate);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return candidate;
    }

    public async Task<DuplicateCandidate?> GetDuplicateCandidateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DuplicateCandidates.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DuplicateCandidate>> ListDuplicateCandidatesAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await _dbContext.DuplicateCandidates.AsNoTracking().ToListAsync(cancellationToken);
        return candidates.OrderByDescending(item => item.EvaluatedAtUtc).ToList();
    }

    public async Task<IReadOnlyList<DuplicateCandidate>> ListDuplicateCandidatesAsync(
        DuplicateCandidateStatus? status,
        decimal? minConfidence,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DuplicateCandidates.AsNoTracking().AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(item => item.Status == status.Value);
        }

        if (minConfidence.HasValue)
        {
            query = query.Where(item => item.Confidence >= minConfidence.Value);
        }

        var candidates = await query.ToListAsync(cancellationToken);
        return candidates.OrderByDescending(item => item.EvaluatedAtUtc).ThenBy(item => item.Id).ToList();
    }

    public async Task<DuplicateCandidate?> UpdateDuplicateCandidateAsync(DuplicateCandidate candidate, CancellationToken cancellationToken = default)
    {
        _dbContext.DuplicateCandidates.Update(candidate);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return candidate;
    }

    public async Task<long?> GetMaxProvenanceStepSequenceAsync(Guid financialRecordId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProvenanceEntries.AsNoTracking()
            .Where(item => item.FinancialRecordId == financialRecordId)
            .MaxAsync(item => (long?)item.StepSequence, cancellationToken);
    }

    public async Task<ProvenanceEntry> AppendProvenanceEntryAsync(ProvenanceEntry entry, CancellationToken cancellationToken = default)
    {
        _dbContext.ProvenanceEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<IReadOnlyList<ProvenanceEntry>> ListProvenanceEntriesAsync(Guid financialRecordId, CancellationToken cancellationToken = default)
    {
        var entries = await _dbContext.ProvenanceEntries.AsNoTracking()
            .Where(item => item.FinancialRecordId == financialRecordId)
            .ToListAsync(cancellationToken);
        return entries.OrderBy(item => item.StepSequence).ThenBy(item => item.CreatedAtUtc).ToList();
    }

    // spec 003 — evidence duplicate detection
    public async Task<FinancialEvidence?> GetEvidenceBySha256Async(string sha256, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Evidence.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Sha256Hash == sha256, cancellationToken);
    }

    // spec 003 — ImportJob CRUD
    public async Task<ImportJob> AddImportJobAsync(ImportJob job, CancellationToken cancellationToken = default)
    {
        job.Id = job.Id == Guid.Empty ? Guid.NewGuid() : job.Id;
        _dbContext.ImportJobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<ImportJob?> GetImportJobAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ImportJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<ImportJob?> UpdateImportJobAsync(ImportJob job, CancellationToken cancellationToken = default)
    {
        _dbContext.ImportJobs.Update(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<ImportJob?> GetImportJobByEvidenceIdAsync(Guid evidenceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ImportJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.EvidenceId == evidenceId, cancellationToken);
    }

    // spec 003 — InstitutionProfile CRUD
    public async Task<InstitutionProfile> AddInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default)
    {
        profile.Id = profile.Id == Guid.Empty ? Guid.NewGuid() : profile.Id;
        _dbContext.InstitutionProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<InstitutionProfile?> GetInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InstitutionProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<InstitutionProfile>> ListInstitutionProfilesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.InstitutionProfiles.AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<InstitutionProfile?> UpdateInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default)
    {
        _dbContext.InstitutionProfiles.Update(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<bool> DeleteInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.InstitutionProfiles
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (profile is null) return false;

        var hasJobs = await _dbContext.ImportJobs
            .AnyAsync(j => j.InstitutionProfileId == id, cancellationToken);
        if (hasJobs) return false;

        profile.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // spec 003 — duplicate detection & job record listing
    public async Task<bool> ExternalReferenceIdExistsAsync(string externalReferenceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Records.AsNoTracking()
            .AnyAsync(r => r.ExternalReferenceId == externalReferenceId, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialRecord>> ListRecordsByImportJobAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Records.AsNoTracking()
            .Where(r => r.ImportJobId == importJobId)
            .ToListAsync(cancellationToken);
    }

    // spec 004 — paged + filtered queries
    public async Task<PagedResult<FinancialRecord>> GetRecordsPagedAsync(
        FilterCriteria filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Records.AsNoTracking().AsQueryable();

        if (filter.AccountId.HasValue)
            query = query.Where(r => r.AccountId == filter.AccountId.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(r => r.CategoryId == filter.CategoryId.Value);

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(r => r.OccurredOn >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(r => r.OccurredOn <= end);
        }

        if (filter.MinAmount.HasValue)
            query = query.Where(r => r.Amount.Amount >= filter.MinAmount.Value);

        if (filter.MaxAmount.HasValue)
            query = query.Where(r => r.Amount.Amount <= filter.MaxAmount.Value);

        if (!string.IsNullOrWhiteSpace(filter.MerchantSearch))
        {
            var search = filter.MerchantSearch.ToLower();
            query = query.Where(r => r.Description.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .ToListAsync(cancellationToken);

        // Sort the results by OccurredOn on the client side (SQLite limitation with DateTimeOffset)
        items = items
            .OrderByDescending(r => r.OccurredOn)
            .ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<FinancialRecord>(items, page, pageSize, totalCount);
    }

    public IAsyncEnumerable<FinancialRecord> StreamRecordsAsync(
        FilterCriteria filter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Records.AsNoTracking().AsQueryable();

        if (filter.AccountId.HasValue)
            query = query.Where(r => r.AccountId == filter.AccountId.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(r => r.CategoryId == filter.CategoryId.Value);

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(r => r.OccurredOn >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(r => r.OccurredOn <= end);
        }

        if (filter.MinAmount.HasValue)
            query = query.Where(r => r.Amount.Amount >= filter.MinAmount.Value);

        if (filter.MaxAmount.HasValue)
            query = query.Where(r => r.Amount.Amount <= filter.MaxAmount.Value);

        if (!string.IsNullOrWhiteSpace(filter.MerchantSearch))
        {
            var search = filter.MerchantSearch.ToLower();
            query = query.Where(r => r.Description.ToLower().Contains(search));
        }

        return query
            .OrderByDescending(r => r.OccurredOn)
            .ThenBy(r => r.Id)
            .AsAsyncEnumerable();
    }

    public Task<PagedResult<FinancialAccount>> GetAccountsPagedAsync(
        string? accountType,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // FinancialAccount has no AccountType or IsActive fields in the current model;
        // filters are accepted but ignored to fulfil the interface contract.
        return GetAccountsPagedAsync(search: null, currency: null, page, pageSize, cancellationToken);
    }

    private async Task<PagedResult<FinancialAccount>> GetAccountsPagedAsync(
        string? search,
        string? currency,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Accounts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(currency))
            query = query.Where(a => a.Currency == currency);

        query = query.OrderBy(a => a.Name).ThenBy(a => a.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FinancialAccount>(items, page, pageSize, totalCount);
    }

    public async Task<PagedResult<Category>> GetCategoriesPagedAsync(
        string? nameSearch,
        Guid? parentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(nameSearch))
        {
            var s = nameSearch.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(s));
        }
        // parentId filter: Category has no ParentId in current model — accepted, ignored.

        query = query.OrderBy(c => c.Name).ThenBy(c => c.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Category>(items, page, pageSize, totalCount);
    }

    public async Task<PagedResult<ClassificationRule>> GetRulesPagedAsync(
        string? ruleType,
        bool? isEnabled,
        Guid? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ClassificationRules.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(ruleType))
        {
            var rt = ruleType.ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(rt));
        }

        if (isEnabled.HasValue)
        {
            // ClassificationRule.Status: Active = enabled, Inactive = disabled
            var targetStatus = isEnabled.Value ? RuleStatus.Active : RuleStatus.Inactive;
            query = query.Where(r => r.Status == targetStatus);
        }

        if (categoryId.HasValue)
            query = query.Where(r => r.TargetCategoryId == categoryId.Value);

        // Priority descending, then Id ascending (per contracts/rules.md)
        query = query.OrderByDescending(r => r.Priority).ThenBy(r => r.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ClassificationRule>(items, page, pageSize, totalCount);
    }
}

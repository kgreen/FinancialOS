using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Data;

public sealed class InMemoryFinancialRepository : IFinancialRepository
{
    private readonly Dictionary<Guid, FinancialEvidence> _evidence = new();
    private readonly Dictionary<Guid, FinancialRecord> _records = new();
    private readonly List<FinancialAccount> _accounts = new();
    private readonly List<Category> _categories = new();
    private readonly List<Merchant> _merchants = new();
    private readonly List<Rule> _rules = new();
    private readonly Dictionary<Guid, PlanningScenario> _planningScenarios = new();
    private readonly Dictionary<Guid, ClassificationRule> _classificationRules = new();
    private readonly Dictionary<Guid, CanonicalMerchant> _canonicalMerchants = new();
    private readonly Dictionary<Guid, MerchantAliasMap> _merchantAliases = new();
    private readonly Dictionary<Guid, NormalizationDecision> _normalizationDecisions = new();
    private readonly Dictionary<Guid, DuplicateCandidate> _duplicateCandidates = new();
    private readonly List<ProvenanceEntry> _provenanceEntries = new();

    public InMemoryFinancialRepository()
    {
        _accounts.Add(new FinancialAccount { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Primary Checking", Currency = "USD" });
        _categories.Add(new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Housing" });
        _merchants.Add(new Merchant { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Contoso Market" });
        _rules.Add(new Rule { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Default Merchant Rule", MatchExpression = "merchant contains market" });
    }

    public Task<FinancialEvidence> AddEvidenceAsync(FinancialEvidence evidence, CancellationToken cancellationToken = default)
    {
        var existing = _evidence.Values.FirstOrDefault(item => item.Sha256Hash == evidence.Sha256Hash);
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        evidence.Id = evidence.Id == Guid.Empty ? Guid.NewGuid() : evidence.Id;
        _evidence[evidence.Id] = evidence;
        _evidenceBySha256[evidence.Sha256Hash] = evidence;
        return Task.FromResult(evidence);
    }

    public Task<FinancialEvidence?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _evidence.TryGetValue(id, out var evidence);
        return Task.FromResult(evidence);
    }

    public Task<IReadOnlyList<FinancialEvidence>> ListEvidenceAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FinancialEvidence>>(_evidence.Values.OrderByDescending(item => item.UploadedAt).ToList());
    }

    public Task<FinancialRecord> AddRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
        _records[record.Id] = record;
        return Task.FromResult(record);
    }

    public Task<FinancialRecord?> GetRecordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _records.TryGetValue(id, out var record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<FinancialRecord>> ListRecordsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FinancialRecord>>(_records.Values.OrderByDescending(item => item.OccurredOn).ToList());
    }

    public Task<IReadOnlyList<FinancialRecord>> ListPotentialDuplicateRecordsAsync(
        Guid recordId,
        Guid? accountId,
        DateTimeOffset occurredOn,
        CancellationToken cancellationToken = default)
    {
        var query = _records.Values
            .Where(item => item.Id != recordId);

        if (accountId.HasValue)
        {
            var accountValue = accountId.Value;
            query = query.Where(item => item.AccountId == accountValue);
        }

        return Task.FromResult<IReadOnlyList<FinancialRecord>>(query
            .OrderBy(item => item.OccurredOn)
            .ThenBy(item => item.Id)
            .ToList());
    }

    public Task<FinancialRecord?> UpdateRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
        _records[record.Id] = record;
        return Task.FromResult<FinancialRecord?>(record);
    }

    public Task<IReadOnlyList<FinancialAccount>> ListAccountsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FinancialAccount>>(_accounts.ToList());
    }

    public Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Category>>(_categories.ToList());
    }

    public Task<IReadOnlyList<Merchant>> ListMerchantsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Merchant>>(_merchants.ToList());
    }

    public Task<IReadOnlyList<Rule>> ListRulesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Rule>>(_rules.ToList());
    }

    public Task<PlanningScenario> AddPlanningScenarioAsync(PlanningScenario scenario, CancellationToken cancellationToken = default)
    {
        scenario.Id = scenario.Id == Guid.Empty ? Guid.NewGuid() : scenario.Id;
        scenario.CreatedAt = scenario.CreatedAt == default ? DateTimeOffset.UtcNow : scenario.CreatedAt;
        _planningScenarios[scenario.Id] = scenario;
        return Task.FromResult(scenario);
    }

    public Task<PlanningScenario?> GetPlanningScenarioAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _planningScenarios.TryGetValue(id, out var scenario);
        return Task.FromResult(scenario);
    }

    public Task<IReadOnlyList<PlanningScenario>> ListPlanningScenariosAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PlanningScenario>>(_planningScenarios.Values.OrderByDescending(item => item.CreatedAt).ToList());
    }

    public Task<ClassificationRule> AddClassificationRuleAsync(ClassificationRule rule, CancellationToken cancellationToken = default)
    {
        rule.Id = rule.Id == Guid.Empty ? Guid.NewGuid() : rule.Id;
        _classificationRules[rule.Id] = rule;
        return Task.FromResult(rule);
    }

    public Task<ClassificationRule?> GetClassificationRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _classificationRules.TryGetValue(id, out var rule);
        return Task.FromResult(rule);
    }

    public Task<IReadOnlyList<ClassificationRule>> ListClassificationRulesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ClassificationRule>>(_classificationRules.Values
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .ToList());
    }

    public Task<ClassificationRule?> UpdateClassificationRuleAsync(ClassificationRule rule, CancellationToken cancellationToken = default)
    {
        rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        _classificationRules[rule.Id] = rule;
        return Task.FromResult<ClassificationRule?>(rule);
    }

    public Task<CanonicalMerchant> AddCanonicalMerchantAsync(CanonicalMerchant merchant, CancellationToken cancellationToken = default)
    {
        merchant.Id = merchant.Id == Guid.Empty ? Guid.NewGuid() : merchant.Id;
        _canonicalMerchants[merchant.Id] = merchant;
        return Task.FromResult(merchant);
    }

    public Task<CanonicalMerchant?> GetCanonicalMerchantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _canonicalMerchants.TryGetValue(id, out var merchant);
        return Task.FromResult(merchant);
    }

    public Task<IReadOnlyList<CanonicalMerchant>> ListCanonicalMerchantsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<CanonicalMerchant>>(_canonicalMerchants.Values.OrderBy(item => item.DisplayName).ToList());
    }

    public Task<MerchantAliasMap> AddMerchantAliasAsync(MerchantAliasMap alias, CancellationToken cancellationToken = default)
    {
        alias.Id = alias.Id == Guid.Empty ? Guid.NewGuid() : alias.Id;
        _merchantAliases[alias.Id] = alias;
        return Task.FromResult(alias);
    }

    public Task<IReadOnlyList<MerchantAliasMap>> ListMerchantAliasesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<MerchantAliasMap>>(_merchantAliases.Values.ToList());
    }

    public Task<NormalizationDecision> AddNormalizationDecisionAsync(NormalizationDecision decision, CancellationToken cancellationToken = default)
    {
        decision.Id = decision.Id == Guid.Empty ? Guid.NewGuid() : decision.Id;
        _normalizationDecisions[decision.Id] = decision;
        return Task.FromResult(decision);
    }

    public Task<IReadOnlyList<NormalizationDecision>> ListNormalizationDecisionsAsync(Guid financialRecordId, CancellationToken cancellationToken = default)
    {
        var decisions = _normalizationDecisions.Values
            .Where(item => item.FinancialRecordId == financialRecordId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToList();
        return Task.FromResult<IReadOnlyList<NormalizationDecision>>(decisions);
    }

    public Task<NormalizationDecision?> MarkNormalizationDecisionSupersededAsync(Guid decisionId, Guid supersededByDecisionId, CancellationToken cancellationToken = default)
    {
        if (!_normalizationDecisions.TryGetValue(decisionId, out var existing))
        {
            return Task.FromResult<NormalizationDecision?>(null);
        }

        existing.SupersededByDecisionId = supersededByDecisionId;
        return Task.FromResult<NormalizationDecision?>(existing);
    }

    public Task<DuplicateCandidate> AddDuplicateCandidateAsync(DuplicateCandidate candidate, CancellationToken cancellationToken = default)
    {
        candidate.Id = candidate.Id == Guid.Empty ? Guid.NewGuid() : candidate.Id;
        _duplicateCandidates[candidate.Id] = candidate;
        return Task.FromResult(candidate);
    }

    public Task<DuplicateCandidate?> GetDuplicateCandidateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _duplicateCandidates.TryGetValue(id, out var candidate);
        return Task.FromResult(candidate);
    }

    public Task<IReadOnlyList<DuplicateCandidate>> ListDuplicateCandidatesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DuplicateCandidate>>(_duplicateCandidates.Values.OrderByDescending(item => item.EvaluatedAtUtc).ToList());
    }

    public Task<IReadOnlyList<DuplicateCandidate>> ListDuplicateCandidatesAsync(
        DuplicateCandidateStatus? status,
        decimal? minConfidence,
        CancellationToken cancellationToken = default)
    {
        var query = _duplicateCandidates.Values.AsEnumerable();
        if (status.HasValue)
        {
            query = query.Where(item => item.Status == status.Value);
        }

        if (minConfidence.HasValue)
        {
            query = query.Where(item => item.Confidence >= minConfidence.Value);
        }

        return Task.FromResult<IReadOnlyList<DuplicateCandidate>>(query.OrderByDescending(item => item.EvaluatedAtUtc).ThenBy(item => item.Id).ToList());
    }

    public Task<DuplicateCandidate?> UpdateDuplicateCandidateAsync(DuplicateCandidate candidate, CancellationToken cancellationToken = default)
    {
        _duplicateCandidates[candidate.Id] = candidate;
        return Task.FromResult<DuplicateCandidate?>(candidate);
    }

    public Task<long?> GetMaxProvenanceStepSequenceAsync(Guid financialRecordId, CancellationToken cancellationToken = default)
    {
        var maxStepSequence = _provenanceEntries
            .Where(item => item.FinancialRecordId == financialRecordId)
            .Select(item => (long?)item.StepSequence)
            .Max();
        return Task.FromResult(maxStepSequence);
    }

    public Task<ProvenanceEntry> AppendProvenanceEntryAsync(ProvenanceEntry entry, CancellationToken cancellationToken = default)
    {
        _provenanceEntries.Add(entry);
        return Task.FromResult(entry);
    }

    public Task<IReadOnlyList<ProvenanceEntry>> ListProvenanceEntriesAsync(Guid financialRecordId, CancellationToken cancellationToken = default)
    {
        var entries = _provenanceEntries
            .Where(item => item.FinancialRecordId == financialRecordId)
            .OrderBy(item => item.StepSequence)
            .ThenBy(item => item.CreatedAtUtc)
            .ToList();
        return Task.FromResult<IReadOnlyList<ProvenanceEntry>>(entries);
    }

    // spec 003 in-memory implementations
    private readonly Dictionary<string, FinancialEvidence> _evidenceBySha256 = new();
    private readonly Dictionary<Guid, ImportJob> _importJobs = new();
    private readonly Dictionary<Guid, InstitutionProfile> _institutionProfiles = new();

    public Task<FinancialEvidence?> GetEvidenceBySha256Async(string sha256, CancellationToken cancellationToken = default)
    {
        _evidenceBySha256.TryGetValue(sha256, out var e);
        return Task.FromResult(e);
    }

    public Task<ImportJob> AddImportJobAsync(ImportJob job, CancellationToken cancellationToken = default)
    {
        if (job.Id == Guid.Empty) job.Id = Guid.NewGuid();
        _importJobs[job.Id] = job;
        return Task.FromResult(job);
    }

    public Task<ImportJob?> GetImportJobAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _importJobs.TryGetValue(id, out var j);
        return Task.FromResult(j);
    }

    public Task<ImportJob?> UpdateImportJobAsync(ImportJob job, CancellationToken cancellationToken = default)
    {
        _importJobs[job.Id] = job;
        return Task.FromResult<ImportJob?>(job);
    }

    public Task<ImportJob?> GetImportJobByEvidenceIdAsync(Guid evidenceId, CancellationToken cancellationToken = default)
    {
        var job = _importJobs.Values.FirstOrDefault(j => j.EvidenceId == evidenceId);
        return Task.FromResult(job);
    }

    public Task<IReadOnlyList<ImportJob>> ListImportJobsAsync(CancellationToken cancellationToken = default)
    {
        var jobs = _importJobs.Values.OrderByDescending(j => j.CreatedAt).ToList();
        return Task.FromResult<IReadOnlyList<ImportJob>>(jobs);
    }

    public Task<InstitutionProfile> AddInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile.Id == Guid.Empty) profile.Id = Guid.NewGuid();
        _institutionProfiles[profile.Id] = profile;
        return Task.FromResult(profile);
    }

    public Task<InstitutionProfile?> GetInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _institutionProfiles.TryGetValue(id, out var p);
        return Task.FromResult(p?.IsDeleted == true ? null : p);
    }

    public Task<IReadOnlyList<InstitutionProfile>> ListInstitutionProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = _institutionProfiles.Values.Where(p => !p.IsDeleted).OrderBy(p => p.Name).ToList();
        return Task.FromResult<IReadOnlyList<InstitutionProfile>>(profiles);
    }

    public Task<InstitutionProfile?> UpdateInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default)
    {
        _institutionProfiles[profile.Id] = profile;
        return Task.FromResult<InstitutionProfile?>(profile);
    }

    public Task<bool> DeleteInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_institutionProfiles.TryGetValue(id, out var profile)) return Task.FromResult(false);
        var hasJobs = _importJobs.Values.Any(j => j.InstitutionProfileId == id);
        if (hasJobs) return Task.FromResult(false);
        profile.IsDeleted = true;
        return Task.FromResult(true);
    }

    public Task<bool> ExternalReferenceIdExistsAsync(string externalReferenceId, CancellationToken cancellationToken = default)
    {
        var exists = _records.Values.Any(r => r.ExternalReferenceId == externalReferenceId);
        return Task.FromResult(exists);
    }

    public Task<IReadOnlyList<FinancialRecord>> ListRecordsByImportJobAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        var records = _records.Values.Where(r => r.ImportJobId == importJobId).ToList();
        return Task.FromResult<IReadOnlyList<FinancialRecord>>(records);
    }

    // spec 004 — paged + filtered queries (in-memory implementations for testing)
    public Task<PagedResult<FinancialRecord>> GetRecordsPagedAsync(
        FilterCriteria filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _records.Values.AsEnumerable();
        if (filter.AccountId.HasValue)   query = query.Where(r => r.AccountId == filter.AccountId);
        if (filter.CategoryId.HasValue)  query = query.Where(r => r.CategoryId == filter.CategoryId);
        if (filter.StartDate.HasValue)   query = query.Where(r => DateOnly.FromDateTime(r.OccurredOn.Date) >= filter.StartDate);
        if (filter.EndDate.HasValue)     query = query.Where(r => DateOnly.FromDateTime(r.OccurredOn.Date) <= filter.EndDate);
        if (filter.MinAmount.HasValue)   query = query.Where(r => r.Amount.Amount >= filter.MinAmount);
        if (filter.MaxAmount.HasValue)   query = query.Where(r => r.Amount.Amount <= filter.MaxAmount);
        if (!string.IsNullOrWhiteSpace(filter.MerchantSearch))
            query = query.Where(r => r.Description.Contains(filter.MerchantSearch, StringComparison.OrdinalIgnoreCase));

        var descending = filter.SortDescending ?? (filter.SortBy is null or "date");
        var ordered = filter.SortBy switch
        {
            "amount"      => descending ? query.OrderByDescending(r => r.Amount.Amount).ThenBy(r => r.Id).ToList()
                                        : query.OrderBy(r => r.Amount.Amount).ThenBy(r => r.Id).ToList(),
            "description" => descending ? query.OrderByDescending(r => r.Description).ThenBy(r => r.Id).ToList()
                                        : query.OrderBy(r => r.Description).ThenBy(r => r.Id).ToList(),
            _             => descending ? query.OrderByDescending(r => r.OccurredOn).ThenBy(r => r.Id).ToList()
                                        : query.OrderBy(r => r.OccurredOn).ThenBy(r => r.Id).ToList()
        };
        var total   = ordered.Count;
        var items   = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<FinancialRecord>(items, page, pageSize, total));
    }

    public IAsyncEnumerable<FinancialRecord> StreamRecordsAsync(
        FilterCriteria filter, CancellationToken cancellationToken = default)
    {
        var query = _records.Values.AsEnumerable();
        if (filter.AccountId.HasValue)   query = query.Where(r => r.AccountId == filter.AccountId);
        if (filter.CategoryId.HasValue)  query = query.Where(r => r.CategoryId == filter.CategoryId);
        if (filter.StartDate.HasValue)   query = query.Where(r => DateOnly.FromDateTime(r.OccurredOn.Date) >= filter.StartDate);
        if (filter.EndDate.HasValue)     query = query.Where(r => DateOnly.FromDateTime(r.OccurredOn.Date) <= filter.EndDate);
        if (filter.MinAmount.HasValue)   query = query.Where(r => r.Amount.Amount >= filter.MinAmount);
        if (filter.MaxAmount.HasValue)   query = query.Where(r => r.Amount.Amount <= filter.MaxAmount);
        return ToAsyncEnumerable(query.OrderByDescending(r => r.OccurredOn).ThenBy(r => r.Id));
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
    {
        foreach (var item in source)
            yield return item;
        await Task.CompletedTask;
    }

    public Task<PagedResult<FinancialAccount>> GetAccountsPagedAsync(
        string? accountType, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = _accounts.OrderBy(a => a.Name).ThenBy(a => a.Id).ToList();
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<FinancialAccount>(paged, page, pageSize, items.Count));
    }

    public Task<PagedResult<Category>> GetCategoriesPagedAsync(
        string? nameSearch, Guid? parentId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _categories.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(nameSearch))
            query = query.Where(c => c.Name.Contains(nameSearch, StringComparison.OrdinalIgnoreCase));
        var items = query.OrderBy(c => c.Name).ThenBy(c => c.Id).ToList();
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<Category>(paged, page, pageSize, items.Count));
    }

    public Task<PagedResult<ClassificationRule>> GetRulesPagedAsync(
        string? ruleType, bool? isEnabled, Guid? categoryId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _classificationRules.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(ruleType))
            query = query.Where(r => r.Name.Contains(ruleType, StringComparison.OrdinalIgnoreCase));
        if (isEnabled.HasValue)
            query = query.Where(r => (r.Status == RuleStatus.Active) == isEnabled.Value);
        if (categoryId.HasValue)
            query = query.Where(r => r.TargetCategoryId == categoryId);
        var items = query.OrderByDescending(r => r.Priority).ThenBy(r => r.Id).ToList();
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<ClassificationRule>(paged, page, pageSize, items.Count));
    }
}

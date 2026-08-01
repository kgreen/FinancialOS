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
}

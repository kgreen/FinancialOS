using FinancialOS.Core.Contracts;
using FinancialOS.Core.Knowledge.Normalization;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Tests;

public sealed class MerchantNormalizationServiceTests
{
    private sealed class FakeRepository : IFinancialRepository
    {
        public List<CanonicalMerchant> CanonicalMerchants { get; } = new();
        public List<MerchantAliasMap> Aliases { get; } = new();

        public Task<CanonicalMerchant> AddCanonicalMerchantAsync(CanonicalMerchant merchant, CancellationToken cancellationToken = default)
        {
            CanonicalMerchants.Add(merchant);
            return Task.FromResult(merchant);
        }

        public Task<CanonicalMerchant?> GetCanonicalMerchantAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(CanonicalMerchants.FirstOrDefault(m => m.Id == id));

        public Task<IReadOnlyList<CanonicalMerchant>> ListCanonicalMerchantsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CanonicalMerchant>>(CanonicalMerchants);

        public Task<MerchantAliasMap> AddMerchantAliasAsync(MerchantAliasMap alias, CancellationToken cancellationToken = default)
        {
            Aliases.Add(alias);
            return Task.FromResult(alias);
        }

        public Task<IReadOnlyList<MerchantAliasMap>> ListMerchantAliasesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MerchantAliasMap>>(Aliases);

        // Unused members for this test's purposes.
        public Task<FinancialEvidence> AddEvidenceAsync(FinancialEvidence evidence, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<FinancialEvidence?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<FinancialEvidence>> ListEvidenceAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<FinancialRecord> AddRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<FinancialRecord?> GetRecordAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<FinancialRecord>> ListRecordsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<FinancialRecord>> ListPotentialDuplicateRecordsAsync(Guid recordId, Guid? accountId, DateTimeOffset occurredOn, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<FinancialRecord?> UpdateRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<FinancialAccount>> ListAccountsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Merchant>> ListMerchantsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Rule>> ListRulesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PlanningScenario> AddPlanningScenarioAsync(PlanningScenario scenario, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PlanningScenario?> GetPlanningScenarioAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<PlanningScenario>> ListPlanningScenariosAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ClassificationRule> AddClassificationRuleAsync(ClassificationRule rule, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ClassificationRule?> GetClassificationRuleAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ClassificationRule>> ListClassificationRulesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ClassificationRule?> UpdateClassificationRuleAsync(ClassificationRule rule, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<NormalizationDecision> AddNormalizationDecisionAsync(NormalizationDecision decision, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<NormalizationDecision>> ListNormalizationDecisionsAsync(Guid financialRecordId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<NormalizationDecision?> MarkNormalizationDecisionSupersededAsync(Guid decisionId, Guid supersededByDecisionId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<DuplicateCandidate> AddDuplicateCandidateAsync(DuplicateCandidate candidate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<DuplicateCandidate?> GetDuplicateCandidateAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<DuplicateCandidate>> ListDuplicateCandidatesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<DuplicateCandidate>> ListDuplicateCandidatesAsync(DuplicateCandidateStatus? status, decimal? minConfidence, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<DuplicateCandidate?> UpdateDuplicateCandidateAsync(DuplicateCandidate candidate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<long?> GetMaxProvenanceStepSequenceAsync(Guid financialRecordId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ProvenanceEntry> AppendProvenanceEntryAsync(ProvenanceEntry entry, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ProvenanceEntry>> ListProvenanceEntriesAsync(Guid financialRecordId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        // spec 003 stubs
        public Task<FinancialEvidence?> GetEvidenceBySha256Async(string sha256, CancellationToken cancellationToken = default) => Task.FromResult<FinancialEvidence?>(null);
        public Task<ImportJob> AddImportJobAsync(ImportJob job, CancellationToken cancellationToken = default) => Task.FromResult(job);
        public Task<ImportJob?> GetImportJobAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ImportJob?>(null);
        public Task<ImportJob?> UpdateImportJobAsync(ImportJob job, CancellationToken cancellationToken = default) => Task.FromResult<ImportJob?>(job);
        public Task<ImportJob?> GetImportJobByEvidenceIdAsync(Guid evidenceId, CancellationToken cancellationToken = default) => Task.FromResult<ImportJob?>(null);
        public Task<InstitutionProfile> AddInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(profile);
        public Task<InstitutionProfile?> GetInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<InstitutionProfile?>(null);
        public Task<IReadOnlyList<InstitutionProfile>> ListInstitutionProfilesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<InstitutionProfile>>(new List<InstitutionProfile>());
        public Task<InstitutionProfile?> UpdateInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default) => Task.FromResult<InstitutionProfile?>(profile);
        public Task<bool> DeleteInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> ExternalReferenceIdExistsAsync(string externalReferenceId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<FinancialRecord>> ListRecordsByImportJobAsync(Guid importJobId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FinancialRecord>>(new List<FinancialRecord>());
        // spec 004 stubs
        public Task<PagedResult<FinancialRecord>> GetRecordsPagedAsync(FilterCriteria filter, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<FinancialRecord> StreamRecordsAsync(FilterCriteria filter, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<FinancialAccount>> GetAccountsPagedAsync(string? accountType, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<Category>> GetCategoriesPagedAsync(string? nameSearch, Guid? parentId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<ClassificationRule>> GetRulesPagedAsync(string? ruleType, bool? isEnabled, Guid? categoryId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private static (FakeRepository Repo, MerchantAliasService Service, Guid CanonicalId) CreateServiceWithCanonicalMerchant(Guid? categoryId = null)
    {
        var repo = new FakeRepository();
        var canonicalId = Guid.NewGuid();
        repo.CanonicalMerchants.Add(new CanonicalMerchant
        {
            Id = canonicalId,
            DisplayName = "Whole Foods",
            NormalizedKey = "whole-foods",
            DefaultCategoryId = categoryId
        });
        var service = new MerchantAliasService(repo);
        return (repo, service, canonicalId);
    }

    [Fact]
    public async Task ResolveAsync_ExactStrategy_MatchesOnlyIdenticalNormalizedText()
    {
        var (repo, service, canonicalId) = CreateServiceWithCanonicalMerchant();
        repo.Aliases.Add(new MerchantAliasMap
        {
            CanonicalMerchantId = canonicalId,
            AliasRawText = "Whole Foods",
            AliasNormalizedText = "whole foods",
            MatchStrategy = AliasMatchStrategy.Exact,
            ConfidenceWeight = 0.9m,
            IsActive = true
        });

        var exactMatch = await service.ResolveAsync("Whole Foods");
        var partialMatch = await service.ResolveAsync("Whole Foods Market #4");

        Assert.NotNull(exactMatch);
        Assert.Equal(canonicalId, exactMatch!.CanonicalMerchantId);
        Assert.Null(partialMatch);
    }

    [Fact]
    public async Task ResolveAsync_ContainsStrategy_MatchesSubstring()
    {
        var (repo, service, canonicalId) = CreateServiceWithCanonicalMerchant();

        repo.Aliases.Add(new MerchantAliasMap
        {
            CanonicalMerchantId = canonicalId,
            AliasRawText = "WFM",
            AliasNormalizedText = "wfm",
            MatchStrategy = AliasMatchStrategy.Contains,
            ConfidenceWeight = 0.85m,
            IsActive = true
        });

        var match = await service.ResolveAsync("WFM #1023 SEATTLE");
        var noMatch = await service.ResolveAsync("Trader Joes");

        Assert.NotNull(match);
        Assert.Equal(canonicalId, match!.CanonicalMerchantId);
        Assert.Null(noMatch);
    }

    [Fact]
    public async Task ResolveAsync_TokenSetStrategy_MatchesWhenAllAliasTokensPresent()
    {
        var repo = new FakeRepository();
        var canonicalId = Guid.NewGuid();
        repo.CanonicalMerchants.Add(new CanonicalMerchant { Id = canonicalId, DisplayName = "Whole Foods", NormalizedKey = "whole-foods" });
        repo.Aliases.Add(new MerchantAliasMap
        {
            CanonicalMerchantId = canonicalId,
            AliasRawText = "Whole Foods Market",
            AliasNormalizedText = "whole foods market",
            MatchStrategy = AliasMatchStrategy.TokenSet,
            ConfidenceWeight = 0.8m,
            IsActive = true
        });
        var service = new MerchantAliasService(repo);

        var match = await service.ResolveAsync("market whole foods downtown");
        var noMatch = await service.ResolveAsync("whole foods"); // missing "market" token

        Assert.NotNull(match);
        Assert.Null(noMatch);
    }

    [Fact]
    public async Task ResolveAsync_MultipleMatches_PicksHighestConfidenceDeterministically()
    {
        var repo = new FakeRepository();
        var canonicalIdLow = Guid.NewGuid();
        var canonicalIdHigh = Guid.NewGuid();
        repo.CanonicalMerchants.Add(new CanonicalMerchant { Id = canonicalIdLow, DisplayName = "Low", NormalizedKey = "low" });
        repo.CanonicalMerchants.Add(new CanonicalMerchant { Id = canonicalIdHigh, DisplayName = "High", NormalizedKey = "high" });

        repo.Aliases.Add(new MerchantAliasMap
        {
            CanonicalMerchantId = canonicalIdLow,
            AliasRawText = "market",
            AliasNormalizedText = "market",
            MatchStrategy = AliasMatchStrategy.Contains,
            ConfidenceWeight = 0.5m,
            IsActive = true
        });
        repo.Aliases.Add(new MerchantAliasMap
        {
            CanonicalMerchantId = canonicalIdHigh,
            AliasRawText = "market",
            AliasNormalizedText = "market",
            MatchStrategy = AliasMatchStrategy.Contains,
            ConfidenceWeight = 0.95m,
            IsActive = true
        });

        var service = new MerchantAliasService(repo);
        var match = await service.ResolveAsync("local market downtown");

        Assert.NotNull(match);
        Assert.Equal(canonicalIdHigh, match!.CanonicalMerchantId);
        Assert.Equal(0.95m, match.ConfidenceWeight);
    }

    [Fact]
    public async Task ResolveAsync_InactiveAlias_IsIgnored()
    {
        var (repo, service, canonicalId) = CreateServiceWithCanonicalMerchant();
        repo.Aliases.Add(new MerchantAliasMap
        {
            CanonicalMerchantId = canonicalId,
            AliasRawText = "Whole Foods",
            AliasNormalizedText = "whole foods",
            MatchStrategy = AliasMatchStrategy.Exact,
            ConfidenceWeight = 0.9m,
            IsActive = false
        });

        var match = await service.ResolveAsync("Whole Foods");

        Assert.Null(match);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsDefaultCategoryFromCanonicalMerchant()
    {
        var categoryId = Guid.NewGuid();
        var (repo, service, canonicalId) = CreateServiceWithCanonicalMerchant(categoryId);
        repo.Aliases.Add(new MerchantAliasMap
        {
            CanonicalMerchantId = canonicalId,
            AliasRawText = "Whole Foods",
            AliasNormalizedText = "whole foods",
            MatchStrategy = AliasMatchStrategy.Exact,
            ConfidenceWeight = 0.9m,
            IsActive = true
        });

        var match = await service.ResolveAsync("Whole Foods");

        Assert.NotNull(match);
        Assert.Equal(categoryId, match!.DefaultCategoryId);
    }

    [Fact]
    public async Task ResolveAsync_NoAliasesMatch_ReturnsNull()
    {
        var (_, service, _) = CreateServiceWithCanonicalMerchant();

        var match = await service.ResolveAsync("Nonexistent Vendor");

        Assert.Null(match);
    }

    [Theory]
    [InlineData("  Whole Foods  ", "whole foods")]
    [InlineData("WHOLE FOODS", "whole foods")]
    [InlineData("", "")]
    public void Normalize_TrimsAndLowercases(string input, string expected)
    {
        Assert.Equal(expected, MerchantAliasService.Normalize(input));
    }
}

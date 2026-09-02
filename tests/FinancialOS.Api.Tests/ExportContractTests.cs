using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using FinancialOS.Infrastructure.Exporters;
using FinancialOS.Shared.Contracts;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T051 — Export endpoint contract tests: Content-Disposition, Content-Type per format,
/// validation errors for bad date range and unrecognised format.
/// </summary>
public sealed class ExportContractTests : IClassFixture<FilterAndExportFixture>
{
    private readonly HttpClient _client;

    public ExportContractTests(FilterAndExportFixture fixture)
    {
        _client = fixture.Client;
    }

    // ── Content-Type per format ───────────────────────────────────────────────

    [Theory]
    [InlineData("csv",        "text/csv")]
    [InlineData("ynab4",      "text/csv")]
    [InlineData("goodbudget", "text/csv")]
    [InlineData("json",       "application/json")]
    public async Task Export_ContentType_MatchesFormat(string format, string expectedMediaType)
    {
        var request = new { format, startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.StartsWith(expectedMediaType, response.Content.Headers.ContentType?.MediaType);
    }

    // ── Content-Disposition filename per format ────────────────────────────────

    [Theory]
    [InlineData("csv",        ".csv")]
    [InlineData("ynab4",      "-ynab4.csv")]
    [InlineData("goodbudget", "-goodbudget.csv")]
    [InlineData("json",       ".json")]
    public async Task Export_ContentDisposition_ContainsCorrectFileExtension(string format, string expectedSuffix)
    {
        var request = new { format, startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var disposition = response.Content.Headers.ContentDisposition?.ToString() ?? "";
        Assert.Contains(expectedSuffix, disposition);
    }

    // ── Validation errors ─────────────────────────────────────────────────────

    [Fact]
    public async Task Export_EndDateBeforeStartDate_Returns400()
    {
        var request = new { format = "csv", startDate = "2025-12-01", endDate = "2025-01-01" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Export_UnrecognisedFormat_Returns400()
    {
        // Use raw JSON to bypass enum serialisation
        var json = """{"format":"excel","startDate":"2025-01-01","endDate":"2025-01-31"}""";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/v1/exports", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Export_FilterMaxAmountLessThanMin_Returns400()
    {
        var request = new
        {
            format    = "csv",
            startDate = "2025-01-01",
            endDate   = "2025-01-31",
            filters   = new { minAmount = 100, maxAmount = 10 }
        };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExportService_WithLargeStreamedRecordSet_CompletesAndProducesNonEmptyContent()
    {
        var repository = new StubFinancialRepository(50_000);
        var exporter = new StubExporter();
        var service = new ExportService(repository, new[] { exporter });

        var snapshot = await service.ExportAsync(new ExportRequest
        {
            Format = ExportFormat.Csv,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 1, 31)
        });

        Assert.NotNull(snapshot.Content);
        using var content = new MemoryStream();
        await snapshot.Content.CopyToAsync(content);
        Assert.True(content.Length > 0);
        Assert.Equal(50_000, exporter.WrittenRecordCount);
        Assert.Equal(50_000, snapshot.RecordCount);
    }

    private sealed class StubFinancialRepository(int recordCount) : IFinancialRepository
    {
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
        public Task<CanonicalMerchant> AddCanonicalMerchantAsync(CanonicalMerchant merchant, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CanonicalMerchant?> GetCanonicalMerchantAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<CanonicalMerchant>> ListCanonicalMerchantsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<MerchantAliasMap> AddMerchantAliasAsync(MerchantAliasMap alias, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<MerchantAliasMap>> ListMerchantAliasesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
        public Task<FinancialEvidence?> GetEvidenceBySha256Async(string sha256, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ImportJob> AddImportJobAsync(ImportJob job, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ImportJob?> GetImportJobAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ImportJob?> UpdateImportJobAsync(ImportJob job, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ImportJob?> GetImportJobByEvidenceIdAsync(Guid evidenceId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ImportJob>> ListImportJobsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<InstitutionProfile> AddInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<InstitutionProfile?> GetInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<InstitutionProfile>> ListInstitutionProfilesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<InstitutionProfile?> UpdateInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> DeleteInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExternalReferenceIdExistsAsync(string externalReferenceId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<FinancialRecord>> ListRecordsByImportJobAsync(Guid importJobId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<FinancialRecord>> GetRecordsPagedAsync(FilterCriteria filter, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public async IAsyncEnumerable<FinancialRecord> StreamRecordsAsync(FilterCriteria filter, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            for (var i = 0; i < recordCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new FinancialRecord
                {
                    Id = Guid.NewGuid(),
                    Description = $"Exported-{i}",
                    Amount = new Money(i % 2 == 0 ? -1m : 1m, "USD"),
                    OccurredOn = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i % 31)
                };
            }
        }
        public Task<PagedResult<FinancialAccount>> GetAccountsPagedAsync(string? accountType, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<Category>> GetCategoriesPagedAsync(string? nameSearch, Guid? parentId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<ClassificationRule>> GetRulesPagedAsync(string? ruleType, bool? isEnabled, Guid? categoryId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Goal> AddGoalAsync(Goal goal, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Goal?> GetGoalAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Goal>> ListGoalsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Goal>>(Array.Empty<Goal>());
        public Task<Goal?> UpdateGoalAsync(Goal goal, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> DeleteGoalAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Budget> AddBudgetAsync(Budget budget, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Budget?> GetBudgetAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Budget>> ListBudgetsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Budget>>(Array.Empty<Budget>());
        public Task<Budget?> UpdateBudgetAsync(Budget budget, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> DeleteBudgetAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StubExporter : IRecordExporter
    {
        public ExportFormat Format => ExportFormat.Csv;
        public string ContentType => "text/csv; charset=utf-8";
        public string FileExtension => ".csv";
        public int WrittenRecordCount { get; private set; }

        public async Task WriteAsync(IAsyncEnumerable<FinancialRecord> records, Stream outputStream, CancellationToken cancellationToken = default)
        {
            await using var writer = new StreamWriter(outputStream, leaveOpen: true);
            await writer.WriteLineAsync("Date,Merchant,Amount,Category,Account,Notes");
            await foreach (var record in records.WithCancellation(cancellationToken))
            {
                WrittenRecordCount++;
                await writer.WriteLineAsync($"{record.OccurredOn:yyyy-MM-dd},{record.Description},{record.Amount.Amount},,,");
            }
            await writer.FlushAsync();
        }
    }
}

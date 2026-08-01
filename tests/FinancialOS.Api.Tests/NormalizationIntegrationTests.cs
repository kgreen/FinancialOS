using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Api.Tests;

public sealed class NormalizationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SeededCanonicalMerchantId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid SeededCategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly WebApplicationFactory<Program> _factory;

    public NormalizationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<Guid> SeedRecordAsync(string description, decimal amount = 10m)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOS.Data.FinancialOsDbContext>();
        var record = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = SeededAccountId,
            Description = description,
            Amount = new Money(amount, "USD"),
            OccurredOn = DateTimeOffset.UtcNow,
            Status = RecordStatus.Pending
        };
        dbContext.Records.Add(record);
        await dbContext.SaveChangesAsync();
        return record.Id;
    }

    [Fact]
    public async Task Normalize_KnownAliasVariant_ResolvesToCanonicalMerchant()
    {
        using var client = _factory.CreateClient();
        var recordId = await SeedRecordAsync("CONTOSO MARKET #99 SEATTLE WA");

        var response = await client.PostAsync($"/api/v1/records/{recordId}/normalize", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var decision = await response.Content.ReadFromJsonAsync<NormalizeRecordResponse>();
        Assert.NotNull(decision);
        Assert.Equal("Resolved", decision!.Status);
        Assert.Equal(SeededCanonicalMerchantId, decision.CanonicalMerchantId);
        Assert.Equal(SeededCategoryId, decision.CategoryId);
    }

    [Fact]
    public async Task Normalize_NoConfidentMatch_LeavesRecordUnresolvedForReview()
    {
        using var client = _factory.CreateClient();
        var recordId = await SeedRecordAsync($"Totally Unknown Vendor {Guid.NewGuid():N}");

        var response = await client.PostAsync($"/api/v1/records/{recordId}/normalize", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var decision = await response.Content.ReadFromJsonAsync<NormalizeRecordResponse>();
        Assert.NotNull(decision);
        Assert.Equal("Unresolved", decision!.Status);
        Assert.Null(decision.CanonicalMerchantId);
        Assert.Contains(decision.ReasonCodes, code => code == "no-match-found");
    }

    [Fact]
    public async Task Normalize_PreservesRawDescription_WhileLinkingCanonicalIdentity()
    {
        using var client = _factory.CreateClient();
        const string rawDescription = "Contoso Market Downtown Branch #7";
        var recordId = await SeedRecordAsync(rawDescription);

        await client.PostAsync($"/api/v1/records/{recordId}/normalize", content: null);

        var recordsResponse = await client.GetAsync("/api/v1/records");
        var records = await recordsResponse.Content.ReadFromJsonAsync<RecordListResponse>();
        Assert.NotNull(records);

        var record = records!.Items.FirstOrDefault(r => r.Id == recordId);
        Assert.NotNull(record);
        Assert.Equal(rawDescription, record!.Description);
    }

    [Fact]
    public async Task Normalize_CalledTwice_SecondDecisionSupersedesFirst()
    {
        using var client = _factory.CreateClient();
        var recordId = await SeedRecordAsync("Contoso Market Repeat Test");

        var firstResponse = await client.PostAsync($"/api/v1/records/{recordId}/normalize", content: null);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<NormalizeRecordResponse>();

        var secondResponse = await client.PostAsync($"/api/v1/records/{recordId}/normalize", content: null);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var second = await secondResponse.Content.ReadFromJsonAsync<NormalizeRecordResponse>();

        Assert.NotNull(first);
        Assert.NotNull(second);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOS.Data.FinancialOsDbContext>();
        var decisions = dbContext.NormalizationDecisions
            .Where(d => d.FinancialRecordId == recordId)
            .ToList()
            .OrderBy(d => d.CreatedAtUtc)
            .ToList();

        Assert.Equal(2, decisions.Count);
        Assert.NotNull(decisions[0].SupersededByDecisionId);
        Assert.Equal(decisions[1].Id, decisions[0].SupersededByDecisionId);
        Assert.Null(decisions[1].SupersededByDecisionId);
    }

    [Fact]
    public async Task Normalize_EmitsProvenanceEntriesForRecord()
    {
        using var client = _factory.CreateClient();
        var recordId = await SeedRecordAsync("Contoso Market Provenance Test");

        await client.PostAsync($"/api/v1/records/{recordId}/normalize", content: null);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOS.Data.FinancialOsDbContext>();
        var entries = dbContext.ProvenanceEntries
            .Where(e => e.FinancialRecordId == recordId)
            .ToList();

        Assert.NotEmpty(entries);
        Assert.Contains(entries, e => e.StepType == ProvenanceStepType.Normalization);
        Assert.All(entries, e => KnowledgeAssertions.AssertConfidenceInRange(e.Confidence ?? 0m));
    }
}

using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Data;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Api.Tests;

public sealed class ExplainabilityCoverageIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExplainabilityCoverageIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DecisionEndpoints_ExposeConfidenceReasonCodesAndProvenance()
    {
        var recordId = await SeedRecordAsync();
        using var client = _factory.CreateClient();

        var normalizeResponse = await client.PostAsync($"/api/v1/records/{recordId}/normalize", content: null);
        Assert.Equal(HttpStatusCode.OK, normalizeResponse.StatusCode);
        var normalize = await normalizeResponse.Content.ReadFromJsonAsync<NormalizeRecordResponse>();
        Assert.NotNull(normalize);
        KnowledgeAssertions.AssertConfidenceInRange(normalize!.Confidence);
        Assert.NotEmpty(normalize.ReasonCodes);

        var provenanceResponse = await client.GetAsync($"/api/v1/records/{recordId}/provenance");
        Assert.Equal(HttpStatusCode.OK, provenanceResponse.StatusCode);
        var provenance = await provenanceResponse.Content.ReadFromJsonAsync<ProvenanceTimelineResponse>();
        Assert.NotNull(provenance);
        Assert.NotEmpty(provenance!.Events);
        Assert.All(provenance.Events, item =>
        {
            KnowledgeAssertions.AssertConfidenceInRange(item.Confidence ?? 0m);
            Assert.NotEmpty(item.ReasonCodes);
        });
    }

    [Fact]
    public async Task DuplicateWorkflow_ExposesExplainabilityAndAuditTrail()
    {
        var recordId = await SeedDuplicatePairAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/duplicates/evaluate", new DuplicateEvaluateRequest(recordId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var candidate = await response.Content.ReadFromJsonAsync<DuplicateCandidateResponse>();
        Assert.NotNull(candidate);
        KnowledgeAssertions.AssertConfidenceInRange(candidate!.Confidence);
        Assert.NotEmpty(candidate.ReasonCodes);

        client.DefaultRequestHeaders.Add("X-Actor-Id", "steward-9");
        var confirm = await client.PostAsync($"/api/v1/duplicates/candidates/{candidate.Id}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        var provenance = await client.GetAsync($"/api/v1/records/{recordId}/provenance");
        Assert.Equal(HttpStatusCode.OK, provenance.StatusCode);
        var timeline = await provenance.Content.ReadFromJsonAsync<ProvenanceTimelineResponse>();
        Assert.NotNull(timeline);
        Assert.Contains(timeline!.Events, item => item.StepType == ProvenanceStepType.DuplicateReview.ToString());
    }

    private async Task<Guid> SeedRecordAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();

        var record = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Description = "Explainability Coverage",
            Amount = new Money(21.50m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)
        };

        db.Records.Add(record);
        await db.SaveChangesAsync();
        return record.Id;
    }

    private async Task<Guid> SeedDuplicatePairAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();

        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var primary = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Explainability Duplicate A",
            Amount = new Money(91m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
        };
        var match = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Explainability Duplicate A posted",
            Amount = new Money(91m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero)
        };

        db.Records.Add(primary);
        db.Records.Add(match);
        await db.SaveChangesAsync();
        return primary.Id;
    }
}

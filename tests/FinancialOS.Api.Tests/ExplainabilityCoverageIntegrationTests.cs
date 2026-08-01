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
    public async Task RuleAndNormalizationResponses_IncludeConfidenceAndReasonCodes()
    {
        var recordId = await SeedRecordAsync("Contoso Market Explainability");
        using var client = _factory.CreateClient();

        var normalizeResponse = await client.PostAsync($"/api/v1/records/{recordId}/normalize", content: null);
        Assert.Equal(HttpStatusCode.OK, normalizeResponse.StatusCode);
        var normalizeBody = await normalizeResponse.Content.ReadFromJsonAsync<NormalizeRecordResponse>();
        Assert.NotNull(normalizeBody);
        KnowledgeAssertions.AssertConfidenceInRange(normalizeBody!.Confidence);
        Assert.NotEmpty(normalizeBody.ReasonCodes);
        Assert.All(normalizeBody.ReasonCodes, KnowledgeAssertions.AssertHasReasonCode);
    }

    [Fact]
    public async Task DuplicateResponses_IncludeConfidenceAndReasonCodes()
    {
        var (primaryId, _) = await SeedDuplicatePairAsync();
        using var client = _factory.CreateClient();

        var evaluateResponse = await client.PostAsJsonAsync("/api/v1/duplicates/evaluate", new DuplicateEvaluateRequest(primaryId));
        Assert.Equal(HttpStatusCode.OK, evaluateResponse.StatusCode);

        var candidate = await evaluateResponse.Content.ReadFromJsonAsync<DuplicateCandidateResponse>();
        Assert.NotNull(candidate);
        KnowledgeAssertions.AssertConfidenceInRange(candidate!.Confidence);
        Assert.NotEmpty(candidate.ReasonCodes);
        Assert.All(candidate.ReasonCodes, KnowledgeAssertions.AssertHasReasonCode);
    }

    [Fact]
    public async Task ProvenanceTimeline_ContainsExplainableSystemAndUserEvents()
    {
        var (primaryId, _) = await SeedDuplicatePairAsync();
        using var client = _factory.CreateClient();

        await client.PostAsync($"/api/v1/records/{primaryId}/normalize", content: null);
        var evaluateResponse = await client.PostAsJsonAsync("/api/v1/duplicates/evaluate", new DuplicateEvaluateRequest(primaryId));
        var candidate = await evaluateResponse.Content.ReadFromJsonAsync<DuplicateCandidateResponse>();
        Assert.NotNull(candidate);

        client.DefaultRequestHeaders.Add("X-Actor-Id", "explainability-user");
        await client.PostAsync($"/api/v1/duplicates/candidates/{candidate!.Id}/dismiss", content: null);

        var timelineResponse = await client.GetAsync($"/api/v1/records/{primaryId}/provenance");
        Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<ProvenanceTimelineResponse>();
        Assert.NotNull(timeline);
        Assert.NotEmpty(timeline!.Events);

        Assert.Contains(timeline.Events, item => item.StepType == ProvenanceStepType.Normalization.ToString());
        Assert.Contains(timeline.Events, item => item.StepType == ProvenanceStepType.DuplicateDetection.ToString());
        Assert.Contains(timeline.Events, item => item.StepType == ProvenanceStepType.DuplicateReview.ToString() && item.ActorId == "explainability-user");
        Assert.All(timeline.Events, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.DecisionSummary));
            Assert.NotNull(item.ReasonCodes);
        });
    }

    private async Task<Guid> SeedRecordAsync(string description)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
        var record = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Description = description,
            Amount = new Money(19.99m, "USD"),
            OccurredOn = DateTimeOffset.UtcNow
        };

        db.Records.Add(record);
        await db.SaveChangesAsync();
        return record.Id;
    }

    private async Task<(Guid PrimaryId, Guid MatchId)> SeedDuplicatePairAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var primary = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Explainability pair",
            Amount = new Money(50m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)
        };
        var match = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Explainability pair posted",
            Amount = new Money(50m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 4, 2, 0, 0, 0, TimeSpan.Zero)
        };

        db.Records.Add(primary);
        db.Records.Add(match);
        await db.SaveChangesAsync();
        return (primary.Id, match.Id);
    }
}

using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Data;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Api.Tests;

public sealed class ProvenanceImmutabilityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProvenanceImmutabilityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Timeline_AfterRepeatedNormalization_AppendsButDoesNotMutateHistory()
    {
        var recordId = await SeedRecordAsync("Contoso Market Provenance Audit");
        using var client = _factory.CreateClient();

        var firstResponse = await client.PostAsync($"/api/v1/records/{recordId}/normalize", content: null);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var timelineResponse = await client.GetAsync($"/api/v1/records/{recordId}/provenance");
        Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);
        var firstTimeline = await timelineResponse.Content.ReadFromJsonAsync<ProvenanceTimelineResponse>();
        Assert.NotNull(firstTimeline);
        Assert.Equal(recordId, firstTimeline!.RecordId);
        Assert.NotEmpty(firstTimeline.Events);
        Assert.All(firstTimeline.Events, eventItem =>
        {
            KnowledgeAssertions.AssertConfidenceInRange(eventItem.Confidence ?? 0m);
            KnowledgeAssertions.AssertHasReasonCode(eventItem.ReasonCodes.FirstOrDefault());
            Assert.Equal(eventItem.Source, eventItem.Source.ToLowerInvariant());
        });

        var firstSnapshot = firstTimeline.Events.Select(eventItem => eventItem.Id).ToList();

        var secondResponse = await client.PostAsync($"/api/v1/records/{recordId}/normalize", content: null);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var secondTimelineResponse = await client.GetAsync($"/api/v1/records/{recordId}/provenance");
        Assert.Equal(HttpStatusCode.OK, secondTimelineResponse.StatusCode);
        var secondTimeline = await secondTimelineResponse.Content.ReadFromJsonAsync<ProvenanceTimelineResponse>();
        Assert.NotNull(secondTimeline);
        Assert.True(secondTimeline!.Events.Count > firstTimeline.Events.Count);
        Assert.All(firstSnapshot, id => Assert.Contains(secondTimeline.Events, eventItem => eventItem.Id == id));
    }

    [Fact]
    public async Task Timeline_IncludesDuplicateReviewExplainability()
    {
        var candidateId = await SeedDuplicateCandidateAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Actor-Id", "auditor-1");

        var reviewResponse = await client.PostAsync($"/api/v1/duplicates/candidates/{candidateId}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
        var candidate = await db.DuplicateCandidates.AsNoTracking().FirstAsync(item => item.Id == candidateId);

        var timelineResponse = await client.GetAsync($"/api/v1/records/{candidate.RecordId}/provenance");
        Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<ProvenanceTimelineResponse>();
        Assert.NotNull(timeline);

        Assert.Contains(timeline!.Events, item => item.StepType == ProvenanceStepType.DuplicateDetection.ToString());
        Assert.Contains(timeline.Events, item => item.StepType == ProvenanceStepType.DuplicateReview.ToString());
        Assert.Contains(timeline.Events, item => item.Source == "user" && item.ActorId == "auditor-1");
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
            Amount = new Money(17.25m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)
        };

        db.Records.Add(record);
        await db.SaveChangesAsync();
        return record.Id;
    }

    private async Task<Guid> SeedDuplicateCandidateAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();

        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var first = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Audit Transaction A",
            Amount = new Money(88m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var second = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Audit Transaction A posted",
            Amount = new Money(88m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 4, 2, 0, 0, 0, TimeSpan.Zero)
        };

        db.Records.Add(first);
        db.Records.Add(second);
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/duplicates/evaluate", new DuplicateEvaluateRequest(first.Id));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var candidate = await response.Content.ReadFromJsonAsync<DuplicateCandidateResponse>();
        Assert.NotNull(candidate);
        return candidate!.Id;
    }
}

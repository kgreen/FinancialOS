using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Data;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
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
    public async Task ProvenanceTimeline_CountOnlyIncreases_AcrossPipelineAndReviewActions()
    {
        var primaryId = await SeedDuplicatePairAndReturnPrimaryAsync();
        using var client = _factory.CreateClient();

        var before = await GetTimelineCountAsync(client, primaryId);

        var normalizeResponse = await client.PostAsync($"/api/v1/records/{primaryId}/normalize", content: null);
        Assert.Equal(HttpStatusCode.OK, normalizeResponse.StatusCode);
        var afterNormalize = await GetTimelineCountAsync(client, primaryId);
        Assert.True(afterNormalize > before);

        var evaluateResponse = await client.PostAsJsonAsync("/api/v1/duplicates/evaluate", new DuplicateEvaluateRequest(primaryId));
        Assert.Equal(HttpStatusCode.OK, evaluateResponse.StatusCode);
        var candidate = await evaluateResponse.Content.ReadFromJsonAsync<DuplicateCandidateResponse>();
        Assert.NotNull(candidate);
        var afterEvaluate = await GetTimelineCountAsync(client, primaryId);
        Assert.True(afterEvaluate > afterNormalize);

        client.DefaultRequestHeaders.Add("X-Actor-Id", "immutability-reviewer");
        var confirmResponse = await client.PostAsync($"/api/v1/duplicates/candidates/{candidate!.Id}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var afterConfirm = await GetTimelineCountAsync(client, primaryId);
        Assert.True(afterConfirm > afterEvaluate);
    }

    [Fact]
    public async Task ProvenanceEndpoints_ExposeNoUpdateOrDeletePaths()
    {
        var primaryId = await SeedDuplicatePairAndReturnPrimaryAsync();
        using var client = _factory.CreateClient();

        var patchResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/records/{primaryId}/provenance"));
        var deleteResponse = await client.DeleteAsync($"/api/v1/records/{primaryId}/provenance");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, patchResponse.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ProvenanceTimeline_UserActionsContainActorIdentityAndTimestamp()
    {
        var primaryId = await SeedDuplicatePairAndReturnPrimaryAsync();
        using var client = _factory.CreateClient();

        var evaluateResponse = await client.PostAsJsonAsync("/api/v1/duplicates/evaluate", new DuplicateEvaluateRequest(primaryId));
        var candidate = await evaluateResponse.Content.ReadFromJsonAsync<DuplicateCandidateResponse>();
        Assert.NotNull(candidate);

        client.DefaultRequestHeaders.Add("X-Actor-Id", "timeline-actor");
        var dismissResponse = await client.PostAsync($"/api/v1/duplicates/candidates/{candidate!.Id}/dismiss", content: null);
        Assert.Equal(HttpStatusCode.OK, dismissResponse.StatusCode);

        var timelineResponse = await client.GetAsync($"/api/v1/records/{primaryId}/provenance");
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<ProvenanceTimelineResponse>();
        Assert.NotNull(timeline);
        var userEntry = timeline!.Events.LastOrDefault(item => item.Source == "user");
        Assert.NotNull(userEntry);
        Assert.Equal("timeline-actor", userEntry!.ActorId);
        Assert.NotEqual(default, userEntry.CreatedAtUtc);
    }

    private async Task<int> GetTimelineCountAsync(HttpClient client, Guid recordId)
    {
        var response = await client.GetAsync($"/api/v1/records/{recordId}/provenance");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var timeline = await response.Content.ReadFromJsonAsync<ProvenanceTimelineResponse>();
        Assert.NotNull(timeline);
        return timeline!.Events.Count;
    }

    private async Task<Guid> SeedDuplicatePairAndReturnPrimaryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var primary = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Provenance immutable primary",
            Amount = new Money(88.10m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)
        };
        var match = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Provenance immutable primary posted",
            Amount = new Money(88.10m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero)
        };

        db.Records.Add(primary);
        db.Records.Add(match);
        await db.SaveChangesAsync();
        return primary.Id;
    }
}

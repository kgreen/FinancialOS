using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Data;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Api.Tests;

public sealed class DuplicateReviewIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DuplicateReviewIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CandidateLifecycle_PendingToConfirmed_PersistsAndEmitsProvenance()
    {
        var candidate = await EvaluateSeededPairAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Actor-Id", "reviewer-a");

        var confirmResponse = await client.PostAsync($"/api/v1/duplicates/candidates/{candidate.Id}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
        var persisted = await db.DuplicateCandidates.AsNoTracking().FirstAsync(item => item.Id == candidate.Id);
        Assert.Equal(DuplicateCandidateStatus.ConfirmedDuplicate, persisted.Status);
        Assert.Equal("reviewer-a", persisted.ReviewedByUserId);

        var provenance = await db.ProvenanceEntries.AsNoTracking()
            .Where(item => item.FinancialRecordId == persisted.RecordId)
            .ToListAsync();
        Assert.Contains(provenance, item => item.StepType == ProvenanceStepType.DuplicateDetection);
        Assert.Contains(provenance, item => item.StepType == ProvenanceStepType.DuplicateReview && item.ActorId == "reviewer-a");
    }

    [Fact]
    public async Task CandidateLifecycle_PendingToDismissed_PersistsAndEmitsProvenance()
    {
        var candidate = await EvaluateSeededPairAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Actor-Id", "reviewer-b");

        var dismissResponse = await client.PostAsync($"/api/v1/duplicates/candidates/{candidate.Id}/dismiss", content: null);
        Assert.Equal(HttpStatusCode.OK, dismissResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
        var persisted = await db.DuplicateCandidates.AsNoTracking().FirstAsync(item => item.Id == candidate.Id);
        Assert.Equal(DuplicateCandidateStatus.Dismissed, persisted.Status);
        Assert.Equal("reviewer-b", persisted.ReviewedByUserId);

        var reviewProvenance = await db.ProvenanceEntries.AsNoTracking()
            .Where(item => item.FinancialRecordId == persisted.RecordId && item.StepType == ProvenanceStepType.DuplicateReview)
            .ToListAsync();
        Assert.Contains(reviewProvenance, item => item.ActorId == "reviewer-b");
    }

    [Fact]
    public async Task ConfirmThenDismiss_RecordsAppendOnlyProvenanceGrowth()
    {
        var candidate = await EvaluateSeededPairAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Actor-Id", "reviewer-c");

        var confirmResponse = await client.PostAsync($"/api/v1/duplicates/candidates/{candidate.Id}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var dismissResponse = await client.PostAsync($"/api/v1/duplicates/candidates/{candidate.Id}/dismiss", content: null);
        Assert.Equal(HttpStatusCode.OK, dismissResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
        var entries = await db.ProvenanceEntries.AsNoTracking()
            .Where(item => item.FinancialRecordId == candidate.RecordId && item.StepType == ProvenanceStepType.DuplicateReview)
            .OrderBy(item => item.StepSequence)
            .ToListAsync();

        Assert.True(entries.Count >= 2);
        Assert.True(entries.Last().StepSequence > entries.First().StepSequence);
        Assert.All(entries, item => Assert.Equal(ProvenanceSourceType.User, item.Source));
    }

    private async Task<DuplicateCandidateResponse> EvaluateSeededPairAsync()
    {
        var recordId = await SeedDuplicateLikePairAndReturnPrimaryAsync();
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/duplicates/evaluate", new DuplicateEvaluateRequest(recordId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var candidate = await response.Content.ReadFromJsonAsync<DuplicateCandidateResponse>();
        Assert.NotNull(candidate);
        Assert.Equal("PendingReview", candidate!.Status);
        return candidate;
    }

    private async Task<Guid> SeedDuplicateLikePairAndReturnPrimaryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();

        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var primary = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Overlap Transaction A",
            Amount = new Money(120.00m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 2, 5, 0, 0, 0, TimeSpan.Zero)
        };

        var match = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Overlap Transaction A - posted",
            Amount = new Money(120.00m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 2, 6, 0, 0, 0, TimeSpan.Zero)
        };

        db.Records.Add(primary);
        db.Records.Add(match);
        await db.SaveChangesAsync();

        return primary.Id;
    }
}

using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Data;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Api.Tests;

public sealed class DuplicateWorkflowContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DuplicateWorkflowContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Evaluate_WithUnknownRecord_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/duplicates/evaluate", new DuplicateEvaluateRequest(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_WithValidRecord_ReturnsPendingCandidate()
    {
        var (primaryId, _) = await SeedDuplicateLikePairAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/duplicates/evaluate", new DuplicateEvaluateRequest(primaryId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<DuplicateCandidateResponse>();
        Assert.NotNull(body);
        Assert.Equal("PendingReview", body!.Status);
        Assert.Equal(primaryId, body.RecordId);
        Assert.NotEqual(Guid.Empty, body.MatchedRecordId);
        Assert.InRange(body.Confidence, 0m, 1m);
    }

    [Fact]
    public async Task List_WithStatusAndConfidenceFilter_ReturnsFilteredCandidates()
    {
        var (primaryId, _) = await SeedDuplicateLikePairAsync();
        using var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/duplicates/evaluate", new DuplicateEvaluateRequest(primaryId));

        var response = await client.GetAsync("/api/v1/duplicates/candidates?status=PendingReview&minConfidence=0.70");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<IReadOnlyList<DuplicateCandidateResponse>>();
        Assert.NotNull(body);
        Assert.NotEmpty(body!);
        Assert.All(body!, item =>
        {
            Assert.Equal("PendingReview", item.Status);
            Assert.True(item.Confidence >= 0.70m);
        });
    }

    [Fact]
    public async Task Confirm_WithoutActorHeader_ReturnsBadRequest()
    {
        var candidateId = await CreateCandidateAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/duplicates/candidates/{candidateId}/confirm", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_WithActorHeader_ReturnsUpdatedCandidate()
    {
        var candidateId = await CreateCandidateAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Actor-Id", "steward-1");

        var response = await client.PostAsync($"/api/v1/duplicates/candidates/{candidateId}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DuplicateCandidateResponse>();
        Assert.NotNull(body);
        Assert.Equal("ConfirmedDuplicate", body!.Status);
        Assert.Equal("steward-1", body.ReviewedByUserId);
        Assert.NotNull(body.ReviewedAtUtc);
    }

    [Fact]
    public async Task Dismiss_WithActorHeader_ReturnsUpdatedCandidate()
    {
        var candidateId = await CreateCandidateAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Actor-Id", "steward-2");

        var response = await client.PostAsync($"/api/v1/duplicates/candidates/{candidateId}/dismiss", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DuplicateCandidateResponse>();
        Assert.NotNull(body);
        Assert.Equal("Dismissed", body!.Status);
        Assert.Equal("steward-2", body.ReviewedByUserId);
    }

    private async Task<Guid> CreateCandidateAsync()
    {
        var (primaryId, _) = await SeedDuplicateLikePairAsync();
        using var client = _factory.CreateClient();
        var evaluate = await client.PostAsJsonAsync("/api/v1/duplicates/evaluate", new DuplicateEvaluateRequest(primaryId));
        var body = await evaluate.Content.ReadFromJsonAsync<DuplicateCandidateResponse>();
        return body!.Id;
    }

    private async Task<(Guid FirstId, Guid SecondId)> SeedDuplicateLikePairAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();

        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var first = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Contoso Market #100",
            Amount = new Money(47.50m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero)
        };
        var second = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = "Contoso Market 100",
            Amount = new Money(47.50m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 1, 11, 0, 0, 0, TimeSpan.Zero)
        };

        db.Records.Add(first);
        db.Records.Add(second);
        await db.SaveChangesAsync();
        return (first.Id, second.Id);
    }
}

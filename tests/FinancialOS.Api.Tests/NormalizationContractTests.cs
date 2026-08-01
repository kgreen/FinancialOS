using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Api.Tests;

public sealed class NormalizationContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededCanonicalMerchantId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly WebApplicationFactory<Program> _factory;

    public NormalizationContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostAlias_WithValidRequest_ReturnsCreatedWithBody()
    {
        using var client = _factory.CreateClient();

        var request = new MerchantAliasCreateRequest(
            AliasRawText: $"WFM #{Guid.NewGuid():N}",
            AliasNormalizedText: $"wfm-{Guid.NewGuid():N}",
            CanonicalMerchantId: SeededCanonicalMerchantId,
            MatchStrategy: AliasMatchStrategy.Contains,
            ConfidenceWeight: 0.92m,
            IsActive: true);

        var response = await client.PostAsJsonAsync("/api/v1/normalization/aliases", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MerchantAliasResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal(SeededCanonicalMerchantId, body.CanonicalMerchantId);
        Assert.Equal("Contains", body.MatchStrategy);
        Assert.Equal(0.92m, body.ConfidenceWeight);
    }

    [Fact]
    public async Task PostAlias_WithUnknownCanonicalMerchant_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var request = new MerchantAliasCreateRequest(
            AliasRawText: "Unknown Alias",
            AliasNormalizedText: "unknown alias",
            CanonicalMerchantId: Guid.NewGuid(),
            MatchStrategy: AliasMatchStrategy.Exact,
            ConfidenceWeight: 0.8m,
            IsActive: true);

        var response = await client.PostAsJsonAsync("/api/v1/normalization/aliases", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostAlias_WithMissingAliasText_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var request = new MerchantAliasCreateRequest(
            AliasRawText: "",
            AliasNormalizedText: "",
            CanonicalMerchantId: SeededCanonicalMerchantId,
            MatchStrategy: AliasMatchStrategy.Exact,
            ConfidenceWeight: 0.8m,
            IsActive: true);

        var response = await client.PostAsJsonAsync("/api/v1/normalization/aliases", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAliases_ReturnsOkWithList()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/normalization/aliases");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<IReadOnlyList<MerchantAliasResponse>>();
        Assert.NotNull(body);
        Assert.NotEmpty(body!);
    }

    [Fact]
    public async Task PostNormalize_WithNonexistentRecord_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/records/{Guid.NewGuid()}/normalize", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostNormalize_WithMatchingSeededMerchant_ReturnsResolvedDecision()
    {
        using var client = _factory.CreateClient();

        Guid recordId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOS.Data.FinancialOsDbContext>();
            var record = new FinancialRecord
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Description = "Contoso Market #204",
                Amount = new Money(42.50m, "USD"),
                OccurredOn = DateTimeOffset.UtcNow,
                Status = RecordStatus.Pending
            };
            dbContext.Records.Add(record);
            await dbContext.SaveChangesAsync();
            recordId = record.Id;
        }

        var normalizeResponse = await client.PostAsync($"/api/v1/records/{recordId}/normalize", content: null);
        Assert.Equal(HttpStatusCode.OK, normalizeResponse.StatusCode);

        var body = await normalizeResponse.Content.ReadFromJsonAsync<NormalizeRecordResponse>();
        Assert.NotNull(body);
        Assert.Equal(recordId, body!.RecordId);
        Assert.Equal("Resolved", body.Status);
        Assert.Equal(SeededCanonicalMerchantId, body.CanonicalMerchantId);
        Assert.True(body.Confidence >= 0m && body.Confidence <= 1m);
        Assert.NotEqual(Guid.Empty, body.ProvenanceCorrelationId);
        Assert.Contains(body.ReasonCodes, code => code.Contains("match", StringComparison.OrdinalIgnoreCase));
    }
}

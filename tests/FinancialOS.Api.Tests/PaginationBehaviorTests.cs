using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Data;
using FinancialOS.Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T037 — Pagination validation: out-of-range or invalid parameters return HTTP 400
/// with the offending parameter named. Accounts, categories, and rules endpoints
/// are also validated.
/// </summary>
public sealed class PaginationBehaviorTests : IClassFixture<FilterAndExportFixture>
{
    private readonly HttpClient _client;
    private readonly FilterAndExportFixture _fixture;

    public PaginationBehaviorTests(FilterAndExportFixture fixture)
    {
        _client = fixture.Client;
        _fixture = fixture;
    }

    // ── Records ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Records_PageSizeOver200_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/records?pageSize=201");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Records_PageZero_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/records?page=0&pageSize=10");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Records_EndDateBeforeStartDate_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/records?startDate=2025-12-01&endDate=2025-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Records_MaxAmountLessThanMinAmount_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/records?minAmount=100&maxAmount=10");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Records_DefaultParameters_ReturnsFirstPage()
    {
        var response = await _client.GetAsync("/api/v1/records");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.Page);
        Assert.Equal(25, body.PageSize);
    }

    // ── Accounts ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Accounts_PageSizeOver200_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/accounts?pageSize=201");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Accounts_DefaultParameters_ReturnsPagedResult()
    {
        var response = await _client.GetAsync("/api/v1/accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<ReferenceItemResponse>>();
        Assert.NotNull(body);
        Assert.True(body!.TotalCount >= 0);
    }

    // ── Categories ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Categories_PageSizeOver200_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/categories?pageSize=201");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Categories_DefaultParameters_ReturnsPagedResult()
    {
        var response = await _client.GetAsync("/api/v1/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<ReferenceItemResponse>>();
        Assert.NotNull(body);
        Assert.True(body!.TotalCount >= 0);
    }

    // ── Classification rules ──────────────────────────────────────────────────

    [Fact]
    public async Task ClassificationRules_PageSizeOver200_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/classification-rules?pageSize=201");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ClassificationRules_DefaultParameters_ReturnsPagedResult()
    {
        var response = await _client.GetAsync("/api/v1/classification-rules");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RuleItemResponse>>();
        Assert.NotNull(body);
        Assert.True(body!.TotalCount >= 0);
    }

    [Fact]
    public async Task Records_SameDateAcrossPages_AreOrderedDeterministically()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();

        for (var i = 0; i < 50; i++)
        {
            db.Records.Add(new FinancialRecord
            {
                Description = $"Deterministic-{i:0000}",
                Amount = new Money(-1.00m, "USD"),
                OccurredOn = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                AccountId = FilterAndExportFixture.AccountA,
                CategoryId = FilterAndExportFixture.CategoryX,
            });
        }

        await db.SaveChangesAsync();

        var page1 = await _client.GetAsync("/api/v1/records?merchant=deterministic&pageSize=10");
        var page2 = await _client.GetAsync("/api/v1/records?merchant=deterministic&pageSize=10&page=2");
        var page3 = await _client.GetAsync("/api/v1/records?merchant=deterministic&pageSize=10&page=3");
        var page4 = await _client.GetAsync("/api/v1/records?merchant=deterministic&pageSize=10&page=4");
        var page5 = await _client.GetAsync("/api/v1/records?merchant=deterministic&pageSize=10&page=5");

        var pages = new[] { page1, page2, page3, page4, page5 };
        var ids = new HashSet<Guid>();

        foreach (var page in pages)
        {
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var body = await page.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
            Assert.NotNull(body);
            foreach (var item in body!.Items)
            {
                Assert.True(ids.Add(item.Id), $"Duplicate ID {item.Id} encountered across pages.");
            }
        }

        Assert.Equal(50, ids.Count);
    }
}

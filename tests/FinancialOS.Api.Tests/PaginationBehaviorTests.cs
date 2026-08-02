using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T037 — Pagination validation: out-of-range or invalid parameters return HTTP 400
/// with the offending parameter named. Accounts, categories, and rules endpoints
/// are also validated.
/// </summary>
public sealed class PaginationBehaviorTests : IClassFixture<FilterAndExportFixture>
{
    private readonly HttpClient _client;

    public PaginationBehaviorTests(FilterAndExportFixture fixture)
    {
        _client = fixture.Client;
    }

    // ── Records ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Records_PageSizeOver200_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/records?pageSize=201");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Records_PageZero_Returns200WithPage1Behaviour()
    {
        // page=0 is normalised to 1 by Math.Max(1, q.Page ?? 1) in Program.cs
        var response = await _client.GetAsync("/api/v1/records?page=0&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.Page);
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
}

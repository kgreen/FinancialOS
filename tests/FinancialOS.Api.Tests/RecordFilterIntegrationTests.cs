using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T036 — Filter integration tests: each query param independently produces the correct subset.
/// Relies on the 30 deterministic records seeded by FilterAndExportFixture:
///   - Records  0-9 : "Amazon",    2025-01-xx, -10.00,  AccountA, CategoryX
///   - Records 10-19: "Starbucks", 2025-02-xx, -5.00,   AccountA
///   - Records 20-29: "Paycheck",  2025-03-xx, +200.00, AccountB
/// </summary>
public sealed class RecordFilterIntegrationTests : IClassFixture<FilterAndExportFixture>
{
    private readonly HttpClient _client;

    public RecordFilterIntegrationTests(FilterAndExportFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Filter_ByStartDate_ReturnsOnlyRecordsOnOrAfter()
    {
        var response = await _client.GetAsync("/api/v1/records?startDate=2025-02-01&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(20, body!.TotalCount); // Feb + Mar = 20
    }

    [Fact]
    public async Task Filter_ByEndDate_ReturnsOnlyRecordsOnOrBefore()
    {
        var response = await _client.GetAsync("/api/v1/records?endDate=2025-01-31&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(10, body!.TotalCount); // Jan only
    }

    [Fact]
    public async Task Filter_ByDateRange_ReturnsOnlyRecordsInRange()
    {
        var response = await _client.GetAsync("/api/v1/records?startDate=2025-02-01&endDate=2025-02-28&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(10, body!.TotalCount); // Feb only
    }

    [Fact]
    public async Task Filter_ByAccountId_ReturnsOnlyRecordsForThatAccount()
    {
        var response = await _client.GetAsync(
            $"/api/v1/records?accountId={FilterAndExportFixture.AccountB}&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(10, body!.TotalCount); // Paycheck records only
    }

    [Fact]
    public async Task Filter_ByCategoryId_ReturnsOnlyRecordsWithThatCategory()
    {
        var response = await _client.GetAsync(
            $"/api/v1/records?categoryId={FilterAndExportFixture.CategoryX}&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(10, body!.TotalCount); // Amazon records only
    }

    [Fact]
    public async Task Filter_ByMerchant_PartialCaseInsensitiveMatch()
    {
        var response = await _client.GetAsync("/api/v1/records?merchant=star&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(10, body!.TotalCount); // Starbucks only
    }

    [Fact]
    public async Task Filter_ByMinAmount_ReturnsOnlyRecordsWithAmountAtLeast()
    {
        // Only Paycheck records have amount >= 100
        var response = await _client.GetAsync("/api/v1/records?minAmount=100&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(10, body!.TotalCount); // Paycheck = +200
    }

    [Fact]
    public async Task Filter_ByMaxAmount_ReturnsOnlyRecordsWithAmountAtMost()
    {
        // Amazon (-10) and Starbucks (-5) both have amounts <= -5; Paycheck (+200) does not
        var response = await _client.GetAsync("/api/v1/records?maxAmount=-5&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(20, body!.TotalCount); // Amazon + Starbucks
    }

    [Fact]
    public async Task Filter_BoundaryInclusive_StartDateEqualsRecordDate_MatchesRecord()
    {
        // 2025-01-10 is the last Amazon record
        var response = await _client.GetAsync("/api/v1/records?startDate=2025-01-10&endDate=2025-01-10&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.TotalCount);
    }
}

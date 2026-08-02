using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T035 — Pagination contract: shape, metadata accuracy, and beyond-last-page behaviour.
/// </summary>
public sealed class RecordFilterContractTests : IClassFixture<FilterAndExportFixture>
{
    private readonly HttpClient _client;

    public RecordFilterContractTests(FilterAndExportFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task GetRecords_PageOne_Returns10ItemsWithCorrectMetadata()
    {
        var response = await _client.GetAsync("/api/v1/records?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(10, body!.Items.Count);
        Assert.Equal(1, body.Page);
        Assert.Equal(10, body.PageSize);
        Assert.Equal(30, body.TotalCount);
        Assert.Equal(3, body.TotalPages);
    }

    [Fact]
    public async Task GetRecords_BeyondLastPage_ReturnsEmptyItemsWithCorrectTotalCount()
    {
        var response = await _client.GetAsync("/api/v1/records?page=4&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Empty(body!.Items);
        Assert.Equal(30, body.TotalCount);
        Assert.Equal(3, body.TotalPages);
    }

    [Fact]
    public async Task GetRecords_LastPage_ReturnsRemainingItems()
    {
        // 30 records / pageSize=25 = page 2 has 5 items
        var response = await _client.GetAsync("/api/v1/records?page=2&pageSize=25");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Equal(5, body!.Items.Count);
        Assert.Equal(30, body.TotalCount);
        Assert.Equal(2, body.TotalPages);
    }

    [Fact]
    public async Task GetRecords_ResponseShape_ContainsRequiredFields()
    {
        var response = await _client.GetAsync("/api/v1/records?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(body);
        Assert.Single(body!.Items);

        var item = body.Items[0];
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.NotNull(item.Status);
    }
}

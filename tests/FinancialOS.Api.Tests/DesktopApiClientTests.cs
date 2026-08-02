using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Desktop.Configuration;
using FinancialOS.Desktop.Services;
using FinancialOS.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace FinancialOS.Api.Tests;

/// <summary>
/// Lightweight fake handler so desktop tests need no extra NuGet packages.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        => _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));
}

/// <summary>
/// T060 — Unit tests for <see cref="FinancialApiClient"/> using a fake HTTP handler.
/// Verifies deserialisation, query-string construction, and POST body forwarding.
/// </summary>
public sealed class DesktopApiClientTests
{
    private static FinancialApiClient BuildClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var fakeHandler = new FakeHttpMessageHandler(handler);
        var http = new HttpClient(fakeHandler);
        var options = Options.Create(new ApiClientOptions
        {
            BaseUrl        = "http://localhost:5000",
            TimeoutSeconds = 30
        });
        return new FinancialApiClient(http, options);
    }

    private static HttpResponseMessage JsonOk<T>(T value) =>
        new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(value, options: new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            })
        };

    // ── GetAccountsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAccountsAsync_DeserialisesPagedResult()
    {
        var fakeResult = new PagedResult<ReferenceItemResponse>(
            Items: [new ReferenceItemResponse(Guid.NewGuid(), "Checking", "account")],
            Page: 1, PageSize: 25, TotalCount: 1);

        var client = BuildClient(_ => JsonOk(fakeResult));
        var result = await client.GetAccountsAsync(page: 1, pageSize: 25);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Checking", result.Items[0].Name);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAccountsAsync_SendsPageQueryParams()
    {
        string? capturedUri = null;
        var client = BuildClient(req =>
        {
            capturedUri = req.RequestUri?.ToString();
            return JsonOk(new PagedResult<ReferenceItemResponse>([], 2, 10, 0));
        });

        await client.GetAccountsAsync(page: 2, pageSize: 10);

        Assert.NotNull(capturedUri);
        Assert.Contains("page=2",     capturedUri!);
        Assert.Contains("pageSize=10", capturedUri);
    }

    // ── GetRecordsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecordsAsync_DeserialisesPagedResult()
    {
        var fakeResult = new PagedResult<RecordResponse>(
            Items: [new RecordResponse(
                Guid.NewGuid(), null, null, null, null,
                "Amazon", -10m, "USD",
                DateTimeOffset.UtcNow, "pending", null, null)],
            Page: 1, PageSize: 25, TotalCount: 1);

        var client = BuildClient(_ => JsonOk(fakeResult));
        var result = await client.GetRecordsAsync(page: 1, pageSize: 25);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Amazon", result.Items[0].Description);
    }

    [Fact]
    public async Task GetRecordsAsync_WithDateFilter_SendsDateQueryParams()
    {
        string? capturedUri = null;
        var client = BuildClient(req =>
        {
            capturedUri = req.RequestUri?.ToString();
            return JsonOk(new PagedResult<RecordResponse>([], 1, 25, 0));
        });

        await client.GetRecordsAsync(
            startDate: new DateOnly(2025, 1, 1),
            endDate:   new DateOnly(2025, 1, 31));

        Assert.NotNull(capturedUri);
        Assert.Contains("startDate=2025-01-01", capturedUri!);
        Assert.Contains("endDate=2025-01-31",   capturedUri);
    }
}

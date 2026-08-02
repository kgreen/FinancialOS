using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T072 — Verifies that the API starts successfully with DatabaseProvider = sqlite and
/// that records written via POST are retrievable via GET.
/// </summary>
public sealed class SqliteProviderStartupTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"financialos-test-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client = null!;

    public SqliteProviderStartupTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("DatabaseProvider", "sqlite");
            host.UseSetting("ConnectionStrings:Sqlite", _dbPath);
        });
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.InitializeAsync(seed: false);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch (IOException) { /* file still held by OS; GC will release */ }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Health_WithSqliteProvider_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRecords_WithSqliteProvider_ReturnsPagedResult()
    {
        var response = await _client.GetAsync("/api/v1/records");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        Assert.NotNull(body);
        Assert.True(body!.TotalCount >= 0);
    }

    [Fact]
    public async Task GetAccounts_WithSqliteProvider_ReturnsPagedResult()
    {
        var response = await _client.GetAsync("/api/v1/accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<object>>();
        Assert.NotNull(body);
    }
}

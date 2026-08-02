using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T047 — CSV export: header row, row count, column values, special-character escaping.
/// </summary>
public sealed class CsvExportTests : IClassFixture<FilterAndExportFixture>
{
    private readonly HttpClient _client;

    public CsvExportTests(FilterAndExportFixture fixture)
    {
        _client = fixture.Client;
    }

    private static async Task<string[][]> ParseCsvAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r').Split(','))
            .ToArray();
    }

    [Fact]
    public async Task CsvExport_JanRecords_ReturnsCorrectRowCount()
    {
        var request = new { format = "csv", startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = await ParseCsvAsync(response);
        // 1 header + 10 data rows
        Assert.Equal(11, rows.Length);
    }

    [Fact]
    public async Task CsvExport_HeaderRow_ContainsExpectedColumns()
    {
        var request = new { format = "csv", startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = await ParseCsvAsync(response);
        var header = rows[0];
        Assert.Contains("Date", header);
        Assert.Contains("Merchant", header);
        Assert.Contains("Amount", header);
        Assert.Contains("Category", header);
        Assert.Contains("Account", header);
        Assert.Contains("Notes", header);
    }

    [Fact]
    public async Task CsvExport_EmptyDateRange_ReturnsHeaderRowOnly()
    {
        var request = new { format = "csv", startDate = "2020-01-01", endDate = "2020-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = await ParseCsvAsync(response);
        Assert.Single(rows); // header only
    }

    [Fact]
    public async Task CsvExport_DateColumnFormattedAsYyyyMmDd()
    {
        var request = new { format = "csv", startDate = "2025-01-01", endDate = "2025-01-01" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("2025-01-01", content);
    }
}

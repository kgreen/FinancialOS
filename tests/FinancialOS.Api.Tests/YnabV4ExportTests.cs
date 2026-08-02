using System.Net;
using System.Net.Http.Json;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T049 — YNAB 4 export: column names, Outflow/Inflow sign splitting, empty result.
/// Seed data: Amazon records (-10.00) and Paycheck records (+200.00).
/// </summary>
public sealed class Ynab4ExportTests : IClassFixture<FilterAndExportFixture>
{
    private readonly HttpClient _client;

    public Ynab4ExportTests(FilterAndExportFixture fixture)
    {
        _client = fixture.Client;
    }

    private static string[][] ParseCsv(string content) =>
        content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
               .Select(l => l.TrimEnd('\r').Split(','))
               .ToArray();

    [Fact]
    public async Task Ynab4Export_HeaderRow_HasExactlyFiveExpectedColumns()
    {
        var request = new { format = "ynab4", startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = ParseCsv(await response.Content.ReadAsStringAsync());
        var header = rows[0];
        Assert.Equal(5, header.Length);
        Assert.Equal("Date",    header[0]);
        Assert.Equal("Payee",   header[1]);
        Assert.Equal("Memo",    header[2]);
        Assert.Equal("Outflow", header[3]);
        Assert.Equal("Inflow",  header[4]);
    }

    [Fact]
    public async Task Ynab4Export_NegativeAmount_AppearsOnlyInOutflowColumn()
    {
        // Amazon records: amount = -10.00
        var request = new { format = "ynab4", startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = ParseCsv(await response.Content.ReadAsStringAsync());
        foreach (var row in rows.Skip(1)) // skip header
        {
            Assert.Equal("10.00", row[3].Trim()); // Outflow = positive abs
            Assert.Equal("",      row[4].Trim()); // Inflow  = empty
        }
    }

    [Fact]
    public async Task Ynab4Export_PositiveAmount_AppearsOnlyInInflowColumn()
    {
        // Paycheck records: amount = +200.00
        var request = new { format = "ynab4", startDate = "2025-03-01", endDate = "2025-03-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = ParseCsv(await response.Content.ReadAsStringAsync());
        foreach (var row in rows.Skip(1))
        {
            Assert.Equal("0.00",   row[3].Trim()); // Outflow = 0.00
            Assert.Equal("200.00", row[4].Trim()); // Inflow  = positive value
        }
    }

    [Fact]
    public async Task Ynab4Export_EmptyDateRange_ReturnsHeaderRowOnly()
    {
        var request = new { format = "ynab4", startDate = "2020-01-01", endDate = "2020-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = ParseCsv(await response.Content.ReadAsStringAsync());
        Assert.Single(rows);
    }

    [Fact]
    public async Task Ynab4Export_DateFormat_IsMmDdYyyy()
    {
        var request = new { format = "ynab4", startDate = "2025-01-01", endDate = "2025-01-01" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("01/01/2025", content);
    }
}

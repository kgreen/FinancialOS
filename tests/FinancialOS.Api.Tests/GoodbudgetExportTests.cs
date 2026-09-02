using System.Net;
using System.Net.Http.Json;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T050 — Goodbudget export: column names, field mapping, amount sign preservation.
/// </summary>
public sealed class GoodbudgetExportTests : IClassFixture<FilterAndExportFixture>
{
    private readonly HttpClient _client;

    public GoodbudgetExportTests(FilterAndExportFixture fixture)
    {
        _client = fixture.Client;
    }

    private static string[][] ParseCsv(string content)
    {
        using var reader = new StringReader(content);
        using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);

        var rows = new List<string[]>();
        if (!csv.Read())
            return Array.Empty<string[]>();

        csv.ReadHeader();
        rows.Add(csv.HeaderRecord!);

        while (csv.Read())
        {
            rows.Add(csv.Context.Parser.Record!);
        }

        return rows.ToArray();
    }

    [Fact]
    public async Task GoodbudgetExport_HeaderRow_HasExactlySixExpectedColumns()
    {
        var request = new { format = "goodbudget", startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = ParseCsv(await response.Content.ReadAsStringAsync());
        var header = rows[0];
        Assert.Equal(6, header.Length);
        Assert.Equal("Date",     header[0]);
        Assert.Equal("Envelope", header[1]);
        Assert.Equal("Account",  header[2]);
        Assert.Equal("Name",     header[3]);
        Assert.Equal("Amount",   header[4]);
        Assert.Equal("Notes",    header[5]);
    }

    [Fact]
    public async Task GoodbudgetExport_NegativeAmount_IsPreservedAsSigned()
    {
        // Amazon = -10.00
        var request = new { format = "goodbudget", startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = ParseCsv(await response.Content.ReadAsStringAsync());
        foreach (var row in rows.Skip(1))
        {
            Assert.Equal("-10.00", row[4].Trim());
        }
    }

    [Fact]
    public async Task GoodbudgetExport_PositiveAmount_IsPreservedAsSigned()
    {
        // Paycheck = +200.00
        var request = new { format = "goodbudget", startDate = "2025-03-01", endDate = "2025-03-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = ParseCsv(await response.Content.ReadAsStringAsync());
        foreach (var row in rows.Skip(1))
        {
            Assert.Equal("200.00", row[4].Trim());
        }
    }

    [Fact]
    public async Task GoodbudgetExport_NameColumn_ContainsMerchantDescription()
    {
        var request = new { format = "goodbudget", startDate = "2025-02-01", endDate = "2025-02-28" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Starbucks", content);
    }

    [Fact]
    public async Task GoodbudgetExport_EmptyDateRange_ReturnsHeaderRowOnly()
    {
        var request = new { format = "goodbudget", startDate = "2020-01-01", endDate = "2020-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = ParseCsv(await response.Content.ReadAsStringAsync());
        Assert.Single(rows);
    }
}

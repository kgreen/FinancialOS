using System.Net;
using System.Net.Http.Json;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T051 — Export endpoint contract tests: Content-Disposition, Content-Type per format,
/// validation errors for bad date range and unrecognised format.
/// </summary>
public sealed class ExportContractTests : IClassFixture<FilterAndExportFixture>
{
    private readonly HttpClient _client;

    public ExportContractTests(FilterAndExportFixture fixture)
    {
        _client = fixture.Client;
    }

    // ── Content-Type per format ───────────────────────────────────────────────

    [Theory]
    [InlineData("csv",        "text/csv")]
    [InlineData("ynab4",      "text/csv")]
    [InlineData("goodbudget", "text/csv")]
    [InlineData("json",       "application/json")]
    public async Task Export_ContentType_MatchesFormat(string format, string expectedMediaType)
    {
        var request = new { format, startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.StartsWith(expectedMediaType, response.Content.Headers.ContentType?.MediaType);
    }

    // ── Content-Disposition filename per format ────────────────────────────────

    [Theory]
    [InlineData("csv",        ".csv")]
    [InlineData("ynab4",      "-ynab4.csv")]
    [InlineData("goodbudget", "-goodbudget.csv")]
    [InlineData("json",       ".json")]
    public async Task Export_ContentDisposition_ContainsCorrectFileExtension(string format, string expectedSuffix)
    {
        var request = new { format, startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var disposition = response.Content.Headers.ContentDisposition?.ToString() ?? "";
        Assert.Contains(expectedSuffix, disposition);
    }

    // ── Validation errors ─────────────────────────────────────────────────────

    [Fact]
    public async Task Export_EndDateBeforeStartDate_Returns400()
    {
        var request = new { format = "csv", startDate = "2025-12-01", endDate = "2025-01-01" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Export_UnrecognisedFormat_Returns400()
    {
        // Use raw JSON to bypass enum serialisation
        var json = """{"format":"excel","startDate":"2025-01-01","endDate":"2025-01-31"}""";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/v1/exports", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Export_FilterMaxAmountLessThanMin_Returns400()
    {
        var request = new
        {
            format    = "csv",
            startDate = "2025-01-01",
            endDate   = "2025-01-31",
            filters   = new { minAmount = 100, maxAmount = 10 }
        };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

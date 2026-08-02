using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T048 — JSON export: array structure, record count, provenance fields, empty result.
/// </summary>
public sealed class JsonExportTests : IClassFixture<FilterAndExportFixture>
{
    private readonly HttpClient _client;

    public JsonExportTests(FilterAndExportFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task JsonExport_JanRecords_ReturnsArrayWith10Items()
    {
        var request = new { format = "json", startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(10, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task JsonExport_EachItem_ContainsProvenanceObject()
    {
        var request = new { format = "json", startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            Assert.True(element.TryGetProperty("provenance", out var provenance),
                "Each item must have a 'provenance' property.");
            Assert.True(provenance.TryGetProperty("confidenceScore", out _),
                "provenance must have 'confidenceScore'.");
        }
    }

    [Fact]
    public async Task JsonExport_EmptyDateRange_ReturnsEmptyArray()
    {
        var request = new { format = "json", startDate = "2020-01-01", endDate = "2020-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", json.Trim());
    }

    [Fact]
    public async Task JsonExport_ContentType_IsApplicationJson()
    {
        var request = new { format = "json", startDate = "2025-01-01", endDate = "2025-01-31" };
        var response = await _client.PostAsJsonAsync("/api/v1/exports", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}

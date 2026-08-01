using System.Net;
using System.Net.Http.Json;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancialOS.Api.Tests;

public sealed class RecordCreationAndClassificationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RecordCreationAndClassificationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListRecords_ReturnsOkWithRecordList()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/records");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RecordListResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
    }

    [Fact]
    public async Task ClassifyRecord_WithNonexistentRecord_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var recordId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();

        var classificationRequest = new RecordClassificationRequest(
            categoryId,
            merchantId,
            0.85m,
            "test-rule",
            null);

        var response = await client.PostAsJsonAsync($"/api/v1/records/{recordId}/classify", classificationRequest);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListRecords_ResponseContainsExpectedFields()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/records");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RecordListResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        
        if (result.Items.Any())
        {
            var record = result.Items.First();
            Assert.NotEqual(Guid.Empty, record.Id);
            Assert.NotEmpty(record.Description);
            Assert.True(record.Amount >= 0);
            Assert.NotEmpty(record.Currency);
            Assert.NotEmpty(record.Status);
        }
    }
}


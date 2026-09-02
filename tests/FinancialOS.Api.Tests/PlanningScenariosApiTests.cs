using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancialOS.Api.Tests;

public sealed class PlanningScenariosApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PlanningScenariosApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAndGetPlanningScenario_RoundTripsScenario()
    {
        using var client = _factory.CreateClient();
        var request = new PlanningScenarioCreateRequest("Emergency Fund", "Build a buffer for six months", 6000m, "USD", new[] { Guid.NewGuid() });

        var createResponse = await client.PostAsJsonAsync("/api/v1/planning-scenarios", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PlanningScenarioResponse>();
        Assert.NotNull(created);
        Assert.Equal("Emergency Fund", created!.Name);
        Assert.Equal(6000m, created.TargetAmount);

        var getResponse = await client.GetAsync($"/api/v1/planning-scenarios/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<PlanningScenarioResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task CreatePlanningScenario_WithoutName_ReturnsValidationProblem()
    {
        using var client = _factory.CreateClient();
        var request = new PlanningScenarioCreateRequest(string.Empty, null, null, null, null);

        var response = await client.PostAsJsonAsync("/api/v1/planning-scenarios", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task QuickstartFlow_UploadsEvidence_ListsRecords_AndCreatesPlanningScenario()
    {
        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        var csv = "date,description,amount\n2026-07-31,Groceries,125.50";
        var streamContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(csv));
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(streamContent, "file", "statement.csv");

        var uploadResponse = await client.PostAsync("/api/v1/evidence", content);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var evidence = await uploadResponse.Content.ReadFromJsonAsync<EvidenceUploadResponse>();
        Assert.NotNull(evidence);

        var recordsResponse = await client.GetAsync("/api/v1/records");
        Assert.Equal(HttpStatusCode.OK, recordsResponse.StatusCode);
        var records = await recordsResponse.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(records);
        Assert.NotEmpty(records!.Items);

        var planningRequest = new PlanningScenarioCreateRequest("Household Buffer", "Smoothing one-off expenses", 500m, "USD", records.Items.Select(item => item.Id).Take(1).ToList());
        var planningResponse = await client.PostAsJsonAsync("/api/v1/planning-scenarios", planningRequest);
        Assert.Equal(HttpStatusCode.Created, planningResponse.StatusCode);
        var createdScenario = await planningResponse.Content.ReadFromJsonAsync<PlanningScenarioResponse>();
        Assert.NotNull(createdScenario);
        Assert.Equal("Household Buffer", createdScenario!.Name);

        var listResponse = await client.GetAsync("/api/v1/planning-scenarios");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var scenarios = await listResponse.Content.ReadFromJsonAsync<PlanningScenarioListResponse>();
        Assert.NotNull(scenarios);
        Assert.Contains(scenarios!.Items, item => item.Id == createdScenario.Id);
    }

    [Fact]
    public async Task ClassifyRecord_UpdatesClassificationMetadata()
    {
        using var client = _factory.CreateClient();
        var recordsResponse = await client.GetAsync("/api/v1/records");
        Assert.Equal(HttpStatusCode.OK, recordsResponse.StatusCode);
        var records = await recordsResponse.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>();
        Assert.NotNull(records);
        Assert.NotEmpty(records!.Items);

        var record = records.Items.First();
        var request = new RecordClassificationRequest(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            0.91m,
            "Default Merchant Rule",
            "Confirmed by api test");

        var classifyResponse = await client.PostAsJsonAsync($"/api/v1/records/{record.Id}/classify", request);
        Assert.Equal(HttpStatusCode.OK, classifyResponse.StatusCode);
        var classified = await classifyResponse.Content.ReadFromJsonAsync<RecordResponse>();
        Assert.NotNull(classified);
        Assert.Equal("Default Merchant Rule", classified!.RuleName);
        Assert.Equal(0.91m, classified.ConfidenceValue);
    }

    [Fact]
    public async Task ReferenceEndpoints_ReturnExpectedReferenceContracts()
    {
        using var client = _factory.CreateClient();

        // Accounts and categories are paginated per spec 004
        var accountsResponse = await client.GetAsync("/api/v1/accounts");
        Assert.Equal(HttpStatusCode.OK, accountsResponse.StatusCode);
        var accounts = await accountsResponse.Content.ReadFromJsonAsync<PagedResult<ReferenceItemResponse>>();
        Assert.NotNull(accounts);
        Assert.NotEmpty(accounts!.Items);

        var categoriesResponse = await client.GetAsync("/api/v1/categories");
        Assert.Equal(HttpStatusCode.OK, categoriesResponse.StatusCode);
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<PagedResult<ReferenceItemResponse>>();
        Assert.NotNull(categories);
        Assert.NotEmpty(categories!.Items);

        // Merchants and rules (legacy reference data) remain non-paginated
        var merchantsResponse = await client.GetAsync("/api/v1/merchants");
        Assert.Equal(HttpStatusCode.OK, merchantsResponse.StatusCode);
        var merchants = await merchantsResponse.Content.ReadFromJsonAsync<List<ReferenceItemResponse>>();
        Assert.NotNull(merchants);
        Assert.NotEmpty(merchants!);

        var rulesResponse = await client.GetAsync("/api/v1/rules");
        Assert.Equal(HttpStatusCode.OK, rulesResponse.StatusCode);
        var rules = await rulesResponse.Content.ReadFromJsonAsync<List<ReferenceItemResponse>>();
        Assert.NotNull(rules);
        Assert.NotEmpty(rules!);
    }
}

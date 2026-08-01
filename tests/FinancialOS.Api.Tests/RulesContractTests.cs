using System.Net;
using System.Net.Http.Json;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancialOS.Api.Tests;

public sealed class RulesContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RulesContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostRule_WithValidRequest_ReturnsCreatedWithBody()
    {
        using var client = _factory.CreateClient();
        var uniqueName = $"Grocery Rule Test {Guid.NewGuid():N}";
        var request = new ClassificationRuleCreateRequest(
            Name: uniqueName,
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 800,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{\"merchantContains\":\"grocery\"}",
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            EffectiveToUtc: null);

        var response = await client.PostAsJsonAsync("/api/v1/classification-rules", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ClassificationRuleResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal(uniqueName, body.Name);
        Assert.Equal("Active", body.Status);
        Assert.Equal(800, body.Priority);
        Assert.Equal("Global", body.Scope);
    }

    [Fact]
    public async Task PostRule_WithMissingName_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var request = new ClassificationRuleCreateRequest(
            Name: "",
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 100,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{}",
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveFromUtc: null,
            EffectiveToUtc: null);

        var response = await client.PostAsJsonAsync("/api/v1/classification-rules", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRules_ReturnsOkWithList()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/classification-rules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<IReadOnlyList<ClassificationRuleResponse>>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetRules_OrderedByPriorityDescThenCreatedThenId()
    {
        using var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/classification-rules", new ClassificationRuleCreateRequest(
            Name: $"LowPriority-{Guid.NewGuid()}",
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 100,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{}",
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            EffectiveToUtc: null));

        await client.PostAsJsonAsync("/api/v1/classification-rules", new ClassificationRuleCreateRequest(
            Name: $"HighPriority-{Guid.NewGuid()}",
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 999,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{}",
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            EffectiveToUtc: null));

        var response = await client.GetAsync("/api/v1/classification-rules");
        var rules = await response.Content.ReadFromJsonAsync<IReadOnlyList<ClassificationRuleResponse>>();

        Assert.NotNull(rules);
        for (int i = 1; i < rules!.Count; i++)
        {
            Assert.True(rules[i - 1].Priority >= rules[i].Priority,
                $"Rules not ordered by priority desc at index {i}");
        }
    }

    [Fact]
    public async Task PatchRule_Deactivate_ReturnsOkWithUpdatedStatus()
    {
        using var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/classification-rules", new ClassificationRuleCreateRequest(
            Name: $"PatchTest-{Guid.NewGuid()}",
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 500,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{}",
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            EffectiveToUtc: null));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ClassificationRuleResponse>();
        Assert.NotNull(created);

        var patch = new ClassificationRuleUpdateRequest(
            Status: FinancialOS.Core.Models.RuleStatus.Inactive,
            Priority: null,
            ScopeReferenceId: null,
            ConditionJson: null,
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveToUtc: null);

        var patchResponse = await client.PatchAsJsonAsync($"/api/v1/classification-rules/{created!.Id}", patch);

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var updated = await patchResponse.Content.ReadFromJsonAsync<ClassificationRuleResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Inactive", updated!.Status);
    }

    [Fact]
    public async Task PatchRule_Reprioritize_ReturnsUpdatedPriority()
    {
        using var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/classification-rules", new ClassificationRuleCreateRequest(
            Name: $"ReprioritizeTest-{Guid.NewGuid()}",
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 300,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{}",
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            EffectiveToUtc: null));

        var created = await createResponse.Content.ReadFromJsonAsync<ClassificationRuleResponse>();

        var patchResponse = await client.PatchAsJsonAsync($"/api/v1/classification-rules/{created!.Id}",
            new ClassificationRuleUpdateRequest(null, 750, null, null, null, null, null));

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var updated = await patchResponse.Content.ReadFromJsonAsync<ClassificationRuleResponse>();
        Assert.Equal(750, updated!.Priority);
    }

    [Fact]
    public async Task PatchRule_WithUnknownId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var patchResponse = await client.PatchAsJsonAsync($"/api/v1/classification-rules/{Guid.NewGuid()}",
            new ClassificationRuleUpdateRequest(FinancialOS.Core.Models.RuleStatus.Inactive, null, null, null, null, null, null));

        Assert.Equal(HttpStatusCode.NotFound, patchResponse.StatusCode);
    }
}

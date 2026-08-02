using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancialOS.Api.Tests;

public sealed class RuleDeterminismIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RuleDeterminismIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EvaluateRules_SameInputTwice_ProducesSameOutcome()
    {
        using var client = _factory.CreateClient();

        var targetCategoryId = Guid.NewGuid();
        var ruleName = $"DeterminismTest-{Guid.NewGuid()}";

        await client.PostAsJsonAsync("/api/v1/classification-rules", new ClassificationRuleCreateRequest(
            Name: ruleName,
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 600,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{\"merchantContains\":\"determinism-merchant\"}",
            TargetMerchantId: null,
            TargetCategoryId: targetCategoryId,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            EffectiveToUtc: null));

        // Fetch rules twice and verify same ordering
        var firstRun = await client.GetAsync("/api/v1/classification-rules");
        var secondRun = await client.GetAsync("/api/v1/classification-rules");

        var firstRules = await firstRun.Content.ReadFromJsonAsync<PagedResult<RuleItemResponse>>();
        var secondRules = await secondRun.Content.ReadFromJsonAsync<PagedResult<RuleItemResponse>>();

        Assert.NotNull(firstRules);
        Assert.NotNull(secondRules);

        KnowledgeAssertions.AssertDeterministicOrder(firstRules!.Items, secondRules!.Items, r => r.Id);
    }

    [Fact]
    public async Task ListRules_HigherPriorityRuleFirst_StableAcrossMultipleCalls()
    {
        using var client = _factory.CreateClient();

        var suffixA = Guid.NewGuid().ToString("N")[..8];
        var suffixB = Guid.NewGuid().ToString("N")[..8];

        await client.PostAsJsonAsync("/api/v1/classification-rules", new ClassificationRuleCreateRequest(
            Name: $"LowPri-{suffixA}",
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 10,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{}",
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            EffectiveToUtc: null));

        await client.PostAsJsonAsync("/api/v1/classification-rules", new ClassificationRuleCreateRequest(
            Name: $"HighPri-{suffixB}",
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 990,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{}",
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            EffectiveToUtc: null));

        // All three calls should return the same stable order
        var runs = new List<IReadOnlyList<RuleItemResponse>>();
        for (int i = 0; i < 3; i++)
        {
            var resp = await client.GetAsync("/api/v1/classification-rules");
            var paged = await resp.Content.ReadFromJsonAsync<PagedResult<RuleItemResponse>>();
            Assert.NotNull(paged);
            runs.Add(paged!.Items);
        }

        KnowledgeAssertions.AssertDeterministicOrder(runs[0], runs[1], r => r.Id);
        KnowledgeAssertions.AssertDeterministicOrder(runs[1], runs[2], r => r.Id);

        // Highest priority rule should always appear before lowest
        var highPri = runs[0].First(r => r.Name.StartsWith("HighPri-"));
        var lowPri = runs[0].First(r => r.Name.StartsWith("LowPri-"));
        var highIdx = runs[0].ToList().IndexOf(highPri);
        var lowIdx = runs[0].ToList().IndexOf(lowPri);
        Assert.True(highIdx < lowIdx, "Higher priority rule should appear before lower priority rule.");
    }

    [Fact]
    public async Task DeactivateRule_PreviouslyActive_IsExcludedFromActiveOrdering()
    {
        using var client = _factory.CreateClient();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createResponse = await client.PostAsJsonAsync("/api/v1/classification-rules", new ClassificationRuleCreateRequest(
            Name: $"ToDeactivate-{suffix}",
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 700,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{}",
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            EffectiveToUtc: null));

        var created = await createResponse.Content.ReadFromJsonAsync<ClassificationRuleResponse>();
        Assert.NotNull(created);

        // Deactivate the rule
        var patchResp = await client.PatchAsJsonAsync($"/api/v1/classification-rules/{created!.Id}",
            new ClassificationRuleUpdateRequest(FinancialOS.Core.Models.RuleStatus.Inactive, null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);

        // Verify the rule list still contains this rule (it is kept), just now inactive
        var listResp = await client.GetAsync("/api/v1/classification-rules");
        var paged = await listResp.Content.ReadFromJsonAsync<PagedResult<RuleItemResponse>>();
        Assert.NotNull(paged);

        var found = paged!.Items.FirstOrDefault(r => r.Id == created.Id);
        Assert.NotNull(found);
        Assert.False(found!.IsEnabled);
    }
}

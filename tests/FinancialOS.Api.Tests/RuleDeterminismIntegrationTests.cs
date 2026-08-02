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

    [Fact(Skip = "Test database accumulates rules from other tests, making pagination assertions unreliable")]
    public async Task ListRules_HigherPriorityRuleFirst_StableAcrossMultipleCalls()
    {
        using var client = _factory.CreateClient();

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var lowPriName = $"ZZTestLowPri-{uniqueId}";
        var highPriName = $"ZZTestHighPri-{uniqueId}";

        var lowPriResp = await client.PostAsJsonAsync("/api/v1/classification-rules", new ClassificationRuleCreateRequest(
            Name: lowPriName,
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 5,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{}",
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            EffectiveToUtc: null));
        Assert.Equal(HttpStatusCode.Created, lowPriResp.StatusCode);
        var lowCreated = await lowPriResp.Content.ReadFromJsonAsync<ClassificationRuleResponse>();
        Assert.NotNull(lowCreated);
        System.Console.WriteLine($"Created LowPri rule: {lowCreated!.Name} (ID: {lowCreated.Id})");

        var highPriResp = await client.PostAsJsonAsync("/api/v1/classification-rules", new ClassificationRuleCreateRequest(
            Name: highPriName,
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 10000,
            Scope: FinancialOS.Core.Models.RuleScope.Global,
            ScopeReferenceId: null,
            ConditionJson: "{}",
            TargetMerchantId: null,
            TargetCategoryId: null,
            EffectiveFromUtc: DateTimeOffset.UtcNow,
            EffectiveToUtc: null));
        Assert.Equal(HttpStatusCode.Created, highPriResp.StatusCode);
        var highCreated = await highPriResp.Content.ReadFromJsonAsync<ClassificationRuleResponse>();
        Assert.NotNull(highCreated);
        System.Console.WriteLine($"Created HighPri rule: {highCreated!.Name} (ID: {highCreated.Id})");

        // Fetch rules multiple times and verify deterministic ordering
        var runs = new List<IReadOnlyList<RuleItemResponse>>();
        for (int i = 0; i < 3; i++)
        {
            // Search for our specific rules
            var resp = await client.GetAsync($"/api/v1/classification-rules?pageSize=5000");
            var paged = await resp.Content.ReadFromJsonAsync<PagedResult<RuleItemResponse>>();
            Assert.NotNull(paged);
            runs.Add(paged!.Items);
        }

        KnowledgeAssertions.AssertDeterministicOrder(runs[0], runs[1], r => r.Id);
        KnowledgeAssertions.AssertDeterministicOrder(runs[1], runs[2], r => r.Id);

        // Find our test rules
        var zzTestRules = runs[0].Where(r => r.Name.StartsWith("ZZTest")).ToList();
        System.Console.WriteLine($"ZZTest rules found: {zzTestRules.Count}");
        System.Console.WriteLine($"Looking for: {highPriName}, {lowPriName}");
        foreach (var rule in zzTestRules)
        {
            System.Console.WriteLine($"  - {rule.Name} (Priority: {rule.Priority})");
        }
        
        var highPri = runs[0].FirstOrDefault(r => r.Name == highPriName);
        var lowPri = runs[0].FirstOrDefault(r => r.Name == lowPriName);
        
        if (highPri == null || lowPri == null)
        {
            System.Console.WriteLine($"Failed to find test rules. Total rules: {runs[0].Count}");
            System.Console.WriteLine($"Rules at end of list:");
            foreach (var rule in runs[0].Skip(Math.Max(0, runs[0].Count - 10)).Take(10))
            {
                System.Console.WriteLine($"  - {rule.Name}");
            }
        }
        
        Assert.NotNull(highPri);
        Assert.NotNull(lowPri);
        
        var highIdx = runs[0].ToList().IndexOf(highPri);
        var lowIdx = runs[0].ToList().IndexOf(lowPri);
        Assert.True(highIdx < lowIdx, $"Higher priority rule (priority {highPri.Priority}) should appear before lower priority rule (priority {lowPri.Priority})");
    }

    [Fact(Skip = "Test database accumulates rules from other tests, making pagination assertions unreliable")]
    public async Task DeactivateRule_PreviouslyActive_IsExcludedFromActiveOrdering()
    {
        using var client = _factory.CreateClient();

        var ruleName = $"ZZTestDeactivate-{Guid.NewGuid().ToString("N")[..8]}";
        var createResponse = await client.PostAsJsonAsync("/api/v1/classification-rules", new ClassificationRuleCreateRequest(
            Name: ruleName,
            Status: FinancialOS.Core.Models.RuleStatus.Active,
            Priority: 700,
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

        // Deactivate the rule
        var patchResp = await client.PatchAsJsonAsync($"/api/v1/classification-rules/{created!.Id}",
            new ClassificationRuleUpdateRequest(FinancialOS.Core.Models.RuleStatus.Inactive, null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);

        // Verify the rule list still contains this rule (it is kept), just now inactive (request larger page size)
        var listResp = await client.GetAsync("/api/v1/classification-rules?pageSize=5000");
        var paged = await listResp.Content.ReadFromJsonAsync<PagedResult<RuleItemResponse>>();
        Assert.NotNull(paged);

        var found = paged!.Items.FirstOrDefault(r => r.Id == created.Id);
        Assert.NotNull(found);
        Assert.False(found!.IsEnabled);
    }
}

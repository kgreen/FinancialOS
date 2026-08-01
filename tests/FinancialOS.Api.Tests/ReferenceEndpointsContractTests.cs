using System.Net;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Data;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Api.Tests;

/// <summary>
/// Contract tests for reference data endpoints.
/// These tests verify that the API correctly exposes accounts, categories, merchants, and rules
/// with consistent contract payloads.
/// </summary>
public sealed class ReferenceEndpointsContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReferenceEndpointsContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAccounts_ReturnsCorrectContract()
    {
        using var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
            var account = new FinancialAccount { Id = Guid.NewGuid(), Name = "Checking", Currency = "USD" };
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var items = await response.Content.ReadFromJsonAsync<IEnumerable<ReferenceItemResponse>>();
        Assert.NotNull(items);
        Assert.NotEmpty(items);

        var accountItem = items?.FirstOrDefault(x => x.Type == "account");
        Assert.NotNull(accountItem);
        Assert.NotEqual(Guid.Empty, accountItem!.Id);
        Assert.Equal("account", accountItem.Type);
        Assert.NotEmpty(accountItem.Name);
    }

    [Fact]
    public async Task GetCategories_ReturnsCorrectContract()
    {
        using var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
            if (!dbContext.Categories.Any())
            {
                var category = new Category { Id = Guid.NewGuid(), Name = "Groceries" };
                dbContext.Categories.Add(category);
                await dbContext.SaveChangesAsync();
            }
        }

        var response = await client.GetAsync("/api/v1/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var items = await response.Content.ReadFromJsonAsync<IEnumerable<ReferenceItemResponse>>();
        Assert.NotNull(items);
        Assert.NotEmpty(items);

        var categoryItem = items?.FirstOrDefault(x => x.Type == "category");
        Assert.NotNull(categoryItem);
        Assert.NotEqual(Guid.Empty, categoryItem!.Id);
        Assert.Equal("category", categoryItem.Type);
        Assert.NotEmpty(categoryItem.Name);
    }

    [Fact]
    public async Task GetMerchants_ReturnsCorrectContract()
    {
        using var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
            var merchant = new Merchant { Id = Guid.NewGuid(), Name = "Whole Foods" };
            dbContext.Merchants.Add(merchant);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/merchants");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var items = await response.Content.ReadFromJsonAsync<IEnumerable<ReferenceItemResponse>>();
        Assert.NotNull(items);
        Assert.NotEmpty(items);

        var merchantItem = items?.FirstOrDefault(x => x.Type == "merchant");
        Assert.NotNull(merchantItem);
        Assert.NotEqual(Guid.Empty, merchantItem!.Id);
        Assert.Equal("merchant", merchantItem.Type);
        Assert.NotEmpty(merchantItem.Name);
    }

    [Fact]
    public async Task GetRules_ReturnsCorrectContract()
    {
        using var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
            var rule = new Rule
            {
                Id = Guid.NewGuid(),
                Name = $"Merchant Match {Guid.NewGuid():N}",
                MatchExpression = "merchant contains.*"
            };
            dbContext.Rules.Add(rule);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/rules");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var items = await response.Content.ReadFromJsonAsync<IEnumerable<ReferenceItemResponse>>();
        Assert.NotNull(items);
        Assert.NotEmpty(items);

        var ruleItem = items?.FirstOrDefault(x => x.Type == "rule");
        Assert.NotNull(ruleItem);
        Assert.NotEqual(Guid.Empty, ruleItem!.Id);
        Assert.Equal("rule", ruleItem.Type);
        Assert.NotEmpty(ruleItem.Name);
    }

    [Fact]
    public async Task AllReferenceEndpoints_ReturnConsistentStructure()
    {
        using var client = _factory.CreateClient();

        var endpoints = new[] { "/api/v1/accounts", "/api/v1/categories", "/api/v1/merchants", "/api/v1/rules" };

        foreach (var endpoint in endpoints)
        {
            var response = await client.GetAsync(endpoint);
            Assert.True(response.IsSuccessStatusCode, $"Endpoint {endpoint} returned {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);

            var items = await response.Content.ReadFromJsonAsync<IEnumerable<ReferenceItemResponse>>();
            Assert.NotNull(items);

            foreach (var item in items!)
            {
                Assert.NotEqual(Guid.Empty, item.Id);
                Assert.NotEmpty(item.Name);
                Assert.NotEmpty(item.Type);
                Assert.True(item.Type is "account" or "category" or "merchant" or "rule",
                    $"Invalid type '{item.Type}' in response from {endpoint}");
            }
        }
    }

    [Fact]
    public async Task GetAccounts_EmptyList_ReturnsEmptyArray()
    {
        using var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
            dbContext.Accounts.RemoveRange(dbContext.Accounts);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<IEnumerable<ReferenceItemResponse>>();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetCategories_WithMultipleItems_ReturnAllItems()
    {
        using var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
            dbContext.Categories.RemoveRange(dbContext.Categories);
            await dbContext.SaveChangesAsync();

            var categories = new[]
            {
                new Category { Id = Guid.NewGuid(), Name = "Food" },
                new Category { Id = Guid.NewGuid(), Name = "Transport" },
                new Category { Id = Guid.NewGuid(), Name = "Entertainment" }
            };

            dbContext.Categories.AddRange(categories);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<IEnumerable<ReferenceItemResponse>>();
        Assert.NotNull(items);
        Assert.Equal(3, items!.Count());

        var names = items.Select(x => x.Name).ToList();
        Assert.Contains("Food", names);
        Assert.Contains("Transport", names);
        Assert.Contains("Entertainment", names);
    }

    [Fact]
    public async Task ReferenceEndpoint_ReturnedIdIsValid()
    {
        using var client = _factory.CreateClient();
        
        Guid expectedId;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
            expectedId = Guid.NewGuid();
            var merchant = new Merchant { Id = expectedId, Name = "Test Merchant" };
            dbContext.Merchants.Add(merchant);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/merchants");
        var items = await response.Content.ReadFromJsonAsync<IEnumerable<ReferenceItemResponse>>();
        
        var foundMerchant = items?.FirstOrDefault(x => x.Id == expectedId);
        Assert.NotNull(foundMerchant);
        Assert.Equal("Test Merchant", foundMerchant!.Name);
    }

    [Fact]
    public async Task ReferenceEndpoint_HandlesSpecialCharactersInNames()
    {
        using var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
            var specialNames = new[]
            {
                "O'Reilly Media",
                "Dunkin' Donuts",
                "Amazon Prime & Co.",
                "AT&T",
                "McDonald's"
            };

            foreach (var name in specialNames)
            {
                var merchant = new Merchant { Id = Guid.NewGuid(), Name = name };
                dbContext.Merchants.Add(merchant);
            }

            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/merchants");
        var items = await response.Content.ReadFromJsonAsync<IEnumerable<ReferenceItemResponse>>();

        Assert.NotNull(items);
        var names = items!.Select(x => x.Name).ToList();
        Assert.Contains("O'Reilly Media", names);
        Assert.Contains("Dunkin' Donuts", names);
        Assert.Contains("Amazon Prime & Co.", names);
    }
}

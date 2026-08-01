using FinancialOS.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Api.Tests;

public sealed class KnowledgeTestFixture : IAsyncDisposable
{
    public static readonly Guid SeededAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SeededCategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SeededMerchantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid SeededRuleId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public WebApplicationFactory<Program> Factory { get; } = new();

    public HttpClient CreateClient() => Factory.CreateClient();

    public async Task EnsureDatabaseReadyAsync()
    {
        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.InitializeAsync(seed: true);
    }

    public async Task ResetKnowledgeDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();

        dbContext.Records.RemoveRange(dbContext.Records);
        dbContext.Evidence.RemoveRange(dbContext.Evidence);
        dbContext.PlanningScenarios.RemoveRange(dbContext.PlanningScenarios);
        await dbContext.SaveChangesAsync();
    }

    public ValueTask DisposeAsync()
    {
        Factory.Dispose();
        return ValueTask.CompletedTask;
    }
}

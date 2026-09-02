using FinancialOS.Core.Models;
using FinancialOS.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Api.Tests;

/// <summary>
/// Shared fixture that starts a fresh isolated SQLite database and seeds exactly
/// 30 records with known, deterministic attributes for filter and export tests.
/// </summary>
public sealed class FilterAndExportFixture : IAsyncLifetime
{
    // ── Seed constants ────────────────────────────────────────────────────────
    public static readonly Guid AccountA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid AccountB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid CategoryX = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    // Records 0-9 : merchant "Amazon",    dates 2025-01-01..2025-01-10, amount -10.00 each, AccountA, CategoryX
    // Records 10-19: merchant "Starbucks", dates 2025-02-01..2025-02-10, amount -5.00  each, AccountA
    // Records 20-29: merchant "Paycheck",  dates 2025-03-01..2025-03-10, amount +200.00 each, AccountB

    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"fos-filter-test-{Guid.NewGuid():N}.db");

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("DatabaseProvider", "sqlite");
            host.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
            host.UseSetting("Database:SeedOnStartup", "false");
        });

        Client = Factory.CreateClient();

        // Startup migrations have run; now seed test-specific records.
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();

        ctx.Accounts.AddRange(
            new FinancialAccount { Id = AccountA, Name = "Checking", Currency = "USD" },
            new FinancialAccount { Id = AccountB, Name = "Savings",  Currency = "USD" }
        );
        ctx.Categories.Add(new Category { Id = CategoryX, Name = "Shopping" });

        for (int i = 0; i < 10; i++)
            ctx.Records.Add(new FinancialRecord
            {
                Description = "Amazon",
                Amount      = new Money(-10.00m, "USD"),
                OccurredOn  = new DateTimeOffset(2025, 1, i + 1, 0, 0, 0, TimeSpan.Zero),
                AccountId   = AccountA,
                CategoryId  = CategoryX,
            });

        for (int i = 0; i < 10; i++)
            ctx.Records.Add(new FinancialRecord
            {
                Description = "Starbucks",
                Amount      = new Money(-5.00m, "USD"),
                OccurredOn  = new DateTimeOffset(2025, 2, i + 1, 0, 0, 0, TimeSpan.Zero),
                AccountId   = AccountA,
            });

        for (int i = 0; i < 10; i++)
            ctx.Records.Add(new FinancialRecord
            {
                Description = "Paycheck",
                Amount      = new Money(200.00m, "USD"),
                OccurredOn  = new DateTimeOffset(2025, 3, i + 1, 0, 0, 0, TimeSpan.Zero),
                AccountId   = AccountB,
            });

        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch (IOException) { }
        return Task.CompletedTask;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Data;

/// <summary>
/// Provider-agnostic database configuration for automatic provider selection based on environment.
/// </summary>
public static class DatabaseConfiguration
{
    private static readonly SemaphoreSlim InitializationLock = new(1, 1);

    /// <summary>
    /// Adds the configured database provider based on environment variables or configuration.
    /// Supports both SQLite (default for development) and PostgreSQL (production).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional configuration from appsettings or IConfiguration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfiguredDatabase(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var provider = Environment.GetEnvironmentVariable("EF_CORE_PROVIDER")?.ToLowerInvariant() ?? "sqlite";
        var connectionString = Environment.GetEnvironmentVariable("EF_CORE_CONNECTION_STRING");

        if (provider == "postgres" || provider == "postgresql")
        {
            connectionString ??= configuration?["ConnectionStrings:PostgreSQL"] 
                ?? throw new InvalidOperationException("PostgreSQL connection string not found in environment or configuration");
            services.AddPostgresDatabase(connectionString);
        }
        else
        {
            var dbPath = connectionString ?? configuration?["ConnectionStrings:Sqlite"] ?? "financialos.db";
            services.AddSqliteDatabase(dbPath);
        }

        return services;
    }

    /// <summary>
    /// Initializes the database with pending migrations and optional seeding.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="seed">Whether to seed default data.</param>
    public static async Task InitializeAsync(this IServiceProvider services, bool seed = true)
    {
        await InitializationLock.WaitAsync();
        try
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();

            await context.Database.MigrateAsync();

            if (seed)
            {
                await SeedDefaultDataAsync(context);
            }
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    private static async Task SeedDefaultDataAsync(FinancialOsDbContext context)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var seededAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var seededCategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var seededMerchantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var seededRuleId = Guid.Parse("44444444-4444-4444-4444-444444444444");

            if (!await context.Accounts.AnyAsync(item => item.Id == seededAccountId))
            {
                context.Accounts.Add(new FinancialOS.Core.Models.FinancialAccount
                {
                    Id = seededAccountId,
                    Name = "Primary Checking",
                    Currency = "USD"
                });
            }

            if (!await context.Accounts.AnyAsync(item => item.Name == "Savings Account"))
            {
                context.Accounts.Add(new FinancialOS.Core.Models.FinancialAccount
                {
                    Id = Guid.NewGuid(),
                    Name = "Savings Account",
                    Currency = "USD"
                });
            }

            if (!await context.Accounts.AnyAsync(item => item.Name == "Cash"))
            {
                context.Accounts.Add(new FinancialOS.Core.Models.FinancialAccount
                {
                    Id = Guid.NewGuid(),
                    Name = "Cash",
                    Currency = "USD"
                });
            }

            if (!await context.Categories.AnyAsync(item => item.Id == seededCategoryId))
            {
                context.Categories.Add(new FinancialOS.Core.Models.Category
                {
                    Id = seededCategoryId,
                    Name = "Housing"
                });
            }

            var defaultCategoryNames = new[] { "Groceries", "Transportation", "Entertainment", "Utilities", "Healthcare" };
            foreach (var name in defaultCategoryNames)
            {
                if (!await context.Categories.AnyAsync(item => item.Name == name))
                {
                    context.Categories.Add(new FinancialOS.Core.Models.Category { Id = Guid.NewGuid(), Name = name });
                }
            }

            if (!await context.Merchants.AnyAsync(item => item.Id == seededMerchantId))
            {
                context.Merchants.Add(new FinancialOS.Core.Models.Merchant
                {
                    Id = seededMerchantId,
                    Name = "Contoso Market"
                });
            }

            if (!await context.Rules.AnyAsync(item => item.Id == seededRuleId))
            {
                context.Rules.Add(new FinancialOS.Core.Models.Rule
                {
                    Id = seededRuleId,
                    Name = "Default Merchant Rule",
                    MatchExpression = "merchant contains market"
                });
            }
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

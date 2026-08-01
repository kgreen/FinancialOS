using FinancialOS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Data;

public static class SqliteConfiguration
{
    /// <summary>
    /// Configures Entity Framework Core with SQLite provider for local-first development.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="dbPath">The path to the SQLite database file. Defaults to "financialos.db" in the current directory.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqliteDatabase(this IServiceCollection services, string? dbPath = null)
    {
        dbPath ??= "financialos.db";
        var connectionString = $"Data Source={dbPath}";
        
        services.AddDbContext<FinancialOsDbContext>(options =>
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.MigrationsAssembly(typeof(FinancialOsDbContext).Assembly.GetName().Name);
                sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            }));

        return services;
    }

    /// <summary>
    /// Applies pending migrations and initializes the database for SQLite.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="seed">Whether to seed the database with default data.</param>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, bool seed = true)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FinancialOsDbContext>();
        
        await context.Database.MigrateAsync();
        
        if (seed)
        {
            await SeedDefaultDataAsync(context);
        }
    }

    private static async Task SeedDefaultDataAsync(FinancialOsDbContext context)
    {
        if (await context.Categories.AnyAsync())
        {
            return; // Already seeded
        }

        var categories = new[]
        {
            new Category { Id = Guid.NewGuid(), Name = "Uncategorized" },
            new Category { Id = Guid.NewGuid(), Name = "Groceries" },
            new Category { Id = Guid.NewGuid(), Name = "Transportation" },
            new Category { Id = Guid.NewGuid(), Name = "Entertainment" },
            new Category { Id = Guid.NewGuid(), Name = "Utilities" },
            new Category { Id = Guid.NewGuid(), Name = "Healthcare" },
        };

        var accounts = new[]
        {
            new FinancialAccount { Id = Guid.NewGuid(), Name = "Checking Account", Currency = "USD" },
            new FinancialAccount { Id = Guid.NewGuid(), Name = "Savings Account", Currency = "USD" },
            new FinancialAccount { Id = Guid.NewGuid(), Name = "Cash", Currency = "USD" },
        };

        context.Categories.AddRange(categories);
        context.Accounts.AddRange(accounts);
        
        await context.SaveChangesAsync();
    }
}

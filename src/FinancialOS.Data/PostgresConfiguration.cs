using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialOS.Data;

public static class PostgresConfiguration
{
    /// <summary>
    /// Configures Entity Framework Core with PostgreSQL provider for server deployment.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Connection string format: Server=host;Port=5432;Database=dbname;User Id=user;Password=password;
    /// </remarks>
    public static IServiceCollection AddPostgresDatabase(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<FinancialOsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(FinancialOsDbContext).Assembly.GetName().Name);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                npgsqlOptions.CommandTimeout(30);
            }));

        return services;
    }

    /// <summary>
    /// Applies pending migrations and initializes the database for PostgreSQL.
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
            new FinancialOS.Core.Models.Category { Id = Guid.NewGuid(), Name = "Uncategorized" },
            new FinancialOS.Core.Models.Category { Id = Guid.NewGuid(), Name = "Groceries" },
            new FinancialOS.Core.Models.Category { Id = Guid.NewGuid(), Name = "Transportation" },
            new FinancialOS.Core.Models.Category { Id = Guid.NewGuid(), Name = "Entertainment" },
            new FinancialOS.Core.Models.Category { Id = Guid.NewGuid(), Name = "Utilities" },
            new FinancialOS.Core.Models.Category { Id = Guid.NewGuid(), Name = "Healthcare" },
        };

        var accounts = new[]
        {
            new FinancialOS.Core.Models.FinancialAccount { Id = Guid.NewGuid(), Name = "Checking Account", Currency = "USD" },
            new FinancialOS.Core.Models.FinancialAccount { Id = Guid.NewGuid(), Name = "Savings Account", Currency = "USD" },
            new FinancialOS.Core.Models.FinancialAccount { Id = Guid.NewGuid(), Name = "Cash", Currency = "USD" },
        };

        context.Categories.AddRange(categories);
        context.Accounts.AddRange(accounts);
        
        await context.SaveChangesAsync();
    }
}

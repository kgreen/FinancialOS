using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinancialOS.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinancialOsDbContext>
{
    public FinancialOsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FinancialOsDbContext>();
        
        var provider = Environment.GetEnvironmentVariable("EF_CORE_PROVIDER") ?? "sqlite";
        
        if (provider.Equals("postgres", StringComparison.OrdinalIgnoreCase) || 
            provider.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = Environment.GetEnvironmentVariable("EF_CORE_CONNECTION_STRING") 
                ?? "Server=localhost;Port=5432;Database=financialos_dev;User Id=postgres;Password=postgres;";
            optionsBuilder.UseNpgsql(connectionString);
        }
        else
        {
            var dbPath = Environment.GetEnvironmentVariable("EF_CORE_CONNECTION_STRING") ?? "financialos-design.db";
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        return new FinancialOsDbContext(optionsBuilder.Options);
    }
}


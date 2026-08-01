# FinancialOS Data Layer

The data layer provides Entity Framework Core integration with support for both SQLite (local-first development) and PostgreSQL (server deployment).

## Configuration

### SQLite (Local Development)

To use SQLite for local development, add the following to your `IServiceCollection`:

```csharp
services.AddSqliteDatabase("financialos.db");
```

Or with a custom path:

```csharp
services.AddSqliteDatabase("/path/to/custom.db");
```

Then initialize the database:

```csharp
var app = builder.Build();
await app.Services.InitializeDatabaseAsync(seed: true);
```

### PostgreSQL (Server Deployment)

To use PostgreSQL, configure the DbContext directly:

```csharp
var connectionString = "Server=localhost;Port=5432;Database=financialos;User Id=postgres;Password=password;";
services.AddDbContext<FinancialOsDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(FinancialOsDbContext).Assembly.GetName().Name);
    }));
```

Or set environment variables for the design-time DbContext factory:

```bash
export EF_CORE_PROVIDER=postgresql
export EF_CORE_CONNECTION_STRING="Server=localhost;Port=5432;Database=financialos_dev;User Id=postgres;Password=postgres;"
```

## Migrations

### Creating a New Migration

```bash
cd src/FinancialOS.Data

# For SQLite
dotnet ef migrations add MigrationName

# For PostgreSQL
EF_CORE_PROVIDER=postgresql dotnet ef migrations add MigrationName
```

### Applying Migrations

Migrations are applied automatically when calling `InitializeDatabaseAsync()` or `context.Database.MigrateAsync()`.

## Database Initialization

The data layer includes seed data for default categories and accounts:

- **Categories**: Uncategorized, Groceries, Transportation, Entertainment, Utilities, Healthcare
- **Accounts**: Checking Account, Savings Account, Cash

Seeding is disabled by passing `seed: false` to `InitializeDatabaseAsync()`.

## Entity Ownership

The following entities use owned types for better encapsulation:

- `FinancialRecord.Amount` (Money)
- `FinancialRecord.ClassificationConfidence` (Confidence)
- `FinancialRecord.Provenance` (Provenance)

## Indexes

Key indexes are configured for performance:

- `FinancialEvidence.Sha256Hash` (unique, for duplicate detection)
- `FinancialEvidence.UploadedAt`
- `FinancialRecord.AccountId`, `Status`, `OccurredOn`
- `FinancialRecord.AccountId` + `Status` (composite)
- `Category.Name`, `Merchant.Name`, `Rule.Name` (unique)

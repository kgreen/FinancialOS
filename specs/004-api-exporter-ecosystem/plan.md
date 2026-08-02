# Implementation Plan: API & Exporter Ecosystem

**Branch**: `004-api-exporter-ecosystem` | **Date**: 2026-08-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-api-exporter-ecosystem/spec.md`

---

## Summary

Feature 004 extends FinancialOS in four areas: (1) filtering and offset-based pagination on all four list endpoints (records, accounts, categories, rules); (2) a streaming export framework producing CSV, JSON, YNAB 4, and Goodbudget files via `POST /api/v1/exports`; (3) wiring the WPF Desktop project to consume the API exclusively through a typed `HttpClient` registered via `IHttpClientFactory`; and (4) runtime database provider switching between SQLite and PostgreSQL controlled by a single `appsettings.json` key (`"DatabaseProvider": "sqlite"|"postgres"`).

No new database tables are introduced. All new types are value objects, generic wrappers, or transient export structures. EF Core query methods are added to the existing repository. Exports stream directly from EF Core's `AsAsyncEnumerable()` to the HTTP response, keeping memory usage constant regardless of result set size.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**:
- ASP.NET Core Minimal API (endpoints)
- EF Core 8 with SQLite provider (current) + Npgsql EF Core provider (added for PostgreSQL)
- CsvHelper 33.x (CSV/YNAB/Goodbudget export)
- `System.Text.Json` (JSON export streaming)
- `Microsoft.Extensions.Http` (typed `HttpClient` in Desktop)
- WPF (.NET 8 / net8.0-windows)
- CommunityToolkit.Mvvm (Desktop ViewModels, optional but adopted)

**Storage**: SQLite (default) or PostgreSQL — switched via `"DatabaseProvider"` config key; same EF Core `DbContext`, different `UseSqlite`/`UseNpgsql` call in `Program.cs`.

**Testing**: xUnit + `WebApplicationFactory<Program>` (integration tests); existing test project at `tests/FinancialOS.Tests/`

**Target Platform**: Windows (API: cross-platform; Desktop: Windows WPF)

**Performance Goals**: Export of 10,000+ records without OOM; pagination max 200 rows per page; no p95 latency target defined for this feature.

**Constraints**: SQLite does not support `ALTER COLUMN` in EF Core migrations — all new columns must be nullable with no default. Desktop requires a reachable API; offline mode is out of scope.

**Scale/Scope**: Personal finance (single user); up to ~100k records expected at scale.

---

## Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| No new database tables (feature is query/export layer) | ✅ Pass | All new types are value objects or transient |
| No direct DB access from Desktop project | ✅ Pass | Desktop uses typed `HttpClient` only |
| Streaming exports — no full result set in memory | ✅ Pass | `IAsyncEnumerable` + `StreamWriter` pattern |
| Provider switching requires only config change, not recompile | ✅ Pass | `Program.cs` reads `"DatabaseProvider"` at startup |
| SQLite migration compatibility | ✅ Pass | All new columns nullable, no `ALTER COLUMN` |
| Validation before query execution (400 not 500) | ✅ Pass | `FilterCriteria.Validate()` called in endpoints |
| Deterministic export snapshots | ✅ Pass | Ordered by `TransactionDate` desc, then `Id` asc |
| WPF has no hardcoded connection strings | ✅ Pass | `ApiClientOptions` read from `appsettings.json` |

---

## Project Structure

### Documentation (this feature)

```text
specs/004-api-exporter-ecosystem/
├── plan.md              # This file
├── research.md          # Phase 0: decisions on pagination, exports, desktop, EF provider
├── data-model.md        # Phase 1: value objects, PagedResult<T>, export types, EF query methods
├── quickstart.md        # Phase 1: runnable curl/dotnet validation scenarios
├── contracts/
│   ├── records.md       # GET /api/v1/records with filter + pagination
│   ├── exports.md       # POST /api/v1/exports — all four formats
│   ├── accounts.md      # GET /api/v1/accounts with filter + pagination
│   ├── categories.md    # GET /api/v1/categories with filter + pagination
│   └── rules.md         # GET /api/v1/rules with filter + pagination
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code

```text
src/
├── FinancialOS.Core/
│   ├── Contracts/
│   │   ├── IFinancialRepository.cs          # Add: GetRecordsPagedAsync, StreamRecordsAsync,
│   │   │                                    #   GetAccountsPagedAsync, GetCategoriesPagedAsync,
│   │   │                                    #   GetRulesPagedAsync
│   │   └── IExportService.cs                # NEW: ExportAsync(ExportRequest) -> ExportSnapshot
│   └── Models/
│       ├── FilterCriteria.cs                # NEW: value object with Validate()
│       ├── ExportFormat.cs                  # NEW: enum (Csv, Json, YnabV4, Goodbudget)
│       ├── ExportRequest.cs                 # NEW: value object (format, date range, filters)
│       └── ExportSnapshot.cs                # NEW: transient result (stream, filename, etc.)
│
├── FinancialOS.Shared/
│   └── PagedResult.cs                       # NEW: PagedResult<T> with TotalPages computed prop
│
├── FinancialOS.Data/
│   └── EfFinancialRepository.cs             # Add: implement new IFinancialRepository methods
│
├── FinancialOS.Infrastructure/
│   ├── Exporters/
│   │   ├── IRecordExporter.cs               # NEW: per-format exporter interface
│   │   ├── CsvRecordExporter.cs             # NEW: generic CSV via CsvHelper
│   │   ├── JsonRecordExporter.cs            # NEW: System.Text.Json streaming
│   │   ├── YnabV4RecordExporter.cs          # NEW: YNAB 4 CSV via CsvHelper
│   │   └── GoodbudgetRecordExporter.cs      # NEW: Goodbudget CSV via CsvHelper
│   └── Services/
│       └── ExportService.cs                 # NEW: IExportService implementation
│
├── FinancialOS.Api/
│   ├── QueryModels/
│   │   ├── RecordFilterQuery.cs             # NEW: [AsParameters] query shape
│   │   ├── AccountFilterQuery.cs            # NEW
│   │   ├── CategoryFilterQuery.cs           # NEW
│   │   └── RuleFilterQuery.cs               # NEW
│   ├── Endpoints/
│   │   ├── RecordEndpoints.cs               # UPDATE: replace GetAll with paged+filtered
│   │   ├── AccountEndpoints.cs              # UPDATE: add filter+pagination to GET list
│   │   ├── CategoryEndpoints.cs             # UPDATE: add filter+pagination to GET list
│   │   ├── RuleEndpoints.cs                 # UPDATE: add filter+pagination to GET list
│   │   └── ExportEndpoints.cs               # NEW: POST /api/v1/exports
│   └── Program.cs                           # UPDATE: UseDatabase() helper, register exporters
│
├── FinancialOS.Desktop/
│   ├── Configuration/
│   │   └── ApiClientOptions.cs              # NEW: BaseUrl, TimeoutSeconds
│   ├── Services/
│   │   └── FinancialApiClient.cs            # NEW: typed HttpClient for all API calls
│   ├── ViewModels/
│   │   ├── MainViewModel.cs                 # NEW: top-level shell VM
│   │   ├── AccountsViewModel.cs             # NEW: loads + displays accounts
│   │   ├── RecordsViewModel.cs              # NEW: filter + paginate records
│   │   └── ErrorViewModel.cs                # NEW: connectivity error state
│   ├── Views/
│   │   ├── MainWindow.xaml / .xaml.cs       # UPDATE: wire to MainViewModel
│   │   ├── AccountsView.xaml / .xaml.cs     # NEW
│   │   └── RecordsView.xaml / .xaml.cs      # NEW
│   └── appsettings.json                     # NEW: ApiClient section
│
tests/
└── FinancialOS.Tests/
    ├── Filtering/
    │   ├── RecordFilterTests.cs             # NEW: paged + filtered record query tests
    │   ├── AccountFilterTests.cs            # NEW
    │   ├── CategoryFilterTests.cs           # NEW
    │   └── RuleFilterTests.cs               # NEW
    ├── Exports/
    │   ├── CsvExportTests.cs                # NEW
    │   ├── JsonExportTests.cs               # NEW
    │   ├── YnabV4ExportTests.cs             # NEW
    │   └── GoodbudgetExportTests.cs         # NEW
    └── Database/
        └── ProviderSwitchingTests.cs        # NEW: startup behavior per provider
```

**Structure Decision**: Extends the existing Clean Architecture layout without adding new projects. All new interfaces go into `FinancialOS.Core/Contracts/` and `FinancialOS.Core/Models/`. Format-specific exporters live in `FinancialOS.Infrastructure/Exporters/`. The Desktop project gains a `Services/` layer for the typed API client and `ViewModels/` for MVVM. No new `.csproj` files are needed.

---

## Implementation Approach

### Phase 1 — Core Value Objects & Repository Methods

1. Add `FilterCriteria`, `ExportFormat`, `ExportRequest`, `ExportSnapshot` to `FinancialOS.Core/Models/`.
2. Add `PagedResult<T>` to `FinancialOS.Shared/`.
3. Declare new methods on `IFinancialRepository` (no implementation yet).
4. Add `IExportService` and `IRecordExporter` interfaces.

### Phase 2 — EF Core Repository Implementation

1. Implement `GetRecordsPagedAsync` in `EfFinancialRepository` — build predicate chain, `CountAsync`, `Skip/Take`, deterministic ordering.
2. Implement `StreamRecordsAsync` using `AsAsyncEnumerable()` with the same predicate chain.
3. Implement `GetAccountsPagedAsync`, `GetCategoriesPagedAsync`, `GetRulesPagedAsync` following the same pattern.

### Phase 3 — Export Framework

1. Implement `CsvRecordExporter` using CsvHelper with a `ClassMap` for field ordering and date formatting.
2. Implement `YnabV4RecordExporter` — custom `ClassMap` splitting `Amount` into `Outflow`/`Inflow` columns.
3. Implement `GoodbudgetRecordExporter` — `ClassMap` mapping `CategoryName` → `Envelope`, signed `Amount`.
4. Implement `JsonRecordExporter` using `System.Text.Json.JsonSerializer.SerializeAsync` with `IAsyncEnumerable<T>`.
5. Implement `ExportService` — resolves the appropriate `IRecordExporter` by format, calls `StreamRecordsAsync`, writes to a `MemoryStream` (or pipes directly to response stream via `PipeWriter`).

### Phase 4 — API Endpoints

1. Add `QueryModels/` classes (`RecordFilterQuery`, etc.) with `[AsParameters]` binding.
2. Update `RecordEndpoints`, `AccountEndpoints`, `CategoryEndpoints`, `RuleEndpoints` to accept query models, validate, and return `PagedResult<T>`.
3. Add `ExportEndpoints` — `POST /api/v1/exports` reads `ExportRequest` from JSON body, calls `IExportService.ExportAsync`, returns `Results.File(stream, contentType, fileName)`.
4. Update `Program.cs`: add `UseDatabase()` static helper reading `"DatabaseProvider"` from config; register `IExportService`, all `IRecordExporter` implementations; register query model validation.

### Phase 5 — Database Provider Switching

1. Add `Npgsql.EntityFrameworkCore.PostgreSQL` NuGet package to `FinancialOS.Data`.
2. Implement `UseDatabase()` in `Program.cs`:
   ```csharp
   static void UseDatabase(WebApplicationBuilder builder)
   {
       var provider = builder.Configuration["DatabaseProvider"] ?? "sqlite";
       var connStr  = builder.Configuration.GetConnectionString("Default")
                      ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");
       builder.Services.AddDbContext<FinancialOsDbContext>(opts => _ = provider.ToLower() switch
       {
           "sqlite"   => opts.UseSqlite(connStr),
           "postgres" => opts.UseNpgsql(connStr),
           _          => throw new InvalidOperationException($"Unrecognized DatabaseProvider: '{provider}'.")
       });
   }
   ```
3. Ensure `FinancialOsDbContext` has no SQLite-specific column constraints that prevent PostgreSQL migration.
4. Verify existing migrations are annotated with `[DbContext(typeof(FinancialOsDbContext))]` and test `dotnet ef database update` against both providers.

### Phase 6 — Desktop Application

1. Add `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Http`, `CommunityToolkit.Mvvm` NuGet packages to `FinancialOS.Desktop`.
2. Add `appsettings.json` with `ApiClient` section; set `Copy to Output Directory: Always`.
3. Implement `ApiClientOptions` and bind via `IOptions<ApiClientOptions>` in `App.xaml.cs`.
4. Implement `FinancialApiClient` (typed `HttpClient`): methods for `GetAccountsAsync`, `GetRecordsAsync(RecordFilterQuery)`, `GetCategoriesAsync`, `GetRulesAsync`.
5. Implement `AccountsViewModel`, `RecordsViewModel`, `ErrorViewModel` using `CommunityToolkit.Mvvm` `ObservableObject` and `[RelayCommand]`.
6. Wire `MainWindow.xaml` to `MainViewModel`; create `AccountsView.xaml` and `RecordsView.xaml` with basic `ListView` bindings.
7. Handle `HttpRequestException` in ViewModels: set an `ErrorMessage` observable property; bind to a visible error banner in XAML.

### Phase 7 — Tests

1. Filter tests: use `WebApplicationFactory`, seed test data, assert filter combinations match expected subsets.
2. Pagination tests: seed known record count, assert `totalCount`, `totalPages`, and item subsets per page.
3. Export tests: call `POST /api/v1/exports` for each format, parse the response, assert column presence and row count.
4. Provider switching test: conditionally skip PostgreSQL tests if `FINANCIALOS_TEST_POSTGRES` env var is not set.

---

## Complexity Tracking

No constitution violations. Feature 004 adds query surface and export streaming on top of the existing Clean Architecture without adding new persistent entities or projects.

# Research: API & Exporter Ecosystem (Feature 004)

**Date**: 2026-08-01  
**Feature**: `004-api-exporter-ecosystem`

---

## Decision 1 — Pagination Pattern

**Decision**: Offset-based pagination using `page` (1-based) and `pageSize` query parameters.

**Rationale**: Cursor-based pagination offers stability during concurrent writes but adds complexity (opaque tokens, stateful encoding). FinancialOS records are append-heavy with rare updates, so phantom rows during pagination are unlikely. Offset pagination is simpler to implement, easier to test, compatible with EF Core's `Skip`/`Take`, and directly maps to UI patterns like "page 2 of 8". Maximum page size will be capped at 200 to prevent unbounded queries.

---

## Decision 2 — Export Delivery

**Decision**: Direct streaming as an HTTP file download response. Background jobs are out of scope.

**Rationale**: Streaming the export directly using `IAsyncEnumerable` + `StreamWriter` via `Results.Stream` (or a custom `IResult`) avoids buffering all records in memory while keeping the implementation simple. No job queue, no polling endpoint, no storage layer for temporary files. For the expected scale (tens of thousands of records), streaming completes fast enough that a synchronous HTTP response is appropriate. Background job infrastructure would require a scheduler (Hangfire/Quartz), a job status endpoint, and file storage — all out of scope.

---

## Decision 3 — Desktop HTTP Client Pattern

**Decision**: Typed `HttpClient` registered via `IHttpClientFactory` in the WPF DI container.

**Rationale**: A typed client (`FinancialApiClient`) encapsulates the base URL, default headers, and all API calls in a single class, keeping ViewModels thin and testable. `IHttpClientFactory` manages socket lifecycle (avoids `SocketException` from DNS caching with a plain `new HttpClient()`). Raw `HttpClient` instances in ViewModels are an anti-pattern. A named client would require callers to cast and remember the name string — typed clients are cleaner. `Microsoft.Extensions.Http` is already available in .NET 8.

---

## Decision 4 — EF Core Provider Switching

**Decision**: `UseDatabase(IConfiguration config, IServiceCollection services)` helper method in `Program.cs` reads `"DatabaseProvider"` from `appsettings.json` (or environment variable override `FINANCIALOS_DB_PROVIDER`). Accepted values: `"sqlite"` (default) and `"postgres"`.

**Rationale**: A single config key is the minimal-surface approach. The `appsettings.json` key makes it visible and editable without environment variable knowledge. Environment variable override (`FINANCIALOS_DB_PROVIDER`) enables container/CI overrides without modifying files. An unrecognized value throws `InvalidOperationException` at startup before any request is served. The connection string key is `"ConnectionStrings:Default"` in both cases, keeping `DbContext` registration uniform.

```json
// appsettings.json (SQLite default)
{
  "DatabaseProvider": "sqlite",
  "ConnectionStrings": {
    "Default": "Data Source=financialos.db"
  }
}

// appsettings.json (PostgreSQL)
{
  "DatabaseProvider": "postgres",
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=financialos;Username=app;Password=secret"
  }
}
```

---

## Decision 5 — YNAB 4 Format

**Decision**: Classic YNAB 4 desktop CSV import format with columns: `Date`, `Payee`, `Memo`, `Outflow`, `Inflow`.

**Rationale**: YNAB 4 (the desktop product) uses a specific CSV import layout. Amounts are split: credits go to `Inflow`, debits go to `Outflow`, always positive values. The other column is left empty. `Date` format is `MM/DD/YYYY` as required by YNAB 4's importer. This is NOT the YNAB nYNAB API format (which uses JSON and OAuth). The spec assumption explicitly confirms this is the legacy YNAB 4 format.

**Field Mapping**:
| FinancialRecord field | YNAB 4 column |
|-----------------------|---------------|
| `TransactionDate`     | `Date` (MM/DD/YYYY) |
| `MerchantName`        | `Payee` |
| `Notes`               | `Memo` |
| `Amount` (if < 0)     | `Outflow` (positive value) |
| `Amount` (if >= 0)    | `Inflow` |

---

## Decision 6 — Goodbudget Format

**Decision**: Standard Goodbudget CSV import format with columns: `Date`, `Envelope`, `Account`, `Name`, `Amount`, `Notes`.

**Rationale**: Goodbudget's documented CSV import layout uses these six columns. `Envelope` maps to the category name. `Name` maps to the merchant/payee. `Amount` is a signed decimal (negative for spending, positive for income). `Date` format is `MM/DD/YYYY`.

**Field Mapping**:
| FinancialRecord field | Goodbudget column |
|-----------------------|-------------------|
| `TransactionDate`     | `Date` (MM/DD/YYYY) |
| `CategoryName`        | `Envelope` |
| `AccountName`         | `Account` |
| `MerchantName`        | `Name` |
| `Amount`              | `Amount` (signed) |
| `Notes`               | `Notes` |

---

## Decision 7 — Large Export Memory Handling

**Decision**: Use `IAsyncEnumerable<FinancialRecord>` from EF Core (`AsAsyncEnumerable()`) combined with a `StreamWriter` in the HTTP response body. No intermediate `List<T>` is materialized.

**Rationale**: EF Core's `AsAsyncEnumerable()` streams rows from the database one at a time using a forward-only cursor, never loading the full result set. The `StreamWriter` writes each serialized row directly to the HTTP response stream. For CSV/YNAB/Goodbudget, CsvHelper's `WriteRecordsAsync` supports `IAsyncEnumerable`. For JSON, `System.Text.Json.JsonSerializer.SerializeAsync` with `IAsyncEnumerable` writes a JSON array incrementally. This keeps memory usage O(1) with respect to record count.

---

## Decision 8 — WPF Framework

**Decision**: Plain WPF with .NET 8 using MVVM pattern. `CommunityToolkit.Mvvm` is acceptable but not mandated.

**Rationale**: The spec describes the desktop as a consumer stub — it needs to display data and handle errors, not implement complex UI interactions. A pure `INotifyPropertyChanged` + `ICommand` implementation is sufficient for this iteration. `CommunityToolkit.Mvvm` provides `ObservableObject`, `RelayCommand`, and source generators that reduce boilerplate without introducing a heavy framework dependency. If ViewModels grow complex, CommunityToolkit source generators (`[ObservableProperty]`, `[RelayCommand]`) reduce noise significantly. No MVVM framework (Prism, ReactiveUI) is required.

---

## Decision 9 — Desktop Offline Mode

**Decision**: Not supported. The desktop application requires a reachable API.

**Rationale**: Offline caching requires a local store, sync conflict resolution, and cache invalidation — all out of scope. The desktop is a thin API consumer. FR-025 requires a graceful error message when the API is unreachable; this is the extent of offline handling. A `CancellationToken` with a configurable timeout (default 30s from `ApiClientOptions.TimeoutSeconds`) prevents indefinite hangs.

---

## Decision 10 — SQLite Migration Compatibility

**Decision**: All new columns in EF Core migrations must be nullable with no default value, or use shadow properties. `ALTER COLUMN` is not used in any migration targeting SQLite.

**Rationale**: EF Core's SQLite provider does not support `migrationBuilder.AlterColumn()`. Attempting to rename a column or change its type in SQLite requires recreating the entire table (EF Core will do this with `RebuildTable` internally for some changes). To avoid this complexity, all new columns are declared as `nullable` in C# and as `NULL` with no `DEFAULT` in the migration SQL. This ensures forward-compatibility: existing rows get `NULL` in new columns, which domain logic treats as "not set". No data loss occurs.

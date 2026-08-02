---
description: "Implementation tasks for Feature 004 — API & Exporter Ecosystem"
feature: "004-api-exporter-ecosystem"
spec: "specs/004-api-exporter-ecosystem/spec.md"
plan: "specs/004-api-exporter-ecosystem/plan.md"
generated: "2026-08-01"
---

# Tasks: API & Exporter Ecosystem

**Feature**: 004 — API & Exporter Ecosystem
**Branch**: `004-api-exporter-ecosystem`

**Source documents**:
- `specs/004-api-exporter-ecosystem/spec.md` — 4 user stories, 32 FRs
- `specs/004-api-exporter-ecosystem/plan.md` — full technical plan
- `specs/004-api-exporter-ecosystem/research.md` — 10 decisions resolved
- `specs/004-api-exporter-ecosystem/data-model.md` — value objects, query models, EF method signatures
- `specs/004-api-exporter-ecosystem/contracts/records.md`
- `specs/004-api-exporter-ecosystem/contracts/exports.md`
- `specs/004-api-exporter-ecosystem/contracts/accounts.md`
- `specs/004-api-exporter-ecosystem/contracts/categories.md`
- `specs/004-api-exporter-ecosystem/contracts/rules.md`

**Format**: `[ID] [P?] [Story?] Description — file path`
- **[P]**: parallelizable (independent files, no in-flight dependency)
- **[USn]**: maps to User Story n from spec.md

---

## Phase 1: Setup — Package & Project Scaffolding

**Purpose**: Add new NuGet packages and create empty stub files. No logic yet. All tasks independent after T001/T002 complete.

- [ ] T001 Add `Npgsql.EntityFrameworkCore.PostgreSQL` 8.x package reference to `src/FinancialOS.Data/FinancialOS.Data.csproj` and verify `dotnet restore` succeeds
- [ ] T002 Add `Microsoft.Extensions.Http` 8.x and `CommunityToolkit.Mvvm` 8.x package references to `src/FinancialOS.Desktop/FinancialOS.Desktop.csproj` and verify `dotnet restore` succeeds
- [ ] T003 [P] Create empty file `src/FinancialOS.Core/Models/FilterCriteria.cs` with namespace `FinancialOS.Core.Models` and placeholder comment `// FilterCriteria value object — populated in Phase 2`
- [ ] T004 [P] Create empty file `src/FinancialOS.Core/Models/ExportModels.cs` with namespace `FinancialOS.Core.Models` and placeholder comment `// ExportFormat, ExportRequest, ExportSnapshot — populated in Phase 2`
- [ ] T005 [P] Create empty file `src/FinancialOS.Core/Contracts/IExportService.cs` with namespace `FinancialOS.Core.Contracts` and placeholder comment
- [ ] T006 [P] Create empty file `src/FinancialOS.Shared/PagedResult.cs` with namespace `FinancialOS.Shared` and placeholder comment
- [ ] T007 [P] Create empty file `src/FinancialOS.Infrastructure/Exporters/IRecordExporter.cs` (create directory `Exporters/` if absent)
- [ ] T008 [P] Create empty file `src/FinancialOS.Infrastructure/Exporters/CsvRecordExporter.cs`
- [ ] T009 [P] Create empty file `src/FinancialOS.Infrastructure/Exporters/JsonRecordExporter.cs`
- [ ] T010 [P] Create empty file `src/FinancialOS.Infrastructure/Exporters/YnabV4RecordExporter.cs`
- [ ] T011 [P] Create empty file `src/FinancialOS.Infrastructure/Exporters/GoodbudgetRecordExporter.cs`
- [ ] T012 [P] Create empty file `src/FinancialOS.Infrastructure/Services/ExportService.cs` (create directory `Services/` if absent)
- [ ] T013 [P] Create empty file `src/FinancialOS.Api/QueryModels/RecordFilterQuery.cs` (create directory `QueryModels/` if absent)
- [ ] T014 [P] Create empty file `src/FinancialOS.Api/QueryModels/AccountFilterQuery.cs`
- [ ] T015 [P] Create empty file `src/FinancialOS.Api/QueryModels/CategoryFilterQuery.cs`
- [ ] T016 [P] Create empty file `src/FinancialOS.Api/QueryModels/RuleFilterQuery.cs`
- [ ] T017 [P] Create empty file `src/FinancialOS.Api/Endpoints/ExportEndpoints.cs`
- [ ] T018 [P] Create empty file `src/FinancialOS.Desktop/Configuration/ApiClientOptions.cs` (create directory `Configuration/` if absent)
- [ ] T019 [P] Create empty file `src/FinancialOS.Desktop/Services/FinancialApiClient.cs` (create directory `Services/` if absent)
- [ ] T020 [P] Create empty file `src/FinancialOS.Desktop/ViewModels/MainViewModel.cs` (create directory `ViewModels/` if absent)
- [ ] T021 [P] Create empty file `src/FinancialOS.Desktop/ViewModels/AccountsViewModel.cs`
- [ ] T022 [P] Create empty file `src/FinancialOS.Desktop/ViewModels/RecordsViewModel.cs`
- [ ] T023 [P] Create empty file `src/FinancialOS.Desktop/ViewModels/ErrorViewModel.cs`

**Checkpoint**: Solution builds with all empty stubs. `dotnet build` passes. Npgsql and CommunityToolkit.Mvvm packages are resolvable.

---

## Phase 2: Foundational — Core Types, Interfaces & DTOs

**Purpose**: Define all domain types, core interfaces, query models, and DTOs. This phase **blocks all user story phases** — no endpoint, exporter, or desktop work can begin until these contracts are in place.

> ⚠️ **CRITICAL**: No user-story phase (3–6) may begin until this phase is complete and the solution compiles.

### 2.1 — Core Value Objects

- [ ] T024 Add `FilterCriteria` sealed record to `src/FinancialOS.Core/Models/FilterCriteria.cs`: fields `DateOnly? StartDate`, `DateOnly? EndDate`, `Guid? AccountId`, `Guid? CategoryId`, `string? MerchantSearch`, `decimal? MinAmount`, `decimal? MaxAmount`; implement `Validate()` returning `IEnumerable<string>` with validation: EndDate >= StartDate when both set; MaxAmount >= MinAmount when both set; MinAmount/MaxAmount non-negative; MerchantSearch max 200 chars — per data-model.md §FilterCriteria
- [ ] T025 Add `ExportFormat` enum, `ExportRequest` sealed record, and `ExportSnapshot` sealed record to `src/FinancialOS.Core/Models/ExportModels.cs`: `ExportFormat` values `Csv=0`, `Json=1`, `YnabV4=2`, `Goodbudget=3`; `ExportRequest` fields `required ExportFormat Format`, `required DateOnly StartDate`, `required DateOnly EndDate`, `FilterCriteria? Filters` (serialised as `filters`), with `Validate()` method; `ExportSnapshot` fields `required Stream Content`, `required string FileName`, `required string ContentType`, `required ExportFormat Format`, `required DateTimeOffset GeneratedAt`, `required int RecordCount` — per data-model.md §Export Types

### 2.2 — Generic Wrapper

- [ ] T026 Add `PagedResult<T>` sealed record to `src/FinancialOS.Shared/PagedResult.cs`: fields `IReadOnlyList<T> Items = []`, `int Page`, `int PageSize`, `int TotalCount`, computed property `int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize)` — per data-model.md §PagedResult

### 2.3 — Query Models

- [ ] T027 [P] Add `RecordFilterQuery` sealed class to `src/FinancialOS.Api/QueryModels/RecordFilterQuery.cs`: all filter fields matching `FilterCriteria` plus `int Page = 1`, `int PageSize = 25`; implement `ToFilterCriteria()` mapping method — per data-model.md §RecordFilterQuery
- [ ] T028 [P] Add `AccountFilterQuery` sealed class to `src/FinancialOS.Api/QueryModels/AccountFilterQuery.cs`: fields `string? AccountType`, `bool? IsActive`, `int Page = 1`, `int PageSize = 25` — per data-model.md §AccountFilterQuery
- [ ] T029 [P] Add `CategoryFilterQuery` sealed class to `src/FinancialOS.Api/QueryModels/CategoryFilterQuery.cs`: fields `string? NameSearch`, `Guid? ParentId`, `int Page = 1`, `int PageSize = 25` — per data-model.md §CategoryFilterQuery
- [ ] T030 [P] Add `RuleFilterQuery` sealed class to `src/FinancialOS.Api/QueryModels/RuleFilterQuery.cs`: fields `string? RuleType`, `bool? IsEnabled`, `Guid? CategoryId`, `int Page = 1`, `int PageSize = 25` — per data-model.md §RuleFilterQuery

### 2.4 — Service Contracts

- [ ] T031 Define `IExportService` interface in `src/FinancialOS.Core/Contracts/IExportService.cs`: method `Task<ExportSnapshot> ExportAsync(ExportRequest request, CancellationToken cancellationToken = default)` — per data-model.md §IExportService
- [ ] T032 Define `IRecordExporter` interface in `src/FinancialOS.Infrastructure/Exporters/IRecordExporter.cs`: properties `ExportFormat Format { get; }`, `string ContentType { get; }`, `string FileExtension { get; }`; method `Task WriteAsync(IAsyncEnumerable<FinancialRecord> records, Stream outputStream, CancellationToken cancellationToken = default)` — per data-model.md §IRecordExporter

### 2.5 — Repository Contract Extensions

- [ ] T033 Add five new method signatures to `IFinancialRepository` in `src/FinancialOS.Core/Contracts/IFinancialRepository.cs`: `GetRecordsPagedAsync(FilterCriteria, int page, int pageSize, CancellationToken)` → `Task<PagedResult<FinancialRecord>>`; `StreamRecordsAsync(FilterCriteria, CancellationToken)` → `IAsyncEnumerable<FinancialRecord>`; `GetAccountsPagedAsync(string? accountType, bool? isActive, int page, int pageSize, CancellationToken)` → `Task<PagedResult<Account>>`; `GetCategoriesPagedAsync(string? nameSearch, Guid? parentId, int page, int pageSize, CancellationToken)` → `Task<PagedResult<Category>>`; `GetRulesPagedAsync(string? ruleType, bool? isEnabled, Guid? categoryId, int page, int pageSize, CancellationToken)` → `Task<PagedResult<Rule>>` — per data-model.md §New methods on IFinancialRepository

### 2.6 — Desktop Configuration

- [ ] T034 Add `ApiClientOptions` sealed class to `src/FinancialOS.Desktop/Configuration/ApiClientOptions.cs`: `const string SectionName = "ApiClient"`, `required string BaseUrl`, `int TimeoutSeconds = 30` — per data-model.md §ApiClientOptions

**Checkpoint**: `dotnet build` compiles the full solution. All `IFinancialRepository` new methods have signatures. All new types are defined. No implementation bodies yet (only stubs/`throw new NotImplementedException()` where needed).

---

## Phase 3: User Story 1 — Filter and Browse Financial Records (Priority: P1) 🎯 MVP

**Goal**: All four list endpoints (records, accounts, categories, rules) return filtered and paginated results. Invalid parameters return `400 Bad Request` with the offending parameter named. Empty results return a valid `PagedResult` with zero items.

**Independent Test**: Query `GET /api/v1/records` with date range, merchant keyword, and page parameters. Verify only matching records are returned, pagination metadata is accurate, and an invalid `pageSize` of 500 returns HTTP 400.

### Tests for User Story 1

- [ ] T035 [P] [US1] Add paged records contract tests in `tests/FinancialOS.Api.Tests/RecordFilterContractTests.cs`: POST evidence to create 30 records; GET /api/v1/records with page=1&pageSize=10 → verify 10 items, totalCount=30, totalPages=3; GET page=4 → verify empty items with correct totalCount; verify JSON shape matches contracts/records.md
- [ ] T036 [P] [US1] Add record filter integration tests in `tests/FinancialOS.Api.Tests/RecordFilterIntegrationTests.cs`: test each filter param independently (startDate, endDate, accountId, categoryId, merchant partial match case-insensitive, minAmount, maxAmount); verify boundary values inclusive; verify no records outside filter appear
- [ ] T037 [P] [US1] Add pagination boundary tests in `tests/FinancialOS.Api.Tests/PaginationBehaviorTests.cs`: test pageSize > 200 → HTTP 400 with offending param named; test page=0 → HTTP 400; test invalid type for amount filter → HTTP 400; test accounts, categories, and rules pagination independently

### Implementation for User Story 1

- [ ] T038 [US1] Implement `GetRecordsPagedAsync` in `src/FinancialOS.Data/EfFinancialRepository.cs`: build `IQueryable<FinancialRecord>` predicate chain applying each non-null `FilterCriteria` field; for `CategoryId` include sub-category records; for `MerchantSearch` use `EF.Functions.Like` with `%search%`; call `CountAsync` for total, then `OrderByDescending(r => r.TransactionDate).ThenBy(r => r.Id).Skip/Take`; return `PagedResult<FinancialRecord>` — per data-model.md §EF query pattern
- [ ] T039 [US1] Implement `GetAccountsPagedAsync` in `src/FinancialOS.Data/EfFinancialRepository.cs`: filter by `AccountType` (string equality) and `IsActive`; order by `Name` ascending; `CountAsync` then `Skip/Take`; return `PagedResult<Account>`
- [ ] T040 [US1] Implement `GetCategoriesPagedAsync` in `src/FinancialOS.Data/EfFinancialRepository.cs`: filter by `NameSearch` via `EF.Functions.Like` and `ParentId`; order by `Name` ascending; return `PagedResult<Category>`
- [ ] T041 [US1] Implement `GetRulesPagedAsync` in `src/FinancialOS.Data/EfFinancialRepository.cs`: filter by `RuleType` (string equality), `IsEnabled`, `CategoryId`; order by `Priority` ascending then `Id`; return `PagedResult<Rule>`
- [ ] T042 [US1] Update `GET /api/v1/records` handler in `src/FinancialOS.Api/Endpoints/RecordEndpoints.cs`: add `[AsParameters] RecordFilterQuery query` parameter; validate `query.Page >= 1`, `query.PageSize` between 1 and 200, and `query.ToFilterCriteria().Validate()` → return HTTP 400 with problem-detail naming each offending param on failure; call `IFinancialRepository.GetRecordsPagedAsync`; map to `PagedResult<FinancialRecordSummary>` using existing DTO mapping; return `200 OK` — per contracts/records.md
- [ ] T043 [US1] Update `GET /api/v1/accounts` handler in `src/FinancialOS.Api/Endpoints/AccountEndpoints.cs` (create file if it doesn't exist): add `[AsParameters] AccountFilterQuery query`; validate page/pageSize bounds; call `GetAccountsPagedAsync`; return `PagedResult<AccountSummaryResponse>` — per contracts/accounts.md
- [ ] T044 [US1] Update `GET /api/v1/categories` handler in `src/FinancialOS.Api/Endpoints/CategoryEndpoints.cs` (create file if it doesn't exist): add `[AsParameters] CategoryFilterQuery query`; validate page/pageSize bounds; call `GetCategoriesPagedAsync`; return `PagedResult<CategoryResponse>` — per contracts/categories.md
- [ ] T045 [US1] Update `GET /api/v1/rules` list handler in `src/FinancialOS.Api/Endpoints/RulesEndpoints.cs`: add `[AsParameters] RuleFilterQuery query`; validate page/pageSize bounds; call `GetRulesPagedAsync`; return `PagedResult<RuleResponse>` — per contracts/rules.md
- [ ] T046 [US1] Register new endpoint groups and add `PagedResult<T>` JSON serialisation support in `src/FinancialOS.Api/Program.cs`: call `app.MapAccountEndpoints()`, `app.MapCategoryEndpoints()` if not already present; ensure `System.Text.Json` serialises `PagedResult<T>` correctly (snake_case or camelCase per existing convention)

**Checkpoint**: GET /api/v1/records with filters returns matching records only, correct pagination metadata. GET with pageSize=500 → 400. GET with no filters → all records paged. Accounts, categories, and rules endpoints all return `PagedResult` shapes.

---

## Phase 4: User Story 2 — Export Financial Data (Priority: P2)

**Goal**: `POST /api/v1/exports` returns a downloadable file in the requested format (CSV, JSON, YNAB 4, Goodbudget) containing all records matching the supplied filters. Large exports stream without OOM. An empty export returns a valid empty file, not an error.

**Independent Test**: POST an export request for a known 30-record date range in each format. Open/parse each file and confirm all 30 records are present with correctly mapped fields. POST an export for an empty date range → valid empty file, not HTTP 4xx.

### Tests for User Story 2

- [ ] T047 [P] [US2] Add CSV export contract tests in `tests/FinancialOS.Api.Tests/CsvExportTests.cs`: POST export with `format: "csv"` for seeded date range; parse CSV; verify header row exists, row count matches, date/merchant/amount/category/account/notes columns are correctly populated; test special characters (comma, quote, newline in merchant name) are correctly escaped
- [ ] T048 [P] [US2] Add JSON export tests in `tests/FinancialOS.Api.Tests/JsonExportTests.cs`: POST export with `format: "json"`; deserialise response; verify record count and that each item includes a `provenance` object with `confidenceScore`, `sourceFile`, and `importedAt`; verify valid JSON for zero-record export
- [ ] T049 [P] [US2] Add YNAB 4 export tests in `tests/FinancialOS.Api.Tests/YnabV4ExportTests.cs`: POST export with `format: "ynab4"`; parse CSV; verify columns are exactly `Date,Payee,Memo,Outflow,Inflow`; verify negative amounts appear only in `Outflow` column; verify positive amounts appear only in `Inflow` column; verify zero-record export has header row only
- [ ] T050 [P] [US2] Add Goodbudget export tests in `tests/FinancialOS.Api.Tests/GoodbudgetExportTests.cs`: POST export with `format: "goodbudget"`; parse CSV; verify columns `Date,Envelope,Account,Name,Amount,Notes`; verify category maps to Envelope, merchant maps to Name; verify amount sign is preserved
- [ ] T051 [P] [US2] Add export contract/error tests in `tests/FinancialOS.Api.Tests/ExportContractTests.cs`: verify `Content-Disposition` header contains correct filename with format suffix; verify `Content-Type` matches format; test endDate before startDate → HTTP 400; test unrecognised format value → HTTP 400; test filter validation errors propagate

### Implementation for User Story 2

- [ ] T052 [P] [US2] Implement `CsvRecordExporter` in `src/FinancialOS.Infrastructure/Exporters/CsvRecordExporter.cs` implementing `IRecordExporter`: `Format = ExportFormat.Csv`, `ContentType = "text/csv; charset=utf-8"`, `FileExtension = ".csv"`; use CsvHelper `CsvWriter` with a `ClassMap` mapping `FinancialRecord` fields to columns `Date,Merchant,Amount,Category,Account,Notes`; date formatted as `yyyy-MM-dd`; iterate `IAsyncEnumerable<FinancialRecord>` writing rows one at a time to `outputStream` via `StreamWriter` — per contracts/exports.md §CSV Generic
- [ ] T053 [P] [US2] Implement `YnabV4RecordExporter` in `src/FinancialOS.Infrastructure/Exporters/YnabV4RecordExporter.cs` implementing `IRecordExporter`: `Format = ExportFormat.YnabV4`, `ContentType = "text/csv; charset=utf-8"`, `FileExtension = "-ynab4.csv"`; define `YnabRow` record with `Date`, `Payee`, `Memo`, `Outflow`, `Inflow` fields; format `Date` as `MM/dd/yyyy`; for each record: `Outflow = amount < 0 ? Math.Abs(amount) : 0m`, `Inflow = amount > 0 ? amount : (decimal?)null`; write via CsvHelper ClassMap — per contracts/exports.md §YNAB 4
- [ ] T054 [P] [US2] Implement `GoodbudgetRecordExporter` in `src/FinancialOS.Infrastructure/Exporters/GoodbudgetRecordExporter.cs` implementing `IRecordExporter`: `Format = ExportFormat.Goodbudget`, `ContentType = "text/csv; charset=utf-8"`, `FileExtension = "-goodbudget.csv"`; define `GoodbudgetRow` with columns `Date,Envelope,Account,Name,Amount,Notes`; `Envelope = CategoryName`, `Name = MerchantName`, `Amount` is signed decimal — per contracts/exports.md §Goodbudget
- [ ] T055 [P] [US2] Implement `JsonRecordExporter` in `src/FinancialOS.Infrastructure/Exporters/JsonRecordExporter.cs` implementing `IRecordExporter`: `Format = ExportFormat.Json`, `ContentType = "application/json; charset=utf-8"`, `FileExtension = ".json"`; write opening `[` to stream, then for each record serialise with `JsonSerializer.SerializeAsync` to stream (include all fields + `confidenceScore`, `sourceFile`, `importedAt`), write `,` between items; write closing `]`; handle zero records (write `[]`) — per contracts/exports.md §JSON
- [ ] T056 [US2] Implement `StreamRecordsAsync` in `src/FinancialOS.Data/EfFinancialRepository.cs`: build same predicate chain as `GetRecordsPagedAsync` using `FilterCriteria`; apply `OrderByDescending(r => r.TransactionDate).ThenBy(r => r.Id)`; return `dbContext.FinancialRecords.Where(predicate).AsAsyncEnumerable()` — same ordering as paginated query for deterministic exports
- [ ] T057 [US2] Implement `ExportService` in `src/FinancialOS.Infrastructure/Services/ExportService.cs` implementing `IExportService`: constructor injects `IFinancialRepository`, `IEnumerable<IRecordExporter>`; `ExportAsync`: (1) validate `ExportRequest.Validate()` — throw `ArgumentException` if invalid; (2) resolve exporter by `request.Format` from injected collection — throw if not found; (3) call `IFinancialRepository.StreamRecordsAsync(combinedFilter)`; (4) stream exporter output directly to the HTTP response body (e.g., via `Results.Stream` / a custom `IResult`) instead of buffering into a `MemoryStream`; (5) build filename `financialos-export-{startDate}_{endDate}{exporter.FileExtension}`; (6) return `ExportSnapshot` metadata as needed — per plan.md §Phase 3
- [ ] T058 [US2] Implement `POST /api/v1/exports` handler in `src/FinancialOS.Api/Endpoints/ExportEndpoints.cs`: bind `ExportRequest` from JSON body; call `ExportRequest.Validate()` → return HTTP 400 problem-detail on validation errors; call `IExportService.ExportAsync`; return `Results.File(snapshot.Content, snapshot.ContentType, snapshot.FileName)` with `Content-Disposition: attachment` — per contracts/exports.md
- [ ] T059 [US2] Register `IExportService`, all four `IRecordExporter` implementations, and `ExportEndpoints` in `src/FinancialOS.Api/Program.cs`: `AddScoped<IExportService, ExportService>()`, `AddScoped<IRecordExporter, CsvRecordExporter>()`, `AddScoped<IRecordExporter, YnabV4RecordExporter>()`, `AddScoped<IRecordExporter, GoodbudgetRecordExporter>()`, `AddScoped<IRecordExporter, JsonRecordExporter>()`, `app.MapExportEndpoints()`

**Checkpoint**: POST /api/v1/exports with format "csv" returns a downloadable CSV file. POST with "json" → valid JSON array. POST with "ynab4" → correct Outflow/Inflow columns. POST with "goodbudget" → correct Envelope column. Empty date range → valid empty file, not 4xx. endDate before startDate → HTTP 400.

---

## Phase 5: User Story 3 — Use the Desktop Application (Priority: P3)

**Goal**: The WPF Desktop application starts, reads `ApiClient:BaseUrl` from `appsettings.json`, connects to the running API, and displays accounts and records. No database credentials required. If the API is unreachable, a clear error state is shown (no crash).

**Independent Test**: Start the API with seeded data. Launch the Desktop app configured with `BaseUrl = "http://localhost:5000"`. Verify accounts and records load and match the API's responses. Stop the API. Refresh the Desktop → clear connectivity error message, no crash.

### Tests for User Story 3

- [ ] T060 [P] [US3] Add `FinancialApiClient` unit tests in `tests/FinancialOS.Api.Tests/DesktopApiClientTests.cs`: use `MockHttpMessageHandler` to simulate API responses; verify `GetAccountsAsync()` deserialises `PagedResult<AccountSummaryResponse>` correctly; verify `GetRecordsAsync(filter)` passes query params correctly; verify `ExportAsync(request)` sends correct POST body
- [ ] T061 [P] [US3] Add Desktop connectivity tests in `tests/FinancialOS.Api.Tests/DesktopConnectivityTests.cs`: verify that when the HTTP response is 5xx, `FinancialApiClient` throws a typed `ApiException`; verify `ErrorViewModel` is populated when `FinancialApiClient` throws; verify no `NullReferenceException` or unhandled exception propagates to the UI layer

### Implementation for User Story 3

- [ ] T062 [US3] Implement `FinancialApiClient` typed HttpClient in `src/FinancialOS.Desktop/Services/FinancialApiClient.cs`: constructor injects `HttpClient`; implement `GetAccountsAsync(AccountFilterQuery, CancellationToken)` → `Task<PagedResult<AccountSummaryResponse>>`; `GetRecordsAsync(RecordFilterQuery, CancellationToken)` → `Task<PagedResult<FinancialRecordSummary>>`; `ExportAsync(ExportRequest, CancellationToken)` → `Task<Stream>`; use `System.Text.Json` for deserialisation; throw typed `ApiException` wrapping `HttpRequestException` or non-success status codes — per plan.md §Phase 5
- [ ] T063 [US3] Implement `MainViewModel` in `src/FinancialOS.Desktop/ViewModels/MainViewModel.cs` using `CommunityToolkit.Mvvm`: observable properties `IsConnected`, `CurrentView`, `ErrorMessage`; `LoadAsync()` command attempts to load accounts, sets `IsConnected = true` on success or `IsConnected = false` + `ErrorMessage` on `ApiException`; use `[RelayCommand]` and `[ObservableProperty]`
- [ ] T064 [US3] Implement `AccountsViewModel` in `src/FinancialOS.Desktop/ViewModels/AccountsViewModel.cs`: `ObservableCollection<AccountSummaryResponse> Accounts`; `LoadAsync()` calls `FinancialApiClient.GetAccountsAsync()`; `CurrentPage`, `TotalPages` properties; `NextPageCommand`, `PreviousPageCommand` relay commands
- [ ] T065 [US3] Implement `RecordsViewModel` in `src/FinancialOS.Desktop/ViewModels/RecordsViewModel.cs`: `ObservableCollection<FinancialRecordSummary> Records`; filter properties matching `RecordFilterQuery`; `LoadAsync()` calls `FinancialApiClient.GetRecordsAsync(filter)`; pagination commands; `ApplyFiltersCommand` resets page to 1 and reloads
- [ ] T066 [US3] Implement `ErrorViewModel` in `src/FinancialOS.Desktop/ViewModels/ErrorViewModel.cs`: properties `ErrorTitle`, `ErrorMessage`, `RetryCommand`; `RetryCommand` triggers `MainViewModel.LoadAsync()`
- [ ] T067 [US3] Implement `MainWindow.xaml` and `MainWindow.xaml.cs` in `src/FinancialOS.Desktop/Views/`: bind to `MainViewModel`; show `AccountsView` or `RecordsView` based on `CurrentView` property; show error panel when `IsConnected = false`; add menu/navigation bar to switch between views
- [ ] T068 [US3] Implement `AccountsView.xaml` and `AccountsView.xaml.cs` in `src/FinancialOS.Desktop/Views/`: `DataGrid` or `ListView` bound to `AccountsViewModel.Accounts`; pagination controls bound to `NextPageCommand`/`PreviousPageCommand`; page indicator showing `CurrentPage / TotalPages`
- [ ] T069 [US3] Implement `RecordsView.xaml` and `RecordsView.xaml.cs` in `src/FinancialOS.Desktop/Views/`: filter panel with date pickers, merchant text box, account/category dropdowns; `DataGrid` bound to `RecordsViewModel.Records`; apply filter button; pagination controls
- [ ] T070 [US3] Add `appsettings.json` to `src/FinancialOS.Desktop/` with `ApiClient` section: `{"ApiClient":{"BaseUrl":"http://localhost:5000","TimeoutSeconds":30}}`; mark file as `CopyToOutputDirectory = Always` in `.csproj`
- [ ] T071 [US3] Wire DI container in `src/FinancialOS.Desktop/App.xaml.cs`: build `IServiceCollection`; `Configure<ApiClientOptions>(config.GetSection("ApiClient"))`; `AddHttpClient<FinancialApiClient>` with `BaseAddress` and `Timeout` from options; `AddTransient<MainViewModel>`, `AddTransient<AccountsViewModel>`, `AddTransient<RecordsViewModel>`, `AddTransient<ErrorViewModel>`; resolve `MainWindow` from `IServiceProvider` in `OnStartup`

**Checkpoint**: Launch Desktop with API running → accounts and records load and display. Stop API → clear error message shown, no crash. Restart API → Retry restores data. No database file path in Desktop config.

---

## Phase 6: User Story 4 — Choose Database Backend (Priority: P4)

**Goal**: Setting `"DatabaseProvider": "sqlite"` or `"DatabaseProvider": "postgres"` in `appsettings.json` (or `FINANCIALOS_DB_PROVIDER` environment variable) and restarting the application switches the active database provider. Migrations auto-apply. An unrecognised value causes a clean startup failure with a useful error message.

**Independent Test**: Run the API with `DatabaseProvider = sqlite` → create a record via POST → confirm it persists. Change to `DatabaseProvider = postgres` (with a running PostgreSQL instance) → restart → migrations apply → same record endpoints work. Set `DatabaseProvider = invalid` → startup fails with a clear error, not an unhandled exception.

### Tests for User Story 4

- [ ] T072 [P] [US4] Add SQLite provider startup test in `tests/FinancialOS.Api.Tests/SqliteProviderStartupTests.cs`: start `WebApplicationFactory` with `DatabaseProvider = sqlite` in test configuration; verify health/startup succeeds; create a record via POST; verify it is retrievable via GET — use in-memory SQLite or a temp file path
- [ ] T073 [P] [US4] Add invalid provider startup test in `tests/FinancialOS.Api.Tests/InvalidProviderStartupTests.cs`: attempt to start `WebApplicationFactory` with `DatabaseProvider = invalidvalue`; verify startup throws `InvalidOperationException` with a message containing "DatabaseProvider" and the invalid value
- [ ] T074 [P] [US4] Add ambiguous/missing provider test in `tests/FinancialOS.Api.Tests/InvalidProviderStartupTests.cs`: start with `DatabaseProvider` key absent → verify defaults to `sqlite` and starts successfully

### Implementation for User Story 4

- [ ] T075 [US4] Implement `UseDatabase` static helper method in `src/FinancialOS.Api/Program.cs`: read `builder.Configuration["DatabaseProvider"]` (default `"sqlite"` if absent); read `builder.Configuration.GetConnectionString("Default")` — throw `InvalidOperationException("ConnectionStrings:Default is required.")` if absent; switch on provider value (case-insensitive): `"sqlite"` → `opts.UseSqlite(connStr)`, `"postgres"` → `opts.UseNpgsql(connStr)`, default → `throw new InvalidOperationException($"Unknown DatabaseProvider '{provider}'. Supported values: sqlite, postgres.")`; call `UseDatabase(builder)` before `builder.Build()` replacing the existing `AddDbContext` call — per research.md §Decision 4 and plan.md §Phase 5
- [ ] T076 [US4] Update `src/FinancialOS.Api/appsettings.json` to add `"DatabaseProvider": "sqlite"` key alongside the existing `ConnectionStrings:Default` SQLite path — per research.md §Decision 4 config example
- [ ] T077 [P] [US4] Create `src/FinancialOS.Api/appsettings.Production.json` with PostgreSQL template: `{"DatabaseProvider":"postgres","ConnectionStrings":{"Default":"Host=localhost;Database=financialos;Username=app;Password=CHANGE_ME"}}` — comment that password must be supplied via environment variable or secrets manager in real deployments
- [ ] T078 [P] [US4] Add auto-migration on startup to `src/FinancialOS.Api/Program.cs`: after `app.Build()`, resolve `FinancialOsDbContext` from scope and call `database.MigrateAsync()` — this already works for SQLite; confirm it is idempotent for PostgreSQL (EF Core `MigrateAsync` is always idempotent)

**Checkpoint**: `DatabaseProvider = sqlite` starts, creates records, persists. `DatabaseProvider = invalid` throws `InvalidOperationException` at startup with a useful message. `DatabaseProvider` absent defaults to `sqlite`.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Hardening across all stories — max page size, deterministic ordering, quickstart validation.

- [ ] T079 [P] Add max page size constant `public const int MaxPageSize = 200` to `src/FinancialOS.Api/QueryModels/RecordFilterQuery.cs` and apply it in validation logic in T042; ensure all four filter query classes reference the same constant or a shared `PaginationConstants` class in `src/FinancialOS.Api/QueryModels/PaginationConstants.cs`
- [ ] T080 [P] Add `PaginationConstants` static class to `src/FinancialOS.Api/QueryModels/PaginationConstants.cs`: `public const int DefaultPageSize = 25`, `public const int MaxPageSize = 200`, `public const int MinPage = 1` — referenced by all four query model validation paths
- [ ] T081 [P] Add deterministic ordering regression tests in `tests/FinancialOS.Api.Tests/PaginationBehaviorTests.cs`: insert 50 records with the same `TransactionDate`; page through all pages; collect all IDs; verify no ID appears twice and all 50 IDs are present exactly once — confirms `ThenBy(r => r.Id)` tiebreaker is working
- [ ] T082 [P] Add streaming export memory test in `tests/FinancialOS.Api.Tests/ExportContractTests.cs` (extend existing file): mock `IFinancialRepository.StreamRecordsAsync` to yield 50,000 `FinancialRecord` objects; verify `ExportService.ExportAsync` completes without `OutOfMemoryException` and produces a non-empty stream
- [ ] T083 Run `specs/004-api-exporter-ecosystem/quickstart.md` validation scenarios end-to-end against the completed implementation; document any deviations

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)          → no dependencies; start immediately
Phase 2 (Foundational)   → depends on Phase 1 completion; BLOCKS Phases 3–6
Phase 3 (US1 — Filtering) → depends on Phase 2; can run in parallel with Phase 4
Phase 4 (US2 — Exports)  → depends on Phase 2; requires StreamRecordsAsync from US1 impl (T056)
Phase 5 (US3 — Desktop)  → depends on Phase 2; can start after Foundational (no runtime dependency on US1/US2, but benefits from US1 endpoint shape being stable)
Phase 6 (US4 — DB Provider) → depends on Phase 2 only; fully independent of US1/US2/US3
Phase 7 (Polish)         → depends on Phases 3–6 all complete
```

> **Note**: Phase 4 (Exports) depends on `StreamRecordsAsync` (T056) which logically belongs to US1's EF implementation. T056 should be completed as part of Phase 3, or Phase 4 can implement it as T056 before the exporter implementations. It is listed in Phase 4 because exports drive its existence, but it can be parallelised with T038–T041.

### User Story Dependencies

| Story | Depends on | Can start in parallel with |
|-------|-----------|---------------------------|
| US1 (Filtering, Phase 3) | Phase 2 | US3 (Phase 5), US4 (Phase 6) |
| US2 (Exports, Phase 4) | Phase 2, T056 (StreamRecordsAsync) | US3 (Phase 5), US4 (Phase 6) |
| US3 (Desktop, Phase 5) | Phase 2 | US1 (Phase 3), US2 (Phase 4), US4 (Phase 6) |
| US4 (DB Provider, Phase 6) | Phase 2 | US1, US2, US3 |

### Within Each User Story

- Execute tests first and confirm they fail before implementation.
- Implement repository methods before endpoint wiring.
- Implement exporters before ExportService.
- Implement ExportService before ExportEndpoints.
- Implement FinancialApiClient before ViewModels.
- Implement ViewModels before Views.
- Ensure DI registration is added before marking story complete.

---

## Parallel Execution Examples

### User Story 1

```
T035 + T036 + T037 in parallel (tests),
then T038 + T039 + T040 + T041 in parallel (EF repo methods),
then T042 + T043 + T044 + T045 in parallel (endpoint updates),
then T046 (Program.cs wiring)
```

### User Story 2

```
T047 + T048 + T049 + T050 + T051 in parallel (tests),
then T052 + T053 + T054 + T055 in parallel (exporters) + T056 (StreamRecordsAsync),
then T057 (ExportService),
then T058 (ExportEndpoints),
then T059 (Program.cs wiring)
```

### User Story 3

```
T060 + T061 in parallel (tests),
then T062 (FinancialApiClient),
then T063 + T064 + T065 + T066 in parallel (ViewModels),
then T067 + T068 + T069 in parallel (Views),
then T070 (appsettings.json),
then T071 (App.xaml.cs DI wiring)
```

### User Story 4

```
T072 + T073 + T074 in parallel (tests),
then T075 (UseDatabase helper),
then T076 + T077 + T078 in parallel (config files + migration wiring)
```

---

## Implementation Strategy

### MVP First (US1)

1. Complete Phase 1 (Setup) and Phase 2 (Foundational).
2. Deliver Phase 3 (US1: Filtering & Pagination) with all four endpoints returning paginated filtered results.
3. Validate US1 independently before expanding scope.

### Incremental Delivery

1. Deliver US1 (filtering/pagination) — foundational data access for all downstream workflows.
2. Deliver US2 (exports) — data portability; independent of desktop and DB provider.
3. Deliver US3 (desktop) — UI client wired to stable API from US1.
4. Deliver US4 (database provider) — operational; independent of all user-facing stories.
5. Complete polish and hardening.

### Constitution Alignment Gates

- All new query operations return `PagedResult<T>` — never unbounded lists.
- Export streams data via `IAsyncEnumerable` — never loads full result set into memory.
- Desktop project has zero database credentials; it is a pure API consumer.
- Provider switching requires only a config change — no recompile, consistent with Modular & API-First principle.
- Invalid filter parameters return `400 Bad Request` with named offending param — Explainability Is Required applies to errors too.

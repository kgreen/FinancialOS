# Data Model: API & Exporter Ecosystem (Feature 004)

**Feature**: `004-api-exporter-ecosystem`  
**Date**: 2026-08-01

---

## Overview

Feature 004 introduces no new database tables. All new types are either value objects used in queries, generic wrappers for API responses, or transient export structures. EF Core query methods are added to the existing repository interface to support filtering and pagination.

---

## Value Objects (no DB persistence)

### `FilterCriteria`

```csharp
// src/FinancialOS.Core/Models/FilterCriteria.cs
namespace FinancialOS.Core.Models;

public sealed record FilterCriteria
{
    public DateOnly? StartDate      { get; init; }
    public DateOnly? EndDate        { get; init; }
    public Guid?     AccountId      { get; init; }
    public Guid?     CategoryId     { get; init; }
    public string?   MerchantSearch { get; init; }  // partial, case-insensitive
    public decimal?  MinAmount      { get; init; }
    public decimal?  MaxAmount      { get; init; }

    public IEnumerable<string> Validate()
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            yield return "EndDate must be on or after StartDate.";
        if (MinAmount.HasValue && MaxAmount.HasValue && MaxAmount < MinAmount)
            yield return "MaxAmount must be greater than or equal to MinAmount.";
        if (MinAmount < 0)
            yield return "MinAmount must be non-negative.";
        if (MaxAmount < 0)
            yield return "MaxAmount must be non-negative.";
        if (MerchantSearch is { Length: > 200 })
            yield return "MerchantSearch must not exceed 200 characters.";
    }
}
```

**Validation rules**:
- `EndDate >= StartDate` when both are provided
- `MaxAmount >= MinAmount` when both are provided
- `MinAmount` and `MaxAmount` must be non-negative
- `MerchantSearch` max length: 200 characters
- All fields are optional; an empty `FilterCriteria` matches all records

---

### `RecordFilterQuery`

The query parameters shape for `GET /api/v1/records`. Mapped in the endpoint via `[AsParameters]`.

```csharp
// src/FinancialOS.Api/QueryModels/RecordFilterQuery.cs
namespace FinancialOS.Api.QueryModels;

public sealed class RecordFilterQuery
{
    public DateOnly? StartDate      { get; set; }
    public DateOnly? EndDate        { get; set; }
    public Guid?     AccountId      { get; set; }
    public Guid?     CategoryId     { get; set; }
    public string?   Merchant       { get; set; }
    public decimal?  MinAmount      { get; set; }
    public decimal?  MaxAmount      { get; set; }
    public int       Page           { get; set; } = 1;
    public int       PageSize       { get; set; } = 25;

    public FilterCriteria ToFilterCriteria() => new()
    {
        StartDate      = StartDate,
        EndDate        = EndDate,
        AccountId      = AccountId,
        CategoryId     = CategoryId,
        MerchantSearch = Merchant,
        MinAmount      = MinAmount,
        MaxAmount      = MaxAmount,
    };
}
```

**Constraints**:
- `Page` minimum: 1
- `PageSize` minimum: 1, maximum: 200 (enforced at endpoint level)

---

### `AccountFilterQuery`

```csharp
// src/FinancialOS.Api/QueryModels/AccountFilterQuery.cs
namespace FinancialOS.Api.QueryModels;

public sealed class AccountFilterQuery
{
    public string? AccountType { get; set; }   // e.g., "Checking", "Savings", "CreditCard"
    public bool?   IsActive    { get; set; }
    public int     Page        { get; set; } = 1;
    public int     PageSize    { get; set; } = 25;
}
```

---

### `CategoryFilterQuery`

```csharp
// src/FinancialOS.Api/QueryModels/CategoryFilterQuery.cs
namespace FinancialOS.Api.QueryModels;

public sealed class CategoryFilterQuery
{
    public string? NameSearch  { get; set; }   // partial, case-insensitive
    public Guid?   ParentId    { get; set; }
    public int     Page        { get; set; } = 1;
    public int     PageSize    { get; set; } = 25;
}
```

---

### `RuleFilterQuery`

```csharp
// src/FinancialOS.Api/QueryModels/RuleFilterQuery.cs
namespace FinancialOS.Api.QueryModels;

public sealed class RuleFilterQuery
{
    public string? RuleType    { get; set; }   // e.g., "MerchantMatch", "CategoryAssign"
    public bool?   IsEnabled   { get; set; }
    public Guid?   CategoryId  { get; set; }
    public int     Page        { get; set; } = 1;
    public int     PageSize    { get; set; } = 25;
}
```

---

## Generic Wrappers

### `PagedResult<T>`

```csharp
// src/FinancialOS.Shared/PagedResult.cs
namespace FinancialOS.Shared;

public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items      { get; init; } = [];
    public int              Page       { get; init; }
    public int              PageSize   { get; init; }
    public int              TotalCount { get; init; }
    public int              TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

**JSON shape** (example with records):
```json
{
  "items": [ /* array of T */ ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 143,
  "totalPages": 6
}
```

---

## Export Types

### `ExportFormat` enum

```csharp
// src/FinancialOS.Core/Models/ExportFormat.cs
namespace FinancialOS.Core.Models;

public enum ExportFormat
{
    Csv         = 0,
    Json        = 1,
    YnabV4      = 2,
    Goodbudget  = 3
}
```

---

### `ExportRequest`

```csharp
// src/FinancialOS.Core/Models/ExportRequest.cs
namespace FinancialOS.Core.Models;

public sealed record ExportRequest
{
    public required ExportFormat  Format          { get; init; }
    public required DateOnly      StartDate       { get; init; }
    public required DateOnly      EndDate         { get; init; }
    public FilterCriteria?        AdditionalFilters { get; init; }

    public IEnumerable<string> Validate()
    {
        if (EndDate < StartDate)
            yield return "EndDate must be on or after StartDate.";
        if (AdditionalFilters is not null)
            foreach (var e in AdditionalFilters.Validate())
                yield return e;
    }
}
```

---

### `ExportSnapshot` (transient — never persisted)

```csharp
// src/FinancialOS.Core/Models/ExportSnapshot.cs
namespace FinancialOS.Core.Models;

/// <summary>
/// Transient result of an export operation. Carries the stream and metadata
/// needed to write the HTTP response. Never stored in the database.
/// </summary>
public sealed record ExportSnapshot
{
    public required Stream       Content     { get; init; }  // readable, caller owns disposal
    public required string       FileName    { get; init; }  // e.g., "export-2026-08-01.csv"
    public required string       ContentType { get; init; }  // e.g., "text/csv"
    public required ExportFormat Format      { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
    public required int          RecordCount  { get; init; }
}
```

---

## Desktop Configuration

### `ApiClientOptions`

```csharp
// src/FinancialOS.Desktop/Configuration/ApiClientOptions.cs
namespace FinancialOS.Desktop.Configuration;

public sealed class ApiClientOptions
{
    public const string SectionName = "ApiClient";

    public required string BaseUrl        { get; init; }  // e.g., "http://localhost:5000"
    public int             TimeoutSeconds { get; init; } = 30;
}
```

**appsettings.json (Desktop)**:
```json
{
  "ApiClient": {
    "BaseUrl": "http://localhost:5000",
    "TimeoutSeconds": 30
  }
}
```

---

## EF Core Query Changes

No new tables. New query methods added to `IFinancialRepository` and implemented in `EfFinancialRepository`.

### New methods on `IFinancialRepository`

```csharp
// src/FinancialOS.Core/Contracts/IFinancialRepository.cs  (additions)

Task<PagedResult<FinancialRecord>> GetRecordsPagedAsync(
    FilterCriteria filter,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

IAsyncEnumerable<FinancialRecord> StreamRecordsAsync(
    FilterCriteria filter,
    CancellationToken cancellationToken = default);

Task<PagedResult<Account>> GetAccountsPagedAsync(
    string? accountType,
    bool? isActive,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

Task<PagedResult<Category>> GetCategoriesPagedAsync(
    string? nameSearch,
    Guid? parentId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

Task<PagedResult<Rule>> GetRulesPagedAsync(
    string? ruleType,
    bool? isEnabled,
    Guid? categoryId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);
```

### EF query pattern for `GetRecordsPagedAsync`

```csharp
// src/FinancialOS.Data/EfFinancialRepository.cs (additions)

public async Task<PagedResult<FinancialRecord>> GetRecordsPagedAsync(
    FilterCriteria filter, int page, int pageSize, CancellationToken ct)
{
    var query = _context.FinancialRecords.AsNoTracking();

    if (filter.StartDate.HasValue)
        query = query.Where(r => r.TransactionDate >= filter.StartDate.Value);
    if (filter.EndDate.HasValue)
        query = query.Where(r => r.TransactionDate <= filter.EndDate.Value);
    if (filter.AccountId.HasValue)
        query = query.Where(r => r.AccountId == filter.AccountId.Value);
    if (filter.CategoryId.HasValue)
        query = query.Where(r => r.CategoryId == filter.CategoryId.Value
                              || r.Category!.ParentId == filter.CategoryId.Value);
    if (!string.IsNullOrWhiteSpace(filter.MerchantSearch))
        query = query.Where(r => r.MerchantName.ToLower()
                                  .Contains(filter.MerchantSearch.ToLower()));
    if (filter.MinAmount.HasValue)
        query = query.Where(r => r.Amount >= filter.MinAmount.Value);
    if (filter.MaxAmount.HasValue)
        query = query.Where(r => r.Amount <= filter.MaxAmount.Value);

    var totalCount = await query.CountAsync(ct);

    var items = await query
        .OrderByDescending(r => r.TransactionDate)
        .ThenBy(r => r.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new PagedResult<FinancialRecord>
    {
        Items      = items,
        Page       = page,
        PageSize   = pageSize,
        TotalCount = totalCount,
    };
}
```

### Export service interface

```csharp
// src/FinancialOS.Core/Contracts/IExportService.cs
namespace FinancialOS.Core.Contracts;

public interface IExportService
{
    Task<ExportSnapshot> ExportAsync(
        ExportRequest request,
        CancellationToken cancellationToken = default);
}
```

### Per-format exporter interfaces

```csharp
// src/FinancialOS.Infrastructure/Exporters/IRecordExporter.cs
namespace FinancialOS.Infrastructure.Exporters;

public interface IRecordExporter
{
    ExportFormat Format { get; }
    string       ContentType { get; }
    string       FileExtension { get; }

    Task WriteAsync(
        IAsyncEnumerable<FinancialRecord> records,
        Stream destination,
        CancellationToken cancellationToken = default);
}
```

**Implementations** (each in `src/FinancialOS.Infrastructure/Exporters/`):
- `CsvRecordExporter` — `ExportFormat.Csv`
- `JsonRecordExporter` — `ExportFormat.Json`
- `YnabV4RecordExporter` — `ExportFormat.YnabV4`
- `GoodbudgetRecordExporter` — `ExportFormat.Goodbudget`

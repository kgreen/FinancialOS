# Implementation Plan: Transaction Parsing & Record Hydration

**Branch**: `003-transaction-parsing` | **Date**: 2026-08-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-transaction-parsing/spec.md`

---

## Summary

Evidence upload (spec 001) stores raw files and creates a single placeholder record with no financial meaning. This feature closes that gap by introducing a full parsing pipeline: when a file is uploaded, the platform detects format (CSV or OFX/QFX), dispatches to the appropriate parser, hydrates one `FinancialRecord` per transaction, invokes the existing rule evaluation service for immediate classification, and returns a structured import summary in the same response. An `InstitutionProfile` entity enables configurable CSV column mapping per bank. An `ImportJob` entity tracks parse status and per-row failure details for post-hoc auditing.

**Technical approach**: Add `InstitutionProfile` and `ImportJob` domain entities; extend `FinancialRecord` with import tracking fields; build `ITransactionParser` abstraction with `CsvTransactionParser` and `OfxTransactionParser` implementations; add `IImportOrchestrationService` that orchestrates file → parse → hydrate → classify → persist; extend `IFinancialRepository`; add EF migration; expose four new endpoint groups; replace the placeholder-record logic in `POST /api/v1/evidence`.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**:
- `Microsoft.EntityFrameworkCore` 8.x (existing) — ORM for `InstitutionProfile`, `ImportJob`, new `FinancialRecord` columns
- `CsvHelper` — robust CSV parsing with configurable mapping strategies (NEEDS CLARIFICATION: confirm version used or adopt latest stable)
- `OFXSharp` or manual SGML/XML fallback — OFX 1.x SGML and OFX 2.x XML parsing (NEEDS CLARIFICATION: library choice; see research.md)
- `System.Text.Json` (existing) — JSON serialization for `ColumnMappings` and `FailedRows` columns
- `Microsoft.AspNetCore.Http` (existing) — multipart form file handling

**Storage**: SQLite (development) + PostgreSQL (production) via EF Core — no change to dual-provider strategy

**Testing**: `xunit` + `Microsoft.AspNetCore.Mvc.Testing` for integration tests (existing pattern in `tests/FinancialOS.Api.Tests/`); unit tests in `tests/FinancialOS.Core.Tests/`

**Target Platform**: ASP.NET Core 8 minimal APIs, cross-platform server deployment

**Performance Goals**: Import jobs of up to 10,000 rows must complete without timeout (per SC-007); synchronous processing is acceptable for this milestone

**Constraints**: No modification of stored evidence artifact at any stage (Constitution II + FR-012); both SQLite and PostgreSQL must exhibit identical behaviour (FR-021)

**Scale/Scope**: Single-tenant deployment; institution profiles are platform-global; background job offloading is out of scope for this milestone

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design (see post-design recheck below).*

| Principle | Assessment |
|-----------|------------|
| **I. Truth Before Convenience** — parsing creates derivative records; raw evidence is never modified | ✅ FR-012 explicitly prohibits modifying stored evidence. `EvidenceImportService` writes the file once and returns; the pipeline only reads it back. |
| **II. Facts Are Immutable** — source amounts, dates, raw text preserved exactly as parsed | ✅ `ParsedTransaction.RawRow` retains the original line. Amounts and dates are stored as parsed, not normalized or rounded during hydration. |
| **III. Explainability Is Required** — every classification exposes confidence + reason code | ✅ FR-015/FR-016/FR-017 mandate confidence score, reason code, provenance entry (EvidenceId + ImportJobId + parser type + row index) on every `FinancialRecord`. |
| **IV. Humans Contain Authority** — auto-classification is advisory; no record is silently overwritten | ✅ Rule evaluation produces `Classified` status with confidence; no record is deleted or merged without user action. |
| **V. Knowledge Before Intelligence** — CSV auto-detection and OFX parsing use deterministic heuristics | ✅ FR-005 limits auto-detection to a shipped set of known layouts; no ML inference. |
| **VI. Modular & API-First** — new entities in Core; parsers in Infrastructure; endpoints in Api | ✅ `ParsedTransaction` DTO, `InstitutionProfile`, `ImportJob` go in `FinancialOS.Core`; parsers in `FinancialOS.Infrastructure`; endpoints in `FinancialOS.Api`. |
| **Architectural constraint** — core must be independent of UI and infrastructure | ✅ `ITransactionParser` and `IImportOrchestrationService` are Core contracts; implementations are in Infrastructure. |

**Gate result: PASS** — no constitution violations.

**Post-design recheck**: see research.md and data-model.md conclusions.

---

## Project Structure

### Documentation (this feature)

```text
specs/003-transaction-parsing/
├── plan.md              ← this file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/
│   ├── import-jobs.md
│   └── institution-profiles.md
└── tasks.md             ← Phase 2 output (speckit.tasks, not generated here)
```

### Source Code

```text
src/
├── FinancialOS.Core/
│   ├── Models/
│   │   ├── DomainEntities.cs          ← extend FinancialRecord; add RecordStatus.Classified
│   │   └── ImportEntities.cs          ← NEW: InstitutionProfile, ImportJob, ParsedTransaction,
│   │                                         AmountLayout, ImportJobStatus, ParserType, FailedRowEntry
│   └── Contracts/
│       ├── IFinancialRepository.cs    ← extend: InstitutionProfile + ImportJob CRUD,
│       │                                          GetEvidenceBySha256, GetRecordByExternalId
│       └── IImportOrchestrationService.cs  ← NEW
│       └── ITransactionParser.cs          ← NEW
│
├── FinancialOS.Infrastructure/
│   └── Import/
│       ├── EvidenceImportService.cs       ← extend: SHA256 duplicate check + format validation
│       ├── Parsers/
│       │   ├── CsvTransactionParser.cs    ← NEW
│       │   ├── OfxTransactionParser.cs    ← NEW
│       │   └── CsvAutoDetector.cs         ← NEW: auto-detect common layouts
│       └── ImportOrchestrationService.cs  ← NEW: orchestrates pipeline
│
├── FinancialOS.Data/
│   ├── FinancialOsDbContext.cs            ← add DbSet<InstitutionProfile>, DbSet<ImportJob>;
│   │                                         update FinancialRecord mapping
│   ├── EfFinancialRepository.cs           ← implement new repository methods
│   └── Migrations/
│       └── [timestamp]_AddTransactionParsing.cs   ← NEW migration
│
├── FinancialOS.Shared/
│   └── Contracts/
│       ├── FinancialDtos.cs               ← extend EvidenceUploadResponse; add ImportJobDto,
│       │                                         ImportJobSummaryDto, FailedRowDto
│       └── ImportDtos.cs                  ← NEW: InstitutionProfileDto, CreateProfileRequest,
│                                                   UpdateProfileRequest, ImportRecordSummary
│
└── FinancialOS.Api/
    ├── Program.cs                         ← register new services; update POST /api/v1/evidence
    └── Endpoints/
        ├── ImportJobEndpoints.cs          ← NEW: GET /api/v1/import-jobs/{id}
        └── InstitutionProfileEndpoints.cs ← NEW: POST/GET/PUT/DELETE /api/v1/institution-profiles

tests/
├── FinancialOS.Api.Tests/
│   ├── EvidenceImportIntegrationTests.cs  ← NEW: end-to-end upload → records scenarios
│   ├── ImportJobEndpointTests.cs          ← NEW: GET import-jobs/{id} query scenarios
│   └── InstitutionProfileEndpointTests.cs ← NEW: CRUD profile scenarios
└── FinancialOS.Core.Tests/
    ├── CsvTransactionParserTests.cs       ← NEW: unit tests per FR-001–FR-006
    └── OfxTransactionParserTests.cs       ← NEW: unit tests per FR-007–FR-011
```

---

## Phase 0: Research

> **Output**: `specs/003-transaction-parsing/research.md`

### Research tasks (all NEEDS CLARIFICATION items)

#### R-1 — OFX 1.x SGML parsing library choice

**Unknown**: OFX 1.x files are not well-formed XML — they use an SGML-derived flat tag format. .NET has no built-in SGML parser.

**Options to evaluate**:
- `OFXSharp` (GitHub: OFXSharp/OFXSharp) — .NET OFX parser; supports 1.x and 2.x; last active ~2022
- `dotnet-ofx` / hand-rolled regex-based SGML tokenizer — simpler, no external dependency
- Parse as XML after stripping OFX 1.x SGML headers (known technique used by Quicken importers)

**Decision criteria**: correctness on real Chase/Ally/Fidelity QFX files, maintenance burden, NuGet availability, MIT/permissive licence.

#### R-2 — CsvHelper version and configuration

**Unknown**: CsvHelper is not listed in current `.csproj` files. Confirm preferred version and configuration pattern for this codebase (record mapping vs. dynamic header discovery).

**Tasks**: Check existing `.csproj` files for any CSV package; evaluate CsvHelper 33.x dynamic reading for auto-detection scenarios.

#### R-3 — CSV auto-detection header heuristics

**Unknown**: FR-005 requires recognising common layouts without a profile. Define the set of supported auto-detected layouts.

**Research tasks**:
- Collect real header rows from Chase checking, Chase credit, Ally Bank, Citi, Discover, Bank of America, Capital One standard exports
- Identify canonical header sets and map them to standard fields
- Define confidence threshold: if no known header is matched, return `UnknownLayout` error

#### R-4 — `FinancialRecord` classification status strategy

**Unknown**: Existing `RecordStatus` enum has `Pending`, `Normalized`, `Reviewed`, `Ignored`. Spec 003 introduces `Classified` status. Two strategies exist:

- **Option A**: Add `Classified` to the existing `RecordStatus` enum in `DomainEntities.cs` — simpler, no new column
- **Option B**: Add a separate `ClassificationStatus` column to `FinancialRecord` — keeps classification state orthogonal to record lifecycle state

**Decision criteria**: spec calls `ClassificationStatus` a separate field (`Pending`/`Classified`); Option B preserves the original `RecordStatus` lifecycle without conflating classification with review state.

**Recommendation (to confirm in research.md)**: Option B — add `ClassificationStatus` enum (`Pending`, `Classified`) as a new column on `FinancialRecord`; leave `RecordStatus` unchanged.

#### R-5 — EF Core migration strategy for `FinancialRecord` column additions

**Unknown**: Adding nullable columns to existing `FinancialRecord` table (`ImportJobId`, `ExternalReferenceId`, `RowIndex`, `ClassificationStatus`) must not break rows created by spec 001 or spec 002.

**Confirm**: All new `FinancialRecord` columns are nullable or have safe defaults so existing rows are not invalidated on migration.

---

## Phase 1: Design & Contracts

> **Prerequisites**: `research.md` complete, all NEEDS CLARIFICATION items resolved.
> **Output**: `data-model.md`, `contracts/import-jobs.md`, `contracts/institution-profiles.md`, `quickstart.md`

---

### 1.1 — Data Model

> Detailed in `data-model.md`. Summary captured here for planning purposes.

#### New entity: `ParsedTransaction` *(transient DTO, never persisted)*

Produced by `ITransactionParser.ParseAsync(...)`. Consumed by `ImportOrchestrationService` to hydrate `FinancialRecord` instances.

| Field | Type | Notes |
|-------|------|-------|
| `TransactionDate` | `DateOnly` | Required — row skipped if absent/unparseable |
| `Amount` | `decimal` | Signed; negative = debit |
| `Description` | `string` | From `NAME` (OFX) or mapped column (CSV) |
| `Balance` | `decimal?` | Optional running balance |
| `ExternalReferenceId` | `string?` | `FITID` (OFX) or mapped reference column (CSV) |
| `RowIndex` | `int` | 0-based source row number (CSV) or element index (OFX) |
| `RawRow` | `string` | Verbatim source line for provenance |

#### New entity: `InstitutionProfile` *(persisted)*

| Field | Type | EF mapping notes |
|-------|------|-----------------|
| `Id` | `Guid` | PK |
| `Name` | `string` | Required; unique index |
| `ColumnMappings` | `Dictionary<string,string>` | JSON column (`nvarchar(max)` / `text`) |
| `AmountLayout` | `AmountLayout` (enum) | `string` conversion: `SingleSigned`, `SplitDebitCredit` |
| `DebitColumnName` | `string?` | Required when `AmountLayout = SplitDebitCredit` |
| `CreditColumnName` | `string?` | Required when `AmountLayout = SplitDebitCredit` |
| `DateFormatPattern` | `string?` | e.g. `MM/dd/yyyy`; null = try common formats |
| `CreatedAt` | `DateTimeOffset` | |
| `UpdatedAt` | `DateTimeOffset` | |
| `IsDeleted` | `bool` | Soft-delete; never hard-deleted if used in an `ImportJob` |

**Standard field key names** for `ColumnMappings`: `date`, `amount`, `description`, `balance`, `reference`.

#### New entity: `ImportJob` *(persisted)*

| Field | Type | EF mapping notes |
|-------|------|-----------------|
| `Id` | `Guid` | PK |
| `EvidenceId` | `Guid` | FK → `FinancialEvidence` |
| `InstitutionProfileId` | `Guid?` | FK → `InstitutionProfile`; null for OFX or auto-detected CSV |
| `ParserType` | `ParserType` (enum) | `string` conversion: `CsvConfigured`, `CsvAutoDetected`, `Ofx` |
| `Status` | `ImportJobStatus` (enum) | `string` conversion: `Pending`, `Processing`, `Completed`, `PartialSuccess`, `Failed` |
| `TotalRows` | `int` | |
| `ParsedCount` | `int` | |
| `FailedRowCount` | `int` | |
| `FailedRows` | `List<FailedRowEntry>` | JSON column |
| `StartedAt` | `DateTimeOffset?` | |
| `CompletedAt` | `DateTimeOffset?` | |
| `CreatedAt` | `DateTimeOffset` | |

`FailedRowEntry` value type: `{ int RowIndex; string Reason }`.

#### Extended `FinancialRecord` columns *(via migration)*

All new columns are **nullable** to preserve compatibility with existing spec 001/002 records.

| New field | Type | Notes |
|-----------|------|-------|
| `ImportJobId` | `Guid?` | FK → `ImportJob`; nullable for manually created records |
| `ExternalReferenceId` | `string?` | OFX `FITID` or CSV reference column value |
| `RowIndex` | `int?` | 0-based source row for traceability |
| `ClassificationStatus` | `ClassificationStatus?` (new enum) | `Pending`, `Classified`; null for legacy records |
| `ClassificationReasonCode` | `string?` | First reason code from `RuleEvaluationResult.ReasonCodes` |

> `ClassificationConfidence` already exists as an owned type on `FinancialRecord` (from spec 001); reused here.

#### EF migration

**Migration name**: `AddTransactionParsing`
**File**: `src/FinancialOS.Data/Migrations/[timestamp]_AddTransactionParsing.cs`

Changes:
- Create `InstitutionProfiles` table
- Create `ImportJobs` table with JSON `FailedRows` column
- Add nullable columns to `FinancialRecords`: `ImportJobId`, `ExternalReferenceId`, `RowIndex`, `ClassificationStatus`, `ClassificationReasonCode`
- Add FK index: `IX_FinancialRecord_ImportJobId`
- Add index: `IX_FinancialRecord_ExternalReferenceId` (for cross-import FITID duplicate detection)
- Add index: `IX_ImportJob_EvidenceId`
- Add index: `IX_InstitutionProfile_Name_Unique` (unique)

---

### 1.2 — Core Contracts

#### `ITransactionParser` *(new, `FinancialOS.Core/Contracts/`)*

```csharp
public interface ITransactionParser
{
    ParserType ParserType { get; }
    bool CanParse(string fileName, EvidenceSourceType sourceType);
    Task<TransactionParseResult> ParseAsync(
        Stream stream,
        InstitutionProfile? profile,
        CancellationToken cancellationToken = default);
}

public sealed record TransactionParseResult(
    IReadOnlyList<ParsedTransaction> Transactions,
    IReadOnlyList<FailedRowEntry> FailedRows,
    int TotalRowsScanned);
```

#### `IImportOrchestrationService` *(new, `FinancialOS.Core/Contracts/`)*

```csharp
public interface IImportOrchestrationService
{
    Task<ImportOrchestrationResult> ImportAsync(
        string fileName,
        Stream fileStream,
        Guid? institutionProfileId,
        CancellationToken cancellationToken = default);
}

public sealed record ImportOrchestrationResult(
    FinancialEvidence Evidence,
    ImportJob Job,
    IReadOnlyList<FinancialRecord> CreatedRecords,
    bool WasDuplicate);
```

#### `IFinancialRepository` additions

```csharp
// Evidence
Task<FinancialEvidence?> GetEvidenceBySha256Async(string sha256, CancellationToken cancellationToken = default);

// ImportJob
Task<ImportJob> AddImportJobAsync(ImportJob job, CancellationToken cancellationToken = default);
Task<ImportJob?> GetImportJobAsync(Guid id, CancellationToken cancellationToken = default);
Task<ImportJob?> UpdateImportJobAsync(ImportJob job, CancellationToken cancellationToken = default);
Task<ImportJob?> GetImportJobByEvidenceIdAsync(Guid evidenceId, CancellationToken cancellationToken = default);

// InstitutionProfile
Task<InstitutionProfile> AddInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default);
Task<InstitutionProfile?> GetInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default);
Task<IReadOnlyList<InstitutionProfile>> ListInstitutionProfilesAsync(CancellationToken cancellationToken = default);
Task<InstitutionProfile?> UpdateInstitutionProfileAsync(InstitutionProfile profile, CancellationToken cancellationToken = default);
Task<bool> DeleteInstitutionProfileAsync(Guid id, CancellationToken cancellationToken = default);  // returns false if profile has been used

// FinancialRecord duplicate detection
Task<bool> ExternalReferenceIdExistsAsync(string externalReferenceId, CancellationToken cancellationToken = default);
Task<IReadOnlyList<FinancialRecord>> ListRecordsByImportJobAsync(Guid importJobId, CancellationToken cancellationToken = default);
```

---

### 1.3 — API Contracts

> Full schemas in `contracts/import-jobs.md` and `contracts/institution-profiles.md`.

#### Updated `POST /api/v1/evidence`

**Request**: multipart form data — `file` (required) + optional `institutionProfileId` (Guid, form field).

**Response `200 OK`** (replaces current `EvidenceUploadResponse`):

```json
{
  "evidenceId": "uuid",
  "importJobId": "uuid",
  "status": "completed",
  "parserType": "csvAutoDetected",
  "parsedTransactionCount": 10,
  "failedRowCount": 0,
  "records": [
    {
      "id": "uuid",
      "date": "2026-07-15",
      "amount": -42.50,
      "currency": "USD",
      "description": "AMAZON.COM*XY1Z",
      "classificationStatus": "classified",
      "classificationConfidence": 0.95,
      "classificationReasonCode": "merchant_match"
    }
  ]
}
```

**Duplicate evidence response (same file re-uploaded)** — `200 OK` with `"status": "duplicate"` and the existing `importJobId`.

**Unsupported format** — `422 Unprocessable Entity` with problem details.

**Zero-byte file** — `400 Bad Request`.

#### `GET /api/v1/import-jobs/{id}`

```json
{
  "id": "uuid",
  "evidenceId": "uuid",
  "institutionProfileId": null,
  "parserType": "ofx",
  "status": "partialSuccess",
  "totalRows": 10,
  "parsedCount": 8,
  "failedRowCount": 2,
  "startedAt": "2026-08-01T16:00:00Z",
  "completedAt": "2026-08-01T16:00:01Z",
  "failedRows": [
    { "rowIndex": 3, "reason": "Missing required field: date" },
    { "rowIndex": 7, "reason": "Amount is not a valid decimal: 'N/A'" }
  ]
}
```

#### `POST /api/v1/institution-profiles`

Request body: `{ name, columnMappings, amountLayout, debitColumnName?, creditColumnName?, dateFormatPattern? }`

Response `201 Created`: full profile object.

#### `GET /api/v1/institution-profiles` / `GET /api/v1/institution-profiles/{id}`

Returns list or single profile. Excludes soft-deleted profiles by default.

#### `PUT /api/v1/institution-profiles/{id}`

Request body: same shape as POST. Response `200 OK`: updated profile.

#### `DELETE /api/v1/institution-profiles/{id}`

- If profile has been referenced by any `ImportJob` → `409 Conflict` with problem details.
- If profile exists and has no import history → `204 No Content` (hard delete; or soft-delete if `IsDeleted = true` strategy preferred — confirm in research.md).

---

### 1.4 — Import Pipeline: Orchestration Flow

```
POST /api/v1/evidence
    │
    ├─ 1. EvidenceImportService.ImportAsync()
    │       ├─ Reject zero-byte files → 400
    │       ├─ Reject unsupported extensions (.pdf, .xlsx, etc.) → 422
    │       ├─ Hash file (SHA256)
    │       └─ Write file to storage → returns EvidenceImportResult
    │
    ├─ 2. IFinancialRepository.GetEvidenceBySha256Async()
    │       └─ If match found → return existing ImportJob (duplicate response)
    │
    ├─ 3. IFinancialRepository.AddEvidenceAsync()
    │
    ├─ 4. IImportOrchestrationService.ImportAsync()
    │       ├─ 4a. Select parser (OfxTransactionParser | CsvTransactionParser)
    │       │       └─ CsvTransactionParser: resolve InstitutionProfile or attempt auto-detect
    │       │               └─ If neither → fail with "UnknownLayout" error
    │       ├─ 4b. Create ImportJob (Status = Processing)
    │       ├─ 4c. parser.ParseAsync() → TransactionParseResult
    │       │       ├─ OFX 1.x: strip SGML headers, extract STMTTRN elements
    │       │       ├─ OFX 2.x: parse as XML
    │       │       └─ CSV: use profile or auto-detected mapping; skip bad rows, accumulate FailedRows
    │       ├─ 4d. For each ParsedTransaction:
    │       │       ├─ Check ExternalReferenceIdExistsAsync() for OFX cross-import duplicates
    │       │       ├─ Hydrate FinancialRecord (EvidenceId, ImportJobId, Amount, Date, Description,
    │       │       │     ExternalReferenceId, RowIndex, ClassificationStatus = Pending)
    │       │       ├─ IFinancialRepository.AddRecordAsync()
    │       │       ├─ IRuleEvaluationService.EvaluateAsync(record)
    │       │       │       └─ If match → update ClassificationStatus = Classified, apply confidence + reason code
    │       │       └─ ProvenanceWriter.AppendProvenanceEntryAsync()
    │       │               (StepType = RuleEvaluation or Normalization, SourceReference = "parser:{parserType}:row:{rowIndex}")
    │       └─ 4e. Finalize ImportJob:
    │               ├─ Status = Completed | PartialSuccess | Failed
    │               └─ IFinancialRepository.UpdateImportJobAsync()
    │
    └─ 5. Return ImportOrchestrationResult → serialize to EvidenceImportResponse
```

**Note**: Placeholder record creation (`new FinancialRecord { Amount = Money.Zero(...) }`) in the current `POST /api/v1/evidence` handler is **removed**. For supported formats, the orchestration service creates hydrated records. Unsupported formats (PDF, Image) continue to create a placeholder via a legacy path until a dedicated parser exists.

---

### 1.5 — Shared DTOs additions (`FinancialOS.Shared/Contracts/`)

New file `ImportDtos.cs`:

```csharp
// Request
public sealed record CreateInstitutionProfileRequest(
    [Required] string Name,
    [Required] Dictionary<string,string> ColumnMappings,
    string AmountLayout,          // "SingleSigned" | "SplitDebitCredit"
    string? DebitColumnName,
    string? CreditColumnName,
    string? DateFormatPattern);

public sealed record UpdateInstitutionProfileRequest( ... same shape ... );

// Response
public sealed record InstitutionProfileResponse(
    Guid Id, string Name, Dictionary<string,string> ColumnMappings,
    string AmountLayout, string? DebitColumnName, string? CreditColumnName,
    string? DateFormatPattern, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record ImportJobResponse(
    Guid Id, Guid EvidenceId, Guid? InstitutionProfileId, string ParserType,
    string Status, int TotalRows, int ParsedCount, int FailedRowCount,
    DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt,
    IReadOnlyList<FailedRowDto> FailedRows);

public sealed record FailedRowDto(int RowIndex, string Reason);

public sealed record ImportRecordSummary(
    Guid Id, DateOnly Date, decimal Amount, string Currency, string Description,
    string ClassificationStatus, decimal? ClassificationConfidence,
    string? ClassificationReasonCode);
```

`EvidenceUploadResponse` in `FinancialDtos.cs` is **replaced** (or extended) to:

```csharp
public sealed record EvidenceImportResponse(
    Guid EvidenceId,
    Guid ImportJobId,
    string Status,              // "completed" | "partialSuccess" | "failed" | "duplicate"
    string ParserType,
    int ParsedTransactionCount,
    int FailedRowCount,
    IReadOnlyList<ImportRecordSummary> Records);
```

---

### 1.6 — Service Registration Changes (`Program.cs`)

```csharp
// Remove: builder.Services.AddSingleton<EvidenceImportService>();
// Add:
builder.Services.AddScoped<EvidenceImportService>();        // now needs IFinancialRepository
builder.Services.AddScoped<ITransactionParser, CsvTransactionParser>();
builder.Services.AddScoped<ITransactionParser, OfxTransactionParser>();
builder.Services.AddScoped<IImportOrchestrationService, ImportOrchestrationService>();

// Register new endpoint groups:
app.MapImportJobEndpoints();
app.MapInstitutionProfileEndpoints();
```

---

## Post-Design Constitution Recheck

| Principle | Post-design status |
|-----------|-------------------|
| **I. Truth Before Convenience** | ✅ `EvidenceImportService` writes immutably; pipeline only reads. `FinancialEvidence` row is never updated post-creation. |
| **II. Facts Are Immutable** | ✅ `ParsedTransaction.RawRow` preserved. `FinancialRecord.Amount` stored as parsed decimal without rounding. `ExternalReferenceId` stored verbatim. |
| **III. Explainability Is Required** | ✅ Every `FinancialRecord` carries `ImportJobId`, `RowIndex`, parser type via `ProvenanceEntry`. Classification carries `ClassificationConfidence` + `ClassificationReasonCode`. |
| **IV. Humans Contain Authority** | ✅ Auto-classification sets `ClassificationStatus = Classified` but does not lock the record; existing `/api/v1/records/{id}/classify` remains for manual override. |
| **V. Knowledge Before Intelligence** | ✅ Auto-detection is a shipped lookup table of known CSV headers. No ML. |
| **VI. Modular & API-First** | ✅ All contracts in Core; all implementations in Infrastructure/Data/Api. |

**Post-design gate: PASS**

---

## Complexity Tracking

> No constitution violations — this section is informational only.

| Decision | Rationale |
|----------|-----------|
| `ITransactionParser` abstraction (not a single monolithic parser) | Enables independent unit testing of CSV vs. OFX paths; allows adding PDF or SWIFT parsers in future without touching the orchestration service. |
| Separate `ClassificationStatus` column on `FinancialRecord` | `RecordStatus` tracks lifecycle (`Pending → Normalized → Reviewed`); `ClassificationStatus` tracks rule engine outcome (`Pending → Classified`). These are orthogonal state machines and must not be conflated. |
| `ImportJob.FailedRows` as JSON column | Row failure data is append-only and queried only by `ImportJobId`; normalizing to a separate table adds FK overhead with no query benefit. |
| `InstitutionProfile.IsDeleted` soft-delete | FR-030 prohibits hard-deleting profiles referenced by past `ImportJob`s; soft-delete satisfies both audit requirement and the "hide from listing" UX expectation. |
| `EvidenceImportService` changed from `Singleton` to `Scoped` | Service now depends on `IFinancialRepository` (scoped EF context) for SHA256 look-up; singleton lifetime would cause a captive dependency issue. |

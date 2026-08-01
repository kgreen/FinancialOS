---
description: "Implementation tasks for Feature 003 — Transaction Parsing & Record Hydration"
feature: "003-transaction-parsing"
spec: "specs/003-transaction-parsing/spec.md"
plan: "specs/003-transaction-parsing/plan.md"
generated: "2026-08-01"
---

# Tasks: Transaction Parsing & Record Hydration

**Feature**: 003 — Transaction Parsing & Record Hydration
**Branch**: `003-transaction-parsing`

**Source documents**:
- `specs/003-transaction-parsing/spec.md` — 4 user stories, 30 FRs
- `specs/003-transaction-parsing/plan.md` — full technical plan
- `specs/003-transaction-parsing/research.md` — all 5 decisions resolved
- `specs/003-transaction-parsing/data-model.md` — entity definitions & EF mappings
- `specs/003-transaction-parsing/contracts/import-jobs.md`
- `specs/003-transaction-parsing/contracts/institution-profiles.md`

**Format**: `[ID] [P?] [Story?] Description — file path`
- **[P]**: parallelizable (independent files, no in-flight dependency)
- **[USn]**: maps to User Story n from spec.md

---

## Phase 1: Setup — Package & Project Scaffolding

**Purpose**: Introduce the one new NuGet dependency and create the empty source files the pipeline will populate. No logic yet. All tasks are independent and parallelizable once `T001` completes.

- [X] T001 Add `CsvHelper` 33.x package reference to `src/FinancialOS.Infrastructure/FinancialOS.Infrastructure.csproj` (`<PackageReference Include="CsvHelper" Version="33.*" />`) and verify `dotnet restore` succeeds
- [X] T002 [P] Create empty file `src/FinancialOS.Core/Models/ImportEntities.cs` with namespace `FinancialOS.Core.Models` and a single placeholder comment `// Transaction parsing domain entities — populated in Phase 2`
- [X] T003 [P] Create empty file `src/FinancialOS.Core/Contracts/ITransactionParser.cs` with namespace `FinancialOS.Core.Contracts` and a single placeholder comment
- [X] T004 [P] Create empty file `src/FinancialOS.Core/Contracts/IImportOrchestrationService.cs` with namespace `FinancialOS.Core.Contracts` and a single placeholder comment
- [X] T005 [P] Create empty file `src/FinancialOS.Infrastructure/Import/Parsers/CsvTransactionParser.cs` (create directory `Import/Parsers/` if absent)
- [X] T006 [P] Create empty file `src/FinancialOS.Infrastructure/Import/Parsers/OfxTransactionParser.cs`
- [X] T007 [P] Create empty file `src/FinancialOS.Infrastructure/Import/Parsers/CsvAutoDetector.cs`
- [X] T008 [P] Create empty file `src/FinancialOS.Infrastructure/Import/ImportOrchestrationService.cs`
- [X] T009 [P] Create empty file `src/FinancialOS.Shared/Contracts/ImportDtos.cs` with namespace `FinancialOS.Shared.Contracts`
- [X] T010 [P] Create empty file `src/FinancialOS.Api/Endpoints/ImportJobEndpoints.cs`
- [X] T011 [P] Create empty file `src/FinancialOS.Api/Endpoints/InstitutionProfileEndpoints.cs`

**Checkpoint**: Solution builds (all files are empty stubs). `dotnet build` passes. `CsvHelper` is resolvable.

---

## Phase 2: Foundational — Domain Entities, Contracts & Migration

**Purpose**: Define all domain types, core interfaces, repository extensions, DTOs, and the EF migration. This phase is the **critical blocker** for all user-story phases — no parser or endpoint work can begin until these contracts are in place.

> ⚠️ **CRITICAL**: No user-story phase (3–6) may begin until this phase is complete and the solution compiles.

### 2.1 — Core Domain Types (`ImportEntities.cs`)

- [X] T012 Add four new enums to `src/FinancialOS.Core/Models/ImportEntities.cs`: `AmountLayout` (`SingleSigned`, `SplitDebitCredit`), `ImportJobStatus` (`Pending`, `Processing`, `Completed`, `PartialSuccess`, `Failed`), `ParserType` (`CsvConfigured`, `CsvAutoDetected`, `Ofx`), and `ClassificationStatus` (`Pending`, `Classified`) — see data-model.md for exact values
- [X] T013 Add `ParsedTransaction` sealed record to `src/FinancialOS.Core/Models/ImportEntities.cs` with fields: `DateOnly TransactionDate`, `decimal Amount`, `string Description`, `decimal? Balance`, `string? ExternalReferenceId`, `int RowIndex`, `string RawRow`
- [X] T014 Add `FailedRowEntry` sealed record to `src/FinancialOS.Core/Models/ImportEntities.cs` with fields: `int RowIndex`, `string Reason`
- [X] T015 Add `InstitutionProfile` sealed class to `src/FinancialOS.Core/Models/ImportEntities.cs` with all properties: `Guid Id`, `string Name`, `Dictionary<string,string> ColumnMappings`, `AmountLayout AmountLayout`, `string? DebitColumnName`, `string? CreditColumnName`, `string? DateFormatPattern`, `DateTimeOffset CreatedAt`, `DateTimeOffset UpdatedAt`, `bool IsDeleted = false`
- [X] T016 Add `ImportJob` sealed class to `src/FinancialOS.Core/Models/ImportEntities.cs` with all properties: `Guid Id`, `Guid EvidenceId`, `Guid? InstitutionProfileId`, `ParserType ParserType`, `ImportJobStatus Status`, `int TotalRows`, `int ParsedCount`, `int FailedRowCount`, `List<FailedRowEntry> FailedRows`, `DateTimeOffset? StartedAt`, `DateTimeOffset? CompletedAt`, `DateTimeOffset CreatedAt`

### 2.2 — `FinancialRecord` Extension

- [X] T017 Add `ClassificationStatus` enum property (from `ImportEntities.cs`) to `FinancialRecord` in `src/FinancialOS.Core/Models/DomainEntities.cs`: `public ClassificationStatus? ClassificationStatus { get; set; }` — nullable, with XML doc comment `// null for legacy records created before spec 003`
- [X] T018 Add remaining new nullable fields to `FinancialRecord` in `src/FinancialOS.Core/Models/DomainEntities.cs`: `Guid? ImportJobId`, `string? ExternalReferenceId`, `int? RowIndex`, `string? ClassificationReasonCode` — each with a doc comment explaining its source (ImportJob FK / OFX FITID or CSV ref / 0-based source row / first reason code from RuleEvaluationResult)

### 2.3 — Core Service Contracts

- [X] T019 Define `TransactionParseResult` sealed record in `src/FinancialOS.Core/Contracts/ITransactionParser.cs`: `IReadOnlyList<ParsedTransaction> Transactions`, `IReadOnlyList<FailedRowEntry> FailedRows`, `int TotalRowsScanned`
- [X] T020 Define `ITransactionParser` interface in `src/FinancialOS.Core/Contracts/ITransactionParser.cs`: `ParserType ParserType { get; }`, `bool CanParse(string fileName, EvidenceSourceType sourceType)`, `Task<TransactionParseResult> ParseAsync(Stream stream, InstitutionProfile? profile, CancellationToken cancellationToken = default)`
- [X] T021 Define `ImportOrchestrationResult` sealed record in `src/FinancialOS.Core/Contracts/IImportOrchestrationService.cs`: `FinancialEvidence Evidence`, `ImportJob Job`, `IReadOnlyList<FinancialRecord> CreatedRecords`, `bool WasDuplicate`
- [X] T022 Define `IImportOrchestrationService` interface in `src/FinancialOS.Core/Contracts/IImportOrchestrationService.cs`: `Task<ImportOrchestrationResult> ImportAsync(string fileName, Stream fileStream, Guid? institutionProfileId, CancellationToken cancellationToken = default)`

### 2.4 — Repository Contract Extensions

- [X] T023 Add `GetEvidenceBySha256Async(string sha256, CancellationToken)` method signature to `IFinancialRepository` in `src/FinancialOS.Core/Contracts/IFinancialRepository.cs` — returns `Task<FinancialEvidence?>`
- [X] T024 [P] Add `ImportJob` CRUD method signatures to `IFinancialRepository` in `src/FinancialOS.Core/Contracts/IFinancialRepository.cs`: `AddImportJobAsync`, `GetImportJobAsync(Guid)`, `UpdateImportJobAsync`, `GetImportJobByEvidenceIdAsync(Guid)` — all returning appropriate `Task<T>` types per plan.md §1.2
- [X] T025 [P] Add `InstitutionProfile` CRUD method signatures to `IFinancialRepository` in `src/FinancialOS.Core/Contracts/IFinancialRepository.cs`: `AddInstitutionProfileAsync`, `GetInstitutionProfileAsync(Guid)`, `ListInstitutionProfilesAsync`, `UpdateInstitutionProfileAsync`, `DeleteInstitutionProfileAsync(Guid)` — per plan.md §1.2
- [X] T026 [P] Add duplicate-detection method signatures to `IFinancialRepository` in `src/FinancialOS.Core/Contracts/IFinancialRepository.cs`: `ExternalReferenceIdExistsAsync(string, CancellationToken)` → `Task<bool>`, and `ListRecordsByImportJobAsync(Guid, CancellationToken)` → `Task<IReadOnlyList<FinancialRecord>>`

### 2.5 — Shared DTOs

- [X] T027 Define all request/response DTOs in `src/FinancialOS.Shared/Contracts/ImportDtos.cs`: `CreateInstitutionProfileRequest`, `UpdateInstitutionProfileRequest`, `InstitutionProfileResponse`, `ImportJobResponse`, `FailedRowDto`, `ImportRecordSummary` — exact field names and types per plan.md §1.5 and contracts/
- [X] T028 Replace (or extend) `EvidenceUploadResponse` in `src/FinancialOS.Shared/Contracts/FinancialDtos.cs` with `EvidenceImportResponse` record: `Guid EvidenceId`, `Guid ImportJobId`, `string Status`, `string ParserType`, `int ParsedTransactionCount`, `int FailedRowCount`, `IReadOnlyList<ImportRecordSummary> Records` — per plan.md §1.5 and contracts/import-jobs.md

### 2.6 — EF Context & Repository

- [X] T029 Add `DbSet<InstitutionProfile> InstitutionProfiles` and `DbSet<ImportJob> ImportJobs` to `src/FinancialOS.Data/FinancialOsDbContext.cs`
- [X] T030 Add `InstitutionProfile` entity configuration block to `FinancialOsDbContext.OnModelCreating` in `src/FinancialOS.Data/FinancialOsDbContext.cs`: PK, required `Name`, `AmountLayout` → string conversion, `ColumnMappings` → JSON column with `DictionaryValueComparer`, unique index `IX_InstitutionProfile_Name_Unique`, global query filter `e => !e.IsDeleted` — per data-model.md EF mapping section
- [X] T031 Add `ImportJob` entity configuration block to `FinancialOsDbContext.OnModelCreating` in `src/FinancialOS.Data/FinancialOsDbContext.cs`: PK, `ParserType` → string, `Status` → string, `FailedRows` → JSON column with `ListValueComparer`, indexes `IX_ImportJob_EvidenceId` and `IX_ImportJob_Status` — per data-model.md EF mapping section
- [X] T032 Add new `FinancialRecord` column mappings to the existing `ConfigureFinancialRecord` block in `src/FinancialOS.Data/FinancialOsDbContext.cs`: `ClassificationStatus` → string column `"ClassificationStatus"`, indexes `IX_FinancialRecord_ImportJobId` and `IX_FinancialRecord_ExternalReferenceId` — per data-model.md §Extended Entity
- [X] T033 Generate EF Core migration `AddTransactionParsing` by running `dotnet ef migrations add AddTransactionParsing --project src/FinancialOS.Data --startup-project src/FinancialOS.Api` — verify the generated `Up()` creates `InstitutionProfiles`, `ImportJobs`, adds 5 nullable columns to `FinancialRecords`, and adds all 6 indexes per data-model.md §EF Migration; review and adjust scaffolded code if auto-generation is incorrect
- [X] T034 Implement all new `IFinancialRepository` methods in `src/FinancialOS.Data/EfFinancialRepository.cs`: `GetEvidenceBySha256Async`, `AddImportJobAsync`, `GetImportJobAsync`, `UpdateImportJobAsync`, `GetImportJobByEvidenceIdAsync`, `AddInstitutionProfileAsync`, `GetInstitutionProfileAsync`, `ListInstitutionProfilesAsync`, `UpdateInstitutionProfileAsync`, `DeleteInstitutionProfileAsync` (checks for ImportJob refs before soft-deleting; returns `false` if referenced), `ExternalReferenceIdExistsAsync`, `ListRecordsByImportJobAsync`

**Checkpoint**: `dotnet build` compiles the full solution. `dotnet ef migrations script` produces valid SQL for both SQLite and PostgreSQL. All `IFinancialRepository` methods are implemented (no `NotImplementedException`). No user-story logic yet.

---

## Phase 3: User Story 1 — Upload a Bank CSV and See Real Transactions (Priority: P1) 🎯 MVP

**Goal**: When a CSV is uploaded via `POST /api/v1/evidence`, the system parses it into individual `FinancialRecord` entries (one per data row), auto-classifies each via the rule engine, and returns a fully populated `EvidenceImportResponse`. No placeholder records are created.

**Independent Test**: Upload a representative 10-row Chase CSV. Verify exactly 10 `FinancialRecord` entries are present, each linked to the evidence by `EvidenceId`. The response `parsedTransactionCount` is 10. No placeholder record exists. Achievable via a single `POST /api/v1/evidence` call.

### CSV Auto-Detector

- [X] T035 [P] [US1] Implement `CsvAutoDetector` class in `src/FinancialOS.Infrastructure/Import/Parsers/CsvAutoDetector.cs`: define the lookup table of 8 known bank CSV header fingerprints (chase-checking, chase-credit, ally-bank, citi-checking, discover, bofa-checking, capital-one, generic-signed) from research.md §R-3; implement `TryDetect(string[] headers, out DetectedCsvLayout? layout)` — normalise headers (lowercase, strip punctuation, collapse whitespace), attempt exact match then generic-signed heuristic, return `null` if no match with the list of actual headers for the error message

### CSV Transaction Parser

- [X] T036 [US1] Implement `CsvTransactionParser` class in `src/FinancialOS.Infrastructure/Import/Parsers/CsvTransactionParser.cs` implementing `ITransactionParser`: constructor accepts `CsvAutoDetector`; set `ParserType = ParserType.CsvConfigured` when a profile is supplied or `CsvAutoDetected` when auto-detected; configure `CsvReader` per research.md §R-2 (`HasHeaderRecord = true`, `MissingFieldFound = null`, `BadDataFound = null`, `TrimOptions = TrimOptions.Trim`); implement `CanParse` returning `true` for `.csv` extension
- [X] T037 [US1] Implement `CsvTransactionParser.ParseAsync` in `src/FinancialOS.Infrastructure/Import/Parsers/CsvTransactionParser.cs`: read header row → resolve column indices from `InstitutionProfile.ColumnMappings` or auto-detected layout; iterate rows and for each row: parse `TransactionDate` using `DateFormatPattern` (or try `MM/dd/yyyy`, `yyyy-MM-dd`, `dd-MMM-yyyy` in order), parse `Amount` (for `SplitDebitCredit` layout: credit positive, debit stored negative; row with both populated is a `FailedRowEntry`), build `ParsedTransaction`; on any required-field failure, append `FailedRowEntry` with row index and human-readable reason, continue to next row; return `TransactionParseResult` with all transactions and failures
- [X] T038 [US1] Add within-file duplicate fingerprint detection to `CsvTransactionParser.ParseAsync` in `src/FinancialOS.Infrastructure/Import/Parsers/CsvTransactionParser.cs` (FR-019): maintain a `HashSet<string>` of `"{date}|{amount}|{description}"` fingerprints per file; if a row's fingerprint already exists, append it as `FailedRowEntry` with reason `"Duplicate row within file"` and skip; do not add to `Transactions`

### Import Orchestration Service (CSV path)

- [X] T039 [US1] Implement `ImportOrchestrationService` constructor in `src/FinancialOS.Infrastructure/Import/ImportOrchestrationService.cs` with injected dependencies: `IFinancialRepository`, `IEnumerable<ITransactionParser>`, `IRuleEvaluationService`, `IProvenanceWriter` (or equivalent provenance-writing interface from spec 001/002)
- [X] T040 [US1] Implement `ImportOrchestrationService.ImportAsync` in `src/FinancialOS.Infrastructure/Import/ImportOrchestrationService.cs` — orchestrate the full pipeline from plan.md §1.4: (1) resolve `InstitutionProfile` if `institutionProfileId` is provided; (2) select parser via `CanParse`; (3) create `ImportJob` with `Status = Processing`; (4) call `parser.ParseAsync`; (5) for each `ParsedTransaction`: check `ExternalReferenceIdExistsAsync` for OFX cross-import dedup (FR-020), hydrate `FinancialRecord` with all provenance fields, call `IRuleEvaluationService.EvaluateAsync`, set `ClassificationStatus` and fields, persist record, write provenance entry; (6) finalise `ImportJob` status per data-model.md status-transition table; (7) return `ImportOrchestrationResult`

### EvidenceImportService (SHA256 dedup + format guard)

- [X] T041 [US1] Change `EvidenceImportService` registration from `AddSingleton` to `AddScoped` in `src/FinancialOS.Api/Program.cs` — per research.md §R-5 captive dependency fix; update the DI call on the same line to avoid breaking startup
- [X] T042 [US1] Extend `EvidenceImportService` in `src/FinancialOS.Infrastructure/Import/EvidenceImportService.cs` to: (a) reject zero-byte files (→ throw with `BadRequest` indicator); (b) reject unsupported extensions (`.pdf`, `.xlsx`, etc.) returning a format-error indicator; (c) after file write, call `IFinancialRepository.GetEvidenceBySha256Async` before inserting a new `FinancialEvidence` row — return the existing evidence and its associated `ImportJob` if a SHA256 match is found (FR-018 duplicate detection)

### Evidence Upload Endpoint (updated)

- [X] T043 [US1] Update `POST /api/v1/evidence` handler in `src/FinancialOS.Api/` (wherever it currently lives, likely `EvidenceEndpoints.cs` or `Program.cs`) to: (a) accept optional `institutionProfileId` form field; (b) call `EvidenceImportService.ImportAsync` for file storage + dedup check; (c) if duplicate → return `EvidenceImportResponse` with `status = "duplicate"` and existing `importJobId`; (d) call `IImportOrchestrationService.ImportAsync`; (e) map `ImportOrchestrationResult` → `EvidenceImportResponse` (populate `records` array from `CreatedRecords` using `ImportRecordSummary`); (f) remove the existing placeholder-record creation for `.csv`, `.ofx`, `.qfx` extensions; (g) return `200 OK` per contracts/import-jobs.md response schema
- [X] T044 [US1] Add `422 Unprocessable Entity` and `400 Bad Request` error handling to the `POST /api/v1/evidence` handler (updated in T043) in `src/FinancialOS.Api/` for unsupported file format and zero-byte file cases — return RFC 9110 problem-detail JSON matching contracts/import-jobs.md error response shapes

### Service Registration (CSV path)

- [X] T045 [US1] Register new services in `src/FinancialOS.Api/Program.cs`: `AddScoped<ITransactionParser, CsvTransactionParser>()`, `AddScoped<ITransactionParser, OfxTransactionParser>()`, `AddScoped<CsvAutoDetector>()`, `AddScoped<IImportOrchestrationService, ImportOrchestrationService>()` — register `ITransactionParser` as a collection (so `IEnumerable<ITransactionParser>` resolves both implementations)

**Checkpoint**: POST a 10-row Chase CSV → response contains `parsedTransactionCount: 10`, `records` array has 10 entries each with `date`, `amount`, `description`, `classificationStatus`. No placeholder record. Database has 10 `FinancialRecords` linked by `EvidenceId` and `ImportJobId`.

---

## Phase 4: User Story 2 — Upload an OFX/QFX File from Any Institution (Priority: P1)

**Goal**: Upload any OFX 1.x (SGML) or OFX 2.x (XML) or QFX file and receive one `FinancialRecord` per `STMTTRN` element — no institution profile required. Amounts are signed as-is. `FITID` stored as `ExternalReferenceId`.

**Independent Test**: Upload a 5-transaction OFX file. Verify 5 records exist, `date` from `DTPOSTED`, `amount` from `TRNAMT` (sign preserved), `description` from `NAME` (or `MEMO` fallback), `ExternalReferenceId` = `FITID`. Test is independent of Phase 3 — uses a different file type.

### OFX SGML Tokenizer

- [X] T046 [P] [US2] Implement OFX 1.x SGML tokenizer as a private method `ParseSgml(string content)` inside `OfxTransactionParser` in `src/FinancialOS.Infrastructure/Import/Parsers/OfxTransactionParser.cs`: (1) discard all content before the first `<OFX>` tag; (2) use regex `<(\w+)>([^<\r\n]*)` to accumulate tag-value pairs within each `<STMTTRN>…</STMTTRN>` block; (3) map `DTPOSTED` → `TransactionDate` (OFX date format `yyyyMMddHHmmss[.xxx]` or `yyyyMMdd`), `TRNAMT` → `Amount` (signed decimal), `NAME` → `Description` (fallback to `MEMO` if `NAME` absent/blank), `FITID` → `ExternalReferenceId`; (4) if `DTPOSTED` is missing or unparseable, append `FailedRowEntry` and skip; if `TRNAMT` is missing or not a valid decimal, append `FailedRowEntry` and skip; (5) return `List<ParsedTransaction>` and `List<FailedRowEntry>` — per research.md §R-1 implementation pattern

### OFX Transaction Parser

- [X] T047 [US2] Implement `OfxTransactionParser` class in `src/FinancialOS.Infrastructure/Import/Parsers/OfxTransactionParser.cs` implementing `ITransactionParser`: `ParserType = ParserType.Ofx`; `CanParse` returns `true` for `.ofx` and `.qfx` extensions (QFX treated identically to OFX — FR-010)
- [X] T048 [US2] Implement `OfxTransactionParser.ParseAsync` in `src/FinancialOS.Infrastructure/Import/Parsers/OfxTransactionParser.cs`: peek the first non-whitespace bytes of the stream to detect format — if stream starts with `OFXHEADER:` or `DATA:OFXSGML` → invoke the SGML tokenizer path (T046); if starts with `<?xml` or `<OFX>` with XML declaration → parse via `XDocument.Load()` and extract `STMTTRN` elements using LINQ to XML with the same field mappings; otherwise → throw `FileFormatException("Not a recognizable OFX/QFX file")` triggering a file-level `Failed` status on the `ImportJob` (FR-011)
- [X] T049 [US2] Add within-file `FITID` duplicate detection to `OfxTransactionParser.ParseAsync` in `src/FinancialOS.Infrastructure/Import/Parsers/OfxTransactionParser.cs` (FR-019): maintain a `HashSet<string>` of `FITID` values seen in the current file; if a `FITID` repeats, record `FailedRowEntry` with reason `"Duplicate FITID within file: {fitid}"` and skip the second occurrence

**Checkpoint**: POST a 5-STMTTRN OFX file → `parsedTransactionCount: 5`, each record's `amount` has correct sign, `ExternalReferenceId` equals `FITID`. POST the same file again → `status: "duplicate"`, 0 new records.

---

## Phase 5: User Story 3 — Define a CSV Institution Profile for a New Bank (Priority: P2)

**Goal**: A user can create, retrieve, update, and soft-delete an `InstitutionProfile` via the API. Once created, a profile can be referenced by a CSV upload via `institutionProfileId`, and the `CsvTransactionParser` applies the custom column mappings — enabling non-standard headers to be parsed correctly.

**Independent Test**: `POST /api/v1/institution-profiles` with a Citi split-debit/credit mapping → `201 Created` with profile ID. Then `POST /api/v1/evidence` with a Citi CSV and `institutionProfileId` → records show correctly signed amounts. Independent of US1 because no prior state is needed.

### Institution Profile Validation

- [X] T050 [P] [US3] Add `InstitutionProfileValidator` (or inline validation logic) to `src/FinancialOS.Infrastructure/` or within the endpoint handler: (a) `Name` required, max 200 chars; (b) `ColumnMappings` must contain `"date"` key always and `"amount"` key when `amountLayout = singleSigned`; (c) when `amountLayout = splitDebitCredit`: both `DebitColumnName` and `CreditColumnName` must be non-null and non-empty; (d) if `DateFormatPattern` is provided, validate via `DateTime.TryParseExact` with a test date — per data-model.md validation rules

### Institution Profile Endpoints

- [X] T051 [US3] Implement `POST /api/v1/institution-profiles` in `src/FinancialOS.Api/Endpoints/InstitutionProfileEndpoints.cs`: validate `CreateInstitutionProfileRequest` (call T050 logic); call `IFinancialRepository.AddInstitutionProfileAsync`; on `DbUpdateException` with unique-constraint violation → return `409 Conflict` per contracts/institution-profiles.md; on success → `201 Created` with `Location` header and `InstitutionProfileResponse` body
- [X] T052 [P] [US3] Implement `GET /api/v1/institution-profiles` in `src/FinancialOS.Api/Endpoints/InstitutionProfileEndpoints.cs`: call `IFinancialRepository.ListInstitutionProfilesAsync` (EF global query filter already excludes `IsDeleted`); map to `IReadOnlyList<InstitutionProfileResponse>`; return `200 OK`
- [X] T053 [P] [US3] Implement `GET /api/v1/institution-profiles/{id}` in `src/FinancialOS.Api/Endpoints/InstitutionProfileEndpoints.cs`: call `IFinancialRepository.GetInstitutionProfileAsync(id)`; return `404 Not Found` with problem-detail if null; otherwise return `200 OK` with `InstitutionProfileResponse`
- [X] T054 [US3] Implement `PUT /api/v1/institution-profiles/{id}` in `src/FinancialOS.Api/Endpoints/InstitutionProfileEndpoints.cs`: fetch profile; return `404` if missing; validate `UpdateInstitutionProfileRequest` (same rules as T050); update fields and set `UpdatedAt = DateTimeOffset.UtcNow`; call `IFinancialRepository.UpdateInstitutionProfileAsync`; return `200 OK` with updated `InstitutionProfileResponse`
- [X] T055 [US3] Implement `DELETE /api/v1/institution-profiles/{id}` in `src/FinancialOS.Api/Endpoints/InstitutionProfileEndpoints.cs`: call `IFinancialRepository.DeleteInstitutionProfileAsync(id)` — returns `false` if the profile has been referenced by any `ImportJob`; if `false` → return `409 Conflict` per contracts/institution-profiles.md with import count in message; if `true` → return `204 No Content`; if profile not found → `404 Not Found`

### Register Endpoints

- [X] T056 [US3] Register institution-profile endpoint group in `src/FinancialOS.Api/Program.cs`: call `app.MapInstitutionProfileEndpoints()` after existing endpoint registrations

**Checkpoint**: `POST /api/v1/institution-profiles` with Chase mapping → `201`. `GET /api/v1/institution-profiles/{id}` → same profile. Upload a Chase CSV with `institutionProfileId` → `parsedTransactionCount` matches row count, `parserType: "csvConfigured"`. Attempt to delete a used profile → `409 Conflict`.

---

## Phase 6: User Story 4 — Review Import Results and Handle Failed Rows (Priority: P2)

**Goal**: The `GET /api/v1/import-jobs/{id}` endpoint returns the full `ImportJob` detail including `TotalRows`, `ParsedCount`, `FailedRowCount`, `Status`, and a `FailedRows` array with row index and reason per skipped row. Import status correctly reflects mixed/all-failed outcomes.

**Independent Test**: Upload a CSV with 2 malformed rows and 8 valid rows. Call `GET /api/v1/import-jobs/{id}`. Verify `parsedCount: 8`, `failedRowCount: 2`, `status: "partialSuccess"`, and `failedRows` array contains both entries with row indexes and reason strings.

### Import Job Status Finalisation

- [X] T057 [US4] Verify (and if necessary correct) `ImportJob` status-transition logic in `ImportOrchestrationService.ImportAsync` in `src/FinancialOS.Infrastructure/Import/ImportOrchestrationService.cs`: if `ParsedCount == TotalRows && FailedRowCount == 0` → `Completed`; if `ParsedCount > 0 && FailedRowCount > 0` → `PartialSuccess`; if `ParsedCount == 0` (all failed or file-level error) → `Failed`; if `TotalRows == 0` (empty file, no data rows) → `Completed` with `ParsedCount = 0`; per data-model.md status-transition table

### Import Job Endpoint

- [X] T058 [US4] Implement `GET /api/v1/import-jobs/{id}` in `src/FinancialOS.Api/Endpoints/ImportJobEndpoints.cs`: call `IFinancialRepository.GetImportJobAsync(id)`; return `404 Not Found` with problem-detail if null; map to `ImportJobResponse` DTO (all fields including full `FailedRows` array); return `200 OK` per contracts/import-jobs.md §Endpoint: GET /api/v1/import-jobs/{id}

### Register Endpoints

- [X] T059 [US4] Register import-job endpoint group in `src/FinancialOS.Api/Program.cs`: call `app.MapImportJobEndpoints()` after existing endpoint registrations

### Edge Cases in Orchestration

- [X] T060 [US4] Implement empty-file handling in `ImportOrchestrationService.ImportAsync` in `src/FinancialOS.Infrastructure/Import/ImportOrchestrationService.cs`: if `parser.ParseAsync` returns `TotalRowsScanned == 0` with no failures, set `ImportJob.Status = Completed`, `TotalRows = 0`, `ParsedCount = 0`, `FailedRowCount = 0` — no records created, no error; per spec edge case "Empty file" and SC-004
- [X] T061 [US4] Implement all-rows-failed handling in `ImportOrchestrationService.ImportAsync` in `src/FinancialOS.Infrastructure/Import/ImportOrchestrationService.cs`: if `ParsedCount == 0` after iterating all rows (all were bad) → set `Status = Failed`, ensure zero `FinancialRecord` rows were persisted (rollback or never added), original evidence file remains intact; per spec User Story 4, Acceptance Scenario 2 and FR-025

**Checkpoint**: `GET /api/v1/import-jobs/{id}` on a partial-success import returns correct `parsedCount`, `failedRowCount`, `status: "partialSuccess"`, and `failedRows` array. An all-failed import shows `status: "failed"` with `parsedCount: 0`. An empty CSV shows `status: "completed"` with `parsedCount: 0` and empty `failedRows`.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Hardening, serialization consistency, JSON enum casing, and any cross-story issues that span multiple phases.

- [X] T062 [P] Configure `System.Text.Json` enum serialisation in `src/FinancialOS.Api/Program.cs` (or shared JSON options): add `JsonStringEnumConverter` with `camelCase` naming for `AmountLayout`, `ImportJobStatus`, `ParserType`, `ClassificationStatus` — serialised values must match the contract-specified strings (`"singleSigned"`, `"splitDebitCredit"`, `"csvConfigured"`, `"csvAutoDetected"`, `"ofx"`, `"pending"`, `"classified"`, `"completed"`, `"partialSuccess"`, `"failed"`) per contracts/
- [X] T063 [P] Implement `null` → `"pending"` serialisation for `FinancialRecord.ClassificationStatus` in `src/FinancialOS.Shared/Contracts/ImportDtos.cs` or in the `ImportRecordSummary` mapping: legacy records with `ClassificationStatus == null` must serialise as `"pending"` in the `records` array of `EvidenceImportResponse` — per research.md §R-4 backward-compatibility note
- [X] T064 [P] Implement `EvidenceImportResponse.status = "duplicate"` path in `POST /api/v1/evidence` handler: when `EvidenceImportService` returns an existing evidence match (T042 SHA256 dedup), return `200 OK` with `status = "duplicate"`, the existing `importJobId`, `parsedTransactionCount: 0`, `failedRowCount: 0`, `records: []` — per contracts/import-jobs.md duplicate response shape
- [X] T065 [P] Add unknown-CSV-layout `422 Unprocessable Entity` error response to `POST /api/v1/evidence` handler when `CsvAutoDetector` returns no match and no `institutionProfileId` was supplied: include actual detected headers in the `detail` message per contracts/import-jobs.md `422` error shape (`"Could not auto-detect CSV layout. Detected headers: [...]"`)
- [X] T066 [P] Add OFX file-level rejection `422 Unprocessable Entity` to `POST /api/v1/evidence` handler when `OfxTransactionParser.ParseAsync` throws `FileFormatException` (FR-011): return problem-detail with format error; ensure no `ImportJob` row is left in `Processing` status after a file-level failure (update to `Failed` before returning)
- [X] T067 [P] Verify SQLite and PostgreSQL migration compatibility: run `dotnet ef migrations script --idempotent` against both providers; confirm all 5 nullable `FinancialRecord` columns use `NULL` default (no `NOT NULL` without default), no `ALTER COLUMN` operations that SQLite does not support — per research.md §R-5 migration safety notes
- [X] T068 Run `specs/003-transaction-parsing/quickstart.md` validation scenarios end-to-end against the completed implementation; document any deviations

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)          → no dependencies; start immediately
Phase 2 (Foundational)   → depends on Phase 1 completion; BLOCKS Phases 3–6
Phase 3 (US1 — CSV)      → depends on Phase 2; can run in parallel with Phase 4
Phase 4 (US2 — OFX)      → depends on Phase 2; can run in parallel with Phase 3
Phase 5 (US3 — Profiles) → depends on Phase 2; requires Phase 3 for profile-driven CSV upload test
Phase 6 (US4 — Results)  → depends on Phase 3 and Phase 4 (ImportJob status set by orchestrator)
Phase 7 (Polish)         → depends on Phases 3–6 all complete
```

### User Story Dependencies

| Story | Depends on | Can start in parallel with |
|-------|-----------|---------------------------|
| US1 (CSV upload, Phase 3) | Phase 2 | US2 (Phase 4) |
| US2 (OFX upload, Phase 4) | Phase 2 | US1 (Phase 3) |
| US3 (Profiles, Phase 5) | Phase 2, Phase 3 (CsvTransactionParser exists) | US2 |
| US4 (Import results, Phase 6) | Phase 3 + Phase 4 (ImportJob created by both) | Phase 5 |

### Within Each Phase

- Phase 1: all tasks parallelizable after T001 (CsvHelper must be resolvable first)
- Phase 2: §2.1–2.5 (T012–T028) are parallelizable within groups; T029–T034 (EF/Data) depend on §2.1 types being defined
- Phase 3: T035 (AutoDetector) and T036 (Parser class) are parallelizable; T037–T038 depend on T036; T039 depends on T035+T036; T040 depends on T039; T041–T044 depend on T040
- Phase 4: T046–T049 build on each other sequentially; Phase 4 is otherwise independent of Phase 3
- Phase 5: T050 (validation) and T052/T053 read-only endpoints are parallelizable; T054/T055 depend on T050
- Phase 6: T057 (status logic) → T058 (endpoint) → T059 (registration); T060–T061 can be implemented alongside T057

### Parallel Opportunities by Phase

```bash
# Phase 1 — after T001:
T002, T003, T004, T005, T006, T007, T008, T009, T010, T011  # all in parallel

# Phase 2 — after T012 (enums defined):
T013, T014, T015, T016   # Core types — sequential within ImportEntities.cs
T017, T018               # FinancialRecord extension — can run alongside T013–T016
T019–T022                # ITransactionParser + IImportOrchestrationService in parallel
T023–T026                # IFinancialRepository additions in parallel
T027, T028               # Shared DTOs in parallel

# Phase 3 (US1) + Phase 4 (US2) — after Phase 2:
Phase 3 tasks and Phase 4 tasks run on separate files — full parallelism possible

# Phase 5 (US3):
T050, T052, T053         # Validation + GET endpoints in parallel
```

---

## Implementation Strategy

### MVP Scope — User Stories 1 & 2 (Phases 1–4)

The minimum viable feature is **both P1 stories**: CSV upload and OFX upload. Together they deliver the core value promise of the feature ("upload a file, see transactions").

1. Complete **Phase 1** (Setup) — ~30 min
2. Complete **Phase 2** (Foundational) — ~2–3 hrs; blocks everything
3. Complete **Phase 3** (US1 — CSV) **and Phase 4** (US2 — OFX) in parallel if team capacity allows — ~3–4 hrs each
4. **STOP and VALIDATE**: POST a Chase CSV → 10 records. POST an OFX file → 5 records. SHA256 re-upload → duplicate response.
5. **Deploy / demo** this increment.

### Incremental Delivery Beyond MVP

| Increment | Phases | New capability |
|-----------|--------|----------------|
| MVP | 1 + 2 + 3 + 4 | CSV + OFX parsing, classification, import response |
| +Profile Management | + 5 | Custom bank CSV mappings via InstitutionProfile CRUD |
| +Import Auditing | + 6 | Retrievable import-job detail with per-row failure log |
| +Polish | + 7 | Serialization correctness, edge cases, validation scenarios |

### Parallel Team Strategy

With two developers after Phase 2 completes:
- **Developer A**: Phase 3 (US1 — CSV parsing pipeline)
- **Developer B**: Phase 4 (US2 — OFX parsing pipeline)
- After both complete: **Developer A** → Phase 5 (Profiles); **Developer B** → Phase 6 (Import results)
- Both join Phase 7 (Polish)

---

## Notes

- **[P]** tasks touch different files with no dependency on each other's output — safe to run in parallel
- **[USn]** label maps each task to its user story for traceability and independent testability
- The `EvidenceImportService` Singleton → Scoped change (T041) **must** be applied before any other DI registration for import services (T045) to avoid a captive dependency startup crash
- The EF migration (T033) must run `dotnet ef migrations add` — do not hand-author the migration file; let EF scaffold it and review the output
- `ITransactionParser` is registered as a collection: `AddScoped<ITransactionParser, CsvTransactionParser>()` followed by `AddScoped<ITransactionParser, OfxTransactionParser>()` — the orchestration service receives `IEnumerable<ITransactionParser>` and dispatches via `CanParse`
- All new `FinancialRecord` columns are nullable — migration is non-destructive on both SQLite and PostgreSQL (no `NOT NULL` without default)
- `ClassificationStatus` is a **new, separate enum** from `RecordStatus` — do not conflate them (research.md §R-4 decision B)
- `ImportJob.FailedRows` is stored as a **JSON column** (not a related table) — no FK or join needed to retrieve failure details

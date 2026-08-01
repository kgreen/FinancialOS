# Feature Specification: Transaction Parsing & Record Hydration

**Feature Branch**: `003-transaction-parsing`

**Created**: 2026-08-01

**Status**: Draft

**Milestone**: 003 — Transaction Parsing & Record Hydration

## Overview

Evidence upload (spec 001) stores raw financial files immutably and creates a single placeholder record with no transaction data. This feature closes that gap: when a file is uploaded, the platform parses it into individual transactions, creates a fully hydrated `FinancialRecord` for each one, auto-classifies records using the rule engine (spec 002), and returns a structured import summary to the caller.

Supported file formats at launch are **CSV** (bank-exported) and **OFX/QFX** (Open Financial Exchange). Each bank's CSV dialect is described by a saved **InstitutionProfile** so the parser knows which column maps to which field. Every created record carries an immutable provenance link back to its source evidence artifact.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Upload a bank CSV and see real transactions (Priority: P1)

A user exports their checking account from their bank's website as a CSV file and uploads it to FinancialOS. Instead of seeing one placeholder record, they immediately see one `FinancialRecord` per transaction row, each with a real date, amount, description, and account classification applied where a matching rule exists.

**Why this priority**: This is the foundational value unlock. Without it, evidence upload is a file-storage feature with no financial meaning. Every downstream capability (budgeting, reconciliation, insights) depends on transactions existing in the system.

**Independent Test**: Upload a representative 10-row Chase CSV. Verify that exactly 10 `FinancialRecord` entries appear, each linked to the uploaded evidence. The placeholder record must not exist. Can be tested end-to-end via a single `POST /api/v1/evidence` call.

**Acceptance Scenarios**:

1. **Given** a valid Chase CSV with 10 transaction rows, **When** the file is uploaded via `POST /api/v1/evidence`, **Then** the response includes `parsedTransactionCount: 10` and 10 `FinancialRecord` entries are created, each linked to the evidence by `SourceEvidenceId`.
2. **Given** an uploaded CSV where 3 of 10 rows match existing classification rules, **When** the import completes, **Then** those 3 records carry a `Classified` status with confidence score and reason code; the remaining 7 are in `Pending` status.
3. **Given** a CSV row with a missing required field (date or amount), **When** parsing encounters it, **Then** that row is skipped with a per-row error recorded in the `ImportJob`; all valid rows are still processed and created.
4. **Given** the same evidence file is uploaded a second time (same SHA256), **When** the duplicate is detected, **Then** the response indicates the file was already imported, no new records are created, and the existing `ImportJob` reference is returned.

---

### User Story 2 — Upload an OFX/QFX file from any institution (Priority: P1)

A user downloads a QFX file from their investment brokerage or savings account (OFX format is standardized across institutions) and uploads it. The platform parses all `STMTTRN` elements and hydrates records without the user needing to configure anything, because OFX has a defined field layout.

**Why this priority**: OFX/QFX is the most universally supported export format. Many institutions do not offer CSV, or their CSV layout is inconsistent. OFX parsing gives broad institution coverage without profile setup.

**Independent Test**: Upload a standard OFX file with 5 `STMTTRN` elements. Verify 5 records are created with correct dates, amounts (signed correctly: debits negative, credits positive), and the OFX `FITID` stored as the external reference.

**Acceptance Scenarios**:

1. **Given** a valid OFX file with 5 `STMTTRN` elements, **When** uploaded, **Then** exactly 5 `FinancialRecord` entries are created with dates from `DTPOSTED`, amounts from `TRNAMT`, descriptions from `NAME` (falling back to `MEMO` if `NAME` is absent), and `ExternalId` set to `FITID`.
2. **Given** an OFX file where `TRNAMT` is negative (a debit), **When** parsed, **Then** the resulting record amount is stored as-is (negative), correctly representing money leaving the account.
3. **Given** an OFX file with a malformed or missing `DTPOSTED` on one element, **When** parsing encounters it, **Then** that element is skipped with a row-level error; valid elements are still created.
4. **Given** a QFX file (Quicken variant of OFX), **When** uploaded, **Then** it is parsed identically to OFX; the format difference is transparent to the user.

---

### User Story 3 — Define a CSV institution profile for a new bank (Priority: P2)

A user's credit union exports CSVs with non-standard column headers (`Trans Date`, `Debit`, `Credit`, `Running Balance`). Rather than being rejected, the user creates an **InstitutionProfile** that maps these columns to the standard fields. Once saved, any future upload tagged with that profile parses correctly.

**Why this priority**: Without profile management, every CSV bank that uses non-standard headers is unsupported. Profile management makes the CSV parser open-ended and institution-agnostic.

**Independent Test**: Create a profile via `POST /api/v1/institution-profiles`. Upload a CSV tagged with that profile ID. Verify records are created using the defined column mapping.

**Acceptance Scenarios**:

1. **Given** a new institution profile with custom column mappings, **When** saved via the API, **Then** the profile is persisted with a unique ID and the caller receives the profile details including all column assignments.
2. **Given** a CSV where separate `Debit` and `Credit` columns exist (split-amount layout), **When** parsed with a profile that declares split-amount mode, **Then** each row produces a signed amount: credit values are positive, debit values are negative, and a row with both populated is flagged as a row-level error.
3. **Given** a CSV uploaded without specifying a profile, **When** the platform cannot auto-detect the column layout, **Then** the import fails with a clear error message identifying which columns were found and what is needed.
4. **Given** an existing profile, **When** updated via the API, **Then** new uploads using that profile use the updated mappings; previously created records are unaffected.

---

### User Story 4 — Review import results and handle failed rows (Priority: P2)

After uploading a file, the user wants to see a structured summary: how many rows were parsed, how many records were created, how many rows were skipped, and why each skip occurred. They want to be able to retrieve this summary later without re-uploading.

**Why this priority**: Without a retrievable import result, users have no way to audit what happened during parsing. Failed rows are silently lost.

**Independent Test**: Upload a CSV with 2 malformed rows and 8 valid rows. Query the `ImportJob` by ID. Verify `ParsedCount: 8`, `FailedRowCount: 2`, and each failed row entry shows row number and reason.

**Acceptance Scenarios**:

1. **Given** a completed import, **When** the caller queries `GET /api/v1/import-jobs/{id}`, **Then** the response includes `TotalRows`, `ParsedCount`, `FailedRowCount`, `Status`, `EvidenceId`, and a `FailedRows` array with row index and reason per failure.
2. **Given** an import where every row failed, **When** the job completes, **Then** status is `Failed` (not `Completed`), no `FinancialRecord` entries are created, and the original evidence file is still intact.
3. **Given** an import that partially succeeded, **When** the job completes, **Then** status is `PartialSuccess`, records exist for all valid rows, and the caller can identify exactly which rows need manual entry.

---

### Edge Cases

- **Empty file**: A CSV with only a header row (no data rows) or an OFX file with zero `STMTTRN` elements produces a completed `ImportJob` with `ParsedCount: 0`, zero records created, and no error — this is a valid but empty import.
- **Completely blank file**: A zero-byte or whitespace-only file is rejected at the evidence layer before parsing begins; the import never starts.
- **Unknown file type**: A `.pdf`, `.xlsx`, or unsupported extension is rejected with a clear format error; no `ImportJob` is created.
- **Duplicate evidence re-import**: Re-uploading a file with a matching SHA256 returns the existing `ImportJob` ID and does not re-parse; evidence and records are untouched.
- **Duplicate transactions within a file**: If the same `FITID` (OFX) or the same row data appears twice in one file, the second occurrence is flagged as a row-level duplicate and skipped; the first is created normally.
- **Cross-upload duplicate detection**: If an OFX transaction's `FITID` already exists in the system from a prior import, the new record is suppressed and the row is logged as a cross-file duplicate.
- **Extremely large files**: Files with thousands of rows complete without timeout; the import runs as a background job if the row count exceeds a configurable threshold.
- **Malformed OFX**: An OFX file that is not well-formed XML (or SGML) is rejected at the file-level; no rows are partially parsed.
- **Amount sign convention conflicts**: Some banks export all amounts as positive with a separate Dr/Cr indicator column. Institution profiles must be able to declare this layout; the parser normalizes to signed amounts.
- **Date format variations**: CSV dates may appear as `MM/DD/YYYY`, `YYYY-MM-DD`, `DD-Mon-YYYY`, etc. The institution profile can declare the expected date format; if unspecified, the parser attempts common formats in order and records a warning when ambiguous.

---

## Requirements *(mandatory)*

### Functional Requirements

#### CSV Parsing

- **FR-001**: The system MUST parse bank-exported CSV files into `ParsedTransaction` records, extracting at minimum: transaction date, amount, description, and optional balance and external reference fields.
- **FR-002**: The system MUST support configurable column mappings via `InstitutionProfile`, allowing any header name to be mapped to a standard field (date, amount, description, balance, reference).
- **FR-003**: The system MUST support split-amount column layouts where separate debit and credit columns represent money movement; the parser normalizes these to a single signed decimal amount.
- **FR-004**: The system MUST skip malformed rows (missing required fields, unparseable amounts, unparseable dates) and record the row index and a human-readable reason in the `ImportJob`; valid rows in the same file must still be processed.
- **FR-005**: The system MUST attempt auto-detection of common CSV column layouts (e.g., Chase, Ally, Citi standard exports) before requiring an explicit `InstitutionProfile`.
- **FR-006**: The system MUST allow an `InstitutionProfile` to declare the expected date format pattern; if absent, the parser tries common formats in order.

#### OFX/QFX Parsing

- **FR-007**: The system MUST parse OFX 1.x (SGML) and OFX 2.x (XML) files, extracting one `ParsedTransaction` per `STMTTRN` element.
- **FR-008**: The system MUST map OFX fields as follows: `DTPOSTED` → transaction date, `TRNAMT` → amount (signed), `NAME` → description (primary), `MEMO` → description (fallback when `NAME` is absent or blank), `FITID` → external reference ID.
- **FR-009**: The system MUST preserve `TRNAMT` sign semantics: negative values represent debits; positive values represent credits.
- **FR-010**: The system MUST treat QFX files (Quicken variant) identically to OFX files during parsing.
- **FR-011**: The system MUST reject entirely malformed OFX/QFX files (not parseable as SGML or XML) with a file-level error before attempting row-level parsing.

#### Evidence-to-Records Hydration Pipeline

- **FR-012**: The system MUST NOT modify the raw evidence artifact at any stage; parsing creates derivative records, never overwrites or transforms stored evidence.
- **FR-013**: The system MUST create exactly one `FinancialRecord` per successfully parsed `ParsedTransaction` and link it to the source evidence via `SourceEvidenceId`.
- **FR-014**: The system MUST NOT create the old placeholder `FinancialRecord` ($0, no line items) when a parser is available for the uploaded file type; placeholder creation is retired for supported formats.
- **FR-015**: The system MUST invoke the rule evaluation service (spec 002) on each newly created `FinancialRecord` immediately after hydration; matched rules apply classification with a confidence score and reason code.
- **FR-016**: Records that match no rule MUST be created in `Pending` status; classified records MUST be in `Classified` status with confidence score and reason code attached.
- **FR-017**: Every created `FinancialRecord` MUST carry a provenance entry identifying the source `EvidenceId`, the `ImportJobId`, the parser type used, and the original row index (CSV) or `FITID` (OFX).
- **FR-018**: The system MUST detect duplicate evidence by SHA256 hash before parsing begins; if a matching hash exists and was already parsed, the system MUST return the existing `ImportJob` without creating new records.
- **FR-019**: The system MUST detect duplicate transactions within a single import (same `FITID` for OFX, identical row fingerprint for CSV) and suppress the second occurrence as a row-level duplicate.
- **FR-020**: The system MUST detect cross-import OFX duplicates by `FITID`; if a matching `FITID` exists in the database from a prior import, the record is suppressed and logged.
- **FR-021**: The system MUST support both SQLite and PostgreSQL as backing stores without parser or hydration code changes.

#### Import Result & Response

- **FR-022**: The `POST /api/v1/evidence` response MUST include `importJobId`, `parsedTransactionCount`, `failedRowCount`, `status`, and a `records` array with one entry per created `FinancialRecord` (id, date, amount, description, classificationStatus).
- **FR-023**: The system MUST expose `GET /api/v1/import-jobs/{id}` returning the full `ImportJob` detail including `TotalRows`, `ParsedCount`, `FailedRowCount`, `Status`, `EvidenceId`, `ParserType`, and a `FailedRows` array with row index and reason per failure.
- **FR-024**: `ImportJob` status MUST be one of: `Pending`, `Processing`, `Completed`, `PartialSuccess`, `Failed`.
- **FR-025**: A file where every row fails parsing MUST yield an `ImportJob` with status `Failed` and zero `FinancialRecord` entries created.

#### Institution Profile Management

- **FR-026**: The system MUST expose `POST /api/v1/institution-profiles` to create a new profile with name, column mappings, date format, and amount layout declaration.
- **FR-027**: The system MUST expose `GET /api/v1/institution-profiles` to list all saved profiles and `GET /api/v1/institution-profiles/{id}` to retrieve one profile.
- **FR-028**: The system MUST expose `PUT /api/v1/institution-profiles/{id}` to update a profile; changes affect future imports only and do not retroactively alter existing records.
- **FR-029**: The system MUST allow a CSV upload to reference an `InstitutionProfile` by ID via request metadata (e.g., a form field or header); if absent, auto-detection is attempted.
- **FR-030**: Deleting an `InstitutionProfile` that has been used in prior imports MUST be rejected; the profile must be retained for historical auditability.

### Key Entities

- **`ParsedTransaction`** *(transient DTO, not persisted)*: Intermediate transfer object produced by a parser. Contains: `TransactionDate` (DateOnly), `Amount` (decimal, signed), `Description` (string), `Balance` (decimal?, optional), `ExternalReferenceId` (string?, the `FITID` or CSV reference), `RowIndex` (int, for error reporting), `RawRow` (string, the original line for provenance).

- **`InstitutionProfile`** *(persisted)*: Describes how to parse a CSV from a specific bank. Contains: `Id`, `Name` (e.g., "Chase Checking CSV"), `ColumnMappings` (dictionary mapping standard field names to actual header strings), `AmountLayout` (enum: `SingleSigned`, `SplitDebitCredit`), `DateFormatPattern` (string?, ISO 8601 assumed if absent), `CreatedAt`, `UpdatedAt`, `IsDeleted` (soft-delete for audit, never hard-deleted if used).

- **`ImportJob`** *(persisted)*: Tracks one parsing execution. Contains: `Id`, `EvidenceId` (FK to source evidence), `InstitutionProfileId` (FK, nullable — null for OFX or auto-detected), `ParserType` (enum: `CsvConfigured`, `CsvAutoDetected`, `Ofx`), `Status` (enum: `Pending`, `Processing`, `Completed`, `PartialSuccess`, `Failed`), `TotalRows` (int), `ParsedCount` (int), `FailedRowCount` (int), `FailedRows` (JSON array: `[{ RowIndex, Reason }]`), `StartedAt`, `CompletedAt`, `CreatedAt`.

- **`FinancialRecord`** *(extended from spec 001)*: Gains new fields: `SourceEvidenceId` (FK), `ImportJobId` (FK), `ExternalReferenceId` (string?, `FITID` or CSV ref), `RowIndex` (int?, source row for traceability), `ClassificationStatus` (enum: `Pending`, `Classified`), `ClassificationConfidence` (decimal?, 0–1), `ClassificationReasonCode` (string?).

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can upload a supported file and see individual transaction records — with correct dates, amounts, and descriptions — within the same API response, with no additional steps required.
- **SC-002**: 100% of successfully parsed transactions have a provenance entry linking them to their source evidence artifact; no orphaned records exist.
- **SC-003**: Re-uploading an identical file (same SHA256) produces the same `ImportJob` ID in the response and zero duplicate records in the database.
- **SC-004**: Files with mixed valid and invalid rows produce records for all valid rows; no valid transaction is lost because of a neighboring malformed row.
- **SC-005**: All auto-classified records expose a confidence score (0–1) and a human-readable reason code; no classification is opaque.
- **SC-006**: A saved `InstitutionProfile` can be applied to a new CSV upload from the same bank and produce correctly parsed records without any user intervention beyond specifying the profile ID.
- **SC-007**: Import jobs of up to 10,000 rows complete without system error; users can retrieve full import results via `GET /api/v1/import-jobs/{id}` after the fact.
- **SC-008**: The same parsing and hydration behavior is exhibited on both SQLite (local development) and PostgreSQL (server deployment) without code changes.

---

## Assumptions

- The SHA256 duplicate-detection mechanism introduced in spec 001 is already operational and will be reused as the gate before parsing begins.
- The rule evaluation service from spec 002 is callable in-process and accepts a `FinancialRecord` returning classification results; no inter-service network call is required.
- OFX 1.x files use a loosely SGML-based format (not strict XML); a permissive parser is required. OFX 2.x files are well-formed XML.
- A `FinancialRecord` carries enough fields to extend with the new classification and provenance columns via a database migration without breaking existing spec 001/002 data.
- CSV auto-detection is limited to a set of known common layouts shipped with the platform; it is not a machine-learning step.
- Institution profiles are platform-global (shared across all users in a single-tenant deployment); per-user profiles are out of scope for this milestone.
- Large file processing (>10,000 rows) is handled synchronously in this milestone; background job offloading is a future concern unless performance testing reveals a blocking issue.
- The `FinancialRecord` placeholder created by spec 001 for already-uploaded files is not retroactively replaced; only new uploads after this feature ships create hydrated records.
- Mobile and desktop client UI for viewing import results is out of scope; the API contract is the deliverable.

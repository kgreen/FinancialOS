# Feature Specification: API & Exporter Ecosystem

**Feature Branch**: `004-api-exporter-ecosystem`

**Created**: 2026-08-01

**Status**: Draft

**Input**: User description: "004 — API & Exporter Ecosystem: Expand the API with filtering and pagination for accounts, records, categories, and rules. Add an export framework with CSV, JSON, YNAB, and Goodbudget exporters. Wire the WPF Desktop project to consume the API over HttpClient. Add runtime database provider switching between SQLite and PostgreSQL."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Filter and Browse Financial Records (Priority: P1)

A user wants to find specific transactions without scrolling through their entire financial history. They can search and narrow down records by applying one or more filters — such as a date window, a category, a merchant name, an amount range, or a specific account — and the results are returned in pages so the interface stays fast and usable regardless of how many records exist.

**Why this priority**: Filtering and pagination are foundational to every downstream workflow. Exports, desktop views, and rule reviews all depend on the ability to retrieve a scoped, manageable set of records. Without this, all other features in this spec either cannot function or become unusable at scale. This is the data-access backbone.

**Independent Test**: Can be fully tested by querying the records endpoint with various filter combinations and confirming that only matching records are returned, in the correct page size, with accurate page metadata — with no export or desktop UI needed.

**Acceptance Scenarios**:

1. **Given** a user has 500 records across three accounts, **When** they request records filtered to a single account for a specific calendar month, **Then** only records belonging to that account within that date range are returned, with a page size of 25 and a total record count in the response.

2. **Given** records exist with merchant names containing "Amazon", **When** a user filters by merchant keyword "amazon" (case-insensitive), **Then** all matching records are returned and no non-matching records appear.

3. **Given** a user requests page 2 of a filtered result set, **When** the filtered result has 60 records and page size is 25, **Then** the second page contains records 26–50, and the response includes current page number, total pages, and total record count.

4. **Given** a user applies an amount range filter of $10.00–$50.00, **When** the results are returned, **Then** every record in the response has an amount within that range, inclusive of boundary values.

5. **Given** no records match the applied filters, **When** the query is executed, **Then** the response returns an empty list with a total count of zero, and no error is raised.

6. **Given** a user filters records by category, **When** the category has sub-categories, **Then** records belonging to any sub-category are included in the results.

---

### User Story 2 — Export Financial Data (Priority: P2)

A user wants to take their financial records out of the system and use them in external tools — such as a budgeting app, a spreadsheet, or a custom analysis script. They can trigger an export for a chosen date range and receive a file in the format of their choice: plain CSV, structured JSON, YNAB 4 import format, or Goodbudget envelope format. Each export is a point-in-time snapshot and does not modify any data in the system.

**Why this priority**: Data portability is a core commitment of a local-first personal finance platform. Users must never feel locked in. Export functionality is self-contained and does not depend on the desktop UI or database provider selection, making it independently deliverable after P1 filtering is in place.

**Independent Test**: Can be fully tested by requesting an export for a known date range in each supported format, then opening or parsing the resulting file to confirm all expected records are present, fields are correctly mapped, and the file conforms to the target format's specification.

**Acceptance Scenarios**:

1. **Given** a user requests a CSV export for a date range, **When** the export is generated, **Then** the file contains one header row and one data row per matching record, with columns for date, merchant, amount, category, account name, and notes.

2. **Given** a user requests a JSON export, **When** the file is received, **Then** it is valid, well-formed JSON containing an array of record objects, each including all record fields and their provenance metadata (source, confidence).

3. **Given** a user requests a YNAB export, **When** the file is generated, **Then** it conforms to YNAB 4's expected import format, with fields mapped to YNAB's column names (Date, Payee, Memo, Outflow, Inflow), and amounts split correctly by transaction direction.

4. **Given** a user requests a Goodbudget export, **When** the file is generated, **Then** it conforms to Goodbudget's CSV import format, with envelope (category), date, payee, and amount fields correctly populated.

5. **Given** two exports are generated for the same date range at different times, **When** no records were added or modified between them, **Then** both files contain identical data — confirming exports are deterministic snapshots.

6. **Given** a user applies filters (date range, account, category) before exporting, **When** the export is triggered, **Then** only records matching those filters appear in the exported file.

7. **Given** the export contains records with special characters in merchant names or notes (e.g., commas, quotes, newlines), **When** the CSV or YNAB export is generated, **Then** those fields are correctly escaped so the file parses without data corruption.

---

### User Story 3 — Use the Desktop Application (Priority: P3)

A user runs the FinancialOS desktop application on their computer and sees their live financial data — accounts, records, categories — exactly as it exists in the system. The desktop application does not read from or write to the database directly; it communicates exclusively through the same API that any other client would use. The user experience in the desktop app is consistent with what the API reports.

**Why this priority**: The desktop application is a consumer of the platform, not a source of truth. Its value depends entirely on the API (P1) being stable and feature-complete. Decoupling the desktop from direct database access also enforces the architecture principle that domain truth lives in the core and UI is a consumer.

**Independent Test**: Can be fully tested by running the desktop application pointed at a live API instance, confirming that data visible in the UI matches data returned directly by API queries, and that no direct database connection string is required in the desktop configuration.

**Acceptance Scenarios**:

1. **Given** the desktop application is configured with the API's base address, **When** the user launches the application, **Then** the application connects to the API, retrieves accounts and recent records, and displays them without requiring any database credentials.

2. **Given** the API is unreachable (e.g., server is stopped), **When** the desktop application starts or refreshes, **Then** the application shows a clear connectivity error message and does not crash or show stale/incorrect data silently.

3. **Given** a new record is imported via the API, **When** the desktop user refreshes their records view, **Then** the new record appears in the list, confirming the desktop is reading live data from the API rather than a local cache.

4. **Given** the desktop application displays a list of accounts, **When** the user views an account's transaction history, **Then** the records shown match the API's filtered results for that account — same count, same amounts, same dates.

---

### User Story 4 — Choose Database Backend (Priority: P4)

An administrator wants to run FinancialOS in two different modes: as a lightweight local installation using a file-based database, or as a shared server installation backed by a full relational database server. They can switch between these modes by changing a single configuration setting. The application detects the setting at startup, connects to the appropriate database, applies any pending schema changes automatically, and begins serving requests — without any code changes or redeployment required.

**Why this priority**: Database provider selection is an operational and deployment concern. It does not affect any user-facing behavior and has no dependency on any other story in this spec. It is last because it is purely infrastructural and its value is realized by administrators rather than end users.

**Independent Test**: Can be fully tested by running the application twice — once with the SQLite configuration and once with the PostgreSQL configuration — and confirming in each case that the application starts successfully, schema migrations are applied, and records can be created and retrieved correctly through the API.

**Acceptance Scenarios**:

1. **Given** the configuration specifies SQLite with a file path, **When** the application starts, **Then** it creates or opens the SQLite database file at the specified path, applies any pending migrations, and begins accepting API requests.

2. **Given** the configuration specifies PostgreSQL with a connection string, **When** the application starts, **Then** it connects to the PostgreSQL server, applies any pending migrations, and begins accepting API requests — with no difference in API behavior compared to SQLite mode.

3. **Given** an administrator changes the provider from SQLite to PostgreSQL in the configuration file, **When** the application is restarted (without recompiling or redeploying), **Then** it uses the PostgreSQL provider and does not attempt to access the SQLite file.

4. **Given** the configured database is unavailable at startup (e.g., PostgreSQL server is down), **When** the application attempts to start, **Then** it logs a clear error describing the connection failure and exits gracefully rather than starting in a degraded state.

5. **Given** both providers are configured (misconfiguration), **When** the application starts, **Then** it raises a configuration error identifying the conflict and refuses to start.

---

### Edge Cases

- What happens when a filter parameter contains an invalid value (e.g., a non-numeric amount, an unrecognized date format)? The API must return a descriptive validation error with HTTP 400, identifying the invalid parameter by name.
- What happens when a requested export contains zero records (all records filtered out)? The export file must still be valid — a CSV with only a header row, an empty JSON array — rather than an error response.
- What happens when a pagination request specifies a page number beyond the total available pages? The API returns an empty result list with correct metadata (total count, total pages) and does not raise an error.
- What happens when an export is requested for a very large record set (tens of thousands of records)? The system must stream or batch the output rather than loading all records into memory at once, preventing out-of-memory failures.
- What happens when the desktop application's configured API version is incompatible with the running server? The desktop must display a version mismatch warning rather than silently misinterpreting data.
- What happens when a PostgreSQL migration fails partway through (e.g., network drop)? The migration must be transactional — either fully applied or fully rolled back — leaving the schema in a consistent state.
- What happens when two export requests are submitted simultaneously for the same date range? Both must complete independently and return identical data, with no locking or race conditions.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Filtering & Pagination

- **FR-001**: The system MUST support filtering records by one or more of the following criteria, individually or in combination: date range (start date, end date), account, category, merchant name (partial, case-insensitive match), minimum amount, maximum amount.
- **FR-002**: The system MUST support filtering accounts by account type and active/inactive status.
- **FR-003**: The system MUST support filtering categories by name (partial match) and parent category.
- **FR-004**: The system MUST support filtering rules by rule type, enabled/disabled status, and target category.
- **FR-005**: All list endpoints (records, accounts, categories, rules) MUST return paginated results with a configurable page size.
- **FR-006**: Every paginated response MUST include: current page number, page size, total record count, and total page count.
- **FR-007**: The system MUST enforce a maximum page size limit to prevent unbounded result sets; requests exceeding this limit must be rejected with a descriptive error.
- **FR-008**: Filter and pagination parameters MUST be validated before query execution; invalid parameters MUST return an HTTP 400 response identifying the offending parameter.
- **FR-009**: Filtered results MUST be deterministically ordered (e.g., by date descending, then by record ID) so that pagination is stable across requests.

#### Export Framework

- **FR-010**: The system MUST provide an export capability that accepts a date range and an optional set of the same filters available on the records endpoint.
- **FR-011**: The system MUST support the following export formats: CSV (generic), JSON, YNAB 4, and Goodbudget.
- **FR-012**: The user MUST be able to specify the desired export format as part of the export request.
- **FR-013**: CSV exports MUST include a header row and one row per record, with columns: Date, Merchant, Amount, Category, Account, Notes.
- **FR-014**: JSON exports MUST include all record fields plus provenance metadata (source file, import date, confidence score) for each record.
- **FR-015**: YNAB 4 exports MUST map fields to YNAB's import column names (Date, Payee, Memo, Outflow, Inflow) and correctly split amounts by debit/credit direction.
- **FR-016**: Goodbudget exports MUST map fields to Goodbudget's import format, including Envelope (category), Date, Payee, Amount, and Account Name.
- **FR-017**: Exports MUST be point-in-time snapshots — they reflect the state of data at the moment the export is requested and do not modify any stored data.
- **FR-018**: Special characters in text fields (commas, quotes, newlines) MUST be correctly escaped in all text-based export formats so that the file parses without data loss.
- **FR-019**: Exports for large record sets MUST be generated without loading all records into memory simultaneously — the system must support streaming or chunked generation.
- **FR-020**: An export request that matches zero records MUST return a valid, empty file in the requested format (e.g., CSV with header only, empty JSON array), not an error.

#### Desktop Application Connectivity

- **FR-021**: The desktop application MUST communicate with all data sources exclusively through the API; it MUST NOT access the database directly.
- **FR-022**: The desktop application MUST read its API base address from a configuration file or environment setting, with no hardcoded connection strings or database paths.
- **FR-023**: The desktop application MUST display accounts, records, categories, and rules by querying the corresponding API endpoints.
- **FR-024**: The desktop application MUST apply the available filter and pagination controls when browsing records, passing filter parameters to the API rather than filtering client-side.
- **FR-025**: The desktop application MUST handle API connectivity failures gracefully — displaying a user-facing error message and remaining in a stable, non-crashed state.
- **FR-026**: The desktop application MUST refresh its displayed data on demand (e.g., a refresh action) by re-querying the API.

#### Database Provider Selection

- **FR-027**: The system MUST support SQLite and PostgreSQL as selectable database providers, controlled by a configuration value (e.g., `"sqlite"` or `"postgres"`).
- **FR-028**: Switching the configured provider MUST require only a configuration change and application restart — no code changes, recompilation, or redeployment.
- **FR-029**: On startup, the application MUST automatically apply any pending schema migrations for the configured provider before accepting requests.
- **FR-030**: If the configured database is unavailable at startup, the application MUST log a clear error and exit gracefully rather than starting in a degraded or undefined state.
- **FR-031**: The API's behavior — request format, response structure, and data semantics — MUST be identical regardless of which database provider is active.
- **FR-032**: If the configuration specifies an unrecognized or ambiguous provider value, the application MUST raise a configuration validation error at startup and refuse to start.

### Key Entities

- **FilterCriteria**: Represents the set of constraints a user applies to scope a query — date range, account reference, category reference, merchant search term, minimum and maximum amount. May be partially specified (any combination of fields).
- **PagedResult**: A wrapper around any list response that carries the data items alongside pagination metadata — current page, page size, total item count, total page count.
- **ExportRequest**: Captures a user's intent to export data — the target format, the date range, and any additional filter criteria to scope the export.
- **ExportFormat**: An enumeration of supported export targets: CSV (generic), JSON, YNAB 4, Goodbudget.
- **ExportSnapshot**: The immutable output of an export operation — a file in the requested format representing the state of matching records at the moment of export.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All four list endpoints (records, accounts, categories, rules) return correctly filtered and paginated results; every filter parameter documented in the spec is supported and independently verifiable.
- **SC-002**: Paginated responses always include accurate total count and total pages metadata, verified against the actual number of matching records in the data set.
- **SC-003**: All four export formats (CSV, JSON, YNAB 4, Goodbudget) produce files that can be successfully imported or parsed by their respective target tools or a standards-conformant parser.
- **SC-004**: Exported files for identical filter criteria produced at two different times (with no intervening data changes) are byte-for-byte identical, confirming deterministic snapshot behavior.
- **SC-005**: The desktop application starts and displays live data when configured with a valid API address, and displays a clear error (not a crash) when the API is unreachable.
- **SC-006**: The desktop application requires zero database credentials or file paths in its configuration — only an API address.
- **SC-007**: The application starts successfully and serves requests correctly with both SQLite and PostgreSQL providers, verified by creating and retrieving records through the API in each mode.
- **SC-008**: Switching from SQLite to PostgreSQL (or vice versa) by changing only the configuration value and restarting the application succeeds without any code or binary changes.
- **SC-009**: Invalid filter parameters (wrong types, out-of-range values) return HTTP 400 responses with a body that names the offending parameter — not HTTP 500 errors.
- **SC-010**: An export of 10,000+ records completes without an out-of-memory error and produces a valid output file.

---

## Assumptions

- Users are already authenticated with the system; this spec does not introduce new authentication or authorization mechanisms.
- The core data model and write paths from specs 001–003 (record creation, account management, import jobs) are stable and will not be redesigned as part of this feature. Existing read endpoints (`/api/v1/records`, `/api/v1/accounts`, `/api/v1/categories`) are extended additively in this feature by adding optional pagination and filter query parameters; no existing query parameters or response fields are removed or changed, so the extensions are backward-compatible. The `/api/v1/classification-rules` endpoint (introduced in spec 002) is also extended with pagination; Feature 004 does not touch the legacy `/api/v1/rules` reference-data endpoint.
- "YNAB 4 format" refers to the classic YNAB 4 desktop import CSV format (Date, Payee, Memo, Outflow, Inflow columns), not the YNAB nYNAB API format.
- "Goodbudget format" refers to Goodbudget's standard CSV export/import layout as publicly documented on the Goodbudget support site.
- The desktop application is a Windows WPF application; mobile and web clients are out of scope for this feature.
- The desktop application does not need to support offline mode in this iteration — it requires a live API connection to display data.
- PostgreSQL migration support assumes the target PostgreSQL server is already provisioned and accessible; database server setup is out of scope.
- Export files are delivered as downloadable file responses; scheduled or recurring exports are out of scope for this feature.
- The maximum page size limit (FR-007) will be determined during implementation based on observed query performance; the spec requires the limit exists and is enforced, not a specific value.
- SQLite is the default provider for local/single-user installations; PostgreSQL is intended for shared/server deployments.

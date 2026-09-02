# Requirements Checklist — 004: API & Exporter Ecosystem

Use this checklist during implementation and review to confirm every requirement in `spec.md` has been addressed and is verifiable.

---

## FR Checklist

### Filtering & Pagination

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| FR-001 | Records endpoint supports filtering by date range, account, category, merchant (partial/case-insensitive), min amount, max amount — individually and in combination | ☐ | |
| FR-002 | Accounts endpoint supports filtering by account type and active/inactive status | ☐ | |
| FR-003 | Categories endpoint supports filtering by name (partial match) and parent category | ☐ | |
| FR-004 | Rules endpoint supports filtering by rule type, enabled/disabled status, and target category | ☐ | |
| FR-005 | All four list endpoints return paginated results with configurable page size | ☐ | |
| FR-006 | Every paginated response includes: current page, page size, total record count, total page count | ☐ | |
| FR-007 | Maximum page size limit is enforced; requests exceeding it are rejected with a descriptive error | ☐ | |
| FR-008 | Invalid filter or pagination parameters return HTTP 400 identifying the offending parameter | ☐ | |
| FR-009 | Filtered results are deterministically ordered (stable pagination across requests) | ☐ | |

### Export Framework

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| FR-010 | Export endpoint accepts date range and the same filter options as the records endpoint | ☐ | |
| FR-011 | All four formats supported: CSV (generic), JSON, YNAB 4, Goodbudget | ☐ | |
| FR-012 | Export format is selectable per request | ☐ | |
| FR-013 | CSV export: header row + one row per record; columns: Date, Merchant, Amount, Category, Account, Notes | ☐ | |
| FR-014 | JSON export: all record fields + provenance metadata (source file, import date, confidence score) | ☐ | |
| FR-015 | YNAB 4 export: columns Date, Payee, Memo, Outflow, Inflow; amounts split by debit/credit direction | ☐ | |
| FR-016 | Goodbudget export: columns Envelope, Date, Payee, Amount, Account Name; correctly mapped | ☐ | |
| FR-017 | Exports are point-in-time snapshots; no stored data is modified by an export | ☐ | |
| FR-018 | Special characters (commas, quotes, newlines) are correctly escaped in all text-based export formats | ☐ | |
| FR-019 | Large exports do not load all records into memory simultaneously (streaming or chunked generation) | ☐ | |
| FR-020 | Export of zero matching records returns a valid empty file (not an error) | ☐ | |

### Desktop Application Connectivity

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| FR-021 | Desktop app communicates exclusively through the API; no direct database access | ☐ | |
| FR-022 | Desktop app reads API base address from configuration; no hardcoded connection strings or DB paths | ☐ | |
| FR-023 | Desktop app displays accounts, records, categories, and rules via API queries | ☐ | |
| FR-024 | Desktop app passes filter parameters to the API; no client-side filtering of API results | ☐ | |
| FR-025 | Desktop app handles API connectivity failures gracefully (error message shown; no crash) | ☐ | |
| FR-026 | Desktop app supports on-demand data refresh by re-querying the API | ☐ | |

### Database Provider Selection

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| FR-027 | SQLite and PostgreSQL are both supported, selected by a configuration value | ☐ | |
| FR-028 | Switching providers requires only a config change + restart; no code/binary changes | ☐ | |
| FR-029 | Pending schema migrations are applied automatically on startup for the active provider | ☐ | |
| FR-030 | Unavailable database at startup causes a clear error log and graceful exit (not degraded start) | ☐ | |
| FR-031 | API behavior (request format, response structure, data semantics) is identical across providers | ☐ | |
| FR-032 | Unrecognized or ambiguous provider config value causes a startup validation error and refusal to start | ☐ | |

---

## Success Criteria Checklist

| ID | Criterion | Verified | Notes |
|----|-----------|----------|-------|
| SC-001 | All four list endpoints return correctly filtered and paginated results; all documented filter params work | ☐ | |
| SC-002 | Paginated responses include accurate total count and total pages, verified against actual data | ☐ | |
| SC-003 | All four export formats produce files parseable/importable by their target tools or standards-conformant parsers | ☐ | |
| SC-004 | Two exports of the same filters with no intervening data changes produce identical files (determinism) | ☐ | |
| SC-005 | Desktop app displays live data with valid API address; shows clear error (not crash) when API unreachable | ☐ | |
| SC-006 | Desktop configuration requires only an API address — no database credentials or file paths | ☐ | |
| SC-007 | Application starts and serves requests correctly in both SQLite and PostgreSQL modes | ☐ | |
| SC-008 | Provider switch via config-only change + restart succeeds without code/binary changes | ☐ | |
| SC-009 | Invalid filter parameters return HTTP 400 naming the offending parameter — not HTTP 500 | ☐ | |
| SC-010 | Export of 10,000+ records completes without out-of-memory error and produces a valid file | ☐ | |

---

## Edge Case Checklist

| Edge Case | Addressed In | Verified | Notes |
|-----------|-------------|----------|-------|
| Invalid filter parameter value returns HTTP 400 naming the parameter | FR-008, SC-009 | ☐ | |
| Export with zero matching records returns valid empty file | FR-020 | ☐ | |
| Pagination page number beyond total pages returns empty list with correct metadata | FR-006, FR-009 | ☐ | |
| Large export (10k+ records) completes without OOM failure | FR-019, SC-010 | ☐ | |
| Desktop app shows version mismatch warning when API version is incompatible | FR-025 | ☐ | |
| PostgreSQL migration failure is transactional (fully applied or fully rolled back) | FR-029 | ☐ | |
| Concurrent exports for the same date range complete independently with identical data | FR-017 | ☐ | |
| Special characters in text fields are escaped without data corruption | FR-018 | ☐ | |
| Misconfigured provider (both or neither specified) raises startup error | FR-032 | ☐ | |

---

## Spec Quality Checks

- [ ] Every user story has at least two acceptance scenarios
- [ ] Every acceptance scenario follows Given / When / Then structure
- [ ] Every FR is testable without implementation knowledge
- [ ] No FR mentions implementation technology (C#, EF Core, LINQ, HttpClient, SQLite driver, etc.)
- [ ] All success criteria are measurable and technology-agnostic
- [ ] Export format specs reference publicly documented target formats (YNAB 4, Goodbudget)
- [ ] Edge cases cover error paths, empty states, and concurrency
- [ ] Assumptions are explicit about what is out of scope (auth, offline mode, server provisioning, scheduled exports)

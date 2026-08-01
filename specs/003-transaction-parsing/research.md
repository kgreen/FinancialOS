# Research: Transaction Parsing & Record Hydration

**Feature**: 003 — Transaction Parsing & Record Hydration
**Date**: 2026-08-01
**Status**: Complete — all NEEDS CLARIFICATION items resolved

---

## R-1 — OFX 1.x SGML Parsing Library Choice

### Decision: Hand-rolled SGML tokenizer + XML fallback (no third-party OFX library)

### Rationale

OFX 1.x files use a flat, unclosed-tag SGML format — they are not XML and cannot be read by `XDocument`/`XmlReader` directly. However, the structure is simple and regular enough that a bespoke tokenizer is the best fit for this codebase:

- **`OFXSharp`** (GitHub: OFXSharp/OFXSharp) — last NuGet publish was 2019; no active maintenance; targets .NET Standard 1.3 and depends on `System.Runtime` overrides that cause warning noise on .NET 8.
- **`dotnet-ofx`** — not available on NuGet as a standalone package; exists only as a personal gist.
- **Manual tokenizer** — the SGML subset used in OFX 1.x is narrow: `<TAG>value` lines with no attributes, and a `<STMTTRN>...</STMTTRN>` block structure. A 100-line regex-based tokenizer covers the full field surface needed by this spec (`DTPOSTED`, `TRNAMT`, `NAME`, `MEMO`, `FITID`). This approach carries zero new dependencies and is easy to unit test with raw string literals.
- **OFX 2.x** files are well-formed XML; `XDocument.Load()` is sufficient.

### Implementation pattern

```
OfxTransactionParser.ParseAsync(stream):
    ├─ Peek first non-whitespace chars
    ├─ If starts with "<?xml" or "<OFX>" with XML declaration → OFX 2.x path (XDocument)
    ├─ If starts with "OFXHEADER:" or "DATA:OFXSGML" → OFX 1.x path (SGML tokenizer)
    └─ Otherwise → throw FileFormatException("Not a recognizable OFX/QFX file")
```

SGML tokenizer approach:
1. Read all lines; discard everything before the first `<OFX>` tag.
2. Accumulate `<STMTTRN>...</STMTTRN>` blocks.
3. Within each block, extract tags by regex: `<(\w+)>([^<\r\n]*)`.
4. Map known tags to `ParsedTransaction` fields; skip unrecognized tags.

### Alternatives considered

| Option | Reason rejected |
|--------|----------------|
| OFXSharp NuGet | Unmaintained since 2019; .NET 8 compat issues |
| Parse as XML after header strip | Risky: unclosed tags in SGML would cause XML parse failures on real bank files |
| Third-party paid library | Out of scope for an open codebase |

---

## R-2 — CSV Parsing Library: CsvHelper

### Decision: Adopt `CsvHelper` 33.x (latest stable as of 2026-Q3)

### Rationale

- CsvHelper is the de-facto standard .NET CSV library; MIT licence; actively maintained; 250M+ NuGet downloads.
- Its `CsvReader` with `HeaderRecord` mode enables dynamic header inspection needed for both auto-detection and profile-driven mapping.
- Version 33.x targets .NET 8 natively with no compatibility shims needed.
- No existing CSV package in the solution — this is a greenfield addition.

### Configuration pattern

```csharp
// Dynamic header reading — used by CsvAutoDetector
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true,
    MissingFieldFound = null,   // do not throw on missing columns
    BadDataFound = null,        // surface bad rows manually
    TrimOptions = TrimOptions.Trim
};
using var reader = new CsvReader(new StreamReader(stream), config);
reader.Read(); reader.ReadHeader();
var headers = reader.HeaderRecord;
```

### Package to add

`src/FinancialOS.Infrastructure/FinancialOS.Infrastructure.csproj`:
```xml
<PackageReference Include="CsvHelper" Version="33.*" />
```

---

## R-3 — CSV Auto-Detection Header Heuristics

### Decision: Compile a lookup table of known common bank CSV layouts; reject if no layout matches.

### Known layouts (shipped with the platform)

| Layout key | Institution | Date header | Amount header | Description header | Notes |
|------------|-------------|-------------|---------------|-------------------|-------|
| `chase-checking` | Chase Bank Checking | `Transaction Date` | `Amount` | `Description` | Includes `Balance` column |
| `chase-credit` | Chase Credit Card | `Transaction Date` | `Amount` | `Description` | Includes `Category`, `Type` |
| `ally-bank` | Ally Bank | `Date` | `Amount` | `Description` | Single `Amount` signed column |
| `citi-checking` | Citi Checking | `Date` | `Debit` + `Credit` | `Description` | Split debit/credit layout |
| `discover` | Discover Card | `Trans. Date` | `Amount` | `Description` | Includes `Category` |
| `bofa-checking` | Bank of America | `Date` | `Amount` | `Description` | Includes `Running Bal.` |
| `capital-one` | Capital One | `Transaction Date` | `Debit` + `Credit` | `Description` | Split debit/credit layout |
| `generic-signed` | Fallback | `Date` OR `Transaction Date` | `Amount` | `Description` OR `Memo` | Single signed amount, case-insensitive match |

**Auto-detection algorithm**:
1. Read the first row as header.
2. Normalise all header strings: lowercase, strip punctuation, collapse whitespace.
3. Attempt exact match against each layout's normalised header fingerprint.
4. If no exact match, attempt `generic-signed` heuristic (any `date`-like + any `amount`-like + any `description`-like header).
5. If still no match → return `UnknownLayoutError` with the actual headers listed, instructing the caller to create an `InstitutionProfile`.

**Confidence signalling**: auto-detected records set `ParserType = CsvAutoDetected` on `ImportJob`; manually profiled records set `ParserType = CsvConfigured`.

---

## R-4 — Classification Status Strategy

### Decision: Option B — add a separate `ClassificationStatus` column to `FinancialRecord`

### Rationale

`RecordStatus` models the **lifecycle** state of a record (`Pending → Normalized → Reviewed → Ignored`). This is about human review workflow and normalization pipeline state. `ClassificationStatus` models the **rule engine outcome** (`Pending → Classified`). These are orthogonal:

- A record can be `RecordStatus.Pending` (not yet reviewed) but `ClassificationStatus.Classified` (a rule already fired).
- A record can be `RecordStatus.Reviewed` (user confirmed it) but `ClassificationStatus.Pending` (no rule matched).
- Conflating them into one enum would require a product of states or break existing spec 001/002 behaviour.

### New enum

```csharp
// Add to DomainEntities.cs (or ImportEntities.cs for isolation)
public enum ClassificationStatus
{
    Pending,    // No rule matched; awaiting manual review or future rule
    Classified  // Rule matched; confidence score and reason code are populated
}
```

### Migration impact

`ClassificationStatus` column added as **nullable** on `FinancialRecord`. Existing records (spec 001/002) remain `null`; the API serializes `null` as `"pending"` for backward compatibility.

---

## R-5 — EF Migration Safety for `FinancialRecord` Column Additions

### Decision: All new columns are nullable with safe defaults; migration is non-destructive.

### Column safety analysis

| New column | Type | Default | Existing rows |
|-----------|------|---------|--------------|
| `ImportJobId` | `Guid?` | `NULL` | Unaffected — manually created records legitimately have no import job |
| `ExternalReferenceId` | `nvarchar(255)?` | `NULL` | Unaffected |
| `RowIndex` | `int?` | `NULL` | Unaffected |
| `ClassificationStatus` | `nvarchar(20)?` | `NULL` | API maps `null` → `"pending"` |
| `ClassificationReasonCode` | `nvarchar(255)?` | `NULL` | Unaffected |

**SQLite migration note**: SQLite does not support `ALTER TABLE ADD COLUMN` with `NOT NULL` without a default, but all columns here are nullable — no issue.

**PostgreSQL migration note**: All `ALTER TABLE ADD COLUMN NULL` operations are non-blocking on PostgreSQL 12+.

### `EvidenceImportService` lifetime correction

`EvidenceImportService` is currently registered as `AddSingleton`. It must become `AddScoped` because the orchestration service (which depends on `IFinancialRepository`, a scoped service) will call it per-request. This is corrected in the DI registration section of `Program.cs`.

---

## Summary Table: All Decisions

| ID | Unknown | Decision |
|----|---------|----------|
| R-1 | OFX parsing library | Hand-rolled SGML tokenizer for 1.x; `XDocument` for 2.x. No new NuGet package for OFX. |
| R-2 | CSV parsing library | `CsvHelper` 33.x — add to `FinancialOS.Infrastructure.csproj` |
| R-3 | CSV auto-detection | Lookup table of 8 known layouts + generic signed fallback; error on unknown with header hint |
| R-4 | Classification status model | New `ClassificationStatus` enum (`Pending`/`Classified`) as nullable column on `FinancialRecord` |
| R-5 | Migration safety | All new `FinancialRecord` columns nullable; non-destructive on both SQLite and PostgreSQL |

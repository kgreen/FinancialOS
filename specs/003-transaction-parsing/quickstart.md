# Quickstart Validation Guide: Transaction Parsing & Record Hydration

**Feature**: 003 — Transaction Parsing & Record Hydration
**Date**: 2026-08-01

---

## Overview

This guide describes runnable end-to-end validation scenarios that prove the parsing pipeline works correctly. Each scenario states prerequisites, the exact command(s) to run, and the expected outcome.

For entity field definitions see [data-model.md](../data-model.md).
For full API schemas see [contracts/import-jobs.md](../contracts/import-jobs.md) and [contracts/institution-profiles.md](../contracts/institution-profiles.md).

---

## Prerequisites

```bash
# 1. Start the API (development / SQLite mode)
cd src/FinancialOS.Api
dotnet run

# API is available at: http://localhost:5000
# Swagger UI:          http://localhost:5000/swagger

# 2. Apply database migrations (run once after checkout)
cd src/FinancialOS.Data
dotnet ef database update
```

---

## Test Fixtures

Two minimal test files are needed. Create them locally:

### `chase-10rows.csv`

```csv
Transaction Date,Description,Category,Type,Amount,Balance
07/01/2026,AMAZON.COM*AA1234567,Shopping,Sale,-42.50,1057.50
07/02/2026,WHOLE FOODS #123,Groceries,Sale,-15.30,1042.20
07/03/2026,PAYROLL DIRECT DEPOSIT,,ACH_CREDIT,1500.00,2542.20
07/04/2026,NETFLIX.COM,Entertainment,Sale,-15.99,2526.21
07/05/2026,SPOTIFY USA,Entertainment,Sale,-9.99,2516.22
07/06/2026,SHELL OIL 12345,Gas,Sale,-55.00,2461.22
07/07/2026,ATM WITHDRAWAL,,ATH,-200.00,2261.22
07/08/2026,STARBUCKS #9921,Food & Drink,Sale,-6.75,2254.47
07/09/2026,VENMO PAYMENT,Transfer,ACH_DEBIT,-100.00,2154.47
07/10/2026,INTEREST PAYMENT,,Debit,0.12,2154.59
```

### `test-5rows.ofx`

```ofx
OFXHEADER:100
DATA:OFXSGML
VERSION:151
SECURITY:NONE
ENCODING:UTF-8
CHARSET:1252
COMPRESSION:NONE
OLDFILEUID:NONE
NEWFILEUID:NONE

<OFX>
<BANKMSGSRSV1>
<STMTTRNRS>
<STMTRS>
<BANKTRANLIST>
<STMTTRN>
<TRNTYPE>DEBIT
<DTPOSTED>20260701120000
<TRNAMT>-42.50
<FITID>TXN-2026-0001
<NAME>AMAZON.COM
</STMTTRN>
<STMTTRN>
<TRNTYPE>CREDIT
<DTPOSTED>20260702120000
<TRNAMT>1500.00
<FITID>TXN-2026-0002
<NAME>PAYROLL DEPOSIT
</STMTTRN>
<STMTTRN>
<TRNTYPE>DEBIT
<DTPOSTED>20260703120000
<TRNAMT>-15.99
<FITID>TXN-2026-0003
<NAME>NETFLIX.COM
</STMTTRN>
<STMTTRN>
<TRNTYPE>DEBIT
<DTPOSTED>20260704120000
<TRNAMT>-9.99
<FITID>TXN-2026-0004
<MEMO>SPOTIFY PREMIUM
</STMTTRN>
<STMTTRN>
<TRNTYPE>DEBIT
<DTPOSTED>20260705120000
<TRNAMT>-55.00
<FITID>TXN-2026-0005
<NAME>SHELL OIL
</STMTTRN>
</BANKTRANLIST>
</STMTRS>
</STMTTRNRS>
</BANKMSGSRSV1>
</OFX>
```

---

## Scenario 1 — Upload a Chase CSV and receive 10 transaction records

**Validates**: US-1 / FR-001, FR-005, FR-013, FR-022

```bash
curl -X POST http://localhost:5000/api/v1/evidence \
  -F "file=@chase-10rows.csv"
```

**Expected response** `200 OK`:

```json
{
  "evidenceId": "<uuid>",
  "importJobId": "<uuid>",
  "status": "completed",
  "parserType": "csvAutoDetected",
  "parsedTransactionCount": 10,
  "failedRowCount": 0,
  "records": [ /* 10 entries */ ]
}
```

**Verification checklist**:
- [ ] `parsedTransactionCount` equals `10`
- [ ] `failedRowCount` equals `0`
- [ ] `status` is `"completed"`
- [ ] `parserType` is `"csvAutoDetected"` (Chase layout auto-detected)
- [ ] Each record in `records[]` has a non-null `date`, non-zero `amount`, non-empty `description`
- [ ] First record: `amount` is `-42.50`, `description` contains `"AMAZON.COM"`
- [ ] Third record: `amount` is `1500.00` (positive credit)

---

## Scenario 2 — Upload an OFX file and receive 5 records with FITID stored

**Validates**: US-2 / FR-007, FR-008, FR-009, FR-010

```bash
curl -X POST http://localhost:5000/api/v1/evidence \
  -F "file=@test-5rows.ofx"
```

**Expected response** `200 OK`:

```json
{
  "status": "completed",
  "parserType": "ofx",
  "parsedTransactionCount": 5,
  "failedRowCount": 0,
  "records": [ /* 5 entries */ ]
}
```

**Verification checklist**:
- [ ] `parsedTransactionCount` equals `5`
- [ ] `parserType` is `"ofx"`
- [ ] Record for `TXN-2026-0001`: `amount` is `-42.50` (negative debit preserved)
- [ ] Record for `TXN-2026-0002`: `amount` is `1500.00` (positive credit preserved)
- [ ] Record for `TXN-2026-0004` (no `NAME`, only `MEMO`): `description` is `"SPOTIFY PREMIUM"` (MEMO fallback)
- [ ] Retrieve import job via `GET /api/v1/import-jobs/{importJobId}` and confirm `failedRows` is empty

---

## Scenario 3 — Re-upload the same file returns duplicate response, no new records

**Validates**: FR-018, SC-003

```bash
# Upload the same OFX file a second time
curl -X POST http://localhost:5000/api/v1/evidence \
  -F "file=@test-5rows.ofx"
```

**Expected response** `200 OK`:

```json
{
  "status": "duplicate",
  "importJobId": "<same job ID from Scenario 2>",
  "parsedTransactionCount": 0,
  "records": []
}
```

**Verification checklist**:
- [ ] `status` is `"duplicate"`
- [ ] `importJobId` matches the job ID from Scenario 2 (not a new ID)
- [ ] `GET /api/v1/records` count has not increased

---

## Scenario 4 — CSV with malformed rows: valid rows succeed, bad rows recorded

**Validates**: US-4 / FR-004, FR-024, FR-025

Create `mixed-errors.csv`:

```csv
Transaction Date,Description,Amount
07/01/2026,VALID ROW ONE,-10.00
07/02/2026,MISSING AMOUNT,
07/03/2026,VALID ROW THREE,-20.00
,MISSING DATE,-5.00
07/05/2026,VALID ROW FIVE,-30.00
07/06/2026,BAD AMOUNT,not-a-number
07/07/2026,VALID ROW SEVEN,-40.00
07/08/2026,VALID ROW EIGHT,-50.00
07/09/2026,VALID ROW NINE,-60.00
07/10/2026,VALID ROW TEN,-70.00
```

```bash
curl -X POST http://localhost:5000/api/v1/evidence \
  -F "file=@mixed-errors.csv"
```

**Expected response**:

```json
{
  "status": "partialSuccess",
  "parsedTransactionCount": 8,
  "failedRowCount": 2,
  "records": [ /* 8 entries */ ]
}
```

Then retrieve the job:

```bash
curl http://localhost:5000/api/v1/import-jobs/{importJobId}
```

**Verification checklist**:
- [ ] `status` is `"partialSuccess"`
- [ ] `parsedCount` is `8`, `failedRowCount` is `2`
- [ ] `failedRows` array has 2 entries
- [ ] Row index `1` failure reason mentions `"Amount"` (or equivalent)
- [ ] Row index `3` failure reason mentions `"Date"` (or equivalent)
- [ ] Row index `5` failure reason mentions `"not a valid decimal"` (or equivalent)

---

## Scenario 5 — Create an institution profile and use it to parse a custom CSV

**Validates**: US-3 / FR-002, FR-003, FR-026, FR-029

### Step 5a: Create the profile

```bash
curl -X POST http://localhost:5000/api/v1/institution-profiles \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Credit Union CSV",
    "columnMappings": {
      "date": "Trans Date",
      "description": "Description",
      "balance": "Running Balance"
    },
    "amountLayout": "splitDebitCredit",
    "debitColumnName": "Debit",
    "creditColumnName": "Credit",
    "dateFormatPattern": "MM/dd/yyyy"
  }'
```

Save the `id` from the `201 Created` response as `PROFILE_ID`.

### Step 5b: Create `credit-union.csv`

```csv
Trans Date,Description,Debit,Credit,Running Balance
07/01/2026,GROCERY STORE,55.00,,2445.00
07/02/2026,DIRECT DEPOSIT,,1500.00,3945.00
07/03/2026,ELECTRIC BILL,120.00,,3825.00
```

### Step 5c: Upload with profile ID

```bash
curl -X POST http://localhost:5000/api/v1/evidence \
  -F "file=@credit-union.csv" \
  -F "institutionProfileId=$PROFILE_ID"
```

**Verification checklist**:
- [ ] `parsedTransactionCount` is `3`
- [ ] `parserType` is `"csvConfigured"`
- [ ] Row 1 (Debit 55.00): `amount` is `-55.00` (debit normalized to negative)
- [ ] Row 2 (Credit 1500.00): `amount` is `1500.00` (positive)
- [ ] Row 3 (Debit 120.00): `amount` is `-120.00`

---

## Scenario 6 — Unsupported file format rejected before parsing

**Validates**: Edge case — unsupported extension

```bash
curl -X POST http://localhost:5000/api/v1/evidence \
  -F "file=@statement.pdf"
```

**Expected response** `422 Unprocessable Entity`:
- [ ] Response status is `422`
- [ ] Response body mentions `.pdf` and lists supported formats
- [ ] No `ImportJob` is created (confirm by checking no new job exists)

---

## Scenario 7 — Retrieve an import job by ID

**Validates**: FR-023

```bash
curl http://localhost:5000/api/v1/import-jobs/{id}
```

**Verification checklist**:
- [ ] `200 OK` for a valid job ID
- [ ] `404 Not Found` for an unknown UUID
- [ ] Response contains all fields: `totalRows`, `parsedCount`, `failedRowCount`, `status`, `evidenceId`, `parserType`, `failedRows`

---

## Automated test coverage (integration tests)

The following test classes in `tests/FinancialOS.Api.Tests/` cover these scenarios programmatically:

| Test class | Scenarios covered |
|------------|-----------------|
| `EvidenceImportIntegrationTests` | 1, 2, 3, 4, 6 |
| `ImportJobEndpointTests` | 7 |
| `InstitutionProfileEndpointTests` | 5 |

Unit test classes in `tests/FinancialOS.Core.Tests/`:

| Test class | Validates |
|------------|-----------|
| `CsvTransactionParserTests` | FR-001–FR-006: column mapping, split amounts, date parsing, bad row skipping |
| `OfxTransactionParserTests` | FR-007–FR-011: SGML/XML detection, field mapping, MEMO fallback, malformed rejection |

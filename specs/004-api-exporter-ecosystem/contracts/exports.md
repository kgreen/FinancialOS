# Contract: Exports Endpoint (Feature 004)

**Endpoint group**: `/api/v1/exports`  
**Feature**: `004-api-exporter-ecosystem`

---

## POST /api/v1/exports

Generates and streams a file export of financial records matching the specified criteria.

### Request

**Method**: `POST`  
**Content-Type**: `application/json`

#### Request Body

```json
{
  "format": "csv",
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "filters": {
    "accountId": "a1b2c3d4-0000-0000-0000-000000000002",
    "categoryId": null,
    "merchant": null,
    "minAmount": null,
    "maxAmount": null
  }
}
```

#### Request Fields

| Field        | Type       | Required | Description |
|-------------|------------|----------|-------------|
| `format`     | `string` (enum) | **Yes** | Export format: `"csv"`, `"json"`, `"ynab4"`, `"goodbudget"` |
| `startDate`  | `DateOnly` (YYYY-MM-DD) | **Yes** | Inclusive start of export date range |
| `endDate`    | `DateOnly` (YYYY-MM-DD) | **Yes** | Inclusive end of export date range |
| `filters`    | `object?`  | No | Optional additional filter criteria (same fields as records endpoint) |
| `filters.accountId`  | `Guid?`    | No | Scope to a specific account |
| `filters.categoryId` | `Guid?`    | No | Scope to a category (includes sub-categories) |
| `filters.merchant`   | `string?`  | No | Partial, case-insensitive merchant name match |
| `filters.minAmount`  | `decimal?` | No | Minimum amount, inclusive |
| `filters.maxAmount`  | `decimal?` | No | Maximum amount, inclusive |

---

### Response: 200 OK — File Download

The response streams the export file directly. No intermediate storage.

**Headers**:
```
Content-Type: text/csv; charset=utf-8
Content-Disposition: attachment; filename="financialos-export-2026-01-01_2026-12-31.csv"
```

(Content-Type and filename vary by format — see table below.)

| Format       | `format` value | Content-Type                         | Filename suffix |
|-------------|---------------|--------------------------------------|-----------------|
| Generic CSV  | `"csv"`        | `text/csv; charset=utf-8`            | `.csv`          |
| JSON         | `"json"`       | `application/json; charset=utf-8`    | `.json`         |
| YNAB 4       | `"ynab4"`      | `text/csv; charset=utf-8`            | `-ynab4.csv`    |
| Goodbudget   | `"goodbudget"` | `text/csv; charset=utf-8`            | `-goodbudget.csv` |

---

## Format Specifications

### CSV (Generic) — `"csv"`

Column order: `Date`, `Merchant`, `Amount`, `Category`, `Account`, `Notes`

```csv
Date,Merchant,Amount,Category,Account,Notes
2026-07-15,Amazon,-49.99,Shopping,Chase Checking,Office supplies
2026-07-10,"Starbucks, Inc",-4.75,Food & Drink,Chase Checking,
```

- `Date` format: `YYYY-MM-DD`
- `Amount`: signed decimal (negative = debit)
- Fields with commas or quotes are double-quoted and internal quotes are escaped as `""`
- Empty optional fields are left blank (not `null`)
- Header row always present, even if no records match

---

### JSON — `"json"`

Returns a JSON array. Each object includes all record fields plus provenance metadata.

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "transactionDate": "2026-07-15",
    "merchantName": "Amazon",
    "normalizedMerchantName": "Amazon",
    "amount": -49.99,
    "categoryName": "Shopping",
    "accountName": "Chase Checking",
    "notes": "Office supplies",
    "provenance": {
      "sourceFile": "chase-july-2026.csv",
      "importedAt": "2026-07-16T10:30:00Z",
      "confidenceScore": 0.95
    }
  }
]
```

- Empty result: `[]` (not an error)
- All `DateTimeOffset` values are ISO 8601 UTC

---

### YNAB 4 — `"ynab4"`

Column order: `Date`, `Payee`, `Memo`, `Outflow`, `Inflow`

```csv
Date,Payee,Memo,Outflow,Inflow
07/15/2026,Amazon,Office supplies,49.99,
07/10/2026,Starbucks,,4.75,
07/01/2026,Paycheck,,0.00,2500.00
```

**Field Mapping**:
| FinancialRecord      | YNAB 4 column | Notes |
|---------------------|--------------|-------|
| `TransactionDate`   | `Date`       | `MM/DD/YYYY` format |
| `MerchantName`      | `Payee`      | |
| `Notes`             | `Memo`       | |
| `Amount` if `< 0`   | `Outflow`    | Positive value; `Inflow` is empty |
| `Amount` if `>= 0`  | `Inflow`     | Positive value; `Outflow` is `0.00` |

- `Outflow` and `Inflow` are always non-negative with two decimal places
- Rows with `Amount = 0` emit `Outflow=0.00`, `Inflow` empty
- Fields with commas or special characters are RFC 4180 quoted

---

### Goodbudget — `"goodbudget"`

Column order: `Date`, `Envelope`, `Account`, `Name`, `Amount`, `Notes`

```csv
Date,Envelope,Account,Name,Amount,Notes
07/15/2026,Shopping,Chase Checking,Amazon,-49.99,Office supplies
07/10/2026,Food & Drink,Chase Checking,Starbucks,-4.75,
07/01/2026,,Chase Checking,Paycheck,2500.00,Bi-weekly paycheck
```

**Field Mapping**:
| FinancialRecord      | Goodbudget column | Notes |
|---------------------|------------------|-------|
| `TransactionDate`   | `Date`           | `MM/DD/YYYY` format |
| `CategoryName`      | `Envelope`       | Empty string if uncategorized |
| `AccountName`       | `Account`        | |
| `MerchantName`      | `Name`           | |
| `Amount`            | `Amount`         | Signed decimal (negative = spending) |
| `Notes`             | `Notes`          | |

---

## Error Responses

### 400 Bad Request — validation errors

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "format": ["Unrecognized export format 'excel'. Supported values: csv, json, ynab4, goodbudget."],
    "endDate": ["EndDate must be on or after StartDate."]
  }
}
```

### 400 Bad Request — unrecognized format

```json
{
  "status": 400,
  "title": "Invalid export format.",
  "detail": "Format 'excel' is not supported. Supported formats: csv, json, ynab4, goodbudget."
}
```

### 200 OK — zero records

The response is still `200 OK` with a valid empty file:
- CSV: header row only
- JSON: `[]`
- YNAB 4: header row only
- Goodbudget: header row only

---

## Example Requests (curl)

**CSV export, full year:**
```bash
curl -X POST http://localhost:5000/api/v1/exports \
  -H "Content-Type: application/json" \
  -d '{"format":"csv","startDate":"2026-01-01","endDate":"2026-12-31"}' \
  --output export.csv
```

**YNAB 4 export, filtered by account:**
```bash
curl -X POST http://localhost:5000/api/v1/exports \
  -H "Content-Type: application/json" \
  -d '{
    "format": "ynab4",
    "startDate": "2026-07-01",
    "endDate": "2026-07-31",
    "filters": { "accountId": "a1b2c3d4-0000-0000-0000-000000000002" }
  }' \
  --output july-ynab4.csv
```

**JSON export:**
```bash
curl -X POST http://localhost:5000/api/v1/exports \
  -H "Content-Type: application/json" \
  -d '{"format":"json","startDate":"2026-01-01","endDate":"2026-06-30"}' \
  --output h1-2026.json
```

**Goodbudget export:**
```bash
curl -X POST http://localhost:5000/api/v1/exports \
  -H "Content-Type: application/json" \
  -d '{"format":"goodbudget","startDate":"2026-01-01","endDate":"2026-12-31"}' \
  --output goodbudget-2026.csv
```

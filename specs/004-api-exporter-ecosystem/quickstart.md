# Quickstart: API & Exporter Ecosystem (Feature 004)

**Feature**: `004-api-exporter-ecosystem`

This guide demonstrates how to validate all four capabilities introduced in Feature 004: filtered pagination, exports, desktop connectivity, and database provider switching.

---

## Prerequisites

- .NET 8 SDK installed
- API running at `http://localhost:5000` (default)
- SQLite database seeded with at least a few records (run existing migrations first)
- `curl` available (or use any HTTP client)

---

## 1. Start the API

# From the repo root
cd .\src\FinancialOS.Api
dotnet run

Confirm the API is up:
```bash
curl http://localhost:5000/health
```

---

## 2. Test Filtered Record Queries

### 2a. Retrieve records with default pagination

```bash
curl "http://localhost:5000/api/v1/records"
```

Expected: `200 OK` with `PagedResult<FinancialRecordSummary>` — `page: 1`, `pageSize: 25`.

### 2b. Filter by date range

```bash
curl "http://localhost:5000/api/v1/records?startDate=2026-07-01&endDate=2026-07-31"
```

Expected: Only records with `transactionDate` in July 2026.

### 2c. Filter by merchant keyword (case-insensitive)

```bash
curl "http://localhost:5000/api/v1/records?merchant=amazon"
```

Expected: Records where `merchantName` contains "amazon" (any case).

### 2d. Filter by amount range

```bash
curl "http://localhost:5000/api/v1/records?minAmount=-50.00&maxAmount=-10.00"
```

Expected: All records with signed `amount` between -50.00 and -10.00 (debits/expenses in that range).

### 2e. Paginate — request page 2

```bash
curl "http://localhost:5000/api/v1/records?page=2&pageSize=25"
```

Expected: `page: 2`; items are the next 25 records.

### 2f. Request a page beyond total (no error, empty items)

```bash
curl "http://localhost:5000/api/v1/records?page=9999&pageSize=25"
```

Expected: `200 OK`, `"items": []`, accurate `totalCount` and `totalPages` in metadata.

### 2g. Invalid page value — expect 400

```bash
curl "http://localhost:5000/api/v1/records?page=0"
```

Expected: `400 Bad Request` identifying `page`.

### 2h. Invalid filter — expect 400

```bash
curl "http://localhost:5000/api/v1/records?startDate=2026-12-01&endDate=2026-01-01"
```

Expected: `400 Bad Request` with `"errors": { "endDate": [...] }`.

### 2i. PageSize too large — expect 400

```bash
curl "http://localhost:5000/api/v1/records?pageSize=999"
```

Expected: `400 Bad Request` identifying `pageSize`.

---

## 3. Test Filtered Accounts

```bash
curl "http://localhost:5000/api/v1/accounts?isActive=true"
curl "http://localhost:5000/api/v1/accounts?accountType=Checking"
curl "http://localhost:5000/api/v1/accounts?page=1&pageSize=5"
```

---

## 4. Test Filtered Categories

```bash
curl "http://localhost:5000/api/v1/categories?nameSearch=food"
curl "http://localhost:5000/api/v1/categories?parentId=<some-parent-id>"
```

---

## 5. Test Filtered Rules

```bash
curl "http://localhost:5000/api/v1/rules?isEnabled=true"
curl "http://localhost:5000/api/v1/rules?ruleType=MerchantMatch"
```

---

## 6. Test Each Export Format

### 6a. Generic CSV export

```bash
curl -X POST http://localhost:5000/api/v1/exports \
  -H "Content-Type: application/json" \
  -d '{"format":"csv","startDate":"2026-01-01","endDate":"2026-12-31"}' \
  --output export.csv
```

Validate: Open `export.csv` in Excel or a text editor. Expect header row: `Date,Merchant,Amount,Category,Account,Notes`.

### 6b. JSON export

```bash
curl -X POST http://localhost:5000/api/v1/exports \
  -H "Content-Type: application/json" \
  -d '{"format":"json","startDate":"2026-01-01","endDate":"2026-12-31"}' \
  --output export.json
```

Validate:
```bash
# Should output valid JSON array
python -c "import json,sys; data=json.load(open('export.json')); print(f'{len(data)} records')"
```

### 6c. YNAB 4 export

```bash
curl -X POST http://localhost:5000/api/v1/exports \
  -H "Content-Type: application/json" \
  -d '{"format":"ynab4","startDate":"2026-01-01","endDate":"2026-12-31"}' \
  --output export-ynab4.csv
```

Validate: Open file; confirm header `Date,Payee,Memo,Outflow,Inflow`. Check that expenses appear in `Outflow` (positive) and income in `Inflow` (positive). Dates in `MM/DD/YYYY`.

### 6d. Goodbudget export

```bash
curl -X POST http://localhost:5000/api/v1/exports \
  -H "Content-Type: application/json" \
  -d '{"format":"goodbudget","startDate":"2026-01-01","endDate":"2026-12-31"}' \
  --output export-goodbudget.csv
```

Validate: Header `Date,Envelope,Account,Name,Amount,Notes`. Signed amounts (negative for spending).

### 6e. Export with additional filters

```bash
curl -X POST http://localhost:5000/api/v1/exports \
  -H "Content-Type: application/json" \
  -d '{
    "format": "csv",
    "startDate": "2026-07-01",
    "endDate": "2026-07-31",
    "filters": { "merchant": "starbucks" }
  }' \
  --output export-filtered.csv
```

### 6f. Export zero records (valid empty file)

```bash
curl -X POST http://localhost:5000/api/v1/exports \
  -H "Content-Type: application/json" \
  -d '{"format":"csv","startDate":"1900-01-01","endDate":"1900-12-31"}' \
  --output export-empty.csv
```

Validate: File contains only the header row. Response is `200 OK`, not `404`.

### 6g. Invalid format — expect 400

```bash
curl -X POST http://localhost:5000/api/v1/exports \
  -H "Content-Type: application/json" \
  -d '{"format":"excel","startDate":"2026-01-01","endDate":"2026-12-31"}'
```

Expected: `400 Bad Request` identifying `format`.

---

## 7. Configure and Run the Desktop Application

### 7a. Configure the API base address

Edit `C:\Users\User\OneDrive\Documents\Projects\FinancialOS\src\FinancialOS.Desktop\appsettings.json`:

```json
{
  "ApiClient": {
    "BaseUrl": "http://localhost:5000",
    "TimeoutSeconds": 30
  }
}
```

### 7b. Run the desktop app

```powershell
cd C:\Users\User\OneDrive\Documents\Projects\FinancialOS\src\FinancialOS.Desktop
dotnet run
```

Expected: Application window opens, loads accounts and recent records from the API.

### 7c. Verify no direct database access

Confirm there is no `ConnectionStrings` entry in `FinancialOS.Desktop\appsettings.json`. The desktop app must not reference SQLite or PostgreSQL directly.

### 7d. Test offline behavior

Stop the API (`Ctrl+C` in the API terminal). Refresh the desktop app.  
Expected: A clear error message is displayed. The application does not crash.

---

## 8. Switch Database Provider

### 8a. Run with SQLite (default)

`C:\Users\User\OneDrive\Documents\Projects\FinancialOS\src\FinancialOS.Api\appsettings.json`:
```json
{
  "DatabaseProvider": "sqlite",
  "ConnectionStrings": {
    "Default": "Data Source=financialos.db"
  }
}
```

```powershell
dotnet run --project src\FinancialOS.Api
```

Expected: API starts, migrations applied, requests served using SQLite.

### 8b. Switch to PostgreSQL

Update `appsettings.json`:
```json
{
  "DatabaseProvider": "postgres",
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=financialos;Username=app;Password=secret"
  }
}
```

Restart:
```powershell
dotnet run --project src\FinancialOS.Api
```

Expected: API starts, EF Core migrations applied to PostgreSQL, same API behavior.

### 8c. Use environment variable override (CI/containers)

```powershell
$env:FINANCIALOS_DB_PROVIDER = "postgres"
$env:ConnectionStrings__Default = "Host=localhost;Database=financialos;Username=app;Password=secret"
dotnet run --project src\FinancialOS.Api
```

### 8d. Invalid provider — expect startup failure

```json
{
  "DatabaseProvider": "mysql"
}
```

Expected: Application fails to start with a clear `InvalidOperationException` identifying `"mysql"` as unrecognized. It does **not** start in a degraded state.

### 8e. Verify identical behavior between providers

Run these requests against both providers and confirm identical response structures:
```bash
curl "http://localhost:5000/api/v1/records?page=1&pageSize=5"
curl "http://localhost:5000/api/v1/accounts"
```

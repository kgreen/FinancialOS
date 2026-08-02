# Contract: Records Endpoint (Feature 004)

**Endpoint group**: `/api/v1/records`  
**Feature**: `004-api-exporter-ecosystem`

---

## Updated: GET /api/v1/records

Returns a paginated, filtered list of financial records.

### Query Parameters

| Parameter    | Type       | Required | Default | Description |
|-------------|------------|----------|---------|-------------|
| `startDate`  | `DateOnly` (YYYY-MM-DD) | No | — | Inclusive lower bound on `TransactionDate` |
| `endDate`    | `DateOnly` (YYYY-MM-DD) | No | — | Inclusive upper bound on `TransactionDate` |
| `accountId`  | `Guid`     | No | — | Filter to a specific account |
| `categoryId` | `Guid`     | No | — | Filter to category or any of its children |
| `merchant`   | `string`   | No | — | Partial, case-insensitive match on merchant name (max 200 chars) |
| `minAmount`  | `decimal`  | No | — | Minimum amount, inclusive (non-negative) |
| `maxAmount`  | `decimal`  | No | — | Maximum amount, inclusive (≥ minAmount) |
| `page`       | `int`      | No | `1`  | 1-based page number |
| `pageSize`   | `int`      | No | `25` | Records per page (1–200) |

### Ordering

Results are ordered by `TransactionDate` descending, then `Id` ascending (stable across pages).

### Response: 200 OK

**Content-Type**: `application/json`

**Shape**: `PagedResult<FinancialRecordSummary>`

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "transactionDate": "2026-07-15",
      "merchantName": "Amazon",
      "normalizedMerchantName": "Amazon",
      "amount": -49.99,
      "categoryId": "a1b2c3d4-0000-0000-0000-000000000001",
      "categoryName": "Shopping",
      "accountId": "a1b2c3d4-0000-0000-0000-000000000002",
      "accountName": "Chase Checking",
      "notes": "Office supplies",
      "importedAt": "2026-07-16T10:30:00Z",
      "sourceFile": "chase-july-2026.csv",
      "confidenceScore": 0.95
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 143,
  "totalPages": 6
}
```

### `FinancialRecordSummary` fields

| Field                   | Type       | Description |
|------------------------|------------|-------------|
| `id`                    | `Guid`     | Record identifier |
| `transactionDate`       | `DateOnly` | Date of the transaction (YYYY-MM-DD) |
| `merchantName`          | `string`   | Raw merchant name from source |
| `normalizedMerchantName`| `string?`  | Cleaned/normalized name; `null` if not yet normalized |
| `amount`                | `decimal`  | Signed amount; negative = debit/expense |
| `categoryId`            | `Guid?`    | Assigned category; `null` if uncategorized |
| `categoryName`          | `string?`  | Category display name |
| `accountId`             | `Guid`     | Account this record belongs to |
| `accountName`           | `string`   | Account display name |
| `notes`                 | `string?`  | Optional transaction notes |
| `importedAt`            | `DateTimeOffset` | When this record entered the system |
| `sourceFile`            | `string?`  | Original import file name |
| `confidenceScore`       | `double?`  | Confidence score from import parsing (0–1) |

---

### Error Responses

#### 400 Bad Request — invalid filter parameters

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "endDate": ["EndDate must be on or after StartDate."],
    "pageSize": ["PageSize must be between 1 and 200."]
  }
}
```

---

### Example Requests

**Filter by account and month:**
```
GET /api/v1/records?accountId=a1b2c3d4-0000-0000-0000-000000000002&startDate=2026-07-01&endDate=2026-07-31&page=1&pageSize=25
```

**Merchant keyword search:**
```
GET /api/v1/records?merchant=amazon&page=1&pageSize=50
```

**Amount range filter:**
```
GET /api/v1/records?minAmount=10.00&maxAmount=50.00
```

**Combined filters, page 2:**
```
GET /api/v1/records?categoryId=a1b2c3d4-0000-0000-0000-000000000001&startDate=2026-01-01&endDate=2026-12-31&page=2&pageSize=25
```

**Empty result (beyond last page):**
```json
{
  "items": [],
  "page": 99,
  "pageSize": 25,
  "totalCount": 143,
  "totalPages": 6
}
```

# Contract: Accounts Endpoint (Feature 004)

**Endpoint group**: `/api/v1/accounts`  
**Feature**: `004-api-exporter-ecosystem`

---

## Updated: GET /api/v1/accounts

Returns a paginated, optionally filtered list of accounts.

### Query Parameters

| Parameter     | Type      | Required | Default | Description |
|--------------|-----------|----------|---------|-------------|
| `accountType` | `string`  | No | — | Filter by account type (case-insensitive); e.g., `"Checking"`, `"Savings"`, `"CreditCard"` |
| `isActive`    | `bool`    | No | — | Filter by active status (`true` or `false`); omit to return all |
| `page`        | `int`     | No | `1`  | 1-based page number |
| `pageSize`    | `int`     | No | `25` | Accounts per page (1–200) |

### Ordering

Results ordered by `Name` ascending.

### Response: 200 OK

**Content-Type**: `application/json`

```json
{
  "items": [
    {
      "id": "a1b2c3d4-0000-0000-0000-000000000002",
      "name": "Chase Checking",
      "accountType": "Checking",
      "institutionName": "Chase",
      "isActive": true,
      "currentBalance": 4821.33,
      "currency": "USD",
      "createdAt": "2026-01-15T09:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 4,
  "totalPages": 1
}
```

### `AccountSummary` fields

| Field             | Type       | Description |
|------------------|------------|-------------|
| `id`              | `Guid`     | Account identifier |
| `name`            | `string`   | Display name |
| `accountType`     | `string`   | Account type string |
| `institutionName` | `string?`  | Financial institution name |
| `isActive`        | `bool`     | Whether this account is active |
| `currentBalance`  | `decimal?` | Last known balance; `null` if not tracked |
| `currency`        | `string`   | ISO 4217 currency code (e.g., `"USD"`) |
| `createdAt`       | `DateTimeOffset` | When the account was added to the system |

---

## GET /api/v1/accounts/{id}

Returns a single account by ID (existing endpoint, unchanged).

### Path Parameters

| Parameter | Type   | Required | Description |
|----------|--------|----------|-------------|
| `id`      | `Guid` | **Yes**  | Account identifier |

### Response: 200 OK

Returns a single `AccountSummary` object (same shape as items in list above).

### Response: 404 Not Found

```json
{
  "status": 404,
  "title": "Account not found.",
  "detail": "No account with ID 'a1b2c3d4-0000-0000-0000-999999999999' exists."
}
```

---

## Error Responses

### 400 Bad Request — invalid parameters

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "pageSize": ["PageSize must be between 1 and 200."]
  }
}
```

---

## Example Requests

**All active accounts:**
```
GET /api/v1/accounts?isActive=true
```

**Checking accounts only:**
```
GET /api/v1/accounts?accountType=Checking
```

**All accounts, paged:**
```
GET /api/v1/accounts?page=1&pageSize=10
```

**Single account:**
```
GET /api/v1/accounts/a1b2c3d4-0000-0000-0000-000000000002
```

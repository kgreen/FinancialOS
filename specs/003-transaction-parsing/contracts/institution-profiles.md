# API Contract: Institution Profiles

**Feature**: 003 — Transaction Parsing & Record Hydration
**Base path**: `/api/v1/institution-profiles`
**Date**: 2026-08-01

---

## Overview

Institution profiles describe how to parse a CSV from a specific bank. They map the bank's actual column headers to the platform's standard fields and declare the amount layout (single signed column vs. separate debit/credit columns).

Profiles are platform-global (shared across all users in a single-tenant deployment). Profiles that have been used in prior imports may not be deleted (FR-030).

---

## Standard field key names

The `columnMappings` dictionary uses these fixed key names on the left side:

| Key | Required | Description |
|-----|----------|-------------|
| `"date"` | Yes | Maps to the CSV column containing the transaction date |
| `"amount"` | When `amountLayout = "singleSigned"` | Maps to the single signed amount column |
| `"description"` | Yes | Maps to the transaction description / memo column |
| `"balance"` | No | Maps to the running balance column (if present) |
| `"reference"` | No | Maps to an external reference / check number column |

---

## `POST /api/v1/institution-profiles` — Create a profile

### Request body (`application/json`)

```json
{
  "name": "Chase Checking CSV",
  "columnMappings": {
    "date": "Transaction Date",
    "amount": "Amount",
    "description": "Description",
    "balance": "Balance"
  },
  "amountLayout": "singleSigned",
  "debitColumnName": null,
  "creditColumnName": null,
  "dateFormatPattern": "MM/dd/yyyy"
}
```

#### Split debit/credit example

```json
{
  "name": "Citi Checking CSV",
  "columnMappings": {
    "date": "Date",
    "description": "Description"
  },
  "amountLayout": "splitDebitCredit",
  "debitColumnName": "Debit",
  "creditColumnName": "Credit",
  "dateFormatPattern": null
}
```

### Field descriptions

| Field | Type | Required | Validation |
|-------|------|----------|-----------|
| `name` | `string` | Yes | Non-empty; max 200 chars; must be unique |
| `columnMappings` | `object` | Yes | Must include `"date"` and (when `singleSigned`) `"amount"` and `"description"` |
| `amountLayout` | `string` | Yes | `"singleSigned"` or `"splitDebitCredit"` |
| `debitColumnName` | `string \| null` | Required when `splitDebitCredit` | Non-empty if provided |
| `creditColumnName` | `string \| null` | Required when `splitDebitCredit` | Non-empty if provided |
| `dateFormatPattern` | `string \| null` | No | Valid .NET date format string; null = auto-detect common formats |

### Response `201 Created`

`Location: /api/v1/institution-profiles/{id}`

```json
{
  "id": "d290f1ee-6c54-4b01-90e6-d701748f0851",
  "name": "Chase Checking CSV",
  "columnMappings": {
    "date": "Transaction Date",
    "amount": "Amount",
    "description": "Description",
    "balance": "Balance"
  },
  "amountLayout": "singleSigned",
  "debitColumnName": null,
  "creditColumnName": null,
  "dateFormatPattern": "MM/dd/yyyy",
  "createdAt": "2026-08-01T12:00:00.000Z",
  "updatedAt": "2026-08-01T12:00:00.000Z"
}
```

### Response `400 Bad Request` — validation failure

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "debitColumnName": ["debitColumnName is required when amountLayout is 'splitDebitCredit'"]
  }
}
```

### Response `409 Conflict` — name already exists

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "An institution profile named 'Chase Checking CSV' already exists."
}
```

---

## `GET /api/v1/institution-profiles` — List all profiles

Returns all non-deleted institution profiles. Deleted profiles are excluded.

### Response `200 OK`

```json
[
  {
    "id": "d290f1ee-6c54-4b01-90e6-d701748f0851",
    "name": "Chase Checking CSV",
    "columnMappings": { "date": "Transaction Date", "amount": "Amount", "description": "Description" },
    "amountLayout": "singleSigned",
    "debitColumnName": null,
    "creditColumnName": null,
    "dateFormatPattern": "MM/dd/yyyy",
    "createdAt": "2026-08-01T12:00:00.000Z",
    "updatedAt": "2026-08-01T12:00:00.000Z"
  }
]
```

---

## `GET /api/v1/institution-profiles/{id}` — Get a single profile

### Response `200 OK`

Same shape as the single element above.

### Response `404 Not Found`

```json
{
  "status": 404,
  "title": "Not Found",
  "detail": "Institution profile not found."
}
```

---

## `PUT /api/v1/institution-profiles/{id}` — Update a profile

Updates a profile. Changes take effect for **future uploads only**; previously created records are unaffected (FR-028).

### Request body

Same shape as `POST` request body.

### Response `200 OK`

Updated profile object (same shape as create response).

### Response `404 Not Found`

Profile does not exist or has been deleted.

### Response `400 Bad Request`

Validation failure — same shape as POST validation error.

---

## `DELETE /api/v1/institution-profiles/{id}` — Delete a profile

Deletes a profile if it has **not** been referenced by any `ImportJob`. If the profile has import history, deletion is rejected to preserve historical auditability (FR-030). Implements soft-delete (`IsDeleted = true`); the row is retained in the database.

### Response `204 No Content`

Profile soft-deleted successfully.

### Response `409 Conflict` — profile has import history

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Institution profile 'd290f1ee-6c54-4b01-90e6-d701748f0851' cannot be deleted because it has been used in 3 import job(s). It has been retained for historical auditability."
}
```

### Response `404 Not Found`

Profile does not exist (or is already deleted).

---

## Amount layout serialization

| Enum value | JSON string |
|-----------|-------------|
| `AmountLayout.SingleSigned` | `"singleSigned"` |
| `AmountLayout.SplitDebitCredit` | `"splitDebitCredit"` |

## Parser type serialization

| Enum value | JSON string |
|-----------|-------------|
| `ParserType.CsvConfigured` | `"csvConfigured"` |
| `ParserType.CsvAutoDetected` | `"csvAutoDetected"` |
| `ParserType.Ofx` | `"ofx"` |

# API Contract: Import Jobs

**Feature**: 003 — Transaction Parsing & Record Hydration
**Base path**: `/api/v1/import-jobs`
**Date**: 2026-08-01

---

## Overview

Import jobs track a single file parsing execution. They are created automatically when a file is uploaded via `POST /api/v1/evidence`. Callers retrieve a job to audit parse results, review per-row failures, and confirm what records were created.

---

## Endpoint: `GET /api/v1/import-jobs/{id}`

Retrieves the full detail of a single import job by ID.

### Path parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | `uuid` | Import job ID (returned by `POST /api/v1/evidence`) |

### Response `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "evidenceId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "institutionProfileId": null,
  "parserType": "ofx",
  "status": "partialSuccess",
  "totalRows": 10,
  "parsedCount": 8,
  "failedRowCount": 2,
  "startedAt": "2026-08-01T16:00:00.000Z",
  "completedAt": "2026-08-01T16:00:01.123Z",
  "failedRows": [
    {
      "rowIndex": 3,
      "reason": "Missing required field: DTPOSTED"
    },
    {
      "rowIndex": 7,
      "reason": "Amount is not a valid decimal: 'N/A'"
    }
  ]
}
```

### Response `404 Not Found`

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Import job not found.",
  "instance": "/api/v1/import-jobs/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Field descriptions

| Field | Type | Notes |
|-------|------|-------|
| `id` | `uuid` | Import job identifier |
| `evidenceId` | `uuid` | The evidence artifact this job parsed |
| `institutionProfileId` | `uuid \| null` | Profile used; null for OFX or auto-detected CSV |
| `parserType` | `string` | `"csvConfigured"`, `"csvAutoDetected"`, or `"ofx"` |
| `status` | `string` | `"pending"`, `"processing"`, `"completed"`, `"partialSuccess"`, `"failed"` |
| `totalRows` | `int` | Total rows (or STMTTRN elements) scanned |
| `parsedCount` | `int` | Number of `FinancialRecord` entries created |
| `failedRowCount` | `int` | Number of rows/elements skipped |
| `startedAt` | `ISO 8601 \| null` | UTC timestamp when parsing began |
| `completedAt` | `ISO 8601 \| null` | UTC timestamp when job reached terminal status |
| `failedRows` | `array` | Per-row failure entries; empty array if none |
| `failedRows[].rowIndex` | `int` | 0-based source row index |
| `failedRows[].reason` | `string` | Human-readable failure reason |

---

## Evidence Upload Response (updated `POST /api/v1/evidence`)

> Defined in full in `contracts/institution-profiles.md` (request side). The response contract is documented here as it is the primary producer of import job IDs.

### Request

`POST /api/v1/evidence`
`Content-Type: multipart/form-data`

| Form field | Type | Required | Description |
|------------|------|----------|-------------|
| `file` | binary | Yes | The CSV, OFX, or QFX file to import |
| `institutionProfileId` | `uuid` | No | If provided, the CSV is parsed using this profile. Ignored for OFX/QFX files. |

### Response `200 OK` — successful parse

```json
{
  "evidenceId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "importJobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "completed",
  "parserType": "csvAutoDetected",
  "parsedTransactionCount": 10,
  "failedRowCount": 0,
  "records": [
    {
      "id": "a1b2c3d4-0000-0000-0000-000000000001",
      "date": "2026-07-15",
      "amount": -42.50,
      "currency": "USD",
      "description": "AMAZON.COM*XY1Z",
      "classificationStatus": "classified",
      "classificationConfidence": 0.95,
      "classificationReasonCode": "merchant_match"
    },
    {
      "id": "a1b2c3d4-0000-0000-0000-000000000002",
      "date": "2026-07-16",
      "amount": 1500.00,
      "currency": "USD",
      "description": "PAYROLL DIRECT DEPOSIT",
      "classificationStatus": "pending",
      "classificationConfidence": null,
      "classificationReasonCode": null
    }
  ]
}
```

### Response `200 OK` — duplicate file re-upload

```json
{
  "evidenceId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "importJobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "duplicate",
  "parserType": "csvAutoDetected",
  "parsedTransactionCount": 0,
  "failedRowCount": 0,
  "records": []
}
```

### Response `400 Bad Request` — zero-byte or missing file

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "A non-empty file is required."
}
```

### Response `422 Unprocessable Entity` — unsupported format

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.21",
  "title": "Unprocessable Entity",
  "status": 422,
  "detail": "File format '.pdf' is not supported. Supported formats: .csv, .ofx, .qfx"
}
```

### Response `422 Unprocessable Entity` — CSV layout undetectable, no profile provided

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.21",
  "title": "Unprocessable Entity",
  "status": 422,
  "detail": "Could not auto-detect CSV layout. Detected headers: [Trans Date, Running Balance, Withdrawal Amt, Deposit Amt]. Create an InstitutionProfile and specify its ID in the 'institutionProfileId' form field."
}
```

---

## Status value reference

| Value | Meaning |
|-------|---------|
| `"completed"` | All rows parsed; full `records` array returned |
| `"partialSuccess"` | Some rows parsed; some failed; `records` contains only successful entries |
| `"failed"` | Zero rows parsed; `records` is empty; check `GET /api/v1/import-jobs/{id}` for failures |
| `"duplicate"` | File already imported (same SHA256); no new records created; existing job ID returned |

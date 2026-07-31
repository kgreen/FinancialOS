# API Contract: FinancialOS Platform Foundation

## Evidence endpoints

### POST /api/v1/evidence

Uploads a financial evidence file for ingestion.

**Request**
- multipart/form-data with file and optional metadata

**Response**
```json
{
  "id": "guid",
  "status": "accepted",
  "sourceType": "csv",
  "fileName": "statement.csv"
}
```

### GET /api/v1/evidence/{id}

Returns immutable evidence metadata and processing state.

## Record endpoints

### GET /api/v1/records

Returns a paged list of financial records.

**Response**
```json
{
  "items": [
    {
      "id": "guid",
      "accountId": "guid",
      "amount": {
        "value": 125.50,
        "currency": "USD"
      },
      "occurredOn": "2026-01-15T00:00:00Z",
      "status": "normalized",
      "classificationConfidence": 0.91
    }
  ],
  "page": 1,
  "pageSize": 50
}
```

### POST /api/v1/records/{id}/classify

Applies or updates classification metadata for a record.

## Reference endpoints

### GET /api/v1/accounts
### GET /api/v1/categories
### GET /api/v1/merchants
### GET /api/v1/rules

All reference endpoints return stable JSON objects that can be consumed by desktop and future web/mobile clients.

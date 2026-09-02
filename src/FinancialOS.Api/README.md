# FinancialOS API

The API exposes filtered record/account/category/rule queries plus export endpoints for CSV, JSON, YNAB 4, and Goodbudget formats.

## Configuration

The API uses the `DatabaseProvider` setting to choose the EF Core provider at startup.

### Defaults

- If `DatabaseProvider` is omitted, the API defaults to `sqlite`.
- `sqlite` uses the `ConnectionStrings:Default` value.
- `postgres` uses the same connection string but through the PostgreSQL provider.

Example configuration:

```json
{
  "DatabaseProvider": "sqlite",
  "ConnectionStrings": {
    "Default": "Data Source=financialos.db"
  }
}
```

To switch providers for another environment, set `DatabaseProvider` accordingly and provide a valid connection string.

## Pagination

The API uses shared pagination defaults for list endpoints:

- default page: `1`
- default page size: `25`
- maximum page size: `200`

Requests with `page < 1` return `400 Bad Request`. Requests with `pageSize` above `200` also return `400 Bad Request`.

## Exports

Export requests support streaming responses for large result sets. The API accepts the same filters used by the list endpoints and returns the exported content in the requested format.

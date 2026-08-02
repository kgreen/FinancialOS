# Contract: Categories Endpoint (Feature 004)

**Endpoint group**: `/api/v1/categories`  
**Feature**: `004-api-exporter-ecosystem`

---

## Updated: GET /api/v1/categories

Returns a paginated, optionally filtered list of categories.

### Query Parameters

| Parameter    | Type     | Required | Default | Description |
|-------------|----------|----------|---------|-------------|
| `nameSearch` | `string` | No | — | Partial, case-insensitive match on category name (max 200 chars) |
| `parentId`   | `Guid`   | No | — | Filter to direct children of the specified parent category |
| `page`       | `int`    | No | `1`  | 1-based page number |
| `pageSize`   | `int`    | No | `25` | Categories per page (1–200) |

### Notes

- `parentId` filters to **direct children only** (one level deep); to find all descendants, make multiple requests.
- Omitting `parentId` returns all categories at all levels.
- To get only top-level (root) categories, use a special value is **not** supported in this version; omit `parentId` and filter client-side, or query with `parentId` set to each known root.

### Ordering

Results ordered by `Name` ascending.

### Response: 200 OK

**Content-Type**: `application/json`

```json
{
  "items": [
    {
      "id": "a1b2c3d4-0000-0000-0000-000000000001",
      "name": "Shopping",
      "parentId": null,
      "parentName": null,
      "description": "Retail purchases"
    },
    {
      "id": "a1b2c3d4-0000-0000-0000-000000000010",
      "name": "Electronics",
      "parentId": "a1b2c3d4-0000-0000-0000-000000000001",
      "parentName": "Shopping",
      "description": "Electronics and gadgets"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 12,
  "totalPages": 1
}
```

### `CategorySummary` fields

| Field        | Type      | Description |
|-------------|-----------|-------------|
| `id`          | `Guid`    | Category identifier |
| `name`        | `string`  | Display name |
| `parentId`    | `Guid?`   | Parent category ID; `null` for root categories |
| `parentName`  | `string?` | Parent category name; `null` for root categories |
| `description` | `string?` | Optional description |

---

## Error Responses

### 400 Bad Request

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "nameSearch": ["NameSearch must not exceed 200 characters."],
    "pageSize": ["PageSize must be between 1 and 200."]
  }
}
```

---

## Example Requests

**All categories:**
```
GET /api/v1/categories
```

**Search by name:**
```
GET /api/v1/categories?nameSearch=food
```

**Children of a specific parent:**
```
GET /api/v1/categories?parentId=a1b2c3d4-0000-0000-0000-000000000001
```

**Paged:**
```
GET /api/v1/categories?page=1&pageSize=10
```

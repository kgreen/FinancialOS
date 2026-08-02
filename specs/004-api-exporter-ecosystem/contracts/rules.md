# Contract: Rules Endpoint (Feature 004)

**Endpoint group**: `/api/v1/rules`  
**Feature**: `004-api-exporter-ecosystem`

---

## Updated: GET /api/v1/rules

Returns a paginated, optionally filtered list of categorization/normalization rules.

### Query Parameters

| Parameter    | Type     | Required | Default | Description |
|-------------|----------|----------|---------|-------------|
| `ruleType`   | `string` | No | — | Filter by rule type (case-insensitive); e.g., `"MerchantMatch"`, `"CategoryAssign"` |
| `isEnabled`  | `bool`   | No | — | Filter by enabled status; omit to return all |
| `categoryId` | `Guid`   | No | — | Filter to rules that target a specific category |
| `page`       | `int`    | No | `1`  | 1-based page number |
| `pageSize`   | `int`    | No | `25` | Rules per page (1–200) |

### Ordering

Results ordered by priority descending, then by `Id` ascending.

### Response: 200 OK

**Content-Type**: `application/json`

```json
{
  "items": [
    {
      "id": "b2c3d4e5-0000-0000-0000-000000000001",
      "ruleType": "MerchantMatch",
      "pattern": "AMZN*",
      "normalizedName": "Amazon",
      "targetCategoryId": "a1b2c3d4-0000-0000-0000-000000000001",
      "targetCategoryName": "Shopping",
      "isEnabled": true,
      "priority": 100,
      "description": "Normalize all Amazon charges"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 8,
  "totalPages": 1
}
```

### `RuleSummary` fields

| Field                | Type      | Description |
|---------------------|-----------|-------------|
| `id`                  | `Guid`    | Rule identifier |
| `ruleType`            | `string`  | Rule type discriminator |
| `pattern`             | `string`  | The match pattern (e.g., glob or regex depending on rule type) |
| `normalizedName`      | `string?` | Normalized merchant name to apply; `null` if not a normalization rule |
| `targetCategoryId`    | `Guid?`   | Target category ID; `null` if rule does not assign a category |
| `targetCategoryName`  | `string?` | Target category name |
| `isEnabled`           | `bool`    | Whether this rule is active |
| `priority`            | `int`     | Evaluation order; higher values evaluated first |
| `description`         | `string?` | Human-readable description of the rule |

---

## Error Responses

### 400 Bad Request

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

**All enabled rules:**
```
GET /api/v1/rules?isEnabled=true
```

**Rules targeting a specific category:**
```
GET /api/v1/rules?categoryId=a1b2c3d4-0000-0000-0000-000000000001
```

**Rules by type:**
```
GET /api/v1/rules?ruleType=MerchantMatch
```

**Disabled rules only:**
```
GET /api/v1/rules?isEnabled=false
```

**Paged:**
```
GET /api/v1/rules?page=1&pageSize=10
```

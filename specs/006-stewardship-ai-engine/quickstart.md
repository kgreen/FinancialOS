# Quickstart: Stewardship & AI Engine

**Feature**: `006-stewardship-ai-engine`
**Date**: 2026-09-02

---

## Prerequisites

- .NET 8 SDK installed
- The FinancialOS API project can be started locally
- At least one imported financial record set (either from the existing import pipeline or seeded test data)

## 1. Start the API

```bash
dotnet restore
DOTNET_ENVIRONMENT=Development dotnet run --project src/FinancialOS.Api
```

The API should become available at `http://localhost:5000` (or the configured ASP.NET Core URL).

## 2. Create a simple goal

```bash
curl -X POST http://localhost:5000/api/v1/goals \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Emergency fund",
    "type": "savings",
    "targetAmount": 1500,
    "currency": "USD",
    "period": "monthly",
    "startDate": "2026-09-01",
    "endDate": "2026-09-30"
  }'
```

Expected outcome:
- `201 Created`
- Response body includes the new goal with an identifier and the submitted values.

## 3. Create a simple budget

```bash
curl -X POST http://localhost:5000/api/v1/budgets \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Dining out",
    "amount": 250,
    "currency": "USD",
    "period": "monthly",
    "startDate": "2026-09-01",
    "endDate": "2026-09-30",
    "categoryId": "00000000-0000-0000-0000-000000000000"
  }'
```

Expected outcome:
- `201 Created`
- The budget is available for subsequent insight generation.

## 4. Generate stewardship insights

```bash
curl -X POST http://localhost:5000/api/v1/insights \
  -H "Content-Type: application/json" \
  -d '{
    "startDate": "2026-09-01",
    "endDate": "2026-09-30",
    "goalIds": ["<goal-id>"],
    "budgetIds": ["<budget-id>"]
  }'
```

Expected outcome:
- `200 OK`
- Response includes spending totals, concentration metrics, goal/budget progress snapshots, and explainability evidence.

## 5. Request advisor guidance

```bash
curl -X POST http://localhost:5000/api/v1/advisor/recommendations \
  -H "Content-Type: application/json" \
  -d '{
    "startDate": "2026-09-01",
    "endDate": "2026-09-30",
    "goalIds": ["<goal-id>"],
    "budgetIds": ["<budget-id>"]
  }'
```

Expected outcome:
- `200 OK`
- Response contains a recommendation summary, rationale, evidence references, confidence, and a suggested action.

## 6. Validate fallback behavior

If the advisor provider is disabled or misconfigured, the service should still return a recommendation with a `status` such as `fallback` and a clear rationale explaining the deterministic fallback path.

## Notes

- The insights and advisor endpoints should be usable even when the request has insufficient history; in that case the response should provide a clear empty-state message rather than failing silently.
- These scenarios are intended to validate the MVP flow end-to-end without requiring direct database access.

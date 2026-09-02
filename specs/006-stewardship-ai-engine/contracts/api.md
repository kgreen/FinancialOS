# Contract: Stewardship & AI Engine API (Feature 006)

**Feature**: `006-stewardship-ai-engine`
**Date**: 2026-09-02

---

## Overview

The stewardship and AI experience is exposed through the API as a set of typed endpoints over the existing FinancialOS API boundary. These endpoints are intentionally API-first so the desktop/web clients can consume the same contracts without direct database access.

## Endpoint Group: Goals

### POST `/api/v1/goals`

Creates a new goal.

#### Request body

```json
{
  "name": "Emergency fund",
  "type": "savings",
  "targetAmount": 1500,
  "currency": "USD",
  "period": "monthly",
  "startDate": "2026-09-01",
  "endDate": "2026-09-30",
  "accountId": null,
  "categoryId": null
}
```

#### Responses

- `201 Created` — returns the created goal.
- `400 Bad Request` — validation failed.

#### Goal response shape

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Emergency fund",
  "type": "savings",
  "targetAmount": 1500,
  "currency": "USD",
  "period": "monthly",
  "startDate": "2026-09-01",
  "endDate": "2026-09-30",
  "accountId": null,
  "categoryId": null,
  "isActive": true,
  "createdAt": "2026-09-02T10:00:00Z",
  "updatedAt": "2026-09-02T10:00:00Z"
}
```

### GET `/api/v1/goals/{id}`

Returns a single goal.

### PUT `/api/v1/goals/{id}`

Updates an existing goal.

### DELETE `/api/v1/goals/{id}`

Deletes an existing goal.

---

## Endpoint Group: Budgets

### POST `/api/v1/budgets`

Creates a new budget.

#### Request body

```json
{
  "name": "Dining out",
  "amount": 250,
  "currency": "USD",
  "period": "monthly",
  "startDate": "2026-09-01",
  "endDate": "2026-09-30",
  "accountId": null,
  "categoryId": "00000000-0000-0000-0000-000000000000"
}
```

#### Responses

- `201 Created` — returns the created budget.
- `400 Bad Request` — validation failed.

### GET `/api/v1/budgets/{id}`

Returns a single budget.

### PUT `/api/v1/budgets/{id}`

Updates an existing budget.

### DELETE `/api/v1/budgets/{id}`

Deletes an existing budget.

---

## Endpoint Group: Insights

### POST `/api/v1/insights`

Generates a stewardship insight summary for the requested scope.

#### Request body

```json
{
  "startDate": "2026-09-01",
  "endDate": "2026-09-30",
  "accountId": null,
  "categoryId": null,
  "goalIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"],
  "budgetIds": ["11111111-2222-3333-4444-555555555555"]
}
```

#### Responses

- `200 OK` — returns a `StewardshipInsight` payload.
- `400 Bad Request` — validation failed.
- `200 OK` — empty-state response if the result set is insufficient for meaningful analysis.

#### Insight response shape

```json
{
  "startDate": "2026-09-01",
  "endDate": "2026-09-30",
  "totalSpending": 672.45,
  "totalIncome": 2800.00,
  "netFlow": 2127.55,
  "trendDirection": "improving",
  "categoryConcentration": 0.68,
  "goalProgress": [
    {
      "goalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "goalName": "Emergency fund",
      "currentAmount": 930.0,
      "targetAmount": 1500.0,
      "percentComplete": 62.0,
      "status": "behind"
    }
  ],
  "budgetProgress": [
    {
      "budgetId": "11111111-2222-3333-4444-555555555555",
      "budgetName": "Dining out",
      "spentAmount": 186.0,
      "budgetAmount": 250.0,
      "percentUsed": 74.4,
      "status": "onTrack"
    }
  ],
  "evidence": [
    {
      "type": "category",
      "label": "Dining out",
      "detail": "Spent 186.00 across 4 transactions"
    }
  ],
  "alignmentStatus": "mixed",
  "generatedAt": "2026-09-02T10:00:00Z"
}
```

---

## Endpoint Group: Advisor Recommendations

### POST `/api/v1/advisor/recommendations`

Generates an explainable recommendation for the selected scope.

#### Request body

```json
{
  "startDate": "2026-09-01",
  "endDate": "2026-09-30",
  "accountId": null,
  "categoryId": null,
  "goalIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"],
  "budgetIds": ["11111111-2222-3333-4444-555555555555"]
}
```

#### Responses

- `200 OK` — returns an `AdvisorRecommendation` payload.
- `400 Bad Request` — validation failed.
- `200 OK` — fallback response with `status` set to `fallback` when the provider is unavailable or misconfigured.

#### Recommendation response shape

```json
{
  "summary": "Reduce discretionary dining spend this month.",
  "rationale": "Dining out transactions account for 27% of the selected spending, while the dining budget is already 74% used.",
  "evidence": [
    {
      "type": "budget",
      "label": "Dining out",
      "detail": "74.4% of budget consumed"
    }
  ],
  "confidence": 0.83,
  "suggestedAction": "Set a reminder to cap dining purchases at 50 USD for the remainder of the month.",
  "status": "ok",
  "generatedAt": "2026-09-02T10:00:00Z"
}
```

---

## Shared Error Contract

Invalid requests should return `application/problem+json` responses in a shape consistent with the current platform.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The request payload is invalid.",
  "errors": {
    "endDate": ["EndDate must be on or after StartDate."]
  }
}
```

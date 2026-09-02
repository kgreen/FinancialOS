# Implementation Plan: Stewardship & AI Engine

**Branch**: `006-stewardship-ai-engine` | **Date**: 2026-09-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/006-stewardship-ai-engine/spec.md`

---

## Summary

Feature 006 adds a stewardship layer on top of the existing financial record model. The first iteration introduces deterministic goals/budgets, stewardship insights that compare actual activity against planned targets, and an explainable advisor interface that can produce guidance with a fallback path when AI services are unavailable. The implementation stays API-first, uses the existing repository and EF Core abstractions, and keeps the advisor explainable by coupling every recommendation to real financial data and explicit rationale.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**:
- ASP.NET Core Minimal API
- EF Core 8 with the existing SQLite/PostgreSQL provider setup
- `System.Text.Json` for API DTOs and response envelopes
- Optional AI provider abstraction (no hard dependency on a specific LLM provider for MVP)
- xUnit + `WebApplicationFactory<Program>` for integration tests

**Storage**: Existing relational data store via EF Core; no new persistence layer is introduced

**Testing**: xUnit, integration tests under `tests/FinancialOS.Api.Tests/`, plus targeted core/domain tests if needed

**Target Platform**: Cross-platform API with Windows-first desktop/web consumption later

**Performance Goals**: Insight generation for a seeded dataset of a few thousand records should complete in a single request without requiring a background job

**Constraints**: Explainability is not optional; all recommendations must include rationale and source data references. AI integration must be optional and degrade gracefully.

**Scale/Scope**: Personal finance MVP; single-user local-first usage with future extension potential for richer charts and scenario planning

---

## Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| Truth before convenience | ✅ Pass | Insights are derived from existing records and goals rather than mutating source data |
| Facts are immutable | ✅ Pass | No changes to imported financial records are required for analysis |
| Explainability is required | ✅ Pass | Advisor responses must include rationale and evidence references |
| Humans contain authority | ✅ Pass | Recommendations remain advisory and do not overwrite financial truth |
| Knowledge before intelligence | ✅ Pass | Deterministic goal/budget evaluation comes first; AI is a secondary enhancement |
| Modular & API-first | ✅ Pass | The feature is implemented through services and API contracts rather than direct UI/database coupling |

---

## Project Structure

### Documentation (this feature)

```text
specs/006-stewardship-ai-engine/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── api.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── FinancialOS.Core/
│   ├── Contracts/
│   │   ├── IGoalService.cs
│   │   ├── IInsightService.cs
│   │   └── IAdvisorService.cs
│   └── Models/
│       ├── Goal.cs
│       ├── Budget.cs
│       ├── StewardshipInsight.cs
│       ├── AdvisorRecommendation.cs
│       ├── InsightRequest.cs
│       └── RecommendationRequest.cs
│
├── FinancialOS.Data/
│   └── EfFinancialRepository.cs
│
├── FinancialOS.Api/
│   ├── Endpoints/
│   │   ├── GoalsEndpoints.cs
│   │   ├── BudgetsEndpoints.cs
│   │   ├── InsightsEndpoints.cs
│   │   └── AdvisorEndpoints.cs
│   ├── QueryModels/
│   │   ├── GoalQuery.cs
│   │   ├── BudgetQuery.cs
│   │   └── InsightQuery.cs
│   └── Program.cs
│
tests/
└── FinancialOS.Api.Tests/
    ├── StewardshipInsightsTests.cs
    ├── GoalAndBudgetTests.cs
    └── AdvisorRecommendationTests.cs
```

**Structure Decision**: Extend the existing Clean Architecture layout rather than adding a new project. Core domain types and contracts remain in `FinancialOS.Core`; persistence implementation stays in `FinancialOS.Data`; API endpoints and DTO/query models live in `FinancialOS.Api`; integration tests remain in `tests/FinancialOS.Api.Tests/`.

---

## Implementation Approach

### Phase 0 — Domain Modeling

1. Add goal, budget, insight, and advisor recommendation models to `FinancialOS.Core/Models/`.
2. Define service contracts for goal management, insights, and advisor recommendations.
3. Add request/response DTOs and validation rules for date ranges, budget amounts, and goal targets.

### Phase 1 — Persistence & Querying

1. Extend the repository contract and EF implementation to support CRUD-like operations for goals and budgets.
2. Add deterministic insight-generation logic that aggregates spending by category/account over a requested date range.
3. Implement comparison rules between actual activity and goals/budgets, returning clear status values such as `OnTrack`, `Behind`, or `OverBudget`.

### Phase 2 — API Surface

1. Add API endpoints for goals, budgets, insights, and advisor recommendations.
2. Validate request parameters and return problem-details-style errors for invalid input.
3. Keep the endpoints API-first and return typed DTOs that are suitable for future UI consumption.

### Phase 3 — Explainable Advisor Layer

1. Introduce an advisor abstraction with a default deterministic implementation and an optional AI-backed implementation behind configuration.
2. Ensure every recommendation contains rationale, evidence references, and a confidence indicator.
3. If AI is disabled or unavailable, fall back to the deterministic implementation rather than failing.

### Phase 4 — Tests & Validation

1. Add integration tests for creating goals/budgets, generating insights, and evaluating progress.
2. Add tests for advisor fallback and explainability metadata.
3. Validate the feature end-to-end using the API and a seeded dataset.

---

## Complexity Tracking

No constitution violations are expected. The feature extends the existing architecture with additional domain models and API endpoints, but it does not introduce a new persistence layer or bypass the API boundary.

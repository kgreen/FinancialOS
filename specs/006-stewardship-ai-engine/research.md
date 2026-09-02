# Research: Stewardship & AI Engine

**Feature**: `006-stewardship-ai-engine`
**Date**: 2026-09-02
**Status**: Complete — all MVP technical unknowns resolved

---

## R-1 — Persistence Strategy for Goals and Budgets

### Decision: Persist goals and budgets as first-class EF Core entities in the existing relational store; keep insights and recommendations as derived response types rather than durable database entities.

### Rationale

- The repository already uses EF Core and the API boundary is the primary integration point, so extending the existing relational model is the least disruptive approach.
- Goals and budgets are user-owned planning artifacts that should survive application restarts and be queryable through the API.
- Insights and advisor outputs are derived from current records and goals/budgets; persisting them would create stale data and complicate correctness guarantees.

### Alternatives considered

| Option | Reason rejected |
|--------|----------------|
| New document store or NoSQL collection | Adds a second persistence technology and breaks the current local-first EF Core pattern. |
| Persist every insight/recommendation as a row | Increases write amplification and risks stale or inconsistent summaries. |
| Keep goals/budgets in-memory only | Fails the requirement for CRUD and persistence across sessions. |

---

## R-2 — Deterministic Insight Computation Model

### Decision: Generate stewardship insights from a deterministic aggregation pipeline that evaluates the selected date range against imported financial records, goals, and budgets.

### Rationale

- The constitution requires knowledge-before-intelligence, so the first iteration should make the core insight logic auditable and reproducible.
- Deterministic summaries can still be surfaced through the API immediately and can later be enhanced with AI explanation layers.
- The approach aligns with the existing repository abstractions: load records, filter them by scope, aggregate them, compare them to planning targets, and emit a result object with explicit status.

### Proposed algorithm

1. Load the relevant financial records from the repository for the requested date range and optional account/category filters.
2. Aggregate expenses and incomes by category and account for the selected period.
3. Compare the totals to each selected goal or budget.
4. Return a structured insight payload with:
   - total spending and trend direction,
   - category concentration metrics,
   - goal/budget progress values,
   - status codes such as `OnTrack`, `Behind`, `OverBudget`, or `InsufficientData`.

### Alternatives considered

| Option | Reason rejected |
|--------|----------------|
| AI-only summarization with no deterministic baseline | Violates explainability and would hide the reasoning chain. |
| Complex scenario-planning engine in MVP | Too much scope for the initial stewardship milestone. |
| Derived metrics only in the UI | Breaks the API-first architecture. |

---

## R-3 — Advisor Design and Fallback Strategy

### Decision: Introduce an advisor contract with a default deterministic implementation and an optional provider adapter for future AI integrations; if the provider is unavailable or misconfigured, the system returns a deterministic fallback recommendation.

### Rationale

- The feature explicitly requires explainability and graceful degradation.
- A default rule-based implementation gives the MVP a reliable path even when no AI provider is configured.
- The interface remains extensible for later LLM or model-backed providers without changing the API contract.

### Proposed contract shape

- `IAdvisorService` exposes `GetRecommendationAsync(RecommendationRequest, CancellationToken)`.
- The default implementation produces a recommendation from recent spending trends, selected goals/budgets, and explainability metadata.
- An optional provider adapter can be registered behind configuration, but it is not required for the first release.

### Alternatives considered

| Option | Reason rejected |
|--------|----------------|
| Hard dependency on an LLM provider | Makes local development brittle and violates graceful fallback requirements. |
| Black-box AI-only advice | Fails the rationale and evidence requirement. |
| Recommendation generation in the UI | Violates the API-first architecture and prevents reuse. |

---

## R-4 — Validation and Error Handling

### Decision: Validate DTOs at the API boundary and enforce service-level validation for date ranges, amount values, and zero/empty target values; return problem-details-style responses for invalid input.

### Rationale

- The spec calls for clear validation errors for invalid goals, budgets, dates, and thresholds.
- A uniform validation flow keeps the service logic deterministic and makes the API behavior predictable for clients.
- The existing API already uses `ProblemDetails` and exception handling, so reusing that pattern is consistent with the current platform.

### Validation rules

- Date ranges must be well-formed and `endDate >= startDate`.
- Amount values must be positive for goals/budgets and non-negative when used in progress calculations.
- Goal/budget names, currencies, and scopes must be non-empty and normalized before evaluation.
- Advisor requests must include a valid scope and at least one evidence source context (records, goals, or budgets).

### Alternatives considered

| Option | Reason rejected |
|--------|----------------|
| Silent fallback on invalid requests | Makes debugging and client integration harder. |
| UI-only validation | Leaves the API unsafe and inconsistent. |

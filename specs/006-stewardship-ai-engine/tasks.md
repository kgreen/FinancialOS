# Tasks: Stewardship & AI Engine

**Input**: Design documents from `/specs/006-stewardship-ai-engine/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/api.md

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the stewardship feature skeleton and align the new API/data/test files with the existing FinancialOS structure.

- [ ] T001 Create new stewardship source and test scaffolding in src/FinancialOS.Core/Models/, src/FinancialOS.Core/Contracts/, src/FinancialOS.Api/Endpoints/, and tests/FinancialOS.Api.Tests/
- [ ] T002 [P] Add shared stewardship validation helpers and problem-details handling in src/FinancialOS.Api/Validation/StewardshipValidation.cs
- [ ] T003 [P] Add initial feature documentation placeholders in specs/006-stewardship-ai-engine/contracts/api.md and specs/006-stewardship-ai-engine/quickstart.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish persistence, service registration, and shared validation before any story implementation can begin.

**⚠️ CRITICAL**: No user story work should begin until this phase is complete.

- [ ] T004 Add EF Core persistence support for goals and budgets in src/FinancialOS.Data/FinancialOsDbContext.cs and add a migration scaffold in src/FinancialOS.Data/Migrations/
- [ ] T005 Extend the repository contract and EF implementation for goal/budget CRUD in src/FinancialOS.Core/Contracts/IFinancialRepository.cs and src/FinancialOS.Data/EfFinancialRepository.cs
- [ ] T006 Implement shared stewardship service registration and endpoint mapping in src/FinancialOS.Api/Program.cs
- [ ] T007 Add shared request validation rules for dates, amounts, names, and scope filters in src/FinancialOS.Core/Models/ and src/FinancialOS.Api/Validation/

**Checkpoint**: The foundation is ready for story implementation.

---

## Phase 3: User Story 1 - Understand spending patterns and alignment with priorities (Priority: P1) 🎯 MVP

**Goal**: Deliver deterministic stewardship insights that summarize spending behavior, concentration, and progress against goals and budgets.

**Independent Test**: Seed financial records and verify that the API returns an insight summary for a selected range with explainable evidence and an empty-state fallback when insufficient data is available.

### Tests for User Story 1

- [ ] T008 [P] [US1] Add an integration test for insight generation from seeded records in tests/FinancialOS.Api.Tests/StewardshipInsightsTests.cs
- [ ] T009 [P] [US1] Add an integration test for insufficient-data fallback behavior in tests/FinancialOS.Api.Tests/StewardshipInsightsTests.cs

### Implementation for User Story 1

- [ ] T010 [P] [US1] Add insight request/response models and evidence types in src/FinancialOS.Core/Models/InsightRequest.cs, src/FinancialOS.Core/Models/StewardshipInsight.cs, src/FinancialOS.Core/Models/GoalProgressSnapshot.cs, src/FinancialOS.Core/Models/BudgetProgressSnapshot.cs, and src/FinancialOS.Core/Models/EvidenceReference.cs
- [ ] T011 [US1] Implement deterministic insight aggregation logic in src/FinancialOS.Core/Services/StewardshipInsightService.cs using imported records, goals, and budgets
- [ ] T012 [US1] Add the `/api/v1/insights` endpoint and DTO mapping in src/FinancialOS.Api/Endpoints/InsightsEndpoints.cs
- [ ] T013 [US1] Wire the insight service into src/FinancialOS.Api/Program.cs and return problem-details-style validation errors for invalid date ranges and filters

**Checkpoint**: User Story 1 should be fully functional and independently testable.

---

## Phase 4: User Story 2 - Create and manage simple goals and budgets (Priority: P1)

**Goal**: Support CRUD for goals and budgets so users can define planning targets and evaluate progress over time.

**Independent Test**: Create, update, and delete a goal or budget through the API and verify that the next insight calculation reflects the stored values.

### Tests for User Story 2

- [ ] T014 [P] [US2] Add API-level tests for goal and budget create/read/update/delete flows in tests/FinancialOS.Api.Tests/GoalAndBudgetTests.cs
- [ ] T015 [P] [US2] Add integration tests for progress evaluation after updating a goal or budget in tests/FinancialOS.Api.Tests/GoalAndBudgetTests.cs

### Implementation for User Story 2

- [ ] T016 [P] [US2] Add Goal and Budget domain models plus enums in src/FinancialOS.Core/Models/Goal.cs, src/FinancialOS.Core/Models/Budget.cs, src/FinancialOS.Core/Models/GoalType.cs, src/FinancialOS.Core/Models/GoalPeriod.cs, and src/FinancialOS.Core/Models/BudgetPeriod.cs
- [ ] T017 [US2] Implement goal and budget service methods and persistence logic in src/FinancialOS.Core/Contracts/IGoalService.cs and src/FinancialOS.Data/EfFinancialRepository.cs
- [ ] T018 [US2] Add `/api/v1/goals` and `/api/v1/budgets` endpoints with request/response DTOs in src/FinancialOS.Api/Endpoints/GoalsEndpoints.cs and src/FinancialOS.Api/Endpoints/BudgetsEndpoints.cs
- [ ] T019 [US2] Add validation for empty names, invalid amounts, and date-range mismatches for goal and budget submissions in src/FinancialOS.Api/Validation/StewardshipValidation.cs

**Checkpoint**: User Story 2 should be independently functional and usable from the API.

---

## Phase 5: User Story 3 - Receive explainable guidance from the AI advisor (Priority: P2)

**Goal**: Provide explainable recommendations with deterministic fallback behavior when no AI provider is configured.

**Independent Test**: Trigger the advisor endpoint with seeded records and goals/budgets, then verify that the response includes rationale, evidence, confidence, and a graceful fallback status when the provider is unavailable.

### Tests for User Story 3

- [ ] T020 [P] [US3] Add advisor contract and fallback tests in tests/FinancialOS.Api.Tests/AdvisorRecommendationTests.cs
- [ ] T021 [P] [US3] Add integration tests for provider-disabled fallback and explanation metadata in tests/FinancialOS.Api.Tests/AdvisorRecommendationTests.cs

### Implementation for User Story 3

- [ ] T022 [P] [US3] Add advisor request/response models in src/FinancialOS.Core/Models/RecommendationRequest.cs, src/FinancialOS.Core/Models/AdvisorRecommendation.cs, and src/FinancialOS.Core/Models/EvidenceReference.cs
- [ ] T023 [US3] Implement deterministic advisor recommendation generation with rationale and confidence scoring in src/FinancialOS.Core/Services/AdvisorService.cs
- [ ] T024 [US3] Add an advisor service contract and fallback registration for optional AI-backed implementations in src/FinancialOS.Core/Contracts/IAdvisorService.cs and src/FinancialOS.Api/Program.cs
- [ ] T025 [US3] Add the `/api/v1/advisor/recommendations` endpoint and wire it to the advisor service in src/FinancialOS.Api/Endpoints/AdvisorEndpoints.cs

**Checkpoint**: User Story 3 should be independently functional and explainable even when AI is unavailable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finalize documentation, ensure consistency across the stewardship surfaces, and validate the end-to-end workflow.

- [ ] T026 [P] Update the feature contract and quickstart examples to reflect the implemented JSON payloads and validation behavior in specs/006-stewardship-ai-engine/contracts/api.md and specs/006-stewardship-ai-engine/quickstart.md
- [ ] T027 [P] Add or update stewardship-focused API documentation comments and response examples in src/FinancialOS.Api/Endpoints/GoalsEndpoints.cs, src/FinancialOS.Api/Endpoints/BudgetsEndpoints.cs, src/FinancialOS.Api/Endpoints/InsightsEndpoints.cs, and src/FinancialOS.Api/Endpoints/AdvisorEndpoints.cs
- [ ] T028 Run the stewardship API test suite and fix regressions across tests/FinancialOS.Api.Tests/ and the affected core/data/api projects
- [ ] T029 Review explainability metadata, fallback status values, and deterministic outputs across goals, budgets, insights, and advisor endpoints before closing the feature

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion and should be implemented first for the MVP
- **User Story 2 (Phase 4)**: Depends on Foundational completion and can proceed in parallel with US1 once shared services are in place
- **User Story 3 (Phase 5)**: Depends on Foundational completion and can be implemented once insights and goals/budgets are available
- **Polish (Phase 6)**: Depends on all desired stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependency on other stories; it is the primary MVP path
- **User Story 2 (P1)**: No dependency on US1 for the CRUD path, but insight evaluation should consume the persisted goals/budgets
- **User Story 3 (P2)**: Depends on the model and data shape produced by US1 and US2, but should remain independently testable with a deterministic fallback path

### Parallel Opportunities

- Setup tasks T002 and T003 can run in parallel
- Foundational tasks T004 through T007 can be split across persistence, service wiring, and validation work
- Tests for US1, US2, and US3 can be authored in parallel once the shared foundation is ready
- Models for US1 and US2 can be introduced in parallel before their service and endpoint tasks are integrated

## Parallel Example: User Story 1

```text
- Add insight model types in src/FinancialOS.Core/Models/
- Add the insights endpoint in src/FinancialOS.Api/Endpoints/InsightsEndpoints.cs
- Add insight integration tests in tests/FinancialOS.Api.Tests/StewardshipInsightsTests.cs
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (critical block)
3. Complete Phase 3: User Story 1
4. Validate the insights endpoint independently with the seeded test dataset
5. Extend to User Story 2 and User Story 3 only after the MVP path is working

### Incremental Delivery

1. Deliver shared persistence and validation infrastructure first
2. Add insights to unlock the primary stewardship story
3. Add goals and budgets to make the insights actionable
4. Add advisor guidance as the explainable enhancement layer
5. Finish with polish tasks and end-to-end validation

# Tasks: Knowledge, Rules & Deduplication

**Input**: Design documents from `/specs/002-knowledge-rules-deduplication/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md, constitution.md

**Tests**: Contract, integration, and unit tests are included per feature request.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create project/test scaffolding for Phase 2 implementation.

- [x] T001 Create core unit test project in tests/FinancialOS.Core.Tests/FinancialOS.Core.Tests.csproj
- [x] T002 Add FinancialOS.Core.Tests project to FinancialOS.sln
- [x] T003 [P] Create API knowledge test fixture and seeded helpers in tests/FinancialOS.Api.Tests/KnowledgeTestFixture.cs
- [x] T004 [P] Add shared deterministic/assertion helpers in tests/FinancialOS.Api.Tests/KnowledgeAssertions.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core data/contracts/infrastructure required before any user story implementation.

**⚠️ CRITICAL**: Complete this phase before starting user stories.

- [x] T005 Add knowledge domain entities and enums in src/FinancialOS.Core/Models/KnowledgeEntities.cs
- [x] T006 Add immutable provenance append-only domain rules in src/FinancialOS.Core/Models/KnowledgeEntities.cs
- [x] T007 [P] Add knowledge repository contracts in src/FinancialOS.Core/Contracts/IFinancialRepository.cs
- [x] T008 Implement knowledge DbSets and model configuration in src/FinancialOS.Data/FinancialOsDbContext.cs
- [x] T009 Generate EF migration AddKnowledgeRulesDeduplication in src/FinancialOS.Data/Migrations/
- [x] T010 Implement repository persistence/query methods for knowledge entities in src/FinancialOS.Data/EfFinancialRepository.cs
- [x] T011 [P] Add knowledge API DTO contracts in src/FinancialOS.Shared/Contracts/KnowledgeDtos.cs
- [x] T012 [P] Add request validators for knowledge endpoints in src/FinancialOS.Api/Validation/KnowledgeValidation.cs
- [x] T013 Add deterministic pipeline service contracts in src/FinancialOS.Core/Contracts/KnowledgeServices.cs
- [x] T014 Add canonical merchant/alias/rule seed data in src/FinancialOS.Data/DatabaseConfiguration.cs

**Checkpoint**: Foundation ready for independent story delivery.

---

## Phase 3: User Story 1 - Apply deterministic classification rules (Priority: P1) 🎯 MVP

**Goal**: Allow users to manage deterministic classification rules and apply explainable outcomes.

**Independent Test**: Create prioritized rules, process a record repeatedly, and verify stable rule selection with provenance.

### Tests for User Story 1

- [x] T015 [P] [US1] Add rules management contract tests for POST/GET/PATCH in tests/FinancialOS.Api.Tests/RulesContractTests.cs
- [x] T016 [P] [US1] Add deterministic replay integration tests for rule ordering in tests/FinancialOS.Api.Tests/RuleDeterminismIntegrationTests.cs
- [x] T017 [P] [US1] Add rule tie-breaker unit tests for ordering logic in tests/FinancialOS.Core.Tests/RuleOrderingServiceTests.cs

### Implementation for User Story 1

- [x] T018 [P] [US1] Implement deterministic rule ordering/evaluation service in src/FinancialOS.Core/Knowledge/Rules/RuleEvaluationService.cs
- [x] T019 [P] [US1] Implement rule management service (create/activate/deactivate/reprioritize) in src/FinancialOS.Core/Knowledge/Rules/RuleManagementService.cs
- [x] T020 [US1] Implement rules API endpoints in src/FinancialOS.Api/Endpoints/RulesEndpoints.cs
- [x] T021 [US1] Wire rules endpoints and services in src/FinancialOS.Api/Program.cs
- [x] T022 [US1] Persist rule evaluation provenance entries in src/FinancialOS.Core/Knowledge/Provenance/ProvenanceWriter.cs

**Checkpoint**: US1 is independently functional, deterministic, and explainable.

---

## Phase 4: User Story 2 - Normalize merchant identities and categories (Priority: P1)

**Goal**: Resolve noisy merchant text into canonical identities while preserving immutable raw facts.

**Independent Test**: Process variant merchant records and verify canonical resolution (or unresolved state) with reason codes.

### Tests for User Story 2

- [x] T023 [P] [US2] Add normalization/alias contract tests for POST aliases and POST normalize in tests/FinancialOS.Api.Tests/NormalizationContractTests.cs
- [x] T024 [P] [US2] Add alias-resolution integration tests (resolved and unresolved paths) in tests/FinancialOS.Api.Tests/NormalizationIntegrationTests.cs
- [x] T025 [P] [US2] Add merchant alias matching unit tests in tests/FinancialOS.Core.Tests/MerchantNormalizationServiceTests.cs

### Implementation for User Story 2

- [x] T026 [P] [US2] Implement canonical merchant and alias management service in src/FinancialOS.Core/Knowledge/Normalization/MerchantAliasService.cs
- [x] T027 [P] [US2] Implement normalization decision pipeline service in src/FinancialOS.Core/Knowledge/Normalization/NormalizationPipelineService.cs
- [x] T028 [US2] Implement normalization and alias API endpoints in src/FinancialOS.Api/Endpoints/NormalizationEndpoints.cs
- [x] T029 [US2] Extend normalization response and reason-code mappings in src/FinancialOS.Shared/Contracts/KnowledgeDtos.cs
- [x] T030 [US2] Wire normalization endpoints/services in src/FinancialOS.Api/Program.cs
- [x] T031 [US2] Persist normalization decisions and provenance lineage in src/FinancialOS.Data/EfFinancialRepository.cs

**Checkpoint**: US2 is independently functional with deterministic-first normalization and human-review fallback.

---

## Phase 5: User Story 3 - Detect duplicates with full audit trail (Priority: P1)

**Goal**: Produce explainable duplicate candidates and require explicit human confirmation/dismissal.

**Independent Test**: Evaluate duplicates on overlapping imports, confirm/dismiss candidates, and verify append-only provenance growth.

### Tests for User Story 3

- [x] T032 [P] [US3] Add duplicate evaluate/list/review contract tests in tests/FinancialOS.Api.Tests/DuplicateWorkflowContractTests.cs
- [x] T033 [P] [US3] Add duplicate lifecycle integration tests (PendingReview→Confirmed/Dismissed) in tests/FinancialOS.Api.Tests/DuplicateReviewIntegrationTests.cs
- [x] T034 [P] [US3] Add duplicate scoring unit tests for signal weighting in tests/FinancialOS.Core.Tests/DuplicateScoringServiceTests.cs

### Implementation for User Story 3

- [x] T035 [P] [US3] Implement duplicate heuristic scoring service in src/FinancialOS.Core/Knowledge/Deduplication/DuplicateScoringService.cs
- [x] T036 [P] [US3] Implement duplicate candidate evaluation and review service in src/FinancialOS.Core/Knowledge/Deduplication/DuplicateReviewService.cs
- [x] T037 [US3] Implement duplicate evaluate/list/confirm/dismiss endpoints in src/FinancialOS.Api/Endpoints/DuplicateEndpoints.cs
- [x] T038 [US3] Implement actor identity requirement for confirm/dismiss actions in src/FinancialOS.Api/Validation/ActorIdentityEndpointFilter.cs
- [x] T039 [US3] Wire duplicate endpoints/services in src/FinancialOS.Api/Program.cs
- [x] T040 [US3] Persist duplicate candidates and review provenance events in src/FinancialOS.Data/EfFinancialRepository.cs

**Checkpoint**: US3 is independently functional with human authority and immutable audit trail.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final hardening across all stories.

- [ ] T041 [P] Add immutable provenance regression tests (no update/delete paths) in tests/FinancialOS.Api.Tests/ProvenanceImmutabilityTests.cs
- [ ] T042 Add provenance query endpoint and timeline response shaping in src/FinancialOS.Api/Endpoints/ProvenanceEndpoints.cs
- [ ] T043 Add cross-story explainability integration test coverage in tests/FinancialOS.Api.Tests/ExplainabilityCoverageIntegrationTests.cs
- [ ] T044 Add duplicate performance indexes and query tuning adjustments in src/FinancialOS.Data/FinancialOsDbContext.cs
- [ ] T045 Update quickstart validation steps for implemented endpoints in specs/002-knowledge-rules-deduplication/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies.
- **Phase 2 (Foundational)**: Depends on Phase 1; blocks all user stories.
- **Phase 3 (US1)**: Depends on Phase 2.
- **Phase 4 (US2)**: Depends on Phase 2; can run in parallel with US1 after foundational completion.
- **Phase 5 (US3)**: Depends on Phase 2; can run in parallel with US1/US2 after foundational completion.
- **Phase 6 (Polish)**: Depends on completion of selected user stories.

### User Story Dependencies

- **US1**: Starts after foundational phase; no dependency on US2/US3.
- **US2**: Starts after foundational phase; no dependency on US1/US3.
- **US3**: Starts after foundational phase; no dependency on US1/US2.

### Within Each User Story

- Execute tests first and confirm they fail before implementation.
- Implement core services before API endpoint wiring.
- Ensure provenance emission is added before marking story complete.

---

## Parallel Execution Examples

### User Story 1

```bash
T015 + T016 + T017 in parallel, then T018 + T019 in parallel, then T020-T022
```

### User Story 2

```bash
T023 + T024 + T025 in parallel, then T026 + T027 in parallel, then T028-T031
```

### User Story 3

```bash
T032 + T033 + T034 in parallel, then T035 + T036 in parallel, then T037-T040
```

---

## Implementation Strategy

### MVP First (US1)

1. Complete Phase 1 and Phase 2.
2. Deliver Phase 3 (US1) with deterministic rule application and provenance.
3. Validate US1 independently before expanding scope.

### Incremental Delivery

1. Deliver US1 (deterministic rules).
2. Deliver US2 (normalization + aliasing).
3. Deliver US3 (duplicate stewardship workflow).
4. Complete polish and hardening.

### Constitution Alignment Gates

- Preserve immutable source facts and append-only provenance in every phase.
- Ensure all automated outputs carry confidence and reason codes.
- Keep duplicate/classification outcomes advisory until explicit human action where required.

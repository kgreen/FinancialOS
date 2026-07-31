# Tasks: FinancialOS Platform Foundation

**Input**: Design documents from `specs/001-financialos-platform/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic solution structure

- [ ] T001 Create the .NET solution structure for `src/FinancialOS.Core`, `src/FinancialOS.Data`, `src/FinancialOS.Infrastructure`, `src/FinancialOS.Api`, `src/FinancialOS.Desktop`, `src/FinancialOS.Shared`, and `tests/FinancialOS.Core.Tests` / `tests/FinancialOS.Api.Tests`
- [ ] T002 Initialize the .NET solution and add EF Core, ASP.NET Core, and test project dependencies
- [ ] T003 [P] Configure solution-level build, test, and formatting conventions for the new projects

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before user stories can be implemented

- [ ] T004 Create the initial domain model abstractions for `FinancialEvidence`, `FinancialEvent`, `FinancialRecord`, `Account`, `Institution`, `Merchant`, `Category`, and `Rule`
- [ ] T005 Implement value objects for `Money`, `Confidence`, and `Provenance` in the core project
- [ ] T006 Create EF Core persistence models and DbContext configuration for the foundational entities
- [ ] T007 [P] Add SQLite provider configuration and migration scaffolding for local-first development
- [ ] T008 [P] Add PostgreSQL provider configuration and environment-based provider switching support
- [ ] T009 Implement repository abstractions and basic CRUD services for core entities
- [ ] T010 Create the API project host, routing structure, and health/metadata endpoints
- [ ] T011 Implement immutable evidence storage handling and checksum-based persistence flow

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Import and preserve financial evidence (Priority: P1) 🎯 MVP

**Goal**: Allow users to upload evidence files and preserve them immutably with provenance metadata.

**Independent Test**: A sample CSV or OFX file can be uploaded through the API and the resulting evidence artifact can be retrieved with checksum and metadata.

### Implementation for User Story 1

- [ ] T012 [P] [US1] Add evidence upload DTOs and request validation in `src/FinancialOS.Api/Models/`
- [ ] T013 [P] [US1] Implement evidence ingestion service in `src/FinancialOS.Infrastructure/Import/`
- [ ] T014 [US1] Implement evidence persistence and checksum storage in `src/FinancialOS.Data/`
- [ ] T015 [US1] Add `POST /api/v1/evidence` endpoint in `src/FinancialOS.Api/Controllers/`
- [ ] T016 [US1] Add `GET /api/v1/evidence/{id}` endpoint in `src/FinancialOS.Api/Controllers/`
- [ ] T017 [US1] Add integration tests for evidence upload and retrieval in `tests/FinancialOS.Api.Tests/`

**Checkpoint**: User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Create explainable financial records from evidence (Priority: P1)

**Goal**: Turn imported evidence into canonical financial records with explainable classification data.

**Independent Test**: A user can import evidence, create a financial record, and inspect its classification confidence and provenance from the API.

### Implementation for User Story 2

- [ ] T018 [P] [US2] Add record DTOs and mapping contracts in `src/FinancialOS.Shared/`
- [ ] T019 [P] [US2] Implement parsing and normalization logic for imported transaction data in `src/FinancialOS.Infrastructure/`
- [ ] T020 [US2] Implement record creation and classification workflow in `src/FinancialOS.Core/Services/`
- [ ] T021 [US2] Persist record, account, merchant, category, and provenance relationships in `src/FinancialOS.Data/`
- [ ] T022 [US2] Add `GET /api/v1/records` endpoint in `src/FinancialOS.Api/Controllers/`
- [ ] T023 [US2] Add `POST /api/v1/records/{id}/classify` endpoint in `src/FinancialOS.Api/Controllers/`
- [ ] T024 [US2] Add integration tests for record creation and classification in `tests/FinancialOS.Api.Tests/`

**Checkpoint**: User Story 2 should be fully functional and testable independently

---

## Phase 5: User Story 3 - Use the platform through API-first clients (Priority: P2)

**Goal**: Expose stable API contracts for accounts, categories, merchants, and rules so clients can consume FinancialOS without database access.

**Independent Test**: A client can call reference endpoints and receive JSON payloads that match the documented contract.

### Implementation for User Story 3

- [ ] T025 [P] [US3] Create account, category, merchant, and rule response DTOs in `src/FinancialOS.Shared/`
- [ ] T026 [US3] Implement reference queries for accounts, categories, merchants, and rules in `src/FinancialOS.Data/`
- [ ] T027 [US3] Add `GET /api/v1/accounts`, `GET /api/v1/categories`, `GET /api/v1/merchants`, and `GET /api/v1/rules` endpoints in `src/FinancialOS.Api/Controllers/`
- [ ] T028 [US3] Add contract tests for the reference endpoints in `tests/FinancialOS.Api.Tests/`

**Checkpoint**: User Story 3 should be fully functional and testable independently

---

## Phase 6: User Story 4 - Plan future stewardship scenarios (Priority: P2)

**Goal**: Support future goal and budget planning scenarios on top of the evidence and record foundation.

**Independent Test**: A planning scenario can be created and linked to the existing financial context without breaking the core ingestion workflow.

### Implementation for User Story 4

- [ ] T029 [P] [US4] Add planning scenario DTOs and domain stubs in `src/FinancialOS.Core/`
- [ ] T030 [US4] Implement persistence and retrieval for planning scenarios in `src/FinancialOS.Data/`
- [ ] T031 [US4] Add minimal planning API endpoints in `src/FinancialOS.Api/Controllers/`
- [ ] T032 [US4] Add integration tests for planning scenario creation in `tests/FinancialOS.Api.Tests/`

**Checkpoint**: User Story 4 should be fully functional and testable independently

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T033 [P] Add documentation updates for the quickstart path and API contract in `specs/001-financialos-platform/`
- [ ] T034 [P] Add shared error handling, logging, and validation middleware in `src/FinancialOS.Api/`
- [ ] T035 [P] Add end-to-end validation of the quickstart workflow in `tests/FinancialOS.Api.Tests/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No dependencies - can start immediately
- **Phase 2**: Depends on Phase 1 completion - blocks all user stories
- **Phase 3+**: All depend on Phase 2 completion
- **Phase 7**: Depends on the implementation of the desired user stories

### User Story Dependencies

- **User Story 1**: Can start after Phase 2; no dependencies on other stories
- **User Story 2**: Can start after Phase 2 and may depend on the outputs of US1 for evidence ingestion
- **User Story 3**: Can start after Phase 2 and uses the shared reference data model
- **User Story 4**: Can start after Phase 2 and uses the existing record foundation

### Parallel Opportunities

- T003 can run in parallel with T001/T002
- T007 and T008 can run in parallel
- T012 and T013 can run in parallel within US1
- T018 and T019 can run in parallel within US2
- T025 can run in parallel with other US3 tasks if the shared DTOs are independent

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2
2. Complete Phase 3 (User Story 1)
3. Stop and validate the evidence upload workflow before moving on

### Incremental Delivery

1. Complete the foundation
2. Deliver User Story 1 for the MVP
3. Add User Story 2 for explainable records
4. Add User Story 3 for API-first clients
5. Add User Story 4 for planning scenarios

# Research: FinancialOS Platform Foundation

## Decision 1: Platform stack

**Decision**: Implement the platform as a modular .NET 8 / ASP.NET Core solution with EF Core for persistence and an API-first architecture.

**Rationale**: The source specification explicitly describes a .NET / ASP.NET Core architecture with a clean separation between domain, data, infrastructure, API, desktop, and shared contracts. .NET 8 is a strong default for long-lived enterprise software and aligns with the stated desire for API-first clients and mixed local/cloud deployment.

**Alternatives considered**:
- Node.js / TypeScript services: rejected because the source plan is explicitly .NET-oriented and the repository direction is platform-focused rather than web-first.
- Pure monolithic desktop application: rejected because the architecture explicitly requires API-first clients and future web/mobile support.

## Decision 2: Persistence strategy

**Decision**: Use EF Core with SQLite for local-first development and PostgreSQL as a drop-in provider for server deployment.

**Rationale**: The implementation plan calls for a local-first SQLite path that can switch to PostgreSQL later without changing the domain model. This supports the project’s stewardship-first design while keeping onboarding simple.

**Alternatives considered**:
- Using PostgreSQL only: rejected because it increases setup friction for local development.
- Using a document database: rejected because the plan requires relational integrity and deterministic relationships between evidence, records, accounts, merchants, and categories.

## Decision 3: Domain modeling approach

**Decision**: Model immutable evidence as a first-class aggregate and separate it from derived knowledge and advisory outputs.

**Rationale**: The constitution explicitly states that facts are immutable and that corrections and classifications accumulate as layers rather than overwriting raw evidence. This requires a strict separation between raw evidence and derived interpretations.

**Alternatives considered**:
- Overwriting imported data in place: rejected because it violates the truth-before-convenience principle.
- Treating AI inferences as source of truth: rejected because the constitution requires explainability and human authority.

## Decision 4: Validation strategy

**Decision**: Use repository-level automated tests for domain rules and API behavior, with sample import and classification scenarios as integration tests.

**Rationale**: The specification calls for explainability, provenance, and deterministic logic; these areas are best validated through integration tests that exercise end-to-end import and record creation flows.

**Alternatives considered**:
- Manual testing only: rejected because the project’s correctness and explainability requirements need repeatable regression coverage.
- AI-based testing only: rejected because deterministic behavioral contracts are necessary for financial accuracy.

## Open questions resolved

- The initial implementation should focus on ingestion, evidence preservation, record creation, and explainable classification rather than full stewardship analytics.
- The first milestone should prioritize the core domain, data access, importer framework, and basic API endpoints.
- The later phases can add rule engines, export providers, desktop client integration, and planning insights after the core foundation exists.

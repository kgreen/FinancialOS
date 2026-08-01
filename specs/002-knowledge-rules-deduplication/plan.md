# Implementation Plan: Knowledge, Rules & Deduplication

**Branch**: `002-knowledge-rules-deduplication` | **Date**: 2026-07-31 | **Spec**: `specs/002-knowledge-rules-deduplication/spec.md`

**Input**: Feature specification from `specs/002-knowledge-rules-deduplication/spec.md`

## Summary

Implement Phase 2 knowledge services on top of the existing .NET 8 foundation: deterministic rules evaluation, merchant normalization with aliasing, duplicate candidate detection with confidence and human override, and append-only provenance/audit records exposed through API-first contracts and contract/integration tests.

## Technical Context

**Language/Version**: C# / .NET 8 / ASP.NET Core minimal APIs

**Primary Dependencies**: ASP.NET Core, EF Core 8, SQLite + Npgsql providers, xUnit + Microsoft.AspNetCore.Mvc.Testing

**Storage**: EF Core relational model (SQLite local-first, PostgreSQL compatible) + existing immutable evidence file storage

**Testing**: xUnit API integration tests in `tests/FinancialOS.Api.Tests` + new deterministic pipeline and contract coverage

**Target Platform**: Local-first API host for desktop-first workflows; cloud/server compatible deployment later

**Project Type**: Modular backend service (core/data/infrastructure/api/shared)

**Performance Goals**: Deterministic pipeline execution for each imported record with stable replay results; duplicate candidate listing/filtering suitable for interactive stewardship review

**Constraints**: Immutable facts/evidence, append-only provenance, deterministic rule ordering, human authority over final duplicate decisions, API-first contracts

**Scale/Scope**: Feature scope is Phase 2 milestone for rule-driven classification, normalization, duplicate review, and audit traceability for imported records

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gate

- Pass — **Truth Before Convenience**: raw evidence and source record facts remain immutable; all new outputs are layered decisions.
- Pass — **Facts Are Immutable**: normalization/rule/duplicate outcomes are modeled as derived artifacts and audit events, not source mutation.
- Pass — **Explainability Is Required**: all machine/system outputs include confidence, reason codes, and provenance chain.
- Pass — **Humans Contain Authority**: duplicate recommendations are advisory until explicit confirm/dismiss action.
- Pass — **Knowledge Before Intelligence**: deterministic rules + explicit normalization + scored duplicate heuristics come before AI.
- Pass — **Modular & API-First**: implementation remains inside Core/Data/Infrastructure/API boundaries with contract-first endpoints.

### Post-Design Re-Check (after Phase 1 artifacts)

- Pass — Data model preserves immutable evidence and append-only provenance timeline.
- Pass — API contracts expose deterministic outcomes, confidence, and user override events.
- Pass — Test strategy validates deterministic replay, explainability payloads, and human override flow end-to-end.

## Project Structure

### Documentation (this feature)

```text
specs/002-knowledge-rules-deduplication/
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
│   ├── Models/
│   └── (new) Knowledge/Rules/Normalization/Deduplication domain services
├── FinancialOS.Data/
│   ├── FinancialOsDbContext.cs
│   ├── EfFinancialRepository.cs
│   └── Migrations/
├── FinancialOS.Infrastructure/
│   └── Import/
├── FinancialOS.Api/
│   ├── Program.cs
│   └── Validation/
└── FinancialOS.Shared/
    └── Contracts/

tests/
└── FinancialOS.Api.Tests/
```

**Structure Decision**: Keep the current multi-project .NET solution layout. Add Phase 2 domain services/entities in `FinancialOS.Core`, persistence/migrations in `FinancialOS.Data`, minimal API endpoints + request/response contracts in `FinancialOS.Api` and `FinancialOS.Shared`, and end-to-end contract/integration tests in `tests/FinancialOS.Api.Tests`.

## Complexity Tracking

No constitution violations or exemptions are required for this plan.

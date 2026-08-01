# Research: Knowledge, Rules & Deduplication

## Decision 1: Deterministic evaluation pipeline

**Decision**: Execute classification in a strict ordered pipeline: normalization pre-pass → active rule filtering → deterministic rule ordering (priority desc, scope specificity desc, created-at asc, id asc) → single outcome assignment + provenance emission.

**Rationale**: This guarantees replayable outcomes (FR-002), keeps behavior explainable, and avoids nondeterministic ties when multiple rules match a record.

**Alternatives considered**:
- First-match by database retrieval order: rejected because DB ordering is not guaranteed and breaks determinism.
- Weighted probabilistic rule ranking: rejected for Phase 2 because constitution requires deterministic knowledge-first behavior.

## Decision 2: Normalization and aliasing model

**Decision**: Separate canonical merchant identity from alias entries. Alias matching uses normalized text tokens and exact/contains strategies, producing a `NormalizationDecision` with confidence and unresolved status when below threshold.

**Rationale**: This supports FR-003/FR-004/FR-006 by preserving raw input while layering interpretable identity resolution.

**Alternatives considered**:
- Overwriting record description with normalized merchant text: rejected because it violates immutable fact principles.
- Single merchant table without alias map: rejected because it cannot reliably represent noisy source variants.

## Decision 3: Duplicate detection workflow with confidence and human override

**Decision**: Produce `DuplicateCandidate` groups from heuristic signals (amount equality, date proximity window, account context, merchant/description similarity). Persist candidate confidence and reasons; final status remains `PendingReview` until user confirms or dismisses.

**Rationale**: Meets FR-007 through FR-009 and constitution principle that humans retain authority over financial truth.

**Alternatives considered**:
- Auto-merging/deleting duplicates above threshold: rejected because it bypasses user authority and risks irreversible mistakes.
- Binary duplicate/no-duplicate without confidence: rejected because explainability requirements demand confidence + reasons.

## Decision 4: Immutable provenance and audit

**Decision**: Use append-only `ProvenanceEntry` records for every pipeline step and user override action, linked to record and optional duplicate candidate; never update historical audit rows.

**Rationale**: Satisfies FR-010/FR-011 and constitution principles Truth Before Convenience + Facts Are Immutable.

**Alternatives considered**:
- Mutable audit row per record: rejected because it loses temporal lineage.
- Storing provenance only in logs: rejected because logs are not a durable domain query surface.

## Decision 5: API-first contracts and test strategy

**Decision**: Expose Phase 2 capabilities through explicit REST contracts (`/rules`, `/normalization/*`, `/duplicates/*`, `/provenance/*`) and validate with contract + integration tests in `FinancialOS.Api.Tests`.

**Rationale**: Aligns with modular/API-first constitution and existing minimal API + DTO approach in `FinancialOS.Shared.Contracts`.

**Alternatives considered**:
- Implement services first with no external contract updates: rejected because desktop/web/mobile consumers rely on stable API boundaries.
- UI-driven behavior validation only: rejected because repeatable automated verification is required for financial correctness.

## Clarification Resolution Status

All Technical Context clarifications for this feature are resolved in the decisions above. No remaining `NEEDS CLARIFICATION` items.

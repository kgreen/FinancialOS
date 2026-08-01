# Feature Specification: Phase 2 Knowledge, Rules & Deduplication

**Feature Branch**: `002-knowledge-rules-deduplication`

**Created**: 2026-07-31

**Status**: Draft

**Input**: User description: "Next roadmap step after foundation work, focused on Phase 2 capabilities from FinancialOSProjectDocuments/FinancialOS.md: rules engine, normalization engine, duplicate detector, and audit/provenance."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Apply deterministic classification rules (Priority: P1)

A stewardship user can define and manage deterministic matching rules so recurring financial activity is classified consistently without manual rework.

**Why this priority**: This delivers the constitution's "knowledge before intelligence" principle and creates reliable structure for all later analysis.

**Independent Test**: Create a rule for a known transaction pattern, run processing on a sample import, and verify matching records are assigned the expected merchant/category with explainable evidence.

**Acceptance Scenarios**:

1. **Given** a user has created an active classification rule, **When** new matching records are processed, **Then** the system applies the rule automatically and records why it matched.
2. **Given** multiple rules could match the same record, **When** processing occurs, **Then** the system resolves the outcome deterministically using a documented priority order and records that decision path.
3. **Given** a user updates or disables a rule, **When** future records are processed, **Then** only the active rule set is applied and the change is reflected in provenance.

---

### User Story 2 - Normalize merchant identities and categories (Priority: P1)

A stewardship user can rely on normalized merchant naming and alias resolution so spending patterns are understandable even when source text is inconsistent.

**Why this priority**: Phase 2 requires reliable identity resolution before insights are trustworthy; this also protects "truth before convenience" by preserving raw text while layering interpretation.

**Independent Test**: Import records containing multiple merchant string variants, run normalization, and verify they map to the intended canonical merchant and category while preserving raw evidence.

**Acceptance Scenarios**:

1. **Given** a record contains noisy or variant merchant text, **When** normalization is applied, **Then** the record is linked to a canonical merchant identity without altering raw source text.
2. **Given** a known alias is mapped to a canonical merchant, **When** matching records are processed, **Then** those records resolve to the same canonical merchant identity.
3. **Given** no confident normalization match exists, **When** processing completes, **Then** the system leaves the record unconfirmed and flags it for human review.

---

### User Story 3 - Detect duplicates with full audit trail (Priority: P1)

A stewardship user can detect likely duplicate records across imports and review a complete provenance trail before confirming any final action.

**Why this priority**: Duplicate control is essential for trustworthy totals, and full provenance ensures explainability and human authority over final truth.

**Independent Test**: Process two overlapping imports containing repeated transactions, review duplicate flags, and confirm each flagged decision includes confidence, source links, and processing lineage.

**Acceptance Scenarios**:

1. **Given** two records share duplicate-signature characteristics, **When** duplicate detection runs, **Then** the system flags them as potential duplicates with a confidence score and reason summary.
2. **Given** a record is flagged as a potential duplicate, **When** the user reviews it, **Then** they can inspect all contributing evidence and decide to confirm or dismiss the duplicate state.
3. **Given** a duplicate decision is confirmed or dismissed by a user, **When** the decision is saved, **Then** the system preserves an immutable audit entry of the action, actor, and timestamp.

---

### Edge Cases

- Conflicting rule matches with equal priority for the same record.
- Merchant text that maps to multiple plausible canonical identities with low certainty.
- Duplicate candidates that share amount and date but come from different accounts.
- Re-import of the same source evidence after rule changes.
- User reversal of a previously confirmed duplicate decision.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow users to create, activate, deactivate, and prioritize deterministic matching rules for merchant and category assignment.
- **FR-002**: The system MUST evaluate rules in a deterministic order and produce the same classification outcome for the same input and active rule set.
- **FR-003**: The system MUST preserve raw evidence fields unchanged while storing normalized interpretations as separate layered data.
- **FR-004**: The system MUST normalize merchant names using canonical identities and alias mappings.
- **FR-005**: The system MUST allow category assignment based on deterministic rule outcomes and normalized identities.
- **FR-006**: The system MUST flag records as unresolved when normalization confidence is insufficient for automatic assignment.
- **FR-007**: The system MUST detect potential duplicate records using date, amount, account context, and textual similarity indicators.
- **FR-008**: The system MUST record duplicate detection outcomes with confidence scores and explicit reason codes.
- **FR-009**: The system MUST provide users a review workflow to confirm or dismiss potential duplicates before final truth is established.
- **FR-010**: The system MUST capture provenance for every rule, normalization, and duplicate decision, including source evidence references, execution sequence, confidence, and decision actor.
- **FR-011**: The system MUST keep all provenance and decision history immutable and append-only.
- **FR-012**: The system MUST provide human override authority for any system-generated classification or duplicate recommendation while retaining full audit history.

### Key Entities *(include if feature involves data)*

- **ClassificationRule**: User-defined deterministic condition set with priority, scope, status, and expected merchant/category outcomes.
- **MerchantAliasMap**: Mapping between raw merchant variants and canonical merchant identity.
- **CanonicalMerchant**: Standardized merchant identity used for consistent reporting across variant source text.
- **NormalizationDecision**: Record-level interpretation outcome containing canonical identity, category recommendation, confidence, and rationale.
- **DuplicateCandidate**: Pair or group of records evaluated as potentially duplicate with confidence and reason metadata.
- **ProvenanceEntry**: Immutable audit object that records processing step, decision source, confidence, actor, and timestamp.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 95% of recurring transactions matching active rules are auto-classified consistently with no user correction required in validation samples.
- **SC-002**: At least 90% of known merchant text variants in validation samples resolve to the intended canonical merchant identity.
- **SC-003**: Duplicate detection identifies at least 95% of true duplicate cases in validation samples while keeping false-positive flags at or below 5%.
- **SC-004**: 100% of system-generated and user-confirmed decisions in this feature expose confidence, provenance, and rationale for user inspection.
- **SC-005**: 100% of user overrides and duplicate confirmations/dismissals are captured as immutable audit events.

## Assumptions

- Phase 1 evidence ingestion and canonical financial record creation are available and stable for this phase.
- Users performing review actions have authority to confirm or dismiss system recommendations.
- Initial rollout targets deterministic classification quality and explainability, not autonomous AI-only decisioning.
- Duplicate detection is advisory by default and does not permanently merge or delete records without explicit human confirmation.

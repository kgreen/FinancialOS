# Quickstart: Knowledge, Rules & Deduplication Validation

This guide validates Phase 2 behavior end-to-end against the contracts in `contracts/api.md` and entities in `data-model.md`.

## Prerequisites

- .NET 8 SDK
- Existing solution restored (`FinancialOS.sln`)
- API test project available (`tests/FinancialOS.Api.Tests`)
- Sample overlapping import files (or API-generated seed records)

## Setup

1. Restore/build:
   - `dotnet restore FinancialOS.sln`
   - `dotnet build FinancialOS.sln`
2. Start API host:
   - `dotnet run --project src/FinancialOS.Api/FinancialOS.Api.csproj`

## Validation Scenarios

### 1) Deterministic rules pipeline

1. Create at least two active rules with distinct priorities.
2. Execute normalization/classification for a target record via `/api/v1/records/{id}/normalize`.
3. Re-run with unchanged input/rule set.
4. Verify same merchant/category/rule/confidence outcome each run.
5. Verify provenance timeline records deterministic ordering reason codes.

### 2) Normalization + alias resolution

1. Create canonical merchant and alias mappings.
2. Normalize records containing merchant text variants.
3. Verify variants resolve to the same canonical merchant.
4. Verify low-confidence/no-match records return `Unresolved` and remain reviewable.

### 3) Duplicate candidate workflow

1. Import or seed overlapping records.
2. Run `/api/v1/duplicates/evaluate`.
3. Verify candidates include confidence + reason signals.
4. Confirm one candidate and dismiss another.
5. Verify status transitions and provenance entries for both actions.

### 4) Immutable provenance audit

1. Query `/api/v1/records/{id}/provenance` before and after each action.
2. Verify event count only increases (append-only behavior).
3. Verify user override events include actor identity and timestamp.
4. Verify the response includes `recordId` and an `events` array with lower-case `source` values.

## Automated Test Runs

- `dotnet test FinancialOS.sln --filter "FullyQualifiedName~FinancialOS.Api.Tests"`
- `dotnet test tests/FinancialOS.Api.Tests/FinancialOS.Api.Tests.csproj --filter "FullyQualifiedName~Provenance || FullyQualifiedName~Explainability"`

Expected coverage from this feature:
- Deterministic rule evaluation replay
- Alias normalization success/failure paths
- Duplicate candidate confidence + review endpoints
- Provenance immutability and explainability payload checks

## Expected Outcomes

- Deterministic classification outputs for unchanged inputs.
- Canonical merchant normalization via alias map without altering raw facts.
- Duplicate recommendations remain advisory until human action.
- 100% of system and human decisions expose confidence + provenance trail.

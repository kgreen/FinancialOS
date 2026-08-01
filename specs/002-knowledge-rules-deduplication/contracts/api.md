# API Contract: Knowledge, Rules & Deduplication

Base route prefix: `/api/v1`

## Rules Management

> **Note**: Implemented at `/api/v1/classification-rules` instead of `/rules` to avoid
> colliding with the pre-existing legacy `GET /api/v1/rules` reference-data endpoint
> (`ReferenceEndpointsContractTests`), which returns simple `Rule` reference items and
> predates this feature.

### POST `/classification-rules`
Create deterministic classification rule.

```json
{
  "name": "Whole Foods Grocery Rule",
  "priority": 900,
  "scope": "Account",
  "scopeReferenceId": "11111111-1111-1111-1111-111111111111",
  "condition": {
    "merchantContains": "whole foods",
    "amountMin": 0
  },
  "targetMerchantId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "targetCategoryId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "effectiveFromUtc": "2026-08-01T00:00:00Z"
}
```

### GET `/classification-rules`
List rules with deterministic ordering metadata (`priority`, `createdAtUtc`, `id`).

### PATCH `/classification-rules/{id}`
Activate/deactivate or reprioritize rule without deleting historical provenance.

## Normalization & Aliasing

### POST `/normalization/aliases`
Create alias mapping to canonical merchant.

```json
{
  "canonicalMerchantId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "aliasRawText": "WFM #1023",
  "matchStrategy": "Contains",
  "confidenceWeight": 0.92
}
```

### POST `/records/{id}/normalize`
Run deterministic normalization + rule classification for one record.

**Response**
```json
{
  "recordId": "guid",
  "status": "Resolved",
  "canonicalMerchantId": "guid",
  "categoryId": "guid",
  "ruleId": "guid",
  "confidence": 0.94,
  "reasonCodes": ["alias-match", "rule-priority-win"],
  "provenanceCorrelationId": "guid"
}
```

## Duplicate Detection & Review

### POST `/duplicates/evaluate`
Trigger duplicate candidate generation for a record set/import batch.

### GET `/duplicates/candidates?status=PendingReview&minConfidence=0.70`
List candidate pairs/groups with confidence and reason signals.

### POST `/duplicates/candidates/{id}/confirm`
Human confirms duplicate recommendation.

### POST `/duplicates/candidates/{id}/dismiss`
Human dismisses duplicate recommendation.

Both confirm/dismiss endpoints must:
- require actor identity
- return updated candidate status
- emit immutable provenance entry

## Provenance & Audit

### GET `/records/{id}/provenance`
Return append-only audit timeline for normalization/rule/duplicate steps and overrides.

**Response**
```json
{
  "recordId": "guid",
  "events": [
    {
      "id": "guid",
      "stepType": "RuleEvaluation",
      "stepSequence": 2,
      "source": "system",
      "sourceReference": "rule:cccccccc-cccc-cccc-cccc-cccccccccccc",
      "confidence": 0.94,
      "decisionSummary": "Rule Whole Foods Grocery Rule selected",
      "reasonCodes": ["priority", "condition-match"],
      "actorId": null,
      "createdAtUtc": "2026-08-01T10:00:00Z"
    }
  ]
}
```

## Contract & Test Strategy Requirements

- Contract tests validate JSON shapes, required fields, and enum/domain values.
- Integration tests validate deterministic replay (same input + active rules => same output).
- Integration tests validate duplicate candidate review lifecycle and immutable provenance growth after each action.
- Negative tests validate unresolved normalization and low-confidence duplicate handling.

# Data Model: FinancialOS Platform Foundation

## Core Entities

### FinancialEvidence

Represents the immutable source artifact associated with a financial import.

**Fields**
- Id: unique identifier
- SourceType: enum such as Csv, Ofx, Pdf, Image
- OriginalFileName: string
- StoragePath: string
- Sha256Hash: string
- ContentType: string
- UploadedAt: datetime
- SourceMetadata: JSON or structured metadata
- CreatedBy: optional user identifier

**Rules**
- Must be immutable once stored.
- Must preserve a checksum and traceable source metadata.
- Must be linked to one or more derived financial records.

### FinancialEvent

Represents a real-world financial occurrence captured from evidence.

**Fields**
- Id: unique identifier
- Date: date/time
- Amount: Money
- Description: string
- AccountId: reference to Account
- Counterparty: optional string
- SourceEvidenceId: reference to FinancialEvidence

**Rules**
- Must include an amount, date, and originating evidence.
- May be represented as an event before a canonical financial record is created.

### FinancialRecord

Represents the canonical record used by the application layer.

**Fields**
- Id: unique identifier
- EventId: optional reference to FinancialEvent
- AccountId: reference to Account
- MerchantId: optional reference to Merchant
- CategoryId: optional reference to Category
- Amount: Money
- OccurredOn: date/time
- Status: pending, normalized, reviewed, ignored
- ClassificationConfidence: decimal
- ProvenanceId: reference to Provenance

**Rules**
- Must be created from evidence-derived data rather than directly from user edits.
- Must support explicit review status transitions.

### Account

Represents a financial container such as a bank account or cash account.

**Fields**
- Id: unique identifier
- Name: string
- InstitutionId: reference to Institution
- Currency: string
- AccountNumberMasked: optional string
- ExternalReference: optional string

### Institution

Represents a financial provider or organization.

**Fields**
- Id: unique identifier
- Name: string
- Type: enum such as Bank, CreditCard, Wallet, Other

### Merchant

Represents a normalized transaction counterparty.

**Fields**
- Id: unique identifier
- Name: string
- NormalizedName: string
- CategoryId: optional reference to Category

### Category

Represents a user or system-defined grouping for financial records.

**Fields**
- Id: unique identifier
- Name: string
- ParentCategoryId: optional self-reference
- IsSystemDefault: bool

### Rule

Represents deterministic matching logic for classification.

**Fields**
- Id: unique identifier
- Name: string
- MatchExpression: string
- TargetCategoryId: reference to Category
- TargetMerchantId: optional reference to Merchant
- Priority: int

### Value Objects

- Money: amount + currency, preserving precision and avoiding floating-point storage.
- Confidence: decimal score between 0.00 and 1.00.
- Provenance: traceability data including source, import job, rules executed, and algorithm version.

## Relationships

- A FinancialEvidence may generate many FinancialEvents and FinancialRecords.
- A FinancialRecord belongs to one Account and may reference one Merchant and one Category.
- A Merchant belongs to one Category by default but may be reclassified later.
- A Rule may target one Category and optionally one Merchant.
- An Account belongs to one Institution.

## State Transitions

FinancialRecord lifecycle:
1. Pending: created from parsed evidence.
2. Normalized: linked to merchant/category and confidence assigned.
3. Reviewed: user or system accepted the assignment.
4. Ignored: explicitly excluded from a future view.

# Data Model: Transaction Parsing & Record Hydration

**Feature**: 003 — Transaction Parsing & Record Hydration
**Date**: 2026-08-01
**Status**: Complete

---

## Overview

This feature introduces two new persisted entities (`InstitutionProfile`, `ImportJob`), a transient value type (`ParsedTransaction`), two new enums (`AmountLayout`, `ImportJobStatus`, `ParserType`, `ClassificationStatus`), and five nullable columns on the existing `FinancialRecord` table.

---

## New Enum: `AmountLayout`

```csharp
// src/FinancialOS.Core/Models/ImportEntities.cs
public enum AmountLayout
{
    SingleSigned,       // One column; negative = debit
    SplitDebitCredit    // Two columns: debit (positive, stored negative) + credit (positive, stored positive)
}
```

## New Enum: `ImportJobStatus`

```csharp
public enum ImportJobStatus
{
    Pending,        // Created, not yet started
    Processing,     // Parsing in progress
    Completed,      // All rows parsed successfully
    PartialSuccess, // At least one row parsed; at least one row failed
    Failed          // Zero rows parsed; all failed or file-level error
}
```

## New Enum: `ParserType`

```csharp
public enum ParserType
{
    CsvConfigured,      // CSV parsed using a named InstitutionProfile
    CsvAutoDetected,    // CSV parsed using auto-detected layout
    Ofx                 // OFX 1.x (SGML) or OFX 2.x (XML); QFX treated identically
}
```

## New Enum: `ClassificationStatus`

```csharp
// Added to DomainEntities.cs
public enum ClassificationStatus
{
    Pending,        // No rule matched; no classification applied
    Classified      // Rule engine matched; confidence score + reason code populated
}
```

---

## Transient DTO: `ParsedTransaction`

**Not persisted.** Produced by `ITransactionParser`; consumed by `ImportOrchestrationService`.

```csharp
// src/FinancialOS.Core/Models/ImportEntities.cs
public sealed record ParsedTransaction(
    DateOnly TransactionDate,
    decimal Amount,               // Signed: negative = debit, positive = credit
    string Description,
    decimal? Balance,             // Optional running balance
    string? ExternalReferenceId,  // FITID (OFX) or mapped CSV reference column
    int RowIndex,                 // 0-based; element index for OFX, row number for CSV
    string RawRow                 // Verbatim source line (for provenance)
);
```

---

## New Entity: `InstitutionProfile`

### Purpose

Describes how to parse a CSV from a specific bank: which headers map to standard fields, the amount layout, and the date format.

### Class definition

```csharp
// src/FinancialOS.Core/Models/ImportEntities.cs
public sealed class InstitutionProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;   // e.g. "Chase Checking CSV" — unique

    // Standard field key names: "date", "amount", "description", "balance", "reference"
    // Values are the actual CSV header strings as they appear in the file
    public Dictionary<string, string> ColumnMappings { get; set; } = new();

    public AmountLayout AmountLayout { get; set; } = AmountLayout.SingleSigned;

    // Only populated when AmountLayout = SplitDebitCredit
    public string? DebitColumnName { get; set; }
    public string? CreditColumnName { get; set; }

    // Date format pattern (e.g. "MM/dd/yyyy"). Null = try common formats in order.
    public string? DateFormatPattern { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Soft-delete: never hard-deleted if referenced by an ImportJob
    public bool IsDeleted { get; set; } = false;
}
```

### EF Core mapping (`FinancialOsDbContext.cs`)

```csharp
modelBuilder.Entity<InstitutionProfile>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Name).IsRequired();
    entity.Property(e => e.AmountLayout).HasConversion<string>();

    entity.Property(e => e.ColumnMappings)
        .HasConversion(
            dict => JsonSerializer.Serialize(dict, JsonSerializerOptions.Default),
            json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonSerializerOptions.Default) ?? new())
        .Metadata.SetValueComparer(/* DictionaryComparer */);

    entity.HasIndex(e => e.Name)
        .IsUnique()
        .HasDatabaseName("IX_InstitutionProfile_Name_Unique");

    entity.HasQueryFilter(e => !e.IsDeleted);  // default filter excludes deleted profiles
});
```

### Validation rules

- `Name`: required, max 200 chars, unique.
- `ColumnMappings`: must contain at least the `"date"` and `"amount"` keys when `AmountLayout = SingleSigned`.
- When `AmountLayout = SplitDebitCredit`: `DebitColumnName` and `CreditColumnName` must both be non-null.
- `DateFormatPattern`: if provided, must be a valid .NET date format string (validated via `DateTime.TryParseExact`).

---

## New Entity: `ImportJob`

### Purpose

Tracks one parsing execution — from evidence upload through to record hydration. Persists per-row failure details for post-hoc auditing.

### Supporting value type

```csharp
// Serialized as JSON within ImportJob.FailedRows
public sealed record FailedRowEntry(int RowIndex, string Reason);
```

### Class definition

```csharp
// src/FinancialOS.Core/Models/ImportEntities.cs
public sealed class ImportJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EvidenceId { get; set; }                        // FK → FinancialEvidence
    public Guid? InstitutionProfileId { get; set; }             // FK → InstitutionProfile (null for OFX / auto-detected)
    public ParserType ParserType { get; set; }
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Pending;
    public int TotalRows { get; set; }
    public int ParsedCount { get; set; }
    public int FailedRowCount { get; set; }
    public List<FailedRowEntry> FailedRows { get; set; } = new();
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

### EF Core mapping

```csharp
modelBuilder.Entity<ImportJob>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.ParserType).HasConversion<string>();
    entity.Property(e => e.Status).HasConversion<string>();

    entity.Property(e => e.FailedRows)
        .HasConversion(
            list => JsonSerializer.Serialize(list, JsonSerializerOptions.Default),
            json => JsonSerializer.Deserialize<List<FailedRowEntry>>(json, JsonSerializerOptions.Default) ?? new())
        .Metadata.SetValueComparer(/* FailedRowListComparer */);

    entity.HasIndex(e => e.EvidenceId)
        .HasDatabaseName("IX_ImportJob_EvidenceId");

    entity.HasIndex(e => e.Status)
        .HasDatabaseName("IX_ImportJob_Status");
});
```

### Status transitions

```
Pending → Processing → Completed
                     ↘ PartialSuccess
                     ↘ Failed
```

| Condition | Final status |
|-----------|-------------|
| All rows parsed successfully | `Completed` |
| Some rows succeeded, some failed | `PartialSuccess` |
| Zero rows parsed (all failed or file-level error) | `Failed` |
| Empty file (0 data rows, no errors) | `Completed` |

---

## Extended Entity: `FinancialRecord`

### New columns (all nullable — safe for existing spec 001/002 rows)

| Column | Type | Description |
|--------|------|-------------|
| `ImportJobId` | `Guid?` | FK → `ImportJob`; null for manually created records |
| `ExternalReferenceId` | `string?` | OFX `FITID` or CSV reference column value; used for cross-import duplicate detection |
| `RowIndex` | `int?` | 0-based source row number for traceability in error reports |
| `ClassificationStatus` | `ClassificationStatus?` | `Pending` or `Classified`; null for legacy records (serialized as `"pending"`) |
| `ClassificationReasonCode` | `string?` | First reason code from `RuleEvaluationResult.ReasonCodes` |

### Updated EF mapping additions

```csharp
// Added inside ConfigureFinancialRecord(modelBuilder):
entity.Property(e => e.ClassificationStatus)
    .HasConversion<string>()
    .HasColumnName("ClassificationStatus");

entity.HasIndex(e => e.ImportJobId)
    .HasDatabaseName("IX_FinancialRecord_ImportJobId");

entity.HasIndex(e => e.ExternalReferenceId)
    .HasDatabaseName("IX_FinancialRecord_ExternalReferenceId");
```

### Field reuse: `ClassificationConfidence`

`FinancialRecord.ClassificationConfidence` (existing owned type, `Confidence` record) is reused for the rule engine confidence score. No changes needed.

### Field reuse: `EvidenceId`

`FinancialRecord.EvidenceId` (existing nullable FK) serves as `SourceEvidenceId` for provenance. The spec's concept of `SourceEvidenceId` maps directly to the existing `EvidenceId` field — no column rename required.

---

## EF Migration: `AddTransactionParsing`

**File**: `src/FinancialOS.Data/Migrations/[timestamp]_AddTransactionParsing.cs`

### Up operations (in order)

1. **Create `InstitutionProfiles` table** with all columns defined above.
2. **Create `ImportJobs` table** with all columns defined above, FK to `FinancialEvidence`.
3. **Add columns to `FinancialRecords`**:
   - `ImportJobId GUID NULL`
   - `ExternalReferenceId NVARCHAR(255) NULL`
   - `RowIndex INT NULL`
   - `ClassificationStatus NVARCHAR(20) NULL`
   - `ClassificationReasonCode NVARCHAR(255) NULL`
4. **Add FK**: `ImportJobs.EvidenceId → FinancialEvidence.Id` (cascade delete: leave records; orphan ImportJob is acceptable).
5. **Add FK**: `FinancialRecords.ImportJobId → ImportJobs.Id` (set null on delete, not cascade — deleting a job should not delete records).
6. **Add indexes**:
   - `IX_InstitutionProfile_Name_Unique`
   - `IX_ImportJob_EvidenceId`
   - `IX_ImportJob_Status`
   - `IX_FinancialRecord_ImportJobId`
   - `IX_FinancialRecord_ExternalReferenceId`

### Down operations

Reverse of the above in reverse order.

---

## Entity Relationship Summary

```
FinancialEvidence (1) ──────────────── (0..1) ImportJob
                                              │
                                              │ ImportJobId (nullable FK)
                                              ▼
                                       FinancialRecord (many)
                                              │
                                              │ EvidenceId (nullable FK)
                                              ▼
                                       FinancialEvidence

InstitutionProfile (0..1) ──────── (0..N) ImportJob
```

---

## Indexes & Query Patterns

| Index | Table | Purpose |
|-------|-------|---------|
| `IX_FinancialEvidence_Sha256Hash_Unique` | `FinancialEvidence` | Duplicate upload detection (already exists from spec 001) |
| `IX_ImportJob_EvidenceId` | `ImportJob` | Look up job by evidence after duplicate SHA256 detection |
| `IX_ImportJob_Status` | `ImportJob` | Admin/monitoring queries by status |
| `IX_FinancialRecord_ImportJobId` | `FinancialRecord` | List records created by a given import |
| `IX_FinancialRecord_ExternalReferenceId` | `FinancialRecord` | Cross-import OFX `FITID` duplicate detection (FR-020) |
| `IX_InstitutionProfile_Name_Unique` | `InstitutionProfile` | Enforce name uniqueness |

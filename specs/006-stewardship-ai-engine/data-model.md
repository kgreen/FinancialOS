# Data Model: Stewardship & AI Engine (Feature 006)

**Feature**: `006-stewardship-ai-engine`
**Date**: 2026-09-02

---

## Overview

Feature 006 introduces lightweight stewardship planning over the existing financial record model. The core domain remains centered on immutable financial facts from imported transactions, while goals, budgets, and derived insight/recommendation types add a planning and advisory layer.

## Persistent Entities

### `Goal`

```csharp
// src/FinancialOS.Core/Models/Goal.cs
namespace FinancialOS.Core.Models;

public sealed class Goal
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public GoalType Type { get; set; }
    public decimal TargetAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public GoalPeriod Period { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

**Validation rules**:
- `Name` must be non-empty and trimmed.
- `TargetAmount` must be greater than zero.
- `EndDate >= StartDate`.
- `Period` must be one of the supported values (`Monthly`, `Weekly`, `Custom`).
- `AccountId` and `CategoryId` are optional but must not conflict when both are supplied.

### `Budget`

```csharp
// src/FinancialOS.Core/Models/Budget.cs
namespace FinancialOS.Core.Models;

public sealed class Budget
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public BudgetPeriod Period { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

**Validation rules**:
- `Name` must be non-empty and trimmed.
- `Amount` must be greater than zero.
- `EndDate >= StartDate`.
- `AccountId` and `CategoryId` are optional and should be interpreted as a single optional scope filter.

## Derived Request/Response Types

### `InsightRequest`

```csharp
// src/FinancialOS.Core/Models/InsightRequest.cs
namespace FinancialOS.Core.Models;

public sealed record InsightRequest
{
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public Guid? AccountId { get; init; }
    public Guid? CategoryId { get; init; }
    public IReadOnlyList<Guid> GoalIds { get; init; } = [];
    public IReadOnlyList<Guid> BudgetIds { get; init; } = [];
}
```

**Validation rules**:
- `EndDate >= StartDate`.
- The request may be empty of goal/budget IDs; in that case it returns a general insight snapshot for the selected range.

### `StewardshipInsight`

```csharp
// src/FinancialOS.Core/Models/StewardshipInsight.cs
namespace FinancialOS.Core.Models;

public sealed record StewardshipInsight
{
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required decimal TotalSpending { get; init; }
    public required decimal TotalIncome { get; init; }
    public required decimal NetFlow { get; init; }
    public required string TrendDirection { get; init; }
    public required decimal CategoryConcentration { get; init; }
    public required IReadOnlyList<GoalProgressSnapshot> GoalProgress { get; init; }
    public required IReadOnlyList<BudgetProgressSnapshot> BudgetProgress { get; init; }
    public required IReadOnlyList<EvidenceReference> Evidence { get; init; }
    public required string AlignmentStatus { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
}
```

### `GoalProgressSnapshot`

```csharp
// src/FinancialOS.Core/Models/GoalProgressSnapshot.cs
namespace FinancialOS.Core.Models;

public sealed record GoalProgressSnapshot
{
    public required Guid GoalId { get; init; }
    public required string GoalName { get; init; }
    public required decimal CurrentAmount { get; init; }
    public required decimal TargetAmount { get; init; }
    public required decimal PercentComplete { get; init; }
    public required string Status { get; init; }
}
```

### `BudgetProgressSnapshot`

```csharp
// src/FinancialOS.Core/Models/BudgetProgressSnapshot.cs
namespace FinancialOS.Core.Models;

public sealed record BudgetProgressSnapshot
{
    public required Guid BudgetId { get; init; }
    public required string BudgetName { get; init; }
    public required decimal SpentAmount { get; init; }
    public required decimal BudgetAmount { get; init; }
    public required decimal PercentUsed { get; init; }
    public required string Status { get; init; }
}
```

### `RecommendationRequest`

```csharp
// src/FinancialOS.Core/Models/RecommendationRequest.cs
namespace FinancialOS.Core.Models;

public sealed record RecommendationRequest
{
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public Guid? AccountId { get; init; }
    public Guid? CategoryId { get; init; }
    public IReadOnlyList<Guid> GoalIds { get; init; } = [];
    public IReadOnlyList<Guid> BudgetIds { get; init; } = [];
}
```

### `AdvisorRecommendation`

```csharp
// src/FinancialOS.Core/Models/AdvisorRecommendation.cs
namespace FinancialOS.Core.Models;

public sealed record AdvisorRecommendation
{
    public required string Summary { get; init; }
    public required string Rationale { get; init; }
    public required IReadOnlyList<EvidenceReference> Evidence { get; init; }
    public required decimal Confidence { get; init; }
    public required string SuggestedAction { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
}
```

### `EvidenceReference`

```csharp
// src/FinancialOS.Core/Models/EvidenceReference.cs
namespace FinancialOS.Core.Models;

public sealed record EvidenceReference
{
    public required string Type { get; init; }
    public required string Label { get; init; }
    public required string Detail { get; init; }
}
```

## Relationships and Notes

- `Goal` and `Budget` are persistent planning entities stored via EF Core.
- `StewardshipInsight` and `AdvisorRecommendation` are derived outputs created from imported financial records plus the selected planning entities.
- The insight and advisor services should never mutate the original financial record facts; they only create interpretation layers over them.
- The API should expose these types through typed DTOs with camelCase JSON naming and stringified enums for browser compatibility.

## Enum Definitions

```csharp
// src/FinancialOS.Core/Models/GoalType.cs
public enum GoalType
{
    Savings = 0,
    SpendingCap = 1,
    CategoryLimit = 2
}

// src/FinancialOS.Core/Models/GoalPeriod.cs
public enum GoalPeriod
{
    Monthly = 0,
    Weekly = 1,
    Custom = 2
}

// src/FinancialOS.Core/Models/BudgetPeriod.cs
public enum BudgetPeriod
{
    Monthly = 0,
    Weekly = 1,
    Custom = 2
}
```

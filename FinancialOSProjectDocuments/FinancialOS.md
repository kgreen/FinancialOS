# FinancialOS Platform Constitution & Technical Implementation Specification

---

## Part I: Platform Constitution

### Article I — Core Vision & Philosophy

FinancialOS is built to transform raw financial evidence into actionable understanding. Unlike traditional accounting or budgeting software that focuses on historical bookkeeping, FinancialOS exists to explain behavior, uncover patterns, and empower users to become better stewards of their resources.

* **The Core Question:** Traditional software asks, *"Where did my money go?"* FinancialOS asks, ***"What is my money helping me become?"***

* **Definition of Stewardship:** Managing resources intentionally so daily actions align with long-term priorities and values. Wisdom takes precedence over net worth.


* **Role of AI:** Artificial Intelligence is designed to advise, explain, and recommend—never to act as an unexplainable black box or silently modify financial truth.



---

### Article II — Guiding Architectural Principles

| Principle | Architectural Mandate |
| --- | --- |
| **1. Truth Before Convenience** | Evidence is preserved permanently. Corrections and classifications form layers on top of raw evidence rather than overwriting it.

 |
| **2. Facts Are Immutable** | Observable reality (amounts, dates, raw text, source files) never changes. Only our interpretation evolves.

 |
| **3. Explainability Is Required** | Every categorization, insight, and recommendation must explicitly state its confidence score, provenance, and underlying rules.

 |
| **4. Humans Contain Authority** | System decisions are advisory. The user retains absolute control over financial truth and decisions.

 |
| **5. Knowledge Before Intelligence** | Core correctness relies on deterministic logic and explicit rules; AI handles interpretation, pattern detection, and natural language tasks.

 |
| **6. Modular & API-First** | The ASP.NET Core Engine owns the domain truth. UI applications (Desktop, Web, Mobile) and integrations act purely as consumers.

 |

---

### Article III — The Stewardship Journey

Features and data structures in FinancialOS must directly support one or more stages of the Stewardship Lifecycle:

$$\text{Awareness} \longrightarrow \text{Understanding} \longrightarrow \text{Alignment} \longrightarrow \text{Planning} \longrightarrow \text{Growth} \longrightarrow \text{Legacy}$$

1. **Awareness:** Capturing immutable evidence to establish complete financial clarity.


2. **Understanding:** Deriving relationships, classifications, and behaviors from facts.


3. **Alignment:** Comparing financial activity against stated personal values and priorities.


4. **Planning:** Modeling future scenarios, budgets, and goal paths intentionally.


5. **Growth:** Evaluating longitudinal behavioral trends and habit improvements.


6. **Legacy:** Structuring long-term impact, charitable strategies, and generational wisdom.



---

## Part II: Domain & Knowledge Architecture

### The Core Knowledge Pipeline

FinancialOS separates raw input from derived knowledge through a strict directional continuum:

```text
  [ Financial Evidence ]   (Source documents, raw files, OCR data)
            │
            ▼
   [ Financial Event ]     (Real-world occurrence in space/time)
            │
            ▼
  [ Financial Record ]     (System's canonical anchor)
            │
            ▼
 [ Knowledge Discovery ]   (Identity, Classification, Relationships)
            │
            ▼
   [ Guidance Engine ]     (Observations, Patterns, Insights, Recommendations)
            │
            ▼
  [ Stewardship Growth ]   (User Decisions, Action Outcomes, Long-term Habit Alignment)
```[cite: 1, 2]

---

### Domain Aggregates & Boundaries

#### Core Domain Aggregates
* **`FinancialEvidence`:** Immutable blob/document state with source tracking, hashes, and acquisition metadata[cite: 1, 2].
* **`FinancialEvent`:** Real-world transaction definition involving actors, timestamp, and exchange of values[cite: 2].
* **`FinancialRecord`:** Canonical entity serving as an anchor in the graph; links evidence, accounts, merchants, and categories[cite: 2].
* **`Account` & `Institution`:** Financial containers holding resources, statement balances, and credential bindings[cite: 1, 3, 4].
* **`Merchant` & `Category`:** Normalization entities that isolate transaction aliases and hierarchical classifications[cite: 3].
* **`Rule`:** Deterministic matching criteria evaluated against ingested evidence[cite: 3].

#### Value Objects
* **`Money`:** Encapsulates precise decimal amounts and currency codes.
* **`Confidence`:** Decimal score ($0.00 \text{ to } 1.00$) representing system certainty for classification or AI inferences.
* **`Provenance`:** Traceability metadata (Source, ImportJobId, RulesExecuted, AlgorithmVersion)[cite: 1].
* **`DateRange` / `ResourceAmount`:** Immutable temporal and quantity descriptors[cite: 2, 4].

---

## Part III: Technical Implementation Plan

### Solution Architecture & Layering (.NET / ASP.NET Core)

The system is structured as a clean, decoupled architecture:

```text
FinanceOS.sln
│
├── src/
│   ├── FinancialOS.Core/           (Domain Models, Aggregates, Interfaces, Invariants)
│   ├── FinancialOS.Data/           (EF Core, Repositories, Database Migrations)
│   ├── FinancialOS.Infrastructure/ (Parsers, OCR, AI Connectors, Exporters)
│   ├── FinancialOS.Api/            (ASP.NET Core Controllers, Endpoints, Auth)
│   ├── FinancialOS.Desktop/        (WPF / UI Shell communicating via API)
│   └── FinancialOS.Shared/         (Contracts, DTOs, Enums)
│
└── tests/
    ├── FinancialOS.Core.Tests/
    └── FinancialOS.Api.Tests/
```[cite: 3]

---

### Data Pipeline & Database Design Strategy

#### Pipeline Execution Sequence
1. **Ingestion:** Files (CSV, OFX, PDF, Images) are uploaded via the ASP.NET Core API[cite: 3].
2. **Evidence Persistence:** `FinancialEvidence` record is generated with SHA256 checksum and immutable raw storage[cite: 1, 2].
3. **Parsing & Normalization:** Importer plugins extract raw statements into standardized DTOs[cite: 3].
4. **Duplicate Detection:** Composite check using `Amount`, `Date window`, and `Source Account`[cite: 3].
5. **Rule & Identity Engine:** Exact matches process normalized merchant names and assign confidence-weighted categories[cite: 3].
6. **Graph Linking:** The `FinancialRecord` anchor is saved to the primary database and linked via relational foreign keys or graph nodes[cite: 2].

```text
           ┌────────────────────────────────────────────────────────┐
           │                     ASP.NET Core API                   │
           └───────────────────────────┬────────────────────────────┘
                                       │
            ┌──────────────────────────┼──────────────────────────┐
            ▼                          ▼                          ▼
   ┌─────────────────┐        ┌─────────────────┐        ┌─────────────────┐
   │ SQLite / Postgres│        │ Storage Engine  │        │ Rules / AI Core │
   │ (Relational Core)│        │ (Raw Evidence)  │        │ (Inferences)    │
   └─────────────────┘        └─────────────────┘        └─────────────────┘
```[cite: 3]

#### Database Migration Strategy
* **Phase 1 (Desktop & Local):** SQLite engine configured via Entity Framework Core[cite: 3]. Zero-configuration footprint running locally alongside ASP.NET Core process[cite: 3].
* **Phase 2 (Cloud & Server):** Seamless swap to PostgreSQL using EF Core provider switching without modifying application or domain logic[cite: 3].

---

## Part IV: Phased Execution Roadmap

```text
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│     PHASE 1     │    │     PHASE 2     │    │     PHASE 3     │    │     PHASE 4     │
│ Core Ingestion  │───>│ Intelligence &  │───>│ API & Client    │───>│  Stewardship &  │
│  & Persistence  │    │  Normalization  │    │   Ecosystem     │    │   AI Engine     │
└─────────────────┘    └─────────────────┘    └─────────────────┘    └─────────────────┘
```[cite: 3]

### Phase 1: Core Domain & Ingestion Engine (Milestone 1)
* **Domain Foundations:** Implement core Domain models (`FinancialEvidence`, `FinancialEvent`, `FinancialRecord`, `Account`, `Money`)[cite: 1, 2, 3].
* **Persistence Layer:** EF Core setup with SQLite provider and baseline migrations[cite: 3].
* **Import Framework:** Build `IImporter` interface and implement CSV/OFX parsers[cite: 3].
* **Basic REST API:** Endpoints for uploading evidence files and listing parsed records[cite: 3].

### Phase 2: Knowledge, Rules & Deduplication (Milestone 2)
* **Rules Engine:** User-definable matching rules for deterministic category and merchant assignments[cite: 3].
* **Normalization Engine:** Merchant string cleaning, alias resolution, and category taxonomy tagging[cite: 3].
* **Duplicate Detector:** Algorithmic duplicate transaction flagging based on date, amount, and text signatures[cite: 3].
* **Audit & Provenance:** Full tracking of system confidence and processing execution pipelines[cite: 1].

### Phase 3: ASP.NET Core API & Exporter Ecosystem (Milestone 3)
* **API Expansion:** Controller/Minimal API layers for Accounts, Records, Categories, and Rules[cite: 3].
* **Export Framework:** Implement `IBudgetProvider` interface with exporters for CSV, JSON, YNAB, and Goodbudget formats[cite: 3].
* **WPF Desktop UI Connection:** Refactor or build WPF interface to operate purely over HttpClient REST contracts[cite: 3].
* **Database Provider Switcher:** Configurable runtime switching between SQLite and PostgreSQL[cite: 3].

### Phase 4: Stewardship, Insights & Advanced Capabilities (Milestone 4)
* **Goal & Budget Engines:** Scenario planning, envelope budgeting, and goal forecasting engines[cite: 3, 4].
* **Stewardship Analytics:** Behavior tracking, longitudinal spending metrics, and alignment metrics[cite: 1, 4].
* **AI Advisor Pipeline:** Integration of LLM/AI services for receipt OCR extraction, natural language querying, and advisory insights with explainability metadata[cite: 1, 3].
* **Multi-Client Readiness:** Authentication (JWT) enabling Mobile (.NET MAUI) or Web clients to consume the ASP.NET Core backend[cite: 3].

```
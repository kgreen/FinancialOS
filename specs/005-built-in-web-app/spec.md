# Feature Specification: Built-In Web App for Manual Testing & Documentation

**Feature Branch**: `005-built-in-web-app`

**Created**: 2026-08-02

**Status**: Draft

**Milestone**: 005 — Internal Web Experience

## Overview

FinancialOS currently has an API layer and a desktop client, but it lacks a simple built-in web experience that can be launched locally for manual validation, stakeholder demos, and internal documentation. This feature adds a first-class web app that is shipped with the solution, runs against the existing API, and provides a guided way to explore core workflows without needing separate tooling.

The initial web app should feel like a lightweight “control room” for FinancialOS: it must be easy to start, easy to understand, and easy to use for manual testing of the platform’s most important behaviors.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Launch the app locally and inspect the platform quickly (Priority: P1)

A developer or product reviewer opens the FinancialOS solution, starts the web app, and sees a working dashboard with sample data and links to the most important workflows. They can immediately browse accounts, records, rules, and import/export actions without needing to set up any external client.

**Why this priority**: A built-in web app is only valuable if it is the easiest path to getting started. This is the main adoption and testing entry point.

**Independent Test**: Start the solution locally, open the web app, and verify that the home page loads, shows the expected summary cards, and exposes navigation to the main domain areas.

**Acceptance Scenarios**:

1. **Given** the solution is running locally, **When** the user opens the web app, **Then** the home page loads successfully and displays summary information for accounts, records, import jobs, and rules.
2. **Given** the app is running in a development environment, **When** the user opens the app without any extra setup, **Then** the app connects to the local API and renders data from the platform.
3. **Given** the app is launched for the first time, **When** no data has been loaded yet, **Then** the app shows a clear empty-state message and points the user to an onboarding or seed-data action.

---

### User Story 2 — Use the app to manually test the API workflows (Priority: P1)

A developer wants to manually verify core FinanceOS workflows such as viewing records, creating or applying rules, importing evidence, and triggering exports. They use the web app as a guided UI that exercises the REST API while also displaying the API response payloads in an understandable way.

**Why this priority**: Manual testing is a primary goal for this feature. The web app must make it easy to observe the system acting on real data.

**Independent Test**: From the web app, navigate to a records page, trigger an import or export action, and verify that the resulting response is displayed or summarized correctly.

**Acceptance Scenarios**:

1. **Given** the app is connected to a running API, **When** the user opens the Records page, **Then** they can view a paged list of records with visible filters and sorting controls.
2. **Given** the user has seeded or imported sample data, **When** they trigger an import or export action from the UI, **Then** the app shows the result status and any returned payload or downloadable artifact.
3. **Given** a validation failure occurs, **When** the user submits a bad request from the web app, **Then** the app surfaces the validation error clearly and explains how to correct it.

---

### User Story 3 — Use the app as a documentation surface (Priority: P2)

A contributor or reviewer wants to understand the platform’s capabilities without reading raw source code. They open the built-in app and use the documentation views to see example flows, sample payloads, and reference information for the API endpoints.

**Why this priority**: The app should reduce the friction of onboarding and documentation by exposing important workflows visually.

**Independent Test**: Open the documentation page and verify that it includes clear examples for at least one import flow and one export flow.

**Acceptance Scenarios**:

1. **Given** the user opens the documentation section, **When** they select a workflow such as import or export, **Then** they see a step-by-step explanation and an example request/response.
2. **Given** the app is running locally, **When** the user opens a documentation page, **Then** the page links to the relevant API endpoints and explains expected behavior in plain language.
3. **Given** a feature changes in the future, **When** the documentation is updated in the app, **Then** the content remains discoverable from the main navigation.

---

### Edge Cases

- The web app must still be usable when the API is unavailable; it should show a clear connection error state.
- The app must not require authentication for local development.
- If the user has no records or rules, the UI should show guidance rather than an empty crash-like experience.
- The app must degrade gracefully on missing sample data or incomplete backend services.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST include a built-in web application in the FinancialOS solution that can be launched locally for manual testing and documentation.
- **FR-002**: The web app MUST be implemented as a first-class project in the solution and MUST communicate with the existing ASP.NET Core API rather than bypassing it.
- **FR-003**: The web app MUST provide a home/dashboard view summarizing core domain state such as accounts, records, import jobs, and rules.
- **FR-004**: The web app MUST include at least the following core pages: Dashboard, Records, Accounts, Rules, Imports/Exports, and Documentation.
- **FR-005**: The web app MUST support viewing and filtering records through the existing API contracts rather than relying on direct database access.
- **FR-006**: The web app MUST provide a lightweight manual testing experience for at least one import flow and one export flow.
- **FR-007**: The web app MUST expose a simple documentation section with plain-language guidance, endpoint references, and sample payloads for common workflows.
- **FR-008**: The web app MUST provide a clear loading, empty, and error state for all core pages.
- **FR-009**: The web app MUST run in local development without authentication or additional setup beyond starting the API and the web app.
- **FR-010**: The web app MUST be designed so that future enhancements can add richer charts, workflows, or deeper API exploration without rewriting the shell.

### Non-Functional Requirements

- **NFR-001**: The web app MUST be lightweight enough to start quickly in local development.
- **NFR-002**: The web app MUST be understandable to new contributors without deep UI framework knowledge.
- **NFR-003**: The web app MUST remain aligned with the platform constitution: API-first, explainable, and human-controlled.
- **NFR-004**: The web app MUST not introduce a new persistence layer or duplicate source of truth for financial data.

### Key Entities

- **`WebAppShell`** *(transient UI shell)*: Shared application layout including navigation, error banner, and page routing.
- **`DashboardViewModel`** *(transient UI state)*: Aggregates summary data fetched from the API for display.
- **`ManualTestPage`** *(transient UI page)*: A simple surface for triggering an import/export flow and reviewing the API result.
- **`DocumentationPage`** *(transient UI page)*: Static or lightly dynamic guidance content linked to API capabilities and sample flows.
- **`ApiStatusState`** *(transient UI state)*: Tracks whether the API is reachable, loading, or failing.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new contributor can start the web app locally and reach the dashboard in under five minutes after the API is running.
- **SC-002**: The web app provides a usable experience for at least one import scenario and one export scenario without requiring external tools.
- **SC-003**: The web app exposes documentation content that covers at least one end-to-end workflow in plain language.
- **SC-004**: The web app shows a clear error state when the API is unavailable.
- **SC-005**: The feature introduces no direct database access from the web app and preserves the existing clean architecture boundaries.

---

## Assumptions

- The existing API endpoints and service layer are already available and will be reused.
- The initial release focuses on a simple, reliable experience rather than a polished production-grade UI.
- Local development is the primary target; authentication and advanced multi-user features are out of scope for this milestone.
- The app is intended as a manual-testing and documentation aid, not a full replacement for the desktop client or future mobile experiences.

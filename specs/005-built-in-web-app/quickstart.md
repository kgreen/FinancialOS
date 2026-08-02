# Quickstart: Built-In Web App

## Goal

Provide a simple, built-in web experience for FinancialOS that can be launched locally for manual testing and documentation.

## Intended Outcome

A contributor should be able to:

1. Start the API.
2. Start the built-in web app.
3. Open the dashboard and navigate to records, rules, imports/exports, and documentation.
4. Exercise at least one import and one export flow with visible results.

## Initial Implementation Notes

- Prefer a lightweight web framework that fits the existing .NET solution.
- Keep the UI API-first and avoid direct database access.
- Make the app discoverable from the main solution and document it in the repo.
- Use sample data and clear empty/error states to support first-run experience.

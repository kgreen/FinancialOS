# Quickstart: FinancialOS Platform Foundation

## Prerequisites

- .NET 8 SDK
- SQL Server or PostgreSQL optional for server deployment
- Sample financial statement files (CSV/OFX/PDF/image) for import testing

## Validate the foundation

1. Restore the .NET solution and build the API project.
2. Start the ASP.NET Core API locally.
3. Upload a sample CSV or OFX file through the evidence endpoint.
4. Confirm the system stores the evidence, creates a record, and exposes the record with provenance metadata.
5. Query the categories, rules, and account endpoints to validate the API contract.
6. Create a planning scenario through the planning-scenarios endpoint and verify it can be fetched by identifier.

## Expected outcomes

- Evidence upload succeeds and returns a persistent evidence identifier.
- The imported data becomes a financial record linked to an account and classification metadata.
- The API returns explainable data for downstream clients.

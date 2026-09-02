FinancialOS transaction CSV test fixtures

Use these files to manually test:
1) CSV import parsing (auto-detected layouts)
2) Partial-failure handling (bad rows)
3) Duplicate-row handling inside a single file
4) Export filters/date ranges with known sample data

Suggested upload command (API running on localhost:5229):
curl -X POST http://localhost:5229/api/v1/evidence -F "file=@test-data/transactions/chase-10rows.csv"

Files:
- chase-10rows.csv            Chase-style signed amount layout (10 valid rows)
- generic-signed-8rows.csv   Generic Date/Amount/Description layout (8 valid rows)
- citi-split-8rows.csv       Split Debit/Credit layout (8 valid rows)
- mixed-errors-6rows.csv     Includes malformed rows for partial-success testing
- duplicates-in-file-6rows.csv Contains repeated logical transaction rows for dedup testing

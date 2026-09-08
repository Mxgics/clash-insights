# Test plan

Verification in progress. Required commands: dotnet test tests/ClashInsights.Tests; web/npm run build; web/npm test -- --watch=false; web/npx playwright test e2e against the local host.

Cover tag normalization/validation, missing credentials with zero HTTP calls, 429 handling, unknown war data, observed roster differences, donation counter reset interpretation, demo labelling, tab navigation, exact history access, desktop/mobile overflow and axe accessibility. PostgreSQL verification must cover initial migration, persisted demo restart, advisory locking and backup restore to a separate database. Real Supercell integration is pending operator credentials and portal contract verification; fixture tests do not establish upstream compatibility.

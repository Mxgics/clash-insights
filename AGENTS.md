# Clash Insights

Local Angular 22 / ASP.NET Core 10 / PostgreSQL 17 dashboard. Read docs/project-plan.md for phase and acceptance evidence. V1 binds localhost, has no accounts, and uses configuration-managed tracked tags. Demo data must always be labelled and must never trigger upstream calls. Live API behaviour is unverified until configured credentials permit a smoke test.

Commands: powershell -File scripts/Start-Local.ps1; dotnet test tests/ClashInsights.Tests; cd web then npm run build, npm test -- --watch=false, npx playwright test e2e (requires running local host).

Keep API keys in local configuration or environment variables, never repository files. Preserve observed timestamps and gaps. No commits, pushes, public hosting or firewall changes without user authorization. Keep numbered plan, ADRs, cause/fix/verification change notes, test plan and versioned runbooks aligned with code. Never claim a planned v2 runbook is executable or validated.

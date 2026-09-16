# V1 verification — 2026-09-08

## Executed checks
- npm ci: successful locked restoration; audit reported zero vulnerabilities.
- npm run build (web): production build passed without budget warnings, approximately 285 kB initial raw bundle.
- npm test -- --watch=false (web): 6 tests passed across 5 suites.
- ./scripts/Test-Local.ps1: locked NuGet restore and 21 backend tests passed. Includes a fresh isolated PostgreSQL database with migrations, 30,240 recent observations plus older observations, history range coverage, independent advisory lock connections, retained successful data, retention cleanup, and a collector restart respecting persisted throttling.
- npx playwright test (web): 4 tests passed in managed Chromium. Desktop 1440x1000 and mobile 390x844; navigation, exact history, 7-day selection, empty/error/stale states, keyboard navigation, all-section axe checks and HTTP range/tag validation. No axe violations; no horizontal mobile page overflow; no page errors in the primary journey.
- ./scripts/Test-Backup.ps1: dump restored into a separate temporary database; count and earliest/latest timestamps matched. 2,311 demo snapshots persisted across application/database restarts and were not reseeded. Verification database removed after the successful comparison; backup retained locally.
- GET /health returned healthy; server listens on 127.0.0.1:5188; PostgreSQL binds 127.0.0.1:55432. .env and backups are excluded by gitignore.

## Evidence and reproduction
Run PowerShell 7 from the repository: ./scripts/Start-Local.ps1. In another terminal run ./scripts/Test-Local.ps1 and ./scripts/Test-Backup.ps1. Frontend commands run from web. Install browser binaries once with npx playwright install chromium. Do not run npm ci while builds/tests use node_modules. Screenshots are generated at web/test-results/dashboard-desktop.png and dashboard-mobile.png; test-results is ignored.

## Limits and remaining gates
All upstream client tests use synthetic responses, not Supercell. No developer account/token has been supplied and no live integration is claimed. User acceptance of the rendered UI is pending. There is no public deployment, automated off-device backup or v2 auth. Existing baseline commit 35e69b9 was observed; this verification task made no commits or pushes and created no additional worktree.


## Profile/theme follow-up — 2026-09-08
Tailwind/SCSS build passes without budget warnings. Frontend suite expanded to 10 tests, browser suite to 6 tests. Profile coverage includes persistent name/tag/range, preferred-player selection, explicit and device-following themes, reset, corrupt storage parsing, blocked storage and API downtime. Dark mode axe checks cover Overview, Players, Clan, Wars, Collection and My profile; mobile light profile has no overflow or axe violations. See changes/002-profile-themes.md.

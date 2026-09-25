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
The original automated upstream client tests use synthetic responses. Live clan integration was subsequently verified on 2026-09-18 as recorded below. User acceptance of the rendered UI is pending. There is no public deployment, automated off-device backup or v2 auth. Existing baseline commit 35e69b9 was observed; this verification task made no commits or pushes and created no additional worktree.


## Profile/theme follow-up — 2026-09-08
Tailwind/SCSS build passes without budget warnings. Frontend suite expanded to 10 tests, browser suite to 6 tests. Profile coverage includes persistent name/tag/range, preferred-player selection, explicit and device-following themes, reset, corrupt storage parsing, blocked storage and API downtime. Dark mode axe checks cover Overview, Players, Clan, Wars, Collection and My profile; mobile light profile has no overflow or axe violations. See changes/002-profile-themes.md.

## Live clan integration — 2026-09-18
- Reviewed official public Getting Started guidance: https://developer.clashofclans.com/#/getting-started . Keys use JWTs and allowed outgoing IPs; responses are UTF-8 JSON with HTTP status codes. Full endpoint documentation redirects to login and was not accessible in this session.
- Direct authenticated GET /v1/clans/%23REDACTED returned 200 and the locally tracked clan with 40 members. GET /v1/clans/%23REDACTED/currentwar returned 403 with reason accessDenied.
- Actual background collector persisted a Live clan observation at 2026-09-18 07:50:50.459868 UTC; PostgreSQL inspection confirmed 40 roster entries and isWarLogPublic=false. War failure persisted separately without a fabricated war snapshot.
- GET /health returned healthy. GET /api/dashboard returned Live mode, the matching clan and roster, successful clan attempt and contextual war-denial status. Browser verification displayed LIVE MODE, the locally tracked clan, 40 members, and the observed timestamp.
- ./scripts/Test-Local.ps1 passed all 23 backend tests, including isolated PostgreSQL tests and added 401/403 war handling cases.
- git check-ignore confirmed appsettings.Local.json is excluded. Local configuration is explicitly excluded from publishing. No secret values were printed in command output or stored in tracked files.
- No frontend changes; frontend suites were not rerun. Private-war success, live player detail, actual 429 throttling and full portal endpoint-reference review remain unverified. Earlier mocked coverage is not evidence of those live cases.

## Launch-readiness implementation — 2026-09-19

Executed on the uncommitted working tree:

- `./scripts/Test-Local.ps1`: 28/28 backend tests passed, including isolated PostgreSQL, public allowlist filtering, denied history, incomplete projections, anonymous private-route rejection, security headers, and explicit identity allowlisting.
- `npm test -- --watch=false`: 12/12 Angular tests passed.
- `npm run build`: passed without warnings; 544.33 kB initial raw and 149.62 kB estimated transfer. The higher explicit 600 kB initial budget accounts for Sentry; component style budget remains enforced at 5 kB. Critical-CSS inlining is disabled because Angular's inline loader is incompatible with the strict script CSP; styles remain minified and external.
- `npx playwright test e2e`: 6/6 passed in Chromium, including desktop/mobile, keyboard, axe, all sections, stale/error states, profile persistence, and public history validation.
- Local smoke: `/health/live` and the public dashboard returned 200; private API returned 401; CSP, `nosniff`, frame denial and public cache behavior were present. The loopback Live workspace remained readable.
- `npm audit`: zero known vulnerabilities. Both API and test projects reported no vulnerable direct or transitive NuGet packages from the configured sources.
- Production `docker build` completed successfully as `clash-insights:launch-readiness` using Node 24.19, .NET SDK/runtime 10, locked restores, Angular build, and non-root runtime.
- `./scripts/Test-Backup.ps1`: restored 2,322 snapshots into an isolated database and matched the count and time range (`2026-08-23` through `2026-09-19`). The verified local dump remains ignored under `backups/`.
- `git diff --check` is clean after removing the two trailing blank lines. Tracked-file checks found no `.env`, local settings, dependency/build folders, backups, or generated hashed web assets. The full two-commit patch scan found only documented CI placeholders and scripts that generate/read the ignored random local database password; no credential-shaped value was found. A dedicated launch-time scanner such as GitHub secret scanning or Gitleaks remains required.

Still required before launch: container runtime smoke with hosted staging configuration; dedicated full-history secret scanner; Render/Azure staging proof; real Auth0 failure/session testing; Sentry scrubbing inspection; WebKit/Firefox/Safari/device review; Lighthouse baselines; three approved public clans; GitHub settings/actions-SHA verification; staging restore/rollback rehearsal; and user visual acceptance. None of those unexecuted checks is claimed as complete.

## Public source release � 2026-09-22

Executed against a clean index export under ignored `work/release-check`, without personal local configuration or database volumes:

- Locked .NET restore and Release build passed with zero warnings/errors; 29/29 tests passed, including the new Demo no-service-resolution regression and isolated PostgreSQL integration. EF tool restoration and migration listing succeeded. API and test dependency audits reported no known vulnerabilities.
- Locked npm installation, 12/12 Angular tests and production build passed. Initial bundle: 583.86 kB raw, 155.41 kB estimated transfer; no budget warnings. npm audit reported zero vulnerabilities.
- Chromium Playwright: 6/6 passed, including keyboard navigation, desktop/mobile accessibility, history validation, profile persistence, themes and storage/API failures.
- Loopback smoke: readiness, public Demo dashboard and session returned 200; anonymous private dashboard returned 401; browser security headers were present.
- Docker image `clash-insights:public-release-check` built successfully from the clean candidate. Image user is non-root (1654), UI is packaged, and neither `.env` nor `appsettings.Local.json` is present. Runtime on a Docker internal network with no internet egress returned 200 for readiness, public Demo data and static UI, and 401 for anonymous private access. Compose validation passed.
- Isolated Demo backup/restore matched 2,311 observations and exact bounds: `2026-09-08 09:05:30.730246+00` through `2026-09-22 08:05:30.730246+00`. All source observations were Demo. Restore database was removed after comparison.
- Checksum-verified Gitleaks 8.30.0 detected a synthetic canary, then reported no findings across all 10 fetched commits or the candidate source files. Historical real clan identifiers were absent from fetched history; no rewrite was needed. A scan of existing GitHub content and 18 downloaded Actions run logs also reported no findings or identifying local clan/path matches. GitHub had no releases, artifacts, issue comments or PR review comments at inspection time; existing PRs were Dependabot updates.
- Historical real clan identifiers are redacted for publication; original timestamps and observed outcomes are retained. No live upstream requests or credential changes were needed.

GitHub publication and final remote checks are pending. Branch protection and outside-contributor workflow approval are unavailable while this repository is private on its current plan; they must be enabled and verified immediately after publication. Hosted application launch remains unverified and separate.

# V1 local operations

Status: local demo commands and recovery verified 2026-09-08; live clan collection verified 2026-09-18; private war data remains unavailable. See ../test-plan.md.

## Prerequisites and startup
PowerShell 7 (pwsh), Node 24.19+, .NET SDK 10, Docker Desktop Linux engine. Run scripts/Start-Local.ps1 from the repository. Database port 55432 and API port 5188 bind 127.0.0.1. Do not open firewall ports. The API applies committed EF migrations before serving requests. Back up before an upgrade that changes migrations. Use -SkipBuild only after a successful UI build/copy.

## Configuration
Generated .env holds CLASH_DB_PASSWORD for the local database. The start script sets ConnectionStrings__Clash only for its process. API appsettings.Local.json (ignored) can override Tracking settings and Clash:ApiToken. Alternatively use environment variables Tracking__Mode, Tracking__PlayerTags__0, Tracking__ClanTags__0 and Clash__ApiToken. Never paste token values into chat, logs or tracked files. Restart to apply tracking changes. Mode is Demo or Live; default Demo makes no Supercell calls. Live without a token pauses safely; empty tag arrays cause no collection.

Accepted default cadence: 60 minutes; retention: 90 days raw snapshots; request spacing: 1000 ms. These are not documented Supercell limits. The collector requests player detail, clan detail and current war endpoints. Public Getting Started guidance was reviewed and clan/current-war routes were smoke-tested on 2026-09-18. The full endpoint reference requires developer login; supported allowlist formats and exact upstream quotas remain portal review items. Home public IP changes may require the operator to update the API key allowlist. Browser localhost IP is not the outgoing public IP. Never rotate/create keys automatically.

## Health and recovery
GET /health checks database connectivity; Collection view shows collection outcomes separately. A healthy API does not imply successful upstream collection. Access denied can mean token, outgoing IP or private war visibility; inspect portal configuration. 429 defers the cycle using Retry-After where supplied; network and server failures wait for the next configured cycle. Last successful data remains visible. Do not infer zero activity from gaps or a counter reset. Refresh view reads the database only.

## Stop and restart
Ctrl+C stops the API and collector. docker compose stop stops PostgreSQL; docker compose up -d --wait restarts it. Do not use docker compose down -v: that deletes history. Collection resumes when the host starts; recent attempts are skipped until due. The demo seed is only created once and becomes stale over time by design.

## Backup and restore
Run ./scripts/Backup.ps1. Store the resulting dump outside the computer for protection against disk loss; the default backups folder is only local convenience. No automatic off-device backup is configured. The dump excludes token configuration; secure that separately.

Verify a dump by restoring into a NEW database (never overwrite your only copy): docker compose exec -T db createdb -U clash clash_restore_check; docker compose cp backups/CHOSEN.dump db:/tmp/restore.dump; docker compose exec -T db pg_restore -U clash -d clash_restore_check --exit-on-error /tmp/restore.dump. Compare snapshot counts and timestamps using psql. Only then plan an explicit cutover with the API stopped. Do not blindly downgrade schema; restore a pre-upgrade backup and matching application version when needed.

## Verification commands
Run ./scripts/Test-Local.ps1 for unit and isolated database tests. It creates a uniquely named test database and removes that database afterward; it never clears the application database. Run ./scripts/Test-Backup.ps1 for automated restoration comparison in another temporary database. From web, run npm test -- --watch=false and npx playwright test; install the managed browser once with npx playwright install chromium. Do not overlap npm ci with builds or tests: Windows locks loaded executables and an interrupted restore can leave incomplete dependencies. Once all frontend processes have stopped, npm ci repairs this.

## Data semantics
History reads use /api/players/{tag}/history?days=7, 14 or 90, relative to current UTC time. The summary uses latest per-entity data without a shared history truncation limit. Unknown numeric fields and absent rosters are unavailable. Refresh view reloads both summary and selected history. Existing demo snapshots retain their timestamps and intentionally become stale; restarting never fabricates newer observations. Collection gaps may conceal resets or membership activity; observed differences are not a complete event log.

## Version boundary
V1 runbook applies only to localhost. V2 public deployment requires its own tested hosting, secrets, TLS, auth, migrations, backups and rollback procedure. See v2-hosting-auth.md.

## My profile and appearance
Open My profile from navigation. Display name, preferred player tag, default history range and light/dark/device appearance are saved in this browser's localStorage under clash-insights.preferences.v1. The preferred tag selects an already-tracked player; it does not start collection. A missing preferred player falls back to the first available player. Reset preferences restores blank name/tag, 14 days and device theme without altering PostgreSQL history. If storage is blocked, settings apply for the visit and an alert explains the persistence failure. Different browsers or localhost versus 127.0.0.1 have separate settings. No login or server profile is introduced.

Styling: Tailwind 4 uses src/tailwind.css and .postcssrc.json; existing SCSS uses shared theme tokens from src/styles.scss. Keep Tailwind directives out of component SCSS. Verify new screens in both themes and with keyboard navigation.

## Configured live clan — 2026-09-18
The ignored src/ClashInsights.Api/appsettings.Local.json selects Live mode, clan [redacted clan tag] (the locally tracked clan), and the supplied token. No individual player tags are configured. Preserve this local file when restarting; it is excluded from publish artifacts. Local JSON overrides environment configuration with the current provider ordering. Do not print its contents when troubleshooting.

Start with pwsh -File scripts/Start-Local.ps1 -SkipBuild when UI assets are already built. Open http://127.0.0.1:5188 and inspect Clan and Collection. Expect 40 members at the initial observation (membership can change), a Collected clan attempt and a war access-denied attempt. Collection runs hourly while the host is running; Refresh view only reads saved data.

The clan currently reports isWarLogPublic=false. A clan leader/co-leader can make the war log public in game if desired. Then wait for the next scheduled collection and inspect Collection; restarting does not bypass persisted due times. Do not delete schedules or history to force a retry. For broader access failures verify the key's allowed outgoing IP in the developer portal.

Rollback to demo: change only Tracking.Mode in the ignored local JSON to Demo and restart. Existing live history is preserved; demo mode performs no upstream requests. For a background launch, stop the ClashInsights.Api process belonging to this checkout before restarting; do not stop unrelated dotnet or PowerShell processes. Background launch logs for this verification are under ignored work/live-host.out.log and work/live-host.err.log.

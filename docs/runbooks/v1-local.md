# V1 local operations

Status: implementation present; see test-plan.md for verification evidence and limitations.

## Prerequisites and startup
Windows PowerShell, Node 24.19+, .NET SDK 10, Docker Desktop Linux engine. Run scripts/Start-Local.ps1 from the repository. Database port 55432 and API port 5188 bind 127.0.0.1. Do not open firewall ports. The API applies committed EF migrations before serving requests. Back up before an upgrade that changes migrations. Use -SkipBuild only after a successful UI build/copy.

## Configuration
Generated .env holds CLASH_DB_PASSWORD for the local database. The start script sets ConnectionStrings__Clash only for its process. API appsettings.Local.json (ignored) can override Tracking settings and Clash:ApiToken. Alternatively use environment variables Tracking__Mode, Tracking__PlayerTags__0, Tracking__ClanTags__0 and Clash__ApiToken. Never paste token values into chat, logs or tracked files. Restart to apply tracking changes. Mode is Demo or Live; default Demo makes no Supercell calls. Live without a token pauses safely; empty tag arrays cause no collection.

Provisional cadence: 60 minutes; retention: 90 days raw snapshots; request spacing: 1000 ms. These are not documented Supercell limits. The collector requests player detail, clan detail and current war endpoints. Endpoint contracts, supported IP allowlist format and exact upstream rate limits must be verified in the developer portal before a real integration smoke test. Home public IP changes may require the operator to update the API key allowlist. Browser localhost IP is not the outgoing public IP. Never rotate/create keys automatically.

## Health and recovery
GET /health checks database connectivity; Collection view shows collection outcomes separately. A healthy API does not imply successful upstream collection. Access denied can mean token, outgoing IP or private war visibility; inspect portal configuration. 429 defers the cycle using Retry-After where supplied; network and server failures wait for the next configured cycle. Last successful data remains visible. Do not infer zero activity from gaps or a counter reset. Refresh view reads the database only.

## Stop and restart
Ctrl+C stops the API and collector. docker compose stop stops PostgreSQL; docker compose up -d --wait restarts it. Do not use docker compose down -v: that deletes history. Collection resumes when the host starts; recent attempts are skipped until due. The demo seed is only created once and becomes stale over time by design.

## Backup and restore
Run ./scripts/Backup.ps1. Store the resulting dump outside the computer for protection against disk loss; the default backups folder is only local convenience. No automatic off-device backup is configured. The dump excludes token configuration; secure that separately.

Verify a dump by restoring into a NEW database (never overwrite your only copy): docker compose exec -T db createdb -U clash clash_restore_check; docker compose cp backups/CHOSEN.dump db:/tmp/restore.dump; docker compose exec -T db pg_restore -U clash -d clash_restore_check --exit-on-error /tmp/restore.dump. Compare snapshot counts and timestamps using psql. Only then plan an explicit cutover with the API stopped. Do not blindly downgrade schema; restore a pre-upgrade backup and matching application version when needed.

## Version boundary
V1 runbook applies only to localhost. V2 public deployment requires its own tested hosting, secrets, TLS, auth, migrations, backups and rollback procedure. See v2-hosting-auth.md.

# Clash Insights

Clash Insights is a read-only Angular 22 dashboard backed by ASP.NET Core 10, EF Core, and PostgreSQL 17. It stores timestamped Clash of Clans observations, keeps missing upstream fields explicit, and separates an anonymous public view from an Auth0-protected private workspace.

## Architecture and visibility

Angular is built into the ASP.NET Core publish artifact. The API reads PostgreSQL; a background collector performs upstream calls and is protected across replicas by a PostgreSQL advisory lock. `/api/public/*` is filtered at the query boundary to the configured `PublicClanTags` and `PublicPlayerTags`. `/api/private/*` requires an authenticated, explicitly allowlisted Auth0 identity. Unknown or non-public history tags return 404. Authenticated responses are `private, no-store`.

Local V1 keeps its loopback-only behavior and can show all locally configured targets. Hosted mode requires Live collection, exactly three public clans, Auth0, trusted proxy addresses, HTTPS/HSTS, and explicit user allowlisting before startup succeeds.

## Run and verify locally

Install PowerShell 7, Node.js 24.19+, the .NET 10 SDK and Docker Desktop with its Linux engine running. Clone the repository and start the app:

```powershell
./scripts/Start-Local.ps1
```

The script creates an ignored random database password, installs locked dependencies and builds the UI. A fresh checkout starts in Demo mode without API credentials. Keep the host running, then run these checks in a second terminal:

```powershell
./scripts/Test-Local.ps1
./scripts/Test-Backup.ps1
```

Open `http://127.0.0.1:5188`. Frontend-only checks run from `web` (install Chromium once with `npx playwright install chromium`):

```powershell
npm test -- --watch=false
npm run build
npx playwright test e2e
```

Demo data is labelled and never triggers upstream calls. Live credentials belong only in ignored `appsettings.Local.json`, environment variables, or a host secret store.

## Configuration

Copy `.env.example` only as a list of variable names; never commit real values. Production requires the database connection, Clash token, three approved public clan tags, any approved public player tags, Auth0 issuer/client credentials, an explicit Auth0 subject or email allowlist, data-protection key storage, trusted proxy IPs, and optional Sentry DSNs. Angular receives only the public Sentry DSN and environment from `/api/client-config`; credentials and allowlists remain server-side.

The three public clans must be manually reviewed for suitability and public war-log behavior. A locally tracked private clan must never be added to the public allowlist without explicit approval. Historical documentation anonymizes the real clan name and tag; redactions and configuration placeholders are not runnable game tags.

## Deployment status

The application is container-ready, but it has not been provisioned or deployed. Render and Azure App Service still require a staging proof-of-concept covering fixed outbound egress, PostgreSQL 17, TLS, previews/slots, backups/PITR, restore, rollback, and approved monthly cost. Run migrations explicitly with the container's `--migrate-only` argument before starting the web service.

See [the project plan](docs/project-plan.md), [verification evidence](docs/test-plan.md), [local runbook](docs/runbooks/v1-local.md), [source release procedure](docs/runbooks/v1-public-source.md), and [hosting evaluation runbook](docs/runbooks/v2-hosting-auth.md). Source publication does not deploy the app or validate hosted operation.

## Privacy and licensing

This is an unofficial fan project. Stored observations can contain public game names, tags, roster membership, and activity counters. Do not treat authentication as consent to expose private clan data, and do not put payloads or identifiers into telemetry. The source code is licensed under the [MIT License](LICENSE). Game names and other third-party material remain the property of their respective owners.

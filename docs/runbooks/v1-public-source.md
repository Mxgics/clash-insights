# Public source release

## Prerequisites

Repository admin access, a clean candidate checkout, .NET 10, Node 24.19+, PowerShell 7, Docker Linux engine and checksum-verified Gitleaks 8.30.0. Never copy personal `.env`, local API configuration, backups or live database volumes into the candidate checkout. Keep scanner output redacted.

## Verify a candidate

1. Fetch all remote branches and tags. Scan history with `gitleaks git --redact --log-opts="--all" .`; scan only publishable candidate files with `gitleaks dir --redact .` before installing/building dependencies. Review scanner findings and anonymized identifiers across history. Do not rewrite history without explicit authorization.
2. Start an isolated Demo checkout with `./scripts/Start-Local.ps1` after setting `$env:COMPOSE_PROJECT_NAME="clash-release-check"` and confirming the default loopback ports 55432 and 5188 are unused. If either port is occupied, use a separate test machine or document explicit Compose/API overrides; never stop an unrelated host. Run `./scripts/Test-Local.ps1` and `./scripts/Test-Backup.ps1` against that isolated project, never a live database.
3. In `web`, run `npm test -- --watch=false`, `npm run build`, `npx playwright install chromium`, `npm run e2e` and `npm audit`. Run `dotnet list tests/ClashInsights.Tests package --vulnerable --include-transitive` and `git diff --check` from the root.
4. Build `docker build -t clash-insights:release-check .`. Smoke the non-root image against an isolated PostgreSQL database in Demo mode. In local mode requests must originate from loopback inside the application container; do not weaken the local-only middleware to expose the test image. Verify ready/public responses, anonymous private rejection, security headers, static UI and absence of local configuration in the image.
5. Record actual results, versions and limitations in `docs/test-plan.md`. Historical evidence is not a substitute for a current candidate check.

## Publish and maintain

Use a `codex/` or feature branch, merge a passing pull request to `develop`, and promote it to `main` through another passing pull request. Preserve ancestry and synchronize `develop` after promotion. `main` is the public default branch. Required checks are Backend, Web, Browser, Compose and Secrets. Require pull requests and resolved conversations; block force pushes/deletion, with no mandatory external approval for the solo owner.

Before changing visibility, inspect all existing repository discussions, pull requests, releases, Actions logs and artifacts for secrets or personal data. Enable available dependency alerts/security updates, private reporting and secret scanning/push protection; verify public-only settings immediately after publication. Keep Actions default permissions read-only and require approval for outside-contributor workflows. Do not pass live secrets to fork tests.

## Failures and recovery

A failed restore, scan, test, image smoke or GitHub check blocks publication. Inspect the failing job and fix the source on the release branch. If access prevents a setting from being verified, report the exact setting rather than claiming completion. Preserve old commits and unrelated Dependabot work. Application deployment uses the separate, still-unverified V2 runbook.

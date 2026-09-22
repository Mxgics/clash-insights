# Hosted staging evaluation and operations

Status: IMPLEMENTATION-READY, NOT PROVISIONED OR VERIFIED. This is an evaluation checklist until one provider is selected and staging evidence is appended. It does not authorize spend, deployment, DNS, or secret creation.

## Provider proof-of-concept

Build the same `Dockerfile` image for Render and Azure App Service. Use isolated staging databases and staging Clash/Auth0/Sentry credentials. A preview must never inherit production credentials.

For each provider record: current monthly cost; .NET 10 container support; PostgreSQL 17 compatibility; fixed outbound addresses accepted by the Clash API allowlist; TLS/custom domain and renewal; preview/slot isolation; liveness and readiness probes; retained logs; backup/PITR window; restore time; and image rollback. Prefer Render only when all requirements and the approved budget pass. Select Azure if Render fails egress, security, reliability, or cost. Do not claim selection from documentation alone.

## Required secret/configuration groups

Use the names in `.env.example`. Store values in provider secrets, never environment files in the image, GitHub logs, browser bundles, or docs. Hosted startup deliberately fails without exactly three public clans, Auth0 plus an allowed identity, and trusted proxy addresses. Never add a locally tracked private clan to `Tracking__PublicClanTags` without explicit approval. Historical identifiers have been redacted.

Persist `Hosting__DataProtectionPath` on encrypted durable storage shared by instances. Limit `Hosting__KnownProxies` to documented platform proxy IPs. Store Sentry source-map credentials only in the deployment environment; the browser receives only its public DSN.

## Release sequence

1. Build and scan an immutable image; record its digest and release identifier.
2. Take and verify a database backup. Put the collector in a controlled state.
3. Run the image once with `--migrate-only`; abort on failure.
4. Deploy staging, then check `/health/live`, `/health/ready`, security headers, HTTPS redirect, and HSTS.
5. Smoke-test anonymous public data, guessed private tags returning 404, Auth0 login/logout/failure, and private `no-store`/`noindex` responses.
6. Verify exactly one collector across replicas, stable Clash egress, stale-data alerts, scrubbed Sentry events, and no private data in anonymous caches/errors.
7. Run Playwright, Lighthouse, restore, and rollback rehearsals. Production/domain work requires a separate approval.

## Restore and rollback

Restore into a new database first and compare observation counts and earliest/latest timestamps. Point staging at the restored database and smoke-test public/private boundaries. For rollback, stop collection, redeploy the prior image, and restore the pre-migration database only when schema compatibility requires it. Do not roll application code backward while leaving an incompatible schema forward.

## Monitoring and alerts

Alert on failed readiness, stale collection beyond two configured intervals, authentication failures above an agreed threshold, elevated server errors, and failed/missed backups. Tag collector failures separately from web request failures. Do not attach bodies, cookies, authorization headers, Clash tokens, or default PII to telemetry.

## Launch gates

Provider/cost approved; three public clans manually approved with public war logs; staging restore and rollback rehearsed; production secrets entered; Auth0 callbacks exact; domain/TLS renewal verified; alerts tested; GitHub rules/security reviewed; CI, Playwright, Lighthouse and scans green; and the owner accepts the visual review. Until then this runbook remains unverified.

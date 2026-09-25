# Change 004: Hosted security and launch readiness

## Cause

The local-only application had no hosted trust boundary, identity layer, public/private data contract, production container, telemetry integration, or deployment acceptance procedure.

## Change

- Added deny-by-default public allowlists and authenticated private routes, with history filtering at the database query boundary.
- Added Auth0 OIDC/PKCE cookies, explicit allowed-user checks, antiforgery, rate limiting, request limits, proxy trust, HTTPS/HSTS, cache controls, CSP and security headers.
- Added liveness/readiness, explicit `--migrate-only`, persistent data-protection configuration, one-container Angular/.NET packaging, and Sentry without default PII or replay.
- Added an intentional 404, metadata, branded favicon, responsive/auth UI, tests, and documented environment names.
- Preserved local loopback behavior and did not provision, deploy, purchase, commit, push, or mutate GitHub settings.

## Verification

`scripts/Test-Local.ps1` passes 28 backend tests including PostgreSQL and anonymous/private boundary checks. Angular has 12 passing unit tests and a warning-free production build. Other results and open gates are in `docs/test-plan.md`.

## Rollback

Retain the prior immutable image and a pre-migration backup. Stop the collector, restore the backup if the migration is not backward-compatible, deploy the prior image, and run public/private isolation smoke tests before collection resumes.

# Project plan

## 1. Product and success criteria
Personal player and clan dashboard. V1: localhost, unauthenticated viewing, config-managed tracking, an original default of up to 10 explicit players and 2 clans (current validation supports up to 10 tracked clans for hosted public/private separation), hourly collection and 90 days of detailed history. Player trophies/progression, donation counters, observed roster changes and current-war snapshots. V2: hosting/auth; CWL, raids and equipment analytics are later milestones.

## 2. Architecture
Angular 22 (exact versions in web/package-lock.json), .NET 10, EF Core 10.0.11, Npgsql EF provider 10.0.3 and PostgreSQL 17. Angular is served by one ASP.NET Core host. The browser only reads local stored data. Background collection is sequential, protected by a PostgreSQL advisory lock; per-target due times and global Retry-After survive restart. Raw JSON is stored with source and observation time. Missing fields remain unavailable, not zero; charts break across gaps and donation resets are not interpreted as negative activity.

## 3. Current phase
The source repository was made public on 2026-09-25 with MIT licensing, anonymized evidence, passing release CI and verified GitHub protections. `main` is the default branch and `develop` remains the integration branch. Hosted security, public/private query boundaries, Auth0 plumbing, Sentry and container packaging are implemented. Local live collection was verified on 2026-09-18; the real clan identity is redacted. No hosted application has been provisioned or deployed. Current release evidence is in test-plan.md and changes/005-public-source.md.

## 4. Completed acceptance checks
Clean frontend build; 12 frontend tests; 29 backend tests including real isolated PostgreSQL integration and security pipeline checks; local security-header/API smoke test; loopback bindings; npm audit clean; secret exclusions. A clean Demo backup/restore was rerun on 2026-09-22. See test-plan.md for exact current evidence.

## 5. Remaining gates
1. User reviews the rendered local dashboard.
2. Completed 2026-09-18: supplied token configured in ignored local configuration; official public API guidance reviewed. Full endpoint reference requires portal login; no exact upstream quota is asserted.
3. Completed live clan collection for [redacted clan tag]: 40 members persisted and rendered. War endpoint returns 403; clan reports isWarLogPublic=false. Public war log is required for war tracking. Player tracking awaits explicitly selected player tags. Repository default remains Demo with no upstream requests.
4. Run the Render/Azure staging proof-of-concept, approve three public clans, configure real Auth0/Sentry secrets, perform restore/rollback/Lighthouse/cross-browser checks, and complete the hosted launch checks.
5. Source-release commits, pushes, merges, GitHub settings and public visibility are authorized for this milestone. Provisioning, purchases, app deployment and DNS still require separate authorization.

## 6. Change history
See changes/001-local-v1.md and decisions/001-local-architecture.md. Local-first v1 supersedes the earlier free-hosting exploration. Counts/cadence/retention are accepted defaults, not unresolved questions or asserted Supercell quotas.


## 7. Profile and theme extension
Implemented browser-local My profile settings and light/dark/device themes using Tailwind 4.3.3 alongside SCSS. These are preferences, not v2 accounts; config-managed server tracking is unchanged. See changes/002-profile-themes.md.

## 8. Live clan activation — 2026-09-18
Local configuration now runs Live with hourly collection for the locally tracked clan. See changes/003-live-clan.md for evidence and limitations.

## 9. Public source release — 2026-09-25
Published at https://github.com/Mxgics/clash-insights. `main` is the default branch and `develop` remains the integration branch. MIT licensing, anonymized identifiers, full-history scanning and reproducible Demo checks passed. Branch protections, required CI, read-only Actions defaults, SHA pin enforcement, dependency security updates, secret scanning, push protection, private vulnerability reporting and external-contributor workflow approval were verified after publication. See changes/005-public-source.md and runbooks/v1-public-source.md. Hosted launch remains separate.

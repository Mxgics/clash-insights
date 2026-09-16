# Project plan

## 1. Product and success criteria
Personal player and clan dashboard. V1: localhost, unauthenticated viewing, config-managed tracking, up to 10 explicit players and 2 clans, hourly collection and 90 days of detailed history. Player trophies/progression, donation counters, observed roster changes and current-war snapshots. V2: hosting/auth; CWL, raids and equipment analytics are later milestones.

## 2. Architecture
Angular 22.1.5, .NET 10, EF Core 10.0.11, Npgsql EF provider 10.0.3 and PostgreSQL 17. Angular is served by one ASP.NET Core host. The browser only reads local stored data. Background collection is sequential, protected by a PostgreSQL advisory lock; per-target due times and global Retry-After survive restart. Raw JSON is stored with source and observation time. Missing fields remain unavailable, not zero; charts break across gaps and donation resets are not interpreted as negative activity.

## 3. Current phase
Local demo implementation and automated verification complete on 2026-09-08. User visual acceptance and live API integration remain separate open gates. See test-plan.md for executed evidence. An existing main-branch baseline commit 35e69b9 was observed during final audit; this task did not create it. No additional worktree exists.

## 4. Completed acceptance checks
Clean frontend build; 6 frontend tests; 21 backend tests including real isolated PostgreSQL integration; 4 browser/API checks; desktop/mobile and keyboard accessibility; persistence across restart; backup restoration with matching counts/timestamps; loopback bindings; secret exclusions. Documentation includes versioned runbooks and verified commands.

## 5. Remaining gates
1. User reviews the rendered local dashboard.
2. Operator creates a Clash developer account, reviews current endpoint contracts/IP restrictions/rate limits, and configures a token locally. Do not share secrets in chat.
3. Operator supplies desired player/clan tags; switch to Live and verify successful observations plus credential/visibility failure handling. Until then the default Demo mode makes no upstream requests.
4. Only after separate authorization: further commits, Codex project registration and an additional worktree. Hosting and authentication remain v2.

## 6. Change history
See changes/001-local-v1.md and decisions/001-local-architecture.md. Local-first v1 supersedes the earlier free-hosting exploration. Counts/cadence/retention are accepted defaults, not unresolved questions or asserted Supercell quotas.


## 7. Profile and theme extension
Implemented browser-local My profile settings and light/dark/device themes using Tailwind 4.3.3 alongside SCSS. These are preferences, not v2 accounts; config-managed server tracking is unchanged. See changes/002-profile-themes.md.

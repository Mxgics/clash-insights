# Project plan

## 1. Product and success criteria
Personal accounts and clan dashboard: player progression, donations, observed membership changes and current war snapshots. V1 local, unauthenticated viewing, config-managed tags. V2 hosting and authentication. All collected upstream JSON is retained to preserve fields for future views; no claim to reconstruct unseen events.

## 2. Architecture and constraints
Angular 22 SPA served by ASP.NET Core 10 Minimal APIs; one BackgroundService collector; EF Core with PostgreSQL 17 on Docker. Browser reads stored data; backend alone calls Supercell. Sequential collection plus a PostgreSQL advisory lock prevents simultaneous collectors. API failure preserves last successful snapshots. Demo and live datasets are separated by source. Local process shutdown leaves gaps.

## 3. Current phase
Local implementation and verification in progress. No commit, push or public deployment. A real worktree remains deferred until an authorized initial commit and Codex project registration exist.

## 4. Acceptance criteria
Build and behaviour tests pass; rendered desktop/mobile navigation and accessible dashboard verified; database migration and backup restore tested; missing credentials cause zero upstream calls; synthetic data stays labelled. Live API endpoint/IP/rate-limit validation requires configured credentials and remains an explicit integration limit.

## 5. Provisional defaults and outstanding work
Demo mode initially. Live cadence 60 minutes, 1000 ms request spacing, 90 days raw history with no aggregation. These are configurable application defaults, NOT asserted API quotas. Counts, cadence, retention and developer-account availability are awaiting user answers. No implicit tracking of every searched entity. V2 provider and auth selection remain future decisions. Expanded CWL/raid analysis and equipment views require verified API contracts; current raw player snapshots preserve returned progression fields.

## 6. Change history
See docs/changes/001-local-v1.md and docs/test-plan.md. Accepted local-first decision supersedes earlier free-hosting exploration.

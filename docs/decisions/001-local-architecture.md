# ADR 001: Local monolith with persistent observations

Status: implemented baseline; defaults accepted: 10 players, 2 clans, hourly, 90 days.

2026-09-18 operational validation: live clan collection now uses ignored local configuration, retaining the same server-only collector and source-separated history. Local token configuration is excluded from publish output. Private current-war access remains unavailable rather than being replaced with demo data. This activation does not change the architectural decision; see ../changes/003-live-clan.md.

Use Angular 22 and .NET 10 per user selection, PostgreSQL 17 for persistent history, and one local web host/collector. PostgreSQL avoids a SQLite provider migration in v2. The user chose local v1 and hosted v2; no cloud resources are needed now. Loopback binding and request checks limit access to this computer. Fixed tracking configuration avoids v1 account/admin complexity.

Collection is snapshot-based and host-bound: unavailable time periods cannot be backfilled unless the upstream API explicitly exposes them. Keep raw observations alongside typed presentation projections. No Redis, broker, SignalR, microservices or persistent scheduler is needed for this local scale. Advisory locking prevents overlapping processes; durable attempt timestamps prevent immediate duplicate refresh on restart.

Sources checked: https://angular.dev/reference/versions ; https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core ; https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services ; https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers .

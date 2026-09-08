# V2 hosting and authentication

Status: PLANNED ONLY — not executable, not deployed or verified.

## Decisions carried forward
Unauthenticated viewing is desired; authentication is planned for v2 management features. Hosting provider, budget, identity provider and exact authorization roles are not selected. Do not provision resources from this document.

## Before implementation
Verify Supercell endpoint contracts, IP allowlisting and rate limits in the official portal. Choose hosting that supports reliable collection and permitted outbound addresses; evaluate database capacity using measured v1 data. Select an authentication provider and specify viewing vs administrative access. Preserve config-managed tracking until an authenticated management workflow is implemented.

## Required operational procedures
Add verified build/start commands, provider-specific secret configuration, stable egress/key setup, TLS/domain and auth callback settings, migrations and local-history import, single-collector coordination, health and stale-data monitoring, backup schedule and restore drill. Rehearse deployment rollback and document schema compatibility. Test authorization on every management endpoint, not just the Angular UI.

## Cutover acceptance
Restore a v1 backup into staging; compare counts and dates; verify demo/live separation; test throttling and upstream failures; test login/logout and forbidden management requests; verify no secrets in browser bundles/logs. Establish recovery objectives and budget controls. Mark this runbook verified only after evidence exists. No public exposure is authorized by v1 setup.

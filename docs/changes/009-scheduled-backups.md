# 009: Verified scheduled backups

Date: 2026-10-01.

Problem: V1 had manual dump and restore-check scripts but no safe retention command suitable for an operator-created daily schedule. Naive age-based deletion could remove the last known-good dump after a failed backup or restore.

Cause: dump creation, restore verification, and retention pruning were separate manual operations. The repository did not define a narrow filename boundary or an ordering guarantee for partial failure.

Change: add `scripts/Backup-Scheduled.ps1` with a mandatory absolute destination and a default retention count of 14. Take an exclusive per-destination lock, create the dump under a temporary name, restore it into a unique database, compare the source/restored snapshot count and time range, publish the verified filename, then prune only older files matching the script's verified-name format. Preserve a locally copied dump as `*-unverified.dump` when verification fails. Document daily Windows Task Scheduler configuration without creating a task or selecting cloud storage.

Rationale: verification precedes both publication and deletion. A failed dump, copy, restore, or comparison therefore cannot prune a known-good backup. The destination lock prevents overlapping tasks from racing through the manual backup's fixed container staging path. Exact destination and filename checks constrain deletion to files owned by this workflow.

Verification: PowerShell parsing, parameter guards, and the overlapping-run guard passed. Against the real local PostgreSQL 17 container, four successive dumps each restored and matched 2,322 snapshots and the same earliest/latest timestamps. With retention set to two, only older verified matching dumps were removed; two verified dumps and the non-prunable lock file remained, and all temporary verification databases were removed. The existing local suite passed 29/29 backend tests and its independent backup/restore comparison. Exact evidence is in `docs/test-plan.md`.

Limits: the scheduled command requires Docker Compose and the database service to be available in the selected Windows task session. The task itself, destination permissions, monitoring, and off-device synchronization remain operator-managed. Snapshot count and time-range equality detects incomplete or stale restores but is not a byte-for-byte logical comparison of every row.

Recovery: an `*-unverified.dump` must not replace a verified backup. Inspect the reported failure, confirm Docker/database health and destination capacity, then rerun. If cleanup warns about a `clash_restore_<guid>` database, confirm the exact generated name before dropping it. Never broaden the retention pattern or delete the destination recursively.

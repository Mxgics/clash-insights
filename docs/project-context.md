# Project context

The local Angular/.NET/PostgreSQL application supports labelled Demo data without upstream requests and configuration-managed Live collection. Browser-local profile preferences and device/light/dark themes are implemented.

Hosted public/private query boundaries, Auth0 integration and container packaging are implemented but hosted deployment remains unverified. Source publication is a separate release milestone and does not imply a running public service.

The public-source release uses MIT licensing, anonymized historical evidence, main as the default branch and develop as the integration branch. See [the numbered plan](project-plan.md), [test evidence](test-plan.md) and [public release runbook](runbooks/v1-public-source.md) for current status.

Preserve observation timestamps, missing values, gaps and counter resets. Collection remains sequential with a PostgreSQL advisory lock, persistent retry scheduling, an hourly default and 90-day retention. No hosting, account-provider provisioning or DNS changes are part of source publication.

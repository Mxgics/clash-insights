# Project context

## Current state

Local V1 is implemented and verified. It is a loopback-only, unauthenticated Angular/.NET/PostgreSQL dashboard for a personal set of tracked Clash of Clans players and clans. The default **Demo** mode uses clearly labelled synthetic data and never calls the upstream API.

The recent extension added Tailwind 4 alongside the established SCSS, browser-local **My profile** preferences (display name, preferred player, default history range), and light, dark and device themes. These settings are local browser preferences, not an account system, and do not change server-side tracking.

## Accepted V1 decisions

- Track up to 10 explicit players and 2 clans; collect hourly and retain 90 days of detailed history.
- Store observed source data and timestamps. Missing values stay unavailable; gaps remain visible; donation resets are not treated as negative activity.
- Keep collection sequential, resilient to restart and protected by a PostgreSQL advisory lock. Persist target due times and global Retry-After handling.
- Keep V1 local, configuration-managed and without accounts. Hosting and authentication are V2 work. CWL, raids and equipment analytics are later work.

## Verification recorded

The original local V1 verification recorded clean frontend and backend checks, PostgreSQL integration, browser/API checks, accessibility and backup restoration. The profile/theme extension subsequently recorded a production frontend build, 10 frontend tests and 6 browser checks, including preference persistence, storage failure handling, theme changes and mobile layouts.

## Remaining gates

1. Review the local rendered dashboard.
2. Set up the Clash developer portal access, review the then-current API constraints, and configure a token locally. Do not place credentials in chat or repository files.
3. Add the desired real player/clan tags, switch from Demo to Live, and verify observations and expected credential/visibility failures.
4. Hosting, authentication and public exposure require a separate V2 decision and authorization.

Use [the project plan](project-plan.md), [test plan](test-plan.md), and the versioned runbooks for operational detail. This file is a consolidated status record from the recent project conversations; it does not claim live API validation.

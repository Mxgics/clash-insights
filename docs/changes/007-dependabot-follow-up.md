# 007: Dependabot follow-up batch

Date: 2026-09-28.

Problem: the dependency refresh triggered seven new Dependabot pull requests, including compatibility-linked Angular 22.2 and Vitest 5 updates plus breaking test-runner and Sentry browser majors. The Sentry-only PR failed because v11 removed `sendDefaultPii` and changed its default data-collection behavior.

Cause: the first batch intentionally stayed on Angular 22.1/Vitest 4; Angular 22.2 subsequently supplied the declared Vitest 5 peer range. Dependabot grouped Angular packages but not Vitest or the .NET test-tooling family. Sentry 11 requires an explicit migration to its granular `dataCollection` option.

Change: align the Angular runtime/build family on 22.2.0 and Vitest on 5.0.2; update Prettier to 3.9.9 and Sentry Angular to the resolved 11.1.0; update coverlet.collector to 10.1.0, Microsoft.NET.Test.Sdk to 18.10.1, and xunit.runner.visualstudio to 4.0.0. Replace `sendDefaultPii: false` with an explicit restrictive Sentry data-collection policy. Group Angular with Vitest, group frontend tooling, and group .NET test tooling for future Dependabot runs.

Rationale: these updates are individually green except for the known Sentry migration, but their shared lockfiles and peer constraints make one regenerated graph safer than seven independent merges. Explicitly disabling sensitive Sentry categories preserves the project's telemetry boundary instead of accepting v11's broader defaults.

Verification: npm clean install, 12 frontend tests, production build, npm audit, locked NuGet restores, Release build, 22 database-independent backend tests, both NuGet vulnerability audits, resolved-version inspection, and `git diff --check` passed locally. Exact results and remaining CI gates are recorded in `docs/test-plan.md`.

Limits: the isolated PostgreSQL connection was not supplied locally, so the guarded database integration test did not run. Browser, Compose, PostgreSQL integration, migration listing, and full-history secret scanning require the branch workflow. No live Sentry event was sent, and this maintenance batch does not create a release tag.

Recovery: revert the batch through a pull request and regenerate all lockfiles with `scripts/Refresh-Dependencies.ps1`. Do not restore `sendDefaultPii` on Sentry 11; either retain the explicit restrictive `dataCollection` policy or revert the SDK major as part of the same change.

# 006: September dependency refresh

Date: 2026-09-26.

Problem: ten independent Dependabot pull requests split tightly coupled Angular and Microsoft packages, proposed TypeScript and Vitest majors outside the selected Angular toolchain's declared compatibility, and left lockfile regeneration as a manual per-project task.

Cause: Dependabot had no package-family grouping or TypeScript-major guard, Angular manifests used ranges that permitted patch-line drift, and the repository had no single command for regenerating both NuGet locks and the npm lock together.

Change: pin Angular runtime, compiler, CLI, and build packages to 22.1.7; keep TypeScript on supported 6.0.x; update jsdom to 30.1.1; align ASP.NET Core, EF Core, and `dotnet-ef` on 10.0.12; update Sentry.AspNetCore to 6.11.1 and coverlet.collector to 10.0.1; retain Npgsql 10.0.3. Add Angular and Microsoft/EF Dependabot groups, ignore TypeScript majors, and add `scripts/Refresh-Dependencies.ps1` to regenerate and clean-install the complete locked graph.

Rationale: package families with exact peers should be reviewed and released together. The refresh command treats manifests as the intentional input, replaces stale lock peer entries, and then proves the result with ordinary locked installation. Vitest remains 4.1.11 because Angular build 22.1.7 declares `^4.0.8`; accepting Vitest 5 requires moving the Angular build line to 22.2 or later.

Verification: frontend tests, production build, npm audit, both NuGet vulnerability audits, Compose configuration validation, lockfile inspection, and `git diff --check` passed. Exact results and the Docker limitation are recorded in `docs/test-plan.md`.

Limits: the local Docker engine did not start because Docker Desktop 4.89.0 failed on runtime-socket rename. Database-backed tests, browser tests, Docker/Demo smoke, backup/restore, and migration listing against PostgreSQL remain pending until a working local engine or green branch CI supplies evidence. No dependency pull request is superseded until the combined branch passes required CI and is merged.

Recovery: revert this batch through a pull request, run `scripts/Refresh-Dependencies.ps1`, and rerun all locked release gates. Do not hand-edit hashes in any lockfile or bypass peer validation in CI.

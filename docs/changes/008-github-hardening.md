# 008: GitHub merge and CodeQL hardening

Date: 2026-10-01.

Problem: the public repository still allowed squash and rebase merges, retained merged branches, and had no CodeQL analysis despite the documented merge-ancestry and security requirements.

Cause: the initial public-source milestone established branch protections, read-only Actions permissions, secret scanning, and dependency security, but deferred merge-method cleanup and CodeQL until after the dependency batches stabilized.

Change: retain merge commits, disable squash and rebase merges, and automatically delete merged branches. Enable CodeQL default setup for C# and JavaScript/TypeScript, then require the stable `Analyze (csharp)` and `Analyze (javascript-typescript)` checks on `main` and `develop` alongside the existing five CI checks.

Rationale: one merge strategy preserves feature and promotion ancestry. Default setup scans pushes and pull requests for the default and protected branches without adding a repository-owned workflow. Requiring the observed successful check names prevents changes from bypassing either language analysis.

Verification: GitHub API reads confirmed the final repository, Actions, CodeQL, and branch-protection settings. Initial CodeQL run 36829973670 passed both language jobs and reported zero open alerts. PR #38 then emitted and passed both required language contexts against protected `develop`, alongside all five existing CI checks. Exact run identifiers are recorded in `docs/test-plan.md`.

Limits: CodeQL is static analysis, not a replacement for dependency audits, secret scanning, runtime security tests, or review. The default query suite and weekly schedule remain in use; changes to languages, runner, or query policy require another evidence-backed settings review.

Recovery: if a required CodeQL check stops reporting, inspect default-setup status and the latest analysis before changing protection. Remove a requirement only when GitHub no longer emits that exact stable context and replace it with the verified successor; do not bypass both CodeQL checks to merge unrelated work.

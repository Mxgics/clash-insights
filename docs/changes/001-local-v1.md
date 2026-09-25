# 001 — Local v1 baseline

Cause: planning had not produced a runnable app; user authorized local implementation after choosing Angular/.NET and deferring hosting/auth to v2.

Fix: create the local SPA/API/database stack, source-separated demo observations, configurable collection, history/roster/war views, diagnostic status and versioned operations documentation. Use observed data and preserve gaps; keep secrets out of source and upstream requests out of the browser.

Verification completed 2026-09-08: 21 backend, 6 frontend and 4 browser/API checks pass; production build has no budget warnings; restored backup matched 2,311 persisted demo observations. See ../test-plan.md.

Follow-up fixes removed the shared 10,000-row truncation, persisted retry timing, handled nullable statistics/missing rosters, made history refresh with the summary, split dashboard components and corrected measured contrast failures. EF runtime/design packages are aligned and dependency locks are included. Empty generated tests were replaced with behaviour tests.

Limits: user layout acceptance and live Supercell setup remain pending. No live integration claim or public deployment. Existing baseline commit 35e69b9 was observed; this task created no commit, push or additional worktree.


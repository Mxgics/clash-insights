# 005: Public source release

Date: 2026-09-22.

Problem/cause: hosted-readiness changes were uncommitted, backend CI invoked project commands from a directory without a solution, and public documentation contained identifying local clan details and outdated setup instructions.

Change: preserve the existing implementation; fix CI project selection; pin official Actions releases to verified SHAs; add isolated Demo browser and full-history secret jobs; cover both NuGet project directories in Dependabot; add MIT licensing; anonymize local clan identifiers; extend secret/build exclusions; reconcile setup, project status and release procedures. Add a regression test proving Demo collection completes without database or upstream services.

Verification: execution results are recorded in the 2026-09-22 section of the test plan. Redacted historical evidence retains original observation timestamps and outcomes. No real upstream requests are required for this release.

Limits: a public source repository is not a hosted application. Auth0 provider flows, hosted networking, telemetry scrubbing and production restore/rollback remain staging gates.

Recovery: revert a defective release through a pull request and rerun the required checks. Do not force-push protected branches. Making a repository private again cannot recall copies already obtained while public; credentials exposed at any point require rotation and an explicit history-remediation decision.

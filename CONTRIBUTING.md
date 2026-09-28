# Contributing

Branch from `develop` and return normal changes through a pull request to `develop`. Promote releases from `develop` to `main` through a separate pull request.

Before opening a pull request, run the backend tests and the Angular test/build commands documented in `AGENTS.md`. Keep demo data clearly labelled, preserve observed timestamps and collection gaps, and update the relevant plan, decision, change, test, or runbook document.

For an intentional dependency batch, update the project manifests first and then run `./scripts/Refresh-Dependencies.ps1` from the repository root. It regenerates both NuGet lockfiles, rewrites the npm lockfile as one coordinated graph, and proves that graph with a clean `npm ci`. Review all three lockfile diffs and keep locked restores enabled in CI.

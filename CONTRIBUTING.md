# Contributing

Branch from `develop` and return normal changes through a pull request to `develop`. `main` is the default and release branch; promote release candidates from `develop` to `main` through a separate pull request.

Create annotated SemVer tags only from verified milestone commits on `main`, after the promotion and its required checks pass. Publish a matching GitHub Release with verification evidence and known limitations. Ordinary dependency or maintenance promotions are not releases; the first planned version is `v1.0.0` after all V1 acceptance gates pass. Never move or reuse a published version tag.

Before opening a pull request, run the backend tests and the Angular test/build commands documented in `AGENTS.md`. Keep demo data clearly labelled, preserve observed timestamps and collection gaps, and update the relevant plan, decision, change, test, or runbook document.

For an intentional dependency batch, update the project manifests first and then run `./scripts/Refresh-Dependencies.ps1` from the repository root. It regenerates both NuGet lockfiles, rewrites the npm lockfile as one coordinated graph, and proves that graph with a clean `npm ci`. Review all three lockfile diffs and keep locked restores enabled in CI.

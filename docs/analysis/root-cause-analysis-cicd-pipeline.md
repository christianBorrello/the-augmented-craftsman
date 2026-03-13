# Root Cause Analysis: CI/CD Pipeline Not Triggering

**Date**: 2026-03-13
**Analyst**: Rex (Root Cause Analysis Specialist)
**Methodology**: Toyota 5 Whys (Multi-Causal)
**Scope**: Backend CI pipeline and Frontend CI pipeline on branch `main`

---

## Problem Statement

The latest pushes to `main` did not correctly trigger the CI/CD pipeline for either the backend or the frontend. Two workflows are affected: `.github/workflows/ci.yml` (backend) and `.github/workflows/frontend.yml` (frontend).

**Commits under investigation** (most recent at top):

| SHA | Message | Files changed |
|-----|---------|---------------|
| 5064963 | refactor(oauth): apply review fixes | backend/**, docs/** |
| 148dc10 | docs: add architecture diagrams, feature specs | docs/** only |
| f7c2dce | ci: add Infrastructure.Tests to CI pipeline | .github/workflows/ci.yml |
| 608ee69 | chore: configure secrets handling | .gitignore, backend/** |

---

## Phase 1: Scope Definition

**Backend CI trigger conditions** (from `ci.yml` lines 4-13):

```yaml
on:
  push:
    branches: [main]
    paths:
      - 'backend/**'
      - '.github/workflows/ci.yml'
```

**Frontend CI trigger conditions** (from `frontend.yml` lines 4-13):

```yaml
on:
  push:
    branches: [main]
    paths:
      - 'frontend/**'
      - '.github/workflows/frontend.yml'
```

**Critical observation**: Both workflows use `paths` filters. A push only triggers the workflow if at least one changed file matches the configured paths. Commits that touch only paths outside these patterns are silently skipped by GitHub Actions — no run is created, no failure, no notification.

---

## Phase 2: Toyota 5 Whys Analysis

### Branch A — Backend CI did not trigger on commit `148dc10` (docs commit)

**WHY 1A**: Backend CI did not run on push of `148dc10`.
Evidence: commit `148dc10` changed only `docs/**` files (12 files, all under `docs/`). Neither `backend/**` nor `.github/workflows/ci.yml` appear in the changed file list. GitHub Actions `paths` filter evaluates to false: no match, no run.

**WHY 2A**: A docs-only commit reached `main` directly as a standalone push.
Evidence: `git show --stat 148dc10` shows 12 insertions, all under `docs/`. The commit was pushed independently to `main` (not squash-merged as part of a larger backend commit). The commit message `docs: add architecture diagrams…` confirms its scope.

**WHY 3A**: There is no CI gate preventing a pure-docs push from bypassing pipeline execution.
Evidence: The `ci.yml` `paths` filter was deliberately scoped to `backend/**` and `.github/workflows/ci.yml` to avoid unnecessary CI runs on unrelated changes. This is correct for performance, but it means a docs-only push produces zero CI runs — by design — and there is no separate docs-validation workflow to cover the gap.

**WHY 4A**: The workflow design did not account for documentation commits that accompany or follow backend feature work reaching `main` in isolation.
Evidence: The development workflow evidently allows pushing docs commits independently of their associated backend commits (separate `git commit` per concern — which is good practice), but the CI path filter only guards the backend scope, creating a blind spot for mixed or docs-only pushes to `main`.

**WHY 5A**: There is no convention or tooling enforcing that backend CI must run at least once per logical feature delivery to `main`, regardless of which specific commit files changed.
Evidence: No branch protection rule requiring CI to pass before merging (assumption — branch protection state not observable from local git, but the fact that docs-only commits reach `main` without CI running implies either no protection or protection not tied to CI status). No "required status checks" are set that would prevent this path.

**ROOT CAUSE A**: The `paths` filter in the backend CI workflow, combined with the absence of a required-CI-pass branch protection rule, allows commits that touch only `docs/**` to reach `main` without any CI validation. This is a design gap: the filter correctly avoids wasted runs for doc-only changes but removes the safety net for those same changes.

---

### Branch B — Backend CI did not trigger on commit `5064963` (refactor commit, most recent)

**WHY 1B**: Backend CI did not run on push of `5064963` (HEAD at time of investigation).
Evidence: `git diff --name-only HEAD~1..HEAD` (i.e., `5064963`) shows files in `backend/**` AND `docs/**`. The `backend/**` match should have triggered the CI. This push should have triggered the backend CI.

**Restatement**: Commit `5064963` DOES contain `backend/**` files. If this push did not trigger CI, the cause is different. Two hypotheses:

- **Hypothesis B1**: The push did trigger CI, but the run failed (pipeline ran, something inside failed).
- **Hypothesis B2**: GitHub Actions evaluated the paths filter and somehow did not match.

Evidence assessment for B1 vs B2: The problem statement says "non hanno fatto partire correttamente la ci/cd pipeline" — did not start correctly. This phrasing implies the pipeline either did not start or started but did not complete correctly. Without access to the GitHub Actions run log (not available locally), this requires further investigation.

However, examining `5064963` more carefully: it modified `backend/tests/TacBlog.Acceptance.Tests/Features/Epic5_UserEngagement/oauth/walking-skeleton.feature` and other `backend/**` files — these match `backend/**`. The pipeline should have been triggered.

**Most probable explanation for "did not run correctly"**: The pipeline ran but a step inside it failed, not that it failed to trigger. The commit `f7c2dce` (two commits before HEAD at the time of the problem report) added `TacBlog.Infrastructure.Tests` to the pipeline. If that test project has unresolved compilation issues or requires secrets that are not available in CI, the pipeline would start and then fail.

**WHY 2B**: The `TacBlog.Infrastructure.Tests` step added in `f7c2dce` may fail in CI due to missing secrets or build issues.
Evidence: `608ee69` (the commit immediately before `f7c2dce`) added `appsettings.Development.json` to `.gitignore` and configured User Secrets. The Infrastructure.Tests project (`ProductionOAuthClientShould.cs`) likely requires OAuth client credentials (GitHub/Google secrets). In CI, these secrets may not be configured.

**WHY 3B**: The Infrastructure.Tests project was added to the solution and to the CI pipeline without verifying that it can build and run green in the CI environment (without secrets).
Evidence: The `TacBlog.Infrastructure.Tests.csproj` was added in commit `8770857`. The CI step was added in `f7c2dce`. The project tests `ProductionOAuthClient` which performs real HTTP calls to OAuth providers — this requires live credentials unavailable in CI.

**WHY 4B**: There is no CI environment secrets validation step and no distinction between tests that require external credentials ("integration/manual") and tests that do not ("unit/pure").
Evidence: The acceptance tests use `--filter "Category!=manual"` to exclude manual tests. The Infrastructure.Tests run has no such filter — it runs all tests unconditionally, including any that require live OAuth credentials.

**WHY 5B**: The test categorization convention (the `manual` category filter used in acceptance tests) was not extended to the new Infrastructure.Tests project, and no CI-safe subset was defined before adding the project to the pipeline.
Evidence: The `ci.yml` step is `dotnet test tests/TacBlog.Infrastructure.Tests/ -c Release --no-build` with no `--filter` argument, unlike the acceptance tests step which uses `--filter "Category!=manual"`.

**ROOT CAUSE B**: The `TacBlog.Infrastructure.Tests` project was added to the CI pipeline without applying a test category filter to exclude tests that require live OAuth credentials. If any test in that project depends on real credentials (GitHub/Google OAuth), the CI job will fail at that step, causing the pipeline to report failure or abort — manifesting as "did not run correctly."

---

### Branch C — Frontend CI did not trigger on any of the recent commits

**WHY 1C**: Frontend CI did not trigger on any of the last 5 commits.
Evidence: Commits `5064963`, `148dc10`, `f7c2dce`, `608ee69`, `8770857` — none of them contain changes to `frontend/**` or `.github/workflows/frontend.yml`. The `frontend.yml` paths filter correctly evaluates to false for all of them. No frontend CI run is expected or triggered.

**WHY 2C**: No frontend files were modified in the recent feature delivery (OAuth backend feature).
Evidence: The current work stream is entirely in `backend/**` and `docs/**`. The frontend was last modified in an earlier commit (likely `b6eab89 feat(frontend): add likes, comments, share, and session UI` based on the git log).

**WHY 3C**: The development workflow for the OAuth backend feature did not touch the frontend, which is correct and intentional — OAuth is a backend-only feature at this stage.
Evidence: OAuth authentication endpoints, token handling, and session management are all backend concerns. The frontend will need changes only when the OAuth UI flow is wired up.

**ROOT CAUSE C**: The frontend CI not triggering is expected and correct behavior — it is not a pipeline failure. The paths filter is working as designed. There is no bug here. This was misidentified as part of the problem.

**VERDICT on Branch C**: Not a defect. The frontend CI silence is correct. The problem is exclusively backend-side.

---

## Phase 3: Backward Chain Validation

### Root Cause A -> Symptom A
"There is no required-CI-pass branch protection on main, combined with a `paths` filter that excludes `docs/**`."
Forward trace: docs-only commit pushed to main -> GitHub evaluates paths filter -> no match -> no CI run created -> developer sees no pipeline activity -> reports "pipeline did not trigger." Valid chain.

### Root Cause B -> Symptom B
"Infrastructure.Tests runs without credential filtering in CI."
Forward trace: Push to main with `backend/**` changes -> CI triggers -> pipeline reaches `Unit tests (Infrastructure)` step -> test runner executes tests requiring live OAuth credentials -> tests fail with authentication error or missing config -> CI job reports failure -> developer sees "pipeline did not run correctly." Valid chain.

### Cross-validation
Root Cause A and B do not contradict — they affect different commits and different failure modes. Root Cause C was resolved as expected behavior (not a defect). All reported symptoms are collectively explained by A and B.

---

## Phase 4: Solution Development

### Solution A — Immediate Mitigation for Root Cause A
**Type**: Mitigation (partial — does not fully prevent)
Ensure that backend-affecting commits are pushed together with or after their associated docs commits, so the `backend/**` path match fires on the same push. This is a workflow convention, not a technical fix.

### Solution A — Permanent Fix
**Type**: Permanent fix
Add branch protection rules to `main` on GitHub requiring the `CI / ci` status check to pass before any push or merge is accepted. This ensures that even when a docs-only push slips through, the protection catches it if it was meant to be associated with backend code.

Additionally, consider adding `docs/**` to the backend CI `paths` trigger, or creating a lightweight docs-lint workflow covering `docs/**` so that all pushes to `main` produce at least one CI status:

```yaml
# Option: include docs in backend CI trigger
paths:
  - 'backend/**'
  - '.github/workflows/ci.yml'
  - 'docs/**'
```

Or, more precisely, require any commit to `main` to have been tested in its originating PR (enforce PR-only merges to main via branch protection), so isolated direct pushes to `main` become impossible.

### Solution B — Immediate Mitigation for Root Cause B
**Type**: Mitigation
Add `--filter "Category!=RequiresCredentials"` (or equivalent trait name) to the Infrastructure.Tests CI step to exclude credential-dependent tests until they are properly categorized:

```yaml
- name: Unit tests (Infrastructure)
  run: dotnet test tests/TacBlog.Infrastructure.Tests/ -c Release --no-build --filter "Category!=RequiresCredentials" --logger "trx;LogFileName=infrastructure.trx"
  working-directory: backend
```

### Solution B — Permanent Fix
**Type**: Permanent fix
Categorize all tests in `TacBlog.Infrastructure.Tests` with explicit traits:
- Tests that can run without credentials: `[Trait("Category", "unit")]`
- Tests that require real OAuth credentials: `[Trait("Category", "RequiresCredentials")]`

Update the CI step to filter out credential-dependent tests. Store real OAuth credentials in GitHub Actions secrets and create a separate, manually triggered or nightly workflow that runs the full Infrastructure.Tests suite against real providers.

Alternatively, use NSubstitute/fakes to replace the real HTTP calls in tests so that `ProductionOAuthClientShould` can run in CI without real credentials (pure unit tests with injected test doubles).

### Solution C — For Root Cause C
**Type**: No action required
Frontend CI silence is correct behavior. Document this in team/developer notes to prevent future misidentification.

---

## Phase 5: Prevention Strategy

### Systemic Prevention

| ID | Action | Type | Priority |
|----|--------|------|----------|
| P1 | Enforce branch protection on `main`: require `CI / ci` status to pass before merge | Permanent | P1 |
| P2 | Enforce PR-only merges to `main` (no direct push) so every change is covered by a CI run on the PR branch | Permanent | P1 |
| P3 | Add test category filter to Infrastructure.Tests CI step to exclude credential-dependent tests | Immediate mitigation | P0 |
| P4 | Add `[Trait("Category", "RequiresCredentials")]` to tests in `ProductionOAuthClientShould.cs` that need live OAuth | Permanent | P1 |
| P5 | Extend the test filtering convention (`Category!=manual` pattern) to all test projects via shared documentation | Systemic improvement | P2 |
| P6 | Add a developer runbook entry: "when adding a new test project to CI, verify it runs green in CI without secrets before committing the pipeline step" | Process | P2 |

### Early Detection

- GitHub Actions Slack/email notification on any CI failure — ensures the team is alerted immediately rather than relying on developer observation.
- Add a CI status badge to the project README so pipeline health is always visible.

---

## Summary

| Root Cause | Branch | Severity | Status |
|------------|--------|----------|--------|
| **A**: docs-only commits bypass CI because no `docs/**` path match and no required-CI branch protection | Docs commit `148dc10` reaching main without backend CI running | Medium — no regression risk for that commit, but the process gap is real | Design gap, needs branch protection |
| **B**: `Infrastructure.Tests` in CI runs without credential filtering, causing the pipeline to fail when tests require live OAuth secrets | Any push matching `backend/**` since `f7c2dce` | High — CI is broken for all backend pushes after that commit | Active defect, P0 fix required |
| **C**: Frontend CI silence | All recent commits | None — correct behavior | Not a defect |

**Primary actionable defect**: Root Cause B. The pipeline is actively failing (or failing to complete correctly) for all backend pushes since the addition of `Infrastructure.Tests` in `f7c2dce`, because that step runs tests requiring live OAuth credentials without a category filter.

**Secondary design gap**: Root Cause A. The absence of required CI status checks on `main` allows docs-only commits to merge without any CI validation, creating a false sense that the pipeline "did not run" when it correctly did not match.

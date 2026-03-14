# Root Cause Analysis: Backend CI Pipeline Failure — Koyeb Deploy Step

**Date**: 2026-03-14
**Analyst**: Rex (Root Cause Analysis Specialist)
**Methodology**: Toyota 5 Whys, Multi-Causal
**Scope**: GitHub Actions `CI` workflow, `deploy` job — commit `e0de04a` on branch `main`
**Run ID**: 23075999383

---

## Problem Statement

The backend CI pipeline failed on commit `e0de04a` (`ci: replace sleep 90 with explicit Koyeb deploy + health polling`). All test and build steps in the `ci` job passed (68 domain tests, 102 application tests, 13 infrastructure tests, 28 integration tests, 156 acceptance tests — all green). The failure occurred in the `deploy` job at the `Force Koyeb to pull latest image` step with exit code 127 (`koyeb: command not found`). The subsequent `smoke-test` job was skipped entirely.

**Observed error:**
```
/home/runner/work/_temp/d02e7bd9-edee-4b58-8fbc-04c82df97a65.sh: line 1: koyeb: command not found
##[error]Process completed with exit code 127.
```

**Jobs affected:**
- `ci`: SUCCESS (all tests passed)
- `deploy`: FAILURE at step `Force Koyeb to pull latest image`
- `smoke-test`: SKIPPED (depends on `deploy`)

**Scope boundary:** This analysis covers the `deploy` job failure only. The `ci` job (build, format, tests) passed cleanly. Secondary observations (code warnings, Node.js deprecation notices) are noted but not investigated as separate branches unless causally linked.

---

## Phase 1: Evidence Collection

### Source Evidence

| Evidence | Detail |
|----------|--------|
| `gh run view 23075999383` | `deploy` job status: FAILURE. `ci` job status: SUCCESS. `smoke-test` skipped. |
| `gh run view 23075999383 --log-failed` | Error: `koyeb: command not found` at timestamp `2026-03-14T00:29:12.9360900Z` |
| `gh run view 23075999383 --log` (Install Koyeb CLI step) | Install script ran successfully. Last line: `Koyeb CLI was installed successfully to /home/runner/.koyeb/bin/koyeb`. Script printed: `Manually add the directory to your $HOME/.bash_profile (or similar) — export PATH="/home/runner/.koyeb/bin:$PATH"`. |
| `git show e0de04a --stat` | Only `.github/workflows/ci.yml` modified. Commit message explicitly states this replaces `sleep 90` with Koyeb CLI-based polling. |
| `.github/workflows/ci.yml` (current) | `Install Koyeb CLI` step: `curl -fsSL https://raw.githubusercontent.com/koyeb/koyeb-cli/master/install.sh | sh`. Next step uses `koyeb` as a bare command with no path prefix. No `$PATH` update between steps. |

### Key Timeline

```
00:29:12.087Z — Install Koyeb CLI: curl | sh executes
00:29:12.877Z — CLI installed to /home/runner/.koyeb/bin/koyeb
00:29:12.887Z — Install script prints: "Manually add the directory to your $PATH"
00:29:12.920Z — Force Koyeb step begins (new shell process)
00:29:12.936Z — /home/runner/work/_temp/....sh: line 1: koyeb: command not found
00:29:12.938Z — exit code 127
```

### Critical observation

The install script explicitly warns that the `PATH` modification is NOT automatic:
> `Manually add the directory to your $HOME/.bash_profile (or similar)`

Each GitHub Actions step runs in a new shell process. Changes to the runner's `$HOME/.bash_profile` by a prior step are not sourced into subsequent steps. The `koyeb` binary is in `/home/runner/.koyeb/bin/` but this directory is not in `$PATH` when the next step's shell is spawned.

---

## Phase 2: Toyota 5 Whys Analysis

### BRANCH A — `koyeb: command not found` (exit code 127)

**WHY 1A:** The `Force Koyeb to pull latest image` step cannot find the `koyeb` binary.
*Evidence: Error log — `/home/runner/work/_temp/....sh: line 1: koyeb: command not found`. Exit code 127 is the POSIX code for "command not found in PATH".*

**WHY 2A:** `/home/runner/.koyeb/bin` is not in `$PATH` when the step's shell is launched.
*Evidence: Install step log — `Koyeb CLI was installed successfully to /home/runner/.koyeb/bin/koyeb`. The install script explicitly states: "Manually add the directory to your $HOME/.bash_profile". GitHub Actions does not source `~/.bash_profile` between steps; each `run:` block gets a fresh `bash -e` subprocess.*

**WHY 3A:** The workflow does not update `$PATH` after installing the Koyeb CLI, and does not invoke it via its absolute path.
*Evidence: `.github/workflows/ci.yml` — the `Install Koyeb CLI` step has no `echo "/home/runner/.koyeb/bin" >> $GITHUB_PATH` directive. The following step calls `koyeb services update ...` using the bare name `koyeb`, not `/home/runner/.koyeb/bin/koyeb`.*

**WHY 4A:** The commit introducing this behavior (`e0de04a`) was written without accounting for how GitHub Actions handles inter-step PATH propagation.
*Evidence: Commit `e0de04a` is authored as `ci: replace sleep 90 with explicit Koyeb deploy + health polling`. The install pattern `curl ... | sh` installs to a user-local path not in the default runner PATH. GitHub Actions requires explicit `$GITHUB_PATH` appends (or `$PATH` exports using the `>>` append mechanism) for binaries installed mid-workflow to be available in subsequent steps.*

**WHY 5A:** There is no CI-environment documentation or pattern established in this project for installing third-party CLIs mid-workflow, and the GitHub Actions `$GITHUB_PATH` mechanism was not applied.
*Evidence: No other workflow step in `ci.yml` or `frontend.yml` installs a CLI tool mid-job. The `$GITHUB_PATH` file mechanism (the correct way to persist PATH changes across steps in GitHub Actions) was not used. The install script's own warning ("Manually add to $HOME/.bash_profile") was not treated as an action item in the workflow.*

**ROOT CAUSE A:** The Koyeb CLI is installed to a non-default path (`/home/runner/.koyeb/bin/`) but the directory is never added to `$GITHUB_PATH`. Each GitHub Actions step runs in an isolated shell that does not inherit changes made to shell profiles by prior steps. The `koyeb` binary is unreachable by name in the step that needs it.

---

### BRANCH B — Secondary observation: `Newtonsoft.Json` vulnerability warning in MSBuild

**WHY 1B:** MSBuild emits a `NU1903` high-severity vulnerability warning during restore and format steps for `TacBlog.Infrastructure.csproj`.
*Evidence: CI log — `warning NU1903: Package 'Newtonsoft.Json' 12.0.3 has a known high severity vulnerability, https://github.com/advisories/GHSA-5crp-9r3c-p9vr`. Appears in both `Restore dependencies` and `Check formatting` steps.*

**WHY 2B:** `Newtonsoft.Json` 12.0.3 is a transitive dependency of one of the packages in `TacBlog.Infrastructure`.
*Evidence: The warning originates from `TacBlog.Infrastructure.csproj`. This package is not directly referenced (no `PackageReference` for `Newtonsoft.Json` is expected in a .NET 10 project using system-text-json). It is a transitive dependency — most likely pulled in by `Imagekit` v4 SDK or another infrastructure dependency.*

**WHY 3B:** The transitive dependency version has not been pinned or upgraded to a non-vulnerable version.
*Evidence: No `<PackageVersion>` or `<PackageReference>` override for `Newtonsoft.Json` appears in `TacBlog.Infrastructure.csproj`. The vulnerability is tracked as GHSA-5crp-9r3c-p9vr (TypeNameHandling deserialization — applicable only when used with untrusted input).*

**WHY 4B:** This is a transitive dependency warning that does not block the build or tests (warning, not error). The current project does not directly invoke `Newtonsoft.Json` deserialization with untrusted input in the image storage adapter.

**WHY 5B:** No `TreatWarningsAsErrors` or `NuGetAuditMode` policy is configured to enforce vulnerability triage. The vulnerability warning is visible but produces no CI gate failure.

**ROOT CAUSE B:** `Newtonsoft.Json` 12.0.3 is a vulnerable transitive dependency. It does not cause the current CI failure (the build and all tests pass) but represents a known security weakness in the dependency tree that should be triaged.

Note: This branch does NOT contribute to the pipeline failure. It is a secondary observation. The only failing job is `deploy`, caused entirely by Branch A.

---

### BRANCH C — `smoke-test` skipped

**WHY 1C:** The `smoke-test` job did not run.
*Evidence: `gh run view 23075999383` shows `smoke-test` status as `-` (skipped/not started).*

**WHY 2C:** `smoke-test` has `needs: deploy`. GitHub Actions skips a job if any of its `needs` dependencies fail.
*Evidence: `ci.yml` line 123: `needs: deploy`. This is expected GitHub Actions behavior — a downstream job is skipped when its upstream dependency fails.*

**WHY 3C:** This is correct pipeline design: running smoke tests after a failed deploy would test a stale deployment, producing misleading results.

**ROOT CAUSE C:** Not a separate root cause. Smoke-test skip is a correct, expected consequence of the `deploy` job failure. No independent defect.

---

## Phase 3: Cross-Validation

### Backward chain validation

| Root Cause | Forward trace | Validates? |
|------------|--------------|------------|
| A: `/home/runner/.koyeb/bin` not in `$PATH` | CLI installed but not in PATH -> next step shell spawned without PATH update -> `koyeb` not found -> exit 127 -> job fails -> smoke-test skipped | Yes |
| B: Newtonsoft.Json 12.0.3 vulnerability | Warning emitted during restore/format -> build proceeds -> tests pass -> no CI gate failure | Not a cause of the failing job — consistent |
| C: smoke-test skip | deploy job fails -> needs-dependency fails -> smoke-test is skipped by GitHub Actions | Expected, not independent |

### Completeness check

All observable symptoms are explained:
- `deploy` job fails at `Force Koyeb to pull latest image`: explained by Root Cause A entirely.
- `smoke-test` skipped: downstream consequence of Root Cause A, not independent.
- `ci` job succeeds: unaffected by Root Cause A (different job, no PATH issue).
- Vulnerability warning: Root Cause B, independent, non-blocking.

No contradictions between branches.

---

## Phase 4: Solution Development

### Solution A — Permanent Fix for Root Cause A

**Problem:** Koyeb CLI installed to non-default path, directory not added to `$GITHUB_PATH`.

**Fix:** Add `echo "/home/runner/.koyeb/bin" >> $GITHUB_PATH` as the last line of the `Install Koyeb CLI` step. This instructs the GitHub Actions runner to prepend the directory to `$PATH` for all subsequent steps in the job.

```yaml
- name: Install Koyeb CLI
  run: |
    curl -fsSL https://raw.githubusercontent.com/koyeb/koyeb-cli/master/install.sh | sh
    echo "/home/runner/.koyeb/bin" >> $GITHUB_PATH
```

No other changes required. The subsequent steps calling `koyeb services update ...` and `koyeb services describe ...` will resolve correctly once the directory is in `$PATH`.

**Alternative:** Call the binary by absolute path in both steps:

```yaml
- name: Force Koyeb to pull latest image
  run: |
    /home/runner/.koyeb/bin/koyeb services update ${{ vars.KOYEB_SERVICE }} \
      --app ${{ vars.KOYEB_APP }} \
      --docker "ghcr.io/${{ github.repository_owner }}/tacblog-api:latest"
```

This works but is brittle — if the install path ever changes, both steps break. The `$GITHUB_PATH` approach is preferred.

**Priority:** P0 — Immediate fix. The deploy stage is completely broken for every push to `main` since this commit.

---

### Solution B — Triage for Newtonsoft.Json vulnerability

**Problem:** Transitive dependency `Newtonsoft.Json` 12.0.3 has a known high-severity vulnerability (GHSA-5crp-9r3c-p9vr: TypeNameHandling deserialization with untrusted input).

**Triage:** Determine which direct dependency pulls in `Newtonsoft.Json`:

```bash
cd backend
dotnet list src/TacBlog.Infrastructure/TacBlog.Infrastructure.csproj package --include-transitive | grep -i newtonsoft
```

If the parent is the `Imagekit` SDK (v4), check whether a newer version of that SDK references a patched `Newtonsoft.Json`. If not, pin a safe version directly:

```xml
<!-- In TacBlog.Infrastructure.csproj — override transitive version -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

`Newtonsoft.Json` 13.0.1+ addresses this vulnerability.

**Priority:** P1 — Not blocking CI, but a known CVE in the deployed binary warrants triage this sprint.

---

### Early Detection — Prevent PATH issues in future CLI installs

Add a comment convention in `ci.yml` for any future CLI tool installation:

```yaml
# Convention: after installing any CLI to a non-default path,
# always append the bin directory to $GITHUB_PATH so subsequent
# steps can call the binary by name.
# Example:
#   echo "<install-dir>/bin" >> $GITHUB_PATH
```

This documents the pattern in the file where it will be seen when the pattern is next needed.

---

## Phase 5: Prevention Strategy

### Systemic factors

| Factor | Prevention |
|--------|-----------|
| GitHub Actions PATH isolation between steps | Document the `$GITHUB_PATH` mechanism as the project's standard for mid-job CLI installation. Add to any developer/CI runbook. |
| Install scripts that modify shell profiles are not automatic in CI | Treat any install script that says "add to ~/.bash_profile" as requiring an explicit `$GITHUB_PATH` append — never assume the profile is sourced. |
| No CI test for the deploy stage itself | The `deploy` job was introduced without a dry-run or local test. A `--help` call in the install step would have caught the PATH issue immediately: `koyeb --help > /dev/null`. |
| Transitive dependency vulnerabilities accumulate silently | Consider enabling `<NuGetAudit>true</NuGetAudit>` with `<NuGetAuditLevel>high</NuGetAuditLevel>` in `Directory.Build.props` to gate on high-severity CVEs during restore. |

### Recommended action items (prioritized)

| Priority | Action | Type |
|----------|--------|------|
| P0 | Add `echo "/home/runner/.koyeb/bin" >> $GITHUB_PATH` to the `Install Koyeb CLI` step | Permanent fix |
| P0 | Trigger `workflow_dispatch` on the fixed `ci.yml` to restore the deploy pipeline | Immediate mitigation |
| P1 | Identify parent dependency pulling `Newtonsoft.Json` 12.0.3; upgrade or pin to 13.0.3+ | Security fix |
| P2 | Add a smoke `koyeb --help` or `koyeb version` call immediately after install to fail fast on PATH issues | Early detection |
| P3 | Document `$GITHUB_PATH` pattern in project CI runbook for future CLI installations | Process improvement |

---

## Summary

The backend CI pipeline (`ci` job) passed completely — all 367 tests across 5 suites are green. The failure is isolated to the `deploy` job and has a single root cause.

**ROOT CAUSE A (sole cause of pipeline failure):** The Koyeb CLI install script places the binary in `/home/runner/.koyeb/bin/` and explicitly requires the user to add this directory to `$PATH` manually. The workflow does not append this path to `$GITHUB_PATH`, so the directory is absent from `$PATH` when the next step's shell is created. The `koyeb` command is therefore not found (exit code 127), and the deploy job fails before any Koyeb API call is made.

**ROOT CAUSE B (secondary, non-blocking):** `Newtonsoft.Json` 12.0.3 is a vulnerable transitive dependency in `TacBlog.Infrastructure`. It does not affect CI success but should be triaged.

**Fix:** One line in `ci.yml`:

```yaml
- name: Install Koyeb CLI
  run: |
    curl -fsSL https://raw.githubusercontent.com/koyeb/koyeb-cli/master/install.sh | sh
    echo "/home/runner/.koyeb/bin" >> $GITHUB_PATH
```

---

## Resolution

**Status:** RESOLVED

**Fix applied:** Commit `38b0a23` (`fix(ci): register Koyeb CLI install path in $GITHUB_PATH`) — 2026-03-14 01:34 CET.

The P0 permanent fix was applied immediately after this analysis: `echo "/home/runner/.koyeb/bin" >> $GITHUB_PATH` was added as the final instruction in the `Install Koyeb CLI` step. The current `.github/workflows/ci.yml` reflects this fix. All subsequent pushes to `main` that modify `backend/**` will execute the deploy job with the Koyeb CLI resolvable by name.

**Root Cause B (Newtonsoft.Json vulnerability):** Remains open. Requires identification of the parent dependency and version pinning or upgrade. Tracked as P1.

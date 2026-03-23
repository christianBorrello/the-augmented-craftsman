# Branching Strategy: Author Mode

**Feature**: author-mode
**Wave**: PLATFORM (DESIGN — infrastructure readiness)
**Date**: 2026-03-14
**Architect**: Apex (platform-architect)

---

## Strategy: GitHub Flow

**Decision PD-03**: GitHub Flow — feature branches from main, PR-triggered pipelines, merge to main after review.

Selected for: solo developer, continuous delivery, no need for parallel release branches, matches existing workflow triggers.

---

## Branch Model

```
main  ←───────────────────────────────────────────────────
  │          ↑ merge PR           ↑ merge PR
  │   feature/author-mode-*   feature/author-mode-*
  └──→ feature/author-mode-walking-skeleton
        feature/author-mode-oauth
        feature/author-mode-post-crud
        feature/author-mode-archive-restore
        feature/author-mode-edit-controls
        feature/author-mode-cover-image
```

---

## Branch Naming Convention

```
feature/author-mode-<short-description>

Examples:
  feature/author-mode-walking-skeleton
  feature/author-mode-admin-oauth
  feature/author-mode-post-crud
  feature/author-mode-archive-restore
  feature/author-mode-edit-controls
  feature/author-mode-cover-image
  fix/author-mode-<issue>
```

All branches are short-lived (target: merged within 1-3 days). No long-lived feature branches.

---

## Pipeline Triggers (Confirmed Against Existing Workflows)

### PR Opened / Updated

| Workflow | Trigger | Jobs |
|----------|---------|------|
| `ci.yml` | `pull_request` on branches matching `backend/**` path | `ci` (build + test + migration check + secrets check) |
| `frontend.yml` | `pull_request` on branches matching `frontend/**` path | `build-and-deploy` (prerender guard + type check + build; NO deploy step) |

### Merge to Main

| Workflow | Trigger | Jobs |
|----------|---------|------|
| `ci.yml` | `push` to `main` (backend/**) | `ci` → `deploy` → `smoke-test` |
| `frontend.yml` | `push` to `main` (frontend/**) | `build-and-deploy` (with deploy step) |

The monorepo path filters (`paths: ['backend/**']`, `paths: ['frontend/**']`) ensure that a backend-only PR does not trigger the frontend workflow and vice versa. A PR touching both (e.g., adding a new env var reference) triggers both workflows independently.

---

## PR Requirements

### Required Gates Before Merge

All configured as GitHub branch protection rules on `main`:

1. **Status check: `ci` job** — must pass for PRs touching `backend/**`
2. **Status check: `build-and-deploy` job** — must pass for PRs touching `frontend/**`
3. **No force push** — main is protected
4. **Linear history** — squash or rebase merge preferred (keeps `main` history readable)

### PR Description Template

For author-mode PRs, include:

```markdown
## What
Brief description of the change.

## Test
- [ ] All existing tests pass
- [ ] New tests written for new behavior
- [ ] Acceptance test scenario exists (or linked issue)

## Infrastructure
- [ ] No new env vars (OR: new env vars added to secrets checklist)
- [ ] No DB migration (OR: migration generated, tested locally, applied to prod)
- [ ] Admin pages have prerender=false (OR: not applicable)
```

---

## Author Mode Development Sequence

Recommended branch sequence to minimize integration conflicts:

| Order | Branch | Backend | Frontend | Deployment Risk |
|-------|--------|---------|----------|----------------|
| 1 | `feature/author-mode-walking-skeleton` | Admin OAuth endpoints, EF migration | `output: hybrid`, Upstash Sessions config, `/admin/login`, `/admin/callback`, middleware stub | HIGH — OAuth + hybrid mode transition |
| 2 | `feature/author-mode-post-crud` | No change | `/admin/posts/index`, `/admin/posts/new`, Actions (create/update) | MEDIUM |
| 3 | `feature/author-mode-archive-restore` | `PostStatus.Archived`, `PreviousStatus`, `ArchivePost`, `RestorePost` use cases + endpoints | Actions (archive/restore) | MEDIUM — DB migration |
| 4 | `feature/author-mode-edit-controls` | No change | `EditControls` Server Island | LOW — read-only Redis lookup |
| 5 | `feature/author-mode-cover-image` | `CoverImageUrl` in `CreatePostRequest`/`EditPostRequest` | `uploadCoverImage` Action, image UI | LOW |

**Branch 1 is the highest-risk item** (OAuth + hybrid mode simultaneously). Validate the walking skeleton end-to-end before starting branch 2.

---

## Hotfix Procedure

For urgent fixes to main while a feature branch is in progress:

```
main ──────── hotfix/fix-description ──→ PR → merge to main
         │
         └── rebase feature/author-mode-* onto updated main
```

1. Branch `hotfix/fix-description` from main.
2. Fix, commit, PR, merge.
3. Rebase the active feature branch: `git rebase origin/main`.

---

## Release Process

Author Mode has no separate release tag — it ships as a continuous delivery of `main`. When all feature branches are merged and smoke tests pass, the feature is live in production.

Post-merge verification: run the KPI-1 check (log in via OAuth as admin) to confirm the feature is operational in production before announcing.

# Branching Strategy -- The Augmented Craftsman v1

**Model**: Trunk-Based Development
**Context**: Solo developer, personal blog, CI gates on every commit

---

## 1. Branch Model

```
main (trunk) ─────────────────────────────────────────────────>
  \              \              \
   feat/create-  feat/publish-  feat/tag-
   post          post           management
   (< 1 day)     (< 1 day)     (< 1 day)
```

### Rules

| Rule | Detail |
|------|--------|
| Single long-lived branch | `main` is the only permanent branch |
| Short-lived feature branches | Max 1 day before merging to `main` |
| No release branches | `main` is always deployable |
| No hotfix branches | Fix on a feature branch, merge to `main` |
| No develop branch | No GitFlow staging. `main` is trunk. |
| Direct commits to main | Allowed for trivial changes (typos, config) |

---

## 2. Branch Naming Convention

```
feat/<feature-name>     -- New feature (feat/create-post, feat/upload-image)
fix/<bug-description>   -- Bug fix (fix/slug-special-chars, fix/jwt-expiry)
chore/<task>            -- Non-feature work (chore/update-deps, chore/ci-config)
docs/<topic>            -- Documentation only (docs/api-contracts)
```

**Rules:**
- Lowercase, hyphen-separated
- Short and descriptive (max 50 characters)
- No ticket numbers (no issue tracker in v1)

---

## 3. Commit Message Convention

Format: `<type>: <description>`

```
feat: add create post endpoint with slug generation
fix: reject empty title with 400 status code
test: add acceptance test for post creation
refactor: extract slug generation into value object
chore: update EF Core to 10.0.1
docs: add API contract for tag endpoints
```

### Types

| Type | Usage |
|------|-------|
| `feat` | New feature or endpoint |
| `fix` | Bug fix |
| `test` | Adding or modifying tests only |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `chore` | Build, CI, dependency updates |
| `docs` | Documentation changes |

### Rules

- Imperative mood: "add" not "added" or "adds"
- Lowercase after the type prefix
- No period at the end
- First line max 72 characters
- Body optional, separated by blank line

---

## 4. CI Trigger Rules

| Event | Branches | CI Stages |
|-------|----------|-----------|
| Push to `main` | `main` | Full pipeline: lint, test (unit + integration + acceptance), build Docker, deploy |
| Push to feature branch | `feat/*`, `fix/*`, `chore/*` | Validation pipeline: lint, test (unit + integration + acceptance), build Docker (no push) |
| Pull request to `main` | Any -> `main` | Same as feature branch push |

### Pipeline Behavior by Branch

```
Feature branch push:
  lint --> unit tests --> integration tests --> acceptance tests --> docker build (verify only)

  Status: reported back to GitHub (required check)
  Deploy: NO

Main branch push:
  lint --> unit tests --> integration tests --> acceptance tests --> docker build --> push to registry --> deploy to Fly.io

  Status: reported back to GitHub
  Deploy: YES (automatic)
```

---

## 5. Merge Strategy

| Setting | Value | Rationale |
|---------|-------|-----------|
| Merge method | Squash and merge | Clean linear history on main, feature branch commits collapsed |
| Delete branch after merge | Yes (automatic) | No stale branches |
| Branch protection on main | Minimal | Solo developer, CI must pass before merge |

### Branch Protection Rules (GitHub)

| Rule | Enabled | Rationale |
|------|---------|-----------|
| Require status checks to pass | Yes | CI must be green before merging |
| Required status checks | `ci` (the workflow job name) | The full test pipeline |
| Require branches to be up to date | No | Solo developer, no merge conflicts |
| Require pull request reviews | No | Solo developer, no reviewers |
| Require signed commits | No | Not needed for personal project |
| Allow force pushes | No | Prevent history destruction |
| Allow deletions | No | Prevent accidental main deletion |

---

## 6. Workflow Examples

### Feature Development (Outside-In TDD)

```bash
# Start feature
git checkout -b feat/create-post

# Write failing acceptance test, commit
git add tests/TacBlog.Acceptance.Tests/Features/CreateAndRetrievePostShould.cs
git commit -m "test: add acceptance test for create and retrieve post"

# Implement domain (inner loop TDD)
git add src/TacBlog.Domain/ tests/TacBlog.Domain.Tests/
git commit -m "feat: add Title and Slug value objects"

# Implement application layer
git add src/TacBlog.Application/ tests/TacBlog.Application.Tests/
git commit -m "feat: add CreatePost handler with slug generation"

# Implement infrastructure and API
git add src/TacBlog.Infrastructure/ src/TacBlog.Api/
git commit -m "feat: wire CreatePost endpoint with EF Core repository"

# Push and merge (CI runs automatically)
git push -u origin feat/create-post
# Squash merge via GitHub (or CLI: gh pr create && gh pr merge --squash)

# Clean up
git checkout main
git pull
git branch -d feat/create-post
```

### Trivial Fix (Direct to Main)

```bash
# Small config change
git checkout main
git add src/TacBlog.Api/Program.cs
git commit -m "chore: update CORS origin for production domain"
git push
# CI runs, deploys automatically
```

---

## 7. Versioning

No semantic versioning in v1. The blog is a single continuously deployed product. If versioning becomes needed (API consumers beyond Astro), adopt CalVer: `YYYY.MM.DD`.

---

## 8. Release Process

There is no release process. Every green merge to `main` deploys to production automatically.

```
Commit to main --> CI green --> Docker build --> Push to Registry --> Fly.io deploys --> Done
```

No manual gates. No staging environment. No approval workflows. The CI pipeline IS the quality gate. If tests pass, it ships.

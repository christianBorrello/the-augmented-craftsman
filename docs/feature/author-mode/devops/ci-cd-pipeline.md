# CI/CD Pipeline: Author Mode

**Feature**: author-mode
**Wave**: PLATFORM (DESIGN — infrastructure readiness)
**Date**: 2026-03-14
**Architect**: Apex (platform-architect)

---

## Design Principle: Extend, Do Not Replace

Two workflows already exist: `ci.yml` (backend) and `frontend.yml` (frontend). Author Mode adds jobs and steps to these existing workflows. No new workflow files are created.

Existing infrastructure reused: `.github/workflows/ci.yml` | `.github/workflows/frontend.yml`

---

## 1. Backend Pipeline — `ci.yml` Changes

### Existing Jobs (unchanged)

```
ci → deploy → smoke-test
```

### Changes Required

**Job `ci` — add steps after existing test steps:**

1. **Migration validation** — verify migration file exists and compiles with the build.
2. **Env var presence check** — verify required secrets are set before allowing deploy.

**Job `smoke-test` — add admin auth probe:**

3. **Admin auth endpoint probe** — verify the new `/api/auth/admin/verify-token` endpoint is reachable (returns 400/422 for an invalid token, not 404 or 500).

### Complete Additions to `ci.yml`

```yaml
# Add to the 'ci' job, after 'Acceptance tests' step:

      - name: Validate EF migration files
        run: |
          # Verify the author-mode migration exists in the compiled output
          MIGRATION="AddArchivedStatusAndPreviousStatus"
          if ! find . -name "*.cs" -path "*/Migrations/*" | xargs grep -l "$MIGRATION" 2>/dev/null | grep -q .; then
            echo "ERROR: Migration '$MIGRATION' not found. Run: dotnet ef migrations add $MIGRATION"
            exit 1
          fi
          echo "Migration $MIGRATION found."
        working-directory: backend

      - name: Check required secrets are configured
        env:
          ADMIN_EMAIL: ${{ secrets.ADMIN_EMAIL }}
          ADMIN_JWT_SECRET: ${{ secrets.ADMIN_JWT_SECRET }}
          UPSTASH_REDIS_REST_URL: ${{ secrets.UPSTASH_REDIS_REST_URL }}
          UPSTASH_REDIS_REST_TOKEN: ${{ secrets.UPSTASH_REDIS_REST_TOKEN }}
        run: |
          MISSING=()
          [ -z "$ADMIN_EMAIL" ]             && MISSING+=("ADMIN_EMAIL")
          [ -z "$ADMIN_JWT_SECRET" ]        && MISSING+=("ADMIN_JWT_SECRET")
          [ -z "$UPSTASH_REDIS_REST_URL" ]  && MISSING+=("UPSTASH_REDIS_REST_URL")
          [ -z "$UPSTASH_REDIS_REST_TOKEN" ] && MISSING+=("UPSTASH_REDIS_REST_TOKEN")
          if [ ${#MISSING[@]} -gt 0 ]; then
            echo "ERROR: Missing required secrets: ${MISSING[*]}"
            echo "Configure these in GitHub repo Settings → Secrets and variables → Actions."
            exit 1
          fi
          echo "All required backend secrets are configured."

# Add to the 'smoke-test' job, after existing smoke test steps:

      - name: Smoke test (admin auth endpoint)
        env:
          SMOKE_URL: ${{ vars.API_URL }}
        run: |
          # POST with an invalid token should return 400 or 422, NOT 404 or 500
          STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
            -X POST "${SMOKE_URL}/api/auth/admin/verify-token" \
            -H "Content-Type: application/json" \
            -d '{"token":"smoke-test-invalid-token"}')
          if [ "$STATUS" = "404" ] || [ "$STATUS" = "500" ]; then
            echo "Smoke test failed: /api/auth/admin/verify-token returned $STATUS (endpoint not registered or crashed)"
            exit 1
          fi
          echo "Admin auth endpoint reachable (returned $STATUS as expected for invalid token)"
```

---

## 2. Frontend Pipeline — `frontend.yml` Changes

### Existing Steps (unchanged)

```
type check → vercel pull → vercel build → vercel deploy
```

### Changes Required

**Add before type check:**

1. **Prerender guard** — CI check that every `.astro` file under `src/pages/admin/` contains `export const prerender = false`. Fails fast before build.
2. **Env var presence check** — verify Vercel secrets are configured.

### Complete Additions to `frontend.yml`

```yaml
# Add to 'build-and-deploy' job, after 'Install dependencies' step and BEFORE 'Type check':

      - name: Guard - verify all admin pages have prerender=false
        run: |
          ADMIN_PAGES=$(find src/pages/admin -name "*.astro" 2>/dev/null)
          if [ -z "$ADMIN_PAGES" ]; then
            echo "No admin pages found yet - skipping prerender check"
            exit 0
          fi
          MISSING=()
          while IFS= read -r file; do
            if ! grep -q "export const prerender = false" "$file"; then
              MISSING+=("$file")
            fi
          done <<< "$ADMIN_PAGES"
          if [ ${#MISSING[@]} -gt 0 ]; then
            echo "ERROR: The following admin pages are missing 'export const prerender = false':"
            printf '  %s\n' "${MISSING[@]}"
            echo ""
            echo "A missing prerender=false flag causes the page to be statically generated,"
            echo "silently bypassing the Astro auth middleware. This is a security gate."
            exit 1
          fi
          echo "Prerender guard passed: all admin pages have export const prerender = false"
        working-directory: frontend

      - name: Check required Vercel secrets are configured
        env:
          UPSTASH_REDIS_REST_URL: ${{ secrets.UPSTASH_REDIS_REST_URL }}
          UPSTASH_REDIS_REST_TOKEN: ${{ secrets.UPSTASH_REDIS_REST_TOKEN }}
          ASTRO_SESSION_SECRET: ${{ secrets.ASTRO_SESSION_SECRET }}
          VERCEL_DEPLOY_HOOK_URL: ${{ secrets.VERCEL_DEPLOY_HOOK_URL }}
        run: |
          MISSING=()
          [ -z "$UPSTASH_REDIS_REST_URL" ]   && MISSING+=("UPSTASH_REDIS_REST_URL")
          [ -z "$UPSTASH_REDIS_REST_TOKEN" ]  && MISSING+=("UPSTASH_REDIS_REST_TOKEN")
          [ -z "$ASTRO_SESSION_SECRET" ]      && MISSING+=("ASTRO_SESSION_SECRET")
          [ -z "$VERCEL_DEPLOY_HOOK_URL" ]    && MISSING+=("VERCEL_DEPLOY_HOOK_URL")
          if [ ${#MISSING[@]} -gt 0 ]; then
            echo "ERROR: Missing required secrets for author-mode: ${MISSING[*]}"
            echo "Configure these in GitHub repo Settings → Secrets and variables → Actions."
            echo "These must also be set in the Vercel project environment variables."
            exit 1
          fi
          echo "All required frontend secrets are configured."
```

---

## 3. Pipeline Visualisation (Post-Change State)

### Backend (`ci.yml`)

```
PR / push to main (backend/**)
  │
  └── Job: ci
        ├── Checkout
        ├── Setup .NET
        ├── Restore dependencies
        ├── Check formatting
        ├── Build
        ├── Unit tests (Domain)
        ├── Unit tests (Application)
        ├── Unit tests (Infrastructure)
        ├── Integration tests
        ├── Acceptance tests
        ├── [NEW] Validate EF migration files
        ├── [NEW] Check required secrets
        ├── Docker build → GHCR
        └── Push to GHCR (main only)
              │
              └── Job: deploy (needs: ci, main only)
                    ├── Install Koyeb CLI
                    ├── Koyeb services update (image pull)
                    └── Wait for HEALTHY
                          │
                          └── Job: smoke-test (needs: deploy, main only)
                                ├── GET /health → 200
                                ├── GET /health/ready → 200
                                └── [NEW] POST /api/auth/admin/verify-token → not 404/500
```

### Frontend (`frontend.yml`)

```
PR / push to main (frontend/**)
  │
  └── Job: build-and-deploy
        ├── Checkout
        ├── Setup Node.js
        ├── Install dependencies
        ├── [NEW] Guard: admin pages prerender=false
        ├── [NEW] Check required Vercel secrets
        ├── Type check (astro check)
        ├── Pull Vercel environment
        ├── Build (vercel build --prod)
        └── Deploy (vercel deploy --prebuilt --prod, main only)
```

---

## 4. Quality Gates

| Gate | Pipeline | Blocks | Description |
|------|----------|--------|-------------|
| Format check | ci.yml | PR merge | `dotnet format --verify-no-changes` |
| All tests pass | ci.yml | PR merge | Domain + Application + Infrastructure + Integration + Acceptance |
| EF migration present | ci.yml | PR merge | Grep for `AddArchivedStatusAndPreviousStatus` in Migrations |
| Backend secrets configured | ci.yml | PR merge | All 4 new secrets present in Actions environment |
| Admin prerender guard | frontend.yml | PR merge | Every `src/pages/admin/*.astro` has `export const prerender = false` |
| Frontend secrets configured | frontend.yml | PR merge | All 4 new Vercel secrets present |
| Type check | frontend.yml | PR merge | `npx astro check` zero errors |
| Koyeb health | ci.yml deploy | Production | Koyeb reports HEALTHY within 4 minutes |
| Backend smoke: liveness | ci.yml smoke | Production | `/health` → 200 |
| Backend smoke: readiness | ci.yml smoke | Production | `/health/ready` → 200 (DB reachable) |
| Backend smoke: admin auth | ci.yml smoke | Production | `/api/auth/admin/verify-token` → not 404/500 |

---

## 5. GitHub Secrets and Variables to Configure

### Repository Secrets (Settings → Secrets and variables → Actions → Secrets)

| Secret Name | Value Source |
|-------------|--------------|
| `ADMIN_EMAIL` | christian.borrello@gmail.com |
| `ADMIN_JWT_SECRET` | `openssl rand -base64 64` |
| `UPSTASH_REDIS_REST_URL` | Upstash console → REST API URL |
| `UPSTASH_REDIS_REST_TOKEN` | Upstash console → REST API token |
| `ASTRO_SESSION_SECRET` | `openssl rand -base64 32` |
| `VERCEL_DEPLOY_HOOK_URL` | Vercel dashboard → Project Settings → Git → Deploy Hooks |

### Repository Variables (existing, verify still set)

| Variable Name | Value |
|---------------|-------|
| `KOYEB_SERVICE` | Service name in Koyeb |
| `KOYEB_APP` | App name in Koyeb |
| `API_URL` | `https://api.theaugmentedcraftsman.christianborrello.dev` |
| `VERCEL_ORG_ID` | From Vercel account settings |
| `VERCEL_PROJECT_ID` | From Vercel project settings |

### Existing Secrets (verify still set)

`KOYEB_TOKEN` | `VERCEL_TOKEN` | `GOOGLE_CLIENT_SECRET` | `GITHUB_CLIENT_SECRET`

---

## 6. Trigger Matrix

| Event | Workflow | Jobs Triggered |
|-------|----------|----------------|
| PR opened/updated (backend/**) | ci.yml | `ci` only (no deploy, no smoke) |
| Push to main (backend/**) | ci.yml | `ci` → `deploy` → `smoke-test` |
| PR opened/updated (frontend/**) | frontend.yml | `build-and-deploy` (no deploy step) |
| Push to main (frontend/**) | frontend.yml | `build-and-deploy` (with deploy step) |
| `workflow_dispatch` | Both | Full pipeline including deploy |

---

## 7. Prerender Guard — Security Justification

The `export const prerender = false` guard is classified as a **security gate**, not a code quality check. Its absence allows Astro to pre-render an admin page at build time, producing a static HTML file that is served without invoking the auth middleware. A visitor could access the raw HTML of an admin page. The `.NET` backend JWT guard (Layer 3) would still block actual API calls, but the page HTML would be exposed.

This guard runs on every PR to main, blocking merge until all admin pages are correctly marked. It is the CI enforcement of DESIGN decision DD-09.

**Why grep over a test**: A unit test would not run at build time and could not catch a new file added by future development. The grep runs on the filesystem before the build, catching the problem at the earliest possible moment.

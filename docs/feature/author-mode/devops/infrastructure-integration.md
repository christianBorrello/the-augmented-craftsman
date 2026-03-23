# Infrastructure Integration: Author Mode

**Feature**: author-mode
**Wave**: PLATFORM (DESIGN — infrastructure readiness)
**Date**: 2026-03-14
**Architect**: Apex (platform-architect)

---

## Purpose

This document is the operational provisioning playbook for author-mode infrastructure. It specifies the exact sequence of operations to execute before any coding starts, so the development environment and production environment are ready to receive the feature.

---

## Provisioning Playbook — Exact Sequence

Execute in order. Each step must complete before the next begins.

### Phase 1: Upstash Redis

**Why first**: Upstash credentials are needed by both Vercel (Astro Sessions) and Koyeb (nonce store). All subsequent steps depend on having these values.

```
1. Go to https://console.upstash.com
2. Sign up / log in (no credit card required)
3. Click "Create Database"
4. Name: tacblog-sessions
   Region: eu-west-1 (or closest to your location)
   Type: Regional
5. Click "Create"
6. From the database detail page, copy:
   - REST URL → save as UPSTASH_REDIS_REST_URL
   - REST Token → save as UPSTASH_REDIS_REST_TOKEN
7. Verify connectivity:
   curl "$UPSTASH_REDIS_REST_URL/ping" \
     -H "Authorization: Bearer $UPSTASH_REDIS_REST_TOKEN"
   # Expected: {"result":"PONG"}
```

### Phase 2: OAuth App Configuration

**Why second**: OAuth callback URLs must be registered before any auth code is written. Callback URLs are fixed by the architecture (DESIGN wave) and must not change.

#### Google OAuth

```
1. Go to https://console.cloud.google.com
2. Select your project (or create: "the-augmented-craftsman")
3. APIs & Services → Credentials → OAuth 2.0 Client IDs
4. Edit existing client (used for reader OAuth) OR create new:
   Application type: Web application
   Name: The Augmented Craftsman
5. Add to "Authorized redirect URIs":
   https://api.theaugmentedcraftsman.christianborrello.dev/api/auth/admin/oauth/google/callback
   http://localhost:5000/api/auth/admin/oauth/google/callback  (for local dev)
6. Save. Verify CLIENT_ID and CLIENT_SECRET are noted.
```

#### GitHub OAuth

```
1. Go to https://github.com/settings/developers → OAuth Apps
2. Edit existing app OR click "New OAuth App":
   Application name: The Augmented Craftsman
   Homepage URL: https://theaugmentedcraftsman.com
   Authorization callback URL:
     https://api.theaugmentedcraftsman.christianborrello.dev/api/auth/admin/oauth/github/callback
3. For local dev: create a SECOND GitHub OAuth App:
   Authorization callback URL: http://localhost:5000/api/auth/admin/oauth/github/callback
4. Note CLIENT_ID and CLIENT_SECRET for each.
```

Note on single vs. dual OAuth apps: GitHub OAuth apps only support one callback URL. For development parity, create a separate "dev" OAuth app. Google supports multiple redirect URIs on one app.

### Phase 3: Secret Generation

Generate cryptographically strong secrets locally. Do not use password managers that may truncate or encode values.

```bash
# Admin JWT signing key (separate from existing Jwt__Secret)
echo "ADMIN_JWT_SECRET:"
openssl rand -base64 64

# Astro session cookie signing key
echo "ASTRO_SESSION_SECRET:"
openssl rand -base64 32
```

Save these values securely (password manager). They are never recoverable after this step.

### Phase 4: Vercel Deploy Hook

```
1. Go to https://vercel.com → your project → Settings → Git
2. Scroll to "Deploy Hooks"
3. Click "Create Hook":
   Hook Name: admin-publish
   Branch to Deploy: main
4. Copy the generated URL
5. Test the hook:
   curl -X POST "<hook-url>"
   # Expected: {"job":{"id":"...","state":"PENDING","createdAt":...}}
6. Save as VERCEL_DEPLOY_HOOK_URL
```

### Phase 5: GitHub Actions Secrets

Set all new secrets in the GitHub repository (Settings → Secrets and variables → Actions → Secrets → New repository secret):

| Secret | Value |
|--------|-------|
| `ADMIN_EMAIL` | christian.borrello@gmail.com |
| `ADMIN_JWT_SECRET` | (from Phase 3) |
| `UPSTASH_REDIS_REST_URL` | (from Phase 1) |
| `UPSTASH_REDIS_REST_TOKEN` | (from Phase 1) |
| `ASTRO_SESSION_SECRET` | (from Phase 3) |
| `VERCEL_DEPLOY_HOOK_URL` | (from Phase 4) |

Verify existing secrets are still set: `KOYEB_TOKEN`, `VERCEL_TOKEN`, `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GITHUB_CLIENT_ID`, `GITHUB_CLIENT_SECRET`.

### Phase 6: Vercel Environment Variables

Set in Vercel project dashboard (Settings → Environment Variables). Set for **Production** and **Preview** environments:

| Variable | Value | Environment |
|----------|-------|-------------|
| `UPSTASH_REDIS_REST_URL` | (from Phase 1) | Production + Preview |
| `UPSTASH_REDIS_REST_TOKEN` | (from Phase 1) | Production + Preview |
| `ASTRO_SESSION_SECRET` | (from Phase 3) | Production + Preview |
| `VERCEL_DEPLOY_HOOK_URL` | (from Phase 4) | Production |
| `ADMIN_EMAIL` | christian.borrello@gmail.com | Production + Preview |

Note: Vercel env vars set in the dashboard are also available to GitHub Actions via `vercel pull --environment=production` (already in `frontend.yml`). Setting them in both GitHub Secrets and Vercel dashboard is intentional — the GitHub secret check validates before build; the Vercel env var is used at runtime.

### Phase 7: Koyeb Environment Variables

Set in Koyeb service settings (App → Service → Settings → Environment variables):

| Variable | Value |
|----------|-------|
| `ADMIN_EMAIL` | christian.borrello@gmail.com |
| `ADMIN_JWT_SECRET` | (from Phase 3) |
| `UPSTASH_REDIS_REST_URL` | (from Phase 1) |
| `UPSTASH_REDIS_REST_TOKEN` | (from Phase 1) |
| `VERCEL_DEPLOY_HOOK_URL` | (from Phase 4) |

After setting, trigger a Koyeb redeploy so the new env vars are loaded (even though the code doesn't use them yet — this confirms Koyeb accepts the variables without error).

### Phase 8: EF Core Migration

Execute after Phase 7 (Koyeb env vars set) and after the `AddArchivedStatusAndPreviousStatus` migration has been created and committed.

```bash
# Generate migration (run from repo root)
cd backend
dotnet ef migrations add AddArchivedStatusAndPreviousStatus \
  --project src/Infrastructure \
  --startup-project src/Api

# Verify the generated migration files
ls -la src/Infrastructure/Migrations/

# Test locally (optional but recommended)
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Api
# Verify rollback
dotnet ef database update <PreviousMigrationName> \
  --project src/Infrastructure \
  --startup-project src/Api

# Apply to production Neon PostgreSQL
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Api \
  --connection "$NEON_CONNECTION_STRING"
```

**Verify**: Connect to Neon and confirm the `PreviousStatus` column exists:
```sql
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'BlogPosts'
AND column_name = 'PreviousStatus';
```

---

## Integration Validation Checklist

Complete this checklist before starting the first PR for author-mode code:

### Upstash Redis
- [ ] Database created in Upstash console
- [ ] REST URL and token copied
- [ ] `curl /ping` returns PONG
- [ ] Free tier confirmed (10K commands/day)

### OAuth Apps
- [ ] Google OAuth: admin callback URL added to authorized redirect URIs
- [ ] GitHub OAuth: admin callback URL configured (or separate dev app created)
- [ ] OAuth credentials available for Koyeb env vars

### Secrets and Variables
- [ ] All 6 new GitHub Actions secrets set
- [ ] All 5 new Vercel env vars set (Production + Preview)
- [ ] All 5 new Koyeb env vars set
- [ ] Koyeb redeployed with new env vars (service stays healthy)

### Vercel Deploy Hook
- [ ] Deploy hook created for main branch
- [ ] Manual `curl -X POST` test returns PENDING job
- [ ] Hook URL stored in `VERCEL_DEPLOY_HOOK_URL` secret/var

### Database
- [ ] `AddArchivedStatusAndPreviousStatus` migration generated and committed
- [ ] Migration forward tested locally
- [ ] Migration rollback tested locally
- [ ] Migration applied to Neon production
- [ ] `PreviousStatus` column confirmed in Neon

### CI/CD
- [ ] New steps added to `ci.yml` (migration check, secrets check, admin smoke test)
- [ ] New steps added to `frontend.yml` (prerender guard, secrets check)
- [ ] CI passes on a feature branch (no admin pages yet — prerender check skips gracefully)

---

## Local Development Configuration

For local development, create `.env.local` in `frontend/` and `.env` in `backend/` (both gitignored):

**`frontend/.env.local`:**
```
UPSTASH_REDIS_REST_URL=<from-upstash>
UPSTASH_REDIS_REST_TOKEN=<from-upstash>
ASTRO_SESSION_SECRET=<any-local-string-32-chars>
VERCEL_DEPLOY_HOOK_URL=<optional-for-local-dev>
ADMIN_EMAIL=christian.borrello@gmail.com
API_URL=http://localhost:5000
```

**`backend/.env` (or via `appsettings.Development.json`):**
```
ADMIN_EMAIL=christian.borrello@gmail.com
ADMIN_JWT_SECRET=<local-dev-secret>
UPSTASH_REDIS_REST_URL=<from-upstash>
UPSTASH_REDIS_REST_TOKEN=<from-upstash>
GOOGLE_CLIENT_ID=<dev-google-oauth-client-id>
GOOGLE_CLIENT_SECRET=<dev-google-oauth-client-secret>
GITHUB_CLIENT_ID=<dev-github-oauth-client-id>
GITHUB_CLIENT_SECRET=<dev-github-oauth-client-secret>
```

Note: You may point local dev to the same Upstash instance as production (sessions are namespaced by key). If you want isolation, create a second Upstash database named `tacblog-sessions-dev`.

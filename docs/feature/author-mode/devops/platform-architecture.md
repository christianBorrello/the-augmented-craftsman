# Platform Architecture: Author Mode

**Feature**: author-mode
**Wave**: PLATFORM (DESIGN — infrastructure readiness)
**Date**: 2026-03-14
**Architect**: Apex (platform-architect)

---

## Prior Wave Artifacts Confirmed

- [x] `docs/feature/author-mode/design/architecture-design.md`
- [x] `docs/feature/author-mode/design/technology-stack.md`
- [x] `docs/feature/author-mode/design/component-boundaries.md`
- [x] `docs/feature/author-mode/design/data-models.md`
- [x] `docs/feature/author-mode/design/wave-decisions.md`
- [x] `docs/feature/author-mode/discuss/outcome-kpis.md`

---

## 1. Platform Decisions (PLATFORM Wave)

| ID | Decision | Rationale |
|----|----------|-----------|
| PD-01 | Cloud-native PaaS — Vercel + Koyeb | No containers managed by us; both handle deployment natively |
| PD-02 | Recreate deployment strategy | Solo developer, acceptable downtime, simplest rollback (redeploy previous image) |
| PD-03 | GitHub Flow for branching | Feature branches from main, PR-triggered pipelines, merge-to-main triggers deploy |
| PD-04 | No formal observability tooling | Solo project; Vercel logs + DB queries are sufficient for KPI measurement |
| PD-05 | Mutation testing disabled | Early-stage solo project; documented in CLAUDE.md |
| PD-06 | Existing GitHub Actions extended | Do not create new workflow files; add jobs to `ci.yml` and `frontend.yml` |

---

## 2. Infrastructure Topology

```
GitHub (monorepo)
  │
  ├── push/PR to main (backend/**) → ci.yml
  │     ├── build + test (all suites)
  │     ├── [NEW] migration validation
  │     ├── [NEW] env var presence check
  │     ├── docker build → GHCR
  │     ├── Koyeb deploy (image update)
  │     └── smoke tests (/health, /health/ready, /api/auth/admin/verify-token)
  │
  └── push/PR to main (frontend/**) → frontend.yml
        ├── [NEW] prerender guard (grep admin pages)
        ├── [NEW] env var presence check (Vercel secrets)
        ├── type check (astro check)
        ├── vercel build --prod
        └── vercel deploy --prebuilt --prod (main only)

Production:
  Vercel (frontend) ←→ Koyeb (backend API)
       │                      │
  Upstash Redis          Neon PostgreSQL
  (Astro Sessions)       (posts, tags)
       │                      │
  Vercel Deploy Hook     ImageKit CDN
  (rebuild trigger)      (cover images)

OAuth:
  Google OAuth App  ──→  Koyeb /api/auth/admin/oauth/google/callback
  GitHub OAuth App  ──→  Koyeb /api/auth/admin/oauth/github/callback
```

---

## 3. Services Inventory

### Existing (no change to provisioning)

| Service | Role | Plan |
|---------|------|------|
| Vercel | Frontend hosting + SSR functions | Hobby (free) |
| Koyeb | Backend API hosting | Free tier |
| Neon PostgreSQL | Database | Free tier |
| ImageKit | Cover image storage + CDN | Free tier |
| GitHub Actions | CI/CD | Free (public repo minutes) |
| GHCR | Docker image registry | Free |

### New (to be provisioned before coding starts)

| Service | Role | Plan | Estimated Usage |
|---------|------|------|-----------------|
| Upstash Redis | Astro Sessions driver + admin token nonce store | Free tier | ~100 commands/day (single author); limit: 10,000/day |
| Google OAuth App | Identity provider for admin login | Free | N/A |
| GitHub OAuth App | Identity provider for admin login | Free | N/A |
| Vercel Deploy Hook | Rebuild trigger after publish/archive | Built into Vercel | 1 POST per publish event |

---

## 4. Environment Variables — Complete Map

### Vercel (frontend)

| Variable | Purpose | New for Author Mode | Notes |
|----------|---------|---------------------|-------|
| `UPSTASH_REDIS_REST_URL` | Astro Sessions Upstash driver | YES | From Upstash console |
| `UPSTASH_REDIS_REST_TOKEN` | Astro Sessions Upstash driver | YES | From Upstash console |
| `ASTRO_SESSION_SECRET` | Signs Astro session cookies | YES | Generate: `openssl rand -base64 32` |
| `VERCEL_DEPLOY_HOOK_URL` | Rebuild trigger URL | YES | From Vercel project settings |
| `ADMIN_EMAIL` | Displayed in admin UI (optional reference) | YES | christian.borrello@gmail.com |
| `API_URL` | Backend base URL for Astro Actions | Existing | https://api.theaugmentedcraftsman.christianborrello.dev |

### Koyeb (backend)

| Variable | Purpose | New for Author Mode | Notes |
|----------|---------|---------------------|-------|
| `ADMIN_EMAIL` | Whitelist check in admin OAuth callback | YES | christian.borrello@gmail.com |
| `ADMIN_JWT_SECRET` | Signs short-lived admin tokens (5-min TTL) | YES | Generate: `openssl rand -base64 64` — use separate key from `Jwt__Secret` |
| `UPSTASH_REDIS_REST_URL` | `RedisAdminTokenStore` nonce storage | YES | Same Upstash instance as frontend |
| `UPSTASH_REDIS_REST_TOKEN` | `RedisAdminTokenStore` nonce storage | YES | Same Upstash instance as frontend |
| `VERCEL_DEPLOY_HOOK_URL` | Optional: backend-side rebuild trigger | YES | Same hook URL as frontend var |
| `GOOGLE_CLIENT_ID` | Admin OAuth — Google | Existing (reader OAuth) | Add `/admin/callback` to authorized redirect URIs |
| `GOOGLE_CLIENT_SECRET` | Admin OAuth — Google | Existing (reader OAuth) | No change |
| `GITHUB_CLIENT_ID` | Admin OAuth — GitHub | Existing (reader OAuth) | Add `/admin/callback` to callback URLs |
| `GITHUB_CLIENT_SECRET` | Admin OAuth — GitHub | Existing (reader OAuth) | No change |
| `Jwt__Secret` | Long-lived JWT for API calls (existing) | Existing | No change |

---

## 5. Rollback Design

**Strategy**: Recreate (D6). Every deployment is a stop-and-replace on both Koyeb and Vercel.

### Backend Rollback

1. **Application rollback**: Redeploy previous GHCR image tag via Koyeb CLI:
   ```bash
   koyeb services update <service> --app <app> --docker ghcr.io/<owner>/tacblog-api:<previous-sha>
   ```
2. **Database rollback**: Run EF Core Down migration:
   ```bash
   dotnet ef database update <PreviousMigrationName> --project src/Infrastructure --startup-project src/Api
   ```
   Migration script generated before deploy; tested in CI.
3. **Rollback trigger**: Manual — solo developer. No automated rollback triggers needed.

### Frontend Rollback

Vercel maintains deployment history. Instant rollback via Vercel dashboard: Deployments tab → select previous deployment → Promote to Production. No CLI needed.

### Database Rollback Safety

The `PreviousStatus` column addition (new nullable column, no data backfill) is safe:
- **Forward**: `ALTER TABLE "BlogPosts" ADD COLUMN "PreviousStatus" VARCHAR(20) NULL;`
- **Backward**: `ALTER TABLE "BlogPosts" DROP COLUMN "PreviousStatus";` — safe, no dependent constraints.

**Rule**: Never deploy the backend image without first running the migration forward. Smoke test gate catches a misconfigured DB before Koyeb routes traffic.

---

## 6. OAuth App Configuration

### Google OAuth App

| Field | Value |
|-------|-------|
| Application name | The Augmented Craftsman |
| Authorized JavaScript origins | `https://theaugmentedcraftsman.com`, `http://localhost:4321` |
| Authorized redirect URIs | `https://api.theaugmentedcraftsman.christianborrello.dev/api/auth/admin/oauth/google/callback` |
| Scopes | `openid`, `email`, `profile` |

Note: The existing reader OAuth app MAY be the same app. If so, add the new admin callback URL to the existing authorized redirect URIs list — do NOT create a separate app.

### GitHub OAuth App

| Field | Value |
|-------|-------|
| Application name | The Augmented Craftsman |
| Homepage URL | `https://theaugmentedcraftsman.com` |
| Authorization callback URL | `https://api.theaugmentedcraftsman.christianborrello.dev/api/auth/admin/oauth/github/callback` |

Note: Same guidance as Google — extend existing OAuth app rather than creating a new one.

### Local Development Callback URLs

For local dev, add a second OAuth app (or use the same app with both URLs):
- Google: `http://localhost:5000/api/auth/admin/oauth/google/callback`
- GitHub: `http://localhost:5000/api/auth/admin/oauth/github/callback`

---

## 7. Vercel Deploy Hook

### Setup Steps

1. In Vercel dashboard: Project → Settings → Git → Deploy Hooks.
2. Click "Create Hook" — name: `admin-publish`, branch: `main`.
3. Copy the generated URL (format: `https://api.vercel.com/v1/integrations/deploy/<hook-id>`).
4. Store as `VERCEL_DEPLOY_HOOK_URL` in both Vercel and Koyeb env vars.

### Testing the Hook

```bash
curl -X POST "$VERCEL_DEPLOY_HOOK_URL"
# Expected: {"job":{"id":"...","state":"PENDING","createdAt":...}}
```

Monitor rebuild in Vercel dashboard: Deployments tab → filter "Triggered by Deploy Hook".

### Hook URL Security

The URL acts as a bearer secret. Treat it as a secret:
- Store in Vercel environment variables (not in source code).
- Store in Koyeb environment variables for backend-triggered rebuilds.
- Never commit to git.

---

## 8. Upstash Redis Setup

### Account and Database Creation

1. Sign up at https://console.upstash.com (free, no credit card required for free tier).
2. Create database: Region → `eu-west-1` (closest to Koyeb/Vercel deployments in Europe).
3. Type: Regional (not Global — Global is paid).
4. Copy `UPSTASH_REDIS_REST_URL` and `UPSTASH_REDIS_REST_TOKEN` from the console.

### Free Tier Limits

| Limit | Value | Expected Usage |
|-------|-------|----------------|
| Commands/day | 10,000 | ~100 (single author) |
| Max data size | 256 MB | < 1 MB (sessions + nonces) |
| Max connections | 1,000 | < 5 |
| Bandwidth | 50 GB/month | < 1 MB/month |

Free tier is sufficient for the lifetime of this feature as a single-author blog.

### Data Namespacing in Shared Redis Instance

Both Astro Sessions and the .NET `RedisAdminTokenStore` use the same Redis instance:

| Key Prefix | Owner | TTL |
|------------|-------|-----|
| `astro-session:*` | Astro Sessions driver (auto-managed) | 24 hours |
| `admin_token:*` | `RedisAdminTokenStore` (.NET) | 5 minutes |

No key conflicts. The `.NET` adapter uses only `admin_token:<nonce>` keys.

---

## 9. EF Core Migration

### Migration Plan

**Migration name**: `AddArchivedStatusAndPreviousStatus`

**Forward SQL** (generated by `dotnet ef migrations add`):
```sql
ALTER TABLE "BlogPosts" ADD COLUMN "PreviousStatus" VARCHAR(20) NULL;
```

**Backward SQL** (for rollback):
```sql
ALTER TABLE "BlogPosts" DROP COLUMN "PreviousStatus";
```

No data backfill required. The `Status` column already stores strings; `"Archived"` is a new valid value — no constraint changes needed.

### Migration Execution Sequence

1. Generate migration locally:
   ```bash
   cd backend
   dotnet ef migrations add AddArchivedStatusAndPreviousStatus \
     --project src/Infrastructure \
     --startup-project src/Api
   ```
2. Verify the generated `Up()` and `Down()` methods in the migration file.
3. Test the migration against a local PostgreSQL (Testcontainers or local instance):
   ```bash
   dotnet ef database update --project src/Infrastructure --startup-project src/Api
   dotnet ef database update <PreviousMigration> --project src/Infrastructure --startup-project src/Api
   ```
4. Commit the migration file.
5. CI validates the migration file exists and the build compiles (see `ci-cd-pipeline.md`).
6. After Koyeb deploy, run migration against Neon production DB via connection string:
   ```bash
   dotnet ef database update \
     --project src/Infrastructure \
     --startup-project src/Api \
     --connection "$NEON_CONNECTION_STRING"
   ```

### Deployment Sequence (strictly ordered)

```
1. Run EF migration against Neon PostgreSQL (production)
2. Deploy new Docker image to Koyeb
3. Wait for Koyeb health check HEALTHY
4. Run smoke tests (including /api/auth/admin/verify-token probe)
5. [IF FAIL] Roll back: redeploy previous image, run Down migration
```

The backend is designed backward-compatible: the new `PreviousStatus` column is nullable with no default, so the old image continues to work after the column is added. This eliminates the need for a maintenance window.

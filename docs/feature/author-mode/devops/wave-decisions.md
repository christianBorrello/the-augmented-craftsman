# Wave Decisions: Author Mode PLATFORM

**Feature**: author-mode
**Wave**: PLATFORM (DESIGN — infrastructure readiness)
**Date**: 2026-03-14
**Architect**: Apex (platform-architect)

---

## Platform Wave Decisions

These decisions govern infrastructure and delivery for author-mode. They complement and do not override DESIGN wave decisions (DD-01 through DD-09).

---

### PD-01: Recreate Deployment Strategy

**Decision**: Use recreate (stop-and-replace) deployment for both Koyeb and Vercel.

**Rationale**: Solo developer project with acceptable downtime. The Koyeb free tier provides a single service instance — blue-green is not available without a second service. Vercel's Hobby plan handles deployment atomically (new deploy becomes active; old is deactivated). The complexity of canary or rolling deployments is not justified.

**Rollback**: Koyeb — redeploy previous GHCR image tag via CLI. Vercel — promote previous deployment in dashboard (instant, one click).

**Rejected alternatives**:
- Canary: requires traffic splitting infrastructure not available on Koyeb free tier.
- Blue-green: requires two Koyeb service instances; cost and complexity not justified for a personal blog.
- Rolling: single Koyeb instance cannot roll — it is already the simplest unit.

---

### PD-02: Extend Existing Workflows — No New Workflow Files

**Decision**: Add jobs and steps to `ci.yml` and `frontend.yml`. Do not create new workflow files.

**Rationale**: The existing workflows already handle the full deployment lifecycle. Creating separate files for author-mode would fragment the pipeline and create maintenance burden. The additions (prerender guard, migration check, secrets check, admin smoke test) are logically part of the existing CI/CD flow.

**Tradeoff**: Future features will continue to extend the same files. If the workflows grow unwieldy, extract reusable actions (GitHub composite actions) — but that is a refactoring concern, not a current concern.

---

### PD-03: Shared Upstash Redis Instance for Sessions and Nonces

**Decision**: A single Upstash Redis database is used for both Astro Sessions (frontend) and admin token nonces (backend `RedisAdminTokenStore`). Keys are prefixed to prevent collision.

**Rationale**: Free tier provides more than sufficient capacity. A second Redis instance would require provisioning, separate credentials, and additional configuration with no practical benefit at this scale.

**Key prefixes**:
- `astro-session:*` — Astro Sessions driver
- `admin_token:*` — `RedisAdminTokenStore` (.NET)

**Risk**: If Upstash is unavailable, both sessions and nonce validation fail simultaneously. For a personal blog, this is acceptable — both features are only used by one person.

---

### PD-04: No Separate OAuth Apps for Admin vs Reader

**Decision**: Extend the existing Google/GitHub OAuth apps with the new admin callback URLs rather than creating separate apps.

**Rationale**: The backend uses `GOOGLE_CLIENT_ID`/`GITHUB_CLIENT_ID` for both reader and admin OAuth flows. Both flows use the same `IOAuthClient` port implementation. Creating separate apps would require separate credential management with no security benefit (the admin whitelist check in the backend is the security control, not the OAuth app itself).

**Exception**: GitHub OAuth apps support only one callback URL. For local development, a separate dev OAuth app is required. For production, the single production app is extended.

---

### PD-05: EF Migration Runs Before Image Deployment

**Decision**: The EF Core migration against Neon PostgreSQL is executed manually before the new Docker image is deployed to Koyeb.

**Rationale**: The `PreviousStatus` column is nullable with no default. The old application image is compatible with the new schema (it simply ignores the new column). This means the migration can be applied before the image update without a maintenance window. If the migration is accidentally skipped, the new image will still start but `PreviousStatus` will fail to persist — the smoke test for `/api/auth/admin/verify-token` would pass but post archive would fail. The CI gate (migration file check) ensures the migration file exists; the developer is responsible for running it before deploying.

**Future automation**: If the project grows, add a `dotnet-ef database update` step to the `deploy` job in `ci.yml` using the Neon connection string as a secret. Deferred until the migration cadence warrants it.

---

### PD-06: VERCEL_DEPLOY_HOOK_URL Stored as Both GitHub Secret and Vercel Env Var

**Decision**: `VERCEL_DEPLOY_HOOK_URL` is stored in GitHub Actions secrets (for the secrets presence check) AND in Vercel project environment variables (for runtime use by Astro Actions).

**Rationale**: The CI check validates that the secret exists before building. The Astro `RebuildService` at runtime reads from the Vercel environment. These are two different access contexts. The GitHub secret check catches misconfiguration early; the Vercel env var is the actual runtime value.

**Security**: The hook URL functions as a bearer token. Both storage locations are secrets management systems (GitHub encrypted secrets, Vercel encrypted env vars). Neither is visible in logs or source code.

---

### PD-07: Prerender Guard Implemented as CI Shell Script

**Decision**: The `export const prerender = false` check is implemented as a `find` + `grep` shell script in the GitHub Actions workflow, not as a unit test or linter rule.

**Rationale**: The check must run before the build, on the filesystem, to catch new files. A unit test in the test suite would not catch a missing declaration in a file that hasn't been imported yet. An ESLint rule could work but adds a rule configuration dependency. The shell script is self-contained, dependency-free, and runs in < 1 second.

**Maintenance**: When new admin pages are added, the check runs automatically — no maintenance required. The check is fail-safe: if no admin pages exist yet, it exits 0 (skips gracefully).

---

## Open Questions Inherited from DESIGN Wave

| OQ | Question | Recommendation | Owner |
|----|----------|----------------|-------|
| OQ-01 | JWT TTL (60 min) vs Astro session TTL (24h) — force re-login mid-session? | Set `Jwt:ExpiryInMinutes=1440` (24h) to match session TTL for first implementation. Revisit if security concern arises. | EXECUTE |
| OQ-02 | `ADMIN_JWT_SECRET` reuse vs separate key? | Use separate key (`ADMIN_JWT_SECRET`) — already provisioned as a distinct secret. Minimal config burden, better isolation. | EXECUTE |
| OQ-03 | EditControls: show [Archive] button in addition to [Modifica]? | MVP: [Modifica] only. Add [Archive] as a separate story in a future iteration. | DISTILL |

---

## Simplest Solution Check

Per platform engineering principle 4 (simplest infrastructure first), documenting two rejected simpler alternatives:

### Rejected Alternative 1: No CI Guards, Manual Review Only

- **What**: Skip the prerender guard and secrets check. Rely on code review to catch missing `prerender = false` flags.
- **Expected Impact**: Meets 80% of requirements (pipeline still builds and deploys).
- **Why Insufficient**: Code review is unreliable for catching a missing boolean export across multiple files. One missed flag creates a silent security bypass that bypasses auth middleware. The cost of the grep check is < 1 second; the cost of a security incident is unbounded.

### Rejected Alternative 2: Platform-Managed Auth (Vercel Edge Middleware Only)

- **What**: Use Vercel Edge Middleware for all auth, removing the three-layer approach.
- **Expected Impact**: Simpler auth architecture.
- **Why Insufficient**: Vercel Edge Middleware does not support Astro Sessions. The `@astrojs/vercel` adapter's session driver requires Vercel Functions context, not Edge context. Additionally, this would remove the backend JWT layer, making the `.NET` API trust Astro implicitly — violating the defense-in-depth principle (DESIGN DD-02).

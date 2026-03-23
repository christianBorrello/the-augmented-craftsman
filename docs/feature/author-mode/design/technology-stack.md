# Technology Stack: Author Mode

**Feature**: author-mode
**Wave**: DESIGN
**Date**: 2026-03-14

---

All technology choices for Author Mode are **extensions of the existing stack**. No new technology categories are introduced. This section documents each addition with rationale and license.

---

## Frontend Additions

### Astro Sessions with Upstash Redis Driver

| Property | Value |
|---|---|
| Package | `@astrojs/db` is NOT used — Astro Sessions uses `astro:session` API with a driver |
| Driver package | `@upstash/redis` (for Astro Sessions Upstash driver) |
| License | MIT (`@astrojs` packages), MIT (`@upstash/redis`) |
| Astro version requirement | 5.7+ (stable Sessions API) — current is 6.0.4, satisfied |
| Rationale | Only session driver supported by `@astrojs/vercel` adapter. Free tier (10K commands/day) is sufficient for a single-author admin (~100 commands/day estimated). Upstash Redis is the official recommendation in Astro docs for Vercel deployments. |
| Alternatives considered | (1) JWT cookie stored client-side — rejected: no server-side invalidation, security risk. (2) Cookie-only session — rejected: not supported by Astro Sessions API; would require custom implementation. |

### Astro Hybrid Output Mode

| Property | Value |
|---|---|
| Change | `output: 'static'` → `output: 'hybrid'` in `astro.config.mjs` |
| License | MIT (Astro) |
| Rationale | Enables per-page opt-out of SSG via `export const prerender = false`. All existing `/blog/*` pages remain pre-rendered (SSG). Only `/admin/*` pages become SSR. No third-party library added. |
| Alternatives considered | (1) `output: 'server'` — rejected: would make all pages SSR, breaking existing SSG performance for readers. (2) Separate Astro project for admin — rejected (D-01). |

### Tiptap (already in project)

| Property | Value |
|---|---|
| Packages | `@tiptap/react`, `@tiptap/starter-kit` (already in `vite.optimizeDeps`) |
| License | MIT |
| Status | Already validated via prototype at `src/pages/admin/test-editor.astro`. No new work. |

---

## Backend Additions

### Short-Lived Admin Token (Redis-backed nonce)

| Property | Value |
|---|---|
| Mechanism | JWT with 5-minute TTL, single-use enforced via Redis nonce (stored in existing Upstash Redis — same instance as Astro Sessions) |
| Library | Existing `JwtTokenGenerator` + `ITokenGenerator` port + Upstash Redis (via `StackExchange.Redis` or `Upstash.Redis` .NET client) |
| License | MIT (`StackExchange.Redis`) |
| Rationale | Prevents replay attacks on the Astro OAuth callback page. A nonce stored in Redis is invalidated on first use, making the token truly single-use. Uses existing JWT infrastructure. |
| Alternatives considered | (1) HMAC-signed opaque token — viable but requires custom signing logic. JWT reuses existing `JwtTokenGenerator`. (2) Plain redirect query params with email — rejected: exposes PII in browser history and access logs. |

### `PostStatus.Archived` Domain Extension

| Property | Value |
|---|---|
| Change | Add `Archived` to existing `PostStatus` enum |
| Migration | EF Core migration to update the database column (string or int representation) |
| `PreviousStatus` | New nullable column on `BlogPost` table |
| Rationale | Soft delete (D-05) — content is recoverable. Minimal domain change. No new infrastructure. |

### New Use Cases

| Use Case | Port Dependency | Notes |
|---|---|---|
| `ArchivePost` | `IBlogPostRepository` (existing) | Sets `Archived`, stores `PreviousStatus` |
| `RestorePost` | `IBlogPostRepository` (existing) | Reads `PreviousStatus`, restores |
| `HandleAdminOAuthCallback` | `IOAuthClient` (existing), `IAdminTokenStore` (new port) | Checks `ADMIN_EMAIL` env var, issues admin token |
| `VerifyAdminToken` | `IAdminTokenStore` (new port) | Validates and invalidates admin token nonce |

### `IAdminTokenStore` Port (New)

| Property | Value |
|---|---|
| Purpose | Store and invalidate single-use admin tokens (nonces) |
| Adapter | `RedisAdminTokenStore` — calls Upstash Redis via its HTTP REST API |
| HTTP client | Existing `IHttpClientFactory` (already registered in `Program.cs`) — no new SDK required |
| License | MIT (HttpClient is built-in .NET) |
| Rationale | Upstash Redis exposes a REST API (`https://<host>/set`, `/get`, `/del`) accessible via plain `HttpClient`. This avoids adding `StackExchange.Redis` or the Upstash .NET SDK as a new dependency. The port is a pure interface; the adapter is in Infrastructure. Follows existing hexagonal pattern. |
| Alternatives considered | (1) In-memory dictionary — rejected: does not survive process restarts (Koyeb can restart containers). (2) PostgreSQL nonce table — viable but adds DB write per login. Redis TTL handles expiry automatically. (3) StackExchange.Redis SDK — viable but adds a new package dependency; Upstash REST API is sufficient for low-frequency admin logins. |

---

## Environment Variables Required

| Variable | Used By | Purpose |
|---|---|---|
| `ADMIN_EMAIL` | .NET backend | Whitelist check in admin OAuth callback |
| `ASTRO_SESSION_SECRET` | Astro (Vercel) | Signs session cookies |
| `UPSTASH_REDIS_REST_URL` | Astro (Vercel) + .NET backend | Upstash Redis endpoint |
| `UPSTASH_REDIS_REST_TOKEN` | Astro (Vercel) + .NET backend | Upstash Redis auth |
| `VERCEL_DEPLOY_HOOK_URL` | Astro (Vercel) | Rebuild trigger URL |
| `OAUTH_ADMIN_JWT_SECRET` | .NET backend | Signing key for short-lived admin tokens (can reuse `Jwt__Secret`) |

---

## Technology Not Introduced

The following were considered and explicitly rejected:

| Technology | Reason for Rejection |
|---|---|
| Auth0 / Cognito | No need — existing OAuth infrastructure already covers identity. Adding a managed IdP would add cost and vendor dependency. |
| NextAuth / Lucia | Frontend auth libraries — overkill given Astro Sessions + backend verification pattern. |
| ISR (Incremental Static Regeneration) | Not supported by `@astrojs/vercel` static adapter. Full rebuild is acceptable. |
| Separate admin subdomain | D-01 decision from DISCUSS wave — one project, one deploy, one domain. |
| React (full) instead of Preact | Preact + `@preact/compat` already validated. Adding React would double bundle size and create adapter conflicts. |

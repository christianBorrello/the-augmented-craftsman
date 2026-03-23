# ADR-002: Astro Hybrid Output + Upstash Redis Sessions

**Status**: Accepted
**Date**: 2026-03-14
**Feature**: author-mode

---

## Context

The Astro frontend currently uses `output: 'static'` — the entire site is pre-rendered at build time. Author Mode requires SSR for `/admin/*` pages (middleware, session access, Astro Actions). Two decisions are linked: the output mode and the session storage mechanism.

Astro Sessions (stable from v5.7, currently on 6.0.4) requires a driver for Vercel deployments. The official recommendation is Upstash Redis.

This ADR was pre-decided in DISCUSS D-01 (hybrid admin) and D-06 (Astro Sessions + Upstash Redis). The DESIGN wave confirms technical feasibility and specifies the exact configuration.

## Decision

### Output Mode: `output: 'hybrid'`

Change `output: 'static'` to `output: 'hybrid'` in `astro.config.mjs`. Every existing page remains pre-rendered (SSG) by default. Admin pages opt out explicitly with `export const prerender = false`.

### Session Storage: Astro Sessions + `@upstash/redis` Driver

Configure Astro Sessions with the Upstash Redis driver. Session TTL: 24 hours. Session cookie: HttpOnly, Secure, SameSite=Lax.

### Session Schema

```json
{
  "isAdmin": true,
  "email": "christian.borrello@gmail.com",
  "name": "Christian Borrello",
  "avatarUrl": "https://...",
  "jwtToken": "<signed-JWT>",
  "jwtExpiresAt": "2026-03-14T15:00:00Z"
}
```

The `jwtToken` is stored in the session and included as `Authorization: Bearer` in all Astro Action calls to the .NET API.

### Server Island Exception

The `EditControls` Server Island uses `Astro.cookies` + direct Upstash Redis lookup (not `Astro.session`) because Server Islands are processed as independent HTTP requests and `Astro.session` availability is not guaranteed in this context in Astro 6.

## Alternatives Considered

### Alternative A: `output: 'server'`
All pages become SSR.

**Rejected**: Breaks SSG guarantees for `/blog/*` pages. Public readers would experience server-side rendering latency and Vercel Function cold starts on every page load. This is a direct violation of NFR-01 (LCP < 1.5s for public blog).

### Alternative B: Cookie-Only Session (No Astro Sessions)
Store `isAdmin` and JWT in a signed, encrypted cookie. No Redis.

**Rejected**: Does not support server-side session invalidation (logout, security revocation). A compromised cookie would be valid until expiry. Astro Sessions with Redis allows `session.destroy()` for immediate invalidation. Also, the Astro Sessions API is the idiomatic Astro solution for this use case.

### Alternative C: JWT Cookie Only (No Session Store)
Issue a long-lived JWT stored in an HttpOnly cookie. No Redis.

**Rejected**: Same invalidation problem as B. Additionally, long-lived JWTs are a known security anti-pattern — if compromised, they cannot be revoked. The combination of a short-to-medium JWTs (60 min) with a session store that can be invalidated is the correct pattern.

### Alternative D: Planetscale / Turso for Session Storage
Use a serverless database instead of Redis for sessions.

**Rejected**: Upstash Redis is the officially supported Astro Sessions driver for Vercel deployments. Using a different backend would require a custom Astro Sessions driver. Upstash Redis free tier (10K commands/day) is sufficient.

## Consequences

**Positive**:
- Zero performance regression for public readers (`/blog/*` remains pure SSG).
- Server-side session invalidation possible via `session.destroy()`.
- Automatic TTL expiry via Redis — no cleanup job needed.
- Free tier sufficient for single-author usage.

**Negative**:
- New Vercel environment variables required (`ASTRO_SESSION_SECRET`, `UPSTASH_REDIS_REST_URL`, `UPSTASH_REDIS_REST_TOKEN`).
- Upstash Redis is an external service dependency. If Upstash is unavailable, admin login fails. Acceptable for single-author blog (readers unaffected).
- JWT expiry (60 min) shorter than session TTL (24h) requires consideration. Mitigation: on 401 from backend, middleware redirects to re-login.

## Quality Attribute Impact

| Attribute | Impact |
|---|---|
| Performance | Positive — public pages unaffected (SSG preserved) |
| Security | Positive — server-side invalidation, HttpOnly cookie |
| Reliability | Minor risk — Upstash dependency; readers are unaffected on failure |
| Maintainability | Neutral — idiomatic Astro approach |

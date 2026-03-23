# ADR-003: Three-Layer Admin Authorization

**Status**: Accepted
**Date**: 2026-03-14
**Feature**: author-mode

---

## Context

Author Mode introduces admin-only operations on a publicly deployed system. The risk of unauthorized content modification is high (reputational impact). The backend already uses JWT Bearer authorization for admin endpoints. The question is how to layer authentication between the Astro frontend and the .NET backend.

This ADR addresses RC-02: "Is `GET /api/posts?admin=true` protected in the .NET backend, or is Astro middleware the only auth layer?"

## Decision

Enforce admin authorization at **three independent layers**. No single layer is trusted to be sufficient.

### Layer 1 — Astro Middleware (`src/middleware.ts`)

- Intercepts every request to `/admin/*` (except `/admin/login` and `/admin/callback`).
- Reads Astro session, checks `isAdmin === true`.
- If valid: populates `Astro.locals.user` with profile + JWT.
- If invalid: redirect to `/admin/login`.
- Uses `getActionContext()` to intercept Astro Actions before they reach handlers.

**What this protects**: SSR page rendering and Action invocation from the browser.

**Gap**: If a page is accidentally pre-rendered (missing `export const prerender = false`), this layer is bypassed. Mitigated by Layer 3 and CI check.

### Layer 2 — Astro Action Handlers

- Every Action that calls the .NET API explicitly checks `Astro.locals.user` at the top of the handler.
- If `Astro.locals.user` is null: return `ActionError` with `code: 'UNAUTHORIZED'`. No backend call is made.

**What this protects**: Defense in depth if middleware is bypassed for any reason (e.g., Astro version change in middleware behavior).

### Layer 3 — .NET JWT Bearer Authorization

- All admin endpoints are decorated with `.RequireAuthorization()`.
- The .NET API validates the JWT signature, issuer, audience, and expiry on every request.
- No JWT: 401 response. Invalid JWT: 401 response.
- The .NET API has no concept of "trust the caller because they said so" — every request is independently validated.

**What this protects**: Direct API calls that bypass the Astro frontend entirely (e.g., curl, third-party client). The .NET API is independently secure.

### Supporting: CI Guard for `prerender = false`

A CI check (grep pattern) fails the build if any file in `src/pages/admin/**/*.astro` is missing `export const prerender = false`. This closes the gap in Layer 1.

### Separation of Admin and Public API Routes

Admin list endpoint: `GET /api/admin/posts` (returns all statuses, requires JWT).
Public browse endpoint: `GET /api/posts` (returns only Published, anonymous).

No single endpoint with a `?admin=true` flag — separate routes with separate authorization requirements.

## Alternatives Considered

### Alternative A: Session-Only Authorization (No JWT on Backend)
Astro validates the session and calls the .NET API without any authentication header. The .NET API trusts all requests from the Astro SSR functions.

**Rejected**: The .NET API would be completely unprotected from direct HTTP clients. Any actor with knowledge of the API URL could create, edit, or delete posts. This violates the "fail secure" principle and OWASP A01 (Broken Access Control).

### Alternative B: API Key Authorization (Static Secret)
Astro includes a shared API key in all admin requests. The backend validates the key.

**Rejected**: A static secret shared between two deployed systems must be rotated manually. JWTs are time-limited and can be invalidated by changing the signing key. API keys also cannot carry identity claims (email, session binding).

### Alternative C: mTLS Between Astro Functions and .NET API
Mutual TLS for service-to-service authentication.

**Rejected**: Disproportionate operational complexity for a solo developer project. JWT Bearer is well-understood, already implemented, and sufficient for this threat model.

## Consequences

**Positive**:
- .NET API is independently secure — testable without Astro in the loop.
- Defense in depth — a bypass at any one layer is contained.
- Follows OWASP principle of complete mediation (check authorization on every request).
- Existing JWT infrastructure reused — no new auth mechanism.

**Negative**:
- JWT expiry (60 min) shorter than Astro session TTL (24 hours) — user may need to re-login mid-session. Mitigated by middleware detecting 401 and redirecting to `/admin/login` with clear message.
- Slightly more verbose Action handlers (explicit `locals.user` check). Accepted as explicit over implicit.

## Quality Attribute Impact

| Attribute | Impact |
|---|---|
| Security | Strongly positive — defense in depth, OWASP A01 addressed |
| Testability | Positive — each layer testable independently |
| Maintainability | Minor negative — three layers to update if auth model changes |
| Performance | Negligible — JWT validation is a local operation (no network call) |

# ADR-001: Admin OAuth Flow — Dedicated Routes with Single-Use Token Bridge

**Status**: Superseded by ADR-005
**Date**: 2026-03-14
**Superseded date**: 2026-03-15
**Feature**: author-mode / admin-auth-simplification

---

## Context

The blog platform needs admin authentication via OAuth (Google/GitHub) as decided in DISCUSS D-02. The backend (.NET 10) already has a working OAuth infrastructure for reader sessions (`/api/auth/oauth/*`, `HandleOAuthCallback`, `IOAuthClient`). The frontend (Astro 6) will manage admin sessions using Astro Sessions with Upstash Redis.

The challenge is the **handoff between backend and frontend**:
- The backend performs OAuth code exchange and knows the user's email.
- The backend must verify `ADMIN_EMAIL` before granting admin access.
- The Astro frontend must create the session — it cannot trust query parameters passed in a browser redirect (browser history, server logs).

This ADR addresses RC-01: "How does the .NET backend authenticate the user and how does Astro create the session? Which endpoint handles the callback? What HTTP flow?"

## Decision

Use **separate admin OAuth routes** on the backend (`/api/auth/admin/oauth/*`) and a **single-use signed token bridge** to transfer authentication state from backend to frontend.

### Flow

1. Author clicks OAuth button on `/admin/login`.
2. Astro page redirects to `.NET` `GET /api/auth/admin/oauth/{provider}`.
3. Backend redirects author to Google/GitHub OAuth provider.
4. Provider redirects author to `.NET` `GET /api/auth/admin/oauth/{provider}/callback`.
5. Backend exchanges code, fetches profile, checks `email == ADMIN_EMAIL`.
   - If fail: redirect to `/admin/login?error=unauthorized`.
   - If pass: generate a signed JWT (5-minute TTL) with a nonce claim. Store nonce data in Redis (`admin_token:<nonce>` key, 5-minute TTL). Redirect to Astro `/admin/callback?token=<JWT>`.
6. Astro `/admin/callback` page posts `token` to `POST /api/auth/admin/verify-token`.
7. Backend validates JWT signature + TTL, looks up nonce in Redis, deletes nonce (single-use), returns `{ email, name, avatarUrl, jwtToken, jwtExpiresAt }`.
8. Astro creates session in Upstash Redis via `session.set({ isAdmin: true, ... })`.
9. Redirect to `/admin/posts`.

## Alternatives Considered

### Alternative A: Plain Redirect Query Parameters
Pass `email`, `name`, `avatarUrl` as plain query params in the redirect from backend to Astro.

**Rejected**: PII exposed in browser history, access logs, and referer headers. No protection against URL tampering or replay.

### Alternative B: Reuse Reader Session Infrastructure
Use the existing `reader_session` cookie and `ReaderSession` PostgreSQL table for admin sessions, adding an `IsAdmin` flag.

**Rejected**: Reader and admin sessions have different TTLs (30 days vs 24 hours), different storage backends (PostgreSQL vs Redis), and different trust semantics. Mixing them increases the risk of privilege confusion and complicates the independent testing of each auth path.

### Alternative C: Astro Handles OAuth Directly (Without Backend)
Use a frontend-only OAuth library (e.g., `arctic` for Astro) to handle the OAuth flow entirely within Astro SSR.

**Rejected**: The ADMIN_EMAIL check and JWT issuance are backend concerns. Moving them to Astro would (1) duplicate OAuth configuration between frontend and backend environments, (2) break the principle that the backend is the source of auth truth, and (3) require storing OAuth client secrets in Vercel environment variables separately from Koyeb.

## Consequences

**Positive**:
- Backend remains the single source of auth truth.
- Single-use token prevents callback URL replay attacks.
- Reuses existing `IOAuthClient` and `ITokenGenerator` ports — no new infrastructure primitives.
- Admin and reader OAuth flows are independently testable and independently evolvable.

**Negative**:
- Two new backend endpoint groups required (`/api/auth/admin/oauth/*`, `/api/auth/admin/verify-token`).
- New `IAdminTokenStore` port and Redis adapter required.
- Slightly more complex OAuth flow than a simple redirect — mitigated by the nonce being auto-expired by Redis TTL.

## Quality Attribute Impact

| Attribute | Impact |
|---|---|
| Security | Positive — single-use token prevents replay; PII not in URL |
| Maintainability | Neutral — additional endpoints, but following existing patterns |
| Testability | Positive — `HandleAdminOAuthCallback` and `VerifyAdminToken` are use cases with stubbed ports |
| Complexity | Minor increase — justified by security requirements |

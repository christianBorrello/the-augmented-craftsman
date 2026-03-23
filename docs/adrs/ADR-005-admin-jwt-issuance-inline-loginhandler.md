# ADR-005: Admin JWT Issuance — Inline in LoginHandler

**Status**: Accepted
**Date**: 2026-03-15
**Feature**: admin-auth-simplification
**Supersedes**: ADR-001 (Admin OAuth Flow — Dedicated Routes with Single-Use Token Bridge)

---

## Context

ADR-001 established an admin OAuth flow: 4-step GitHub/Google OAuth → `HandleAdminOAuthCallback` → nonce stored in Redis → `VerifyAdminToken` → admin JWT issued. This created accidental complexity: two GitHub OAuth apps, Redis dependency, three use cases, keyed DI registrations, and a production crash bug (`SingleOrDefault` failure from dual `IOAuthClient` registrations).

A `LoginHandler` use case already exists that validates email/password via bcrypt and emits a JWT. The JWT it emitted had two defects:
1. It was signed with `JwtSettings.Secret` (the reader secret) instead of `IAdminSettings.JwtSecret`
2. It carried no `role: "admin"` claim

All admin endpoints require `role: "admin"` on the bearer token. These two defects made `LoginHandler` unusable for admin access, which drove the OAuth workaround.

The question is: given that `LoginHandler` already handles credential validation and brute-force protection correctly, what is the minimal change to make it emit a valid admin JWT?

---

## Decision

`LoginHandler` inlines JWT issuance using `IAdminSettings.JwtSecret`. The `ITokenGenerator` port is removed from `LoginHandler`'s dependencies.

The JWT issued by `LoginHandler` on successful login carries:
- `sub`: validated admin email (from `LoginCommand.Email`)
- `email`: same as `sub`
- `role`: `"admin"` (hardcoded — this is the only admin, the only role)
- `jti`: new `Guid` per issuance
- `exp`: `IClock.UtcNow + 480 minutes`
- Signed with `IAdminSettings.JwtSecret` via HMAC-SHA256
- Issuer and audience: `"TacBlog"` (matches existing `JwtBearerOptions` validation)

All three OAuth admin use cases (`InitiateAdminOAuth`, `HandleAdminOAuthCallback`, `VerifyAdminToken`) are deleted. `IAdminTokenStore`, `InMemoryAdminTokenStore`, and `RedisAdminTokenStore` are deleted. `AdminOAuthEndpoints.cs` is deleted. The frontend OAuth button is replaced with an email/password form.

JWT transport: response body (`{ "token": "...", "expiresAt": "..." }`). Frontend stores in memory, attaches as Bearer token on admin API requests.

---

## Alternatives Considered

### Alternative A: Keep OAuth, Fix JWT Gap Only

Retain the OAuth flow but fix `VerifyAdminToken` to issue the correct JWT (it already does this correctly — `IssueAdminJwt` in `VerifyAdminToken.cs` is the reference implementation). Remove only the `role` claim gap.

**Rejected**: This leaves all complexity in place — Redis dependency, dual `IOAuthClient`, three use cases, keyed DI. The crash bug remains. The OAuth flow requires two GitHub OAuth apps to be maintained. The fundamental problem (accidental complexity) is not addressed.

### Alternative B: Email/Password via `LoginHandler` with `ITokenGenerator` Retained

Keep `ITokenGenerator` but make `JwtTokenGenerator` emit the `role: "admin"` claim. `LoginHandler` continues to delegate to `ITokenGenerator`.

**Rejected**: `ITokenGenerator` is also used for reader OAuth sessions. Modifying it to emit `role: "admin"` would contaminate reader tokens. A separate `IAdminTokenGenerator` port would add complexity rather than remove it. Inlining JWT issuance in `LoginHandler` (where the admin context is certain) is cleaner.

### Alternative C: httpOnly Cookie Transport

Issue the admin JWT as an httpOnly `Set-Cookie` response header instead of a response body field.

**Rejected**: Requires `credentials: include` CORS configuration and cookie domain alignment between Vercel (frontend) and Fly.io (backend). The `SameSite` cookie attribute would need careful tuning for cross-origin. The existing CORS policy uses `AllowCredentials()` but the cookie domain gap adds deployment complexity. The threat model (sole admin, personal device, HTTPS) does not require httpOnly cookie security. This can be revisited if the threat model changes.

---

## Consequences

**Positive**:
- Admin login path reduced from 4 HTTP round-trips to 1
- Redis eliminated as an admin auth dependency — login is now Redis-independent
- Three use cases, two ports, three adapters, and one endpoint file deleted
- `WebApplicationFactory` simplification: `IAdminTokenStore` stub and keyed `IOAuthClient("admin")` override removed
- Acceptance test step `"Christian is authenticated"` uses `POST /api/auth/login` — no OAuth stub required
- `LoginHandler` unit tests require only `IAdminSettings`, `IPasswordHasher`, `IClock` — simpler test setup

**Negative**:
- JWT issuance is now inlined in `LoginHandler` rather than delegated to a port — one fewer extension point. Acceptable: the admin JWT strategy is unlikely to change independently of the login flow.
- ADR-001 is superseded — the investment in the OAuth flow is discarded. Justified: the OAuth flow introduced a production bug and is being replaced, not just augmented.

## Quality Attribute Impact

| Attribute | Impact |
|---|---|
| Maintainability | Highly positive — ~400 LOC and 8 files removed |
| Testability | Positive — simpler `LoginHandler` dependencies, no OAuth stub needed |
| Security | Neutral — bcrypt + brute-force protection unchanged; JWT isolation maintained (different secret from reader) |
| Reliability | Positive — Redis failure no longer blocks admin login |
| Complexity | Highly positive — accidental complexity eliminated |

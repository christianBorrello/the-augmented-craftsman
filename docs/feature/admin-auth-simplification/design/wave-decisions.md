# Wave Decisions: Admin Auth Simplification — DESIGN Wave

**Feature ID**: admin-auth-simplification
**Wave**: DESIGN (nw-solution-architect)
**Date**: 2026-03-15
**Author**: Morgan (nw-solution-architect)
**Handoff target**: DISTILL wave (nw-acceptance-designer) + DEVOPS wave (nw-platform-architect)

---

## DISCUSS Wave Decisions (Inherited — Confirmed)

The following decisions were made in the DISCUSS wave and are confirmed without modification:

| Decision | Value | Reference |
|---|---|---|
| Full email/password form (not password-only) | Confirmed | DISCUSS wave-decisions.md — Open Question 1 |
| JWT lifetime: 480 minutes, hardcoded | Confirmed | DISCUSS wave-decisions.md — Open Question 2 |
| Keep `IAdminSettings` as interface (port abstraction, 1 property) | Confirmed | DISCUSS wave-decisions.md — Open Question 3 |
| Reader OAuth out of scope | Confirmed | DISCUSS wave-decisions.md — Open Question 4 |

---

## DESIGN Wave Decisions (Resolved Here)

The DISCUSS wave deferred four decisions to DESIGN. All are resolved below.

### D-1: JWT Transport — Response Body (not httpOnly cookie)

**Decision**: Admin JWT is returned in the `POST /api/auth/login` response body. Frontend stores in memory (JavaScript variable) and attaches as `Authorization: Bearer <token>` on admin API requests.

**Options considered**:
1. **Response body + in-memory storage** (chosen)
2. **httpOnly cookie** — Rejected: requires CORS `credentials: include` + cookie domain alignment between Vercel and Fly.io; httpOnly prevents frontend from reading expiry for display; no security benefit given the existing HTTPS + solo-admin threat model
3. **localStorage** — Rejected: XSS risk (low but non-zero); no benefit over in-memory for a page session

**Consequences**:
- Token lost on page refresh — user must re-authenticate after closing the admin tab (acceptable: 8h lifetime covers a writing session; browser password manager handles re-authentication quickly)
- No CORS cookie configuration changes needed
- Consistent with existing `LoginResponse` record shape — no API contract change

**ADR**: ADR-005

---

### D-2: Lockout State Communication — HTTP 429 (existing)

**Decision**: Lockout is communicated via HTTP 429 Too Many Requests. No change to the API. The frontend distinguishes 401 (wrong credentials) from 429 (locked out) by HTTP status code.

**Options considered**:
1. **HTTP 429** (chosen — already implemented in `AuthEndpoints.cs`)
2. **HTTP 401 with body field `"isLockedOut": true`** — Rejected: 401 semantically means "unauthenticated"; mixing lockout state into it requires frontend to parse JSON on every 401
3. **Response header `X-Lockout-Duration`** — Rejected: non-standard; harder to parse in Astro fetch error handling

**Consequences**:
- No API changes required for lockout communication
- Frontend: `if (response.status === 429)` shows lockout message; `if (response.status === 401)` shows invalid credentials

**ADR**: Covered in ADR-005 (auth flow decisions).

---

### D-3: Frontend Form Submit — JavaScript `fetch`

**Decision**: Form submission uses JavaScript `fetch` (not native HTML `<form action="...">` POST).

**Options considered**:
1. **JavaScript `fetch`** (chosen)
2. **Native HTML form action** — Rejected: causes full page navigation on submit, preventing inline error display (AC-02-6) and the 100ms loading state requirement (AC-02-5) without JavaScript interception anyway

**Rationale**: The admin login page already has `prerender = false` (Astro SSR mode). An inline `<script>` is appropriate and expected. Astro pages support client-side scripts; this is the standard Astro pattern for interactive forms.

**Consequences**:
- `<script>` in `login.astro` handles: form submit → fetch → loading state → success redirect / error display
- Zero new dependencies — vanilla JavaScript fetch, no framework

---

### D-4: Email Field Pre-Population — Empty (browser autocomplete only)

**Decision**: The email field renders empty (`value=""`). Browser autocomplete fills it if credentials are saved. The admin email is not hardcoded in source or injected via server-side rendering.

**Options considered**:
1. **Empty + browser autocomplete** (chosen)
2. **Static default via SSR** — Rejected: would require `ADMIN_EMAIL` as a frontend environment variable on Vercel; leaks the admin email into the rendered HTML source; violates the principle that credentials do not appear in frontend source

**Consequences**:
- On a new device, Christian must type his email manually the first time (acceptable — browser saves it thereafter)
- Frontend source contains no admin email

---

### D-5: `ITokenGenerator` Fate — Delete

**Decision**: Delete `ITokenGenerator` (port) and `JwtTokenGenerator` (implementation).

**Options considered**:
1. **Delete** (chosen)
2. **Keep for future use** — Rejected: keeping a no-consumer port is the same class of problem this feature is fixing (dead code). If a reader JWT need arises, the port can be reintroduced with a clear requirement. YAGNI.

**Consequences**:
- `JwtSettings` record is retained (used by `JwtBearerOptions` for reader JWT validation key)
- Only `JwtTokenGenerator` (the implementation class) and `ITokenGenerator` (the interface) are deleted
- `Program.cs` removes one service registration

**ADR**: ADR-007

---

### D-6: Attempt Counter for "1 Remaining" Warning — Frontend State

**Decision**: The backend does not return a remaining-attempts count. The frontend tracks the attempt count locally (in JavaScript memory) and shows the "1 attempt remaining" warning after the 4th failed response (401) within the current page session.

**Rationale**:
- Returning attempt count from the backend exposes account probe state — an attacker could use it to determine how many attempts remain before rotating attacks across IPs
- The AC reads "after 4 failed attempts, the form displays..." — this is observable behavior achievable with pure frontend state
- The backend already correctly returns 401 for < 5 failures and 429 for >= 5 failures — the frontend count is 0..4 (pre-lockout range)
- If the page is refreshed, the count resets — acceptable given the per-session nature of the admin login

**Consequences**:
- AC-03-1 ("1 attempt remaining") is a frontend behavior verified by acceptance tests that control the form interaction count
- No API change

---

## ADRs Produced by This Wave

| ADR | Title | Status |
|---|---|---|
| ADR-005 | Admin JWT Issuance Strategy: Inline in LoginHandler | Accepted |
| ADR-006 | IAdminSettings Simplification: Remove AdminEmail Property | Accepted |
| ADR-007 | ITokenGenerator Deletion: No Remaining Consumers | Accepted |

ADR-001 (Admin OAuth Flow) is superseded by ADR-005. Its status is updated to `Superseded by ADR-005`.

---

## Risks — DESIGN Wave Assessment

| Risk | Assessment | Mitigation |
|---|---|---|
| JWT secret mismatch in test environment | Low — single `IAdminSettings` singleton in DI, both signing and verification consume same instance | Integration checkpoint: acceptance test "walking skeleton" validates end-to-end token round-trip |
| Reader OAuth regression | Low — reader OAuth code untouched; only keyed `IOAuthClient("admin")` removed | `WebApplicationFactory` retains unkeyed `StubOAuthClient`; reader OAuth acceptance tests must run as part of PR validation |
| `StackExchange.Redis` package removal breaks build | Medium — unknown if Redis is used by reader sessions | Crafter must audit remaining Redis consumers before removing NuGet package reference |
| `FailureTracker` single-instance assumption | Accepted risk — in-memory, acceptable for Fly.io single instance | Documented in ADR-006 for future reference |
| Frontend `fetch` CORS | Low — `AllowCredentials()` CORS already configured; `Authorization: Bearer` header is a standard header | No config change needed |

---

## Handoff Notes for DISTILL Wave (Acceptance-Designer)

The acceptance-designer should read these files first, in order:

1. This file (`design/wave-decisions.md`) — decisions and rationale
2. `discuss/acceptance-criteria.md` — the observable behaviors to specify
3. `design/architecture-design.md` — error response contract and auth flow
4. `design/component-boundaries.md` — what changes, what stays
5. `design/data-models.md` — JWT claims and API response shapes

**Key behavioral contracts for BDD scenarios**:

1. `POST /api/auth/login` with correct credentials → 200 OK + JWT containing `role: "admin"` claim
2. `POST /api/auth/login` with wrong credentials → 401 + `{ "error": "Invalid email or password" }`
3. `POST /api/auth/login` after 5 failures → 429 + `{ "error": "Too many failed attempts. Try again in 15 minutes." }`
4. Admin endpoint with valid JWT → 200 OK
5. Admin endpoint without JWT or with expired JWT → 401
6. `GET /admin/oauth/initiate` → 404 (deleted)
7. `GET /admin/oauth/callback` → 404 (deleted)
8. `dotnet build` after cleanup → zero errors

**Step definition update required**: The "Christian is authenticated" background step must call `POST /api/auth/login` with credentials from `WebApplicationFactory` test config, store the returned JWT in `AuthContext.JwtToken`, and use it as Bearer token for subsequent admin requests. The `AdminAuthDriver.AuthenticateAsAdmin()` method must be replaced with an `AuthDriver.AuthenticateAsAdmin()` method that uses the login endpoint.

---

## Handoff Notes for DEVOPS Wave (Platform-Architect)

### Environment Variables to Remove from Fly.io and CI

```
OAUTH__ADMIN__GITHUB__CLIENTID
OAUTH__ADMIN__GITHUB__CLIENTSECRET
REDIS__HOST
REDIS__PASSWORD
ADMIN__EMAIL           (was IAdminSettings.AdminEmail — now AdminCredentials:Email only)
```

### Environment Variables to Retain

```
ADMIN__JWTSECRET                      (IAdminSettings.JwtSecret)
ADMINCREDENTIALS__EMAIL               (AdminCredentials.Email)
ADMINCREDENTIALS__HASHEDPASSWORD      (AdminCredentials.HashedPassword)
JWT__SECRET                           (JwtSettings.Secret — reader JWT)
```

### Infrastructure Change

Redis is no longer required for admin auth. If Redis has no other consumers (e.g., reader session caching), the Redis service configuration can be removed from the deployment pipeline. The production guard in `Program.cs` (`"Redis is required in production"`) must be removed as part of this feature.

### Deployment Concern

This is a **breaking change on deploy**: the old admin OAuth endpoints disappear. Deploying while a browser session is mid-OAuth-flow would strand the OAuth callback. Mitigation: standard deployment (no in-progress OAuth session expected) or coordinate deployment outside writing hours.

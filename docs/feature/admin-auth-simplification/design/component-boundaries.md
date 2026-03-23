# Component Boundaries: Admin Auth Simplification

**Feature ID**: admin-auth-simplification
**Wave**: DESIGN
**Date**: 2026-03-15
**Author**: Morgan (nw-solution-architect)

---

## Hexagonal Architecture Compliance

This feature operates entirely within the existing architecture. No new layers, no new ports, no new adapters. All changes respect the dependency rule: domain and application layers have zero knowledge of infrastructure or API concerns.

```
┌─────────────────────────────────────────────────────────┐
│  Api (Driving Adapter)                                   │
│  AuthEndpoints.cs  [MODIFY — no structural change]       │
│  AdminOAuthEndpoints.cs  [DELETE]                        │
└───────────────────┬─────────────────────────────────────┘
                    │ depends on
┌───────────────────▼─────────────────────────────────────┐
│  Application (Use Cases + Ports)                         │
│                                                          │
│  Features/Auth/Login.cs      [MODIFY — LoginHandler]     │
│  Features/Auth/VerifyAdminToken.cs  [DELETE]             │
│  Features/Auth/InitiateAdminOAuth.cs  [DELETE]           │
│  Features/Auth/HandleAdminOAuthCallback.cs  [DELETE]     │
│                                                          │
│  Ports/Driven/IAdminSettings.cs  [MODIFY — remove prop]  │
│  Ports/Driven/ITokenGenerator.cs  [DELETE]               │
│  Ports/Driven/IAdminTokenStore.cs  [DELETE]              │
└───────────────────┬─────────────────────────────────────┘
                    │ depends on (outward — adapters implement ports)
┌───────────────────▼─────────────────────────────────────┐
│  Infrastructure (Driven Adapters)                        │
│                                                          │
│  Auth/AdminSettings.cs  [MODIFY — remove adminEmail]     │
│  Auth/InMemoryAdminTokenStore.cs  [DELETE]               │
│  Auth/RedisAdminTokenStore.cs  [DELETE]                  │
│  Identity/JwtTokenGenerator.cs  [DELETE]                 │
└─────────────────────────────────────────────────────────┘
```

**Dependency rule**: `Login.cs` (Application) depends on `IAdminSettings` (Application port) — not on `AdminSettings` (Infrastructure). This is unchanged and correct.

---

## Files to DELETE (8 backend + 1 frontend)

| File | Layer | Reason |
|---|---|---|
| `Application/Features/Auth/InitiateAdminOAuth.cs` | Application | Use case with no post-simplification consumer |
| `Application/Features/Auth/HandleAdminOAuthCallback.cs` | Application | Use case with no post-simplification consumer |
| `Application/Features/Auth/VerifyAdminToken.cs` | Application | Use case with no post-simplification consumer; JWT issuance logic migrates to `LoginHandler` |
| `Application/Ports/Driven/IAdminTokenStore.cs` | Application (port) | Port with no post-simplification consumers or implementations |
| `Application/Ports/Driven/ITokenGenerator.cs` | Application (port) | Port with no post-simplification consumers (see ADR-007) |
| `Infrastructure/Auth/InMemoryAdminTokenStore.cs` | Infrastructure | Adapter for deleted port |
| `Infrastructure/Auth/RedisAdminTokenStore.cs` | Infrastructure | Adapter for deleted port |
| `Infrastructure/Identity/JwtTokenGenerator.cs` | Infrastructure | Adapter for deleted port |
| `Api/Endpoints/AdminOAuthEndpoints.cs` | Api | Endpoints for deleted use cases |
| `frontend/src/pages/admin/oauth/callback.astro` | Frontend | Callback page for deleted OAuth flow |

---

## Files to MODIFY

### `Application/Features/Auth/Login.cs`

**What changes**:
- `LoginHandler` constructor: remove `ITokenGenerator tokenGenerator` parameter; add `IAdminSettings adminSettings` parameter
- JWT issuance: replace `tokenGenerator.Generate(command.Email)` with inline JWT construction using `IAdminSettings.JwtSecret`, including `role: "admin"` claim, 480-minute lifetime
- `expiresAt` calculation: change from `now.AddHours(1)` to `now.AddMinutes(480)`
- `LoginResult.Lockout()` message: change from "Too many attempts. Try again in 15 minutes." to align with AC-03-3: "Too many failed attempts. Try again in 15 minutes."

**What does NOT change**:
- `LoginCommand` record shape
- `LoginResult` record shape
- `AdminCredentials` record
- `FailureTracker` class — zero changes
- `AreCredentialsValid` method logic
- Brute-force flow (check lockout → verify → record failure → check lockout again)

**Port dependencies after change**:
- `IPasswordHasher` — retained
- `IAdminSettings` — retained (narrowed: `JwtSecret` only)
- `IClock` — retained
- `AdminCredentials` — retained (value object / config record, not a port)
- `ITokenGenerator` — **removed**

### `Application/Ports/Driven/IAdminSettings.cs`

**What changes**:
- Remove `string AdminEmail { get; }` property
- Retain `string JwtSecret { get; }`

**Post-change interface**:
```
IAdminSettings
  string JwtSecret { get; }
```

This interface remains a valid driven port. `LoginHandler` (Application) depends on `IAdminSettings` (Application port). `AdminSettings` (Infrastructure) implements it. Dependency direction: inward only.

### `Infrastructure/Auth/AdminSettings.cs`

**What changes**:
- Remove `adminEmail` constructor parameter
- Remove `AdminEmail` property implementation

**Post-change constructor**:
```
AdminSettings(string jwtSecret) : IAdminSettings
```

### `Api/Program.cs`

**What changes**:
1. Remove `using StackExchange.Redis;` (if Redis has no other consumers)
2. Remove keyed `IOAuthClient("admin")` registrations (both development and production branches)
3. Remove `builder.Services.AddScoped<InitiateAdminOAuth>()` registration
4. Remove `builder.Services.AddScoped<HandleAdminOAuthCallback>()` registration
5. Remove `builder.Services.AddScoped<VerifyAdminToken>()` registration
6. Remove `IAdminSettings` factory: remove `Admin:Email` config read; retain `Admin:JwtSecret`; simplify to `new AdminSettings(jwtSecret)`
7. Remove entire Redis setup block (`hasRedis` check, `ConfigurationOptions` setup, `IConnectionMultiplexer` registration, `IAdminTokenStore` registrations)
8. Remove `ITokenGenerator` / `JwtTokenGenerator` registration (`builder.Services.AddSingleton<ITokenGenerator, JwtTokenGenerator>()`)
9. Remove `JwtSettings` registration only if no remaining consumer — **do not remove** `JwtSettings` if `JwtBearerOptions` still reads `jwtSettings.Secret` for reader JWT validation (it does — retain `JwtSettings`)
10. Remove `app.MapAdminOAuthEndpoints()` call

**What does NOT change**:
- Reader `IOAuthClient` registration (unkeyed)
- `HandleOAuthCallback`, `CheckSession`, `InitiateOAuth`, `SignOut` registrations
- `JwtBearerOptions` configuration — `IssuerSigningKeys` array retains both `jwtSettings.Secret` and `adminSettings.JwtSecret`
- `AdminCredentials` registration
- `LoginHandler` registration

### `Api/Endpoints/AuthEndpoints.cs`

**What changes**: None. The existing endpoint already returns 429 for lockout and 401 for failure, and returns `LoginResponse(token, expiresAt)` on success. The endpoint contract is correct. The response body change is in `LoginHandler`.

### `tests/Acceptance.Tests/Support/TacBlogWebApplicationFactory.cs`

**What changes**:
1. Remove `TestAdminOAuthEmail` constant (was used for OAuth whitelist)
2. Remove `TestAdminJwtSecret` constant — the admin JWT secret is already in `Admin:JwtSecret` config key; rename to make clear it applies to `LoginHandler` test signing
3. Remove `services.AddKeyedSingleton<IOAuthClient>("admin", ...)` override
4. Remove `IAdminTokenStore` descriptor removal and `InMemoryAdminTokenStore` registration
5. Remove `Admin:Email` from `ConfigureAppConfiguration` in-memory dictionary

**What does NOT change**:
- `TestAdminEmail` / `TestAdminPassword` — used by `LoginHandler` credential validation
- `TestJwtSecret` — reader JWT secret
- The unkeyed `IOAuthClient` override block (reader OAuth)
- `StubOAuthClient` registration
- PostgreSQL container setup
- ImageKit stub

**Key config post-change** (in `WebApplicationFactory`):
```
AdminCredentials:Email         = TestAdminEmail     ("christian.borrello@live.it")
AdminCredentials:HashedPassword = <hashed TestAdminPassword>
Admin:JwtSecret                = TestAdminJwtSecret ("test-admin-jwt-secret-key-minimum-32-characters-long!")
Jwt:Secret                     = TestJwtSecret      (reader JWT)
```

The `Admin:JwtSecret` injected here must match what `LoginHandler` uses to sign. `JwtBearerOptions` reads `adminSettings.JwtSecret` as one of the `IssuerSigningKeys` — so the same secret is used for signing and verification automatically.

### `tests/Acceptance.Tests/Steps/AuthorMode/AuthSteps.cs`

**What changes**:
1. Remove `AdminAuthDriver` dependency — replace with `AuthDriver` (or direct `HttpClient` POST to `/api/auth/login`)
2. Steps that call `_adminAuthDriver.AuthenticateAsAdmin()` (lines 160, 165, 172) must be updated to call the new `_authDriver.Authenticate()` method (or equivalent driver)
3. Steps covering OAuth-specific scenarios (token exchange, used token, SimulateOAuthCallback) are deleted along with the OAuth feature files
4. `GivenChristianIsAuthenticatedAsAdmin` and `GivenChristianHasAValidAdminSession` steps both call `AuthenticateAsAdmin` — both must be updated

**What does NOT change**:
- Steps that assert on admin endpoint behavior (post list, 401 on expired token, etc.) — these are behavior-only and remain valid
- Step text (Gherkin) — the behavioral steps remain; only the driver implementation changes

**Note for crafter**: The `AdminAuthDriver` class itself likely wraps the OAuth callback simulation. If `AdminAuthDriver` only exists to support OAuth, it can be deleted. The replacement is a direct `POST /api/auth/login` call storing the returned JWT in `AuthContext.JwtToken`. The crafter owns the driver class structure.

### `frontend/src/pages/admin/login.astro`

**What changes**:
- Remove: OAuth anchor tag pointing to `googleOAuthUrl`
- Remove: `backendApiUrl` variable and `googleOAuthUrl` construction
- Remove: `error` query param handling for `unauthorized_email`
- Add: HTML form with email input, password input, Sign In button
- Add: `<script>` for fetch-based form submission with loading state, inline error display, and redirect on success
- Add: Lockout state display (429 response → disable button + lockout message)
- Add: Attempt counter logic for "1 attempt remaining" warning after 4 failures

**What does NOT change**:
- Page metadata (`<title>`, `<head>`)
- `export const prerender = false`
- Page layout/structure approach

**Frontend form target**: `POST ${backendApiUrl}/api/auth/login` (cross-origin from Astro to .NET API).

---

## Test Files to Address

### `milestone-1-auth.feature` (if exists)

The DISCUSS wave references this file. If it contains BDD scenarios covering the OAuth flow (token exchange, nonce, single-use token), those scenarios are deleted with the OAuth code. The crafter must:
1. Locate the feature file
2. Delete OAuth-specific scenarios
3. Retain any auth guard scenarios that remain valid (e.g., "admin accesses post list with valid JWT")

New BDD scenarios for the email/password flow will be authored by the acceptance-designer (DISTILL wave) based on the AC in `acceptance-criteria.md`.

### `Application.Tests/LoginShould.cs` (if exists)

If unit tests for `LoginHandler` exist and they stub `ITokenGenerator`, those stubs must be updated to use `IAdminSettings` instead. The crafter owns this as part of the TDD inner loop.

---

## Dependency-Inversion Compliance Check

| Rule | Status |
|---|---|
| `LoginHandler` (Application) depends only on ports (`IPasswordHasher`, `IAdminSettings`, `IClock`) | Compliant — `ITokenGenerator` removal makes this cleaner |
| `IAdminSettings` defined in Application, implemented in Infrastructure | Compliant — unchanged |
| `AdminCredentials` is a value record defined in Application | Compliant — unchanged |
| `AuthEndpoints` (Api) depends on `LoginHandler` (Application) directly | Compliant — use cases registered as singletons in DI |
| No upward dependency (Infrastructure → Application) | Compliant — `AdminSettings` implements `IAdminSettings` inward |
| No Infrastructure type referenced from Application layer | Compliant — `IAdminSettings` is the boundary |

# Shared Artifacts Registry

**Feature**: admin-auth-simplification
**Date**: 2026-03-15
**Journey**: Admin Login

---

## Registry

### admin_jwt_token

| Field | Value |
|---|---|
| **Description** | JWT token issued upon successful admin login |
| **Source of truth** | `Application/Features/Auth/Login.cs` — `LoginHandler` |
| **Owner** | Auth feature (Application layer) |
| **Integration risk** | HIGH — must carry `role: "admin"` claim; must be signed with `IAdminSettings.JwtSecret` |
| **Validation** | Decode token and assert `role == "admin"` claim present; verify signature with admin secret |

**Consumers**:

| Consumer | How used |
|---|---|
| `Api/Endpoints/*` (all admin endpoints) | Bearer token — authorization middleware checks `role: "admin"` |
| `frontend/src/pages/admin/*` (Astro) | Session check — redirects to login if absent or expired |
| `tests/Acceptance.Tests/` step definitions | `"Christian is authenticated"` step now POSTs to `/api/auth/login` to obtain token |

**Risk detail**: The former `ITokenGenerator.Generate(email)` did NOT include the `role: "admin"` claim. The fix is the core behavioral change of this feature. Any regression here silently breaks all admin endpoint access.

---

### jwt_secret (admin)

| Field | Value |
|---|---|
| **Description** | HMAC secret used to sign and verify the admin JWT |
| **Source of truth** | `IAdminSettings.JwtSecret` — loaded from `appsettings.json` / environment variable `ADMIN_JWT_SECRET` |
| **Owner** | `Application/Ports/Driven/IAdminSettings.cs` |
| **Integration risk** | HIGH — signing secret and verification secret must be identical; configuration drift causes silent auth failures |
| **Validation** | Integration test: token signed in `LoginHandler` must be accepted by admin endpoint auth middleware |

**Consumers**:

| Consumer | How used |
|---|---|
| `Application/Features/Auth/Login.cs` | Signs the JWT |
| `Api/` auth middleware / endpoint authorization | Verifies JWT signature |
| `tests/Acceptance.Tests/` `WebApplicationFactory` | Must inject consistent test secret |

**Post-simplification note**: `IAdminSettings` is proposed to retain only `JwtSecret` (removing `AdminEmail` property). See wave-decisions.md — Open Question 3.

---

### admin_email

| Field | Value |
|---|---|
| **Description** | Email address of the sole admin, used for credential validation |
| **Source of truth** | `appsettings.json` / environment variable (e.g., `ADMIN_EMAIL`) — `AdminCredentials` configuration object |
| **Owner** | `Application/Features/Auth/Login.cs` — `LoginHandler` reads from `AdminCredentials` |
| **Integration risk** | MEDIUM — case-sensitivity risk on comparison; must not be exposed in error messages |
| **Validation** | Unit test: credential validation matches configured email exactly (case-insensitive or exact, to be decided in DESIGN) |

**Consumers**:

| Consumer | How used |
|---|---|
| `Application/Features/Auth/Login.cs` | Compared against submitted email |
| `frontend/src/pages/admin/login.astro` | Optional browser autocomplete hint only — NOT hardcoded in source |

---

### admin_password_hash

| Field | Value |
|---|---|
| **Description** | bcrypt hash of the admin password |
| **Source of truth** | `appsettings.json` / environment variable `ADMIN_PASSWORD_HASH` |
| **Owner** | `Application/Features/Auth/Login.cs` — `LoginHandler` (bcrypt verify) |
| **Integration risk** | MEDIUM — hash algorithm version must match bcrypt library; never logged or returned in responses |
| **Validation** | Unit test: `LoginHandler` correctly verifies known plaintext against known hash |

**Consumers**:

| Consumer | How used |
|---|---|
| `Application/Features/Auth/Login.cs` | `bcrypt.Verify(submitted_password, stored_hash)` |

---

### lockout_state

| Field | Value |
|---|---|
| **Description** | Per-admin failure tracking: attempt count + lockout timestamp |
| **Source of truth** | `FailureTracker` (in-memory — acceptable for single-instance deployment on Fly.io) |
| **Owner** | `Application/Features/Auth/Login.cs` or dedicated `FailureTracker` class |
| **Integration risk** | LOW — single admin, single instance. Multi-instance deployments would need distributed state (out of scope for mono-admin blog). |
| **Validation** | Integration test: 5 consecutive failed logins lock the account; 6th attempt is rejected before bcrypt |

**Consumers**:

| Consumer | How used |
|---|---|
| `Application/Features/Auth/Login.cs` | Pre-check on every login attempt |
| `frontend/src/pages/admin/login.astro` | Display lockout message when API returns lockout status |

---

## Removed Artifacts (OAuth cleanup)

The following artifacts are REMOVED by this feature. They must not appear in any surviving file.

| Artifact | Was defined in | Consumers to update |
|---|---|---|
| `admin_oauth_nonce` | `IAdminTokenStore` / Redis | Remove: `VerifyAdminToken`, `AdminOAuthEndpoints` |
| `admin_oauth_state` | `InitiateAdminOAuth` | Remove: `AdminOAuthEndpoints`, callback Astro page |
| `admin_oauth_access_token` (GitHub) | `HandleAdminOAuthCallback` | Remove: all OAuth callback handlers |
| `keyed_oauth_client("admin")` | `Program.cs` DI registration | Remove: keyed `IOAuthClient` registration, `WebApplicationFactory` override |

---

## Integration Checkpoints

1. **Token round-trip test**: A JWT issued by `LoginHandler` must be accepted by a protected admin endpoint in integration tests.
2. **Claim presence test**: Decoded JWT must contain `{ "role": "admin" }` claim.
3. **Secret consistency test**: `IAdminSettings.JwtSecret` value in test `WebApplicationFactory` must match the value used by `LoginHandler`.
4. **OAuth endpoint 404 test**: `GET /admin/oauth/initiate` and `GET /admin/oauth/callback` must return 404 after cleanup.
5. **Reader OAuth isolation test**: Reader OAuth flow must complete without any regression from admin OAuth removal.

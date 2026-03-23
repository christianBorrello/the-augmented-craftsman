# Data Models: Admin Auth Simplification

**Feature ID**: admin-auth-simplification
**Wave**: DESIGN
**Date**: 2026-03-15
**Author**: Morgan (nw-solution-architect)

---

## Overview

This document specifies the data shapes involved in admin authentication post-simplification: the admin JWT claims structure, the `IAdminSettings` port contract, the `AdminCredentials` configuration shape, and the `LoginResult` discriminated union. No database schema changes are required by this feature.

---

## 1. Admin JWT Claims Structure

### Post-Simplification Claims

The JWT issued by `LoginHandler` on successful admin login must carry the following claims:

| Claim | Type | Value | Source |
|---|---|---|---|
| `sub` | Standard (`JwtRegisteredClaimNames.Sub`) | Admin email address | `LoginCommand.Email` (the submitted email, validated against `AdminCredentials.Email`) |
| `email` | Standard (`JwtRegisteredClaimNames.Email`) | Admin email address | Same as `sub` |
| `role` | Custom | `"admin"` | Hardcoded — sole purpose of this claim is admin authorization |
| `jti` | Standard (`JwtRegisteredClaimNames.Jti`) | New GUID per token | Generated at issuance — uniqueness, replay detection |
| `exp` | Standard | `UtcNow + 480 minutes` | `IClock.UtcNow + TimeSpan.FromMinutes(480)` |

### JWT Metadata

| Property | Value |
|---|---|
| Issuer (`iss`) | `"TacBlog"` |
| Audience (`aud`) | `"TacBlog"` |
| Signing algorithm | HMAC-SHA256 (`SecurityAlgorithms.HmacSha256`) |
| Signing key | `IAdminSettings.JwtSecret` (UTF-8 bytes → `SymmetricSecurityKey`) |
| Lifetime | 480 minutes (hardcoded constant) |

**Rationale for `"TacBlog"` issuer/audience**: Matches the existing `JwtBearerOptions` `ValidIssuers` and `ValidAudiences` arrays in `Program.cs`. The admin JWT must pass the same bearer middleware validation as the reader JWT — only the signing key differs (verified via the `IssuerSigningKeys` array containing both secrets).

### Source of Truth for JWT Structure

The JWT construction logic visible in `VerifyAdminToken.cs` (`IssueAdminJwt` private method) is the reference implementation that migrates into `LoginHandler`. The only structural change is:
- `sub` and `email` claims now use `LoginCommand.Email` (validated credential email) instead of `adminSettings.AdminEmail`
- `exp` uses `IClock.UtcNow` (testable) instead of `DateTime.UtcNow` (not mockable)

---

## 2. `IAdminSettings` Port Contract (Post-Simplification)

```
IAdminSettings
│
└── string JwtSecret { get; }
```

**Removed**: `string AdminEmail { get; }` — was used only for the OAuth whitelist check in `HandleAdminOAuthCallback`. With OAuth deleted, this property has no consumer.

**Implementation** (`AdminSettings`):
- Constructor takes `string jwtSecret` only
- `JwtSecret` property returns the injected value

**Configuration source**: `Admin:JwtSecret` in `appsettings.json` or environment variable `ADMIN__JWTSECRET` (ASP.NET Core convention).

**Minimum secret length**: The secret must be sufficient for HMAC-SHA256. A minimum of 32 characters (256 bits) is enforced at startup. The existing production guard already enforces this.

---

## 3. `AdminCredentials` Configuration Shape (Unchanged)

```
AdminCredentials
│
├── string Email          — Admin email; compared case-insensitively in LoginHandler
└── string HashedPassword — ASP.NET Core PasswordHasher v3 (bcrypt-equivalent) hash
```

**No change from current state.** `AdminCredentials` is an application-layer value record. It is populated from configuration at startup:

| Config key | Maps to |
|---|---|
| `AdminCredentials:Email` | `AdminCredentials.Email` |
| `AdminCredentials:HashedPassword` | `AdminCredentials.HashedPassword` |

**Note**: `AdminCredentials.Email` is now the sole location for the admin email in the application. The `Admin:Email` environment variable (previously feeding `IAdminSettings.AdminEmail`) is removed.

---

## 4. `LoginResult` Discriminated Union (Minor Change)

The `LoginResult` record shape is unchanged. The `Lockout()` factory method error message changes to align with AC-03-3:

| Factory method | `IsSuccess` | `IsLockedOut` | `Token` | `ExpiresAt` | `ErrorMessage` |
|---|---|---|---|---|---|
| `Success(token, expiresAt)` | `true` | `false` | JWT string | `DateTime (UTC)` | `null` |
| `Failure(errorMessage)` | `false` | `false` | `null` | `null` | `"Invalid email or password"` |
| `Lockout()` | `false` | `true` | `null` | `null` | `"Too many failed attempts. Try again in 15 minutes."` |

**Change**: `Lockout()` message was `"Too many attempts. Try again in 15 minutes."` — updated to `"Too many failed attempts. Try again in 15 minutes."` to match AC-03-3 exactly.

**`ExpiresAt` semantics**: On `LoginResult.Success`, `ExpiresAt` is `IClock.UtcNow + 480 minutes`. This value is returned in the `LoginResponse` response body so the frontend can display or calculate session expiry.

---

## 5. `LoginResponse` API Contract (No Change)

The API response shape is unchanged:

```
POST /api/auth/login → 200 OK
{
  "token": "<JWT string>",
  "expiresAt": "2026-03-15T17:00:00Z"
}

POST /api/auth/login → 401 Unauthorized
{
  "error": "Invalid email or password"
}

POST /api/auth/login → 429 Too Many Requests
{
  "error": "Too many failed attempts. Try again in 15 minutes."
}
```

**No breaking change to the API contract.** `AuthEndpoints.cs` does not need modification. The response records `LoginResponse` and `ErrorResponse` remain as-is.

---

## 6. Removed Data Structures

The following data types are deleted with their containing files. No migration is needed — none persist to the database.

| Type | File | Reason |
|---|---|---|
| `VerifyAdminTokenResult` | `VerifyAdminToken.cs` | Response type for deleted use case |
| `IAdminTokenStore` | `IAdminTokenStore.cs` | Port for deleted OAuth nonce pattern |
| `InMemoryAdminTokenStore` | `InMemoryAdminTokenStore.cs` | Implementation of deleted port |
| `RedisAdminTokenStore` | `RedisAdminTokenStore.cs` | Implementation of deleted port |

---

## 7. Integration Checkpoint: Secret Consistency

The single most critical integration point is that the secret used to sign the JWT in `LoginHandler` matches the secret used for validation in `JwtBearerOptions`:

```
Signing (LoginHandler):
  IAdminSettings.JwtSecret → SymmetricSecurityKey → SigningCredentials

Validation (JwtBearerOptions in Program.cs):
  IssuerSigningKeys = [
    new SymmetricSecurityKey(jwtSettings.Secret),    ← reader JWTs
    new SymmetricSecurityKey(adminSettings.JwtSecret) ← admin JWTs  ← must match
  ]
```

In the test environment (`WebApplicationFactory`):
- `Admin:JwtSecret` config value injected must match between `LoginHandler` (signing) and `JwtBearerOptions` (verification)
- Both are satisfied by the same `IAdminSettings` singleton resolved from the DI container — there is no drift risk as long as a single `IAdminSettings` registration exists

The acceptance test "walking skeleton" integration checkpoint (token issued by `LoginHandler` accepted by admin endpoint) directly validates this.

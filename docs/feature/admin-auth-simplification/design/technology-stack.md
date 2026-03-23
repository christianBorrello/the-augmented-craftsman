# Technology Stack: Admin Auth Simplification

**Feature ID**: admin-auth-simplification
**Wave**: DESIGN
**Date**: 2026-03-15
**Author**: Morgan (nw-solution-architect)

---

## Overview

This is a brownfield simplification. The technology stack does not change. This document records which components are **retained**, which are **removed**, and the rationale for each removal decision.

No new external dependencies are introduced. This feature reduces the dependency surface.

---

## Retained Components

| Component | Version | License | Role | Rationale |
|---|---|---|---|---|
| .NET 10 / C# 14 | 10.0 | MIT | Runtime | Existing stack — no change |
| ASP.NET Core Minimal API | 10.0 | MIT | HTTP layer | Existing driving adapter — no change |
| Entity Framework Core + Npgsql | 9.x | MIT / Apache 2.0 | Data access | Existing driven adapter — no change |
| `Microsoft.IdentityModel.Tokens` | 8.x | MIT | JWT signing and validation | Already used in `VerifyAdminToken.cs`; JWT issuance migrates to `LoginHandler` using this same library |
| `System.IdentityModel.Tokens.Jwt` | 8.x | MIT | JWT creation | Already used — `JwtSecurityToken`, `JwtSecurityTokenHandler` |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0 | MIT | JWT bearer middleware | Unchanged — validates both reader and admin JWTs via `IssuerSigningKeys` array |
| `BCrypt.Net-Next` (via `IPasswordHasher`) | — | MIT | Password hashing | `FailureTracker` + bcrypt verify in `LoginHandler` — unchanged |
| xUnit + FluentAssertions + NSubstitute | — | Apache 2.0 / Apache 2.0 / BSD-2 | Testing | Unchanged |
| Reqnroll | 2.4 | BSD-3 | BDD acceptance tests | Unchanged — step definitions updated, not the framework |
| Testcontainers.PostgreSql | — | MIT | Integration test infra | Unchanged |
| Astro | 5.x | MIT | Frontend | `login.astro` page modified — framework unchanged |
| Serilog | — | Apache 2.0 | Structured logging | Unchanged |

---

## Removed Components

### `StackExchange.Redis` (admin auth usage)

| Field | Detail |
|---|---|
| **License** | MIT |
| **Was used for** | `RedisAdminTokenStore` — persisting and consuming single-use OAuth nonces |
| **Removed because** | `IAdminTokenStore` port is deleted. Redis was only required to store OAuth nonces between the OAuth callback and the nonce verification step. That step no longer exists. |
| **Impact** | `RedisAdminTokenStore.cs` deleted. `IConnectionMultiplexer` registration removed from `Program.cs`. `Redis:Host` and `Redis:Password` environment variables no longer needed. |
| **Caveat** | Before removing the `StackExchange.Redis` NuGet package reference, the crafter must verify no other consumers exist in the codebase. If reader sessions use Redis, the package reference is retained but the admin-auth registrations are still removed. |

### `IOAuthClient` (keyed "admin" registration)

| Field | Detail |
|---|---|
| **Was used for** | Keyed DI registration `IOAuthClient("admin")` — admin OAuth initiation and callback handling |
| **Removed because** | `InitiateAdminOAuth` and `HandleAdminOAuthCallback` use cases are deleted. No consumer of `IOAuthClient("admin")` remains. |
| **Impact** | Both the development (`DevOAuthClient`) and production (`ProductionOAuthClient`) keyed registrations are removed from `Program.cs`. The unkeyed `IOAuthClient` registration (reader flow) is untouched. |
| **Root cause of crash** | The dual `IOAuthClient` registration (keyed + unkeyed) caused `SingleOrDefault` to throw in `WebApplicationFactory`. Removing the keyed registration eliminates this class of bug. |

### `ITokenGenerator` / `JwtTokenGenerator`

| Field | Detail |
|---|---|
| **Was used for** | `LoginHandler` called `ITokenGenerator.Generate(email)` to produce a reader-style JWT |
| **Removed because** | After this change, `LoginHandler` inlines JWT issuance directly using `IAdminSettings.JwtSecret`. `ITokenGenerator` has no remaining consumers in the Application layer. |
| **Decision** | Delete the port (`ITokenGenerator.cs`) and its implementation (`JwtTokenGenerator.cs`). Keeping a no-consumer interface is dead code — the exact problem this feature fixes. See ADR-007. |
| **Impact** | `ITokenGenerator` registration removed from `Program.cs`. `JwtSettings` record and its registration remain if the reader JWT middleware still requires the secret for validation (it does — `JwtBearerOptions` reads `jwtSettings.Secret` as one of the `IssuerSigningKeys`). `JwtSettings` is retained; only `JwtTokenGenerator` (the implementation) is deleted. |

---

## Configuration Shape (Post-Simplification)

### `appsettings.json` / Environment Variables — Admin Auth Section

```
Admin:JwtSecret         → IAdminSettings.JwtSecret (signing admin JWT)
AdminCredentials:Email  → AdminCredentials.Email (credential validation)
AdminCredentials:HashedPassword → AdminCredentials.HashedPassword (bcrypt verify)
```

### Removed Environment Variables

```
Admin:Email             → was IAdminSettings.AdminEmail — removed (now lives in AdminCredentials:Email)
OAuth:Admin:GitHub:ClientId     → removed
OAuth:Admin:GitHub:ClientSecret → removed
Redis:Host              → removed (admin auth only)
Redis:Password          → removed (admin auth only)
ConnectionStrings:Redis → removed (if admin auth was the sole consumer)
```

---

## No New Dependencies

This feature introduces zero new packages, zero new external services, and zero new infrastructure. All components used in the simplified implementation (`System.IdentityModel.Tokens.Jwt`, `IAdminSettings`, `IClock`, `IPasswordHasher`) already exist in the codebase.

This is consistent with the quality attribute priority for this feature: **Maintainability > Testability > Time-to-market**.

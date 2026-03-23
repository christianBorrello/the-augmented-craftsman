# ADR-006: IAdminSettings Simplification — Remove AdminEmail Property

**Status**: Accepted
**Date**: 2026-03-15
**Feature**: admin-auth-simplification

---

## Context

`IAdminSettings` is a driven port in the Application layer with two properties:

```
IAdminSettings
  string AdminEmail { get; }
  string JwtSecret  { get; }
```

`AdminEmail` was used by `HandleAdminOAuthCallback` to whitelist the admin's email against the email returned by the OAuth provider: only `AdminEmail` could proceed through the OAuth flow to receive an admin JWT.

With ADR-005 accepted (OAuth flow deleted, `LoginHandler` handles admin auth via email/password), `AdminEmail` has no consumer. The email used for login is read from `AdminCredentials.Email` (a configuration record), not from `IAdminSettings`.

The question is: should `AdminEmail` be removed from `IAdminSettings`, or kept for future use?

---

## Decision

Remove `string AdminEmail { get; }` from `IAdminSettings`.

Post-simplification interface:
```
IAdminSettings
  string JwtSecret { get; }
```

`AdminSettings` (the infrastructure implementation) loses the `adminEmail` constructor parameter and `AdminEmail` property.

`Program.cs` factory lambda no longer reads `Admin:Email` from configuration. The `Admin:Email` environment variable is decommissioned.

The admin email lives exclusively in `AdminCredentials.Email`, read from `AdminCredentials:Email` configuration.

---

## Alternatives Considered

### Alternative A: Retain AdminEmail on IAdminSettings

Keep `string AdminEmail { get; }` even though no consumer exists post-simplification.

**Rejected**: A no-consumer property on an interface is dead code. The exact same problem — dead code causing accidental complexity — is the root cause of this entire feature. Keeping it "just in case" violates YAGNI and creates a confusing discrepancy between the interface contract and actual usage. `AdminCredentials.Email` already provides the admin email where it is actually needed.

### Alternative B: Merge AdminCredentials into IAdminSettings

Move `Email` and `HashedPassword` from `AdminCredentials` into `IAdminSettings`, making `IAdminSettings` the single admin configuration port.

**Rejected**: `IAdminSettings.JwtSecret` is a security primitive (signing key). `AdminCredentials.Email` and `HashedPassword` are authentication credentials. Merging them into one interface violates the Interface Segregation Principle — they have different consumers (`JwtSecret` is used for JWT signing; credentials are used for bcrypt verification) and different configuration sources. Keeping them separate maintains clear responsibility boundaries.

### Alternative C: Delete IAdminSettings Entirely — Read Config Directly

Remove `IAdminSettings` and have `LoginHandler` read the JWT secret directly from `IConfiguration` or `IOptions<AdminJwtOptions>`.

**Rejected**: This violates Hexagonal Architecture. The Application layer would depend on `IConfiguration` (an Infrastructure/ASP.NET concern). `IAdminSettings` with one property is not over-engineering — it is the dependency-inversion rule applied to a security credential. It can be stubbed trivially in unit tests: `Substitute.For<IAdminSettings>().JwtSecret.Returns("test-secret")`. The DISCUSS wave explicitly resolved this (Open Question 3).

---

## Consequences

**Positive**:
- `IAdminSettings` interface is minimal and focused — one property, one purpose
- `AdminSettings` constructor is simpler (one parameter)
- `Program.cs` factory lambda is shorter; `Admin:Email` config key is removed
- Eliminates the `Admin:Email` environment variable from deployment configuration
- No ambiguity about where the admin email lives — exclusively in `AdminCredentials`

**Negative**:
- Minor refactor scope: `IAdminSettings`, `AdminSettings`, `Program.cs`, `WebApplicationFactory` config injection all updated
- Developers who knew `IAdminSettings` as a 2-property interface must adjust mental model

**FailureTracker single-instance note**: `FailureTracker` is in-memory state within `LoginHandler`. This is acceptable for the current single-instance Fly.io deployment. If horizontal scaling is introduced, distributed lockout state would be required (e.g., distributed cache, Redis). This is a future concern, not a current risk. Documented here for awareness.

## Quality Attribute Impact

| Attribute | Impact |
|---|---|
| Maintainability | Positive — interface minimalism reduces cognitive load |
| Testability | Unchanged — `IAdminSettings` with 1 property is equally testable as with 2 |
| Security | Positive — cleaner separation between signing secret and credentials |

# ADR-007: ITokenGenerator Deletion — No Remaining Consumers

**Status**: Accepted
**Date**: 2026-03-15
**Feature**: admin-auth-simplification

---

## Context

`ITokenGenerator` is a driven port in the Application layer:

```
ITokenGenerator
  string Generate(string email);
```

`JwtTokenGenerator` (Infrastructure) implements it: it creates a JWT signed with `JwtSettings.Secret`, without a role claim, with `ExpiryInMinutes` from `JwtSettings`.

`LoginHandler` currently depends on `ITokenGenerator` to produce the JWT after a successful login. With ADR-005 accepted, `LoginHandler` inlines JWT issuance using `IAdminSettings.JwtSecret` — `ITokenGenerator` is no longer called.

The question is: with zero consumers in the Application layer, what is the fate of `ITokenGenerator` and `JwtTokenGenerator`?

---

## Decision

Delete `ITokenGenerator` (port) and `JwtTokenGenerator` (infrastructure adapter).

`JwtSettings` (the configuration record) is retained. It is still read by `JwtBearerOptions` in `Program.cs` to supply `jwtSettings.Secret` as one of the `IssuerSigningKeys` for reader JWT validation. Only the `JwtTokenGenerator` class (the generator that used `JwtSettings`) is deleted, not the `JwtSettings` record itself.

Remove the `builder.Services.AddSingleton<ITokenGenerator, JwtTokenGenerator>()` registration from `Program.cs`.

---

## Alternatives Considered

### Alternative A: Keep ITokenGenerator for Future Use

Retain the interface and implementation in case a future feature (e.g., reader JWT issuance from a use case) needs it.

**Rejected**: This is the definition of YAGNI. `ITokenGenerator.Generate(string email)` is a narrow interface tied to email-based JWT issuance without claims or configurable lifetime — it would not be suitable for a use case that required role claims or custom lifetimes without modification. If a JWT generation port is needed in the future, it will be designed to meet that requirement. Keeping the current one would create false confidence that a ready-made solution exists when it may not fit. Dead code is the enemy of maintainability — that is the lesson of this entire feature.

### Alternative B: Move ITokenGenerator to Reader OAuth Flow

Repurpose `ITokenGenerator` for reader session JWT issuance (if the reader flow issues JWTs).

**Rejected**: The reader auth flow uses `IOAuthClient` and `HandleOAuthCallback` — it manages reader sessions via `IReaderSessionRepository` and `ReaderSession` (PostgreSQL-backed), not JWTs. There is no current consumer of `ITokenGenerator` in the reader path. If reader JWTs become a requirement, the interface can be reintroduced with a requirements-driven design.

### Alternative C: Keep JwtSettings Registration Only, Delete ITokenGenerator

Retain `JwtSettings` but delete `ITokenGenerator` and `JwtTokenGenerator`.

**This is the accepted decision.** `JwtSettings` is needed for `JwtBearerOptions` to validate reader JWTs. Only the generator (the thing that used `JwtSettings` to issue tokens) is deleted.

---

## Consequences

**Positive**:
- Two fewer files (`ITokenGenerator.cs`, `JwtTokenGenerator.cs`)
- One fewer DI registration in `Program.cs`
- `LoginHandler` depends on fewer ports — simpler constructor, simpler test setup
- No dead code remaining in the auth infrastructure

**Negative**:
- If a future feature requires JWT generation for a non-admin context, the interface must be reintroduced. This is a 5-minute task with a clear requirement.

**`JwtSettings` note**: `JwtSettings(string Secret, string Issuer, int ExpiryInMinutes = 60)` is retained. It is used only by `JwtBearerOptions` for signing key validation. Its `ExpiryInMinutes` field is now unused (no generator reads it), but removing it from the record would be a minor unrelated refactor — leave it for the crafter to decide on cleanup scope.

## Quality Attribute Impact

| Attribute | Impact |
|---|---|
| Maintainability | Positive — fewer abstractions, no dead ports |
| Testability | Positive — LoginHandler has one fewer dependency to stub |
| Simplicity | Positive — eliminates a delegation chain that provided no benefit |

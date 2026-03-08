# Evolution: Epic 1 - Authentication (US-010)

**Date**: 2026-03-07
**Project ID**: epic1-authentication
**Status**: COMPLETE

## Summary

Implemented admin-only authentication for the blog platform using Outside-In TDD with a full 5-phase cycle (PREPARE, RED_ACCEPTANCE, RED_UNIT, GREEN, COMMIT). The feature allows the blog owner (Christian) to authenticate via email/password and receive a JWT token for protected API operations (creating posts). Public read endpoints remain unauthenticated.

## Architecture

### Hexagonal Architecture Alignment

- **Driven Ports** (Application layer):
  - `IPasswordHasher` — hash and verify passwords
  - `ITokenGenerator` — generate authentication tokens

- **Driven Adapters** (Infrastructure layer):
  - `AspNetPasswordHasher` — wraps ASP.NET Identity `PasswordHasher<string>`
  - `JwtTokenGenerator` — creates HS256 JWT tokens with configurable settings

- **Application Core**:
  - `LoginHandler` — orchestrates credential verification, failure tracking, token generation
  - `FailureTracker` — first-class collection encapsulating rate-limiting (5 attempts / 10 min window, 15 min lockout)
  - `LoginCommand`, `LoginResult`, `AdminCredentials` — immutable records

- **Driving Adapter** (API layer):
  - `AuthEndpoints` — POST /api/auth/login endpoint
  - JWT Bearer middleware on `PostEndpoints` (RequireAuthorization)

### Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Auth scope | Single admin user | Blog has one author; no need for user registration |
| Token type | JWT (HS256) | Stateless, standard, sufficient for single-user |
| Lockout | In-memory FailureTracker | Singleton lifetime; no persistence needed for single-instance |
| Password hashing | ASP.NET Identity PasswordHasher | Battle-tested, PBKDF2 with automatic rehashing |
| Credential storage | appsettings.json | Simple config; dev mode auto-generates credentials |

## Execution Summary

### Phases (5 roadmap phases, 10 steps)

| Phase | Steps | Description |
|-------|-------|-------------|
| 01 | 01-01 | Define driven ports (IPasswordHasher, ITokenGenerator) |
| 02 | 02-01 to 02-04 | LoginHandler TDD cycle: RED unit tests, GREEN implementation, wire adapters + endpoint, review |
| 03 | 03-01 to 03-02 | Account lockout: FailureTracker unit tests + acceptance scenario |
| 04 | 04-01 to 04-02 | JWT middleware: protect POST endpoints, adapt Epic 0 tests |
| 05 | 05-01 | Full regression and validation pass |

### Quality Gates

| Gate | Result |
|------|--------|
| 5-phase TDD (all 10 steps) | PASS (50/50 phases complete) |
| L1-L4 Refactoring | PASS (readability, design, SOLID, Object Calisthenics) |
| Adversarial Review | PASS (with revision: secrets management, boundary tests, tautological test removal) |
| Mutation Testing (Stryker.NET) | PASS (80.77% kill rate, 21/26 mutants killed) |
| Deliver Integrity Verification | PASS (all steps verified complete) |

### Test Coverage

| Project | Tests | Status |
|---------|-------|--------|
| Application.Tests (auth) | 12 tests | GREEN |
| Api.Tests (auth endpoints) | 4 tests | GREEN |
| Acceptance.Tests (auth scenarios) | 5 scenarios | GREEN |
| Domain.Tests (existing) | 27 tests | GREEN |
| Application.Tests (total) | 23 tests | GREEN |

### Mutation Testing Details

- **Tool**: Stryker.NET 4.12.0
- **Scope**: `Features/Auth/**/*.cs`
- **Mutants**: 52 created, 26 testable (4 compile errors, 22 ignored/filtered)
- **Killed**: 21 | **Survived**: 5
- **Score**: 80.77%
- **Surviving mutants**: Boolean mutations in factory methods, statement removal in Record/PruneExpired, boundary equality in lockout duration check

## Files Modified/Created

### New Files (14)
- `src/TacBlog.Application/Features/Auth/Login.cs`
- `src/TacBlog.Application/Ports/Driven/IPasswordHasher.cs`
- `src/TacBlog.Application/Ports/Driven/ITokenGenerator.cs`
- `src/TacBlog.Infrastructure/Identity/AspNetPasswordHasher.cs`
- `src/TacBlog.Infrastructure/Identity/JwtTokenGenerator.cs`
- `src/TacBlog.Api/Endpoints/AuthEndpoints.cs`
- `tests/TacBlog.Application.Tests/Features/Auth/LoginShould.cs`
- `tests/TacBlog.Api.Tests/Endpoints/AuthEndpointsShould.cs`
- `tests/TacBlog.Acceptance.Tests/Drivers/AuthApiDriver.cs`
- `tests/TacBlog.Acceptance.Tests/StepDefinitions/AuthSteps.cs`
- `tests/TacBlog.Acceptance.Tests/xunit.runner.json`

### Modified Files (7)
- `src/TacBlog.Api/Program.cs` — DI wiring, JWT middleware
- `src/TacBlog.Api/Endpoints/PostEndpoints.cs` — RequireAuthorization on POST
- `src/TacBlog.Api/appsettings.Development.json` — JWT settings (no secrets)
- `src/TacBlog.Infrastructure/TacBlog.Infrastructure.csproj` — JWT NuGet packages
- `tests/TacBlog.Acceptance.Tests/TacBlog.Acceptance.Tests.csproj` — Identity package, xunit.runner.json
- `tests/TacBlog.Acceptance.Tests/Hooks/TestHooks.cs` — LoginHandler reset
- `tests/TacBlog.Acceptance.Tests/Support/TacBlogWebApplicationFactory.cs` — test JWT config

## Commits (17)

```
6270074 fix(auth): address adversarial review
155f09c refactor(auth): L1-L4 complete refactoring pass
58e3fa7 feat(auth): full regression and refactor - step 05-01
bd621ac feat(auth): adapt Epic 0 tests for auth - step 04-02
5adda89 feat(auth): add JWT middleware - step 04-01
d6d42fc fix(test): disable parallel execution in acceptance tests
75b7d07 feat(auth): wire lockout 429 response - step 03-02
bcb61d6 feat(auth): implement login lockout - step 03-01
207daba feat(auth): review and commit login scenarios 1-3 - step 02-04
5f8c7e7 feat(auth): wire adapters and endpoint - step 02-03
c41c354 feat(auth): implement LoginHandler - step 02-02
4361ca3 feat(epic1-auth): write LoginShould unit tests (RED) - step 02-01
3b1541e feat(epic1-auth): define driven ports - step 01-01
```

## Issues Encountered

1. **Parallel test race condition**: xUnit default parallelism caused intermittent failures with singleton LoginHandler's in-memory FailureTracker. Fixed by disabling parallel execution in xunit.runner.json.

2. **DES CLI path misalignment**: Python CLI expected `python` (system only has `python3`) and wrote to wrong paths. Orchestrator maintained execution-log.yaml manually.

3. **Stryker initial run failure**: >50% test failures due to pending acceptance tests from unimplemented epics. Fixed by running Stryker from the Application.Tests directory only.

4. **Secrets in dev config**: Adversarial review caught hardcoded admin credentials and JWT secret in appsettings.Development.json. Fixed with auto-generation in dev mode.

## Lessons Learned

- Singleton services with mutable state (FailureTracker) require explicit test isolation — disable parallel execution or use per-test instances
- Stryker.NET needs careful project scoping when the solution has many pending/unimplemented test projects
- Factory method record properties (Success, Lockout) benefit from dedicated assertion tests to improve mutation kill rate
- Dev-mode credential auto-generation prevents accidental secret leakage while keeping the dev experience smooth

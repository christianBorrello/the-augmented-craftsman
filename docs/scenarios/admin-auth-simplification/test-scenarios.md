# Test Scenarios: Admin Auth Simplification

**Feature ID**: admin-auth-simplification
**Wave**: DISTILL (nw-acceptance-designer)
**Date**: 2026-03-15
**Author**: Quinn (nw-acceptance-designer)

---

## Coverage Summary

| User Story | Scenarios | Type | Status |
|---|---|---|---|
| US-01: Admin JWT with role claim | 3 | Happy path + error + regression guard | Feature file |
| US-02: Email/password login form | Covered by walking skeletons + US-01 scenarios | Happy path + error | Feature file |
| US-03: Brute-force lockout feedback | 4 | Boundary + error | Feature file (3 @skip) |
| US-04: Remove admin OAuth dead code | 2 | Error (deleted endpoints) | Feature file (@skip) |
| Cross-story AC-F-2 (reader OAuth regression) | 0 new | Existing reader OAuth scenarios cover this | Existing features |

**Total scenarios**: 12 (2 walking skeleton + 10 focused)
**Error/edge scenarios**: 8 of 12 = **67%** (target: 40%)

---

## Scenario Inventory

### Walking Skeletons (2)

| # | Title | AC Covered | Skip |
|---|---|---|---|
| WS-1 | Christian logs in and can immediately manage posts | AC-01-1, AC-01-4, AC-F-3 | No |
| WS-2 | Unauthenticated request to admin area is turned away | AC-01-5 variant | No |

Both walking skeletons are enabled (no `@skip`). They form the outer RED loop entry point.

### Focused Scenarios (10)

| # | Title | AC Covered | Skip | Type |
|---|---|---|---|---|
| F-01 | Authenticated author accesses the admin post list without interruption | AC-01-4, AC-F-3 | @skip | Happy path |
| F-02 | Login with wrong password is rejected | AC-01-1 (negative) | @skip | Error |
| F-03 | Login with unknown email is rejected | BR-6 (no enumeration) | @skip | Error / security |
| F-04 | Request to admin area with an expired token is turned away | AC-01-5 | @skip | Error |
| F-05 | Fourth failed login attempt warns about one remaining attempt | AC-03-1 | @skip | Boundary |
| F-06 | Fifth failed attempt locks the account for 15 minutes | AC-03-2, AC-03-3 | @skip | Error |
| F-07 | Correct credentials succeed after the lockout period has cleared | AC-03-4, AC-03-5 | @skip | Happy path / boundary |
| F-08 | Account remains locked while lockout period is still active | AC-03-2 | @skip | Error / boundary |
| F-09 | Old admin OAuth initiate endpoint is no longer available | AC-04-1 | @skip | Error |
| F-10 | Old admin OAuth callback endpoint is no longer available | AC-04-2 | @skip | Error |

---

## Acceptance Criteria Coverage Matrix

| AC | Scenario | Notes |
|---|---|---|
| AC-01-1 JWT contains role:admin | WS-1 (`ThenTheIssuedTokenCarriesAdminAuthorisation`) | Decodes JWT, asserts role claim |
| AC-01-2 Signed with IAdminSettings.JwtSecret | WS-1 (implicit — token accepted by admin endpoint) | End-to-end round-trip proves correct secret |
| AC-01-3 480-minute lifetime | Not a BDD scenario | Verified by unit test on LoginHandler; JWT `exp` claim inspectable in crafter's inner loop |
| AC-01-4 Admin endpoints accept the token | WS-1 (`ThenChristianCanAccessTheAdminPostList`) | Direct GET /api/admin/posts with token |
| AC-01-5 Expired token rejected with 401 | F-04 | Expired token fixture injected into AuthContext |
| AC-01-6 Step does not require OAuth infrastructure | WS-1 infrastructure | AuthApiDriver uses POST /api/auth/login; no StubOAuthClient involved |
| AC-02-1 to AC-02-8 | Not BDD scenarios | Frontend-only AC; verified by Playwright/visual E2E if added |
| AC-03-1 4th failure warning | F-05 | Response body assertion |
| AC-03-2 5th failure disables button | F-06 | 429 response confirmed |
| AC-03-3 Lockout message text | F-06 | Response body contains exact message |
| AC-03-4 Re-enables after 15 min | F-07 | 5 failures > 16 min ago → login succeeds |
| AC-03-5 Correct credentials after lockout clears | F-07 | Full login success assertion |
| AC-03-6 No bcrypt during lockout | Not a BDD scenario | Unit test spy on IPasswordHasher in inner loop |
| AC-04-1 /admin/oauth/initiate → 404 | F-09 | GET returns NotFound |
| AC-04-2 /admin/oauth/callback → 404 | F-10 | GET returns NotFound |
| AC-04-3 Build succeeds | Not a BDD scenario | CI pipeline build step |
| AC-04-4 to AC-04-9 | Not BDD scenarios | Static analysis / grep assertions; verified in CI |
| AC-F-1 Complete journey in one session | WS-1 (end-to-end without OAuth redirect) | Passing WS-1 proves this |
| AC-F-2 Reader OAuth unaffected | Existing feature files | reader OAuth scenarios remain in existing AuthorMode tests |
| AC-F-3 Existing admin scenarios use POST /api/auth/login | CommonSteps.GivenChristianIsAuthenticated | Already uses AuthApiDriver.Authenticate() |

---

## Deferred Acceptance Criteria (Not BDD Scenarios)

The following AC are either static analysis concerns (grep/build) or purely frontend UX concerns. They are not modelled as Gherkin scenarios:

- AC-01-3 (480-minute lifetime) — inner loop unit test
- AC-02-1 through AC-02-8 (frontend form) — Playwright E2E if added in future
- AC-03-6 (no bcrypt during lockout) — unit test spy
- AC-04-3 through AC-04-9 (build clean / grep) — CI pipeline assertions
- AC-F-3 (step definition update) — implementation concern for crafter

---

## Implementation Sequence (One-at-a-Time TDD)

The crafter enables scenarios in this order:

1. **WS-1**: Christian logs in and can immediately manage posts (already enabled)
2. **WS-2**: Unauthenticated request to admin area is turned away (already enabled)
3. **F-01**: Authenticated author accesses the admin post list without interruption
4. **F-02**: Login with wrong password is rejected
5. **F-03**: Login with unknown email is rejected
6. **F-04**: Request to admin area with an expired token is turned away
7. **F-05**: Fourth failed login attempt warns about one remaining attempt
8. **F-06**: Fifth failed attempt locks the account for 15 minutes
9. **F-07**: Correct credentials succeed after the lockout period has cleared
10. **F-08**: Account remains locked while lockout period is still active
11. **F-09**: Old admin OAuth initiate endpoint is no longer available
12. **F-10**: Old admin OAuth callback endpoint is no longer available

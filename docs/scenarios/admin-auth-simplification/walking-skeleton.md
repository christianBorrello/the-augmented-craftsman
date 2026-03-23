# Walking Skeleton: Admin Auth Simplification

**Feature ID**: admin-auth-simplification
**Wave**: DISTILL (nw-acceptance-designer)
**Date**: 2026-03-15

---

## Walking Skeleton 1: Christian logs in and can immediately manage posts

### Litmus Test

| Check | Result |
|---|---|
| Title describes user goal | Yes — "logs in and can immediately manage posts" |
| Given/When describe user actions | Yes — login with concrete credentials |
| Then describes user observation | Yes — token carries admin authorisation; post list is accessible |
| Non-technical stakeholder confirms value | Yes — "Christian can log in and see his posts" |

### What This Skeleton Validates

The core assumption of the feature: **the existing `LoginHandler` infrastructure, with the JWT gap closed, is sufficient for admin access**.

Specifically:
- `POST /api/auth/login` with correct credentials returns a JWT
- The JWT contains `role: "admin"` claim
- The JWT is signed with `IAdminSettings.JwtSecret`
- `GET /api/admin/posts` with that JWT returns 200

This is the Release 1 walking skeleton from the story map. Passing it means the outer loop is green and the inner TDD loop (unit tests on `LoginHandler`) has produced a working `LoginHandler`.

### Entry Point (Driving Port)

`POST /api/auth/login` — the HTTP endpoint in `AuthEndpoints.cs`, which calls `LoginHandler.HandleAsync`. All other components (`FailureTracker`, `IPasswordHasher`, `IAdminSettings`, `IClock`) are exercised indirectly as a consequence of invoking through this port.

### Demo Script (for stakeholder)

> "I open the login form, enter my email and password, and click Sign In. I receive a confirmation that I'm logged in, and I can immediately see my post list."

Acceptance test WS-1 mirrors this: it calls the login endpoint, decodes the returned token to verify admin role, then calls the admin post list and confirms a 200 response.

### Status

Enabled — no `@skip` tag. This is the first test the crafter runs.

---

## Walking Skeleton 2: Unauthenticated request to admin area is turned away

### Litmus Test

| Check | Result |
|---|---|
| Title describes user goal | Yes — describes what happens when someone without credentials tries to access the admin area |
| Given/When describe user actions | Yes — no authentication provided, request made to admin area |
| Then describes user observation | Yes — access is denied (401) |
| Non-technical stakeholder confirms value | Yes — "someone without a login cannot access the admin area" |

### What This Skeleton Validates

The auth guard: protected admin endpoints reject requests that carry no JWT. This confirms the `[Authorize]` policy on admin endpoints is correctly configured and that removing the OAuth DI registrations has not broken the authorization middleware.

### Entry Point (Driving Port)

`GET /api/admin/posts` with no `Authorization` header — same driving port as WS-1, different auth precondition.

### Demo Script (for stakeholder)

> "If someone navigates to the admin area without logging in, they are turned away immediately."

### Status

Enabled — no `@skip` tag. Runs together with WS-1 as the outer RED phase.

---

## Relationship to Story Map Walking Skeleton

The story map defined the walking skeleton as:

> 1. `/admin/login` renders with email + password form
> 2. `POST /api/auth/login` receives `{email, password}`
> 3. `LoginHandler` issues JWT with `role: "admin"`, signed with `IAdminSettings.JwtSecret`
> 4. At least one admin endpoint accepts the JWT and returns a protected resource

WS-1 covers steps 2–4 (the API-testable surface). Step 1 (frontend rendering) is a frontend concern testable by Playwright/visual E2E and is deferred. The API walking skeleton is the Release 1 entry point and the correct outer loop starting point for the DELIVER wave.

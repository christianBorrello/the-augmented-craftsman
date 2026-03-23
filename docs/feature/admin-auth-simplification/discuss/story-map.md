# Story Map: Admin Auth Simplification

**User**: Christian — sole author and administrator
**Goal**: Replace the fragile 4-step OAuth admin login with a direct email/password login
**Date**: 2026-03-15

---

## Backbone

The activities follow the admin login journey from left to right. This is a brownfield feature — the walking skeleton is a replacement of existing behavior, not new behavior from scratch.

| Activity 1 | Activity 2 | Activity 3 | Activity 4 |
|---|---|---|---|
| **Access Login Page** | **Submit Credentials** | **Receive Auth Result** | **Access Admin Features** |
| Navigate to /admin/login | POST credentials to /api/auth/login | API validates and responds | Use admin endpoints and dashboard |

---

## Story Map

```
Activity 1          Activity 2          Activity 3          Activity 4
Access Login Page   Submit Credentials  Receive Auth Result Access Admin Features
─────────────────   ──────────────────  ───────────────────  ────────────────────

[Show email/        [Send POST          [Issue JWT with      [Admin endpoints
 password form]      /api/auth/login]    role="admin"]        accept JWT]           ← WALKING SKELETON

─ ─ ─ ─ ─ ─ ─ ─ ─ skeleton line ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─

[Remove OAuth       [Validate against   [Sign with           [Redirect to
 button from        AdminCredentials    IAdminSettings        /admin/dashboard]     ← Release 1
 login page]        (email + bcrypt)]   .JwtSecret]

[Show loading       [Check FailureTracker [Return generic     [Session persists
 state on           before bcrypt]        error — no enum     for 480 min]          ← Release 1
 button submit]                           leak]

                    [Record failed       [Lockout after
                     attempts]            5 failures —
                                          15 min]                                   ← Release 1

                                        [Show lockout
                                          message with
                                          duration]                                 ← Release 1

─ ─ ─ ─ ─ ─ ─ ─ ─ release line ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─

[Delete OAuth       [Delete             [Delete              [Update acceptance
 callback Astro      AdminOAuthEndpoints  InitiateAdminOAuth   test step defs:       ← Release 2
 page]               .cs]                HandleAdminOAuthCallback  "Christian is
                                         VerifyAdminToken.cs   authenticated"        (cleanup)
                                         IAdminTokenStore.cs   uses POST /api/
                                         InMemoryAdminTokenStore auth/login]
                                         RedisAdminTokenStore]

                    [Remove keyed        [Remove IAdminSettings  [Remove
                     IOAuthClient         AdminEmail property]     WebApplicationFactory
                     ("admin")                                     IAdminTokenStore
                     DI registration]                              override]
```

---

## Walking Skeleton

The thinnest end-to-end slice that replaces the broken OAuth admin auth:

1. **Activity 1** — `/admin/login` renders with email + password form (no OAuth)
2. **Activity 2** — `POST /api/auth/login` receives `{email, password}`
3. **Activity 3** — `LoginHandler` issues JWT with `role: "admin"`, signed with `IAdminSettings.JwtSecret`
4. **Activity 4** — At least one admin endpoint accepts the JWT and returns a protected resource

This skeleton validates the core assumption: **the existing `LoginHandler` infrastructure, with the JWT gap closed, is sufficient for admin access**.

---

## Release 1: Full Admin Login Flow

**Target outcome**: Christian can log in with email/password and access all admin features without any OAuth dependency.

Tasks included:
- Replace OAuth login button with email/password form on frontend
- Wire `LoginHandler` to emit `role: "admin"` claim signed with `IAdminSettings.JwtSecret`
- Implement loading state on submit button
- Validate against `AdminCredentials` (email + bcrypt)
- Check `FailureTracker` before bcrypt (lockout-first)
- Record failed attempts
- Return generic error (no enumeration leak)
- Enforce 15-minute lockout after 5 failures
- Show lockout message with duration on frontend
- Session lifetime: 480 minutes
- Redirect to `/admin/dashboard` after success

KPI targeted: **Admin authentication complexity reduced** — single flow, zero external dependencies.

---

## Release 2: OAuth Cleanup

**Target outcome**: All OAuth admin artifacts removed from codebase; no dead code, no zombie DI registrations, no dormant GitHub OAuth app credentials in environment.

Tasks included:
- Delete 8 files (see brief)
- Remove keyed `IOAuthClient("admin")` DI registration from `Program.cs`
- Remove `IAdminTokenStore` DI registration
- Remove `AdminEmail` property from `IAdminSettings`
- Remove `WebApplicationFactory` overrides for removed services
- Update acceptance test step definitions: `"Christian is authenticated"` uses `POST /api/auth/login`
- Verify reader OAuth remains unaffected

KPI targeted: **Codebase simplification** — ~350-400 LOC removed, 4 fewer environment variables.

---

## Dependency Note

Release 2 (cleanup) depends on Release 1 (walking skeleton + full flow) being green. Do not delete OAuth artifacts until the new email/password flow is covered by passing acceptance tests.

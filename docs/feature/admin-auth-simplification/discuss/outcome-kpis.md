# Outcome KPIs: Admin Auth Simplification

**Feature ID**: admin-auth-simplification
**Date**: 2026-03-15

---

## Feature: Admin Auth Simplification

### Objective

Eliminate the accidental complexity of the admin OAuth flow so Christian can authenticate into his blog's admin panel with zero external dependencies, in under one second, by the end of the sprint.

---

### Outcome KPIs

| # | Who | Does What | By How Much | Baseline | Measured By | Type |
|---|---|---|---|---|---|---|
| 1 | Christian (sole admin) | Completes admin login via email/password without OAuth redirect | 100% of login attempts — no OAuth path exists | OAuth flow required 4 HTTP round-trips + GitHub external dependency | Count of login HTTP round-trips in browser DevTools: target = 1 (form POST + redirect) | Leading |
| 2 | Christian (sole admin) | Reaches /admin/dashboard from /admin/login without authentication errors | Zero OAuth-related 5xx/4xx errors post-deployment | OAuth flow produced intermittent failures (documented crash bug) | Admin auth error rate in application logs (target: 0 errors attributable to OAuth admin) | Leading |
| 3 | Codebase (maintainability proxy) | Auth-related files in admin path shrink | 8 files → 1 file; ~350-400 LOC removed | 8 OAuth files + 1 LoginHandler = 9 auth-related files | `git diff --stat` on merge PR; `wc -l` on deleted files | Leading |
| 4 | Christian (sole admin) | Builds and deploys without configuring admin GitHub OAuth app | Zero admin OAuth env vars in production | 4 env vars required (CLIENT_ID, CLIENT_SECRET, CALLBACK_URL, REDIS for admin OAuth) | Production env var count for admin auth: target = 0 OAuth vars (only ADMIN_EMAIL, ADMIN_PASSWORD_HASH, ADMIN_JWT_SECRET) | Leading |

---

### Metric Hierarchy

- **North Star**: Christian completes admin login in 1 HTTP round-trip with zero OAuth dependencies
- **Leading Indicators**:
  - Login form submits directly to `POST /api/auth/login` (no pre-redirect)
  - JWT with `role: "admin"` accepted by all admin endpoints
  - Zero `IAdminTokenStore` / `IOAuthClient("admin")` references in surviving codebase
- **Guardrail Metrics** (must NOT degrade):
  - Reader OAuth flow: still completes successfully (zero regressions)
  - Admin endpoint authorization: still enforces `role: "admin"` (zero unauthorized access)
  - Brute-force protection: lockout after 5 failures still active (FailureTracker unchanged)

---

### Measurement Plan

| KPI | Data Source | Collection Method | Frequency | Owner |
|---|---|---|---|---|
| KPI-1: Login round-trips | Browser DevTools / acceptance test | Count HTTP requests in `"Christian is authenticated"` acceptance test step | Per test run | Acceptance test suite |
| KPI-2: Auth error rate | Application logs (Fly.io) | `grep "admin.*auth.*error\|OAuth.*admin" logs` | Per deployment + 24h post-deploy | Christian (author) |
| KPI-3: File count / LOC | Git diff | `git diff --stat` on the feature PR | At PR creation | Christian (author) |
| KPI-4: Env var count | Production env config | Count of admin-OAuth-specific env vars | At deployment | Christian (author) |

---

### Hypothesis

We believe that fixing `LoginHandler` to emit an admin-role JWT and replacing the OAuth admin frontend with an email/password form will eliminate OAuth-related authentication failures and reduce admin login complexity to a single HTTP interaction.

We will know this is true when:
- Christian completes admin login in 1 form submission (no external redirect)
- Zero admin OAuth environment variables remain in production
- All admin acceptance tests pass using `POST /api/auth/login` as the authentication step

---

### KPI Smell Test

| Check | KPI-1 | KPI-2 | KPI-3 | KPI-4 |
|---|---|---|---|---|
| Measurable today? | Yes — browser DevTools or test assertion | Yes — log grep | Yes — git diff | Yes — env config review |
| Rate not total? | Yes — round-trip count (ratio of actual/expected) | Yes — error rate | N/A — absolute count appropriate (file reduction) | Yes — absolute count appropriate (target: 0) |
| Outcome not output? | Yes — user behavior (completes login) | Yes — system behavior (no errors) | Proxy metric (acceptable for maintenance work) | Yes — environment state change |
| Has baseline? | Yes — 4 round-trips (OAuth) | Yes — documented crash bug | Yes — 9 files | Yes — 4 OAuth env vars |
| Team can influence? | Yes — direct code change | Yes — direct code change | Yes — direct deletion | Yes — direct removal |
| Has guardrails? | Yes — reader OAuth, admin authorization, lockout | Yes — reader OAuth, admin authorization, lockout | Yes — no functionality removed, only dead code | Yes — 3 legitimate admin env vars remain |

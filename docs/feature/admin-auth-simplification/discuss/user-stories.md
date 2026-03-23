<!-- markdownlint-disable MD024 -->
# User Stories: Admin Auth Simplification

**Feature ID**: admin-auth-simplification
**Date**: 2026-03-15

---

## US-01: Admin JWT with Role Claim

### Problem

Christian is the sole author and administrator of The Augmented Craftsman. He finds it impossible to log in to the admin panel using the existing email/password form at `/api/auth/login` because the `LoginHandler` issues a JWT without the `role: "admin"` claim, signed with the wrong secret. Every admin endpoint rejects the token. The OAuth flow — built as a workaround — introduced dual DI registrations, two GitHub OAuth apps, Redis dependency, and a real crash bug.

### Who

- Christian (sole author) | Writing or editing a blog post | Already knows his email and password

### Solution

Fix `LoginHandler` to emit a JWT carrying `role: "admin"` claim, signed with `IAdminSettings.JwtSecret`, with a lifetime of 480 minutes.

### Domain Examples

#### 1: Successful Login on a New Device

Christian opens a new browser profile and navigates to `/admin/login`. He enters `christian@theaugmentedcraftsman.dev` and his password. `LoginHandler` validates credentials, emits a JWT with `{ "role": "admin", "email": "christian@theaugmentedcraftsman.dev" }`, signed with `IAdminSettings.JwtSecret`, expiring in 480 minutes. He is redirected to `/admin/dashboard` and can immediately create a new post.

#### 2: Session Expiry After 8 Hours

Christian logs in at 08:30 and works all morning. At 17:00 (510 minutes later), he tries to save a draft. The request fails with 401 — the JWT has expired. He is redirected to `/admin/login`, logs in again, and the draft is saved.

#### 3: Acceptance Test Stub

In `Acceptance.Tests`, the step `"Christian is authenticated"` sends `POST /api/auth/login` with `{ "email": "christian@test.dev", "password": "testPassword123!" }` and stores the returned JWT as a Bearer token for subsequent admin requests. No OAuth stub, no `IAdminTokenStore` mock required.

### UAT Scenarios (BDD)

#### Scenario: Correct credentials produce JWT with admin role claim
Given Christian is on the admin login page
And the account is not locked out
When Christian enters his correct email and password and clicks "Sign In"
Then a JWT is returned
And the JWT contains the claim role = "admin"
And the JWT is signed with IAdminSettings.JwtSecret
And the JWT has a lifetime of 480 minutes

#### Scenario: Admin JWT is accepted by a protected admin endpoint
Given Christian holds a valid admin JWT containing role = "admin"
When Christian sends a request to GET /admin/posts with the JWT as Bearer token
Then the response is 200 OK
And the admin posts list is returned

#### Scenario: JWT issued by LoginHandler is rejected by admin endpoint without role claim (regression guard)
Given a JWT is issued without the role = "admin" claim
When a request is sent to GET /admin/posts with that JWT as Bearer token
Then the response is 401 Unauthorized

#### Scenario: Expired JWT is rejected
Given Christian holds a JWT that expired 30 minutes ago
When Christian attempts to access /admin/dashboard
Then the request is rejected with 401
And Christian is redirected to /admin/login

#### Scenario: Acceptance test authentication step uses POST /api/auth/login
Given the acceptance test step "Christian is authenticated" runs
When the step executes
Then a POST request is sent to /api/auth/login with Christian's test credentials
And a valid admin JWT is returned
And no OAuth mock or IAdminTokenStore stub is required

### Acceptance Criteria

- [ ] JWT returned by `POST /api/auth/login` contains claim `role: "admin"`
- [ ] JWT is signed with `IAdminSettings.JwtSecret` (not `JwtSettings.Secret`)
- [ ] JWT lifetime is 480 minutes from issuance
- [ ] All existing admin endpoints continue to accept the JWT (no endpoint changes required)
- [ ] Expired JWT (> 480 min old) is rejected with 401
- [ ] Acceptance test step "Christian is authenticated" does not require OAuth infrastructure

### Outcome KPIs

- **Who**: Christian (sole admin)
- **Does what**: Accesses admin features via email/password without OAuth dependency
- **By how much**: Zero OAuth-related authentication failures post-deployment (baseline: intermittent OAuth failures in prod)
- **Measured by**: Admin login error rate in application logs
- **Baseline**: OAuth flow currently requires 2 GitHub apps + Redis, with documented crash bug

### Technical Notes

- Change scope is `LoginHandler` only — specifically the JWT emission section
- `IAdminSettings.JwtSecret` already exists; use it for signing instead of `JwtSettings.Secret`
- Remove dependency on `ITokenGenerator` from `LoginHandler`
- Lifetime: 480 minutes (hardcoded — see wave-decisions.md for configurability decision)
- Dependencies: `IAdminSettings` (existing), `AdminCredentials` (existing), `FailureTracker` (existing)

---

## US-02: Email/Password Admin Login Form

### Problem

Christian is the sole author of The Augmented Craftsman. He finds it jarring to be redirected to GitHub to authenticate into his own blog's admin panel. The OAuth redirect disrupts his writing flow, requires two GitHub OAuth apps to be configured, and has introduced production bugs. There is no reason for the complexity — he just needs to enter his email and password.

### Who

- Christian (sole author) | Starting a writing session | Wants to reach the post editor in seconds, not through an OAuth dance

### Solution

Replace the OAuth login button/redirect on the `/admin/login` Astro page with a standard HTML email/password form that submits to `POST /api/auth/login`.

### Domain Examples

#### 1: Quick Login on a Known Device

Christian sits down to write, opens his browser, navigates to `/admin/login`. His password manager auto-fills email and password. He clicks Sign In, is redirected to `/admin/dashboard` in under a second. He opens the post editor immediately.

#### 2: Manual Login on a New Device

Christian is traveling and logs in from a hotel computer. He sees the email/password form, manually types his credentials, clicks Sign In, is authenticated and redirected to the dashboard. No GitHub app authorization dialog appears.

#### 3: Sign In Button Shows Loading Feedback

Christian clicks Sign In. The button text changes to "Signing in..." and is disabled, preventing double-submit. The API responds in ~250ms (bcrypt cost). The button change is visible within 100ms of click.

### UAT Scenarios (BDD)

#### Scenario: Admin login page shows email and password form only
Given Christian navigates to /admin/login
When the page loads
Then an email input field is visible
And a password input field is visible
And a "Sign In" button is visible
And no OAuth button or GitHub login link is present
And no redirect to an external OAuth provider occurs

#### Scenario: Successful form submission redirects to admin dashboard
Given Christian is on the admin login page
And the account is not locked out
When Christian enters his correct email and password
And submits the form
Then the browser is redirected to /admin/dashboard
And the admin dashboard renders successfully

#### Scenario: Sign In button shows loading state on submit
Given Christian is on the admin login page
When Christian submits the form
Then the Sign In button is disabled within 100ms
And the button text changes to "Signing in..."
And the button re-enables if the request fails

#### Scenario: Form is keyboard-navigable
Given Christian navigates to /admin/login
When Christian presses Tab from the email field
Then focus moves to the password field
And pressing Tab again moves focus to the Sign In button
And pressing Enter on the button submits the form

#### Scenario: Invalid credentials display inline error
Given Christian is on the admin login page
When Christian submits an incorrect password
Then an error message "Invalid email or password" is displayed on the page
And the password field is highlighted as invalid
And the error message does not reveal whether the email address is registered

### Acceptance Criteria

- [ ] `/admin/login` renders email field, password field, and "Sign In" button
- [ ] No OAuth button, GitHub link, or OAuth redirect exists on the page
- [ ] Form submits to `POST /api/auth/login`
- [ ] Successful authentication redirects to `/admin/dashboard`
- [ ] Sign In button enters loading/disabled state within 100ms of click
- [ ] Failed authentication displays "Invalid email or password" inline
- [ ] Form is keyboard-navigable (Tab order: email → password → button)
- [ ] OAuth callback Astro page (`/admin/oauth/callback`) is removed

### Outcome KPIs

- **Who**: Christian (sole admin)
- **Does what**: Reaches the admin dashboard via email/password form without being redirected to GitHub
- **By how much**: Login path reduced from 4 steps (redirect → GitHub → callback → nonce) to 1 step (form submit)
- **Measured by**: Number of HTTP round-trips to complete admin login (observable in browser DevTools)
- **Baseline**: 4 round-trips minimum (initiate → GitHub OAuth → callback → nonce verify)

### Technical Notes

- Target file: `frontend/src/pages/admin/login.astro`
- Remove: OAuth button, redirect logic, `callback.astro` page
- Add: HTML form with `action="/api/auth/login"` (or JS `fetch` — DESIGN wave decision)
- Error display: inline below form (no full page reload required — DESIGN wave decision)
- No CAPTCHA — admin-only internal page behind HTTPS
- Dependencies: US-01 must be complete (endpoint must issue role-bearing JWT)

---

## US-03: Brute-Force Lockout Feedback

### Problem

Christian is the sole admin of The Augmented Craftsman. He occasionally misremembers his password (especially after time away). The `FailureTracker` lockout logic exists and works in the backend, but the frontend shows no useful feedback — Christian does not know whether a lockout is active, how long it lasts, or how many attempts remain. A silent rejection is confusing and alarming.

### Who

- Christian (sole admin) | Struggling to recall correct password | Needs to understand what is happening and when he can try again

### Solution

Surface the brute-force lockout state in the frontend: show remaining attempts before lockout, show lockout message with duration, disable the form during lockout.

### Domain Examples

#### 1: Warning on Fourth Failure

Christian misremembers his password and fails three times. On the fourth attempt, the API still returns a 401 but the frontend receives additional context. The form displays: "1 attempt remaining before account lockout."

#### 2: Lockout After Fifth Failure

Christian fails a fifth time. The form disables the Sign In button and displays: "Too many failed attempts. Try again in 15 minutes." Christian checks the time, waits, and returns to log in successfully.

#### 3: Returning After Lockout Clears

Fifteen minutes later, Christian navigates back to `/admin/login`. The form is enabled, no lockout message is shown. He enters his correct password and logs in successfully.

### UAT Scenarios (BDD)

#### Scenario: Warning appears on fourth failed attempt
Given Christian has failed 3 consecutive login attempts
When Christian submits an incorrect password for the fourth time
Then the error message includes a warning about remaining attempts
And the warning reads "1 attempt remaining before account lockout"

#### Scenario: Lockout activates after fifth failure
Given Christian has failed 4 consecutive login attempts
When Christian submits incorrect credentials again
Then the Sign In button is disabled
And a message reads "Too many failed attempts. Try again in 15 minutes."
And subsequent correct credentials are still rejected during the lockout period

#### Scenario: Form indicates lockout on page load during active lockout
Given the account is locked out
When Christian navigates to /admin/login
Then the Sign In button is disabled
And a lockout message is visible with the duration

#### Scenario: Form re-enables and login succeeds after lockout expires
Given Christian was locked out exactly 15 minutes ago
When Christian enters correct credentials and submits
Then the login succeeds
And a valid admin JWT is issued
And the browser redirects to /admin/dashboard

### Acceptance Criteria

- [ ] After 4th failed attempt, form shows "1 attempt remaining before account lockout"
- [ ] After 5th failed attempt, Sign In button is disabled
- [ ] Lockout message reads "Too many failed attempts. Try again in 15 minutes."
- [ ] Form re-enables after 15-minute lockout period expires
- [ ] Correct credentials succeed after lockout clears
- [ ] No bcrypt comparison occurs during lockout (backend enforced)

### Outcome KPIs

- **Who**: Christian (sole admin)
- **Does what**: Understands lockout state and returns to retry at the correct time rather than refreshing repeatedly
- **By how much**: Zero support incidents or confusion about "login not working" (baseline: N/A — solo author, self-support)
- **Measured by**: Qualitative — author does not need to investigate logs to understand lockout state
- **Baseline**: No lockout feedback on frontend; only raw 401 returned

### Technical Notes

- Backend: `FailureTracker` already exists; API response must include lockout state signal in addition to 401
- DESIGN wave decision: whether lockout state is communicated via HTTP status code (429), response body field, or response header
- Frontend: read lockout signal from API response, update form state accordingly
- Dependencies: US-01, US-02

---

## US-04: Remove Admin OAuth Dead Code

### Problem

Christian maintains The Augmented Craftsman solo. Eight files of admin OAuth infrastructure — use cases, port interfaces, adapters, endpoints, and frontend pages — remain in the codebase after the authentication method changes to email/password. Dead code increases cognitive load, triggers false-positive tool warnings, and was already responsible for one production bug (`SingleOrDefault` crash from dual `IOAuthClient` DI registration). Every day this code exists is a maintenance liability.

### Who

- Christian (sole author and maintainer) | Reviewing or extending the codebase | Encounters OAuth use cases that are no longer called by anything

### Solution

Delete all 8 admin OAuth files, remove their DI registrations from `Program.cs`, simplify `IAdminSettings`, and clean up `WebApplicationFactory`.

### Domain Examples

#### 1: Building the Project After Cleanup

Christian runs `dotnet build`. No warnings about unresolved services, missing implementations, or unused registrations. The build is clean.

#### 2: Reader Login Unaffected

A reader initiates the GitHub OAuth flow for newsletter comments or gating. The reader's `IOAuthClient` (unkeyed) registration is intact. The reader OAuth callback works correctly. No reader-facing behavior changes.

#### 3: Admin Acceptance Test Still Passes

After removing `InMemoryAdminTokenStore`, the acceptance tests still pass. The `"Christian is authenticated"` step now calls `POST /api/auth/login` instead of the OAuth stub. All admin scenarios remain green.

### UAT Scenarios (BDD)

#### Scenario: Deleted OAuth endpoints return 404
Given the admin-auth-simplification feature is fully deployed
When a GET request is made to /admin/oauth/initiate
Then the response status is 404
When a GET request is made to /admin/oauth/callback
Then the response status is 404

#### Scenario: Project builds without OAuth admin DI registrations
Given all admin OAuth files are deleted
And Program.cs no longer registers IAdminTokenStore or keyed IOAuthClient("admin")
When dotnet build is run
Then the build succeeds with zero errors

#### Scenario: Reader OAuth flow completes successfully after admin cleanup
Given admin OAuth artifacts are removed
When a reader initiates the reader OAuth login flow
Then the reader OAuth callback processes correctly
And a reader session is established
And no error occurs related to missing IOAuthClient

#### Scenario: Acceptance test step "Christian is authenticated" passes without OAuth stubs
Given admin OAuth step definitions are updated to use POST /api/auth/login
When the "Christian is authenticated" step runs in acceptance tests
Then the step sends POST /api/auth/login
And receives a valid admin JWT
And no IAdminTokenStore stub is required in WebApplicationFactory

#### Scenario: IAdminSettings contains only JwtSecret after simplification
Given IAdminSettings is simplified to remove AdminEmail
When LoginHandler accesses IAdminSettings
Then only JwtSecret is accessed
And admin email is read from AdminCredentials configuration (not IAdminSettings)

### Acceptance Criteria

- [ ] All 8 listed files are deleted from the repository
- [ ] `Program.cs` contains no registration for `IAdminTokenStore` or keyed `IOAuthClient("admin")`
- [ ] `IAdminSettings` interface contains only `JwtSecret` property
- [ ] `WebApplicationFactory` does not override `IAdminTokenStore` or unkeyed `IOAuthClient`
- [ ] `dotnet build` succeeds with zero errors after cleanup
- [ ] Reader OAuth flow is unaffected (acceptance tests pass)
- [ ] `GET /admin/oauth/initiate` returns 404
- [ ] `GET /admin/oauth/callback` returns 404
- [ ] Acceptance test step "Christian is authenticated" uses `POST /api/auth/login`

### Outcome KPIs

- **Who**: Christian (sole maintainer)
- **Does what**: Navigates the auth codebase without encountering dead use cases or zombie DI registrations
- **By how much**: Auth-related files reduced from 9 (8 OAuth + 1 LoginHandler) to 1 (LoginHandler); ~350-400 LOC removed
- **Measured by**: `git diff --stat` on the PR; `grep -r "AdminTokenStore\|InitiateAdminOAuth\|HandleAdminOAuthCallback\|VerifyAdminToken" --include="*.cs"` returns zero results
- **Baseline**: 8 dead auth files, 4 unnecessary env vars, 2 unnecessary GitHub OAuth apps

### Technical Notes

- Files to delete (exact paths from brief):
  - `Application/Features/Auth/InitiateAdminOAuth.cs`
  - `Application/Features/Auth/HandleAdminOAuthCallback.cs`
  - `Application/Features/Auth/VerifyAdminToken.cs`
  - `Application/Ports/Driven/IAdminTokenStore.cs`
  - `Infrastructure/Auth/InMemoryAdminTokenStore.cs`
  - `Infrastructure/Auth/RedisAdminTokenStore.cs`
  - `Api/Endpoints/AdminOAuthEndpoints.cs`
  - `frontend/src/pages/admin/oauth/callback.astro`
- DI cleanup in `Program.cs`: remove keyed `IOAuthClient("admin")`, `IAdminTokenStore` registrations
- `WebApplicationFactory` cleanup: remove `IAdminTokenStore` and unkeyed `IOAuthClient` descriptor overrides
- `IAdminSettings`: remove `AdminEmail` property
- Step definition update: `Acceptance.Tests` — change `"Christian is authenticated"` step
- Dependencies: US-01, US-02, US-03 must all be green before this story is worked

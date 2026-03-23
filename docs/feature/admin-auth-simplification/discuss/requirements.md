# Requirements: Admin Auth Simplification

**Feature ID**: admin-auth-simplification
**Date**: 2026-03-15
**Author**: Luna (nw-product-owner)
**Status**: Ready for DESIGN wave

---

## Business Context

The Augmented Craftsman is a solo-author blog. Its admin authentication was implemented with a 4-step OAuth flow involving two GitHub OAuth applications, a Redis-backed nonce token store, three use cases, and keyed DI registrations. This produced accidental complexity that caused a real bug (`SingleOrDefault` crash in `WebApplicationFactory` from dual `IOAuthClient` registration).

The decision is to replace the entire admin OAuth flow with the existing `LoginHandler` infrastructure — a standard email/password form backed by bcrypt verification and brute-force protection — correcting only the one gap that made it insufficient: the issued JWT did not carry the `role: "admin"` claim and was not signed with the admin secret.

This is not a new feature. It is a radical simplification of an existing one.

---

## Scope

### In Scope
- Fix `LoginHandler` to emit JWT with `role: "admin"` claim, signed with `IAdminSettings.JwtSecret`, lifetime 480 minutes
- Replace admin OAuth login button/redirect on frontend with email/password form
- Delete all OAuth admin artifacts (8 files, DI registrations, env vars)
- Update acceptance test step definitions to use `POST /api/auth/login`
- Simplify `IAdminSettings` (remove `AdminEmail` property)

### Out of Scope
- Reader OAuth flow (remains unchanged)
- Multi-admin support
- 2FA / TOTP
- Remember-me / persistent sessions
- Any changes to blog post management features

---

## Ubiquitous Language

| Term | Definition |
|---|---|
| **Admin** | Christian — the sole author and administrator. One instance, always. |
| **Admin JWT** | A JWT carrying `role: "admin"` claim, signed with `IAdminSettings.JwtSecret`, valid for 480 minutes |
| **LoginHandler** | The existing application use case at `Application/Features/Auth/Login.cs` that validates email/password and emits a JWT |
| **AdminCredentials** | Configuration object holding the admin email and bcrypt password hash |
| **FailureTracker** | Component tracking consecutive failed login attempts; triggers lockout after 5 failures for 15 minutes |
| **IAdminSettings** | Port interface providing `JwtSecret` to the application layer |
| **Walking Skeleton** | The minimum end-to-end slice: form submit → API → JWT with `role: "admin"` → admin endpoint accepts it |

---

## Functional Requirements

### FR-1: Admin JWT Must Carry Role Claim

**Statement**: When `LoginHandler` processes a successful login, it must issue a JWT containing the claim `role: "admin"`, signed with `IAdminSettings.JwtSecret`, with a lifetime of 480 minutes.

**Current gap**: `ITokenGenerator.Generate(email)` issues a JWT signed with `JwtSettings.Secret` without a role claim. Admin endpoints require `role: "admin"`. This gap makes the existing `LoginHandler` unsuitable for admin access.

**Business rule**: The admin JWT must be signed with a different secret than the reader JWT. This preserves token isolation between the admin and reader authentication domains.

**Examples**:
1. Christian submits `christian@theaugmentedcraftsman.dev` and correct password at 09:00 — receives JWT with `{ "email": "christian@theaugmentedcraftsman.dev", "role": "admin", "exp": [09:00 + 480min] }`, signed with `IAdminSettings.JwtSecret`. Can access `GET /admin/posts` until 17:00.
2. Christian's JWT expires at 17:00 — request to `GET /admin/posts` at 17:01 returns 401. Christian re-authenticates via the login form.
3. Automated test `"Christian is authenticated"` step POSTs to `/api/auth/login` and receives a valid admin JWT in response. No OAuth stub required.

---

### FR-2: Admin Login Page Must Use Email/Password Form

**Statement**: The `/admin/login` page must present an email field and a password field with a "Sign In" button. No OAuth button, link, or redirect must exist on this page.

**Business rule**: The sole admin is Christian. The email field may be pre-filled via browser autocomplete. It must not be hardcoded in source.

**Examples**:
1. Christian opens `/admin/login` in his browser on a new device — sees the email/password form, enters credentials, clicks Sign In, is redirected to `/admin/dashboard`.
2. Christian's browser has saved his admin credentials — the email and password fields auto-fill; he clicks Sign In immediately.
3. A reader who bookmarks `/admin/login` sees only the form; there is no GitHub OAuth button to confuse them.

---

### FR-3: Brute-Force Protection Must Remain Functional and Visible

**Statement**: The existing `FailureTracker` lockout behavior (5 attempts, 15-minute lockout) must remain functional. The frontend must display the lockout status and communicate the lockout duration when it occurs.

**Business rule**: Error messages must be generic — "Invalid email or password" — to prevent email enumeration. The lockout message must communicate duration explicitly.

**Examples**:
1. Christian misremembers his password — types it wrong 3 times. Each attempt returns "Invalid email or password". On the 4th failure, the message adds "1 attempt remaining before account lockout".
2. Christian fails 5 times — the Sign In button is disabled and the message reads "Too many failed attempts. Try again in 15 minutes."
3. After 15 minutes, the form is re-enabled and Christian successfully logs in with the correct password.

---

### FR-4: OAuth Admin Artifacts Must Be Completely Removed

**Statement**: All 8 files listed in the input brief must be deleted. All DI registrations for admin OAuth services must be removed from `Program.cs`. All environment variables for admin OAuth must be removed from documentation and deployment config. The `WebApplicationFactory` must be simplified.

**Business rule**: No dead code. No dormant DI registrations that can cause future bugs (as the `SingleOrDefault` crash demonstrated).

**Examples**:
1. `GET /admin/oauth/initiate` returns 404 after deployment.
2. `GET /admin/oauth/callback` returns 404 after deployment.
3. `dotnet build` produces zero warnings related to unresolved services for `IAdminTokenStore` or keyed `IOAuthClient("admin")`.

---

## Non-Functional Requirements

### NFR-1: Authentication Response Time

Login response from `POST /api/auth/login` must complete in under 500ms under normal conditions. bcrypt is intentionally slow; 200-300ms is expected and acceptable. The UX must show a loading state immediately on submit (within 100ms).

### NFR-2: Security — No Credential Exposure

- Admin password hash must never appear in logs, responses, or error messages.
- JWT secret must never appear in responses.
- Error messages must be generic (no enumeration of valid emails).
- All admin endpoints continue to require Bearer token with `role: "admin"` claim.

### NFR-3: JWT Lifetime

- Admin JWT lifetime: 480 minutes (8 hours).
- Hardcoded in `LoginHandler` — not configurable in initial implementation.
- Rationale: covers a full writing day; KISS; value is stable over the life of a solo-author blog.
- See wave-decisions.md Open Question 2 for full reasoning.

### NFR-4: Accessibility

The admin login form must be keyboard-navigable. Tab order: email → password → Sign In button. Error messages must be programmatically associated with their fields (ARIA or native HTML validation).

### NFR-5: Reader OAuth Isolation

The removal of admin OAuth must not affect the reader OAuth flow in any way. The reader's `IOAuthClient` registration, endpoints, and callback remain intact and pass all existing acceptance tests.

---

## Business Rules

| ID | Rule | Source |
|---|---|---|
| BR-1 | Admin JWT must carry `role: "admin"` claim | Existing admin endpoint authorization requirement |
| BR-2 | Admin JWT must be signed with `IAdminSettings.JwtSecret`, not `JwtSettings.Secret` | Token domain isolation |
| BR-3 | Admin JWT lifetime is 480 minutes | Input brief decision — see wave-decisions.md |
| BR-4 | Login fails after 5 consecutive wrong passwords — 15-minute lockout | Existing `FailureTracker` behavior |
| BR-5 | Lockout check must precede bcrypt verification | Performance and security |
| BR-6 | Error messages must not reveal whether email is registered | Security — anti-enumeration |
| BR-7 | No OAuth button or redirect on admin login page | Simplification goal |
| BR-8 | Reader OAuth flow must not be affected | Scope isolation |

---

## Dependencies

| Dependency | Status | Notes |
|---|---|---|
| `LoginHandler` exists and handles bcrypt | Resolved — exists and works | Only JWT emission needs changing |
| `FailureTracker` exists and works | Resolved — confirmed in brief | No changes needed |
| `IAdminSettings` interface exists | Resolved — exists, provides `JwtSecret` | Remove `AdminEmail` property |
| Admin endpoints use `role: "admin"` for authorization | Resolved — confirmed in brief | No changes to endpoints |
| Astro frontend `/admin/login` page exists | Resolved — exists (OAuth version) | Replace OAuth content with form |

---

## Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| JWT secret mismatch in test environment | Medium | High | Integration checkpoint: token issued by `LoginHandler` accepted by admin endpoint in acceptance tests |
| Reader OAuth regression | Low | High | Acceptance test: reader login flow unaffected after cleanup |
| Forgotten DI registration (zombie service) | Low | Medium | Build verification + startup smoke test |
| Acceptance test step defs not updated | Medium | Medium | Release 2 story (US-04) explicitly covers step def migration |

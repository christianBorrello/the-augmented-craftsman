# Acceptance Criteria Summary

**Feature ID**: admin-auth-simplification
**Date**: 2026-03-15

This document consolidates all acceptance criteria across the four user stories. Each criterion is observable, testable, and derived from a UAT scenario. Technical implementation choices are deferred to the DESIGN wave.

---

## US-01: Admin JWT with Role Claim

- [ ] **AC-01-1** JWT returned by `POST /api/auth/login` with valid credentials contains claim `role: "admin"`
- [ ] **AC-01-2** JWT is signed with `IAdminSettings.JwtSecret` (not `JwtSettings.Secret` or any reader secret)
- [ ] **AC-01-3** JWT lifetime is 480 minutes from issuance
- [ ] **AC-01-4** All existing admin endpoints accept a JWT produced by the updated `LoginHandler`
- [ ] **AC-01-5** A JWT expired beyond 480 minutes is rejected with 401 on all admin endpoints
- [ ] **AC-01-6** The acceptance test step "Christian is authenticated" succeeds using `POST /api/auth/login` without any OAuth stub or `IAdminTokenStore` mock

---

## US-02: Email/Password Admin Login Form

- [ ] **AC-02-1** `/admin/login` renders an email input field, a password input field, and a "Sign In" button
- [ ] **AC-02-2** No OAuth button, GitHub login link, or OAuth redirect mechanism exists on `/admin/login`
- [ ] **AC-02-3** Form submits to `POST /api/auth/login`
- [ ] **AC-02-4** Successful authentication results in browser redirect to `/admin/dashboard`
- [ ] **AC-02-5** Sign In button enters disabled/loading state within 100ms of form submission
- [ ] **AC-02-6** Failed authentication displays "Invalid email or password" inline — no full-page reload required
- [ ] **AC-02-7** Form is keyboard-navigable: Tab order is email → password → Sign In; Enter on button submits
- [ ] **AC-02-8** The page `/admin/oauth/callback` is removed and returns 404

---

## US-03: Brute-Force Lockout Feedback

- [ ] **AC-03-1** After 4 failed attempts, the form displays "1 attempt remaining before account lockout"
- [ ] **AC-03-2** After 5 failed attempts, the Sign In button is disabled
- [ ] **AC-03-3** The lockout message reads "Too many failed attempts. Try again in 15 minutes."
- [ ] **AC-03-4** The form re-enables after 15 minutes have elapsed since lockout began
- [ ] **AC-03-5** Correct credentials submitted after lockout clears result in successful login and redirect
- [ ] **AC-03-6** No bcrypt comparison is performed when the account is locked (backend enforced — verifiable via test spy or log assertion)

---

## US-04: Remove Admin OAuth Dead Code

- [ ] **AC-04-1** `GET /admin/oauth/initiate` returns 404
- [ ] **AC-04-2** `GET /admin/oauth/callback` (API endpoint) returns 404
- [ ] **AC-04-3** `dotnet build` succeeds with zero errors after all 8 files are deleted
- [ ] **AC-04-4** `Program.cs` contains no registration of `IAdminTokenStore`
- [ ] **AC-04-5** `Program.cs` contains no keyed `IOAuthClient` registration with key `"admin"`
- [ ] **AC-04-6** `IAdminSettings` interface contains only `JwtSecret` property (no `AdminEmail`)
- [ ] **AC-04-7** `WebApplicationFactory` does not override `IAdminTokenStore` or unkeyed `IOAuthClient`
- [ ] **AC-04-8** Reader OAuth acceptance tests pass without modification after admin OAuth removal
- [ ] **AC-04-9** `grep -r "AdminTokenStore\|InitiateAdminOAuth\|HandleAdminOAuthCallback\|VerifyAdminToken" --include="*.cs"` returns zero results

---

## Cross-Story Acceptance Criteria (Feature-Level)

- [ ] **AC-F-1** The complete admin login journey (navigate → form → submit → dashboard) completes in a single browser session without any external OAuth provider interaction
- [ ] **AC-F-2** Reader OAuth flow (existing) continues to pass all acceptance tests after this feature is deployed
- [ ] **AC-F-3** All admin endpoint acceptance tests that previously used an OAuth stub now use `POST /api/auth/login` and pass

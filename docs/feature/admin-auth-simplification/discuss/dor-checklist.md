# Definition of Ready Checklist

**Feature ID**: admin-auth-simplification
**Date**: 2026-03-15
**Validator**: Luna (nw-product-owner)

---

## US-01: Admin JWT with Role Claim

| DoR Item | Status | Evidence |
|---|---|---|
| Problem statement clear, domain language | PASS | "LoginHandler issues JWT without `role: 'admin'` claim, signed with wrong secret — admin endpoints reject it" |
| User/persona with specific characteristics | PASS | "Christian — sole author, credentials known, no multi-user scenario" |
| 3+ domain examples with real data | PASS | 3 examples with real email (`christian@theaugmentedcraftsman.dev`), real scenario (new device, session expiry, acceptance test stub) |
| UAT in Given/When/Then (3-7 scenarios) | PASS | 5 scenarios covering: success, endpoint acceptance, regression guard, expiry, acceptance test step |
| AC derived from UAT | PASS | 6 AC items, each traceable to a scenario |
| Right-sized (1-3 days, 3-7 scenarios) | PASS | 1-2 days; single method change in `LoginHandler` + JWT claim addition |
| Technical notes: constraints/dependencies | PASS | Identifies `IAdminSettings`, `AdminCredentials`, `FailureTracker`; notes `ITokenGenerator` removal |
| Dependencies resolved or tracked | PASS | All dependencies exist in codebase; no external blockers |
| Outcome KPIs defined | PASS | KPI-1, KPI-2 in `outcome-kpis.md` |

### DoR Status: PASSED

---

## US-02: Email/Password Admin Login Form

| DoR Item | Status | Evidence |
|---|---|---|
| Problem statement clear, domain language | PASS | "OAuth redirect disrupts writing flow; requires 2 GitHub OAuth apps; caused production bugs" |
| User/persona with specific characteristics | PASS | "Christian — sole author, starting a writing session, wants sub-second access to post editor" |
| 3+ domain examples with real data | PASS | 3 examples: quick login with password manager, manual login on travel device, loading state feedback |
| UAT in Given/When/Then (3-7 scenarios) | PASS | 5 scenarios covering: page render, success redirect, loading state, keyboard navigation, error display |
| AC derived from UAT | PASS | 8 AC items, each traceable to a scenario |
| Right-sized (1-3 days, 3-7 scenarios) | PASS | 1-2 days; Astro page replacement; focused on frontend only |
| Technical notes: constraints/dependencies | PASS | Target file identified; DESIGN wave decisions flagged (fetch vs form action, error display) |
| Dependencies resolved or tracked | PASS | Depends on US-01 (documented) |
| Outcome KPIs defined | PASS | KPI-1 in `outcome-kpis.md` |

### DoR Status: PASSED

---

## US-03: Brute-Force Lockout Feedback

| DoR Item | Status | Evidence |
|---|---|---|
| Problem statement clear, domain language | PASS | "Frontend shows no useful feedback when FailureTracker lockout is active; silent 401 is confusing" |
| User/persona with specific characteristics | PASS | "Christian — sole admin, occasionally misremembers password, needs to understand lockout state" |
| 3+ domain examples with real data | PASS | 3 examples: 4th failure warning, lockout activation, return after 15 min |
| UAT in Given/When/Then (3-7 scenarios) | PASS | 4 scenarios covering: warning, lockout activation, page load during lockout, re-enable after expiry |
| AC derived from UAT | PASS | 6 AC items traceable to scenarios |
| Right-sized (1-3 days, 3-7 scenarios) | PASS | 1 day; `FailureTracker` exists; only API response signal and frontend display needed |
| Technical notes: constraints/dependencies | PASS | DESIGN wave decision flagged (HTTP 429 vs response body); depends on US-01, US-02 |
| Dependencies resolved or tracked | PASS | `FailureTracker` confirmed existing; depends on US-01, US-02 (documented) |
| Outcome KPIs defined | PASS | Qualitative KPI defined in `outcome-kpis.md` |

### DoR Status: PASSED

---

## US-04: Remove Admin OAuth Dead Code

| DoR Item | Status | Evidence |
|---|---|---|
| Problem statement clear, domain language | PASS | "8 dead OAuth files increase cognitive load; dual DI registration caused production crash; every day this exists is a maintenance liability" |
| User/persona with specific characteristics | PASS | "Christian — sole maintainer; encounters dead use cases while reviewing or extending the codebase" |
| 3+ domain examples with real data | PASS | 3 examples: clean build, reader OAuth unaffected, acceptance test passing without stubs |
| UAT in Given/When/Then (3-7 scenarios) | PASS | 5 scenarios covering: 404 endpoints, clean build, reader OAuth, acceptance test step, IAdminSettings simplification |
| AC derived from UAT | PASS | 9 AC items traceable to scenarios |
| Right-sized (1-3 days, 3-7 scenarios) | PASS | 1-2 days; mechanical file deletion + DI cleanup; well-understood scope |
| Technical notes: constraints/dependencies | PASS | Exact file paths listed; DI cleanup scope defined; step def update scope defined |
| Dependencies resolved or tracked | PASS | Explicitly depends on US-01 + US-02 + US-03 being green (documented) |
| Outcome KPIs defined | PASS | KPI-3, KPI-4 in `outcome-kpis.md` |

### DoR Status: PASSED

---

## Overall Feature DoR Status: ALL PASSED

All four stories pass all 9 DoR items. The feature package is ready for handoff to the DESIGN wave.

# Prioritization: Admin Auth Simplification

**Feature**: admin-auth-simplification
**Date**: 2026-03-15

---

## Release Priority

| Priority | Release | Target Outcome | KPI | Rationale |
|---|---|---|---|---|
| 1 | Walking Skeleton | End-to-end email/password admin login works | Admin JWT with `role: "admin"` issued and accepted by admin endpoint | Validates the core assumption: LoginHandler gap can be closed without architectural change |
| 2 | Release 1: Full Flow | Complete admin login with brute-force protection, UX feedback, and session lifetime | Zero OAuth dependencies in admin auth path | Fully replaces OAuth for the only admin user |
| 3 | Release 2: Cleanup | All OAuth admin artifacts removed; ~350-400 LOC deleted | 4 env vars removed, 1 GitHub OAuth app retired | Eliminates dead code, removes dual DI registration bug source |

---

## Backlog Suggestions

> **Note**: Story IDs below are Phase 2.5 placeholders. Final IDs are assigned in Phase 4 (Requirements).
> Revisit after `user-stories.md` is produced.

| Story | Release | Priority | Outcome Link | Dependencies |
|---|---|---|---|---|
| US-01: Admin JWT with role claim (backend) | Walking Skeleton | P1 | `admin_jwt_token` carries `role: "admin"` | None — LoginHandler exists |
| US-02: Email/password login form (frontend) | Release 1 | P2 | Zero OAuth on frontend | US-01 (needs endpoint to call) |
| US-03: Brute-force protection visible feedback | Release 1 | P2 | Lockout UX accessible to sole admin | US-01 |
| US-04: OAuth admin code deletion | Release 2 | P3 | Codebase size -350 LOC | US-01 + US-02 + US-03 green |
| US-05: Acceptance test step def migration | Release 2 | P3 | Test suite passes without OAuth stubs | US-04 (stubs no longer needed) |

---

## Value / Effort / Urgency Scoring

Scale 1-5. Priority Score = (Value x Urgency) / Effort.

| Story | Value | Urgency | Effort | Priority Score | Notes |
|---|---|---|---|---|---|
| US-01: Admin JWT with role claim | 5 | 5 | 2 | 12.5 | Core gap fix; small change in LoginHandler |
| US-02: Login form (frontend) | 4 | 5 | 2 | 10.0 | Replace OAuth button with form; Astro page change |
| US-03: Brute-force UX feedback | 3 | 3 | 1 | 9.0 | FailureTracker already exists; frontend display only |
| US-04: OAuth code deletion | 4 | 4 | 2 | 8.0 | High value (removes bug source); moderate effort (careful file deletion + DI unwiring) |
| US-05: Step def migration | 3 | 4 | 1 | 12.0 | Quick win; unblocks clean test suite |

---

## Riskiest Assumption Validation

| Assumption | Risk Level | Validated By | Status |
|---|---|---|---|
| `LoginHandler` issues JWT when given correct credentials | HIGH | Walking skeleton acceptance test | Validates in Release 1 |
| Adding `role: "admin"` claim to existing JWT does not break other flows | MEDIUM | Integration test: admin endpoints accept new JWT format | Validates in Walking Skeleton |
| Reader OAuth is completely independent of admin OAuth | MEDIUM | Acceptance test: reader login flow unaffected | Validates in Release 2 cleanup |
| `FailureTracker` in-memory state is sufficient for single-instance Fly.io | LOW | Accepted risk — single admin, single instance | Accepted (see wave-decisions.md) |
| 480-minute JWT lifetime is acceptable security tradeoff for a solo author | LOW | Sole author decision — see wave-decisions.md | Decision recorded |

---

## MoSCoW Classification

| Category | Stories |
|---|---|
| Must Have | US-01 (JWT fix), US-02 (login form) — without these, admin access is broken |
| Should Have | US-03 (brute-force UX) — FailureTracker logic exists; UX feedback needed |
| Should Have | US-04 (code deletion) — removes bug-prone dual DI; reduces maintenance surface |
| Could Have | US-05 (step def migration) — test infrastructure cleanup; important but not blocking |
| Won't Have | Multi-admin support, TOTP/2FA, remember-me — out of scope for mono-admin blog |

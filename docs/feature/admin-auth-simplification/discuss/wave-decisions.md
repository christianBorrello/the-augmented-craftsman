# Wave Decisions: Admin Auth Simplification

**Feature ID**: admin-auth-simplification
**Wave**: DISCUSS (nw-product-owner)
**Date**: 2026-03-15
**Author**: Luna (nw-product-owner)

---

## Prior Wave Files Checked

| File | Status |
|---|---|
| `docs/project-brief.md` | Not found (⊘) |
| `docs/stakeholders.yaml` | Not found (⊘) |
| `docs/architecture/constraints.md` | Not found (⊘) |
| `docs/feature/admin-auth-simplification/discover/` | Not found (⊘) — no DISCOVER wave ran |

All context sourced from the input brief and `CLAUDE.md`.

---

## Wave Configuration Decisions (from brief)

| Decision | Value | Rationale |
|---|---|---|
| Feature type | Cross-cutting | Spans API backend, frontend, DI configuration, test infrastructure |
| Walking skeleton | Brownfield | Existing infrastructure; this is a deletion + gap-fix refactor |
| UX research depth | Lightweight | Single known user, clear flows, happy-path focused |
| JTBD analysis | Skipped | User motivations crystal clear; competing jobs absent; single actor |

---

## Open Questions — Resolved

### Open Question 1: Login form design — full email/password form vs. password-only field?

**Question (from brief)**: Does the frontend admin login need a dedicated email/password form, or is a simple password field enough (email is fixed — only one admin)?

**Luna's recommendation**: Full email/password form.

**Reasoning**:
- Aligns with browser credential managers (password managers auto-fill by email + domain pair; password-only fields often break auto-fill)
- Aligns with `LoginHandler`'s existing API contract — it expects `{email, password}` in the request body
- More conventional and recognizable — reduces cognitive surprise if Christian returns after months away
- The email field can be pre-filled by browser autocomplete or left blank for manual entry; it does not need to be hardcoded in the frontend source

**Status**: Resolved — reflected in US-02 and all frontend mockups.

**Confirmation needed**: No. This decision is consistent with KISS, existing API contract, and browser UX standards. The DESIGN wave may choose to pre-populate the email field if it simplifies the UX further — that is a DESIGN decision.

---

### Open Question 2: JWT admin lifetime — 480 min fixed or configurable?

**Question (from brief)**: Should the admin JWT lifetime differ from 480 minutes? Should it be configurable?

**Luna's recommendation**: 480 minutes, hardcoded in `LoginHandler` for the initial implementation. Not configurable.

**Reasoning**:
- 480 minutes (8 hours) covers a full writing day without re-authentication — appropriate for a solo author on a trusted personal device
- Configurability adds a `JwtLifetimeMinutes` property to `IAdminSettings`, a new environment variable, validation logic, and test coverage — all for a value that will almost certainly never change
- KISS principle (CLAUDE.md): "Technologies are tools chosen based on the problem." Configurability is the wrong tool here — the problem is "I need to authenticate once per writing day," which 480 minutes solves permanently
- If the need to change this ever arises, it is a 2-line code change — not a configuration change

**Status**: Resolved — hardcoded 480 minutes reflected in US-01 technical notes and requirements.md FR NFR-3.

**Confirmation needed**: No. If Christian wants a different value (e.g., shorter for security reasons, longer for convenience), the DESIGN wave can adjust the constant. Flag this to solution-architect.

---

### Open Question 3: IAdminSettings with only JwtSecret — keep interface or read directly from config?

**Question (from brief)**: Does `IAdminSettings` with only `JwtSecret` still justify its existence as an interface, or should `LoginHandler` read the secret directly from configuration?

**Luna's recommendation**: Keep `IAdminSettings` as an interface with `JwtSecret` only.

**Reasoning**:
- Hexagonal Architecture principle (CLAUDE.md): application layer depends on port abstractions, not infrastructure concretions. `IAdminSettings` is a driven port — removing it would make `LoginHandler` depend directly on `IConfiguration` or `IOptions<>`, which are infrastructure concerns
- Testability: `IAdminSettings` can be mocked in unit tests trivially; direct config access requires test configuration setup
- The interface shrinks from 2 properties to 1 — that is simplification, not overhead. A 1-property interface is not inherently wrong when it represents a domain boundary
- Removing the `AdminEmail` property (moved to `AdminCredentials` config object) is correct — the email is a credential, not an admin setting

**Status**: Resolved — reflected in US-04 AC-04-6 and requirements.md FR-4.

**Confirmation needed**: No. This is consistent with the project's architectural principles.

---

### Open Question 4: Reader OAuth simplification opportunities?

**Question (from brief)**: The reader OAuth remains unchanged — is there anything in the reader flow that can be simplified as a consequence?

**Luna's recommendation**: No simplification in this feature. Reader OAuth is out of scope.

**Reasoning**:
- The dual `IOAuthClient` registration issue was the root cause of the `WebApplicationFactory` crash. With the keyed `IOAuthClient("admin")` removed, the reader's unkeyed `IOAuthClient` registration is the only one — the `SingleOrDefault` → `Where` fix (from the recent commit) becomes unnecessary, but it is also harmless. No rollback needed.
- Reader OAuth has a different purpose (commenter/newsletter gate), different callback URL, and different token lifecycle. Simplifying it is a separate feature requiring its own discovery
- Mixing reader and admin auth changes in the same feature increases risk and complicates rollback

**Status**: Resolved — reader OAuth explicitly excluded from scope. Noted as potential future feature if reader OAuth complexity becomes a pain point.

**Confirmation needed**: No. This is a clear scope boundary.

---

## DESIGN Wave Decisions (deferred — not Luna's to make)

The following decisions are deliberately left open for the solution-architect (DESIGN wave):

| Decision | Options | Constraint from Requirements |
|---|---|---|
| How admin JWT is transported to the browser | httpOnly cookie vs. Authorization header + localStorage | Must be inaccessible to XSS if possible; DESIGN decides |
| Error state communication from API to frontend | HTTP 429 (lockout) vs. 401 with body field vs. response header | Lockout state must be communicable; method is DESIGN's choice |
| Frontend form submit mechanism | Native HTML form action vs. JS `fetch` | Must show loading state within 100ms; DESIGN decides |
| Email field pre-population | Static default vs. empty field | Must not hardcode in source; DESIGN decides |

---

## Risks Surfaced for DESIGN Wave

| Risk | Notes |
|---|---|
| JWT secret configuration drift | `IAdminSettings.JwtSecret` must be identical at signing (LoginHandler) and verification (middleware). If test and production configs differ, auth silently breaks. Integration checkpoint in `shared-artifacts-registry.md`. |
| Reader OAuth regression | Acceptance tests covering reader OAuth must run as part of feature validation. Confirm reader OAuth test coverage exists before merging US-04. |
| `FailureTracker` single-instance assumption | In-memory state is accepted for current single-instance Fly.io deployment. If the blog ever scales to multiple instances, distributed lockout state would be needed. Not a current risk — document for future. |

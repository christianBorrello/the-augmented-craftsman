# Wave Decisions: Admin Auth Simplification — DISTILL Wave

**Feature ID**: admin-auth-simplification
**Wave**: DISTILL (nw-acceptance-designer)
**Date**: 2026-03-15
**Author**: Quinn (nw-acceptance-designer)
**Handoff target**: DELIVER wave (nw-software-crafter)

---

## Prior Wave Decisions Inherited

All DISCUSS and DESIGN wave decisions are confirmed. The following influenced scenario design directly:

| Decision | Impact on BDD Scenarios |
|---|---|
| JWT transport: response body | Steps decode `token` field from response JSON; no cookie handling |
| Lockout via HTTP 429 | `ThenTheLoginIsRejectedWithReason` accepts both 401 and 429; exact message verified |
| Frontend attempt counter: frontend-only state | AC-03-1 (4th failure warning) is tested at the response level, not via a distinct 401 body field |
| JWT lifetime: 480 minutes hardcoded | AC-01-3 is a unit test concern; no BDD scenario required |
| `ITokenGenerator` deleted | No scenarios reference it; `AuthApiDriver` calls `POST /api/auth/login` directly |

---

## DISTILL Wave Decisions

### DD-1: Feature file location — new `Epic1_AdminAuth/` directory

**Decision**: New feature files live in `Features/Epic1_AdminAuth/`, separate from `Features/AuthorMode/` (which contains the OAuth-era auth tests).

**Rationale**:
- `Features/AuthorMode/milestone-1-auth.feature` contains OAuth-specific scenarios that will be deleted as part of US-04. Placing the new scenarios in the same file would create conflicts during the transition period.
- A dedicated epic directory follows the existing naming convention (`Epic0_WalkingSkeleton`, `Epic1_PostManagement`, etc.).
- The old `milestone-1-auth.feature` is left in place until the crafter confirms the OAuth code is deleted (Release 2); it serves as the regression guard for the transition period.

### DD-2: Existing step reuse — no re-declaration

**Decision**: Steps already bound in `StepDefinitions/AuthSteps.cs` and `StepDefinitions/CommonSteps.cs` are reused. `AdminLoginSteps.cs` adds only net-new step bindings.

**Reused steps (not re-declared)**:
- `[When("Christian logs in with email {string} and password {string}")]` — `AuthSteps.cs:20`
- `[Given("no authentication is provided")]` — `CommonSteps.cs:27`
- `[Given("Christian has failed login {int} times in the last {int} minutes")]` — `AuthSteps.cs:26`

**New steps added in `AdminLoginSteps.cs`**:
- `[Given("Christian has logged in successfully")]`
- `[Given("Christian holds a token that expired 30 minutes ago")]`
- `[When("a request is made to the admin post list")]`
- `[When("Christian accesses the admin post list")]`
- `[When("a request is made to the admin OAuth initiate path")]`
- `[When("a request is made to the admin OAuth callback path")]`
- `[Then("the login succeeds")]`
- `[Then("the issued token carries admin authorisation")]`
- `[Then("Christian can access the admin post list")]`
- `[Then("access is denied")]`
- `[Then("the login is rejected with reason {string}")]`
- `[Then("no token is issued")]`
- `[Then("the endpoint is not found")]`

**Note**: The existing `Steps/AuthorMode/AuthSteps.cs` defines `[Then("the post list is returned normally")]` — reused in scenario F-01 without re-declaration.

### DD-3: `"Christian accesses the admin post list"` vs `"Christian requests the admin post list"`

**Decision**: Use `"Christian accesses the admin post list"` for the new scenarios. The existing `"Christian requests the admin post list"` in `Steps/AuthorMode/AuthSteps.cs:187` calls `GetAdminPosts()` on `PostApiDriver`. The new step calls `GetPostsByStatus("Draft")` on `AdminPostDriver`.

**Rationale**: Slight wording difference avoids a binding conflict while retaining business language. The crafter should note that both steps hit `GET /api/admin/posts` — they can be unified in a future cleanup if desired.

### DD-4: AC-03-1 (fourth failure warning) — response body assertion, not client-side count

**Decision**: Scenario F-05 ("Fourth failed login attempt warns about one remaining attempt") tests only that the fourth rejection still returns a valid 401 with the standard error message. The "1 attempt remaining" UI text is a frontend state machine concern (DESIGN wave decision D-6).

**Rationale**: The API does not return a remaining-attempt count. There is no server-side observable to assert on at the API level beyond a 401 response. The scenario documents the behavioral boundary: after 3 failures, the 4th still returns 401 (not 429 yet). The frontend layer reads this as "4th failure" and shows the warning. This is correctly tested at the frontend E2E layer (Playwright), not in the BDD API acceptance tests.

### DD-5: AC-01-2 (signed with IAdminSettings.JwtSecret) — implicit via round-trip

**Decision**: No dedicated scenario asserts the signing key identity. The round-trip in WS-1 proves it: `LoginHandler` signs with `IAdminSettings.JwtSecret`; the test admin secret is injected by `TacBlogWebApplicationFactory` as `Admin:JwtSecret`; `JwtBearerOptions` uses the same secret for verification. If the secrets diverged, the admin post list call in `ThenChristianCanAccessTheAdminPostList` would return 401, failing the scenario.

**Rationale**: Explicit secret-identity assertion would require exposing infrastructure details (key material) in the step — a business language violation. The round-trip is the correct observable test.

### DD-6: Walking skeletons enabled immediately (no @skip)

**Decision**: Both WS-1 and WS-2 are enabled from day one. All 10 focused scenarios carry `@skip`.

**Rationale**: Per the project's Outside-In Double Loop TDD approach: the two walking skeletons define the outer RED phase. The crafter starts with these failing, drops to the inner loop (`LoginHandler` unit tests), and works until both pass. Then removes `@skip` from F-01, and so on.

---

## Mandate Compliance Evidence

### CM-A: Hexagonal Boundary Enforcement

All test code invokes through driving ports exclusively:
- `AuthApiDriver.Login()` → `POST /api/auth/login`
- `AdminPostDriver.GetPostsByStatus()` → `GET /api/admin/posts?status=...`
- `AdminPostDriver.RequestEndpoint()` → arbitrary HTTP verb + path (for 404 assertions)

No internal components are referenced from test code. `AuthContext`, `ApiContext` are test-only context holders with no production type dependencies.

### CM-B: Business Language Purity

Gherkin terms audit:
- "logs in" — domain term
- "email and password" — domain term
- "admin authorisation" — domain term (not "role claim" or "JWT")
- "access is denied" — domain term (not "401 Unauthorized")
- "endpoint is not found" — borderline; acceptable because it describes a deleted feature's observable removal
- "token" — domain term in the context of a login system (used by stakeholders)

No HTTP verbs, status codes, JSON, database terms, or infrastructure names in Gherkin.

Step methods use `HttpStatusCode` enum values internally — these are invisible to Gherkin. Business language boundary is maintained.

### CM-C: Walking Skeleton + Focused Scenario Counts

- Walking skeletons: 2 (WS-1, WS-2)
- Focused scenarios: 10 (F-01 through F-10)
- Total: 12
- Error/edge ratio: 8/12 = 67% (target: 40%)

---

## Peer Review

**Dimension 1 (Happy Path Bias)**: Pass — 67% error/edge scenarios.

**Dimension 2 (GWT Format)**: Pass — all scenarios follow Given → single When → Then. No conjunction steps in When position.

**Dimension 3 (Business Language Purity)**: Pass — zero technical terms in Gherkin. Step assertions use `HttpStatusCode` enum values internally, invisible to the feature file.

**Dimension 4 (Coverage Completeness)**: Pass — all testable AC mapped. Non-BDD AC (build, grep, frontend) documented with rationale.

**Dimension 5 (Walking Skeleton User-Centricity)**: Pass — WS-1 title "Christian logs in and can immediately manage posts" describes a user goal, not technical connectivity. Then steps describe user-observable outcomes.

**Dimension 6 (Priority Validation)**: Pass — scenarios address the largest gap (broken admin login) directly. The walking skeleton is the Release 1 entry point per the story map.

**Approval status**: approved

---

## Peer Review (External)

**Review Date**: 2026-03-15
**Reviewer**: Sentinel (nw-acceptance-designer-reviewer)
**Verdict**: APPROVED — Initial verdict was NEEDS_REVISION; both reported blockers resolved on investigation.

### Dimension Scores
- Happy Path Bias: 9/10 — 67% error/edge coverage exceeds 40% target
- GWT Format Compliance: 10/10 — all scenarios properly structured
- Business Language Purity: 9/10 — zero technical jargon in Gherkin; HTTP details abstracted
- Coverage Completeness: 9/10 — all testable AC mapped; deferred items justified
- Walking Skeleton User-Centricity: 10/10 — both skeletons express user goals; correct ratio
- Priority Validation: 8/10 — correctly prioritized; story map reference provided

**Average**: 9.2/10 — Excellent

### Mandate Compliance
- CM-A (Hexagonal Boundary): PASS — All tests invoke through HTTP endpoints
- CM-B (Business Language Abstraction): PASS — Gherkin pure; business outcomes asserted
- CM-C (User Journey Completeness): PASS — 2 skeletons + 10 focused scenarios; ratio correct

### Reported Issues — Resolution

**Issue 1 (False Positive)**: Reviewer reported `[Then("the post list is returned normally")]` as missing.
**Resolution**: Binding exists at `Steps/AuthorMode/AuthSteps.cs:193`. Reviewer searched `StepDefinitions/` only; Reqnroll picks up bindings from both `Steps/` and `StepDefinitions/` directories. Not a blocker.

**Issue 2 (Acknowledged TODO)**: `ExpiredAdminTokenFixture.Create()` returns placeholder string.
**Resolution**: This is an intentional crafter TODO, correctly documented in `AdminLoginSteps.cs:160-167`. F-04 carries `@skip` — the fixture will be implemented before that scenario is enabled. Not a blocker for DELIVER handoff.

### Strengths
- Error/edge coverage at 67% significantly exceeds 40% threshold
- Lockout boundary conditions properly modeled (F-05 through F-08)
- Clean reuse of existing steps; no redundant re-declarations
- Walking skeleton strategy aligns perfectly with Outside-In Double Loop TDD
- AC deferral rationale is clear and well-justified
- Feature location decision (new `Epic1_AdminAuth/` directory) avoids conflicts during OAuth transition

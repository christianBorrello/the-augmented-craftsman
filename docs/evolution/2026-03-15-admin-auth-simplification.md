# Evolution: Admin Auth Simplification

**Date**: 2026-03-15
**Feature ID**: admin-auth-simplification
**Status**: COMPLETE — all 4 steps + REFACTOR COMMIT/PASS

---

## Feature Summary

Replaced the 4-step GitHub OAuth admin authentication flow with a direct email/password login. The previous flow required: browser redirect to GitHub, OAuth callback, JWT nonce exchange, and session cookie creation — four round-trips for a solo author with no external identity requirements. The new flow: POST credentials, receive JWT, done.

The simplification removed 10 dead files, eliminated the Redis dependency from the admin auth path, deleted one port (`ITokenGenerator`) and one keyed DI registration (`IOAuthClient("admin")`), and replaced the OAuth button on the frontend login page with a standard email/password form.

---

## Business Context

The Author Mode feature (2026-03-14) built admin authentication on GitHub OAuth. That choice was reasonable at the time, but revealed three pain points in practice:

1. **Operational complexity**: Redis was required in production solely to hold short-lived OAuth nonces. Any Redis connectivity issue broke admin login.
2. **Test infrastructure friction**: The dual `IOAuthClient` DI registration (keyed admin + unkeyed reader) caused `WebApplicationFactory` crashes that required a `Where` → `SingleOrDefault` workaround.
3. **Flow complexity out of proportion to the threat model**: A solo author on a personal blog does not need OAuth redirect flows. The existing `LoginHandler` (email/password + BCrypt + rate limiting) was already implemented and tested — it just was not wired to the frontend.

The north-star question: "Can Christian log in to manage his blog with fewer steps and no external dependencies?" The walking skeleton answers yes.

---

## Waves Completed

| Wave | Agent | Outcome |
|---|---|---|
| DISCUSS | Luna (product-owner) | 4 open questions resolved, scope boundaries set, reader OAuth explicitly excluded |
| DESIGN | Morgan (solution-architect) | 6 decisions resolved (D-1 through D-6), 3 ADRs produced, crafter/DEVOPS handoff notes |
| DISTILL | Quinn (acceptance-designer) | 12 BDD scenarios (2 walking skeletons + 10 focused), peer review APPROVED (9.2/10) |
| DELIVER | software-crafter | 4 steps + REFACTOR — all COMMIT/PASS |

---

## Steps Completed

| Step | Name | Phases | Result |
|---|---|---|---|
| 01-01 | Fix LoginHandler JWT: emit role claim, inline JWT construction | PREPARE / RED_ACCEPTANCE / RED_UNIT / GREEN / COMMIT | PASS |
| 01-02 | Delete 10 dead OAuth files and clean up registrations | PREPARE / RED_ACCEPTANCE / GREEN / COMMIT (RED_UNIT N/A) | PASS |
| 01-03 | Update AuthorMode step definitions to use login endpoint | PREPARE / RED_ACCEPTANCE / GREEN / COMMIT (RED_UNIT N/A) | PASS |
| 01-04 | Replace OAuth button on admin login page with email/password form | PREPARE / RED_ACCEPTANCE / GREEN / COMMIT (RED_UNIT N/A) | PASS |
| REFACTOR | L1–L4 refactoring pass on all modified files | GREEN / COMMIT | PASS |

### Step Timing

All 5 phases (4 steps + REFACTOR) were executed on 2026-03-15. Duration from first PREPARE (19:17 UTC) to REFACTOR COMMIT (20:18 UTC): approximately 1 hour.

### Git Commits

| Commit | Message |
|---|---|
| `c5930cd` | feat(admin-auth): LoginHandler emits role:admin JWT signed with IAdminSettings.JwtSecret |
| `cea7a9f` | feat(admin-auth): delete OAuth admin infrastructure and clean up registrations |
| `b2eb19d` | test(admin-auth): update AuthorMode steps to authenticate via POST /api/auth/login |
| `d3d0f7f` | feat(admin-auth): replace OAuth button with email/password login form |
| `b8fa77f` | refactor(admin-auth): L1-L4 refactoring pass on modified files |

---

## Adversarial Review Verdict

**APPROVED — no blocking issues.**

The DISTILL wave peer review (Sentinel, nw-acceptance-designer-reviewer) returned an initial NEEDS_REVISION verdict that resolved to APPROVED on investigation:

- **Issue 1 (False Positive)**: Missing step binding `[Then("the post list is returned normally")]`. Resolution: binding exists at `Steps/AuthorMode/AuthSteps.cs:193` — reviewer searched only `StepDefinitions/`. Not a blocker.
- **Issue 2 (Acknowledged TODO)**: `ExpiredAdminTokenFixture.Create()` returns a placeholder string. Resolution: intentional crafter TODO, correctly documented. F-04 carries `@skip`. Not a blocker for DELIVER handoff.

Average dimension score: 9.2/10.

---

## Key Decisions by Wave

### DISCUSS Wave (Luna — product-owner)

| Decision | Choice |
|---|---|
| Login form design | Full email/password form (not password-only) — aligns with browser credential managers and existing API contract |
| JWT lifetime | 480 minutes, hardcoded — covers a full writing day; configurability adds overhead for a value that will never change |
| IAdminSettings interface | Retained as a 1-property interface (JwtSecret only) — hexagonal port abstraction; keeps LoginHandler independent of IConfiguration |
| Reader OAuth scope | Explicitly excluded — different purpose, different callback URL, different token lifecycle; separate feature if needed |

### DESIGN Wave (Morgan — solution-architect)

| Decision | Choice |
|---|---|
| D-1: JWT transport | Response body + in-memory storage — no httpOnly cookie (CORS complexity outweighs benefit on solo-admin threat model) |
| D-2: Lockout communication | HTTP 429 (already implemented) — frontend distinguishes 401 vs 429 by status code |
| D-3: Frontend form submit | JavaScript `fetch` — native form action prevents inline error display and 100ms loading state |
| D-4: Email field pre-population | Empty field, browser autocomplete only — admin email not hardcoded in source or injected via SSR |
| D-5: ITokenGenerator fate | Deleted — YAGNI; no remaining consumers; can be reintroduced with a clear requirement |
| D-6: Attempt counter for "1 remaining" warning | Frontend-only state — backend does not expose attempt count (avoids account probe vector) |

### DISTILL Wave (Quinn — acceptance-designer)

| Decision | Choice |
|---|---|
| DD-1: Feature file location | New `Features/Epic1_AdminAuth/` directory — avoids conflict with OAuth-era `milestone-1-auth.feature` during transition |
| DD-2: Step reuse | Existing steps in `AuthSteps.cs` and `CommonSteps.cs` reused; `AdminLoginSteps.cs` adds only net-new bindings |
| DD-3: Step wording | `"Christian accesses the admin post list"` (new) vs `"Christian requests..."` (existing) — avoids binding conflict |
| DD-4: AC-03-1 (4th failure warning) | Response body assertion only at API level — frontend state machine concern not testable via BDD API tests |
| DD-5: JWT signing key | Implicit via round-trip (WS-1 proves it) — no dedicated scenario exposes key material |
| DD-6: Walking skeletons | Both WS-1 and WS-2 enabled from day one; 10 focused scenarios carry `@skip` |

---

## Issues Encountered

### Issue 1: DES Path Confusion

**Phase affected**: All DES (software-crafter) phases
**Problem**: The crafter ran `dotnet test` from the `backend/` subdirectory instead of the repository root. This caused relative path resolution failures in some tooling calls and required the crafter to re-anchor commands using absolute paths.
**Resolution**: Added absolute path discipline to crafter execution. The DELIVER workflow now documents that all CLI commands must use absolute paths from the repository root.
**nWave lesson**: DES prompts must include a RECORDING_INTEGRITY section that mandates absolute paths and verifies the working directory before any CLI execution.

### Issue 2: `acceptance_criteria` vs `criteria` field name in roadmap schema

**Phase affected**: DELIVER roadmap parsing
**Problem**: The roadmap schema used `criteria` as the field name for step acceptance criteria. Some tooling and documentation used `acceptance_criteria` (the more verbose name from earlier schema versions). This caused minor confusion during step validation.
**Resolution**: Confirmed `criteria` is the canonical field name in roadmap schema v3.0. All roadmap files updated to use `criteria`.
**nWave lesson**: Schema field name changes must be reflected in all agent prompts that reference the schema. A SCHEMA_VERSION check at roadmap load time would catch mismatches early.

---

## Acceptance Test Results

- **Total scenarios**: 12 (2 walking skeletons + 10 focused)
- **Error/edge coverage**: 67% (8/12) — significantly above the 40% target
- **Lockout boundary conditions**: F-05 through F-08 (4 scenarios)
- **Business rules validated**: Rate limiting, JWT issuance, role claim, OAuth endpoint removal
- **Walking skeleton outcome**: WS-1 ("Christian logs in and can immediately manage posts") passes end-to-end

---

## Architecture Artifacts

Permanent artifacts migrated to:
- `docs/architecture/admin-auth-simplification/architecture-design.md` — auth flow, JWT issuance, API contracts
- `docs/architecture/admin-auth-simplification/component-boundaries.md` — what changed, what was deleted, what was retained
- `docs/architecture/admin-auth-simplification/technology-stack.md` — technology decisions
- `docs/architecture/admin-auth-simplification/data-models.md` — JWT claims, API request/response shapes
- `docs/scenarios/admin-auth-simplification/test-scenarios.md` — full BDD scenario inventory
- `docs/scenarios/admin-auth-simplification/walking-skeleton.md` — walking skeleton rationale and demo script
- `docs/ux/admin-auth-simplification/journey-admin-login.yaml` — UX journey (machine-readable)
- `docs/ux/admin-auth-simplification/journey-admin-login-visual.md` — UX journey (visual)

ADRs produced:
- `docs/adrs/ADR-005-admin-jwt-issuance-inline-loginhandler.md` — inline JWT in LoginHandler; supersedes ADR-001
- `docs/adrs/ADR-006-iadminsettings-simplification.md` — IAdminSettings with JwtSecret only
- `docs/adrs/ADR-007-itokengenerator-deletion.md` — ITokenGenerator deleted (YAGNI)

---

## DORA Metrics Impact

| Metric | Observation |
|---|---|
| Deployment frequency | 5 commits in ~1 hour — high velocity on a focused, well-scoped feature |
| Lead time | DISCUSS to final commit: same day (5-wave sequence in a single session) |
| Change failure rate | Zero post-delivery fix commits — 0/5 = 0% |
| Time to restore | N/A — no production incident |

The tight scope (4 steps, 1 phase) and the pre-existing `LoginHandler` implementation contributed to the clean delivery. When the production code already exists and the feature is primarily a wiring + deletion task, DORA metrics reflect that efficiency.

---

## Lessons Learned

1. **DES path confusion is a recurring risk**: The crafter running from a subdirectory (`backend/`) instead of the repository root caused tool resolution failures. DELIVER wave DES prompts must include a mandatory RECORDING_INTEGRITY section that validates the working directory before any execution phase begins.

2. **RECORDING_INTEGRITY section is mandatory in DES prompts**: Without it, the agent has no checkpoint to verify that its environment is correct before it begins writing test files or executing builds. This is especially critical in monorepos where multiple `dotnet` projects exist.

3. **`acceptance_criteria` → `criteria` field name in roadmap schema**: Roadmap schema v3.0 uses `criteria` (not `acceptance_criteria`). This must be reflected consistently in all agent prompts that parse or validate the roadmap. A schema version assertion at load time would catch future mismatches.

4. **Deletion steps have no RED_UNIT phase**: Steps whose primary action is file deletion and DI cleanup have no production logic to unit-test. The roadmap correctly marked `01-02`, `01-03`, and `01-04` RED_UNIT phases as NOT_APPLICABLE. This pattern is valid and should be recognized by the DES agent without requiring explicit override justification.

5. **Pre-existing, tested implementation accelerates delivery**: `LoginHandler` (email/password, BCrypt, rate limiting) was already implemented and passing tests before this feature began. The feature was primarily a wiring task. When the domain logic exists, DELIVER velocity increases dramatically.

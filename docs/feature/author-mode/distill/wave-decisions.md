# Wave Decisions: Author Mode DISTILL

**Feature**: author-mode
**Wave**: DISTILL
**Date**: 2026-03-14
**Agent**: acceptance-designer

---

## Prior Wave Artifacts Confirmed

- [x] `docs/feature/author-mode/discuss/acceptance-criteria.md`
- [x] `docs/feature/author-mode/discuss/story-map.md`
- [x] `docs/feature/author-mode/discuss/user-stories.md`
- [x] `docs/feature/author-mode/discuss/wave-decisions.md`
- [x] `docs/feature/author-mode/design/architecture-design.md`
- [x] `docs/feature/author-mode/design/component-boundaries.md`
- [x] `docs/feature/author-mode/design/wave-decisions.md`
- [x] `docs/feature/author-mode/devops/platform-architecture.md`
- [x] `docs/feature/author-mode/devops/ci-cd-pipeline.md`
- [x] `docs/feature/author-mode/devops/wave-decisions.md`

---

## Decisions Made During DISTILL Wave

---

### DT-01: Acceptance Tests Target .NET API Driving Port Only

**Decision**: All acceptance tests for the author-mode feature drive through the `.NET` Minimal API endpoints via `WebApplicationFactory<Program>` + `HttpClient`. No Astro/frontend behaviour is tested at this boundary.

**Rationale**: The acceptance test project (`TacBlog.Acceptance.Tests`) is a backend project. The Astro layer (middleware, session, OAuth callback page) is a separate system. The .NET API is the correct driving port for backend behaviour. Frontend-layer behaviours (session creation in Redis, Astro middleware redirect) are validated by the three-layer auth design and are not testable in this project without a full E2E browser test.

**What this means for software-crafter**: The acceptance tests prove all backend business rules. The Astro frontend layer is validated by integration tests within the frontend project (TypeScript/Vitest) or manual verification.

---

### DT-02: Implementation-Coupled AC Rewritten as Observable Behaviours

**Decision**: All DISCUSS acceptance criteria that referenced internal implementation details have been rewritten as observable user outcomes.

**Examples of rewrites applied**:

| Original (implementation-coupled) | Rewritten (observable) |
|---|---|
| "Astro crea sessione in Upstash Redis con `isAdmin: true`" | "The admin session is active" |
| "`context.locals.user` viene popolato con i dati dell'autore" | "The post list is returned normally" (middleware worked) |
| "`export const prerender = false`" | Enforced by CI guard (DD-09), not an acceptance test |
| "La Server Island `EditControls` usa `server:defer`" | "The toolbar is empty for readers, non-empty for author" |
| "Il `post.id` viene passato come prop criptata" | Not testable at API boundary — architectural concern |

**Rationale**: As noted in the DESIGN reviewer note, AC must reference observable behaviour. The rewrite ensures tests are decoupled from implementation and remain valid if the implementation changes.

---

### DT-03: StubRebuildService Tracks Trigger State

**Decision**: A new `StubRebuildService` test double is created to record whether a rebuild was triggered during a scenario. It replaces any real HTTP call to Vercel Deploy Hook in tests.

**Rationale**: Acceptance tests must not make real external HTTP calls to Vercel. The stub captures the observable outcome (rebuild was or was not triggered) without requiring actual Vercel infrastructure. The software-crafter will wire the production `RebuildService` to call `VERCEL_DEPLOY_HOOK_URL` — this is tested at the unit level.

**Integration note**: `StubRebuildService` is registered as a Scoped dependency and injected into step definitions via Reqnroll DI. It resets automatically between scenarios.

---

### DT-04: Walking Skeleton Enables First Scenario Only

**Decision**: Only the first walking skeleton scenario is enabled (no `@skip` tag). All 44 remaining focused scenarios carry `@skip` and are implemented one at a time following the outside-in TDD sequence documented in `test-scenarios.md`.

**Rationale**: Multiple failing tests break the TDD feedback loop (principle 4 of the acceptance-designer methodology). The software-crafter enables one scenario, makes it pass, then enables the next.

---

### DT-05: AdminAuthDriver Uses StubOAuthClient — Not Real OAuth

**Decision**: The `AdminAuthDriver` configures `StubOAuthClient` (already in the project) to return the admin email, then drives the `/api/auth/admin/oauth/{provider}/callback` and `/api/auth/admin/verify-token` endpoints. No real Google/GitHub OAuth is performed in tests.

**Rationale**: Real OAuth requires browser interaction and external provider dependencies. The acceptance tests focus on the backend's response to an OAuth callback — validating the ADMIN_EMAIL check, token generation, and token exchange. The stub provides the OAuth user profile; the backend enforces the business rule.

**Implication for software-crafter**: The `StubOAuthClient.ConfigureConsentGranted(displayName)` method is reused. The admin OAuth callback must extract the email from the user profile returned by `IOAuthClient.GetUserProfileAsync()`. The test configuration seeds `Admin:Email = christian.borrello@gmail.com`. The stub email mechanism may need refinement once the actual `HandleAdminOAuthCallback` use case is implemented.

---

### DT-06: OQ-03 Resolved — EditControls Shows [Modifica] Only

**Decision**: As recommended in DESIGN wave OQ-03, the `EditControls` Server Island shows only the post status badge and the [Modifica] link. No [Archive] button in MVP.

**Rationale**: Not specified in US-07, deferred to a future story.

---

## Open Questions for DELIVER Wave (software-crafter)

| OQ | Question | Priority |
|---|---|---|
| DT-OQ-01 | `StubOAuthClient.ConfigureConsentGranted(displayName)` doesn't include an email field. The `HandleAdminOAuthCallback` use case needs to check ADMIN_EMAIL against `profile.Email`. Either the stub needs an email-aware configure method, or the existing method is sufficient if the backend reads email from `displayName` in tests. Clarify during inner TDD loop. | High |
| DT-OQ-02 | `GET /api/admin/posts/{id}/edit-controls` endpoint does not exist in the current backend. The `AdminPostDriver.GetEditControls()` drives it. The software-crafter must implement this endpoint (or verify the toolbar data comes from the existing `GET /api/admin/posts/{slug}` endpoint with a different shape). | High |
| DT-OQ-03 | `StubRebuildService` is a test double, not the production port. The production `RebuildService` (`src/lib/rebuild.ts`) is in the Astro frontend. The backend API endpoints for archive/restore must return enough information (e.g., `{ rebuildRequired: true }`) for the Astro Action to decide whether to call the rebuild hook. | Medium |

---

## Artifacts Produced

| File | Description |
|---|---|
| `Features/AuthorMode/walking-skeleton.feature` | 3 walking skeleton scenarios (1 enabled, 2 skipped) |
| `Features/AuthorMode/milestone-1-auth.feature` | 11 auth scenarios (US-01, US-02) — all skipped |
| `Features/AuthorMode/milestone-2-post-creation.feature` | 18 post creation/draft/publish scenarios (US-03–06) — all skipped |
| `Features/AuthorMode/milestone-3-image-and-toolbar.feature` | 9 image and toolbar scenarios (US-06b, US-07) — all skipped |
| `Features/AuthorMode/milestone-4-tags-archive-restore.feature` | 18 tag/archive/restore scenarios (US-09–12) — all skipped |
| `Steps/AuthorMode/AuthSteps.cs` | Step definitions for auth scenarios |
| `Steps/AuthorMode/PostSteps.cs` | Step definitions for post scenarios |
| `Steps/AuthorMode/AdminToolbarSteps.cs` | Step definitions for image, toolbar, tag, archive, restore |
| `Drivers/AdminAuthDriver.cs` | Drives admin OAuth endpoints |
| `Drivers/AdminPostDriver.cs` | Drives admin post CRUD + archive/restore + edit-controls |
| `Support/StubRebuildService.cs` | Test double for Vercel rebuild hook |
| `Support/StubImageStorage.cs` | Extended with `WasUploaded` tracking property |
| `Support/TacBlogWebApplicationFactory.cs` | Extended with `Admin:Email`, `Admin:JwtSecret` config |
| `Support/DependencyConfig.cs` | Extended with new driver registrations |
| `docs/feature/author-mode/distill/test-scenarios.md` | Full scenario inventory with coverage map |
| `docs/feature/author-mode/distill/walking-skeleton.md` | Walking skeleton rationale + demo script |
| `docs/feature/author-mode/distill/acceptance-review.md` | Peer review (critique-dimensions) |
| `docs/feature/author-mode/distill/wave-decisions.md` | This file |

---

## Mandate Compliance Evidence

### CM-A: Driving Port Usage

All step definitions import only driver classes (`AdminAuthDriver`, `AdminPostDriver`, `PostApiDriver`, `TagApiDriver`, `ImageApiDriver`). These drivers make HTTP calls to the .NET API endpoints (driving port). No domain entities, use cases, repositories, or infrastructure classes are imported in any step definition.

### CM-B: Business Language Purity

Verified by inspection: no Gherkin scenario contains the terms `HTTP`, `REST`, `JSON`, `status code`, `Redis`, `JWT`, `OAuth token`, `endpoint`, `database`, `repository`, `DbContext`, or any other technical term. All scenarios use domain vocabulary from the ubiquitous language: `post`, `author`, `draft`, `published`, `archived`, `slug`, `tag`, `cover image`, `admin session`, `rebuild`.

### CM-C: Walking Skeleton + Focused Scenario Counts

- Walking skeletons: 3 (Scenario 1 enabled, Scenarios 2–3 @skip)
- Focused scenarios: 44 (all @skip, enabled one at a time during DELIVER)
- Total: 47
- Error/edge ratio: 43% (20 of 47)
- Story coverage: 12/12 user stories have at least one scenario

---

## Handoff to DELIVER Wave

**Status**: Ready for handoff to `nw:deliver` (software-crafter)

**First task for software-crafter**:
Enable the first walking skeleton scenario:

```
Features/AuthorMode/walking-skeleton.feature
Scenario: Author logs in via OAuth, creates a draft post, and sees it in the post list
```

Remove the `@skip` tag from this scenario only. Run tests. Expect failure (scenario is RED because the new backend endpoints don't exist yet). Begin inner TDD loop.

**New backend work required** (from DESIGN wave + DISTILL verification):
1. `POST /api/auth/admin/oauth/{provider}/callback` — admin OAuth callback endpoint
2. `POST /api/auth/admin/verify-token` — token exchange endpoint
3. `PATCH /api/posts/{id}/archive` — archive endpoint
4. `PATCH /api/posts/{id}/restore` — restore endpoint
5. `GET /api/admin/posts/{id}/edit-controls` — toolbar data endpoint (or verify existing endpoint covers this)
6. `PostStatus.Archived` + `PreviousStatus` domain extension
7. `ArchivePost` + `RestorePost` use cases
8. `HandleAdminOAuthCallback` + `VerifyAdminToken` use cases
9. `IAdminTokenStore` port + `RedisAdminTokenStore` adapter (for production; test uses in-memory)

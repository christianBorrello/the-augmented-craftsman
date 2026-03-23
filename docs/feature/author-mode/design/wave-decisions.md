# Wave Decisions: Author Mode DESIGN

**Feature**: author-mode
**Wave**: DESIGN
**Date**: 2026-03-14
**Architect**: Morgan (solution-architect)

---

## Decisions Made During DESIGN Wave

These decisions are binding for the DISTILL wave (acceptance-designer) and EXECUTE wave (software-crafter).

---

### DD-01: Admin OAuth Uses Separate Backend Route + Single-Use Admin Token

**Decision**: The admin OAuth flow uses dedicated routes (`/api/auth/admin/oauth/*`) separate from the reader OAuth routes (`/api/auth/oauth/*`). After the backend verifies `ADMIN_EMAIL`, it issues a short-lived JWT (5-minute TTL) with a single-use nonce stored in Upstash Redis. Astro's `/admin/callback` page exchanges this token via `POST /api/auth/admin/verify-token` for a long-lived JWT + user profile, then creates the Astro session.

**Rationale**: Separating admin and reader OAuth prevents accidental privilege escalation if the reader session infrastructure is extended. The single-use token pattern prevents replay attacks on the callback URL. Reusing existing `IOAuthClient` and `ITokenGenerator` ports minimizes new infrastructure.

**Alternatives rejected**:
- Redirect with email in query params: exposes PII in browser history and server logs.
- Sharing reader session infrastructure: different TTL, different storage semantics, different trust level.
- Magic link via email: requires SMTP service (DISCUSS D-02 already rejected this).

**Resolves**: RC-01

---

### DD-02: Three-Layer Authorization (Astro Middleware + Action Guard + JWT Bearer)

**Decision**: Admin protection is enforced at three independent layers: (1) Astro middleware validates session before any `/admin/*` request, (2) each Astro Action explicitly checks `Astro.locals.user`, (3) the .NET backend validates the JWT Bearer token on every authorized endpoint. None of the three layers trusts the others.

**Rationale**: Defense in depth (OWASP). A bypass at layer 1 (e.g., missing `prerender = false`) would still be stopped at layer 3 (JWT required). This is proportionate security for a single-author blog without being operationally complex.

**Alternatives rejected**:
- Session-only on Astro, no JWT: the .NET API would need a separate authentication mechanism or trust Astro completely — increases coupling and reduces backend's ability to be tested independently.
- JWT-only (no Astro session): requires the frontend to store and rotate JWTs without Astro Sessions infrastructure — more complex, less secure (localStorage vs HttpOnly cookie).

**Resolves**: RC-02

---

### DD-03: Server Island Uses `Astro.cookies` + Direct Redis Lookup (Not `Astro.session`)

**Decision**: The `EditControls` Server Island reads the Astro session cookie via `Astro.cookies` and validates it against Upstash Redis directly, rather than using the `Astro.session` API.

**Rationale**: Server Islands are processed as independent HTTP requests (similar to API routes), not through the standard Astro page middleware pipeline. `Astro.session` availability in Server Islands is not guaranteed across Astro versions and render modes. Reading the cookie and performing a direct Redis lookup is deterministic and version-stable.

**Alternatives rejected**:
- `Astro.session` in Server Island: undocumented in Astro 6 for this context; risks silent failures in future upgrades.
- Calling .NET API for auth check: adds latency to every blog page load for all readers; the Redis lookup is faster.

**Resolves**: Session ambiguity red card

---

### DD-04: `output: 'hybrid'` for Astro Config

**Decision**: Change `output: 'static'` to `output: 'hybrid'` in `astro.config.mjs`. All existing pages remain SSG by default. Admin pages opt out with `export const prerender = false`.

**Rationale**: `output: 'hybrid'` is the minimal change enabling SSR for admin pages without affecting the static blog. `output: 'server'` would convert all pages to SSR, degrading performance for readers.

**Alternatives rejected**:
- `output: 'server'`: All pages become SSR. Breaks SSG guarantees for `/blog/*`. Performance regression for readers.
- Separate Astro project for admin: Already rejected in DISCUSS D-01.

---

### DD-05: Archived Post URLs Return Native 404

**Decision**: When a reader visits a `/blog/{slug}` URL for an archived post, the page simply does not exist in the static build (removed after rebuild). The Astro blog page returns a 404 via the standard Astro 404 mechanism. No "Contenuto rimosso" page is created for MVP.

**Rationale**: The simplest correct behavior. A 404 is semantically accurate — the content is no longer publicly available. Custom "content removed" pages can be added in a future iteration if needed. The .NET API already returns 404 for non-published posts at `GET /api/posts/{slug}`.

**Resolves**: RC-03

---

### DD-06: Full Vercel Rebuild via Deploy Hook (No ISR)

**Decision**: After publish, edit, archive, or restore-to-published operations, the frontend triggers a full Vercel rebuild via `VERCEL_DEPLOY_HOOK_URL`. ISR is not used. Rebuild feedback uses a 60-second timeout with a manual link fallback.

**Rationale**: ISR is not available on the `@astrojs/vercel` static adapter. Full rebuild for a personal blog with a small number of posts is acceptable (30-60 seconds). The timeout-based feedback (US-11) avoids requiring a Vercel API token for deployment status polling.

**Alternatives rejected**:
- ISR: Not supported by `@astrojs/vercel` static adapter.
- Vercel REST API polling: Requires additional Vercel token configuration; adds complexity for minimal benefit given single-author usage.

**Resolves**: RC-04

---

### DD-07: `PostStatus.Archived` Added to Domain; `PreviousStatus` Stored on `BlogPost`

**Decision**: `Archived` is added to the `PostStatus` enum. `BlogPost` stores a nullable `PreviousStatus` field. Archive and restore are domain methods on `BlogPost`. A database migration adds the `PreviousStatus` column.

**Rationale**: The restore-to-prior-state requirement (US-12, D-05) requires knowing the state before archiving. Storing `PreviousStatus` on the aggregate is the simplest approach — no separate history table, no event sourcing.

**Alternatives rejected**:
- Separate status history table: overkill for a two-state transition history.
- Always restore to Draft: violates US-12 AC — "restore to previous state (Published or Draft)".
- Event sourcing: DISCUSS wave explicitly deferred domain events. Not warranted for this use case.

---

### DD-08: `CreatePostRequest` and `EditPostRequest` Extended with `CoverImageUrl`

**Decision**: Both request contracts gain an optional `CoverImageUrl` (nullable string) field. The upload happens before form submission — the Astro Action uploads the image first (getting back the ImageKit URL), then includes that URL in the create/edit request.

**Rationale**: The ImageKit upload flow returns a URL before the post is saved. The post create/edit operation then saves this URL as `FeaturedImageUrl` on the domain entity. This matches the existing `SetFeaturedImage` use case pattern and avoids a two-step HTTP flow from the browser.

---

### DD-09: CI Guard for `prerender = false`

**Decision**: A CI check (grep or custom test) verifies that every `.astro` file under `src/pages/admin/` contains `export const prerender = false`. Build fails if any admin page is missing this flag.

**Rationale**: A missing flag causes the page to be pre-rendered in production, silently bypassing the Astro middleware auth guard. This is identified as the highest-risk misconfiguration in the DISCUSS requirements risk register.

---

## Open Questions (Non-blocking for DISTILL)

| # | Question | Priority | Owner |
|---|---|---|---|
| OQ-01 | Should the JWT TTL (60 min default) be extended to match the Astro session TTL (24h)? Currently the user would be forced to re-login mid-session when the JWT expires. Consider storing refresh logic in the middleware. | Medium | EXECUTE |
| OQ-02 | Should `OAUTH_ADMIN_JWT_SECRET` reuse `Jwt__Secret` or be a separate key? Separate key is safer but adds configuration burden. | Low | EXECUTE |
| OQ-03 | Should the `EditControls` Server Island also show an [Archive] button, or only [Modifica]? Not specified in US-07 — MVP shows only [Modifica]. | Low | DISTILL validation |

---

## Artifacts Produced

| File | Description |
|---|---|
| `architecture-design.md` | C4 L1+L2+L3 diagrams, OAuth flow, auth layers, quality strategies |
| `technology-stack.md` | Technology additions with license, rationale, alternatives |
| `component-boundaries.md` | Frontend and backend component responsibilities and contracts |
| `data-models.md` | Domain changes, DB schema, Astro session schema, API contracts |
| `wave-decisions.md` | This file — DESIGN wave decisions |
| `docs/adrs/ADR-001-admin-oauth-flow.md` | OAuth admin flow decision record |
| `docs/adrs/ADR-002-astro-hybrid-sessions.md` | Astro hybrid mode + sessions decision record |
| `docs/adrs/ADR-003-three-layer-authorization.md` | Defense-in-depth auth decision record |
| `docs/adrs/ADR-004-post-status-archived.md` | Domain extension decision record |

---

## Handoff to DISTILL Wave

**Status**: Ready for acceptance-designer (DISTILL wave)

**Key constraints for acceptance-designer**:
1. All AC must reference observable behavior — never reference `Astro.session`, `Astro.locals`, `jwtToken`, or other internal identifiers.
2. The Server Island behavior is observable: readers see no toolbar; the author sees the toolbar. Tests should verify DOM presence/absence.
3. The OAuth flow has two observable outcomes: session created + redirect to `/admin/posts`, or error message on `/admin/login`.
4. Post status transitions are observable via the list page and via the behavior of `/blog/{slug}` (404 for archived/draft, accessible for published).
5. The rebuild feedback (spinner → redirect OR timeout → manual link) is an observable UI behavior testable with Reqnroll BDD scenarios.

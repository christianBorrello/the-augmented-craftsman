# Component Boundaries: Author Mode

**Feature**: author-mode
**Wave**: DESIGN
**Date**: 2026-03-14

---

## Principle

Components are organized by **vertical slice** (feature/use-case) within the existing **hexagonal architecture**. New components extend existing bounded contexts rather than creating new ones.

---

## Frontend Component Boundaries

### Middleware: `src/middleware.ts`

**Responsibility**: Single-entry guard for all `/admin/*` routes.

**Contract**:
- Intercepts every request where `pathname.startsWith('/admin')`.
- Excludes `/admin/login` and `/admin/callback` from the guard.
- Reads `Astro.session` — checks `isAdmin === true`.
- If valid: populates `Astro.locals.user` with `{ email, name, avatarUrl, jwtToken }`.
- If invalid or missing: redirects to `/admin/login` with appropriate message query param.
- Uses `getActionContext()` to intercept Action requests — blocks unauthenticated Action calls.

**Does NOT**:
- Validate the JWT token against the backend (JWT validation is backend's responsibility).
- Know anything about post CRUD or domain entities.

---

### Pages: `/admin/*` (all `export const prerender = false`)

| Page | Path | Responsibility |
|---|---|---|
| Login | `src/pages/admin/login.astro` | Render OAuth buttons. Initiate redirect to `/api/auth/admin/oauth/{provider}`. Show error banner from query params. |
| OAuth Callback | `src/pages/admin/callback.astro` | Receive `?token=` from backend. POST to `/api/auth/admin/verify-token`. Create Astro session. Redirect to `/admin/posts`. |
| Post List | `src/pages/admin/posts/index.astro` | Fetch all posts from `GET /api/admin/posts` (server-side, with JWT). Render list with status filter tabs. |
| New Post | `src/pages/admin/posts/new.astro` | Fetch tags from `GET /api/tags`. Render form with Tiptap island and TagSelector island. |
| Edit Post | `src/pages/admin/posts/[id]/edit.astro` | Fetch post from `GET /api/admin/posts/{slug}` or `GET /api/posts/{id}/preview`. Fetch tags. Render pre-populated form. |

**Shared constraint**: Every admin page MUST have `export const prerender = false` as its first export. CI check enforces this.

---

### Astro Actions: `src/actions/admin/`

All actions follow the pattern: (1) check `Astro.locals.user`, (2) validate input, (3) call .NET API with JWT, (4) return typed result.

| Action | File | .NET API Call | Rebuild? |
|---|---|---|---|
| `admin.createPost` | `posts.ts` | `POST /api/posts` | Yes, if `status: published` |
| `admin.updatePost` | `posts.ts` | `PUT /api/posts/{id}` | Yes, if current or new status is `published` |
| `admin.archivePost` | `posts.ts` | `PATCH /api/posts/{id}/archive` | Yes, if was `published` |
| `admin.restorePost` | `posts.ts` | `PATCH /api/posts/{id}/restore` | Yes, if restoring to `published` |
| `admin.uploadCoverImage` | `images.ts` | `POST /api/images` | No |
| `admin.createTag` | `tags.ts` | `POST /api/tags` | No |

**Actions do NOT**:
- Access the database directly.
- Implement domain rules (slug generation, status transition validation — all in .NET).
- Render HTML.

---

### Preact Islands

| Island | File | Rendered When | Responsibility |
|---|---|---|---|
| `TiptapEditor` | `src/components/admin/TiptapEditor.tsx` | `client:only="preact"` — only on admin pages | Rich text editing. Emits HTML content via callback prop. Never calls backend directly. |
| `TagSelector` | `src/components/admin/TagSelector.tsx` | `client:only="preact"` — only on admin pages | Autocomplete + multi-select for tags. Calls `admin.createTag` Action on new tag. Updates selected tags list client-side. |

**Islands do NOT**:
- Call the .NET API directly.
- Manage auth state.
- Know about post lifecycle (draft/publish/archive).

---

### Server Island: `EditControls`

**File**: `src/components/EditControls.astro` (used in blog page layouts, NOT in `/admin/*`)

**Rendered as**: `<EditControls server:defer postId={encryptedPostId} />`

**Responsibility**:
- Read Astro session cookie directly via `Astro.cookies`.
- Look up session in Upstash Redis to check `isAdmin`.
- If author: render floating toolbar with post status badge and link to `/admin/posts/{id}/edit`.
- If reader: return empty HTML fragment.

**Props**:
- `postId`: encrypted by Astro's native prop encryption — the raw `Guid` is not visible in HTML.

**Does NOT**:
- Call the .NET API.
- Render any content for non-admin visitors.
- Go through the auth middleware (Server Islands are independent request handlers).

---

### Rebuild Service: `src/lib/rebuild.ts`

**Responsibility**:
- Encapsulate the Vercel Deploy Hook call.
- Provide timeout management (60 seconds).
- Return a status that Actions can use to show appropriate feedback.

**Contract** (behavioral, not code):
- Called by any Action that needs a rebuild.
- Sends HTTP POST to `VERCEL_DEPLOY_HOOK_URL`.
- Waits up to 60 seconds for the deployment to appear as "READY" (or uses a fixed timeout if no polling).
- Returns `{ success: boolean, timedOut: boolean }`.

---

## Backend Component Boundaries

### New Use Cases (Application Layer)

All new use cases follow the existing pattern — scoped classes with constructor-injected ports.

#### `HandleAdminOAuthCallback`

**Location**: `TacBlog.Application/Features/AdminAuth/`

**Responsibility**:
- Accept `provider`, `code`, `redirectUri`.
- Delegate code exchange and profile fetch to `IOAuthClient` (existing port).
- Check `profile.Email == ADMIN_EMAIL` (from `IAdminSettings` — new port).
- If match: generate short-lived signed token via `IAdminTokenStore.IssueAsync(email, name, avatarUrl)`.
- If no match: return failure with `"unauthorized"` error.

**Depends on**: `IOAuthClient` (existing), `IAdminTokenStore` (new), `IAdminSettings` (new, backed by env var).

#### `VerifyAdminToken`

**Location**: `TacBlog.Application/Features/AdminAuth/`

**Responsibility**:
- Accept `token` string.
- Validate JWT signature and TTL.
- Look up nonce in `IAdminTokenStore` — if not found: reject (already used or expired).
- Invalidate nonce.
- Return `{ email, name, avatarUrl }` plus a long-lived admin JWT (issued by `ITokenGenerator`).

**Depends on**: `IAdminTokenStore` (new), `ITokenGenerator` (existing).

#### `ArchivePost`

**Location**: `TacBlog.Application/Features/Posts/`

**Responsibility**:
- Find post by ID.
- Validate current status is not already `Archived`.
- Store `PreviousStatus = post.Status`.
- Transition post to `Archived`.
- Persist.

**Depends on**: `IBlogPostRepository` (existing), `IClock` (existing).

#### `RestorePost`

**Location**: `TacBlog.Application/Features/Posts/`

**Responsibility**:
- Find post by ID.
- Validate current status is `Archived`.
- Read `PreviousStatus`.
- Transition post back to `PreviousStatus`.
- Clear `PreviousStatus`.
- Persist.
- Return `RestoredStatus` in result so the API layer knows whether to trigger rebuild.

**Depends on**: `IBlogPostRepository` (existing), `IClock` (existing).

---

### New API Endpoints (Api Layer)

**File**: `TacBlog.Api/Endpoints/AdminAuthEndpoints.cs` (new file)

| Route | Handler | Auth |
|---|---|---|
| `GET /api/auth/admin/oauth/{provider}` | `InitiateAdminOAuthAsync` | Anonymous |
| `GET /api/auth/admin/oauth/{provider}/callback` | `HandleAdminCallbackAsync` | Anonymous |
| `POST /api/auth/admin/verify-token` | `VerifyAdminTokenAsync` | Anonymous (validates token internally) |

**File**: Post endpoints additions in `PostEndpoints.cs`

| Route | Handler | Auth |
|---|---|---|
| `PATCH /api/posts/{id}/archive` | `ArchivePostAsync` | RequireAuthorization |
| `PATCH /api/posts/{id}/restore` | `RestorePostAsync` | RequireAuthorization |

---

### New Ports (Application Layer)

#### `IAdminTokenStore`

**Location**: `TacBlog.Application/Ports/Driven/`

**Contract** (behavioral):
- Issue a signed, time-limited, single-use token for a user profile.
- Validate a token: check signature, TTL, and that nonce has not been used.
- Invalidate a nonce: mark as used so it cannot be reused.

**Adapter**: `RedisAdminTokenStore` in `TacBlog.Infrastructure/Identity/`

#### `IAdminSettings`

**Location**: `TacBlog.Application/Ports/Driven/`

**Contract** (behavioral):
- Expose `AdminEmail` — the single authorized admin email address.

**Adapter**: `EnvironmentAdminSettings` or configuration-backed class in `TacBlog.Infrastructure/`

---

### Domain Changes

**File**: `TacBlog.Domain/PostStatus.cs`
- Add `Archived` value.

**File**: `TacBlog.Domain/BlogPost.cs` (aggregate)
- Add `PreviousStatus` property (nullable `PostStatus?`).
- Add `Archive(DateTime now)` domain method: validates not already archived, stores `PreviousStatus`, transitions.
- Add `Restore(DateTime now)` domain method: validates is archived, restores from `PreviousStatus`.
- Slug immutability: `UpdateTitle` already prevents slug changes after first publish — verify this constraint exists; if not, add it to `BlogPost.Publish()`.

---

## Dependency Rule Compliance

All new components follow the inward dependency rule:

```
Api Layer           → Application Layer → Domain Layer
(new endpoints)       (new use cases)     (new status, methods)
                    → Infrastructure via Ports
                      (IAdminTokenStore → RedisAdminTokenStore)
                      (IAdminSettings   → EnvironmentAdminSettings)
```

No domain class depends on any infrastructure class. No application class depends on any adapter class. All inward.

---

## What Explicitly Does NOT Change

| Component | Status |
|---|---|
| `HandleOAuthCallback` (reader OAuth) | Unchanged — reader sessions are separate |
| `LoginHandler` (email+password) | Unchanged |
| `IBlogPostRepository` | Unchanged — existing find/save methods sufficient |
| `IOAuthClient` | Unchanged — reused by `HandleAdminOAuthCallback` |
| All existing public blog pages | Unchanged |
| `BrowsePublishedPosts`, `ReadPublishedPost` | Unchanged — archived posts not returned (same as draft) |

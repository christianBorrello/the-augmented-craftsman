# Walking Skeleton: Author Mode

**Feature**: author-mode
**Wave**: DISTILL
**Date**: 2026-03-14

---

## Definition

The walking skeleton is the thinnest vertical slice that delivers observable user value end-to-end and can be demonstrated to a stakeholder to answer: "Can Christian manage his blog without touching the database?"

---

## Walking Skeleton Scenarios

File: `backend/tests/TacBlog.Acceptance.Tests/Features/AuthorMode/walking-skeleton.feature`

### Skeleton 1 (enabled — implement first)

**"Author logs in via OAuth, creates a draft post, and sees it in the post list"**

Covers US-01 + US-02 + US-03 + US-04 in a single journey:
1. OAuth login completes → admin JWT issued → session active
2. Author creates a post → saved as draft
3. Post appears in admin list with "Draft" status
4. Post is NOT accessible on the public blog

This is the first scenario enabled. All others are `@skip`.

### Skeleton 2

**"Author publishes a post and it becomes publicly accessible"**

Covers US-06 (publish):
1. Authenticated author publishes a draft
2. Status changes to "Published"
3. Post appears on the public blog
4. Post no longer appears among drafts

### Skeleton 3

**"Unauthenticated visitor is redirected away from admin pages"**

Covers US-02 (access guard):
1. Visitor without session attempts to access admin list
2. Visitor is redirected to login
3. No post data is returned

---

## Stakeholder Demo Script

After implementing the walking skeleton, Christian can demo the following:

1. Open `http://localhost:4321/admin/login`
2. Click "Login with Google" → complete OAuth flow
3. See `/admin/posts` with the post list
4. Click "+ New Post" → fill in title and content → click "Save draft"
5. See the post in the list with "Draft" badge
6. Open a new incognito tab → navigate to `/blog/{slug}` → confirm 404 (draft not visible)
7. Back in admin → click "Publish" → confirm post appears at `/blog/{slug}`

This is demonstrable without touching PostgreSQL, the .NET API directly, or any terminal.

---

## Stories Covered

| Story | In Skeleton |
|---|---|
| US-01: OAuth login | Skeleton 1 |
| US-02: Auth guard | Skeleton 1, Skeleton 3 |
| US-03: Create post | Skeleton 1 |
| US-04: Post list | Skeleton 1 |
| US-06: Publish | Skeleton 2 |

---

## Infrastructure Required for Skeleton

- Testcontainers PostgreSQL (already in project)
- `StubOAuthClient` configured to return authorised email (already exists)
- New: `AdminAuthDriver` — drives `/api/auth/admin/oauth/*/callback` + `/api/auth/admin/verify-token`
- New: `AdminPostDriver` — drives `GET /api/admin/posts`
- New: `StubRebuildService` — records whether rebuild was triggered (no actual HTTP call in tests)

The skeleton does NOT require:
- Real Upstash Redis (session management is at the Astro layer, not the .NET API layer)
- Real Vercel Deploy Hook (stub records trigger)
- Real ImageKit (StubImageStorage already exists)

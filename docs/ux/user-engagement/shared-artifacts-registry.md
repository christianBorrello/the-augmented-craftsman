# Shared Artifacts Registry -- User Engagement

## Purpose

Track every data value that flows across journey steps or appears in multiple places. Each artifact has a single source of truth. Untracked artifacts cause integration failures.

---

## Artifacts by Sub-Feature

### Likes (J5)

| Artifact | Source of Truth | Consumers | Owner | Integration Risk | Validation |
|----------|----------------|-----------|-------|-----------------|------------|
| `visitor_id` | Generated client-side (UUID), stored in localStorage | Like API deduplication | Frontend (Astro Island) | LOW -- anonymous, disposable | Check localStorage on page load |
| `visitor_liked_{slug}` | Set in localStorage after successful POST | Like button state on page load | Frontend (Astro Island) | MEDIUM -- must reconcile with server state on load | Compare localStorage flag with API response |
| `like_count` | API response: `GET /api/posts/{slug}/likes` | Like button display, admin dashboard | Backend API | HIGH -- optimistic UI may diverge from server | Page load fetches fresh count from API |
| `slug` | Post URL path (from BlogPost domain entity) | Like API endpoint: `POST /api/posts/{slug}/likes` | Content Context | HIGH -- must match existing post slug | Validate slug exists before accepting like |

### Comments (J6)

| Artifact | Source of Truth | Consumers | Owner | Integration Risk | Validation |
|----------|----------------|-----------|-------|-----------------|------------|
| `session_token` | Backend session store (httpOnly cookie) | Comment API auth, identity display, sign-out | Backend (Identity) | HIGH -- expired session blocks commenting | Check session validity on page load |
| `display_name` | OAuth provider profile (Google/GitHub) | Comment attribution, "Signed in as..." display | Backend (extracted during OAuth) | MEDIUM -- only fetched once at sign-in | Store at sign-in, never re-fetch |
| `avatar_url` | OAuth provider profile (Google/GitHub) | Comment avatar display | Backend (extracted during OAuth) | LOW -- cosmetic, fallback to initials | Display placeholder if URL fails |
| `provider` | OAuth callback metadata | "via GitHub" / "via Google" badge | Backend (set during OAuth) | LOW -- set once, never changes | Validate is "google" or "github" |
| `comment_id` | API response: `POST /api/posts/{slug}/comments` | Comment rendering, admin moderation (delete) | Backend API | HIGH -- must be unique, stable | UUID generated server-side |
| `comment_text` | User input (textarea) | Comment display, moderation review | Backend API | MEDIUM -- must be sanitized for XSS | Server-side HTML sanitization |
| `created_at` | API response timestamp | Comment timestamp display | Backend API | LOW -- server-generated, consistent | ISO 8601 format |
| `comment_count` | API response: `GET /api/posts/{slug}/comments` | Comments section header | Backend API | MEDIUM -- must update after post/delete | Re-fetch or update client-side on mutation |
| `slug` | Post URL path (from BlogPost domain entity) | Comment API endpoint: `POST /api/posts/{slug}/comments` | Content Context | HIGH -- shared with Likes and Sharing | Same slug artifact as Likes |
| `oauth_token` | OAuth provider callback | Backend session creation (never exposed to client) | Backend (Identity) | HIGH -- must never leak to frontend | Server-side only, exchange for session |

### Sharing (J7)

| Artifact | Source of Truth | Consumers | Owner | Integration Risk | Validation |
|----------|----------------|-----------|-------|-----------------|------------|
| `post_title` | BlogPost entity title (via page metadata) | `navigator.share({ title })`, clipboard, OG tags | Content Context | MEDIUM -- must match OG title | Astro generates both from same source |
| `post_url` | Canonical URL constructed from slug | `navigator.share({ url })`, clipboard, OG tags | Frontend (Astro) | HIGH -- must be absolute URL with domain | Astro `Astro.url` or site config |
| `slug` | Post URL path (from BlogPost domain entity) | URL construction | Content Context | HIGH -- shared across all sub-features | Same slug artifact as Likes and Comments |

---

## Cross-Feature Shared Artifacts

These artifacts are consumed by multiple sub-features and must remain consistent:

| Artifact | Used By | Single Source | Risk if Inconsistent |
|----------|---------|---------------|---------------------|
| `slug` | Likes (J5), Comments (J6), Sharing (J7) | BlogPost.Slug value object | Wrong post liked/commented/shared |
| `post_title` | Sharing (J7), OG tags, Comments display | BlogPost.Title value object | Mismatched title in share previews |
| `post_url` | Sharing (J7), OG tags | Astro site config + slug | Broken links when shared |

---

## Cross-Epic Shared Artifacts

These artifacts connect Epic 5 (User Engagement) to existing Epics:

| Artifact | Epic 5 Consumer | Source Epic | Source Journey |
|----------|----------------|-------------|---------------|
| `slug` | All sub-features (API endpoints) | Epic 1 (Post Management) | J1.2 create-post |
| `post_title` | Sharing, OG tags | Epic 1 (Post Management) | J1.2 create-post |
| `auth_token` (admin JWT) | Comment moderation | Epic 1 (Post Management) | J1.1 login |
| `featured_image_url` | OG image for sharing | Epic 3 (Image Management) | J1.4 upload-image |

---

## Integration Checkpoints

### Checkpoint 1: Like Deduplication

- **Producer**: Frontend localStorage (`visitor_liked_{slug}`)
- **Consumer**: Backend API (`POST /api/posts/{slug}/likes`)
- **Validation**: Same visitor_id does not increment count twice. Frontend and backend agree on like state after page refresh.
- **Failure mode**: Count inflated if localStorage cleared and visitor re-likes. Accepted trade-off.

### Checkpoint 2: OAuth Session to Comment Flow

- **Producer**: OAuth callback (J6.3)
- **Consumer**: Comment POST endpoint (J6.4)
- **Validation**: Session token from OAuth grants permission to post comments. Display name from OAuth matches comment attribution.
- **Failure mode**: Expired session blocks commenting. Mitigation: clear error message + re-auth preserves draft.

### Checkpoint 3: Comment Count Consistency

- **Producer**: Comment POST/DELETE endpoints
- **Consumer**: Comments section header, admin panel
- **Validation**: Count matches actual number of comments. Updates on post and delete without page refresh.
- **Failure mode**: Stale count if client-side update fails. Mitigation: re-fetch on next page load.

### Checkpoint 4: Share URL Matches Canonical URL

- **Producer**: Astro page generation (site config + slug)
- **Consumer**: `navigator.share()`, clipboard, OG meta tags
- **Validation**: Shared URL resolves to the correct post. OG tags match page content.
- **Failure mode**: Broken link if URL construction differs from actual route. Mitigation: single source for URL construction in Astro.

### Checkpoint 5: OG Meta Tags Present for Sharing

- **Producer**: Astro page head (build time)
- **Consumer**: Social platforms scraping shared URLs
- **Validation**: og:title, og:description, og:url, og:image all present. og:image falls back to default when no featured image.
- **Failure mode**: Missing OG tags result in ugly share previews. Mitigation: Astro layout template enforces OG tags on all post pages.

### Checkpoint 6: Admin Moderation Affects Public View

- **Producer**: Admin comment delete (J6.M2)
- **Consumer**: Public comments section (J6.1)
- **Validation**: Deleted comment no longer appears on public post. Comment count decrements.
- **Failure mode**: Cached/stale comment visible after deletion. Mitigation: comments fetched from API on each page load (Astro Island, not SSG cached).

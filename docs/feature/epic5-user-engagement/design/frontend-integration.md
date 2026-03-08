# Frontend Integration -- Epic 5: User Engagement

## Astro Island Architecture

The existing blog is fully static SSG (`output: 'static'`). Engagement features are dynamic and require JavaScript. Astro Islands allow hydrating interactive components within static pages without changing the overall architecture.

Each engagement feature is a separate Astro Island with `client:visible` hydration -- they load lazily when scrolled into view, avoiding impact on initial page load performance.

---

## Component Architecture

### Post Page Layout (existing `[slug].astro`)

```
+--------------------------------------------------+
|  Article Header (static)                         |
|  - Title, date, tags, reading time               |
+--------------------------------------------------+
|  Article Body (static)                           |
|  - Markdown content rendered at build time       |
+--------------------------------------------------+
|  Engagement Bar (Island: client:visible)          |
|  +-------------------+  +-------------------+    |
|  | LikeButton        |  | ShareButton       |    |
|  | (heart + count)   |  | (share icon)      |    |
|  +-------------------+  +-------------------+    |
+--------------------------------------------------+
|  Article Footer (static)                         |
|  - Author info, "More posts" link                |
+--------------------------------------------------+
|  CommentSection (Island: client:visible)          |
|  +----------------------------------------------+|
|  | Comments Header: "Comments (N)"              ||
|  +----------------------------------------------+|
|  | Comment List                                  ||
|  |   Comment 1 (avatar, name, badge, text, time) ||
|  |   Comment 2 ...                               ||
|  +----------------------------------------------+|
|  | Auth State                                    ||
|  |   [Not signed in] Sign in with Google/GitHub  ||
|  |   [Signed in] Textarea + Post Comment button  ||
|  +----------------------------------------------+|
+--------------------------------------------------+
```

---

## Island 1: LikeButton

**Hydration:** `client:visible`
**Props:** `slug: string`, `apiBase: string`
**Framework:** Vanilla JS or lightweight Preact (decision for crafter)

**Behavior:**
1. On mount: read `visitor_id` from localStorage (create if absent)
2. Fetch `GET /api/posts/{slug}/likes/check/{visitorId}` to get server state
3. Reconcile: if server says liked, show filled heart even if localStorage disagrees
4. On click: if not liked, optimistic UI (fill heart, increment count), then `POST /api/posts/{slug}/likes`
5. On click: if already liked, no action (prevent double-like)
6. Store `visitor_liked_{slug}` in localStorage on successful like
7. On network failure: heart stays filled (optimistic), reconciles on next page load

**State management:** localStorage for `visitor_id` and `visitor_liked_{slug}` flags. Server is source of truth for count.

**Accessibility:**
- `<button>` element with `aria-label="Like this post"` (or `"Liked"` when active)
- Keyboard focusable, activatable with Enter/Space
- Minimum 44x44px touch target
- `aria-pressed="true/false"` for toggle state

**Graceful degradation:** The island is rendered with `display: none` in static HTML. The island script sets `display: flex` on mount. No JS = no like button = no broken UI.

---

## Island 2: ShareButton

**Hydration:** `client:visible`
**Props:** `slug: string`, `title: string`, `siteUrl: string`
**Framework:** Vanilla JS (tiny -- no framework needed)

**Behavior:**
1. Feature detection on mount:
   - `navigator.share` available -> use Web Share API
   - `navigator.clipboard` available -> use clipboard copy
   - Neither -> show selectable URL popover
2. On click (Web Share API path): call `navigator.share({ title, url })`
3. On click (clipboard path): copy URL, show toast "Link copied!" for 3 seconds
4. On click (fallback path): toggle popover with selectable URL text

**Toast notification:**
- Positioned bottom-center or top-right (design system decision)
- `aria-live="polite"` region for screen reader announcement
- Auto-dismiss after 3 seconds
- CSS animation for enter/exit

**Accessibility:**
- `<button>` element with `aria-label="Share this post"`
- Keyboard focusable, activatable with Enter/Space
- Minimum 44x44px touch target
- Popover dismissible with Escape key

**Graceful degradation:** Same pattern as LikeButton -- hidden in static HTML, visible on mount.

---

## Island 3: CommentSection

**Hydration:** `client:visible`
**Props:** `slug: string`, `apiBase: string`
**Framework:** Preact recommended (more complex state: auth, form, list)

**Behavior flow:**

### Phase 1: Load comments
1. Fetch `GET /api/posts/{slug}/comments`
2. Render header "Comments (N)" with count
3. Render comment list (or empty state message)

### Phase 2: Check auth state
1. Fetch `GET /api/auth/session` (credentials: include for cookie)
2. If authenticated: show comment form with "Signed in as {name} ({provider})"
3. If not authenticated: show "Sign in with Google" and "Sign in with GitHub" buttons

### Phase 3: Sign-in flow
1. On sign-in button click: redirect to `GET /api/auth/oauth/{provider}?returnUrl={currentUrl}`
2. OAuth flow happens (redirect to provider, consent, callback)
3. After redirect back: page reloads, session cookie is set, step 2 shows comment form
4. Check URL for `?auth_error=denied` (show nothing) or `?auth_error=failed` (show error)

### Phase 4: Post comment
1. Textarea with character counter (visible at 1800+ chars)
2. "Post Comment" button disabled when textarea empty or over 2000 chars
3. On submit: `POST /api/posts/{slug}/comments` with `{ text }` (credentials: include)
4. On success: append comment to list, increment count, show toast, clear textarea
5. On 401: session expired -- save draft to localStorage, show re-auth message
6. On network error: preserve text in textarea, show error message
7. After re-auth: check localStorage for draft, pre-populate textarea

### Phase 5: Sign out
1. On "Sign out" click: `POST /api/auth/signout` (credentials: include)
2. Replace comment form with sign-in buttons
3. Comments list remains visible (read is public)

**Draft preservation:**
- localStorage key: `comment_draft_{slug}`
- Saved on: session expiry error (401), periodically while typing (debounced)
- Restored on: page load if draft exists
- Cleared on: successful comment post

**Accessibility:**
- `<textarea>` with associated `<label>` ("Write a comment")
- After OAuth sign-in (page reload): page scrolls to comment section via URL fragment (`#comments`). If client-side auth were used (future), focus would programmatically move to textarea.
- Sign-in buttons have `aria-label="Sign in with Google to comment"` etc.
- Each comment in `<article>` element with semantic structure
- Character counter announced via `aria-live="polite"`
- Toast for "Comment posted." via `aria-live="polite"`
- Color contrast 4.5:1 for all text

**Graceful degradation:** Hidden in static HTML. Without JS, no comments section visible, post content remains fully readable.

---

## Open Graph Meta Tags (Static -- Not an Island)

Implemented in the Astro layout template (`BaseLayout.astro` or `[slug].astro` `<head>`). Generated at build time, not runtime.

**Tags to add in `<head>`:**
```html
<meta property="og:title" content="{post.title}" />
<meta property="og:description" content="{post.excerpt}" />
<meta property="og:url" content="{siteUrl}/blog/{post.slug}" />
<meta property="og:type" content="article" />
<meta property="og:image" content="{post.featuredImageUrl || defaultOgImage}" />
<meta name="twitter:card" content="summary_large_image" />
```

**Default OG image:** Static asset in `frontend/public/` (e.g., `public/og-default.png`). Blog banner/logo image.

**Source of truth:** Post data fetched at build time already contains title, slug, excerpt, featuredImageUrl.

---

## API Client Configuration

The engagement islands need the API base URL. Options:
- Pass as prop from Astro (using `import.meta.env.PUBLIC_API_URL`)
- Same pattern as existing `frontend/src/data/api.ts`

For session endpoints, `fetch` calls must include `credentials: 'include'` to send the httpOnly cookie.

---

## CORS Update Required

The existing CORS policy:
```csharp
policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader()
```

Must add `.AllowCredentials()` to support the reader session cookie:
```csharp
policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials()
```

**Note:** `AllowCredentials()` is incompatible with wildcard origins, but the existing config already uses explicit origins.

---

## Astro Configuration Change

The current `astro.config.mjs` uses `output: 'static'`. No change needed. Astro Islands with `client:visible` work within static output mode -- they hydrate client-side after the static page loads.

If a Preact island framework is chosen, add the Astro Preact integration:
```bash
npx astro add preact
```

---

## File Structure (Frontend)

```
frontend/src/
  components/
    engagement/
      LikeButton.tsx        # Preact or vanilla JS island
      ShareButton.tsx        # Preact or vanilla JS island
      CommentSection.tsx     # Preact island (complex state)
      Toast.tsx              # Shared toast notification component
  pages/
    blog/
      [slug].astro           # Updated to include engagement islands
  public/
    og-default.png           # Default OG image for posts without featured image
```

---

## Performance Budget

| Component | Target | Strategy |
|-----------|--------|----------|
| LikeButton bundle | < 5 KB gzipped | Minimal JS, no framework or Preact |
| ShareButton bundle | < 3 KB gzipped | Vanilla JS only |
| CommentSection bundle | < 15 KB gzipped | Preact (3KB) + component logic |
| Total engagement JS | < 25 KB gzipped | Lazy load via `client:visible` |
| Like API response | < 500ms p95 | Simple DB query |
| Comments API response | < 1s | Single query with index |

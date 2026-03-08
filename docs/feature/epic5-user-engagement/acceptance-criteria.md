<!-- markdownlint-disable MD024 -->
# Acceptance Criteria -- Epic 5: User Engagement

All acceptance criteria derived from UAT scenarios. Organized by user story for traceability.

---

## US-050: Like a Blog Post

### Functional Criteria

- [ ] Heart icon with like count displayed at footer of every published post
- [ ] Tapping/clicking the heart icon fills it with color and increments count by 1
- [ ] Same visitor cannot like the same post twice (deduplication via localStorage `visitor_id`)
- [ ] Like state (filled/outlined) persists across visits via localStorage
- [ ] Clearing localStorage allows the visitor to like again (accepted trade-off)
- [ ] Like interaction uses optimistic UI: visual update before API response

### Non-Functional Criteria

- [ ] Like button interaction responds in under 100ms (optimistic UI)
- [ ] API POST completes in under 500ms (p95)
- [ ] Heart icon has aria-label ("Like this post" or "Liked")
- [ ] Heart icon is keyboard-focusable and activatable with Enter/Space
- [ ] Touch target minimum 44x44px
- [ ] Like button hidden when JavaScript is disabled
- [ ] No personally identifiable information stored for likes
- [ ] Rate limiting: max 5 likes per visitor_id per 10 seconds

---

## US-051: Sign In with Social Login to Comment

### Functional Criteria

- [ ] Two sign-in buttons: "Sign in with Google" and "Sign in with GitHub"
- [ ] Clicking a button redirects to the OAuth provider consent screen
- [ ] On consent granted: redirect back to the post with session established
- [ ] Display name and avatar URL extracted from OAuth profile
- [ ] Email from OAuth provider is NOT stored or displayed
- [ ] Session persists across visits via httpOnly cookie
- [ ] "Sign out" button clears the session and shows sign-in prompt
- [ ] OAuth consent denied: return to post, no error message
- [ ] OAuth provider error: return to post with "Sign in failed. Please try again."

### Non-Functional Criteria

- [ ] OAuth tokens never exposed to client-side JavaScript
- [ ] Session cookie: httpOnly, Secure, SameSite=Lax
- [ ] Sign-in buttons have descriptive aria-labels including provider name
- [ ] OAuth redirect round-trip completes in under 5 seconds (provider-dependent)
- [ ] After OAuth sign-in: if page reloads, page scrolls to comment section; if client-side auth, focus programmatically moves to textarea

---

## US-052: Post a Comment on a Blog Post

### Functional Criteria

- [ ] Comment textarea displayed when reader is authenticated
- [ ] "Post Comment" button disabled when textarea is empty
- [ ] Character counter appears at 1800+ characters
- [ ] Error message and button disabled at 2000 characters
- [ ] Comment appears immediately in the thread after successful POST
- [ ] Comment displays: avatar, display name, provider badge, relative timestamp (e.g., "just now", "2 hours ago" — client-side formatted from ISO-8601 `createdAt`), text
- [ ] Toast "Comment posted." shown on success
- [ ] Network failure: error message shown, comment text preserved in textarea
- [ ] Session expiry: error message shown, draft saved to localStorage, pre-populated after re-auth

### Non-Functional Criteria

- [ ] Comment POST API completes in under 500ms (p95)
- [ ] Comment text sanitized server-side: HTML tags stripped or escaped before persistence (e.g., posting `<script>alert(1)</script>` results in visible escaped text, not JS execution)
- [ ] CSRF protection on comment POST endpoint
- [ ] Rate limiting: max 5 comments per user per 10 minutes
- [ ] Textarea has associated label
- [ ] Keyboard accessible (tab to textarea, tab to submit, Enter does not submit)

---

## US-053: Author Moderates Comments

### Functional Criteria

- [ ] Admin panel lists all comments across all posts, newest first
- [ ] Each entry shows: post title, commenter name, provider, date, text, delete button
- [ ] Delete requires confirmation dialog: "Delete this comment by {display_name}?"
- [ ] Confirmed deletion removes comment from public post view
- [ ] Comment count on the post updates after deletion
- [ ] No notification sent to commenter when their comment is deleted
- [ ] Attempting to delete already-deleted comment: "Comment not found"

### Non-Functional Criteria

- [ ] Delete endpoint requires admin JWT authentication
- [ ] Admin panel loads in under 1 second

---

## US-054: Share a Blog Post

### Functional Criteria

- [ ] Share button (share icon + "Share" label) displayed alongside like button
- [ ] On mobile/browsers with Web Share API: native share sheet with post title and URL
- [ ] On desktop without Web Share API: copy post URL to clipboard
- [ ] Toast "Link copied!" displayed on clipboard copy, auto-dismisses after 3 seconds
- [ ] On browsers without Clipboard API: popover with selectable URL text
- [ ] Share button hidden when JavaScript is disabled

### Non-Functional Criteria

- [ ] Share button interaction responds in under 100ms
- [ ] Share button has aria-label "Share this post"
- [ ] Share button is keyboard-focusable and activatable with Enter/Space
- [ ] Touch target minimum 44x44px
- [ ] Toast announced to screen readers (aria-live: polite)
- [ ] No third-party tracking scripts loaded
- [ ] No social media SDKs

---

## US-055: Open Graph Meta Tags for Social Sharing

### Functional Criteria

- [ ] Every post page has `og:title` matching the post title
- [ ] Every post page has `og:description` with the post excerpt
- [ ] Every post page has `og:url` with the canonical post URL
- [ ] Every post page has `og:type` set to "article"
- [ ] Posts with featured image: `og:image` with ImageKit URL
- [ ] Posts without featured image: `og:image` with default blog image
- [ ] Every post page has `twitter:card` set to "summary_large_image"

### Non-Functional Criteria

- [ ] OG tags generated at build time (Astro layout template)
- [ ] Default OG image is a static asset (no external dependency)
- [ ] `og:url` matches the canonical URL exactly (no trailing slashes, no query params)

---

## US-056: View Comments on a Post

### Functional Criteria

- [ ] Comments section visible below every published post article
- [ ] Section header: "Comments (N)" with correct count
- [ ] Each comment displays: avatar, display name, provider badge, timestamp, text
- [ ] Empty state: "No comments yet. Be the first to share your thoughts."
- [ ] Comments listed in chronological order (oldest first)

### Non-Functional Criteria

- [ ] Comments section loads in under 1 second (Astro Island hydration)
- [ ] Semantic HTML: each comment in an `<article>` element
- [ ] Comments fetched from API on hydration (not SSG cached -- content is dynamic)
- [ ] Color contrast meets 4.5:1 ratio for all comment text

---

## Cross-Cutting Acceptance Criteria

These apply to all engagement features:

### Privacy

- [ ] No third-party tracking scripts loaded by engagement features
- [ ] No social media SDKs loaded
- [ ] Like feature stores no PII (anonymous visitor_id only)
- [ ] Comment feature stores minimal data (display_name, avatar_url, provider -- no email)

### Graceful Degradation

- [ ] All engagement features hidden when JavaScript is disabled
- [ ] Post content remains fully readable and accessible without JavaScript
- [ ] No broken UI elements when Astro Islands fail to hydrate

### Consistency with Design System

- [ ] Engagement UI elements follow "Forge & Ink" design system
- [ ] Typography, spacing, and color consistent with existing blog design
- [ ] Animations are CSS-only where possible (heart fill animation may need JS for state)

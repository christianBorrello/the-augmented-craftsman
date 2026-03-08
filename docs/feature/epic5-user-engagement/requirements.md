# Requirements Specification -- Epic 5: User Engagement

**Epic**: Likes, Comments & Sharing
**Project**: The Augmented Craftsman
**Audience**: Developers interested in TDD, DDD, Clean Architecture, XP
**Budget**: No additional cost (all services have free tiers)
**Architecture**: Hexagonal + Vertical Slice (.NET 10) | Astro SSG + Islands | PostgreSQL

---

## 1. Functional Requirements

### 1.1 Engagement Context -- Likes

| ID | Requirement | Details |
|----|-------------|---------|
| FR-E01 | Like a post | Reader likes a post with a single tap/click. No authentication required. Like is anonymous. |
| FR-E02 | One like per visitor per post | Same visitor cannot like the same post twice. Deduplication based on `visitor_id` stored in localStorage. |
| FR-E03 | Like count display | Each post displays total like count next to the heart icon. Count fetched from API on page load. |
| FR-E04 | Like state persistence | After liking, the heart icon shows as filled on return visits. State stored in localStorage under `visitor_liked_{slug}`. |
| FR-E05 | Optimistic UI for likes | Heart fills and count increments immediately on tap, before API response returns. Reconciles with server on next page load. |
| FR-E06 | Like graceful degradation | Like button is hidden when JavaScript is disabled. Post content remains fully readable. |

### 1.2 Engagement Context -- Comments

| ID | Requirement | Details |
|----|-------------|---------|
| FR-E10 | View comments on a post | Comments displayed below the article in chronological order. Each comment shows avatar, display name, provider badge ("via GitHub"/"via Google"), timestamp, and text. |
| FR-E11 | Comment count display | Comments section header shows count: "Comments (N)". Empty state: "No comments yet. Be the first to share your thoughts." |
| FR-E12 | Sign in with OAuth to comment | Reader must sign in with Google or GitHub OAuth before commenting. Two buttons: "Sign in with Google", "Sign in with GitHub". |
| FR-E13 | OAuth identity extraction | Display name and avatar URL extracted from OAuth provider profile. Email is NOT stored or displayed. Provider name recorded. |
| FR-E14 | Post a comment | Authenticated reader types comment (max 2000 characters) and clicks "Post Comment". Comment appears immediately (post-moderation). |
| FR-E15 | Comment validation | Empty comments rejected (button disabled). Character counter appears at 1800+ characters. Over 2000 characters shows error and disables submit. |
| FR-E16 | Comment session persistence | OAuth session persists across visits via httpOnly cookie. Reader remains signed in on return. |
| FR-E17 | Sign out | Reader can sign out. Comment form reverts to sign-in prompt. |
| FR-E18 | Comment text preservation | On network failure or session expiry during submit, comment text is preserved (textarea or localStorage). Re-auth pre-populates the draft. |
| FR-E19 | Author moderates comments | Admin can view all comments across posts and delete inappropriate ones. Confirmation dialog before deletion. No notification to commenter. |
| FR-E20 | Comment sanitization | Comment text sanitized server-side to prevent XSS. HTML tags stripped or escaped. |

### 1.3 Engagement Context -- Sharing

| ID | Requirement | Details |
|----|-------------|---------|
| FR-E30 | Share a post (Web Share API) | Reader taps share button. On supported browsers, invokes `navigator.share()` with post title and URL. |
| FR-E31 | Share fallback (clipboard) | On browsers without Web Share API, clicking share copies post URL to clipboard. Toast: "Link copied!" auto-dismisses after 3 seconds. |
| FR-E32 | Share fallback (selectable URL) | On browsers without Web Share API or Clipboard API, show a popover with the URL as selectable text. |
| FR-E33 | Share graceful degradation | Share button hidden when JavaScript is disabled. Post URL available in browser address bar. |
| FR-E34 | Open Graph meta tags | Every post page includes og:title, og:description, og:url, og:image, og:type, and twitter:card meta tags. og:image falls back to default blog image when no featured image. |

---

## 2. Non-Functional Requirements

### 2.1 Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-E01 | Like button interaction | < 100ms visual response (optimistic UI) |
| NFR-E02 | Like API POST | < 500ms (p95) |
| NFR-E03 | Comments section load | < 1s from Astro Island hydration |
| NFR-E04 | Comment POST | < 500ms (p95) |
| NFR-E05 | Share button interaction | < 100ms visual response |
| NFR-E06 | OAuth redirect round-trip | < 5s (provider-dependent, not our SLA) |

### 2.2 Accessibility

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-EA01 | Like button | aria-label: "Like this post" / "Liked". Keyboard focusable + activatable (Enter/Space). |
| NFR-EA02 | Like button touch target | Minimum 44x44px |
| NFR-EA03 | Share button | aria-label: "Share this post". Keyboard focusable + activatable (Enter/Space). |
| NFR-EA04 | Share button touch target | Minimum 44x44px |
| NFR-EA05 | Toast notifications | Announced to screen readers (aria-live: polite) |
| NFR-EA06 | Comment form | Textarea has associated label. Focus moves to form after sign-in. |
| NFR-EA07 | Comments list | Semantic HTML (article elements for each comment) |
| NFR-EA08 | Sign-in buttons | Descriptive aria-labels including provider name |
| NFR-EA09 | Color contrast | All engagement UI elements meet 4.5:1 contrast ratio |

### 2.3 Security

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-ES01 | OAuth tokens | Never exposed to client-side JavaScript. Exchanged server-side for session. |
| NFR-ES02 | Session cookie | httpOnly, Secure, SameSite=Lax |
| NFR-ES03 | Comment XSS prevention | All comment text sanitized server-side |
| NFR-ES04 | CSRF protection | Comment POST endpoint protected against CSRF |
| NFR-ES05 | Rate limiting (likes) | Max 5 likes per visitor_id per 10 seconds (silent ignore) |
| NFR-ES06 | Rate limiting (comments) | Max 5 comments per user per 10 minutes |
| NFR-ES07 | Like API abuse | Ignore duplicate likes from same visitor_id silently |

### 2.4 Privacy

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-EP01 | Like anonymity | No personally identifiable information stored for likes. visitor_id is anonymous UUID. |
| NFR-EP02 | Comment minimal data | Only display_name, avatar_url, and provider stored from OAuth. Email NOT stored. |
| NFR-EP03 | No tracking | No third-party tracking scripts. No social media SDKs. No analytics on engagement button clicks. |
| NFR-EP04 | Sharing privacy | Web Share API is browser-native. No data sent to third parties by the share feature. |

### 2.5 Resilience

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-ER01 | Like offline resilience | Optimistic UI handles network failures. Reconciles on next page load. |
| NFR-ER02 | Comment draft preservation | Comment text preserved on network failure or session expiry. |
| NFR-ER03 | Share fallback chain | Three-tier fallback: Web Share API -> Clipboard -> Selectable URL. |
| NFR-ER04 | JS-disabled degradation | All engagement features degrade gracefully. Post content always accessible. |

---

## 3. Business Rules

| ID | Rule | Details |
|----|------|---------|
| BR-E01 | One like per visitor per post | Enforced by visitor_id. Clearing localStorage resets identity (acceptable). |
| BR-E02 | Comments require social login | No anonymous commenting. No custom accounts. Google and GitHub only. |
| BR-E03 | Post-moderation | Comments appear immediately. Author removes inappropriate ones at their own pace. |
| BR-E04 | Comment character limit | Maximum 2000 characters per comment. |
| BR-E05 | No comment editing | Commenters cannot edit their comments after posting (v1 simplification). |
| BR-E06 | No comment replies/threading | Comments are a flat list, not threaded (v1 simplification). |
| BR-E07 | No notifications | No email or push notifications for new comments (v1 simplification). |
| BR-E08 | Share uses native APIs only | No third-party social SDKs. Privacy-first. |

---

## 4. Bounded Context: Engagement

```
+-------------------+
|                   |
|  Engagement       |
|  Context          |
|                   |
|  - Like           |
|  - Comment        |
|  - Share (FE only)|
|                   |
+--------+----------+
         |
         | depends on
         v
+--------+----------+     +-------------------+
|                   |     |                   |
|  Content Context  |     |  Identity Context |
|                   |     |  (extended)       |
|  - BlogPost       |     |                   |
|  - Slug           |     |  - Admin JWT      |
|  - Title          |     |  - Reader OAuth   |
|                   |     |  - Session mgmt   |
+-------------------+     +-------------------+
```

**Note**: Share is a frontend-only feature (Web Share API / Clipboard). It has no backend component beyond existing OG meta tags. The Engagement Context introduces two new domain concepts: **Like** and **Comment**. It extends the Identity Context with reader-facing OAuth (distinct from admin JWT auth).

---

## 5. Ubiquitous Language (Additions)

| Term | Definition |
|------|-----------|
| Like | An anonymous appreciation signal for a post. One per visitor per post. No authentication. |
| Comment | A text response to a post, attributed to a social-login identity. Max 2000 characters. |
| Visitor | An anonymous blog reader identified by a client-side UUID (visitor_id). Not a user account. |
| Commenter | A reader who has authenticated via OAuth to leave comments. Has display name and avatar. |
| Post-moderation | Moderation model where content appears immediately and is reviewed/removed after the fact. |
| Provider | The OAuth identity source: "google" or "github". |
| Share | The action of distributing a post URL to an external channel via native device APIs. |
| Engagement | The collective term for likes, comments, and shares on the blog. |

---

## 6. Technical Implementation Notes

### OAuth Provider Setup

| Concern | Decision |
|---------|----------|
| Google OAuth | Google Cloud Console project (free). OAuth 2.0 consent screen. Redirect URI to backend. |
| GitHub OAuth | GitHub Settings > Developer Settings > OAuth Apps (free). Redirect URI to backend. |
| Credential storage | `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GITHUB_CLIENT_ID`, `GITHUB_CLIENT_SECRET` in environment variables |
| Token exchange | Server-side only. Frontend never sees OAuth tokens. |
| Session | httpOnly cookie with server-side session store (in-memory or database-backed). |

### Astro Island Architecture

| Concern | Decision |
|---------|----------|
| Like button | Separate Astro Island, `client:visible`, lightweight |
| Comment section | Separate Astro Island, `client:visible`, manages auth state |
| Share button | Separate Astro Island, `client:visible`, lightweight |
| Hydration | All three islands hydrate lazily (on visible) to avoid blocking page load |
| Static content | Article body, tags, metadata remain static HTML (not in islands) |

### Comment Data Model Consideration

| Concern | Decision |
|---------|----------|
| Aggregate boundary | Comment is a separate entity, not a child of BlogPost aggregate. BlogPost should not grow unboundedly. |
| Relationship | Comment references BlogPost by slug (or post_id). One-to-many. |
| Identity | Commenter identity stored on Comment: display_name, avatar_url, provider, provider_user_id. |
| Ordering | Displayed chronologically (oldest first) for conversation flow. |

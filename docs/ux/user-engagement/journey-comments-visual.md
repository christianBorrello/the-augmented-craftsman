# Journey: Reader Comments on a Post

## Persona

**Tomasz Kowalski** -- Senior developer, 8 years experience, active on GitHub, reads the blog at his desk. He wants to share a counterpoint or ask a clarifying question about a post on Outside-In TDD. He has a GitHub account and expects social login to be fast and trustworthy.

**Maria Santos** -- Junior developer, 2 years experience, has a Google account but no GitHub. She wants to thank the author for a helpful post on Clean Architecture. She expects commenting to be as simple as leaving a YouTube comment.

---

## Goal

Authenticated reader leaves a comment on a blog post. Comments appear immediately (post-moderation). Identity comes from social login (Google or GitHub OAuth).

## Trigger

Reader finishes a post and wants to respond -- ask a question, share an experience, or offer thanks. They scroll to the comments section below the article.

## Success

Comment is posted, appears immediately in the thread, and the reader sees their social profile name next to it. On return visits, they see their comment in context.

---

## Happy Path Flow

```
  READING POST      SCROLL TO COMMENTS    CLICK "SIGN IN"     OAUTH FLOW          WRITE COMMENT       COMMENT POSTED
+---------------+ +------------------+  +-----------------+ +----------------+  +------------------+ +-----------------+
| Tomasz reads  | | Sees comment     |  | Clicks "Sign in |  | GitHub OAuth   |  | Types comment    | | Comment appears |
| "Outside-In   |->| section:        |->| with GitHub"    |->| consent screen |->| in text area     |->| immediately     |
| TDD" article  | | 3 existing       |  | button          |  | (one click)    |  | Clicks "Post     | | with GitHub     |
|               | | comments shown   |  |                 |  |                |  |  Comment"         | | avatar + name   |
+---------------+ +------------------+  +-----------------+ +----------------+  +------------------+ +-----------------+
       |                  |                     |                   |                    |                    |
       v                  v                     v                   v                    v                    v
  Engaged            Curious,              Confident,          Brief pause,          Focused,            Satisfied,
  "Good post,        social proof          trusts GitHub       expected             composing            "My voice
   have thoughts"    "Others commented"    "One click"         redirect             response             is heard"
```

## Emotional Arc

```
Confidence
   5 |                                                       *-----------*
   4 |                                   *                                          *
   3 |         *-----------*
   2 |  *
   1 |                              (dip: OAuth redirect, brief uncertainty)
     +------------------------------------------------------------------>
       Reading   See Comments   Sign In    OAuth Flow   Write Comment   Posted
```

The OAuth redirect is the only moment of mild uncertainty ("will this work?"). It resolves quickly -- GitHub/Google OAuth for developers is a familiar, trusted flow. Confidence rises once authenticated and stays high through posting.

---

## Step Detail

| Step | Reader Does | System Responds | Artifacts | Emotional State |
|------|-------------|-----------------|-----------|-----------------|
| Reading post | Scrolls past article content | Comments section visible below article | `slug`, `current_post` | Engaged |
| See comments | Reads existing comments (if any) | Display comments: avatar, name, date, text. Show count. | `comments[]`, `comment_count` | Curious, social proof |
| Sign in prompt | Sees "Sign in to comment" with provider buttons | Two buttons: "Sign in with Google", "Sign in with GitHub" | -- | Confident (trusted providers) |
| OAuth flow | Clicks provider button | Redirect to provider consent screen. On success, redirect back to post with session. | `oauth_token`, `display_name`, `avatar_url`, `provider` | Brief pause, then relief |
| Write comment | Types in textarea, clicks "Post Comment" | POST to API. Validate non-empty. Save comment. | `comment_id`, `comment_text`, `created_at` | Focused, composing |
| Comment posted | Sees own comment at top/bottom of thread | Comment appears with avatar, name, timestamp. Toast: "Comment posted." | `comment` (added to `comments[]`) | Satisfied |
| Return visit | Returns to same post | Own comment visible in thread. Still signed in (session persists). | `session_token` | Recognized |

---

## Web UI Mockup -- Comments Section (Desktop)

```
+-----------------------------------------------------------------------+
|                                                                       |
|  Comments (3)                                                        |
|                                                                       |
|  +-------------------------------------------------------------------+
|  | [avatar] Tomasz Kowalski · via GitHub · 2 hours ago               |
|  |                                                                   |
|  | Great breakdown of the double loop! I've been struggling with     |
|  | where to start the outer test. This clarified my mental model.    |
|  +-------------------------------------------------------------------+
|  |                                                                   |
|  | [avatar] Maria Santos · via Google · 5 hours ago                  |
|  |                                                                   |
|  | Thank you for this! Finally understanding how ports and adapters  |
|  | fit together.                                                     |
|  +-------------------------------------------------------------------+
|  |                                                                   |
|  | [avatar] Dev Reader · via GitHub · 1 day ago                      |
|  |                                                                   |
|  | Would love to see a follow-up on property-based testing.          |
|  +-------------------------------------------------------------------+
|                                                                       |
|  +-------------------------------------------------------------------+
|  |  Sign in to comment                                               |
|  |                                                                   |
|  |  [Google icon] Sign in with Google                                |
|  |  [GitHub icon] Sign in with GitHub                                |
|  +-------------------------------------------------------------------+
|                                                                       |
+-----------------------------------------------------------------------+
```

## Web UI Mockup -- After Sign In

```
+-----------------------------------------------------------------------+
|                                                                       |
|  +-------------------------------------------------------------------+
|  |  Signed in as Tomasz Kowalski (GitHub)          [Sign out]        |
|  |                                                                   |
|  |  +---------------------------------------------------------------+|
|  |  |                                                               ||
|  |  | Write your comment...                                         ||
|  |  |                                                               ||
|  |  +---------------------------------------------------------------+|
|  |                                                                   |
|  |                                    [Post Comment]                 |
|  +-------------------------------------------------------------------+
|                                                                       |
+-----------------------------------------------------------------------+
```

## Web UI Mockup -- Mobile

```
+-----------------------------------+
|                                   |
|  Comments (3)                    |
|                                   |
|  [avatar] Tomasz K. · GitHub     |
|  · 2 hours ago                   |
|                                   |
|  Great breakdown of the double   |
|  loop! I've been struggling...   |
|                                   |
|  ---                             |
|                                   |
|  Sign in to comment             |
|                                   |
|  [Google] Sign in with Google    |
|  [GitHub] Sign in with GitHub    |
|                                   |
+-----------------------------------+
```

---

## Error Paths

```
OAuth flow fails (provider error):
  Reader is redirected back to the post
  Message: "Sign in failed. Please try again."
  Comment form remains in "sign in" state
  No session created

OAuth flow cancelled (user clicks "Deny"):
  Reader returns to post page
  No message shown (user chose to cancel)
  Comment form remains in "sign in" state

Empty comment submitted:
  "Post Comment" button disabled when textarea is empty
  If somehow submitted empty: "Comment cannot be empty."
  Focus returns to textarea

Comment too long (>2000 characters):
  Character counter appears at 1800+ characters
  At 2000: "Comment is too long (2000 character limit)"
  "Post Comment" button disabled

Network error on comment submission:
  "Could not post your comment. Check your connection and try again."
  Comment text preserved in textarea (not lost)
  Reader can retry

Session expired during writing:
  On submit: "Your session has expired. Please sign in again."
  Comment text preserved in localStorage
  After re-auth, textarea pre-populated with draft

Moderation: comment removed by author:
  Comment disappears from thread
  No notification to commenter (post-moderation is quiet removal)
  Commenter sees their comment gone on next visit

Provider account suspended/deleted:
  OAuth flow fails at provider level
  Same handling as "OAuth flow fails"
```

---

## Cross-Device Behavior

| Scenario | Behavior | Rationale |
|----------|----------|-----------|
| Desktop GitHub login, then mobile | Must sign in again on mobile | Session is per-device |
| Same browser, return visit | Session persists (cookie-based) | Convenience for returning commenters |
| Different provider on different device | Same person may appear as two identities | Acceptable -- no account merging for a blog |
| Signed in via Google, wants to switch to GitHub | "Sign out" then sign in with other provider | Simple, no linking needed |

---

## Shared Artifacts

| Artifact | Source | Consumers |
|----------|--------|-----------|
| `oauth_token` | OAuth provider callback | API authentication for comment creation |
| `session_token` | Backend session after OAuth | Comment API, sign-out, identity display |
| `display_name` | OAuth provider profile | Comment display, "Signed in as..." |
| `avatar_url` | OAuth provider profile | Comment avatar display |
| `provider` | OAuth callback metadata | "via GitHub" / "via Google" badge |
| `comment_id` | API response on POST | Comment rendering, moderation |
| `comment_text` | User input | Comment display |
| `created_at` | API response on POST | Comment timestamp |
| `slug` | Post URL | Comment API endpoint path |
| `comment_count` | API response `GET /api/posts/{slug}/comments` | Comments section header |

---

## Astro Island Notes

The comments section is an interactive Astro Island. It hydrates on visible and manages its own state (auth, comment list, form).

```
[Static HTML page]
  |
  +-- [Static: Article content]
  |
  +-- [Astro Island: CommentsSection]
  |     - client:visible
  |     - props: { slug }
  |     - fetches: GET /api/posts/{slug}/comments
  |     - manages: OAuth flow, comment form, comment list
  |     - session: httpOnly cookie for auth state
```

## Author Moderation Flow (Brief)

```
  ADMIN VIEW           REVIEW COMMENT       DELETE COMMENT
+---------------+    +-----------------+   +-----------------+
| Christian sees |    | Reads comment   |   | Clicks delete   |
| comment list   |--->| content, checks |--->| Confirms in     |
| in admin panel |    | for spam/abuse  |   | dialog          |
+---------------+    +-----------------+   +-----------------+
       |                     |                     |
       v                     v                     v
  In control           Evaluating             Resolved
```

This is a lightweight admin action -- the author reviews comments at their own pace and removes inappropriate ones. No approval queue, no notification system, no urgency.

<!-- markdownlint-disable MD024 -->
# User Stories -- Epic 5: User Engagement

**Prioritization**: MoSCoW (Must / Should / Could)
**Sizing**: S (< 1 day) | M (1-2 days) | L (2-3 days)
**Traceability**: Each story references its journey step (J5-J7) and bounded context

---

## US-050: Like a Blog Post

### Problem

Ana Ferreira is a backend developer who reads the blog on her phone during commute. She finds it impossible to express appreciation for a post she enjoyed. There is no feedback mechanism -- she either bookmarks the URL or moves on, and the author never knows which posts resonated.

### Who

- Blog reader | Reading a post they found valuable | Wants to signal appreciation with zero friction

### Solution

An anonymous like button (heart icon with count) on every post page. One tap to like, no login required.

**Size**: M
**Priority**: Must
**Bounded Context**: Engagement
**Journey**: J5 steps 1-4

### Domain Examples

#### 1: Happy Path -- Ana likes a TDD post

Ana finishes reading "TDD Is Not About Testing" (slug: `tdd-is-not-about-testing`). The post has 12 likes. She taps the heart icon. The heart fills immediately, the count shows 13, and a brief animation confirms her action. Her localStorage now has `visitor_liked_tdd-is-not-about-testing = true`.

#### 2: Return Visit -- Like persists

Ana returns to "TDD Is Not About Testing" three days later on the same phone. The heart icon is already filled. The count shows 15 (others liked it too). She cannot like again.

#### 3: Edge Case -- localStorage cleared

Ana cleared her browser data. She visits "TDD Is Not About Testing" again. The heart is outlined (not filled). She can like again. The count goes from 15 to 16. This is an accepted trade-off for anonymous, no-auth likes.

### UAT Scenarios (BDD)

#### Scenario: Reader likes a post

Given Ana Ferreira is reading "TDD Is Not About Testing" which has 12 likes
And Ana has not previously liked this post
When Ana taps the heart icon
Then the heart icon fills with color
And the like count displays 13
And the visitor_liked flag for "tdd-is-not-about-testing" is stored in localStorage

#### Scenario: Like persists on return visit

Given Ana Ferreira previously liked "TDD Is Not About Testing"
And the visitor_liked flag exists in localStorage
When Ana visits "TDD Is Not About Testing" again
Then the heart icon is displayed as filled
And the current like count is shown

#### Scenario: Reader cannot like the same post twice

Given Ana Ferreira already liked "TDD Is Not About Testing"
When Ana taps the heart icon again
Then the like count does not change

#### Scenario: Like uses optimistic UI

Given Ana is reading "Value Objects in Practice" which has 5 likes
When Ana taps the heart icon
Then the heart fills immediately without waiting for the API response
And the count displays 6 immediately

#### Scenario: Like button hidden without JavaScript

Given a reader visits "TDD Is Not About Testing" with JavaScript disabled
Then the like button is not visible
And the post content is fully readable

### Acceptance Criteria

- [ ] Heart icon with count displayed on every post page
- [ ] Single tap/click toggles like state (outlined to filled)
- [ ] Like count increments by 1 on like
- [ ] Same visitor cannot like same post twice (localStorage deduplication)
- [ ] Like state persists across visits via localStorage
- [ ] Optimistic UI update (immediate visual feedback before API response)
- [ ] Like button hidden when JavaScript disabled (Astro Island graceful degradation)
- [ ] Minimum 44x44px touch target
- [ ] aria-label on heart icon ("Like this post" / "Liked")

### Technical Notes

- Astro Island: `client:visible` hydration
- `visitor_id` generated client-side (UUID), stored in localStorage
- API endpoint: `POST /api/posts/{slug}/likes` with `visitor_id` body
- API endpoint: `GET /api/posts/{slug}/likes` returns `{ count: number }`
- Rate limiting: max 5 likes per visitor_id per 10 seconds (silent ignore)
- No PII stored server-side

### Dependencies

- US-043 (single post view must exist)
- Post pages must support Astro Islands

---

## US-051: Sign In with Social Login to Comment

### Problem

Tomasz Kowalski is a senior developer who wants to ask a clarifying question about an Outside-In TDD post. He finds it frustrating that there is no way to engage in discussion. He has a GitHub account and expects social login to be fast -- he will not create a custom account for a blog.

### Who

- Blog reader | Wants to comment on a post | Expects one-click social login, no custom account

### Solution

OAuth sign-in with Google or GitHub. One-click, display name from social profile, session persists across visits.

**Size**: L
**Priority**: Must
**Bounded Context**: Identity (extended) + Engagement
**Journey**: J6 steps 2-3

### Domain Examples

#### 1: Happy Path -- Tomasz signs in with GitHub

Tomasz clicks "Sign in with GitHub". He sees the GitHub consent screen. He clicks "Authorize". He is redirected back to the post page. The comment form shows "Signed in as Tomasz Kowalski (GitHub)" with his GitHub avatar.

#### 2: Happy Path -- Maria signs in with Google

Maria Santos clicks "Sign in with Google". She selects her Google account. She is redirected back. The comment form shows "Signed in as Maria Santos (Google)".

#### 3: Error Path -- OAuth denied

Tomasz clicks "Sign in with GitHub" but clicks "Deny" on the consent screen. He is returned to the post. The sign-in prompt is shown again. No error message.

#### 4: Error Path -- Provider error

Maria clicks "Sign in with Google" but Google's OAuth service returns an error. She is returned to the post. Message: "Sign in failed. Please try again."

### UAT Scenarios (BDD)

#### Scenario: Reader signs in with GitHub

Given Tomasz Kowalski is not signed in
And he is viewing "Outside-In TDD"
When Tomasz clicks "Sign in with GitHub"
Then he is redirected to the GitHub OAuth consent screen
And after granting consent he is redirected back to "Outside-In TDD"
And the comment form shows "Signed in as Tomasz Kowalski (GitHub)"

#### Scenario: Reader signs in with Google

Given Maria Santos is not signed in
When Maria clicks "Sign in with Google"
Then she is redirected to the Google OAuth consent screen
And after granting consent she is redirected back to the post
And the comment form shows "Signed in as Maria Santos (Google)"

#### Scenario: OAuth consent denied

Given Ana Ferreira is not signed in
When Ana clicks "Sign in with GitHub" and denies consent
Then she is returned to the post page
And the sign-in prompt is still displayed

#### Scenario: OAuth provider error

Given Maria Santos is not signed in
When Maria clicks "Sign in with Google" and the provider returns an error
Then Maria is returned to the post page
And the message "Sign in failed. Please try again." is displayed

#### Scenario: Session persists on return visit

Given Tomasz signed in via GitHub yesterday
When Tomasz visits "Outside-In TDD" today
Then the comment form is ready without re-authentication

### Acceptance Criteria

- [ ] Two sign-in buttons: "Sign in with Google" and "Sign in with GitHub"
- [ ] OAuth redirect to provider consent screen on button click
- [ ] On success: redirect back to post with session established
- [ ] Display name and avatar extracted from OAuth profile
- [ ] Email from OAuth NOT stored or displayed
- [ ] Session persists via httpOnly, Secure, SameSite=Lax cookie
- [ ] OAuth tokens exchanged server-side, never exposed to client
- [ ] Failed OAuth shows appropriate message or silently returns
- [ ] Sign-out button clears session

### Technical Notes

- Google OAuth 2.0 via Google Cloud Console (free)
- GitHub OAuth via GitHub Developer Settings (free)
- Redirect URIs configured for production and development environments
- Client IDs/secrets in environment variables
- Server-side token exchange only
- Session store: database-backed or in-memory (DESIGN wave decision)

### Dependencies

- OAuth provider accounts created and configured
- Backend capable of handling OAuth callback endpoints

---

## US-052: Post a Comment on a Blog Post

### Problem

Tomasz Kowalski wants to share a counterpoint about Outside-In TDD. Maria Santos wants to thank the author for a Clean Architecture post. Currently there is no way for readers to engage in discussion or provide feedback. The blog is a one-way channel.

### Who

- Authenticated blog reader | Viewing a post they want to respond to | Wants to share thoughts, ask questions, or thank the author

### Solution

A comment form (textarea + submit) for authenticated readers. Comment appears immediately (post-moderation). Max 2000 characters.

**Size**: M
**Priority**: Must
**Bounded Context**: Engagement
**Journey**: J6 steps 4-5

### Domain Examples

#### 1: Happy Path -- Tomasz posts a comment

Tomasz is signed in via GitHub. He types "Great breakdown of the double loop! I've been struggling with where to start the outer test." and clicks "Post Comment". His comment appears with his GitHub avatar, "Tomasz Kowalski", "via GitHub", and "just now". Toast: "Comment posted."

#### 2: Edge Case -- Character limit

Tomasz writes a lengthy comment. At 1800 characters, a counter appears: "200 characters remaining". At 2000, the message "Comment is too long (2000 character limit)" appears and "Post Comment" is disabled.

#### 3: Error Path -- Network failure

Maria types a comment on mobile. Her train enters a tunnel and the network drops. She clicks "Post Comment". Message: "Could not post your comment. Check your connection and try again." Her text is preserved in the textarea.

#### 4: Error Path -- Session expired

Tomasz has been on the page for hours composing a long response. His session expired. He clicks "Post Comment". Message: "Your session has expired. Please sign in again." His text is saved to localStorage. After re-auth, the textarea is pre-populated.

### UAT Scenarios (BDD)

#### Scenario: Reader posts a comment

Given Tomasz Kowalski is signed in via GitHub
And he is viewing "Outside-In TDD"
When Tomasz types "Great breakdown of the double loop!" in the comment textarea
And clicks "Post Comment"
Then his comment appears in the comments list with his avatar, name, "via GitHub", and "just now"
And a toast "Comment posted." is displayed

#### Scenario: Empty comment prevented

Given Maria Santos is signed in via Google
Then the "Post Comment" button is disabled when the textarea is empty

#### Scenario: Character limit enforced

Given Tomasz is writing a comment reaching 1800 characters
Then a character counter appears
And at 2000 characters the "Post Comment" button is disabled

#### Scenario: Comment text preserved on network failure

Given Maria has typed a comment and the network drops
When Maria clicks "Post Comment"
Then the error message is shown
And her comment text is preserved in the textarea

#### Scenario: Comment text preserved on session expiry

Given Tomasz's session has expired while writing a comment
When Tomasz clicks "Post Comment"
Then the session expiry message is shown
And after re-auth the textarea is pre-populated with his draft

### Acceptance Criteria

- [ ] Comment textarea visible when authenticated
- [ ] "Post Comment" button disabled when textarea empty
- [ ] Character counter at 1800+ characters, error at 2000
- [ ] Comment appears immediately after successful POST
- [ ] Comment shows avatar, name, provider badge, timestamp, text
- [ ] Toast "Comment posted." on success
- [ ] Comment text preserved on network failure
- [ ] Comment draft preserved in localStorage on session expiry
- [ ] Comment text sanitized server-side (XSS prevention)

### Technical Notes

- API endpoint: `POST /api/posts/{slug}/comments` (requires session)
- API endpoint: `GET /api/posts/{slug}/comments` returns comment list
- Comment entity: id, post_slug, display_name, avatar_url, provider, text, created_at
- Rate limiting: max 5 comments per user per 10 minutes
- CSRF protection on POST endpoint

### Dependencies

- US-051 (OAuth sign-in must work)
- US-043 (single post view must exist)

---

## US-053: Author Moderates Comments

### Problem

Christian is the blog author. He expects that occasionally a spam comment or an inappropriate response will appear. He needs a way to remove such comments without complex moderation workflows. Since the blog uses post-moderation, comments appear immediately and are cleaned up afterward.

### Who

- Blog author (admin) | Reviewing comments across all posts | Wants to remove spam or inappropriate content quickly

### Solution

Admin panel shows all comments with delete capability. Confirmation dialog before deletion. No notification to commenter.

**Size**: S
**Priority**: Must
**Bounded Context**: Engagement + Identity
**Journey**: J6 M1-M2

### Domain Examples

#### 1: Happy Path -- Christian deletes spam

A comment from "SpamBot" says "Buy cheap watches at..." on "Outside-In TDD". Christian sees it in the admin panel. He clicks delete, confirms, and the comment disappears from the public post. Comment count drops from 4 to 3.

#### 2: Happy Path -- Christian reviews and keeps a comment

A comment from Tomasz Kowalski disagrees with a point in the article. Christian reads it, finds it respectful and valuable, and takes no action. The comment stays.

#### 3: Edge Case -- Comment already deleted

Christian opens the admin panel in two tabs. He deletes a comment in tab 1. He tries to delete the same comment in tab 2. The system says "Comment not found."

### UAT Scenarios (BDD)

#### Scenario: Author deletes spam comment

Given Christian is authenticated as admin
And "Outside-In TDD" has a spam comment by "SpamBot"
When Christian clicks delete on the spam comment and confirms
Then the comment is removed from the public view
And the comment count decrements by 1

#### Scenario: Author views all comments

Given Christian is authenticated as admin
When he navigates to the comments management page
Then he sees all comments across all posts, newest first
And each entry shows post title, commenter name, provider, date, and text

#### Scenario: Deleting already-deleted comment

Given Christian tries to delete a comment that was already removed
Then the system shows "Comment not found"

### Acceptance Criteria

- [ ] Admin panel lists all comments across all posts
- [ ] Each comment shows post title, commenter name, provider, date, text, delete button
- [ ] Delete requires confirmation dialog
- [ ] Deleted comment removed from public post view
- [ ] Comment count updates after deletion
- [ ] No notification sent to commenter on deletion

### Technical Notes

- API endpoint: `DELETE /api/comments/{id}` (requires admin JWT)
- Admin panel: existing admin interface extended with comments section
- Depends on admin auth (Epic 1, US-010)

### Dependencies

- US-052 (comments must exist to moderate)
- US-010 (admin authentication)

---

## US-054: Share a Blog Post

### Problem

Ana Ferreira just read a post on "Value Objects in Practice" that perfectly explains a concept her team has been debating. She wants to send it to her team on Slack. Currently she has to manually copy the URL from the browser address bar -- there is no share button. On mobile, the URL bar is tiny and fiddly to select.

### Who

- Blog reader | Found a post worth sharing | Wants to share with minimal friction via their preferred channel

### Solution

A share button that uses the Web Share API on supported browsers (mobile native share sheet) with a clipboard copy fallback on desktop.

**Size**: S
**Priority**: Should
**Bounded Context**: Engagement (frontend only)
**Journey**: J7 steps 1-3

### Domain Examples

#### 1: Happy Path -- Tomasz shares on mobile

Tomasz is reading "TDD Myths" on his phone (Chrome Android). He taps the share icon. The Android share sheet appears with his apps: Twitter/X, WhatsApp, Slack. He selects Twitter/X. The app opens with the post title and URL pre-filled.

#### 2: Happy Path -- Ana shares on desktop

Ana is reading "Value Objects in Practice" on Chrome desktop. She clicks the share icon. The URL `https://theaugmentedcraftsman.com/posts/value-objects-in-practice` is copied to her clipboard. Toast: "Link copied!" She pastes it in Slack.

#### 3: Edge Case -- Browser without clipboard API

A reader on an older browser clicks share. A small popover appears with the URL as selectable text. They select it and press Ctrl+C.

### UAT Scenarios (BDD)

#### Scenario: Reader shares via native share sheet on mobile

Given Tomasz is reading "TDD Myths" on mobile
And his browser supports Web Share API
When Tomasz taps the share icon
Then the native share sheet appears with title "TDD Myths" and the post URL

#### Scenario: Reader shares via clipboard on desktop

Given Ana is reading "Value Objects in Practice" on desktop
And her browser does not support Web Share API
When Ana clicks the share icon
Then the post URL is copied to her clipboard
And a toast "Link copied!" appears and auto-dismisses after 3 seconds

#### Scenario: Fallback for browsers without clipboard API

Given a reader's browser supports neither Web Share API nor Clipboard API
When the reader clicks the share icon
Then a popover appears with the post URL as selectable text

#### Scenario: Share button hidden without JavaScript

Given a reader visits a post with JavaScript disabled
Then the share button is not visible

### Acceptance Criteria

- [ ] Share button displayed on every post page alongside like button
- [ ] On mobile/supported browsers: invokes Web Share API
- [ ] On desktop without Web Share API: copies URL to clipboard
- [ ] Toast "Link copied!" auto-dismisses after 3 seconds
- [ ] Fallback popover with selectable URL for oldest browsers
- [ ] Share button hidden when JavaScript disabled
- [ ] Minimum 44x44px touch target
- [ ] aria-label: "Share this post"

### Technical Notes

- Feature detection: `navigator.share` -> `navigator.clipboard` -> selectable text
- Astro Island: `client:visible`, lightweight (no API calls)
- OG meta tags must be present on all post pages (see US-055)
- No backend component required

### Dependencies

- US-043 (single post view must exist)
- US-055 (OG meta tags for rich share previews)

---

## US-055: Open Graph Meta Tags for Social Sharing

### Problem

When Ana shares a post URL on Slack or LinkedIn, the platform scrapes the URL for preview metadata. Without proper Open Graph tags, the preview shows a generic title and no image -- making the shared link look unprofessional and reducing click-through.

### Who

- Blog reader who shared a post | Wants the shared link to look professional with title, description, and image

### Solution

Every post page includes Open Graph and Twitter Card meta tags with post title, description (excerpt), URL, and featured image (or default fallback).

**Size**: S
**Priority**: Should
**Bounded Context**: Reading (extends existing NFR-S02)
**Journey**: J7 (sharing)

### Domain Examples

#### 1: Happy Path -- Post with featured image

"Value Objects in Practice" has a featured image uploaded to ImageKit. When shared on LinkedIn, the preview shows: title "Value Objects in Practice", a description excerpt, and the featured image.

#### 2: Edge Case -- Post without featured image

"TDD Myths" has no featured image. When shared on Twitter/X, the preview shows: title "TDD Myths", description, and a default blog image (e.g., The Augmented Craftsman logo/banner).

#### 3: Happy Path -- Correct canonical URL

The shared URL `https://theaugmentedcraftsman.com/posts/value-objects-in-practice` matches the `og:url` meta tag exactly. Social platforms display the correct link.

### UAT Scenarios (BDD)

#### Scenario: Post with featured image has correct OG tags

Given "Value Objects in Practice" is published with a featured image
When the post URL is shared on a social platform
Then the platform displays og:title "Value Objects in Practice"
And the platform displays the featured image as og:image

#### Scenario: Post without featured image uses fallback

Given "TDD Myths" is published without a featured image
When the post URL is shared on a social platform
Then the platform displays a default blog Open Graph image

#### Scenario: OG URL matches canonical URL

Given "Value Objects in Practice" is published
Then the og:url meta tag matches the canonical post URL

### Acceptance Criteria

- [ ] Every post page has og:title matching post title
- [ ] Every post page has og:description with excerpt
- [ ] Every post page has og:url with canonical URL
- [ ] Every post page has og:type set to "article"
- [ ] Posts with featured image have og:image with ImageKit URL
- [ ] Posts without featured image have og:image with default blog image
- [ ] Every post page has twitter:card set to "summary_large_image"

### Technical Notes

- Implemented in Astro layout template (build-time, static)
- Default OG image stored as static asset in Astro public/ folder
- Extends existing NFR-S02 requirement

### Dependencies

- US-043 (single post view)
- US-031 (featured image, for og:image source)

---

## US-056: View Comments on a Post

### Problem

Maria Santos visits a post on Clean Architecture and wants to know if other readers found it useful or had questions. Currently there is no way to see community feedback. She cannot tell if a post sparked discussion or was read in silence.

### Who

- Blog reader | Viewing a post | Wants to see what other readers thought

### Solution

A comments section below each article showing existing comments with commenter identity and timestamps.

**Size**: S
**Priority**: Must
**Bounded Context**: Engagement
**Journey**: J6 step 1

### Domain Examples

#### 1: Happy Path -- Post with comments

Maria visits "Outside-In TDD". Below the article, she sees "Comments (3)". Three comments are listed with avatars, names, provider badges, timestamps, and text.

#### 2: Empty State -- No comments

Maria visits "New Post About DDD". Below the article: "No comments yet. Be the first to share your thoughts."

#### 3: Boundary -- Many comments

A popular post has 50 comments. All are loaded (v1 simplification -- no pagination for comments).

### UAT Scenarios (BDD)

#### Scenario: Reader sees existing comments

Given "Outside-In TDD" has 3 comments
When Maria scrolls to the comments section
Then she sees "Comments (3)" as the section header
And each comment shows avatar, display name, provider badge, timestamp, and text

#### Scenario: Empty comments section

Given "New Post About DDD" has no comments
When Maria scrolls to the comments section
Then she sees "No comments yet. Be the first to share your thoughts."

### Acceptance Criteria

- [ ] Comments section displayed below every post article
- [ ] Section header shows "Comments (N)" with correct count
- [ ] Each comment shows avatar, display name, provider badge, timestamp, text
- [ ] Empty state shows invitation message
- [ ] Comments listed in chronological order (oldest first)
- [ ] Comments section is an Astro Island (fetches from API, not SSG cached)

### Technical Notes

- API endpoint: `GET /api/posts/{slug}/comments`
- Astro Island: `client:visible`, fetches comments on hydration
- Semantic HTML: each comment in an `<article>` element
- No pagination in v1 (add if comment volume warrants it)

### Dependencies

- US-043 (single post view)
- US-052 (comments must be creatable for this to show content)

---

## Story Dependency Map

```
Epic 5 (User Engagement)

  US-050 (like) ←── US-043 (post view, Epic 4)

  US-051 (OAuth) ──→ US-052 (post comment) ──→ US-053 (moderate)
       ↑                    ↑                        ↑
    [no deps]          US-043 (post view)        US-010 (admin auth)

  US-054 (share) ←── US-055 (OG tags)
       ↑                    ↑
    US-043              US-043, US-031

  US-056 (view comments) ←── US-043
```

---

## Story Count Summary

| Story | Feature | Priority | Size | Scenarios |
|-------|---------|----------|------|-----------|
| US-050 | Likes | Must | M | 5 |
| US-051 | OAuth Sign-In | Must | L | 5 |
| US-052 | Post Comment | Must | M | 5 |
| US-053 | Moderate Comments | Must | S | 3 |
| US-054 | Share Post | Should | S | 4 |
| US-055 | OG Meta Tags | Should | S | 3 |
| US-056 | View Comments | Must | S | 2 |
| **Total** | | **5 Must, 2 Should** | **1L + 2M + 4S** | **27** |

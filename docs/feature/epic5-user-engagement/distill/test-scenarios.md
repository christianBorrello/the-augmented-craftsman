# Test Scenarios -- Epic 5: User Engagement

## Scenario Inventory

### Summary

| Feature File | Total Scenarios | Happy Path | Error Path | Boundary | Error Ratio |
|-------------|----------------|------------|------------|----------|-------------|
| WalkingSkeleton.feature | 2 | 2 | 0 | 0 | -- |
| Likes.feature | 12 | 5 | 5 | 1 (+1 @property) | 50% |
| OAuth.feature | 12 | 5 | 7 | 0 | 58% |
| Comments.feature | 14 | 5 | 6 | 2 | 57% |
| Moderation.feature | 7 | 3 | 4 | 0 | 57% |
| **Total** | **47** | **20** | **22** | **3** | **53%** |

Error path ratio: 22/42 (excluding walking skeletons) = **52%** -- exceeds 40% target.

---

## Traceability Matrix

### US-050: Like a Blog Post

| Acceptance Criterion | Scenario(s) | File |
|---------------------|-------------|------|
| Heart icon with count on every post | Walking Skeleton: like + count | WalkingSkeleton.feature |
| Single tap toggles like state | Like is idempotent for same visitor | Likes.feature |
| Like count increments by 1 | Walking Skeleton: like and count increments | WalkingSkeleton.feature |
| Same visitor cannot like twice | Like is idempotent for same visitor | Likes.feature |
| Deduplication via visitor_id | Like with invalid visitor identifier rejected | Likes.feature |
| Multiple visitors can like | Multiple visitors can like same post | Likes.feature |
| Unlike a post | Unlike a previously liked post | Likes.feature |
| Like on non-existent post | Like a non-existent post returns not found | Likes.feature |
| Like count never negative | Like count never negative (@property) | Likes.feature |

### US-051: Sign In with Social Login to Comment

| Acceptance Criterion | Scenario(s) | File |
|---------------------|-------------|------|
| Redirect to OAuth provider | Initiate GitHub sign-in redirects | OAuth.feature |
| On success: session established | GitHub/Google OAuth callback creates session | OAuth.feature |
| Display name and avatar from profile | Session contains display name and provider | OAuth.feature |
| Session persists via httpOnly cookie | Session check returns authenticated | OAuth.feature |
| Sign-out clears session | Sign out clears reader session | OAuth.feature |
| OAuth denied: return to post | OAuth consent denied returns without error | OAuth.feature |
| OAuth error: return with message | OAuth provider error returns with indicator | OAuth.feature |
| Expired session: not authenticated | Session check with expired session | OAuth.feature |
| No session: not authenticated | Session check with no session | OAuth.feature |
| Invalid state rejected | OAuth callback with invalid state | OAuth.feature |
| Unsupported provider rejected | Initiate sign-in with unsupported provider | OAuth.feature |

### US-052: Post a Comment on a Blog Post

| Acceptance Criterion | Scenario(s) | File |
|---------------------|-------------|------|
| Comment textarea when authenticated | Authenticated reader posts a comment | Comments.feature |
| Comment appears after POST | Authenticated reader posts a comment | Comments.feature |
| Comment shows avatar, name, provider, time | Comment shows display name/provider | Comments.feature |
| Empty comment rejected | Empty comment text rejected | Comments.feature |
| 2000 char limit | Comment exceeding 2000 chars rejected | Comments.feature |
| Comment text sanitized (XSS) | Comment text sanitized to prevent injection | Comments.feature |
| Unauthenticated blocked | Unauthenticated reader cannot post | Comments.feature |
| Boundary: exactly 2000 chars | Comment at exactly 2000 characters accepted | Comments.feature |
| Boundary: 1 character | Comment at exactly 1 character accepted | Comments.feature |

### US-053: Author Moderates Comments

| Acceptance Criterion | Scenario(s) | File |
|---------------------|-------------|------|
| Admin lists all comments | Admin lists all comments across posts | Moderation.feature |
| Delete removes from public view | Admin deletes a spam comment | Moderation.feature |
| Comment count updates after deletion | Comment count updates after deletion | Moderation.feature |
| Delete non-existent returns 404 | Delete non-existent comment | Moderation.feature |
| Delete without admin JWT: 401 | Delete without admin auth rejected | Moderation.feature |
| Reader session cannot delete | Reader session cannot delete comments | Moderation.feature |
| Admin list without auth: 401 | Admin list without authentication rejected | Moderation.feature |

### US-054: Share a Blog Post

Frontend-only feature (Web Share API / Clipboard). No API-level acceptance tests.
Covered by manual visual acceptance tests and Astro component tests.

### US-055: Open Graph Meta Tags

Frontend-only feature (Astro build-time templates). No API-level acceptance tests.
Validated via Astro build output inspection.

### US-056: View Comments on a Post

| Acceptance Criterion | Scenario(s) | File |
|---------------------|-------------|------|
| Comments section with count | Reader sees existing comments | Comments.feature |
| Each comment: avatar, name, provider, time, text | Each comment contains all fields | Comments.feature |
| Empty state message | Empty comments section returns zero count | Comments.feature |
| Chronological order | Comments in chronological order | Comments.feature |
| Comment count retrieval | Comment count matches number of comments | Comments.feature |
| Non-existent post: 404 | Request comments for non-existent post | Comments.feature |

---

## Implementation Sequence

One scenario at a time, enabling and implementing in this order:

### Phase 0: Walking Skeleton (implement first, no @skip tag)
1. Reader likes a post and the like count increments
2. Reader checks like count for a post

### Phase 1: Likes (US-050)
3. Like is idempotent for the same visitor
4. Unlike a previously liked post
5. Unlike is idempotent when no like exists
6. Check if visitor has liked a post
7. Check like status when visitor has not liked
8. Multiple visitors can like the same post
9. Like a non-existent post returns not found
10. Unlike/check non-existent post returns not found
11. Like with invalid visitor identifier
12. Like count never negative (@property)

### Phase 2: OAuth & Sessions (US-051)
13. Initiate GitHub sign-in redirects to provider
14. GitHub OAuth callback creates session
15. Google OAuth callback creates session
16. Session check returns authenticated status
17. Sign out clears session
18. OAuth consent denied returns without error
19. OAuth provider error returns with indicator
20. Session check with no session
21. Session check with expired session
22. Unsupported provider rejected
23. Invalid state parameter rejected
24. Sign out with no active session

### Phase 3: Comments (US-052, US-056)
25. Authenticated reader posts a comment
26. Comment text sanitized (XSS)
27. Reader sees existing comments on a post
28. Empty comments section returns zero
29. Comment count matches number
30. Empty comment text rejected
31. Comment exceeding 2000 chars rejected
32. Whitespace-only comment rejected
33. Unauthenticated reader cannot post
34. Comment on non-existent post
35. Request comments for non-existent post
36. Comment at exactly 2000 characters
37. Comment at exactly 1 character

### Phase 4: Moderation (US-053)
38. Admin deletes a spam comment
39. Comment count updates after deletion
40. Admin lists all comments across posts
41. Delete non-existent comment
42. Delete without admin authentication
43. Reader session cannot delete comments
44. Admin list without authentication

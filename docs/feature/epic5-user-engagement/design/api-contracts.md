# API Contracts -- Epic 5: User Engagement

## Base URL

`/api` -- all endpoints prefixed. Same backend as existing post/tag/image endpoints.

---

## Likes Endpoints

### POST /api/posts/{slug}/likes

**Purpose:** Like a post (anonymous, no auth required).

**Auth:** None (AllowAnonymous)

**Request:**
```json
{
  "visitorId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response 200 (OK):**
```json
{
  "liked": true,
  "count": 13
}
```

**Response 404 (Not Found):**
```json
{
  "error": "Post not found"
}
```

**Behavior:**
- Idempotent: if visitor already liked this post, returns current count without incrementing
- Rate limited: max 5 likes per visitor_id per 10 seconds (excess silently ignored with 200)
- Validates slug references an existing published post

---

### DELETE /api/posts/{slug}/likes/{visitorId}

**Purpose:** Unlike a post.

**Auth:** None (AllowAnonymous)

**Response 200 (OK):**
```json
{
  "liked": false,
  "count": 12
}
```

**Response 404 (Not Found):**
```json
{
  "error": "Post not found"
}
```

**Behavior:**
- Idempotent: if like does not exist, returns current count
- No error if like was already removed

---

### GET /api/posts/{slug}/likes/count

**Purpose:** Get the like count for a post.

**Auth:** None (AllowAnonymous)

**Response 200 (OK):**
```json
{
  "count": 13
}
```

**Response 404 (Not Found):**
```json
{
  "error": "Post not found"
}
```

---

### GET /api/posts/{slug}/likes/check/{visitorId}

**Purpose:** Check if a specific visitor liked a post.

**Auth:** None (AllowAnonymous)

**Response 200 (OK):**
```json
{
  "liked": true,
  "count": 13
}
```

**Response 404 (Not Found):**
```json
{
  "error": "Post not found"
}
```

**Note:** Returns `liked: false` if the visitor has not liked the post.

---

## Comments Endpoints

### GET /api/posts/{slug}/comments

**Purpose:** List all comments for a post.

**Auth:** None (AllowAnonymous)

**Response 200 (OK):**
```json
{
  "count": 3,
  "comments": [
    {
      "id": "d290f1ee-6c54-4b01-90e6-d701748f0851",
      "displayName": "Tomasz Kowalski",
      "avatarUrl": "https://avatars.githubusercontent.com/u/12345",
      "provider": "GitHub",
      "text": "Great breakdown of the double loop!",
      "createdAt": "2026-03-08T14:30:00Z"
    }
  ]
}
```

**Response 404 (Not Found):**
```json
{
  "error": "Post not found"
}
```

**Behavior:**
- Comments sorted chronologically (oldest first)
- Returns empty array for posts with no comments
- count field matches array length

---

### GET /api/posts/{slug}/comments/count

**Purpose:** Get comment count for a post.

**Auth:** None (AllowAnonymous)

**Response 200 (OK):**
```json
{
  "count": 3
}
```

---

### POST /api/posts/{slug}/comments

**Purpose:** Post a comment on a post.

**Auth:** Reader session cookie (httpOnly). Returns 401 if not authenticated.

**Request:**
```json
{
  "text": "Great breakdown of the double loop!"
}
```

**Response 201 (Created):**
```json
{
  "id": "d290f1ee-6c54-4b01-90e6-d701748f0851",
  "displayName": "Tomasz Kowalski",
  "avatarUrl": "https://avatars.githubusercontent.com/u/12345",
  "provider": "GitHub",
  "text": "Great breakdown of the double loop!",
  "createdAt": "2026-03-08T14:30:00Z"
}
```

**Response 400 (Bad Request):**
```json
{
  "error": "Comment text is required"
}
```
or
```json
{
  "error": "Comment cannot exceed 2000 characters"
}
```

**Response 401 (Unauthorized):**
```json
{
  "error": "Authentication required"
}
```

**Response 404 (Not Found):**
```json
{
  "error": "Post not found"
}
```

**Response 429 (Too Many Requests):**
```json
{
  "error": "Too many comments. Please wait before posting again."
}
```

**Behavior:**
- DisplayName, AvatarUrl, Provider extracted from session (not sent by client)
- Text sanitized server-side (HTML tags stripped/escaped)
- Rate limited: max 5 comments per user per 10 minutes
- CSRF protection via SameSite=Lax cookie + origin check

---

### DELETE /api/posts/{slug}/comments/{id}

**Purpose:** Delete a comment (admin moderation).

**Auth:** Admin JWT (RequireAuthorization)

**Response 204 (No Content):** Success, no body.

**Response 404 (Not Found):**
```json
{
  "error": "Comment not found"
}
```

**Response 401 (Unauthorized):**
```json
{
  "error": "Authentication required"
}
```

---

## OAuth / Session Endpoints

### GET /api/auth/oauth/{provider}

**Purpose:** Initiate OAuth flow. Redirects to provider consent screen.

**Path params:** `provider` = `google` or `github`

**Query params:** `returnUrl` = post URL to redirect back to after auth

**Behavior:**
- Generates OAuth state parameter (CSRF protection)
- Stores state + returnUrl in short-lived cookie or server-side
- Redirects to provider's authorization URL

---

### GET /api/auth/oauth/{provider}/callback

**Purpose:** OAuth callback. Handles provider redirect after consent.

**Query params:** `code`, `state` (from OAuth provider)

**Behavior on success:**
1. Exchange authorization code for access token (server-side)
2. Fetch user profile from provider API
3. Extract display_name, avatar_url, provider_id (no email)
4. Create reader session in database
5. Set httpOnly session cookie
6. Redirect to original post URL (from state)

**Behavior on failure (denied or error):**
- Redirect to original post URL with `?auth_error=denied` or `?auth_error=failed`

---

### GET /api/auth/session

**Purpose:** Check current reader session status.

**Auth:** Reader session cookie

**Response 200 (Authenticated):**
```json
{
  "authenticated": true,
  "displayName": "Tomasz Kowalski",
  "avatarUrl": "https://avatars.githubusercontent.com/u/12345",
  "provider": "GitHub"
}
```

**Response 200 (Not authenticated):**
```json
{
  "authenticated": false
}
```

---

### POST /api/auth/signout

**Purpose:** Sign out reader session.

**Auth:** Reader session cookie

**Behavior:**
- Delete session from database
- Clear session cookie

**Response 204 (No Content):** Success.

---

## Admin Endpoints

### GET /api/admin/comments

**Purpose:** List all comments across all posts (for moderation).

**Auth:** Admin JWT (RequireAuthorization)

**Response 200 (OK):**
```json
[
  {
    "id": "d290f1ee-6c54-4b01-90e6-d701748f0851",
    "postSlug": "outside-in-tdd",
    "postTitle": "Outside-In TDD",
    "displayName": "Tomasz Kowalski",
    "avatarUrl": "https://avatars.githubusercontent.com/u/12345",
    "provider": "GitHub",
    "text": "Great breakdown of the double loop!",
    "createdAt": "2026-03-08T14:30:00Z"
  }
]
```

**Behavior:**
- Sorted by createdAt descending (newest first)
- Includes postSlug and postTitle for admin context
- No pagination in v1

---

## Error Response Format

All error responses follow the existing pattern:

```json
{
  "error": "Human-readable error message"
}
```

---

## Rate Limiting Summary

| Endpoint | Limit | Window | Key | Behavior |
|----------|-------|--------|-----|----------|
| POST /api/posts/{slug}/likes | 5 | 10 seconds | visitor_id | Silent ignore (return 200 with current count) |
| POST /api/posts/{slug}/comments | 5 | 10 minutes | session_id | 429 with error message |

---

## Authentication Summary

| Endpoint | Auth Type | Details |
|----------|-----------|---------|
| Like endpoints | None | Anonymous, visitor_id in request/path |
| GET comments | None | Public read |
| POST comment | Reader session cookie | httpOnly, Secure, SameSite=Lax |
| DELETE comment | Admin JWT | Existing admin auth from Epic 1 |
| OAuth endpoints | None | Public (initiate flow) |
| Session check | Reader session cookie | Optional -- returns status |
| Admin comments list | Admin JWT | Existing admin auth |

---

## CORS

Existing CORS policy already allows the frontend origin. The reader session cookie requires `AllowCredentials()` to be added to the CORS policy alongside the existing origin allowlist.

`AllowCredentials()` is required for all endpoints that send or receive the session cookie:
- `POST /api/posts/{slug}/comments` (sends cookie)
- `GET /api/auth/session` (sends cookie)
- `POST /api/auth/signout` (sends cookie)
- `GET /api/auth/oauth/{provider}/callback` (sets cookie on redirect)

Like endpoints and public GET endpoints do not require credentials.

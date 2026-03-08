# API Contracts -- The Augmented Craftsman v1

**Base URL**: `https://api.theaugmentedcraftsman.christianborrello.dev` (production) | `http://localhost:5000` (development)
**Format**: JSON
**Authentication**: JWT Bearer token for admin endpoints
**API Documentation**: OpenAPI 3.1 at `/openapi/v1.json`

---

## Authentication

### POST /api/auth/login

Authenticate as admin and receive a JWT token.

**Auth**: Public
**Rate Limit**: 5 attempts per 10 minutes, then locked for 15 minutes.

**Request Body:**

```json
{
  "email": "christian.borrello@live.it",
  "password": "valid-password"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `email` | string | yes | Valid email format |
| `password` | string | yes | Non-empty |

**Response 200 (success):**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-03-06T10:30:00Z"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `token` | string | JWT Bearer token |
| `expiresAt` | string (ISO 8601) | Token expiration timestamp |

**Response 400 (validation):**

```json
{
  "error": "Invalid email or password"
}
```

**Response 429 (locked):**

```json
{
  "error": "Too many attempts. Try again in 15 minutes."
}
```

---

## Posts

### POST /api/posts

Create a new draft blog post.

**Auth**: JWT required

**Request Body:**

```json
{
  "title": "TDD Is Not About Testing",
  "content": "## The Misconception\nMost people think TDD is about testing. It's about **design**.\n\n```csharp\n[Fact]\npublic void should_design_through_tests() { }\n```",
  "tagIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"],
  "featuredImageUrl": "https://ik.imagekit.io/augmented/tdd-cycle.png"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `title` | string | yes | Non-empty, max 200 characters |
| `content` | string | yes | Non-empty, Markdown |
| `tagIds` | string[] (UUIDs) | no | Array of existing tag IDs |
| `featuredImageUrl` | string (URL) | no | Valid ImageKit URL |

**Response 201 (created):**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "title": "TDD Is Not About Testing",
  "slug": "tdd-is-not-about-testing",
  "content": "## The Misconception\nMost people think TDD is about testing...",
  "status": "Draft",
  "publishedAt": null,
  "featuredImageUrl": "https://ik.imagekit.io/augmented/tdd-cycle.png",
  "tags": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "TDD",
      "slug": "tdd"
    }
  ],
  "createdAt": "2026-03-05T08:00:00Z",
  "updatedAt": "2026-03-05T08:00:00Z"
}
```

**Response 400 (validation):**

```json
{
  "error": "Title is required"
}
```

**Response 409 (conflict — duplicate slug):**

```json
{
  "error": "A post with this URL already exists"
}
```

**Note**: Duplicate slug is a 409 Conflict (not 400) because the request itself is valid — the conflict is with existing state. This matches HTTP semantics: "The request could not be completed due to a conflict with the current state of the target resource."

**Response 401**: Missing or invalid JWT

**Walking Skeleton (Feature 0)**: Only `title` and `content` are accepted. `tagIds` and `featuredImageUrl` are not supported until Epic 1 and Epic 3 respectively. No JWT required during Walking Skeleton (authentication added in Epic 1).

---

### GET /api/posts

List published posts (public endpoint for Astro build and readers).

**Auth**: Public
**Query Parameters:**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `tag` | string | (none) | Filter by tag slug (e.g., `?tag=tdd`) |

**Request Example:**

```
GET /api/posts?tag=tdd
```

**Response 200:**

```json
{
  "posts": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "title": "TDD Is Not About Testing",
      "slug": "tdd-is-not-about-testing",
      "content": "## The Misconception\nMost people think TDD is about testing...",
      "status": "Published",
      "publishedAt": "2026-03-05T10:00:00Z",
      "featuredImageUrl": "https://ik.imagekit.io/augmented/tdd-cycle.png",
      "tags": [
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "name": "TDD",
          "slug": "tdd"
        },
        {
          "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
          "name": "Clean Code",
          "slug": "clean-code"
        }
      ],
      "createdAt": "2026-03-05T08:00:00Z",
      "updatedAt": "2026-03-05T10:00:00Z"
    }
  ]
}
```

**Notes:**
- Returns only Published posts, sorted by `publishedAt` descending (newest first).
- When `tag` parameter is provided, returns only posts associated with that tag slug. Response shape is identical (same fields) regardless of whether a tag filter is applied.
- Returns full post objects including `content` and `tags[]`. Excerpt derivation is the Astro frontend's responsibility (first 150 chars of plain text, Markdown stripped, at word boundary).

---

### GET /api/posts/{slug}

Get a single post by its URL slug.

**Auth**: Public

**Request Example:**

```
GET /api/posts/tdd-is-not-about-testing
```

**Response 200:**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "title": "TDD Is Not About Testing",
  "slug": "tdd-is-not-about-testing",
  "content": "## The Misconception\nMost people think TDD is about testing. It's about **design**.\n\n```csharp\n[Fact]\npublic void should_design_through_tests() { }\n```",
  "status": "Published",
  "publishedAt": "2026-03-05T10:00:00Z",
  "featuredImageUrl": "https://ik.imagekit.io/augmented/tdd-cycle.png",
  "tags": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "TDD",
      "slug": "tdd"
    },
    {
      "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "name": "Clean Code",
      "slug": "clean-code"
    }
  ],
  "createdAt": "2026-03-05T08:00:00Z",
  "updatedAt": "2026-03-05T10:00:00Z"
}
```

**Response 404:**

```json
{
  "error": "Post not found"
}
```

**Notes:**
- Returns the post regardless of status (Draft or Published) so the admin can preview.
- The Astro frontend filters for Published at build time.

---

### PUT /api/posts/{id}

Update an existing post. Slug is immutable and not accepted in the request.

**Auth**: JWT required

**Request Example:**

```
PUT /api/posts/a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**Request Body:**

```json
{
  "title": "TDD Is About Design, Not Testing",
  "content": "## Updated Content\nRefreshed perspective on TDD as a design practice.",
  "tagIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6", "c3d4e5f6-a7b8-9012-cdef-123456789012"],
  "featuredImageUrl": "https://ik.imagekit.io/augmented/new-tdd-diagram.png"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `title` | string | yes | Non-empty, max 200 characters |
| `content` | string | yes | Non-empty, Markdown |
| `tagIds` | string[] (UUIDs) | no | Replaces all current tag associations |
| `featuredImageUrl` | string (URL) or null | no | Set to null to remove featured image |

**Response 200 (updated):**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "title": "TDD Is About Design, Not Testing",
  "slug": "tdd-is-not-about-testing",
  "content": "## Updated Content\n...",
  "status": "Published",
  "publishedAt": "2026-03-05T10:00:00Z",
  "featuredImageUrl": "https://ik.imagekit.io/augmented/new-tdd-diagram.png",
  "tags": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "TDD",
      "slug": "tdd"
    },
    {
      "id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "name": "DDD",
      "slug": "ddd"
    }
  ],
  "createdAt": "2026-03-05T08:00:00Z",
  "updatedAt": "2026-03-05T14:30:00Z"
}
```

**Notes:**
- **Slug is immutable.** The response always returns the original slug set at creation time. Even if the title changes from "TDD Is Not About Testing" to "TDD Is About Design, Not Testing", the slug remains `"tdd-is-not-about-testing"`. This preserves URL stability for SEO, social shares, and reader bookmarks.
- `tagIds` replaces all current tag associations. To remove all tags, send an empty array `[]`.
- Set `featuredImageUrl` to `null` to remove the featured image.

**Response 400:**

```json
{
  "error": "Title is required"
}
```

**Response 401**: Missing or invalid JWT
**Response 404:**

```json
{
  "error": "Post not found"
}
```

---

### DELETE /api/posts/{id}

Delete a post permanently. Removes post-tag associations. Tags are not deleted.

**Auth**: JWT required

**Request Example:**

```
DELETE /api/posts/a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**Response 204**: No content (success)

**Response 401**: Missing or invalid JWT
**Response 404:**

```json
{
  "error": "Post not found"
}
```

---

### POST /api/posts/{id}/publish

Publish a draft post. Sets status to Published and records the publish date.

**Auth**: JWT required

**Request Example:**

```
POST /api/posts/a1b2c3d4-e5f6-7890-abcd-ef1234567890/publish
```

**Request Body**: Empty

**Response 200 (published):**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "title": "TDD Is Not About Testing",
  "slug": "tdd-is-not-about-testing",
  "status": "Published",
  "publishedAt": "2026-03-05T10:00:00Z"
}
```

**Response 400:**

```json
{
  "error": "Post is already published"
}
```

**Response 401**: Missing or invalid JWT
**Response 404:**

```json
{
  "error": "Post not found"
}
```

---

### GET /api/admin/posts

List all posts including drafts (admin view).

**Auth**: JWT required

**Request Example:**

```
GET /api/admin/posts
```

**Response 200:**

```json
{
  "posts": [
    {
      "id": "d4e5f6a7-b8c9-0123-defg-456789012345",
      "title": "Draft: Upcoming Post",
      "slug": "draft-upcoming-post",
      "status": "Draft",
      "publishedAt": null,
      "tagCount": 2,
      "createdAt": "2026-03-06T09:00:00Z",
      "updatedAt": "2026-03-06T09:00:00Z"
    },
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "title": "TDD Is Not About Testing",
      "slug": "tdd-is-not-about-testing",
      "status": "Published",
      "publishedAt": "2026-03-05T10:00:00Z",
      "tagCount": 2,
      "createdAt": "2026-03-05T08:00:00Z",
      "updatedAt": "2026-03-05T10:00:00Z"
    }
  ]
}
```

**Notes:**
- Returns all posts (Draft + Published), sorted by `createdAt` descending.
- Uses a **dedicated read model** (not the same shape as POST/PUT/GET detail responses). This is intentional:
  - `tagCount` (integer) instead of full `tags[]` array — the admin list only needs a count, not full tag details.
  - No `content` field — the list view doesn't render post bodies.
  - This follows the Vertical Slice principle: each endpoint returns exactly what its consumer needs, no more.
- The full post detail (with `content`, `tags[]`, `featuredImageUrl`) is retrieved via GET /api/posts/{slug} or the edit form's fetch.

**Response 401**: Missing or invalid JWT

---

## Tags

### POST /api/tags

Create a new tag.

**Auth**: JWT required

**Request Body:**

```json
{
  "name": "Refactoring"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `name` | string | yes | Non-empty, max 50 characters, unique |

**Response 201 (created):**

```json
{
  "id": "e5f6a7b8-c9d0-1234-efgh-567890123456",
  "name": "Refactoring",
  "slug": "refactoring",
  "postCount": 0,
  "createdAt": "2026-03-05T08:00:00Z"
}
```

**Response 400 (validation):**

```json
{
  "error": "Tag name is required"
}
```

```json
{
  "error": "Tag name must be 50 characters or fewer"
}
```

**Response 409 (conflict — duplicate name):**

```json
{
  "error": "A tag named 'Refactoring' already exists"
}
```

**Response 401**: Missing or invalid JWT

---

### GET /api/tags

List all tags with post counts. Used by both admin (tag management) and public (Astro build).

**Auth**: Public

**Request Example:**

```
GET /api/tags
```

**Response 200:**

```json
{
  "tags": [
    {
      "id": "f6a7b8c9-d0e1-2345-fghi-678901234567",
      "name": "Architecture",
      "slug": "architecture",
      "postCount": 2
    },
    {
      "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "name": "Clean Code",
      "slug": "clean-code",
      "postCount": 3
    },
    {
      "id": "a7b8c9d0-e1f2-3456-ghij-789012345678",
      "name": "DDD",
      "slug": "ddd",
      "postCount": 1
    },
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "TDD",
      "slug": "tdd",
      "postCount": 5
    }
  ]
}
```

**Notes:**
- Sorted alphabetically by name.
- `postCount` reflects the number of **published** posts when called without auth (public).
- `postCount` reflects **all** posts (Draft + Published) when called with a valid JWT (admin).

---

### PUT /api/tags/{id}

Rename a tag. Slug regenerates from the new name.

**Auth**: JWT required

**Request Example:**

```
PUT /api/tags/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Request Body:**

```json
{
  "name": "Test-Driven Development"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `name` | string | yes | Non-empty, max 50 characters, unique |

**Response 200 (renamed):**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Test-Driven Development",
  "slug": "test-driven-development",
  "postCount": 5,
  "createdAt": "2026-03-01T08:00:00Z"
}
```

**Response 400 (validation):**

```json
{
  "error": "Tag name is required"
}
```

**Response 409 (conflict — duplicate name):**

```json
{
  "error": "A tag named 'Clean Code' already exists"
}
```

**Response 401**: Missing or invalid JWT
**Response 404:**

```json
{
  "error": "Tag not found"
}
```

---

### DELETE /api/tags/{id}

Delete a tag. Removes all post-tag associations. Posts are not deleted.

**Auth**: JWT required

**Request Example:**

```
DELETE /api/tags/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Response 204**: No content (success)

**Response 401**: Missing or invalid JWT
**Response 404:**

```json
{
  "error": "Tag not found"
}
```

---

## Images

### POST /api/images/upload

Upload an image to ImageKit via server-side upload.

**Auth**: JWT required
**Content-Type**: `multipart/form-data`

**Request:**

```
POST /api/images/upload
Content-Type: multipart/form-data

file: (binary image data)
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `file` | binary (multipart) | yes | Max 10MB. Allowed types: PNG, JPEG, WebP, GIF |

**Response 200 (uploaded):**

```json
{
  "url": "https://ik.imagekit.io/augmented/tdd-cycle.png"
}
```

**Response 400:**

```json
{
  "error": "Image must be under 10MB"
}
```

```json
{
  "error": "File type not allowed. Accepted: PNG, JPEG, WebP, GIF"
}
```

**Response 401**: Missing or invalid JWT

**Response 500 (ImageKit failure):**

```json
{
  "error": "Upload failed. Try again."
}
```

---

## Endpoint Summary

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/login` | Public | Authenticate, receive JWT |
| POST | `/api/posts` | JWT | Create draft post |
| GET | `/api/posts` | Public | List published posts (optional `?tag=` filter) |
| GET | `/api/posts/{slug}` | Public | Get single post by slug |
| PUT | `/api/posts/{id}` | JWT | Update post |
| DELETE | `/api/posts/{id}` | JWT | Delete post |
| POST | `/api/posts/{id}/publish` | JWT | Publish draft post |
| GET | `/api/admin/posts` | JWT | List all posts (admin) |
| POST | `/api/tags` | JWT | Create tag |
| GET | `/api/tags` | Public | List tags with post counts |
| PUT | `/api/tags/{id}` | JWT | Rename tag |
| DELETE | `/api/tags/{id}` | JWT | Delete tag |
| POST | `/api/images/upload` | JWT | Upload image to ImageKit |

**Total endpoints**: 13

---

## Response Shape Conventions

**Single resource**: object at root level

```json
{
  "id": "...",
  "title": "...",
  ...
}
```

**Collection**: wrapped in a named array

```json
{
  "posts": [ ... ]
}
```

**Error**: object with `error` string

```json
{
  "error": "Human-readable error message"
}
```

**Timestamps**: ISO 8601 UTC (`2026-03-05T10:00:00Z`)
**IDs**: UUID v4 strings
**Null fields**: included in response with `null` value (not omitted)

### JSON Serialization of Value Objects

Value Objects (Title, Slug, PostContent, TagName, etc.) are **serialized as their primitive representation** in JSON. The API consumer never sees the Value Object wrapper — only the underlying string or GUID.

| Value Object | JSON Type | Example |
|-------------|-----------|---------|
| `PostId` | string (UUID) | `"a1b2c3d4-e5f6-7890-abcd-ef1234567890"` |
| `Title` | string | `"TDD Is Not About Testing"` |
| `Slug` | string | `"tdd-is-not-about-testing"` |
| `PostContent` | string | `"## The Misconception\n..."` |
| `PostStatus` | string (enum) | `"Draft"` or `"Published"` |
| `TagId` | string (UUID) | `"3fa85f64-5717-4562-b3fc-2c963f66afa6"` |
| `TagName` | string | `"TDD"` |
| `TagSlug` | string | `"tdd"` |
| `ImageUrl` | string (URL) or null | `"https://ik.imagekit.io/augmented/..."` |

**Implementation**: Each Value Object has an `implicit operator` to its primitive type. ASP.NET Minimal API serializes via `System.Text.Json`. Custom `JsonConverter<T>` implementations are needed for each Value Object to serialize/deserialize through the factory methods (e.g., `Title.From(string)` for deserialization, `title.ToString()` for serialization). The crafter decides whether to use source-generated converters or a convention-based approach.

**Naming convention**: JSON property names use **camelCase** (ASP.NET default): `publishedAt`, `featuredImageUrl`, `tagIds`, `postCount`.

# Domain Model -- Epic 5: User Engagement

## Bounded Context: Engagement

Separate from Content context. Like and Comment are independent entities -- NOT children of BlogPost aggregate. They reference posts by `Slug` (existing Value Object from Content context).

---

## Entities

### Like

Represents an anonymous appreciation signal for a post. One per visitor per post.

**Properties:**
- `PostSlug` (Slug) -- FK reference to BlogPost, uses existing Value Object
- `VisitorId` (VisitorId) -- client-generated UUID, anonymous
- `CreatedAtUtc` (DateTime) -- when the like was recorded

**Invariants:**
- Unique constraint on (PostSlug, VisitorId) -- one like per visitor per post
- PostSlug must reference an existing published post
- VisitorId must be a valid, non-empty UUID

**Identity:** Composite key (PostSlug, VisitorId). No surrogate ID needed.

**Behavior:** Like is essentially an immutable fact -- created or deleted, never modified.

---

### Comment

Represents a text response to a post, attributed to a social-login identity.

**Properties:**
- `Id` (CommentId) -- server-generated UUID
- `PostSlug` (Slug) -- FK reference to BlogPost
- `DisplayName` (DisplayName) -- from OAuth profile
- `AvatarUrl` (AvatarUrl) -- from OAuth profile
- `Provider` (AuthProvider) -- Google or GitHub
- `Text` (CommentText) -- sanitized comment body, max 2000 chars
- `CreatedAtUtc` (DateTime) -- when the comment was posted

**Invariants:**
- Text must be 1..2000 characters, not blank
- Provider must be Google or GitHub
- DisplayName must not be blank
- PostSlug must reference an existing published post

**Identity:** `CommentId` (server-generated UUID).

**Behavior:** Immutable after creation (v1 -- no editing). Only deletion by admin.

---

## Value Objects

### VisitorId

Wraps a UUID string representing an anonymous blog visitor.

**Validation rules:**
- Must not be null or empty
- Must be a valid GUID format
- Must not be `Guid.Empty`

**Pattern:** `readonly record struct` (matches existing `PostId`, `TagId`)

---

### CommentText

Wraps the comment body text.

**Validation rules:**
- Must not be null, empty, or whitespace
- Must not exceed 2000 characters
- Trimmed on creation

**Pattern:** `readonly record struct` (matches existing `Title`, `PostContent`)

---

### DisplayName

Wraps the display name from OAuth profile.

**Validation rules:**
- Must not be null, empty, or whitespace
- Must not exceed 200 characters
- Trimmed on creation

**Pattern:** `readonly record struct`

---

### AvatarUrl

Wraps the avatar URL from OAuth profile.

**Validation rules:**
- Must be a valid absolute URL (HTTP or HTTPS)
- Must not exceed 2048 characters
- Nullable at the entity level (optional -- some profiles may lack avatars)

**Pattern:** `readonly record struct`

---

### AuthProvider

Represents the OAuth identity source.

**Values:** `Google`, `GitHub`

**Pattern:** `enum` -- only two values, no behavior needed. Simpler than a full Value Object.

---

### CommentId

Wraps a GUID for comment identity.

**Validation rules:**
- Must not be `Guid.Empty`

**Pattern:** `readonly record struct` (matches existing `PostId`, `TagId`)

---

## Aggregate Boundaries

```
+-----------------------+
|                       |
|  Like (independent)   |
|  Key: (PostSlug,      |
|        VisitorId)     |
|                       |
+-----------------------+

+-----------------------+
|                       |
|  Comment              |
|  (independent)        |
|  Key: CommentId       |
|  Ref: PostSlug        |
|                       |
+-----------------------+

+-----------------------+
|                       |
|  BlogPost (existing)  |
|  Key: PostId          |
|  NO reference to      |
|  likes or comments    |
|                       |
+-----------------------+
```

**Rationale:** Like and Comment are NOT children of BlogPost aggregate because:
1. BlogPost should not grow unboundedly with engagement data
2. Likes and comments have different consistency boundaries
3. They can be queried, created, and deleted independently
4. No business rule requires transactional consistency between a post and its likes/comments

## Domain Rules Summary

| Rule | Enforcement |
|------|-------------|
| One like per visitor per post | Unique constraint (PostSlug, VisitorId) + idempotent API |
| Comments require authentication | Application layer (use case checks session) |
| Max 2000 chars per comment | CommentText Value Object validation |
| Post-moderation | Comments appear immediately; admin deletes afterward |
| No comment editing | No update method on Comment entity |
| No comment threading | Flat list, no parent reference |
| Like anonymity | VisitorId is anonymous UUID, no PII |
| Comment minimal data | Only DisplayName, AvatarUrl, Provider -- no email stored |

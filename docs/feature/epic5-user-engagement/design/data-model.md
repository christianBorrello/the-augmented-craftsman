# Data Model -- Epic 5: User Engagement

## PostgreSQL Table Schemas

### likes

```sql
CREATE TABLE likes (
    post_slug       VARCHAR(250)    NOT NULL,
    visitor_id      UUID            NOT NULL,
    created_at_utc  TIMESTAMP       NOT NULL,

    CONSTRAINT pk_likes PRIMARY KEY (post_slug, visitor_id),
    CONSTRAINT fk_likes_post_slug FOREIGN KEY (post_slug)
        REFERENCES blog_posts(slug) ON DELETE CASCADE
);

CREATE INDEX ix_likes_post_slug ON likes (post_slug);
```

**Notes:**
- Composite primary key (post_slug, visitor_id) enforces one-like-per-visitor-per-post at the database level
- FK on post_slug references the unique `slug` column on `blog_posts`
- CASCADE delete: if a post is deleted, its likes are removed
- Index on post_slug for count queries (`SELECT COUNT(*) WHERE post_slug = ?`)

---

### comments

```sql
CREATE TABLE comments (
    id              UUID            NOT NULL,
    post_slug       VARCHAR(250)    NOT NULL,
    display_name    VARCHAR(200)    NOT NULL,
    avatar_url      VARCHAR(2048),
    provider        VARCHAR(10)     NOT NULL,
    text            VARCHAR(2000)   NOT NULL,
    created_at_utc  TIMESTAMP       NOT NULL,

    CONSTRAINT pk_comments PRIMARY KEY (id),
    CONSTRAINT fk_comments_post_slug FOREIGN KEY (post_slug)
        REFERENCES blog_posts(slug) ON DELETE CASCADE,
    CONSTRAINT chk_comments_provider CHECK (provider IN ('Google', 'GitHub'))
);

CREATE INDEX ix_comments_post_slug_created ON comments (post_slug, created_at_utc);
```

**Notes:**
- UUID primary key (server-generated)
- FK on post_slug references the unique `slug` column on `blog_posts`
- CASCADE delete: if a post is deleted, its comments are removed
- Composite index on (post_slug, created_at_utc) for chronological listing queries
- CHECK constraint on provider limits to valid values
- avatar_url is nullable (some OAuth profiles may lack avatars)

---

### reader_sessions (for OAuth session management)

```sql
CREATE TABLE reader_sessions (
    id              UUID            NOT NULL,
    display_name    VARCHAR(200)    NOT NULL,
    avatar_url      VARCHAR(2048),
    provider        VARCHAR(10)     NOT NULL,
    provider_id     VARCHAR(50)     NOT NULL,
    created_at_utc  TIMESTAMP       NOT NULL,
    expires_at_utc  TIMESTAMP       NOT NULL,

    CONSTRAINT pk_reader_sessions PRIMARY KEY (id),
    CONSTRAINT chk_sessions_provider CHECK (provider IN ('Google', 'GitHub'))
);

CREATE INDEX ix_reader_sessions_expires ON reader_sessions (expires_at_utc);
```

**Notes:**
- Database-backed session store for reader OAuth sessions
- Session ID becomes the value of the httpOnly cookie
- provider_id stores the OAuth provider's user ID as string — GitHub uses numeric IDs (up to 10 digits), Google uses numeric strings (up to 21 digits). VARCHAR(50) covers both.
- Expired sessions can be cleaned up periodically via the expiry index
- No email stored (privacy requirement)

---

## EF Core Entity Configurations

### LikeConfiguration

Following existing pattern from `BlogPostConfiguration` and `TagConfiguration`:

- Maps to table `likes`
- Composite key on (PostSlug, VisitorId)
- Value Object conversions for `Slug` and `VisitorId`
- Column naming follows snake_case convention

### CommentConfiguration

- Maps to table `comments`
- Key on Id (CommentId)
- Value Object conversions for CommentId, Slug, DisplayName, AvatarUrl, CommentText
- Enum conversion for AuthProvider
- Column naming follows snake_case convention

### ReaderSessionConfiguration

- Maps to table `reader_sessions`
- Key on Id (UUID)
- Enum conversion for AuthProvider
- Column naming follows snake_case convention

---

## Migration Strategy

Single migration adding all three tables:

1. **Migration name**: `AddEngagementTables`
2. **Tables created**: `likes`, `comments`, `reader_sessions`
3. **Dependencies**: Requires `blog_posts` table with unique `slug` column (already exists via `InitialCreate` migration)
4. **Rollback**: Drop all three tables

**Migration command:**
```bash
cd backend
dotnet ef migrations add AddEngagementTables --project src/TacBlog.Infrastructure --startup-project src/TacBlog.Api
```

---

## Relationship to Existing Tables

```
+-------------------+          +-------------------+
|   blog_posts      |          |      likes        |
|-------------------|          |-------------------|
| id (PK)           |          | post_slug (PK,FK) |----+
| title             |<---------| visitor_id (PK)   |    |
| slug (UNIQUE)     |          | created_at_utc    |    |
| content           |          +-------------------+    |
| status            |                                    |
| created_at        |          +-------------------+    |
| updated_at        |          |    comments       |    |
| published_at      |          |-------------------|    |
| featured_image_url|          | id (PK)           |    |
+-------------------+          | post_slug (FK)    |----+
        |                      | display_name      |
        |                      | avatar_url        |
   +----+-----+               | provider          |
   | post_tags |               | text              |
   +----------+                | created_at_utc    |
   |   tags    |               +-------------------+
   +----------+
                               +-------------------+
                               | reader_sessions   |
                               |-------------------|
                               | id (PK)           |
                               | display_name      |
                               | avatar_url        |
                               | provider          |
                               | provider_id       |
                               | created_at_utc    |
                               | expires_at_utc    |
                               +-------------------+
```

## Query Patterns

| Query | Table | Expected Access Pattern |
|-------|-------|------------------------|
| Get like count for post | `likes` | `COUNT(*) WHERE post_slug = ?` -- covered by ix_likes_post_slug |
| Check if visitor liked post | `likes` | `EXISTS WHERE post_slug = ? AND visitor_id = ?` -- covered by PK |
| Like a post | `likes` | `INSERT` with ON CONFLICT DO NOTHING (idempotent) |
| Unlike a post | `likes` | `DELETE WHERE post_slug = ? AND visitor_id = ?` -- covered by PK |
| List comments for post | `comments` | `SELECT WHERE post_slug = ? ORDER BY created_at_utc` -- covered by ix_comments_post_slug_created |
| Get comment count | `comments` | `COUNT(*) WHERE post_slug = ?` -- covered by ix_comments_post_slug_created |
| Create comment | `comments` | `INSERT` |
| Delete comment (admin) | `comments` | `DELETE WHERE id = ?` -- covered by PK |
| Find session by ID | `reader_sessions` | `SELECT WHERE id = ?` -- covered by PK |
| Clean expired sessions | `reader_sessions` | `DELETE WHERE expires_at_utc < ?` -- covered by ix_reader_sessions_expires |

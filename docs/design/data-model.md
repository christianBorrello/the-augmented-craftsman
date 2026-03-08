# Data Model -- The Augmented Craftsman v1

**Database**: PostgreSQL 16+
**ORM**: Entity Framework Core 10 with Npgsql provider
**Naming Convention**: snake_case for tables and columns (PostgreSQL convention)

---

## 1. Schema

### blog_posts

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| `id` | UUID | no | `gen_random_uuid()` | PRIMARY KEY |
| `title` | VARCHAR(200) | no | | |
| `slug` | VARCHAR(250) | no | | UNIQUE |
| `content` | TEXT | no | | |
| `status` | VARCHAR(20) | no | `'Draft'` | CHECK (status IN ('Draft', 'Published')) |
| `published_at` | TIMESTAMPTZ | yes | | |
| `featured_image_url` | VARCHAR(2048) | yes | | |
| `created_at` | TIMESTAMPTZ | no | `now()` | |
| `updated_at` | TIMESTAMPTZ | no | `now()` | |

### tags

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| `id` | UUID | no | `gen_random_uuid()` | PRIMARY KEY |
| `name` | VARCHAR(50) | no | | UNIQUE |
| `slug` | VARCHAR(75) | no | | UNIQUE |
| `created_at` | TIMESTAMPTZ | no | `now()` | |

### post_tags (join table)

| Column | Type | Nullable | Constraints |
|--------|------|----------|-------------|
| `post_id` | UUID | no | FK -> blog_posts(id) ON DELETE CASCADE |
| `tag_id` | UUID | no | FK -> tags(id) ON DELETE CASCADE |

**Primary Key**: composite (`post_id`, `tag_id`)

---

## 2. Indexes

### blog_posts

| Index Name | Columns | Type | Rationale |
|-----------|---------|------|-----------|
| `PK_blog_posts` | `id` | Primary Key | Identity lookup |
| `IX_blog_posts_slug` | `slug` | Unique | GET /api/posts/{slug} -- primary read path |
| `IX_blog_posts_status_published_at` | `status`, `published_at DESC` | Composite | List published posts sorted by date (homepage query) |
| `IX_blog_posts_created_at` | `created_at DESC` | Non-unique | Admin list sorted by creation date |

### tags

| Index Name | Columns | Type | Rationale |
|-----------|---------|------|-----------|
| `PK_tags` | `id` | Primary Key | Identity lookup |
| `IX_tags_name` | `name` | Unique | Duplicate name prevention |
| `IX_tags_slug` | `slug` | Unique | URL routing for tag pages |

### post_tags

| Index Name | Columns | Type | Rationale |
|-----------|---------|------|-----------|
| `PK_post_tags` | `post_id`, `tag_id` | Primary Key (composite) | Prevent duplicate associations |
| `IX_post_tags_tag_id` | `tag_id` | Non-unique | Query posts by tag (reverse lookup) |

**Note**: `post_id` is already covered by the composite PK (leftmost column) so no separate index is needed. The `tag_id` index enables the "filter posts by tag" and "count posts per tag" queries.

---

## 3. Entity Relationship Diagram

```
+------------------+       +----------------+       +------------------+
|   blog_posts     |       |   post_tags    |       |     tags         |
+------------------+       +----------------+       +------------------+
| id (PK)          |<------| post_id (FK)   |       | id (PK)          |
| title            |       | tag_id (FK)    |------>| name (UNIQUE)    |
| slug (UNIQUE)    |       +----------------+       | slug (UNIQUE)    |
| content          |              M:M               | created_at       |
| status           |                                +------------------+
| published_at     |
| featured_image_url|
| created_at       |
| updated_at       |
+------------------+
```

**Cardinality**:
- blog_posts 1:M post_tags M:1 tags (many-to-many through join table)
- A blog post can have zero or many tags
- A tag can be associated with zero or many posts

---

## 4. Cascade Rules

| Relationship | ON DELETE | Rationale |
|-------------|-----------|-----------|
| post_tags.post_id -> blog_posts.id | CASCADE | Deleting a post removes its tag associations |
| post_tags.tag_id -> tags.id | CASCADE | Deleting a tag removes its post associations |

**Important**: Deleting a tag never deletes posts. Only the join table rows are removed.

---

## 5. SQL DDL

```sql
CREATE TABLE blog_posts (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title           VARCHAR(200) NOT NULL,
    slug            VARCHAR(250) NOT NULL,
    content         TEXT NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'Draft'
                    CHECK (status IN ('Draft', 'Published')),
    published_at    TIMESTAMPTZ,
    featured_image_url VARCHAR(2048),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IX_blog_posts_slug
    ON blog_posts (slug);

CREATE INDEX IX_blog_posts_status_published_at
    ON blog_posts (status, published_at DESC);

CREATE INDEX IX_blog_posts_created_at
    ON blog_posts (created_at DESC);


CREATE TABLE tags (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(50) NOT NULL,
    slug        VARCHAR(75) NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IX_tags_name
    ON tags (name);

CREATE UNIQUE INDEX IX_tags_slug
    ON tags (slug);


CREATE TABLE post_tags (
    post_id UUID NOT NULL REFERENCES blog_posts(id) ON DELETE CASCADE,
    tag_id  UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (post_id, tag_id)
);

CREATE INDEX IX_post_tags_tag_id
    ON post_tags (tag_id);
```

---

## 6. Key Queries

### List published posts (homepage)

```sql
SELECT p.*, t.id as tag_id, t.name as tag_name, t.slug as tag_slug
FROM blog_posts p
LEFT JOIN post_tags pt ON p.id = pt.post_id
LEFT JOIN tags t ON pt.tag_id = t.id
WHERE p.status = 'Published'
ORDER BY p.published_at DESC;
```

### Filter published posts by tag

```sql
SELECT p.*, t.id as tag_id, t.name as tag_name, t.slug as tag_slug
FROM blog_posts p
INNER JOIN post_tags pt ON p.id = pt.post_id
INNER JOIN tags t ON pt.tag_id = t.id
WHERE p.status = 'Published'
  AND t.slug = @tagSlug
ORDER BY p.published_at DESC;
```

### List tags with post counts

```sql
SELECT t.id, t.name, t.slug, t.created_at,
       COUNT(pt.post_id) as post_count
FROM tags t
LEFT JOIN post_tags pt ON t.id = pt.tag_id
LEFT JOIN blog_posts p ON pt.post_id = p.id AND p.status = 'Published'
GROUP BY t.id, t.name, t.slug, t.created_at
ORDER BY t.name ASC;
```

### Get post by slug (single post view)

```sql
SELECT p.*, t.id as tag_id, t.name as tag_name, t.slug as tag_slug
FROM blog_posts p
LEFT JOIN post_tags pt ON p.id = pt.post_id
LEFT JOIN tags t ON pt.tag_id = t.id
WHERE p.slug = @slug;
```

---

## 7. EF Core Mapping Notes

### Value Object Mapping Strategy

EF Core value converters map between domain Value Objects and database primitives.

```csharp
// Example value converter pattern (crafter implements exact code)
// PostId <-> Guid
// Title <-> string
// Slug <-> string
// PostContent <-> string
// PostStatus <-> string
// TagId <-> Guid
// TagName <-> string
// TagSlug <-> string
// ImageUrl <-> string (nullable)
```

**Options (crafter decides):**
- **Value Converters**: `HasConversion<Guid, PostId>()` on each property
- **OwnsOne**: for composite Value Objects if extracted (e.g., PostTimestamps)
- **Complex Types** (EF Core 8+): if Value Objects are record structs

### BlogPost Configuration

```csharp
// Key points for EF configuration:
// - Slug as alternate key: HasAlternateKey(p => p.Slug)
// - Many-to-many: through post_tags join table
// - Status stored as string, not integer
// - featured_image_url nullable
// - Value converters for all Value Objects
```

### Tag Configuration

```csharp
// Key points:
// - Name unique index
// - Slug unique index
// - Value converters for TagId, TagName, TagSlug
```

### Many-to-Many (Post-Tags)

```csharp
// EF Core implicit join table configuration:
// - Table name: "post_tags"
// - Columns: post_id, tag_id
// - Both as foreign keys with cascade delete
// - Composite primary key
```

**Note**: EF Core 5+ supports implicit many-to-many without an explicit join entity. The crafter should use this unless a reason to add explicit join entity emerges.

---

## 8. Migration Strategy

- **Development**: EF Core code-first migrations
- **Testing**: Testcontainers spins up a real PostgreSQL instance, applies migrations automatically
- **Production**: EF Core migrations applied at startup or via a migration script in the deployment pipeline

**Initial migration creates**: `blog_posts`, `tags`, `post_tags` with all indexes.

No data migration from the old `blog/` database. The new system starts empty.

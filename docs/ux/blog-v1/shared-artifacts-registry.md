# Shared Artifacts Registry — The Augmented Craftsman v1

This registry tracks every piece of data that flows between journey steps.
Each artifact has a single source of truth and explicit consumers.
Untracked variables are integration failures waiting to happen.

---

## Authentication

| Artifact | Type | Source | Consumers | Validation |
|----------|------|--------|-----------|------------|
| `auth_token` | JWT string | J1.1 (login) | J1.2-J1.6, J3.1-J3.4, J4.1-J4.4 (all admin operations) | All admin API endpoints return 401 without valid token |

**Notes**:
- Single admin user, no multi-tenant concerns.
- Token issued on login, consumed by every admin endpoint.
- No refresh token in v1 -- session-based auth is acceptable for single user.

---

## Blog Post

| Artifact | Type | Source | Consumers | Validation |
|----------|------|--------|-----------|------------|
| `post_id` | UUID | J1.2 (create-post) | J1.3, J1.4, J1.5, J1.6 (publish flow); J3.2, J3.3, J3.4 (manage flow) | Unique per post, generated server-side |
| `slug` | Value Object (string) | J1.2 (create-post) | J1.7 (verify), J2.4 (read-post), public URL routing | **Immutable after creation.** Auto-generated from title at creation time only. Never changes on edit. Preserves URL stability for SEO and bookmarks. |
| `title` | Value Object (string) | J1.2 (create-post), J3.2 (edit-post) | J2.1 (homepage list), J2.4 (single post), J3.1 (admin list), J3.4 (delete confirm) | Required, non-empty |
| `content` | Value Object (Markdown string) | J1.2 (create-post), J3.2 (edit-post) | J1.5 (preview), J2.4 (read-post, rendered via Shiki) | Markdown with code blocks, rendered at build time by Astro |
| `status` | Enum: Draft, Published | J1.2 (create-post: Draft), J1.6 (publish: Published) | J2.1 (only Published posts shown), J3.1 (admin list shows status) | Draft posts invisible to readers |
| `published_date` | DateTime | J1.6 (publish) | J2.1 (homepage sort + display), J2.4 (single post display), J3.1 (admin list) | Set on publish, used for chronological ordering |
| `featured_image_url` | URL string (ImageKit) | J1.4 (upload-image) | J1.5 (preview), J2.4 (single post), J3.2 (edit form) | Optional. ImageKit URL. Null if no image uploaded. |
| `excerpt` | Derived string | Derived from `content` (first N characters or explicit) | J2.1 (homepage post cards) | Auto-derived from content, no separate storage needed |
| `post_list` | Collection of posts | J2.1 (homepage), J3.1 (admin list) | J2.2 (browse), J2.3 (filter) | Homepage: only Published, sorted by date desc. Admin: all statuses. |

---

## Tags

| Artifact | Type | Source | Consumers | Validation |
|----------|------|--------|-----------|------------|
| `tag_id` | UUID | J4.2 (create-tag), J1.3 (inline create) | J4.3 (edit-tag), J4.4 (delete-tag), J1.3/J3.3 (post association) | Unique per tag, generated server-side |
| `tag_name` | Value Object (string) | J4.2 (create-tag), J4.3 (edit-tag) | J2.1 (post card badges), J2.3 (filter label), J2.4 (post tag badges), J4.1 (tag list), J1.3/J3.3 (tag selector) | Required, unique, max 50 chars |
| `tag_slug` | Value Object (string) | J4.2 (create-tag), J4.3 (edit-tag) | J2.3 (filter URL: /tags/{slug}), public URL routing | Auto-generated from tag_name, URL-safe |
| `tag_ids[]` | Array of UUIDs | J1.3 (add-tags), J3.3 (update-tags) | J1.5 (preview tag display), J2.4 (post tag badges), J2.5 (related post matching) | Many-to-many relationship: Post <-> Tag |
| `tag_list` | Collection of tags | J4.1 (view-tags) | J1.3 (tag selector dropdown), J4.2-J4.4 (tag management) | Sorted alphabetically with post count per tag |
| `tag_post_count` | Integer (derived) | Derived from post-tag associations | J4.1 (tag list display), J4.4 (delete confirmation message) | Count of posts linked to a specific tag |

---

## Reader Navigation

| Artifact | Type | Source | Consumers | Validation |
|----------|------|--------|-----------|------------|
| `active_tag` | Tag reference | J2.3 (filter-by-tag, user click) | J2.3 (filter display, active highlight) | Null when no filter active. Set by clicking tag badge. |
| `current_post` | Post reference | J2.4 (read-post, URL routing) | J2.5 (related posts: exclude current, match tags) | Set when reader opens a single post page |
| `related_posts` | Collection (max 3) | J2.5 (computed from shared tags) | J2.5 (related posts section at bottom of article) | Posts sharing tags with current_post, excluding current_post, max 3. **Ranked by shared tag count DESC, then publish date DESC.** |

---

## Integration Flow Diagram

```
AUTHOR WRITES                            READER READS
=============                            ============

J1.1 login
  |
  v
auth_token -----> [gates all admin endpoints]
  |
  v
J1.2 create-post
  |
  +--> post_id ---------> J3.2 edit
  +--> slug ------------> J2.4 read (URL)
  +--> title -----------> J2.1 list, J2.4 display
  +--> content ----------> J2.4 display (Shiki)
  +--> status (Draft) ---> J2.1 (hidden until Published)
  |
  v
J1.3 add-tags
  |
  +--> tag_ids[] --------> J2.4 badges, J2.5 related matching
  |
  v
J1.4 upload-image
  |
  +--> featured_image_url -> J2.4 display
  |
  v
J1.6 publish
  |
  +--> status (Published) -> J2.1 (now visible)
  +--> published_date -----> J2.1 sort, J2.4 display

J4.2 create-tag
  |
  +--> tag_id, tag_name, tag_slug
  |       |
  |       +--> J1.3 selector
  |       +--> J2.1 badges
  |       +--> J2.3 filter
  |
J4.3 edit-tag
  |
  +--> Propagates new tag_name + tag_slug to ALL consumers above
  |
J4.4 delete-tag
  |
  +--> Removes tag_ids[] association from affected posts
  +--> Tag disappears from J2.1 badges, J2.3 filters
```

---

## Single Source of Truth Rules

1. **Post data** originates ONLY from J1.2 (create) or J3.2 (edit). No other step modifies post title or content. **Slug is set only in J1.2 (create) and is immutable thereafter.**
2. **Tag data** originates ONLY from J4.2 (create) or J4.3 (edit). Inline tag creation in J1.3 delegates to the same create logic.
3. **Post-tag associations** are modified in J1.3, J3.3 (add/remove from post side) or J4.4 (cascade remove from tag side).
4. **Image URLs** originate ONLY from J1.4 (upload). The URL is an ImageKit reference, not a local file path.
5. **Auth token** originates ONLY from J1.1 (login). Consumed by all admin operations.
6. **Published date** is set ONLY in J1.6 (publish). It is never editable after publish.
7. **Excerpt** is derived from content at render time. It is not a stored artifact.

---

## Data Flow: Build-Time SSG Boundary

```
+-------------------+          +-------------------+          +-------------------+
|   Author writes   |   API    |    PostgreSQL      |   API    |   Astro builds    |
|   (admin UI)      | -------> |    (source of      | -------> |   (static HTML)   |
|                   |  POST/   |     truth)         |  GET     |                   |
|   J1, J3, J4      |  PUT/    |                    |  at      |   J2 (reader      |
|                   |  DELETE  |                    |  build   |   experience)     |
+-------------------+          +-------------------+  time    +-------------------+
```

Key implication: changes made by the Author are NOT immediately visible to Readers.
The Astro site must be rebuilt (manually or via webhook) for changes to appear.
This is by design -- SSG means fast, cached pages for readers at the cost of
a short delay between publish and visibility.

# Requirements Specification -- The Augmented Craftsman v1

**Project**: Personal blog platform for daily Software Craftsmanship content
**Author**: Christian (single admin user)
**Audience**: Developers interested in TDD, DDD, Clean Architecture, XP
**Architecture**: Hexagonal + Vertical Slice (.NET 10) | Astro SSG frontend | PostgreSQL

---

## 1. Functional Requirements

### 1.1 Content Context (BlogPost CRUD)

| ID | Requirement | Details |
|----|-------------|---------|
| FR-C01 | Create blog post | Author creates a post with title and Markdown body. Post is saved as Draft. |
| FR-C02 | Markdown support | Body content supports full Markdown including headings, bold, italic, lists, links, and fenced code blocks with language annotation. |
| FR-C03 | Slug generation | System auto-generates a URL-safe slug from the post title (e.g., "TDD Is Not About Testing" becomes "tdd-is-not-about-testing"). Slugs must be unique. |
| FR-C04 | Excerpt derivation | System derives an excerpt from the post content for display in post lists. No separate storage -- derived at render time. |
| FR-C05 | Featured image association | Author can optionally set a featured image on a post (image stored in ImageKit, URL reference stored on post). |
| FR-C06 | Draft/Published lifecycle | Posts are created as Draft. Author explicitly publishes a draft, which sets status to Published and records the publish date. Only Published posts are visible to readers. |
| FR-C07 | Edit blog post | Author can edit title, body, tags, and featured image of an existing post. **Slug is immutable** — set at creation time, never changes. This preserves URL stability for SEO, social shares, and reader bookmarks. |
| FR-C08 | Delete blog post | Author can delete a post after explicit confirmation showing the post title. Deletion is irreversible. Tag associations are removed. |
| FR-C09 | List posts (admin) | Author sees all posts sorted by date (newest first) with title, status, publish date, tag count, and edit/delete actions. |
| FR-C10 | Preview post | Author can preview a draft post with rendered Markdown and syntax-highlighted code blocks before publishing. |

### 1.2 Tags Context (Tag Management)

| ID | Requirement | Details |
|----|-------------|---------|
| FR-T01 | Create tag | Author creates a tag by name. System auto-generates a URL-safe slug. Tag names must be unique, non-empty, and 50 characters or fewer. |
| FR-T02 | List tags | Author sees all tags sorted alphabetically with post count for each tag. |
| FR-T03 | Rename tag | Author renames a tag. Slug regenerates. New name must not conflict with existing tag names. The rename propagates to all post displays. |
| FR-T04 | Delete tag | Author deletes a tag after confirmation showing impact count ("{N} posts will lose this tag"). Posts themselves are not deleted -- only the tag association is removed. |
| FR-T05 | Associate tags with post | Author selects existing tags or creates new tags inline while editing a post. Many-to-many relationship. |
| FR-T06 | Disassociate tags from post | Author removes tag associations from a post. |

### 1.3 Media Context (Image Management)

| ID | Requirement | Details |
|----|-------------|---------|
| FR-M01 | Upload image to ImageKit | Author uploads an image file via authenticated admin endpoint `POST /api/images/upload`. System validates file size (max 10MB) and type, uploads to ImageKit using **server-side upload** (not client-side), and returns the ImageKit URL. |
| FR-M02 | Set featured image | Author sets an uploaded image as the featured image for a post. Thumbnail preview shown in admin. |
| FR-M03 | Remove featured image | Author removes the featured image from a post. Post remains valid without an image. |

### 1.4 Identity Context (Authentication)

| ID | Requirement | Details |
|----|-------------|---------|
| FR-I01 | Admin login | Single admin user authenticates with email and password. System issues a JWT token on success. |
| FR-I02 | Protected endpoints | All admin API endpoints (create, edit, delete posts/tags, upload images) require a valid JWT token. Requests without a valid token receive 401 Unauthorized. |
| FR-I03 | Login failure handling | Invalid credentials show "Invalid email or password". After 5 failed attempts within 10 minutes, account locks for 15 minutes with message "Too many attempts. Try again in 15 minutes." |

### 1.5 Reading Context (Public Experience)

| ID | Requirement | Details |
|----|-------------|---------|
| FR-R01 | Homepage with latest posts | Homepage displays published posts in reverse chronological order. Each post card shows title, publish date, tags, and excerpt. |
| FR-R02 | Browse all posts | Reader can browse the full list of published posts. |
| FR-R03 | Filter posts by tag | Reader clicks a tag badge to see only posts with that tag. Active filter is visually indicated. |
| FR-R04 | Single post view | Reader views a full post: title, publish date, featured image (if present), rendered Markdown body with syntax-highlighted code blocks (Shiki), and clickable tag badges. |
| FR-R05 | Related posts | At the bottom of a single post, display up to 3 posts sharing the same tags, excluding the current post. **Ranking**: posts with more shared tags rank higher; ties broken by publish date (newest first). Section is hidden if no related posts exist. |
| FR-R06 | Tag listing page | Reader can browse all tags with post counts, linking to filtered post lists. |
| FR-R07 | Empty states | No posts: "Coming soon. The first post is being forged." No posts for tag: "No posts tagged '{tag}' yet." Post not found: 404 page with link to homepage. |

### 1.6 Admin UI (Deferred to DESIGN Wave)

The admin interface is the driving adapter through which the Author manages content. Detailed wireframes and UI specifications are **deferred to the DESIGN wave**. The following constraints apply:

| ID | Constraint | Details |
|----|------------|---------|
| FR-UI01 | Login page | Email input, password input, "Sign In" button. Error message area for validation feedback. |
| FR-UI02 | Admin dashboard | Post list as default view after login. Navigation to tags management. |
| FR-UI03 | Post editor | Title field, Markdown body editor, tag selector (multi-select with inline create), featured image upload area, "Preview" and "Publish"/"Save Draft" actions. Slug displayed as read-only. |
| FR-UI04 | Tag management | Tag list with post counts, inline create, edit, and delete actions with confirmation dialogs. |
| FR-UI05 | Admin is API-driven | All admin UI actions call the .NET API. The admin UI can be a separate Astro island, a minimal SPA, or even a future mobile app — the API boundary is what matters. |

**Note**: For v1, the admin UI implementation is a **Should** priority. A working API with acceptance tests is sufficient to validate all business logic. The admin UI can be as minimal as needed — even Postman or curl against the API is acceptable for the Walking Skeleton and early Epics.

---

## 2. Non-Functional Requirements

### 2.1 Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-P01 | API read response time | < 200ms (p95) for GET endpoints |
| NFR-P02 | API write response time | < 500ms (p95) for POST/PUT/DELETE endpoints |
| NFR-P03 | Frontend page load | < 1 second for static pages (SSG, no JavaScript) |
| NFR-P04 | Image upload | < 3 seconds for images under 5MB |

### 2.2 SEO

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-S01 | Static site generation | Astro SSG -- all pages pre-built as static HTML at deploy time |
| NFR-S02 | Meta tags | Each page has proper title, description, and Open Graph meta tags |
| NFR-S03 | Structured data | Blog posts include JSON-LD structured data (Article schema) |
| NFR-S04 | Semantic HTML | Proper heading hierarchy, article elements, time elements |
| NFR-S05 | Clean URLs | Posts at /posts/{slug}, tags at /tags/{slug} |

### 2.3 Accessibility

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-A01 | WCAG 2.1 AA compliance | All public pages pass automated and manual WCAG 2.1 AA checks |
| NFR-A02 | Keyboard navigation | All interactive elements accessible via keyboard |
| NFR-A03 | Image alt text | Featured images include descriptive alt text |
| NFR-A04 | Color contrast | Minimum 4.5:1 contrast ratio for body text, 3:1 for large text |

### 2.4 Security

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-SEC01 | Authentication | JWT-based auth for admin endpoints |
| NFR-SEC02 | Input validation | All user input validated server-side (title, content, tag name, image) |
| NFR-SEC03 | HTTPS | All traffic served over HTTPS |
| NFR-SEC04 | CORS | API configured to accept requests only from known frontend origins |
| NFR-SEC05 | SQL injection prevention | Parameterized queries via EF Core |

### 2.5 Deployment

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-D01 | Frontend hosting | Vercel (free tier, edge CDN, zero config for Astro) |
| NFR-D02 | Backend hosting | Fly.io free tier (Docker container for .NET 10 API) |
| NFR-D03 | Database hosting | Neon serverless PostgreSQL (free tier) |
| NFR-D04 | Image hosting | ImageKit (existing integration) |
| NFR-D05 | Separation of deployments | Frontend and backend deploy independently, proving Hexagonal Architecture |

### 2.6 Testability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-T01 | TDD approach | Outside-In double loop (acceptance test drives unit tests) |
| NFR-T02 | Test coverage | All domain logic and use cases covered by tests |
| NFR-T03 | BDD acceptance tests | Gherkin scenarios translated to automated acceptance tests |
| NFR-T04 | Integration tests | API endpoints tested with in-memory or containerized database |

---

## 3. Walking Skeleton Requirements (Feature 0)

**Purpose**: Validate that all architectural layers connect end-to-end before building features.
**Target duration**: 1-2 days.

### What the Walking Skeleton proves

| Layer | Validation |
|-------|------------|
| Domain | BlogPost entity created with Title and Slug value objects |
| Application | CreatePost and GetPostBySlug use cases wired and functional |
| Infrastructure | EF Core persists and retrieves a BlogPost from PostgreSQL |
| API | Minimal API POST /api/posts and GET /api/posts/{slug} accept/return JSON |
| Frontend | Astro fetches post from API at build time and renders HTML at /posts/{slug} |

### Walking Skeleton scope

- Minimal BlogPost: title + content only (no tags, no image, no auth)
- No authentication (added in Epic 1)
- No validation beyond non-empty title
- No Markdown rendering (raw content stored and returned)
- Single happy path: create post, retrieve by slug, render in Astro

### Walking Skeleton does NOT include

- Tag management
- Image upload
- Authentication
- Draft/Published lifecycle
- Admin UI
- Error handling beyond basic 404

---

## 4. Bounded Context Map

```
+-------------------+     +-------------------+     +-------------------+
|                   |     |                   |     |                   |
|  Identity Context |     |  Content Context  |     |  Media Context    |
|                   |     |                   |     |                   |
|  - Admin login    |     |  - BlogPost CRUD  |     |  - Image upload   |
|  - JWT tokens     |---->|  - Slug generation|<----|  - ImageKit       |
|  - Auth gate      |     |  - Draft/Publish  |     |  - URL storage    |
|                   |     |  - Preview        |     |                   |
+-------------------+     +--------+----------+     +-------------------+
                                   |
                          +--------+----------+
                          |                   |
                          |   Tags Context    |
                          |                   |
                          |  - Tag CRUD       |
                          |  - Slug generation|
                          |  - Post-tag assoc.|
                          |                   |
                          +--------+----------+
                                   |
                          +--------+----------+
                          |                   |
                          |  Reading Context  |
                          |                   |
                          |  - Post listing   |
                          |  - Tag filtering  |
                          |  - Single post    |
                          |  - Related posts  |
                          |                   |
                          +-------------------+
```

**Data flow**: Identity gates access to Content, Tags, and Media (write side).
Content consumes Tags (many-to-many) and Media (featured image URL).
Reading consumes Content and Tags (read side, at Astro build time).

---

## 5. Ubiquitous Language

| Term | Definition |
|------|-----------|
| Post | A blog article with title, Markdown body, slug, tags, optional featured image, and status |
| Draft | A post that has been created but not yet published; invisible to readers |
| Published | A post that is live and visible to readers on the public site |
| Slug | A URL-safe, lowercase, hyphenated string derived from a title (e.g., "tdd-is-not-about-testing") |
| Tag | A label for categorizing posts; has a name and slug; many-to-many with posts |
| Featured Image | An optional image associated with a post, stored in ImageKit, displayed at the top of the post |
| Excerpt | A short preview of post content derived at render time for post list cards |
| Author | Christian -- the single admin user who creates, edits, and publishes content |
| Reader | An anonymous developer who browses and reads published content |
| Build | The Astro SSG process that generates static HTML from API data; changes are visible after rebuild |

---

## 6. Technical Implementation Notes

### ImageKit Image Upload (FR-M01)

| Concern | Decision |
|---------|----------|
| Upload method | **Server-side upload** via .NET API endpoint `POST /api/images/upload` |
| Authentication | JWT required — only the authenticated admin can upload |
| Credentials storage | ImageKit URL endpoint, public key, and private key stored in **environment variables** (`IMAGEKIT_URL_ENDPOINT`, `IMAGEKIT_PUBLIC_KEY`, `IMAGEKIT_PRIVATE_KEY`) |
| Client-side upload | **Not used** — all uploads go through the .NET API to prevent unauthorized uploads and quota exhaustion |
| File validation | Server-side: max 10MB, allowed types: PNG, JPEG, WebP, GIF |
| CORS | Configured on the .NET API (not on ImageKit) — only the admin frontend origin is allowed |
| Upload folder | Images organized under `augmented-craftsman/posts/` in ImageKit |

### Slug Immutability (FR-C03, FR-C07)

| Concern | Decision |
|---------|----------|
| Creation | Slug auto-generated from title at **creation time only** |
| On edit | Slug is **read-only** — title changes do NOT regenerate the slug |
| Rationale | URL stability for SEO, social media shares, and reader bookmarks |
| Uniqueness | Enforced at creation. If slug already exists, reject with "A post with this URL already exists" |

### Excerpt Derivation (FR-C04)

| Concern | Decision |
|---------|----------|
| Algorithm | First 150 characters of plain text content (Markdown stripped), ending at word boundary |
| Storage | **Not stored** — derived at render time (Astro build) |
| Fallback | If content is shorter than 150 characters, use full content as excerpt |

### SSG Build Trigger

| Concern | Decision |
|---------|----------|
| Mechanism | **Deferred to DESIGN wave** — options: Vercel deploy hook (webhook on publish), manual rebuild, GitHub Actions |
| Implication | Author changes are NOT immediately visible to readers — rebuild required |
| Acceptable delay | Under 5 minutes from publish to live (Vercel builds are typically 30-90 seconds for Astro) |

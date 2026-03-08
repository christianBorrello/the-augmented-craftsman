# User Stories -- The Augmented Craftsman v1

**Prioritization**: MoSCoW (Must / Should / Could)
**Sizing**: S (< 1 day) | M (1-2 days) | L (2-3 days)
**Traceability**: Each story references its journey step (J1-J4) and bounded context

---

## Epic 0: Walking Skeleton

> Validate all architecture layers end-to-end before building features.
> Must complete before any other Epic begins.

### US-001: Create a blog post via API

**As** the development team
**I want to** create a blog post by sending title and content to the API
**So that** I validate the domain, application, infrastructure, and API layers connect

**Size**: M
**Priority**: Must
**Bounded Context**: Content
**Journey**: Feature 0 (Walking Skeleton)
**Dependencies**: PostgreSQL provisioned, .NET 10 project scaffolded

---

### US-002: Retrieve a blog post via API

**As** the development team
**I want to** retrieve a blog post by its slug from the API
**So that** I validate the read path from database through API returns correct JSON

**Size**: S
**Priority**: Must
**Bounded Context**: Content
**Journey**: Feature 0 (Walking Skeleton)
**Dependencies**: US-001 (post must exist to retrieve)

---

### US-003: Render a blog post in Astro

**As** the development team
**I want to** fetch a blog post from the API at build time and render it as a static HTML page
**So that** I validate the frontend driving adapter connects to the backend

**Size**: M
**Priority**: Must
**Bounded Context**: Reading
**Journey**: Feature 0 (Walking Skeleton)
**Dependencies**: US-002 (API GET endpoint must work)

---

## Epic 1: Blog Post Management

> Full authoring workflow: authenticate, create, preview, publish, edit, delete.

### US-010: Authenticate as admin

**As** Christian (the author)
**I want to** log in with my email and password
**So that** I can access the admin area to manage my blog content

**Size**: M
**Priority**: Must
**Bounded Context**: Identity
**Journey**: J1 step 1 (login)

---

### US-011: Create blog post with full content

**As** Christian (the author)
**I want to** create a blog post with title, Markdown body, excerpt, tags, and featured image
**So that** I can write about Software Craftsmanship topics for my readers

**Size**: L
**Priority**: Must
**Bounded Context**: Content
**Journey**: J1 steps 2-4 (create-post, add-tags, upload-image)
**Dependencies**: US-010 (authentication), US-020 (tags exist), US-030 (image upload)

---

### US-012: Auto-generate URL slug from title

**As** Christian (the author)
**I want to** have a URL-safe slug automatically generated from my post title
**So that** my posts have clean, readable URLs without manual URL management

**Size**: S
**Priority**: Must
**Bounded Context**: Content
**Journey**: J1 step 2 (create-post)

---

### US-013: Preview blog post before publishing

**As** Christian (the author)
**I want to** preview my draft post with rendered Markdown and syntax-highlighted code blocks
**So that** I can verify the formatting looks right before publishing

**Size**: S
**Priority**: Should
**Bounded Context**: Content
**Journey**: J1 step 5 (preview)
**Dependencies**: US-011 (post must exist as draft)

---

### US-014: Publish a draft blog post

**As** Christian (the author)
**I want to** publish a draft post, setting its status to Published and recording the publish date
**So that** the post becomes visible to readers on the public site after the next build

**Size**: S
**Priority**: Must
**Bounded Context**: Content
**Journey**: J1 step 6 (publish)
**Dependencies**: US-011 (draft post exists)

---

### US-015: Edit an existing blog post

**As** Christian (the author)
**I want to** edit the title, body, tags, and image of an existing post
**So that** I can fix typos, update content, and keep my posts accurate

**Size**: M
**Priority**: Must
**Bounded Context**: Content
**Journey**: J3 step 2 (edit-post)
**Dependencies**: US-011 (post exists)

---

### US-016: Delete a blog post

**As** Christian (the author)
**I want to** delete a blog post after seeing a confirmation with the post title
**So that** I can remove outdated or incorrect content deliberately

**Size**: S
**Priority**: Must
**Bounded Context**: Content
**Journey**: J3 step 4 (delete-post)
**Dependencies**: US-011 (post exists)

---

### US-017: List all posts in admin view

**As** Christian (the author)
**I want to** see all my posts with status indicators (Draft/Published), dates, and tag counts
**So that** I have a complete overview of my blog content at a glance

**Size**: S
**Priority**: Must
**Bounded Context**: Content
**Journey**: J3 step 1 (view-all-posts)
**Dependencies**: US-010 (authentication)

---

## Epic 2: Tag Management

> Create, rename, delete, and associate tags with posts.

### US-020: Create a new tag

**As** Christian (the author)
**I want to** create a tag by entering a name (slug auto-generated)
**So that** I can organize my posts by topic

**Size**: S
**Priority**: Must
**Bounded Context**: Tags
**Journey**: J4 step 2 (create-tag)
**Dependencies**: US-010 (authentication)

---

### US-021: List all tags with post counts

**As** Christian (the author)
**I want to** see all my tags listed alphabetically with the number of posts using each tag
**So that** I can assess my content taxonomy at a glance

**Size**: S
**Priority**: Must
**Bounded Context**: Tags
**Journey**: J4 step 1 (view-tags)
**Dependencies**: US-010 (authentication)

---

### US-022: Rename a tag

**As** Christian (the author)
**I want to** rename a tag and have the new name propagate to all associated posts
**So that** I can improve my taxonomy without losing tag-post associations

**Size**: M
**Priority**: Should
**Bounded Context**: Tags
**Journey**: J4 step 3 (edit-tag)
**Dependencies**: US-020 (tag exists)

---

### US-023: Delete a tag

**As** Christian (the author)
**I want to** delete a tag after seeing how many posts will be affected
**So that** I can clean up unused or redundant tags with full awareness of the impact

**Size**: S
**Priority**: Should
**Bounded Context**: Tags
**Journey**: J4 step 4 (delete-tag)
**Dependencies**: US-020 (tag exists)

---

### US-024: Associate and disassociate tags with posts

**As** Christian (the author)
**I want to** add or remove tags on a post, including creating new tags inline
**So that** I can categorize my posts accurately as I write or edit them

**Size**: M
**Priority**: Must
**Bounded Context**: Tags + Content
**Journey**: J1 step 3 (add-tags), J3 step 3 (update-tags)
**Dependencies**: US-011 (post exists), US-020 (tag creation)

---

## Epic 3: Image Management

> Upload images to ImageKit and associate them with posts.

### US-030: Upload image to ImageKit

**As** Christian (the author)
**I want to** upload an image file and have it stored in ImageKit
**So that** I can use images in my blog posts without managing file storage

**Size**: M
**Priority**: Must
**Bounded Context**: Media
**Journey**: J1 step 4 (upload-image)
**Dependencies**: US-010 (authentication), ImageKit account configured

---

### US-031: Set featured image for a blog post

**As** Christian (the author)
**I want to** set an uploaded image as the featured image for a post
**So that** readers see a visual header when reading my post

**Size**: S
**Priority**: Must
**Bounded Context**: Media + Content
**Journey**: J1 step 4 (upload-image)
**Dependencies**: US-030 (image uploaded), US-011 (post exists)

---

### US-032: Remove featured image from a blog post

**As** Christian (the author)
**I want to** remove the featured image from a post
**So that** I can publish a post without an image or replace an incorrect image

**Size**: S
**Priority**: Should
**Bounded Context**: Media + Content
**Journey**: J3 step 2 (edit-post)
**Dependencies**: US-031 (featured image set)

---

## Epic 4: Public Reading Experience

> Everything a reader sees and interacts with on the public site.

### US-040: View homepage with latest posts

**As** a reader interested in Software Craftsmanship
**I want to** see the most recent blog posts when I visit the homepage
**So that** I can quickly find new content to read

**Size**: M
**Priority**: Must
**Bounded Context**: Reading
**Journey**: J2 step 1 (land-on-homepage)
**Dependencies**: US-014 (published posts exist)

---

### US-041: Browse all posts

**As** a reader
**I want to** scroll through all published posts with titles, dates, tags, and excerpts
**So that** I can scan the full catalog and choose what to read

**Size**: S
**Priority**: Must
**Bounded Context**: Reading
**Journey**: J2 step 2 (browse-posts)
**Dependencies**: US-040 (post list rendered)

---

### US-042: Filter posts by tag

**As** a reader
**I want to** click a tag badge to see only posts with that tag
**So that** I can focus on a specific topic like TDD or DDD

**Size**: M
**Priority**: Must
**Bounded Context**: Reading
**Journey**: J2 step 3 (filter-by-tag)
**Dependencies**: US-040 (posts displayed), US-020 (tags exist on posts)

---

### US-043: Read a single blog post

**As** a reader
**I want to** read a full blog post with rendered Markdown, syntax-highlighted code blocks, and featured image
**So that** I can learn from detailed Software Craftsmanship content

**Size**: M
**Priority**: Must
**Bounded Context**: Reading
**Journey**: J2 step 4 (read-post)
**Dependencies**: US-014 (post published)

---

### US-044: See related posts

**As** a reader
**I want to** see up to 3 related posts at the bottom of an article (same tags, excluding current)
**So that** I can continue learning about the same topic

**Size**: S
**Priority**: Should
**Bounded Context**: Reading
**Journey**: J2 step 5 (navigate-related)
**Dependencies**: US-043 (single post view), US-024 (posts have tags)

---

### US-045: Browse all tags with post counts

**As** a reader
**I want to** see a page listing all tags with the number of posts for each
**So that** I can discover what topics the blog covers

**Size**: S
**Priority**: Should
**Bounded Context**: Reading
**Journey**: J2 step 3 (filter-by-tag)
**Dependencies**: US-020 (tags exist)

---

### US-046: Navigate between tag pages and filtered post lists

**As** a reader
**I want to** click a tag on the tags page or on a post and land on a filtered post list for that tag
**So that** I can seamlessly explore content by topic

**Size**: S
**Priority**: Should
**Bounded Context**: Reading
**Journey**: J2 steps 3-5 (filter-by-tag, read-post, navigate-related)
**Dependencies**: US-042 (tag filtering), US-045 (tag listing page)

---

## Story Dependency Map

```
Epic 0 (Walking Skeleton)
  US-001 --> US-002 --> US-003
     |
     v
Epic 1 (Blog Post Management)
  US-010 (auth) ----+----> US-011 (create) ----> US-012 (slug, embedded)
                    |          |
                    |          +----> US-013 (preview)
                    |          +----> US-014 (publish)
                    |          +----> US-015 (edit)
                    |          +----> US-016 (delete)
                    +----> US-017 (list)
                    |
Epic 2 (Tags)       |
  US-020 (create) --+----> US-021 (list)
                    |----> US-022 (rename)
                    |----> US-023 (delete)
                    +----> US-024 (associate)
                    |
Epic 3 (Images)     |
  US-030 (upload) --+----> US-031 (set featured)
                    +----> US-032 (remove featured)
                    |
                    v
Epic 4 (Reading) -- requires Epics 1-3 data
  US-040 (homepage) --> US-041 (browse) --> US-042 (filter)
  US-043 (single post) --> US-044 (related)
  US-045 (tag page) --> US-046 (navigation)
```

---

## Story Count Summary

| Epic | Stories | Must | Should | Could | Effort |
|------|---------|------|--------|-------|--------|
| Epic 0: Walking Skeleton | 3 | 3 | 0 | 0 | S+M+M |
| Epic 1: Blog Post Management | 8 | 7 | 1 | 0 | 2S+3M+1L+2S |
| Epic 2: Tag Management | 5 | 3 | 2 | 0 | 3S+2M |
| Epic 3: Image Management | 3 | 2 | 1 | 0 | 2S+1M |
| Epic 4: Public Reading | 7 | 4 | 3 | 0 | 3S+3M+1S |
| **Total** | **26** | **19** | **7** | **0** | |

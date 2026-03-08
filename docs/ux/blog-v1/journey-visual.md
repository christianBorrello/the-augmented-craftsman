# UX Journey Maps — The Augmented Craftsman v1

## Personas

**Christian (The Author)** — Software Engineer who writes daily about Software Craftsmanship.
Single admin user. Writes in Markdown with code blocks. Uploads images via ImageKit.
Wants the publishing flow to feel like a craftsman's notebook: focused, efficient, no friction.

**Reader** — Developer interested in TDD, Clean Architecture, DDD, XP.
Arrives from search engines, social links, or direct navigation.
Wants to find relevant content quickly and read it without distraction.

---

## Feature 0: Walking Skeleton

**Purpose**: Validate architecture end-to-end before building features.
Minimal slice: create a blog post through the API, store it, retrieve it, render it in Astro.

```
  .NET API (POST)         PostgreSQL            .NET API (GET)          Astro (SSG)
 +-----------------+   +----------------+   +-----------------+   +-----------------+
 | POST /api/posts |-->| INSERT         |-->| GET /api/posts/1|-->| Build & render  |
 | {title, content}|   | blog_posts     |   | => JSON         |   | /posts/{slug}   |
 +-----------------+   +----------------+   +-----------------+   +-----------------+
        |                      |                     |                      |
   Driving Adapter      Driven Adapter         Driving Adapter       Driving Adapter
   (Minimal API)        (EF Core/PG)           (Minimal API)         (fetch at build)

 Validates:
   [x] Domain: BlogPost entity + Slug value object created
   [x] Application: CreatePost + GetPostById use cases wired
   [x] Infrastructure: EF Core persistence round-trips
   [x] API: Minimal API endpoints accept/return JSON
   [x] Frontend: Astro fetches from API and renders HTML
```

**Emotional arc**: Anxiety ("Will all layers connect?") --> Relief ("It works end-to-end")

---

## Journey 1: Author Publishes a Post

**Goal**: Christian writes and publishes a blog post about Software Craftsmanship.
**Trigger**: Christian has an idea for today's post.
**Success**: Post is live on the public blog with correct formatting, tags, and image.

```
  LOGIN            CREATE POST         ADD TAGS          UPLOAD IMAGE       PREVIEW          PUBLISH          VERIFY
+--------+      +--------------+    +------------+    +--------------+   +------------+   +-----------+   +-------------+
| Enter  |      | Write title  |    | Select     |    | Upload       |   | See post   |   | Set       |   | Open public |
| admin  |----->| Write body   |--->| existing   |--->| featured     |-->| as reader  |-->| status to |-->| URL in new  |
| creds  |      | (Markdown)   |    | tags or    |    | image via    |   | would see  |   | Published |   | tab, check  |
+--------+      +--------------+    | create new |    | ImageKit     |   | it         |   |           |   | rendering   |
    |                |              +------------+    +--------------+   +------------+   +-----------+   +-------------+
    |                |                   |                  |                  |                |                |
    v                v                   v                  v                  v                v                v
 Confident       Focused &          Quick &            Smooth              Satisfied        Relieved        Proud
 "I have       creative           effortless         "Image is           "Looks right"    "It's live"     "That reads
  access"      "Flow state"       "Tags just work"    uploaded"                                            well"
```

**Emotional Arc**:

```
Confidence
   5 |                                                        *------*-------*
   4 |              *-----------*-----------*--------*
   3 |
   2 | *
   1 |
     +------------------------------------------------------------>
       Login     Create      Tags        Image    Preview  Publish  Verify
```

Login starts lower (credential entry is a speed bump) then rises into creative flow.
Preview confirms expectations. Publish and verify bring closure and satisfaction.

### Step Detail

| Step | Christian Types/Clicks | System Responds | Artifacts |
|------|----------------------|-----------------|-----------|
| Login | email + password | JWT token, redirect to admin dashboard | `auth_token` |
| Create Post | title, markdown body | Slug auto-generated, draft saved | `post_id`, `slug` |
| Add Tags | select from list or type new | Tags linked to post | `tag_ids[]` |
| Upload Image | drag/drop or file picker | Image uploaded to ImageKit, URL stored | `featured_image_url` |
| Preview | clicks "Preview" | Rendered markdown with Shiki highlighting | -- |
| Publish | clicks "Publish" | Status changes to Published, publish date set | `published_date` |
| Verify | opens public URL | Astro page displays full post | -- |

### Error Paths

```
Login failed:                  "Invalid credentials" --> retry (max 5)
Empty title:                   "Title is required" --> focus title field
Duplicate slug:                "A post with this URL already exists" --> suggest alternative
Image upload fails:            "Upload failed. Try again." --> retry, post remains saveable without image
Markdown parse error:          Graceful degradation, show raw text in preview with warning
```

---

## Journey 2: Reader Discovers and Reads Content

**Goal**: A developer finds and reads a post about Software Craftsmanship.
**Trigger**: Search engine result, social media link, or direct visit.
**Success**: Reader finishes the article, gains insight, and explores more content.

```
  LAND ON SITE       BROWSE POSTS       FILTER BY TAG      READ POST         NAVIGATE RELATED
+---------------+  +---------------+  +---------------+  +---------------+  +------------------+
| See homepage  |  | Scroll post   |  | Click tag     |  | Read article  |  | See related      |
| with recent   |->| list with     |->| badge to      |->| with code     |->| posts by same    |
| posts         |  | titles, dates,|  | filter         |  | blocks,       |  | tag at bottom    |
|               |  | tags, excerpt |  |               |  | images        |  | of article       |
+---------------+  +---------------+  +---------------+  +---------------+  +------------------+
    |                   |                   |                   |                   |
    v                   v                   v                   v                   v
 Curious            Scanning          Focused             Engaged &          Interested
 "Clean, fast       "I can see        "Now showing        absorbed           "More on this
  site. Looks       what's here"      only TDD posts"     "Good content"     topic..."
  credible."
```

**Emotional Arc**:

```
Engagement
   5 |                                              *-----------*
   4 |                                   *
   3 |         *-----------*
   2 | *
   1 |
     +------------------------------------------------------------>
       Land     Browse       Filter       Read Post      Related
```

First impression is quick (fast load, clean design). Engagement deepens as reader finds
relevant content and enters reading flow. Related content sustains interest.

### Step Detail

| Step | Reader Does | System Shows | Artifacts |
|------|------------|-------------|-----------|
| Land | Navigates to homepage | Recent posts in reverse chronological order | -- |
| Browse | Scrolls post list | Title, date, tags, excerpt for each post | `post_list` |
| Filter by Tag | Clicks a tag badge | Filtered list showing only posts with that tag | `active_tag` |
| Read Post | Clicks post title | Full article: rendered Markdown, Shiki code blocks, featured image | `current_post` |
| Navigate Related | Scrolls to bottom | Posts sharing the same tags | `related_posts` |

### Empty States

```
No posts yet:          "Coming soon. The first post is being forged."
No posts for tag:      "No posts tagged '{tag}' yet."
Post not found:        404 page with link back to homepage
```

### Key UX Requirements

- Homepage loads in under 1 second (static HTML, no JS)
- Code blocks use Shiki syntax highlighting (built into Astro)
- Tag badges are clickable on both post list and single post view
- Related posts section shows 3 posts max, same tag, excluding current

---

## Journey 3: Author Manages Content

**Goal**: Christian edits, updates, or deletes existing posts.
**Trigger**: Typo found, content needs updating, or post should be removed.
**Success**: Content is updated on the live site after the next build.

```
  VIEW ALL POSTS     EDIT POST          UPDATE TAGS        DELETE POST
+----------------+ +---------------+  +---------------+  +------------------+
| See list of    | | Modify title, |  | Add/remove    |  | Confirm deletion |
| all posts with |->| body, image  |->| tags from     |  | with post title  |
| status, date,  | | in editor     |  | existing post |  | shown            |
| edit/delete    | |               |  |               |  |                  |
+----------------+ +---------------+  +---------------+  +------------------+
    |                   |                   |                   |
    v                   v                   v                   v
 In control         Focused             Quick               Cautious then
 "I see everything  "Same editor        "Tags updated"      confirmed
  at a glance"      as create"                              "Gone. No undo."
```

**Emotional Arc**:

```
Confidence
   5 |  *-----------*-----------*
   4 |                                   *
   3 |
   2 |                                              * (if delete confirmed)
   1 |
     +------------------------------------------------------------>
       View All    Edit         Tags       Delete (confirm)
```

Management feels controlled and predictable. Delete drops confidence briefly
due to the irreversibility, which is correct -- it should feel deliberate.

### Step Detail

| Step | Christian Does | System Responds | Artifacts |
|------|---------------|-----------------|-----------|
| View All Posts | Navigates to admin post list | All posts sorted by date, showing title, status, date, tag count | `post_list` |
| Edit Post | Clicks edit, modifies fields | Pre-populated form, same UI as create | `post_id` |
| Update Tags | Add/remove tag associations | Immediate save, tag list refreshes | `tag_ids[]` |
| Delete Post | Clicks delete, confirms in modal | Post removed, redirect to post list | -- |

### Error Paths

```
Edit non-existent post:    404 "Post not found" --> redirect to post list
Save with empty title:     "Title is required" --> same as create validation
Delete last post:          Allowed. No special handling (this is a blog, not a CMS).
```

---

## Journey 4: Author Manages Tags

**Goal**: Christian creates, edits, and deletes tags to organize content.
**Trigger**: New topic area, rename needed, or cleanup of unused tags.
**Success**: Tag taxonomy is clean and consistent.

```
  VIEW TAGS          CREATE TAG         EDIT TAG           DELETE TAG
+---------------+  +---------------+  +---------------+  +------------------+
| See all tags  |  | Enter tag     |  | Rename tag    |  | Confirm delete   |
| with post     |->| name          |->| (updates all  |  | Shows post count |
| count for     |  |               |  |  linked posts)|  | "3 posts will    |
| each          |  |               |  |               |  |  lose this tag"  |
+---------------+  +---------------+  +---------------+  +------------------+
    |                   |                   |                   |
    v                   v                   v                   v
 Organized          Quick               Careful             Informed then
 "I see my          "Tag created"       "Rename propagated  deliberate
  taxonomy"                              everywhere"        "I understand
                                                            the impact"
```

**Emotional Arc**:

```
Confidence
   5 |  *-----------*
   4 |                           *
   3 |                                              *
   2 |
   1 |
     +------------------------------------------------------------>
       View       Create        Edit         Delete
```

Tag management is a utility task. Confidence stays high for viewing and creating.
Edit requires care (propagation). Delete requires awareness of impact.

### Step Detail

| Step | Christian Does | System Responds | Artifacts |
|------|---------------|-----------------|-----------|
| View Tags | Navigates to tag management | All tags with post count, sorted alphabetically | `tag_list` |
| Create Tag | Types tag name, submits | Tag created, slug auto-generated, appears in list | `tag_id`, `tag_slug` |
| Edit Tag | Clicks edit, changes name | Name updated, slug regenerated, all linked posts reflect change | `tag_id` |
| Delete Tag | Clicks delete, sees impact count | Confirmation shows "{N} posts will lose this tag", then removes | -- |

### Error Paths

```
Duplicate tag name:        "A tag named '{name}' already exists"
Empty tag name:            "Tag name is required"
Delete tag with posts:     Allowed after confirmation. Posts lose the tag but are not deleted.
Tag name too long:         "Tag name must be 50 characters or fewer"
```

---

## Cross-Journey Shared Artifact Flow

```
                    Journey 1                Journey 3
                    (Publish)                (Manage)
                       |                        |
                  auth_token  <-- LOGIN --> auth_token
                       |                        |
                    post_id   <-- CREATE/EDIT -->  post_id
                       |                        |
                     slug     <-- DOMAIN  -->     slug
                       |                        |
                   tag_ids[]  <-- TAGS    -->   tag_ids[]
                       |                        |
             featured_image_url <-- IMAGE -->  featured_image_url
                       |                        |
                       v                        v
                  Journey 2 (Read)
                       |
                  post_list (homepage)
                  current_post (single post)
                  related_posts (navigation)
                  active_tag (filter)
```

All data originates from Journeys 1/3/4 (author writes), is stored in PostgreSQL,
and is consumed by Journey 2 (reader reads) via the .NET API at Astro build time.

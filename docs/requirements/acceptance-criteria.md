# Acceptance Criteria -- The Augmented Craftsman v1

All scenarios use concrete domain data. Personas: Christian (author/admin), Reader (anonymous developer).
Format: Gherkin (Given/When/Then). Each scenario is automatable as an xUnit + BDD acceptance test.

---

## Epic 0: Walking Skeleton

### Feature: US-001 -- Create a blog post via API

```gherkin
Scenario: Create a blog post with title and content
  Given the API is running and connected to PostgreSQL
  When a POST request is sent to "/api/posts" with:
    | title   | Hello World                 |
    | content | This is the **first** post. |
  Then the response status is 201
  And the response contains a post with slug "hello-world"
  And the post is persisted in the database

Scenario: Reject a post with empty title
  Given the API is running and connected to PostgreSQL
  When a POST request is sent to "/api/posts" with:
    | title   |                              |
    | content | Some content here.           |
  Then the response status is 400
  And the response contains "Title is required"

Scenario: Generate slug from title with special characters
  Given the API is running and connected to PostgreSQL
  When a POST request is sent to "/api/posts" with:
    | title   | TDD Is Not About Testing!    |
    | content | Design, not verification.    |
  Then the response contains a post with slug "tdd-is-not-about-testing"
```

### Feature: US-002 -- Retrieve a blog post via API

```gherkin
Scenario: Retrieve an existing post by slug
  Given a post exists with slug "hello-world" and title "Hello World"
  When a GET request is sent to "/api/posts/hello-world"
  Then the response status is 200
  And the response contains:
    | title   | Hello World                 |
    | slug    | hello-world                 |
    | content | This is the **first** post. |

Scenario: Return 404 for non-existent slug
  Given no post exists with slug "nonexistent-post"
  When a GET request is sent to "/api/posts/nonexistent-post"
  Then the response status is 404
```

### Feature: US-003 -- Render a blog post in Astro

```gherkin
Scenario: Astro renders a post fetched from the API
  Given a published post exists with slug "hello-world" and title "Hello World"
  When Astro builds the static site
  Then the page "/posts/hello-world" contains the post title "Hello World"
  And the page contains the rendered content

Scenario: Astro build handles missing API gracefully
  Given the API is unreachable
  When Astro attempts to build the static site
  Then the build fails with a clear error message indicating API connectivity
```

---

## Epic 1: Blog Post Management

### Feature: US-010 -- Authenticate as admin

```gherkin
Scenario: Successful login with valid credentials
  Given Christian is on the admin login page
  When Christian enters email "christian.borrello@live.it" and password "valid-password"
  Then Christian is redirected to the admin dashboard
  And Christian receives a valid JWT token

Scenario: Failed login with incorrect password
  Given Christian is on the admin login page
  When Christian enters email "christian.borrello@live.it" and password "wrong-password"
  Then the login page shows "Invalid email or password"
  And Christian remains on the login page
  And no JWT token is issued

Scenario: Failed login with unknown email
  Given Christian is on the admin login page
  When Christian enters email "unknown@example.com" and password "any-password"
  Then the login page shows "Invalid email or password"
  And no JWT token is issued

Scenario: Account locked after 5 failed attempts
  Given Christian has failed login 5 times in the last 10 minutes
  When Christian enters email "christian.borrello@live.it" and password "any-password"
  Then the login page shows "Too many attempts. Try again in 15 minutes."

Scenario: Unauthenticated access to admin endpoint is rejected
  Given no valid JWT token is provided
  When a POST request is sent to "/api/posts"
  Then the response status is 401

Scenario: Login page displays correct form layout
  Given Christian navigates to "/admin/login"
  Then the page displays an email input field
  And a password input field
  And a "Sign In" button
  And the error message area is initially empty
```

### Feature: US-011 -- Create blog post with full content

```gherkin
Scenario: Create a blog post with all fields
  Given Christian is authenticated
  When Christian creates a post with:
    | title   | TDD Is Not About Testing                                             |
    | content | ## The Misconception\nMost people think TDD is about testing. It's about **design**.\n\n```csharp\n[Fact]\npublic void should_design_through_tests() { }\n``` |
    | tags    | TDD, Clean Code                                                      |
    | image   | tdd-cycle.png                                                        |
  Then a draft post is created with slug "tdd-is-not-about-testing"
  And the post status is "Draft"
  And the post has tags "TDD" and "Clean Code"
  And the post has a featured image from ImageKit

Scenario: Create a post with title and content only
  Given Christian is authenticated
  When Christian creates a post with title "Quick Note on Refactoring" and content "Refactor on green."
  Then a draft post is created with slug "quick-note-on-refactoring"
  And the post has no tags
  And the post has no featured image

Scenario: Reject post with empty title
  Given Christian is authenticated
  When Christian creates a post with an empty title
  Then the system shows "Title is required"
  And no post is created

Scenario: Reject post with duplicate slug
  Given Christian is authenticated
  And a post exists with slug "tdd-is-not-about-testing"
  When Christian creates a post with title "TDD Is Not About Testing"
  Then the system shows "A post with this URL already exists"
  And no new post is created

Scenario: Create a post with Markdown containing code blocks
  Given Christian is authenticated
  When Christian creates a post with content containing a fenced C# code block
  Then the post content is stored with the raw Markdown including the code fence
```

### Feature: US-012 -- Auto-generate URL slug from title

```gherkin
Scenario: Generate slug from simple title
  Given Christian is authenticated
  When Christian creates a post with title "Value Objects Are Not DTOs"
  Then the generated slug is "value-objects-are-not-dtos"

Scenario: Generate slug from title with special characters
  Given Christian is authenticated
  When Christian creates a post with title "Why I Practice Object Calisthenics Daily!"
  Then the generated slug is "why-i-practice-object-calisthenics-daily"

Scenario: Generate slug from title with multiple spaces
  Given Christian is authenticated
  When Christian creates a post with title "The  Walking   Skeleton  Pattern"
  Then the generated slug is "the-walking-skeleton-pattern"

Scenario: Slug is lowercase
  Given Christian is authenticated
  When Christian creates a post with title "SOLID Principles in Practice"
  Then the generated slug is "solid-principles-in-practice"
```

### Feature: US-013 -- Preview blog post before publishing

```gherkin
Scenario: Preview renders Markdown with syntax highlighting
  Given Christian is authenticated
  And a draft post "TDD Is Not About Testing" exists with Markdown content containing a C# code block
  When Christian requests a preview of the post
  Then the preview shows rendered HTML with syntax-highlighted code

Scenario: Preview shows featured image
  Given Christian is authenticated
  And a draft post "TDD Is Not About Testing" exists with a featured image
  When Christian requests a preview of the post
  Then the preview displays the featured image

Scenario: Preview shows tags
  Given Christian is authenticated
  And a draft post "TDD Is Not About Testing" exists with tags "TDD" and "Clean Code"
  When Christian requests a preview of the post
  Then the preview displays tag badges for "TDD" and "Clean Code"
```

### Feature: US-014 -- Publish a draft blog post

```gherkin
Scenario: Publish a draft post
  Given Christian is authenticated
  And a draft post "TDD Is Not About Testing" exists
  When Christian publishes the post
  Then the post status changes to "Published"
  And the post has a publish date of today
  And the post is visible to readers after the next build

Scenario: Published post retains its content
  Given Christian is authenticated
  And a draft post "The Walking Skeleton Pattern" exists with tags "Architecture" and content about walking skeletons
  When Christian publishes the post
  Then the post title is "The Walking Skeleton Pattern"
  And the post tags are "Architecture"
  And the post content is unchanged

Scenario: Cannot publish an already published post
  Given Christian is authenticated
  And a published post "TDD Is Not About Testing" exists
  When Christian attempts to publish the post again
  Then the system indicates the post is already published
```

### Feature: US-015 -- Edit an existing blog post

```gherkin
Scenario: Edit post title and content preserves slug
  Given Christian is authenticated
  And a published post "TDD Is Not About Testing" exists with slug "tdd-is-not-about-testing"
  When Christian changes the title to "TDD Is About Design, Not Testing"
  And changes the content to updated Markdown
  And saves the post
  Then the post title is "TDD Is About Design, Not Testing"
  And the slug remains "tdd-is-not-about-testing"

Scenario: Slug is set at creation and never changes
  Given Christian is authenticated
  And a draft post exists with title "Value Objects Are Not DTOs" and slug "value-objects-are-not-dtos"
  When Christian changes the title to "Why Value Objects Matter"
  And saves the post
  Then the slug remains "value-objects-are-not-dtos"
  And the post is accessible at "/posts/value-objects-are-not-dtos"

Scenario: Edit form is pre-populated with existing data
  Given Christian is authenticated
  And a post exists with:
    | title | TDD Is Not About Testing |
    | slug  | tdd-is-not-about-testing |
    | tags  | TDD, Clean Code          |
  When Christian opens the edit form for "TDD Is Not About Testing"
  Then the title field contains "TDD Is Not About Testing"
  And the slug "tdd-is-not-about-testing" is displayed as read-only
  And the tags "TDD" and "Clean Code" are selected

Scenario: Edit a post that no longer exists
  Given Christian is authenticated
  When Christian tries to edit a post that has been deleted
  Then the system shows "Post not found"

Scenario: Reject edit with empty title
  Given Christian is authenticated
  And a post "TDD Is Not About Testing" exists
  When Christian clears the title and saves
  Then the system shows "Title is required"
  And the original title is preserved
```

### Feature: US-016 -- Delete a blog post

```gherkin
Scenario: Delete a post with confirmation
  Given Christian is authenticated
  And a post "Old Draft Post" exists
  When Christian requests deletion of "Old Draft Post"
  Then the system shows "Delete 'Old Draft Post'? This cannot be undone."
  When Christian confirms the deletion
  Then the post is removed from the database
  And "Old Draft Post" no longer appears in the admin post list

Scenario: Cancel post deletion
  Given Christian is authenticated
  And a post "Keep This Post" exists
  When Christian requests deletion of "Keep This Post"
  And cancels the confirmation
  Then the post "Keep This Post" still exists

Scenario: Delete a post with tags
  Given Christian is authenticated
  And a post "Old Draft Post" exists with tags "TDD" and "Clean Code"
  When Christian confirms deletion of "Old Draft Post"
  Then the post is removed
  And the tags "TDD" and "Clean Code" still exist (only the association is removed)

Scenario: Delete a post that no longer exists
  Given Christian is authenticated
  When Christian tries to delete a post that has already been removed
  Then the system shows "Post not found"
```

### Feature: US-017 -- List all posts in admin view

```gherkin
Scenario: View all posts sorted by date
  Given Christian is authenticated
  And these posts exist:
    | title                        | status    | date       |
    | TDD Is Not About Testing     | Published | 2026-03-05 |
    | Draft: Upcoming Post         | Draft     | 2026-03-06 |
    | Value Objects Are Not DTOs   | Published | 2026-03-04 |
  When Christian navigates to the admin post list
  Then posts are shown in order:
    | Draft: Upcoming Post         | Draft     |
    | TDD Is Not About Testing     | Published |
    | Value Objects Are Not DTOs   | Published |
  And each post shows title, status, date, and action buttons

Scenario: Admin list shows both drafts and published posts
  Given Christian is authenticated
  And 2 draft posts and 3 published posts exist
  When Christian navigates to the admin post list
  Then all 5 posts are shown

Scenario: Admin list with no posts
  Given Christian is authenticated
  And no posts exist
  When Christian navigates to the admin post list
  Then the list is empty with an indication to create a first post
```

---

## Epic 2: Tag Management

### Feature: US-020 -- Create a new tag

```gherkin
Scenario: Create a tag with valid name
  Given Christian is authenticated
  When Christian creates a tag with name "Refactoring"
  Then the tag "Refactoring" is created with slug "refactoring"
  And the tag appears in the tag list with post count 0

Scenario: Reject duplicate tag name
  Given Christian is authenticated
  And a tag "TDD" already exists
  When Christian creates a tag with name "TDD"
  Then the system shows "A tag named 'TDD' already exists"
  And no new tag is created

Scenario: Reject empty tag name
  Given Christian is authenticated
  When Christian creates a tag with an empty name
  Then the system shows "Tag name is required"

Scenario: Reject tag name exceeding 50 characters
  Given Christian is authenticated
  When Christian creates a tag with a name of 51 characters
  Then the system shows "Tag name must be 50 characters or fewer"

Scenario: Tag slug generated from name with spaces
  Given Christian is authenticated
  When Christian creates a tag with name "Clean Architecture"
  Then the tag slug is "clean-architecture"
```

### Feature: US-021 -- List all tags with post counts

```gherkin
Scenario: View tags sorted alphabetically with post counts
  Given Christian is authenticated
  And these tags exist:
    | name         | post_count |
    | TDD          | 5          |
    | Clean Code   | 3          |
    | Architecture | 2          |
    | DDD          | 1          |
  When Christian navigates to the tag management page
  Then tags are shown in order: Architecture, Clean Code, DDD, TDD
  And each tag displays its post count

Scenario: View tags when none exist
  Given Christian is authenticated
  And no tags exist
  When Christian navigates to the tag management page
  Then the page shows "No tags yet. Create your first tag to start organizing content."
```

### Feature: US-022 -- Rename a tag

```gherkin
Scenario: Rename a tag successfully
  Given Christian is authenticated
  And a tag "TDD" exists with slug "tdd" linked to 5 posts
  When Christian renames the tag from "TDD" to "Test-Driven Development"
  Then the tag name is updated to "Test-Driven Development"
  And the tag slug is updated to "test-driven-development"
  And all 5 linked posts now display "Test-Driven Development"

Scenario: Reject rename to existing tag name
  Given Christian is authenticated
  And tags "TDD" and "Clean Code" exist
  When Christian renames "TDD" to "Clean Code"
  Then the system shows "A tag named 'Clean Code' already exists"
  And the tag "TDD" retains its original name

Scenario: Reject rename to empty name
  Given Christian is authenticated
  And a tag "TDD" exists
  When Christian renames "TDD" to an empty name
  Then the system shows "Tag name is required"
  And the tag "TDD" retains its original name
```

### Feature: US-023 -- Delete a tag

```gherkin
Scenario: Delete a tag with linked posts
  Given Christian is authenticated
  And a tag "Legacy" exists linked to 3 posts
  When Christian requests deletion of tag "Legacy"
  Then the system shows "Delete tag 'Legacy'? 3 posts will lose this tag."
  When Christian confirms the deletion
  Then the tag "Legacy" is removed
  And the 3 previously linked posts no longer have the tag "Legacy"
  And the 3 posts themselves are not deleted

Scenario: Delete a tag with no linked posts
  Given Christian is authenticated
  And a tag "Unused" exists linked to 0 posts
  When Christian requests deletion of tag "Unused"
  Then the system shows "Delete tag 'Unused'? 0 posts will lose this tag."
  When Christian confirms the deletion
  Then the tag "Unused" is removed

Scenario: Cancel tag deletion
  Given Christian is authenticated
  And a tag "TDD" exists linked to 5 posts
  When Christian requests deletion of tag "TDD"
  And cancels the confirmation
  Then the tag "TDD" still exists with 5 linked posts
```

### Feature: US-024 -- Associate and disassociate tags with posts

```gherkin
Scenario: Add existing tags to a post
  Given Christian is authenticated
  And a draft post "TDD Is Not About Testing" exists
  And tags "TDD", "Clean Code", "XP" exist
  When Christian adds tags "TDD" and "Clean Code" to the post
  Then the post has tags "TDD" and "Clean Code"

Scenario: Create a new tag inline while tagging a post
  Given Christian is authenticated
  And a draft post "TDD Is Not About Testing" exists
  And no tag named "Software Design" exists
  When Christian types "Software Design" in the tag selector
  Then the tag "Software Design" is created with slug "software-design"
  And the tag is added to the post

Scenario: Remove a tag from a post
  Given Christian is authenticated
  And a post "TDD Is Not About Testing" exists with tags "TDD", "Clean Code", "XP"
  When Christian removes the tag "XP" from the post
  Then the post has tags "TDD" and "Clean Code"
  And the tag "XP" still exists in the system (only the association is removed)

Scenario: Post with no tags is valid
  Given Christian is authenticated
  And a draft post "Quick Note on Refactoring" exists with no tags
  When Christian saves the post without adding tags
  Then the post is saved successfully with no tags
```

---

## Epic 3: Image Management

### Feature: US-030 -- Upload image to ImageKit

```gherkin
Scenario: Upload a valid image
  Given Christian is authenticated
  When Christian uploads "tdd-cycle.png" (2MB)
  Then the image is stored in ImageKit
  And an ImageKit URL is returned

Scenario: Reject image exceeding size limit
  Given Christian is authenticated
  When Christian uploads "huge-diagram.png" (15MB)
  Then the system shows "Image must be under 10MB"
  And no image is stored

Scenario: Handle ImageKit service failure
  Given Christian is authenticated
  And ImageKit is temporarily unavailable
  When Christian uploads "tdd-cycle.png"
  Then the system shows "Upload failed. Try again."
  And the post remains saveable without the image
```

### Feature: US-031 -- Set featured image for a blog post

```gherkin
Scenario: Set featured image on a draft post
  Given Christian is authenticated
  And a draft post "TDD Is Not About Testing" exists
  When Christian uploads "tdd-cycle.png" as the featured image
  Then the post displays a thumbnail of the uploaded image
  And the ImageKit URL is stored on the post

Scenario: Replace existing featured image
  Given Christian is authenticated
  And a post "TDD Is Not About Testing" exists with a featured image
  When Christian uploads "new-tdd-diagram.png" as the new featured image
  Then the post featured image is updated to the new image
```

### Feature: US-032 -- Remove featured image from a blog post

```gherkin
Scenario: Remove featured image from a post
  Given Christian is authenticated
  And a post "TDD Is Not About Testing" exists with a featured image
  When Christian removes the featured image
  Then the post no longer has a featured image
  And the post is still valid

Scenario: Post without featured image displays correctly
  Given a published post "Quick Note on Refactoring" exists with no featured image
  When a reader navigates to "/posts/quick-note-on-refactoring"
  Then the post page displays without an image section
  And the layout adjusts gracefully
```

---

## Epic 4: Public Reading Experience

### Feature: US-040 -- View homepage with latest posts

```gherkin
Scenario: Homepage shows recent posts in reverse chronological order
  Given these published posts exist:
    | title                        | date       | tags             |
    | TDD Is Not About Testing     | 2026-03-05 | TDD, Clean Code  |
    | Hexagonal Architecture       | 2026-03-04 | Architecture     |
    | Why Value Objects Matter      | 2026-03-03 | DDD              |
  When a reader visits the homepage
  Then the posts are shown in order:
    | TDD Is Not About Testing     |
    | Hexagonal Architecture       |
    | Why Value Objects Matter      |
  And each post shows title, date, tags, and excerpt

Scenario: Homepage with no published posts
  Given no published posts exist
  When a reader visits the homepage
  Then the page shows "Coming soon. The first post is being forged."

Scenario: Homepage does not show draft posts
  Given a draft post "Upcoming Draft" exists
  And a published post "TDD Is Not About Testing" exists
  When a reader visits the homepage
  Then "TDD Is Not About Testing" is shown
  And "Upcoming Draft" is not shown
```

### Feature: US-041 -- Browse all posts

```gherkin
Scenario: Browse all published posts
  Given 5 published posts exist
  When a reader visits the posts page
  Then all 5 posts are shown with title, date, tags, and excerpt

Scenario: Post cards show metadata
  Given a published post exists:
    | title   | TDD Is Not About Testing |
    | date    | 2026-03-05               |
    | tags    | TDD, Clean Code          |
  When a reader sees the post card
  Then the card shows title "TDD Is Not About Testing"
  And the date "March 5, 2026"
  And tag badges for "TDD" and "Clean Code"
  And an excerpt of the post content
```

### Feature: US-042 -- Filter posts by tag

```gherkin
Scenario: Filter posts by clicking a tag
  Given these published posts exist:
    | title                        | tags             |
    | TDD Is Not About Testing     | TDD, Clean Code  |
    | The Red-Green-Refactor Cycle | TDD              |
    | Why Value Objects Matter      | DDD              |
  When a reader clicks the "TDD" tag
  Then only posts tagged "TDD" are shown:
    | TDD Is Not About Testing     |
    | The Red-Green-Refactor Cycle |
  And "Why Value Objects Matter" is not shown

Scenario: Filter by tag with no matching posts
  Given no posts are tagged "Event Sourcing"
  When a reader navigates to the tag page for "Event Sourcing"
  Then the page shows "No posts tagged 'Event Sourcing' yet."

Scenario: Active tag filter is visually indicated
  Given a reader is viewing posts filtered by tag "TDD"
  Then the "TDD" tag badge is visually highlighted as active
```

### Feature: US-043 -- Read a single blog post

```gherkin
Scenario: Read a full blog post
  Given a published post exists:
    | title   | TDD Is Not About Testing                           |
    | date    | 2026-03-05                                         |
    | tags    | TDD, Clean Code                                    |
    | image   | https://ik.imagekit.io/augmented/tdd-cycle.png  |
    | content | Markdown with C# code block                        |
  When a reader navigates to "/posts/tdd-is-not-about-testing"
  Then the page displays the title "TDD Is Not About Testing"
  And the publish date "March 5, 2026"
  And the featured image
  And the Markdown content with syntax-highlighted code blocks
  And clickable tag badges for "TDD" and "Clean Code"

Scenario: Read a post without featured image
  Given a published post "Quick Note on Refactoring" exists without a featured image
  When a reader navigates to "/posts/quick-note-on-refactoring"
  Then the post displays without an image section
  And the content renders normally

Scenario: Post not found returns 404
  When a reader navigates to "/posts/nonexistent-post"
  Then the page shows a 404 error
  And a link back to the homepage

Scenario: Tag badges on post are clickable
  Given a reader is viewing "TDD Is Not About Testing" with tag "TDD"
  When the reader clicks the "TDD" tag badge
  Then the reader is taken to the tag page showing all posts tagged "TDD"
```

### Feature: US-044 -- See related posts

```gherkin
Scenario: Related posts shown at bottom of article
  Given these published posts exist:
    | title                        | tags             |
    | TDD Is Not About Testing     | TDD, Clean Code  |
    | The Red-Green-Refactor Cycle | TDD              |
    | Test Doubles Explained        | TDD, Testing     |
    | Why Value Objects Matter      | DDD              |
  When a reader is viewing "TDD Is Not About Testing"
  Then the related posts section shows:
    | The Red-Green-Refactor Cycle |
    | Test Doubles Explained        |
  And "Why Value Objects Matter" is not in related posts
  And the current post is not in related posts

Scenario: Related posts limited to 3
  Given 5 other posts share tags with the current post
  When a reader views the current post
  Then at most 3 related posts are shown

Scenario: Related posts ranked by shared tag count then by date
  Given these published posts exist:
    | title                        | tags                  | date       |
    | Current Post                 | TDD, Clean Code, DDD  | 2026-03-05 |
    | Post A                       | TDD, Clean Code, DDD  | 2026-03-04 |
    | Post B                       | TDD, Clean Code       | 2026-03-03 |
    | Post C                       | TDD                   | 2026-03-02 |
    | Post D                       | TDD                   | 2026-03-01 |
  When a reader is viewing "Current Post"
  Then the related posts are shown in order:
    | Post A | (3 shared tags, newest) |
    | Post B | (2 shared tags)         |
    | Post C | (1 shared tag, newest)  |
  And "Post D" is not shown (limit 3)

Scenario: Related posts tie-break by date when same tag count
  Given these published posts exist:
    | title         | tags | date       |
    | Current Post  | TDD  | 2026-03-05 |
    | Newer Post    | TDD  | 2026-03-04 |
    | Older Post    | TDD  | 2026-03-01 |
  When a reader is viewing "Current Post"
  Then the related posts are shown in order:
    | Newer Post |
    | Older Post |

Scenario: No related posts available
  Given only one post exists tagged "Architecture"
  When a reader is viewing that post
  Then no related posts section is shown
```

### Feature: US-045 -- Browse all tags with post counts

```gherkin
Scenario: Tags page shows all tags with counts
  Given these tags exist with published posts:
    | name         | post_count |
    | TDD          | 5          |
    | Clean Code   | 3          |
    | DDD          | 1          |
  When a reader visits the tags page
  Then all tags are displayed with their post counts
  And each tag links to its filtered post list

Scenario: Tags page with no tags
  Given no tags exist
  When a reader visits the tags page
  Then an appropriate empty state is shown
```

### Feature: US-046 -- Navigate between tag pages and filtered post lists

```gherkin
Scenario: Click tag on tags page navigates to filtered list
  Given a reader is on the tags page
  And a tag "TDD" exists with 5 posts
  When the reader clicks "TDD"
  Then the reader sees the filtered post list for tag "TDD" at "/tags/tdd"

Scenario: Click tag on post navigates to filtered list
  Given a reader is viewing "TDD Is Not About Testing" with tag "DDD"
  When the reader clicks the "DDD" tag badge
  Then the reader sees the filtered post list for tag "DDD" at "/tags/ddd"

Scenario: Tag page URL uses slug
  Given a tag "Clean Architecture" exists with slug "clean-architecture"
  When a reader navigates to "/tags/clean-architecture"
  Then the filtered post list for "Clean Architecture" is shown
```

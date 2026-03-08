# ============================================================
# The Augmented Craftsman — v1 Gherkin Scenarios
# ============================================================
# Personas:
#   Christian (Author) — single admin user
#   Reader — anonymous developer visitor
# ============================================================

# ------------------------------------------------------------
# Feature 0: Walking Skeleton
# ------------------------------------------------------------
Feature: Walking Skeleton
  As the development team
  I want to verify the architecture end-to-end
  So that I know all layers connect before building features

  Scenario: Create and retrieve a blog post through all layers
    Given the API is running and connected to PostgreSQL
    When a POST request is sent to "/api/posts" with:
      | title   | Hello World                          |
      | content | This is the **first** post.          |
    Then the response status is 201
    And the response contains a post with slug "hello-world"
    When a GET request is sent to "/api/posts/hello-world"
    Then the response contains:
      | title   | Hello World                          |
      | content | This is the **first** post.          |
      | slug    | hello-world                          |

  Scenario: Astro renders a post fetched from the API
    Given a published post exists with slug "hello-world"
    When Astro builds the static site
    Then the page "/posts/hello-world" contains the post title "Hello World"
    And the page contains the rendered Markdown content

# ------------------------------------------------------------
# Journey 1: Author Publishes a Post
# ------------------------------------------------------------
Feature: Author publishes a blog post
  As Christian (the author)
  I want to write and publish blog posts about Software Craftsmanship
  So that I share daily insights with the developer community

  # --- Authentication ---

  Scenario: Successful login
    Given Christian is on the admin login page
    When Christian enters email "christian.borrello@live.it" and password "valid-password"
    Then Christian is redirected to the admin dashboard
    And Christian has a valid session

  Scenario: Failed login with wrong password
    Given Christian is on the admin login page
    When Christian enters email "christian.borrello@live.it" and password "wrong-password"
    Then the login page shows "Invalid email or password"
    And Christian remains on the login page

  Scenario: Account locked after too many attempts
    Given Christian has failed login 5 times in the last 10 minutes
    When Christian enters email "christian.borrello@live.it" and password "any-password"
    Then the login page shows "Too many attempts. Try again in 15 minutes."

  # --- Post Creation ---

  Scenario: Create a blog post with Markdown content
    Given Christian is authenticated
    When Christian creates a post with:
      | title   | TDD Is Not About Testing                              |
      | content | ## The Misconception\nMost people think TDD is about testing. It's about **design**.\n\n```csharp\n[Fact]\npublic void should_design_through_tests()\n{\n    // This test drives the design\n}\n``` |
    Then a draft post is created with slug "tdd-is-not-about-testing"
    And the post status is "Draft"

  Scenario: Auto-generate slug from title
    Given Christian is authenticated
    When Christian creates a post with title "Why I Practice Object Calisthenics Daily"
    Then the generated slug is "why-i-practice-object-calisthenics-daily"

  Scenario: Reject post with empty title
    Given Christian is authenticated
    When Christian creates a post with an empty title
    Then the system shows "Title is required"
    And no post is created

  Scenario: Handle duplicate slug
    Given Christian is authenticated
    And a post exists with slug "tdd-is-not-about-testing"
    When Christian creates a post with title "TDD Is Not About Testing"
    Then the system shows "A post with this URL already exists"
    And no new post is created

  # --- Tags ---

  Scenario: Add existing tags to a post
    Given Christian is authenticated
    And a draft post "TDD Is Not About Testing" exists
    And tags "TDD", "Clean Code", "XP" exist
    When Christian adds tags "TDD" and "Clean Code" to the post
    Then the post has tags "TDD" and "Clean Code"

  Scenario: Create a new tag while adding tags to a post
    Given Christian is authenticated
    And a draft post "TDD Is Not About Testing" exists
    When Christian types "Software Design" in the tag selector
    And the tag "Software Design" does not exist
    Then the tag "Software Design" is created with slug "software-design"
    And the tag is added to the post

  # --- Image Upload ---

  Scenario: Upload a featured image
    Given Christian is authenticated
    And a draft post "TDD Is Not About Testing" exists
    When Christian uploads "tdd-cycle.png" as the featured image
    Then the image is stored in ImageKit
    And the post displays a thumbnail of the uploaded image

  Scenario: Post is saveable without a featured image
    Given Christian is authenticated
    When Christian creates a post with title "Quick Note on Refactoring" and no image
    Then the post is created successfully without a featured image

  Scenario: Image upload failure
    Given Christian is authenticated
    And ImageKit is temporarily unavailable
    When Christian uploads "tdd-cycle.png" as the featured image
    Then the system shows "Upload failed. Try again."
    And the post remains saveable without the image

  # --- Preview ---

  Scenario: Preview renders Markdown with syntax highlighting
    Given Christian is authenticated
    And a draft post exists with Markdown content containing a C# code block
    When Christian clicks "Preview"
    Then the preview shows rendered HTML with Shiki syntax-highlighted code

  # --- Publish ---

  Scenario: Publish a draft post
    Given Christian is authenticated
    And a draft post "TDD Is Not About Testing" exists with tags and content
    When Christian clicks "Publish"
    Then the post status changes to "Published"
    And the post has a publish date of today

  Scenario: Verify published post on public site
    Given a post "TDD Is Not About Testing" is published
    When Christian opens "/posts/tdd-is-not-about-testing" on the public site
    Then the page displays the full post with:
      | title          | TDD Is Not About Testing   |
      | tags           | TDD, Clean Code            |
      | code_highlight | yes                        |

# ------------------------------------------------------------
# Journey 2: Reader Discovers and Reads Content
# ------------------------------------------------------------
Feature: Reader discovers and reads blog content
  As a developer interested in Software Craftsmanship
  I want to browse and read blog posts
  So that I learn from real-world TDD, DDD, and Clean Architecture practice

  # --- Homepage ---

  Scenario: Homepage shows recent posts
    Given these published posts exist:
      | title                                  | date       | tags             |
      | TDD Is Not About Testing               | 2026-03-05 | TDD, Clean Code  |
      | Hexagonal Architecture for Real         | 2026-03-04 | Architecture     |
      | Why Value Objects Matter                | 2026-03-03 | DDD              |
    When a reader visits the homepage
    Then the posts are shown in reverse chronological order
    And each post shows title, date, tags, and excerpt

  Scenario: Homepage with no published posts
    Given no published posts exist
    When a reader visits the homepage
    Then the page shows "Coming soon. The first post is being forged."

  # --- Tag Filtering ---

  Scenario: Filter posts by tag
    Given these published posts exist:
      | title                                  | tags             |
      | TDD Is Not About Testing               | TDD, Clean Code  |
      | The Red-Green-Refactor Cycle            | TDD              |
      | Why Value Objects Matter                | DDD              |
    When a reader clicks the "TDD" tag
    Then only posts tagged "TDD" are shown:
      | TDD Is Not About Testing    |
      | The Red-Green-Refactor Cycle |
    And the "DDD" post is not shown

  Scenario: Filter by tag with no matching posts
    Given no posts are tagged "Event Sourcing"
    When a reader navigates to the tag page for "Event Sourcing"
    Then the page shows "No posts tagged 'Event Sourcing' yet."

  # --- Single Post ---

  Scenario: Read a full blog post
    Given a published post exists:
      | title   | TDD Is Not About Testing                                |
      | date    | 2026-03-05                                              |
      | tags    | TDD, Clean Code                                         |
      | image   | https://ik.imagekit.io/augmented/tdd-cycle.png            |
      | content | Markdown with C# code block                             |
    When a reader navigates to "/posts/tdd-is-not-about-testing"
    Then the page displays the post title "TDD Is Not About Testing"
    And the publish date "March 5, 2026"
    And the featured image
    And the Markdown content with syntax-highlighted code blocks
    And clickable tag badges for "TDD" and "Clean Code"

  Scenario: Post not found
    When a reader navigates to "/posts/nonexistent-post"
    Then the page shows a 404 error
    And a link back to the homepage

  # --- Related Posts ---

  Scenario: Related posts shown at bottom of article
    Given these published posts exist:
      | title                                  | tags             |
      | TDD Is Not About Testing               | TDD, Clean Code  |
      | The Red-Green-Refactor Cycle            | TDD              |
      | Test Doubles Explained                  | TDD, Testing     |
      | Why Value Objects Matter                | DDD              |
    When a reader is viewing "TDD Is Not About Testing"
    Then the related posts section shows:
      | The Red-Green-Refactor Cycle |
      | Test Doubles Explained       |
    And "Why Value Objects Matter" is not in related posts
    And the current post is not in related posts

  Scenario: No related posts available
    Given only one post exists tagged "Architecture"
    When a reader is viewing that post
    Then no related posts section is shown

# ------------------------------------------------------------
# Journey 3: Author Manages Content
# ------------------------------------------------------------
Feature: Author manages blog content
  As Christian (the author)
  I want to edit and delete existing posts
  So that I keep the blog content accurate and up to date

  # --- Post List ---

  Scenario: View all posts in admin
    Given Christian is authenticated
    And these posts exist:
      | title                                  | status    | date       |
      | TDD Is Not About Testing               | Published | 2026-03-05 |
      | Draft: Upcoming Refactoring Post        | Draft     | 2026-03-06 |
    When Christian navigates to the admin post list
    Then all posts are shown sorted by date newest first
    And each post shows title, status, date, and action buttons

  # --- Editing ---

  Scenario: Edit an existing post
    Given Christian is authenticated
    And a published post "TDD Is Not About Testing" exists
    When Christian clicks edit on "TDD Is Not About Testing"
    Then the editor is pre-populated with the existing title, content, tags, and image
    When Christian changes the title to "TDD Is About Design, Not Testing"
    And saves the post
    Then the post title is updated to "TDD Is About Design, Not Testing"
    And the slug is updated to "tdd-is-about-design-not-testing"

  Scenario: Edit a post that no longer exists
    Given Christian is authenticated
    When Christian tries to edit a post that has been deleted
    Then the system shows "Post not found"
    And Christian is redirected to the post list

  # --- Deleting ---

  Scenario: Delete a post with confirmation
    Given Christian is authenticated
    And a post "Old Draft Post" exists
    When Christian clicks delete on "Old Draft Post"
    Then a confirmation dialog shows "Delete 'Old Draft Post'? This cannot be undone."
    When Christian confirms the deletion
    Then the post is removed
    And Christian is redirected to the post list
    And "Old Draft Post" no longer appears in the list

  Scenario: Cancel post deletion
    Given Christian is authenticated
    And a post "Keep This Post" exists
    When Christian clicks delete on "Keep This Post"
    And cancels the confirmation dialog
    Then the post "Keep This Post" still exists

# ------------------------------------------------------------
# Journey 4: Author Manages Tags
# ------------------------------------------------------------
Feature: Author manages tags
  As Christian (the author)
  I want to create, rename, and delete tags
  So that I maintain a clean and consistent content taxonomy

  # --- View Tags ---

  Scenario: View all tags with post counts
    Given Christian is authenticated
    And these tags exist:
      | name           | post_count |
      | TDD            | 5          |
      | Clean Code     | 3          |
      | Architecture   | 2          |
      | DDD            | 1          |
    When Christian navigates to the tag management page
    Then all tags are shown alphabetically with their post counts

  Scenario: View tags when none exist
    Given Christian is authenticated
    And no tags exist
    When Christian navigates to the tag management page
    Then the page shows "No tags yet. Create your first tag to start organizing content."

  # --- Create Tag ---

  Scenario: Create a new tag
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

  Scenario: Reject tag name exceeding maximum length
    Given Christian is authenticated
    When Christian creates a tag with a name of 51 characters
    Then the system shows "Tag name must be 50 characters or fewer"

  # --- Edit Tag ---

  Scenario: Rename a tag
    Given Christian is authenticated
    And a tag "TDD" exists linked to 5 posts
    When Christian renames the tag from "TDD" to "Test-Driven Development"
    Then the tag name is updated to "Test-Driven Development"
    And the tag slug is updated to "test-driven-development"
    And all 5 linked posts now show "Test-Driven Development"

  Scenario: Reject rename to existing tag name
    Given Christian is authenticated
    And tags "TDD" and "Clean Code" exist
    When Christian renames "TDD" to "Clean Code"
    Then the system shows "A tag named 'Clean Code' already exists"
    And the tag "TDD" retains its original name

  # --- Delete Tag ---

  Scenario: Delete a tag with linked posts
    Given Christian is authenticated
    And a tag "Legacy" exists linked to 3 posts
    When Christian clicks delete on the tag "Legacy"
    Then a confirmation shows "Delete tag 'Legacy'? 3 posts will lose this tag."
    When Christian confirms the deletion
    Then the tag "Legacy" is removed
    And the 3 previously linked posts no longer have the tag "Legacy"
    And the 3 posts are NOT deleted

  Scenario: Delete a tag with no linked posts
    Given Christian is authenticated
    And a tag "Unused" exists linked to 0 posts
    When Christian clicks delete on the tag "Unused"
    Then a confirmation shows "Delete tag 'Unused'? 0 posts will lose this tag."
    When Christian confirms the deletion
    Then the tag "Unused" is removed

  # --- Cross-Journey: Auth Gate ---

  Scenario: Unauthenticated access to admin endpoints is rejected
    Given no valid session exists
    When a request is made to any admin endpoint
    Then the response status is 401
    And the user is redirected to the login page

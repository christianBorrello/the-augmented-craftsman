@epic0 @walking-skeleton @frontend @manual
Feature: Render a blog post in Astro
  As the development team
  I want Astro to fetch and render a published post
  So that I know the frontend-to-backend pipeline works

  # US-003: Render a blog post in Astro
  #
  # These scenarios require the Astro build environment (Node.js + running API).
  # They are NOT automated via WebApplicationFactory.
  # Verify manually or via build script: `npm run build` in frontend/
  # Tagged @manual to exclude from `dotnet test` runs.

  @manual
  Scenario: Astro renders a published post
    Given a published post exists with slug "hello-world" and title "Hello World"
    And the blog backend is running
    When Astro builds the static site
    Then the page "/posts/hello-world" contains the post title "Hello World"
    And the page contains the rendered content

  @manual
  Scenario: Astro build fails when backend is unreachable
    Given the blog backend is not running
    When Astro attempts to build the static site
    Then the build fails with a clear error message

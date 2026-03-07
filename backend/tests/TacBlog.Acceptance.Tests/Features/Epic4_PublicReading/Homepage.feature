@epic4 @api
Feature: View homepage with latest posts
  As a developer interested in Software Craftsmanship
  I want to see the latest posts on the homepage
  So that I can discover new content

  # US-040: View homepage with latest posts

  Scenario: Homepage shows recent posts in reverse chronological order
    Given these published posts exist:
      | title                        | date       | tags            |
      | TDD Is Not About Testing     | 2026-03-05 | TDD, Clean Code |
      | Hexagonal Architecture       | 2026-03-04 | Architecture    |
      | Why Value Objects Matter      | 2026-03-03 | DDD             |
    When a GET request is sent to "/api/posts"
    Then the response status is 200
    And the posts are returned in reverse chronological order
    And each post contains title, slug, date, tags, and excerpt

  Scenario: Homepage with no published posts
    Given no published posts exist
    When a GET request is sent to "/api/posts"
    Then the response status is 200
    And the post list is empty

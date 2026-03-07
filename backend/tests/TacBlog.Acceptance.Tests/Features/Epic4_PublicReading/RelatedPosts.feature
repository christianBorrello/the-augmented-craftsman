@epic4 @api
Feature: See related posts
  As a reader
  I want to see related posts at the bottom of an article
  So that I can continue reading about similar topics

  # US-044: See related posts
  # Related posts are ranked by number of shared tags (descending),
  # then by publish date (newest first) as tie-breaker.
  # Maximum 3 related posts returned.

  Scenario: Related posts shown based on shared tags
    Given these published posts exist:
      | title                        | tags             |
      | TDD Is Not About Testing     | TDD, Clean Code  |
      | The Red-Green-Refactor Cycle | TDD              |
      | Test Doubles Explained       | TDD, Testing     |
      | Why Value Objects Matter      | DDD              |
    When a reader requests related posts for "tdd-is-not-about-testing"
    Then the related posts include:
      | The Red-Green-Refactor Cycle |
      | Test Doubles Explained       |
    And "Why Value Objects Matter" is not in the related posts
    And "TDD Is Not About Testing" is not in the related posts

  Scenario: Related posts ranked by shared tag count then date
    Given these published posts exist:
      | title                        | date       | tags                    |
      | TDD Is Not About Testing     | 2026-03-05 | TDD, Clean Code, SOLID  |
      | Clean Code Fundamentals      | 2026-03-04 | Clean Code, SOLID       |
      | Red-Green-Refactor           | 2026-03-03 | TDD                     |
      | SOLID Deep Dive              | 2026-03-02 | SOLID                   |
    When a reader requests related posts for "tdd-is-not-about-testing"
    Then the first related post is "Clean Code Fundamentals"
    And "Clean Code Fundamentals" appears before "Red-Green-Refactor"
    And "Red-Green-Refactor" appears before "SOLID Deep Dive"

  Scenario: Related posts tie-break by newest date
    Given these published posts exist:
      | title                        | date       | tags |
      | TDD Is Not About Testing     | 2026-03-05 | TDD  |
      | Older TDD Post               | 2026-03-01 | TDD  |
      | Newer TDD Post               | 2026-03-04 | TDD  |
    When a reader requests related posts for "tdd-is-not-about-testing"
    Then "Newer TDD Post" appears before "Older TDD Post"

  Scenario: Related posts limited to 3
    Given these published posts exist:
      | title                        | tags |
      | TDD Is Not About Testing     | TDD  |
      | Related Post 1               | TDD  |
      | Related Post 2               | TDD  |
      | Related Post 3               | TDD  |
      | Related Post 4               | TDD  |
    When a reader requests related posts for "tdd-is-not-about-testing"
    Then exactly 3 related posts are returned

  Scenario: Current post is excluded from related posts
    Given these published posts exist:
      | title                        | tags |
      | TDD Is Not About Testing     | TDD  |
      | Another TDD Post             | TDD  |
    When a reader requests related posts for "tdd-is-not-about-testing"
    Then "TDD Is Not About Testing" is not in the related posts

  Scenario: No related posts available
    Given only one post exists tagged "Architecture"
    When a reader requests related posts for that post
    Then the related posts list is empty

  Scenario: Related posts for a non-existent post
    When a reader requests related posts for "nonexistent-slug"
    Then the response status is 404

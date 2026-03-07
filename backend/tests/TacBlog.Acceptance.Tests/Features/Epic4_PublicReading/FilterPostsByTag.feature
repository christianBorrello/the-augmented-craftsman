@epic4 @api
Feature: Filter posts by tag
  As a reader
  I want to filter posts by tag
  So that I can focus on topics that interest me

  # US-042: Filter posts by tag

  Scenario: Filter posts by a specific tag
    Given these published posts exist:
      | title                        | tags            |
      | TDD Is Not About Testing     | TDD, Clean Code |
      | The Red-Green-Refactor Cycle | TDD             |
      | Why Value Objects Matter      | DDD             |
    When a reader filters posts by tag "TDD"
    Then the response status is 200
    And only posts tagged "TDD" are returned:
      | TDD Is Not About Testing     |
      | The Red-Green-Refactor Cycle |
    And "Why Value Objects Matter" is not included

  Scenario: Filter by tag with no matching posts
    Given no posts are tagged "Event Sourcing"
    When a reader filters posts by tag "event-sourcing"
    Then the response status is 200
    And the post list is empty

  Scenario: Filter by non-existent tag slug
    When a reader filters posts by tag "nonexistent-tag"
    Then the response status is 404

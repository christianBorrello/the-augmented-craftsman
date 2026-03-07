@epic4 @api
Feature: Browse tags and navigate between tag pages
  As a reader
  I want to browse all tags and click through to filtered post lists
  So that I can explore content by topic

  # US-045: Browse all tags with post counts
  # US-046: Navigate between tag pages and filtered post lists

  Scenario: Browse all tags with post counts
    Given these tags exist with published post counts:
      | name         | post_count |
      | TDD          | 5          |
      | Clean Code   | 3          |
      | Architecture | 2          |
      | DDD          | 1          |
    When a reader requests all tags
    Then the response status is 200
    And all tags are returned with their post counts
    And tags are sorted alphabetically

  Scenario: Tag page links to filtered posts
    Given a tag "TDD" exists with slug "tdd" and 3 published posts
    When a reader requests posts filtered by tag slug "tdd"
    Then the response status is 200
    And 3 posts are returned

  Scenario: Empty tag is not shown to readers
    Given a tag "Unused" exists with 0 published posts
    When a reader requests all tags
    Then "Unused" is not included in the public tag list

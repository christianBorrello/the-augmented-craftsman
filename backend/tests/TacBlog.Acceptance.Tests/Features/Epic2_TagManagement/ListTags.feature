@epic2 @skip @api
Feature: List all tags with post counts
  As Christian (the author)
  I want to see all tags and how many posts use each
  So that I can maintain a clean taxonomy

  # US-021: List all tags with post counts

  Scenario: View all tags with post counts
    Given these tags exist with post counts:
      | name         | post_count |
      | TDD          | 5          |
      | Clean Code   | 3          |
      | Architecture | 2          |
      | DDD          | 1          |
    When a GET request is sent to "/api/tags"
    Then the response status is 200
    And all tags are returned alphabetically with their post counts

  Scenario: View tags when none exist
    When a GET request is sent to "/api/tags"
    Then the response status is 200
    And the tag list is empty

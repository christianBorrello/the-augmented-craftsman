@epic1 @api
Feature: List all posts in admin view
  As Christian (the author)
  I want to see all posts (drafts and published)
  So that I can manage my content

  # US-017: List all posts in admin view

  Background:
    Given Christian is authenticated

  Scenario: View all posts sorted by date
    Given these posts exist:
      | title                      | status    | date       |
      | TDD Is Not About Testing   | Published | 2026-03-05 |
      | Draft: Upcoming Post       | Draft     | 2026-03-06 |
      | Value Objects Are Not DTOs | Published | 2026-03-04 |
    When Christian requests the admin post list
    Then the response status is 200
    And posts are returned in reverse chronological order
    And each post contains title, status, date, and id

  Scenario: Admin list shows both drafts and published posts
    Given 2 draft posts and 3 published posts exist
    When Christian requests the admin post list
    Then 5 posts are returned

  Scenario: Admin list with no posts
    When Christian requests the admin post list
    Then the response status is 200
    And the post list is empty

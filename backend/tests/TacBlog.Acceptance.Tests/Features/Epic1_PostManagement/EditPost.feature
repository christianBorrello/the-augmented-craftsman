@epic1 @skip @api
Feature: Edit an existing blog post
  As Christian (the author)
  I want to edit posts after creation
  So that I keep content accurate and up to date

  # US-015: Edit an existing blog post

  Background:
    Given Christian is authenticated

  Scenario: Edit post title and content
    Given a published post "TDD Is Not About Testing" exists with slug "tdd-is-not-about-testing"
    When Christian updates the post with:
      | title   | TDD Is About Design, Not Testing |
      | content | Updated Markdown content          |
    Then the response status is 200
    And the post title is "TDD Is About Design, Not Testing"
    And the slug remains "tdd-is-not-about-testing"

  Scenario: Slug is immutable after creation
    Given a draft post exists with title "Value Objects Are Not DTOs" and slug "value-objects-are-not-dtos"
    When Christian updates the post title to "Why Value Objects Matter"
    Then the slug remains "value-objects-are-not-dtos"

  Scenario: Edit form is pre-populated with existing data
    Given a post exists with:
      | title | TDD Is Not About Testing |
      | slug  | tdd-is-not-about-testing |
      | tags  | TDD, Clean Code          |
    When Christian retrieves the post for editing
    Then the response contains:
      | title | TDD Is Not About Testing |
      | slug  | tdd-is-not-about-testing |
    And the post has tags "TDD" and "Clean Code"

  Scenario: Edit a post that no longer exists
    When Christian tries to update a non-existent post
    Then the response status is 404
    And the response contains "Post not found"

  Scenario: Reject edit with empty title
    Given a post "TDD Is Not About Testing" exists
    When Christian updates the post with an empty title
    Then the response status is 400
    And the response contains "Title is required"

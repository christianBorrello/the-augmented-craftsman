@epic4 @api
Feature: Read a single blog post
  As a reader
  I want to read the full content of a blog post
  So that I can learn from the author's insights

  # US-043: Read a single blog post

  Scenario: Read a full blog post
    Given a published post exists:
      | title   | TDD Is Not About Testing                            |
      | date    | 2026-03-05                                          |
      | tags    | TDD, Clean Code                                     |
      | image   | https://ik.imagekit.io/augmented/tdd-cycle.png      |
      | content | Technical content with a code example                |
    When a reader requests the post with slug "tdd-is-not-about-testing"
    Then the response status is 200
    And the response contains the full post with title, content, tags, image, and publishedAt

  Scenario: Post not found
    When a reader requests the post with slug "nonexistent-post"
    Then the response status is 404

  Scenario: Draft posts are not publicly accessible
    Given a draft post "Work In Progress" exists
    When a reader requests the post with slug "work-in-progress"
    Then the response status is 404

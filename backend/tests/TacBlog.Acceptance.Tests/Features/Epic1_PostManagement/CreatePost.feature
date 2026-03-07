@epic1 @api
Feature: Create blog post with full content
  As Christian (the author)
  I want to create blog posts with rich content, tags, and images
  So that I can share Software Craftsmanship insights

  # US-011: Create blog post with full content
  # US-012: Auto-generate URL slug from title

  Background:
    Given Christian is authenticated

  Scenario: Create a blog post with all fields
    Given tags "TDD" and "Clean Code" exist
    When Christian creates a post with:
      | title   | TDD Is Not About Testing                                         |
      | content | ## The Misconception\nTDD is about **design**, not verification. |
    And assigns tags "TDD" and "Clean Code" to the post
    Then the response status is 201
    And a draft post is created with slug "tdd-is-not-about-testing"
    And the post status is "Draft"
    And the post has tags "TDD" and "Clean Code"

  Scenario: Create a post with title and content only
    When Christian creates a post with:
      | title   | Quick Note on Refactoring |
      | content | Refactor on green.        |
    Then the response status is 201
    And a draft post is created with slug "quick-note-on-refactoring"
    And the post has no tags
    And the post has no featured image

  Scenario: Reject post with empty title
    When Christian creates a post with:
      | title   |              |
      | content | Some content |
    Then the response status is 400
    And the response contains "Title is required"

  Scenario: Reject post with duplicate slug
    Given a post exists with slug "tdd-is-not-about-testing"
    When Christian creates a post with:
      | title   | TDD Is Not About Testing |
      | content | Different content        |
    Then the response status is 409
    And the response contains "A post with this URL already exists"

  Scenario: Create a post with content containing code examples
    When Christian creates a post with content including a code example
    Then the post content is stored with the raw content preserved

  # --- Slug Generation (US-012) ---

  Scenario: Generate slug from simple title
    When Christian creates a post with title "Value Objects Are Not DTOs"
    Then the generated slug is "value-objects-are-not-dtos"

  Scenario: Generate slug from title with special characters
    When Christian creates a post with title "Why I Practice Object Calisthenics Daily!"
    Then the generated slug is "why-i-practice-object-calisthenics-daily"

  Scenario: Generate slug from title with multiple spaces
    When Christian creates a post with title "The  Walking   Skeleton  Pattern"
    Then the generated slug is "the-walking-skeleton-pattern"

  Scenario: Slug is always lowercase
    When Christian creates a post with title "SOLID Principles in Practice"
    Then the generated slug is "solid-principles-in-practice"

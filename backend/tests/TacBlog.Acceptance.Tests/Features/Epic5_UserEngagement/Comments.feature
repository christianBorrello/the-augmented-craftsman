@epic5 @engagement @comments
Feature: Post and view comments on blog posts
  As an authenticated blog reader
  I want to post comments on blog posts
  So that I can participate in discussions about the content

  # US-052: Post a Comment on a Blog Post
  # US-056: View Comments on a Post
  # Drives: CommentEndpoints -> PostComment/GetComments/GetCommentCount use cases

  Background:
    Given the blog system is running
    And Christian is authenticated

  # --- Happy Path ---

  Scenario: Authenticated reader posts a comment
    Given a reader session exists for "Tomasz Kowalski" via "GitHub"
    And a published post "Outside-In TDD" exists
    When the reader posts a comment "Great breakdown of the double loop!" on "outside-in-tdd"
    Then the response status is 201
    And the response contains comment text "Great breakdown of the double loop!"
    And the response contains display name "Tomasz Kowalski"
    And the response contains provider "GitHub"

  @ignore
  Scenario: Comment text is sanitized to prevent injection
    Given a reader session exists for "Maria Santos" via "Google"
    And a published post "Outside-In TDD" exists
    When the reader posts a comment "<script>alert('xss')</script>Hello" on "outside-in-tdd"
    Then the response status is 201
    And the comment text does not contain "<script>"

  Scenario: Reader sees existing comments on a post
    Given a published post "Outside-In TDD" exists
    And "outside-in-tdd" has a comment by "Tomasz Kowalski" via "GitHub" saying "Great post!"
    And "outside-in-tdd" has a comment by "Maria Santos" via "Google" saying "Very helpful!"
    When a reader requests comments for "outside-in-tdd"
    Then the response status is 200
    And the comments count is 2
    And the comments are in chronological order

  @ignore
  Scenario: Empty comments section returns zero count
    Given a published post "Outside-In TDD" exists
    When a reader requests the comment count for "outside-in-tdd"
    Then the response status is 200
    And the comment count is 0

  @ignore
  Scenario: Comment count matches number of comments
    Given a published post "Outside-In TDD" exists
    And "outside-in-tdd" has a comment by "Tomasz Kowalski" via "GitHub" saying "First!"
    And "outside-in-tdd" has a comment by "Maria Santos" via "Google" saying "Second!"
    And "outside-in-tdd" has a comment by "Tomasz Kowalski" via "GitHub" saying "Third!"
    When a reader requests the comment count for "outside-in-tdd"
    Then the comment count is 3

  # --- Error Path ---

  @ignore
  Scenario: Empty comment text is rejected
    Given a reader session exists for "Tomasz Kowalski" via "GitHub"
    And a published post "Outside-In TDD" exists
    When the reader posts a comment "" on "outside-in-tdd"
    Then the response status is 400

  @ignore
  Scenario: Comment exceeding 2000 characters is rejected
    Given a reader session exists for "Tomasz Kowalski" via "GitHub"
    And a published post "Outside-In TDD" exists
    When the reader posts a comment of 2001 characters on "outside-in-tdd"
    Then the response status is 400

  @ignore
  Scenario: Whitespace-only comment is rejected
    Given a reader session exists for "Tomasz Kowalski" via "GitHub"
    And a published post "Outside-In TDD" exists
    When the reader posts a comment "   " on "outside-in-tdd"
    Then the response status is 400

  @ignore
  Scenario: Unauthenticated reader cannot post a comment
    Given a published post "Outside-In TDD" exists
    When an unauthenticated reader posts a comment "Hello!" on "outside-in-tdd"
    Then the response status is 401

  @ignore
  Scenario: Comment on a non-existent post returns not found
    Given a reader session exists for "Tomasz Kowalski" via "GitHub"
    When the reader posts a comment "Hello!" on "non-existent-post"
    Then the response status is 404

  @ignore
  Scenario: Request comments for a non-existent post returns not found
    When a reader requests comments for "non-existent-post"
    Then the response status is 404

  # --- Boundary ---

  @ignore
  Scenario: Comment at exactly 2000 characters is accepted
    Given a reader session exists for "Tomasz Kowalski" via "GitHub"
    And a published post "Outside-In TDD" exists
    When the reader posts a comment of 2000 characters on "outside-in-tdd"
    Then the response status is 201

  @ignore
  Scenario: Comment at exactly 1 character is accepted
    Given a reader session exists for "Tomasz Kowalski" via "GitHub"
    And a published post "Outside-In TDD" exists
    When the reader posts a comment "X" on "outside-in-tdd"
    Then the response status is 201

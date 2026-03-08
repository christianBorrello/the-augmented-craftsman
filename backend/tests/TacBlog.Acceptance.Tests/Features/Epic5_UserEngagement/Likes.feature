@epic5 @engagement @likes
Feature: Like and unlike blog posts
  As a blog reader
  I want to like posts I find valuable
  So that I can signal appreciation and the author sees which content resonates

  # US-050: Like a Blog Post
  # Drives: LikeEndpoints -> LikePost/UnlikePost/GetLikeCount/CheckIfLiked use cases

  Background:
    Given the blog system is running
    And Christian is authenticated
    And a published post "TDD Is Not About Testing" exists

  # --- Happy Path ---

  @skip
  Scenario: Like is idempotent for the same visitor
    Given a visitor has liked "TDD Is Not About Testing"
    When the same visitor likes "TDD Is Not About Testing" again
    Then the like count for "TDD Is Not About Testing" is 1
    And the response indicates the post is liked

  Scenario: Unlike a previously liked post
    Given a visitor has liked "TDD Is Not About Testing"
    When the visitor unlikes "TDD Is Not About Testing"
    Then the like count for "TDD Is Not About Testing" is 0
    And the response indicates the post is not liked

  Scenario: Unlike is idempotent when no like exists
    Given a visitor has not previously liked "TDD Is Not About Testing"
    When the visitor unlikes "TDD Is Not About Testing"
    Then the like count for "TDD Is Not About Testing" is 0

  @skip
  Scenario: Check if visitor has liked a post
    Given a visitor has liked "TDD Is Not About Testing"
    When the visitor checks their like status for "TDD Is Not About Testing"
    Then the response indicates the post is liked
    And the like count is included in the response

  @skip
  Scenario: Check like status when visitor has not liked a post
    Given a visitor has not previously liked "TDD Is Not About Testing"
    When the visitor checks their like status for "TDD Is Not About Testing"
    Then the response indicates the post is not liked

  @skip
  Scenario: Multiple visitors can like the same post
    Given "TDD Is Not About Testing" has been liked by 3 visitors
    When a new visitor likes "TDD Is Not About Testing"
    Then the like count for "TDD Is Not About Testing" is 4

  # --- Error Path ---

  @skip
  Scenario: Like a non-existent post returns not found
    When a visitor likes a post with slug "nonexistent-post"
    Then the response status is 404
    And the response contains "Post not found"

  @skip
  Scenario: Unlike a non-existent post returns not found
    When a visitor unlikes a post with slug "nonexistent-post"
    Then the response status is 404
    And the response contains "Post not found"

  @skip
  Scenario: Check like status on non-existent post returns not found
    When a visitor checks their like status for a post with slug "nonexistent-post"
    Then the response status is 404
    And the response contains "Post not found"

  @skip
  Scenario: Get like count for non-existent post returns not found
    When a visitor requests the like count for a post with slug "nonexistent-post"
    Then the response status is 404
    And the response contains "Post not found"

  @skip
  Scenario: Like with invalid visitor identifier is rejected
    When a visitor with an empty identifier likes "TDD Is Not About Testing"
    Then the response status is 400

  # --- Boundary ---

  @skip @property
  Scenario: Like count is never negative regardless of unlike operations
    Given "TDD Is Not About Testing" has 0 likes
    When a visitor unlikes "TDD Is Not About Testing"
    Then the like count for "TDD Is Not About Testing" is 0

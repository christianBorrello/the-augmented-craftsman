@epic5 @walking-skeleton @engagement
Feature: Reader likes a post and sees updated count
  As a blog reader
  I want to like a post I enjoyed
  So that I can express appreciation and the author knows which posts resonate

  # Walking skeleton: proves the Engagement vertical slice works end-to-end
  # Exercises: LikeEndpoints -> LikePost use case -> ILikeRepository -> PostgreSQL -> response
  # US-050: Like a Blog Post

  Background:
    Given the blog system is running
    And Christian is authenticated
    And a published post "TDD Is Not About Testing" exists

  @walking_skeleton
  Scenario: Reader likes a post and the like count increments
    Given "TDD Is Not About Testing" has 0 likes
    And a visitor has not previously liked "TDD Is Not About Testing"
    When the visitor likes "TDD Is Not About Testing"
    Then the like is recorded successfully
    And the like count for "TDD Is Not About Testing" is 1

  @walking_skeleton
  Scenario: Reader checks like count for a post
    Given "TDD Is Not About Testing" has been liked by 3 visitors
    When a visitor requests the like count for "TDD Is Not About Testing"
    Then the like count for "TDD Is Not About Testing" is 3

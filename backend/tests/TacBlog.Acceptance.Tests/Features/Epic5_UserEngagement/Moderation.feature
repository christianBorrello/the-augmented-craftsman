@epic5 @engagement @moderation
Feature: Author moderates comments
  As the blog author (admin)
  I want to moderate comments on my posts
  So that I can remove spam and inappropriate content

  # US-053: Author Moderates Comments
  # Drives: CommentEndpoints -> DeleteComment, AdminCommentEndpoints -> ListAdminComments

  Background:
    Given the blog system is running
    And Christian is authenticated

  # --- Happy Path ---

  @ignore
  Scenario: Admin deletes a spam comment
    Given a published post "Outside-In TDD" exists
    And "outside-in-tdd" has a comment by "SpamBot" via "GitHub" saying "Buy cheap stuff!"
    And the admin is authenticated
    When the admin deletes the comment by "SpamBot" on "outside-in-tdd"
    Then the response status is 204
    And the comments count for "outside-in-tdd" is 0

  @ignore
  Scenario: Comment count updates after deletion
    Given a published post "Outside-In TDD" exists
    And "outside-in-tdd" has a comment by "Tomasz Kowalski" via "GitHub" saying "Great post!"
    And "outside-in-tdd" has a comment by "SpamBot" via "GitHub" saying "Buy cheap stuff!"
    And the admin is authenticated
    When the admin deletes the comment by "SpamBot" on "outside-in-tdd"
    Then the comments count for "outside-in-tdd" is 1

  @ignore
  Scenario: Admin lists all comments across posts
    Given a published post "Outside-In TDD" exists
    And a published post "Clean Architecture" exists
    And "outside-in-tdd" has a comment by "Tomasz Kowalski" via "GitHub" saying "Great post!"
    And "clean-architecture" has a comment by "Maria Santos" via "Google" saying "Insightful!"
    And the admin is authenticated
    When the admin lists all comments
    Then the response status is 200
    And the admin comments list contains 2 comments

  # --- Error Path ---

  @ignore
  Scenario: Delete non-existent comment returns not found
    Given a published post "Outside-In TDD" exists
    And the admin is authenticated
    When the admin deletes a non-existent comment on "outside-in-tdd"
    Then the response status is 404

  @ignore
  Scenario: Delete without admin authentication is rejected
    Given a published post "Outside-In TDD" exists
    And "outside-in-tdd" has a comment by "SpamBot" via "GitHub" saying "Buy cheap stuff!"
    When an unauthenticated user deletes the comment by "SpamBot" on "outside-in-tdd"
    Then the response status is 401

  @ignore
  Scenario: Reader session cannot delete comments
    Given a published post "Outside-In TDD" exists
    And "outside-in-tdd" has a comment by "SpamBot" via "GitHub" saying "Buy cheap stuff!"
    And a reader session exists for "Tomasz Kowalski" via "GitHub"
    When the reader deletes the comment by "SpamBot" on "outside-in-tdd"
    Then the response status is 401

  @ignore
  Scenario: Admin list without authentication is rejected
    When an unauthenticated user lists all comments
    Then the response status is 401

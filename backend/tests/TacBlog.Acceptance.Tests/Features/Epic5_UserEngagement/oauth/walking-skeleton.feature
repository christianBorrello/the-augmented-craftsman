@epic5 @oauth @walking_skeleton
Feature: OAuth Authentication Walking Skeleton
  As a blog reader
  I want to sign in with my social account
  So that I can participate in discussions

  # Walking skeleton: verifies the core OAuth flow works end-to-end
  # This validates that users can complete the full OAuth journey

  Background:
    Given the blog system is running

  @walking_skeleton
  Scenario: Reader initiates sign-in and is redirected to authorization page
    When a reader initiates sign-in with "github" for post "outside-in-tdd"
    Then the reader is redirected to the authorization page

  @walking_skeleton
  Scenario: Reader completes OAuth and can participate in discussions
    Given a valid authorization code is available for "github"
    When the OAuth callback is received with a valid authorization code for "github"
    Then a reader session is created
    And the reader can participate in discussions

  @walking_skeleton
  Scenario: Reader verifies session is authenticated
    Given an authenticated reader session exists
    When the reader checks their session status
    Then the session status indicates authenticated

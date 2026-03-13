@epic5 @oauth @configuration
Feature: OAuth Configuration Validation
  As a system administrator
  I want OAuth to validate provider configuration
  So that missing credentials are detected early and handled gracefully

  # AC1: OAuth Configuration Validation
  # Bug fix: OAuth returns 500 instead of 400 when credentials are missing

  Background:
    Given the blog system is running

  Scenario: Initiate OAuth with unsupported provider returns error
    When a reader initiates sign-in with "unsupported" for post "test-post"
    Then the request is rejected with bad request error
    And the error indicates the provider is not supported

  Scenario: Initiate OAuth with empty provider name returns error
    When a reader initiates sign-in with "" for post "test-post"
    Then the request is rejected with bad request error

  Scenario: OAuth callback with missing code parameter is handled gracefully
    When the OAuth callback is received without authorization code for "github"
    Then the reader is redirected back with an error indicator
    And no session is created

  Scenario: OAuth callback with empty state parameter is handled gracefully
    When the OAuth callback is received with empty state for "github"
    Then the reader is redirected back with an error indicator

  Scenario: OAuth callback with missing provider is handled gracefully
    When the OAuth callback is received for missing provider
    Then the request is rejected with bad request error

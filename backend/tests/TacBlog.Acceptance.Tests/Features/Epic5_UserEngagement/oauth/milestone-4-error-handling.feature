@epic5 @oauth @errors
Feature: OAuth Error Handling
  As a blog reader
  I want clear feedback when OAuth fails
  So that I understand what went wrong and can try again

  # AC4: Error Handling (unsupported provider, consent denied, provider errors)

  Background:
    Given the blog system is running

  Scenario: OAuth consent denied redirects to post without session
    When the OAuth callback is received with consent denied for "github"
    Then the reader is redirected back to the original post
    And no session is created
    And no error indicator is shown

  Scenario: OAuth provider error redirects to post with error
    When the OAuth callback is received with a provider error for "google"
    Then the reader is redirected back to the original post with an error indicator
    And no session is created

  Scenario: OAuth callback with invalid state parameter is rejected
    When the OAuth callback is received with an invalid state parameter for "github"
    Then the reader is redirected back to the original post with an error indicator
    And no session is created

  Scenario: OAuth token exchange failure is handled gracefully
    Given the OAuth provider is configured to fail token exchange
    When the OAuth callback is received with a valid authorization code for "github"
    Then the reader is redirected back with an error indicator
    And no session is created

  Scenario: OAuth user profile fetch failure is handled gracefully
    Given the OAuth provider is configured to fail user profile fetch
    When the OAuth callback is received with a valid authorization code for "google"
    Then the reader is redirected back with an error indicator
    And no session is created

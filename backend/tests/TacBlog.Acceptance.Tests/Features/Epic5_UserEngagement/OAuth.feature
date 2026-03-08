@epic5 @engagement @oauth
Feature: Sign in with social login to comment
  As a blog reader
  I want to sign in with my Google or GitHub account
  So that I can participate in discussions without creating a custom account

  # US-051: Sign In with Social Login to Comment
  # Drives: OAuthEndpoints -> HandleOAuthCallback/CheckSession/SignOut use cases

  Background:
    Given the blog system is running

  # --- Happy Path ---

  Scenario: Initiate GitHub sign-in redirects to provider
    When a reader initiates sign-in with "github" for post "outside-in-tdd"
    Then the reader is redirected to the GitHub authorization page

  Scenario: GitHub OAuth callback creates a reader session
    Given a reader has granted consent on GitHub as "Tomasz Kowalski"
    When the OAuth callback is received with a valid authorization code for "github"
    Then a reader session is created
    And the session contains display name "Tomasz Kowalski"
    And the session contains provider "GitHub"
    And the reader is redirected back to the original post

  Scenario: Google OAuth callback creates a reader session
    Given a reader has granted consent on Google as "Maria Santos"
    When the OAuth callback is received with a valid authorization code for "google"
    Then a reader session is created
    And the session contains display name "Maria Santos"
    And the session contains provider "Google"

  Scenario: Session check returns authenticated status
    Given a reader session exists for "Tomasz Kowalski" via "GitHub"
    When the reader checks their session status
    Then the session status indicates authenticated
    And the response contains display name "Tomasz Kowalski"
    And the response contains provider "GitHub"

  Scenario: Sign out clears the reader session
    Given a reader session exists for "Maria Santos" via "Google"
    When the reader signs out
    Then the session is cleared
    And checking session status shows not authenticated

  # --- Error Path ---

  @ignore
  Scenario: OAuth consent denied returns to post without error
    When the OAuth callback is received with consent denied for "github"
    Then the reader is redirected back to the original post
    And no session is created

  @ignore
  Scenario: OAuth provider error returns to post with error indicator
    When the OAuth callback is received with a provider error for "google"
    Then the reader is redirected back to the original post with an error indicator
    And no session is created

  @ignore
  Scenario: Session check with no session returns not authenticated
    When a reader with no session checks their session status
    Then the session status indicates not authenticated

  @ignore
  Scenario: Session check with expired session returns not authenticated
    Given a reader session exists for "Tomasz Kowalski" via "GitHub" that has expired
    When the reader checks their session status
    Then the session status indicates not authenticated

  @ignore
  Scenario: Initiate sign-in with unsupported provider is rejected
    When a reader initiates sign-in with "twitter" for post "outside-in-tdd"
    Then the response status is 400

  @ignore
  Scenario: OAuth callback with invalid state parameter is rejected
    When the OAuth callback is received with an invalid state parameter for "github"
    Then the reader is redirected back to the original post with an error indicator
    And no session is created

  Scenario: Sign out with no active session succeeds silently
    When a reader with no session signs out
    Then the response status is 204

@epic5 @oauth @google
Feature: Google OAuth Flow
  As a blog reader
  I want to sign in with my Google account
  So that I can comment on posts using my Google identity

  # AC3: Google OAuth Flow (initiate → callback → session)

  Background:
    Given the blog system is running

  Scenario: Initiate Google sign-in redirects to Google
    When a reader initiates sign-in with "google" for post "clean-architecture"
    Then the reader is redirected to the authorization page
    And the authorization URL contains Google

  Scenario: Google OAuth callback creates session with user profile
    Given a reader has granted consent on Google as "Maria Santos"
    When the OAuth callback is received with a valid authorization code for "google"
    Then a reader session is created
    And the session contains display name "Maria Santos"
    And the session contains provider "Google"

  Scenario: Google OAuth callback redirects to original post
    Given a reader has granted consent on Google as "Test User"
    When the OAuth callback is received with a valid authorization code for "google" with return URL "/blog/ddd-patterns"
    Then the reader is redirected back to "/blog/ddd-patterns"

  Scenario: Google OAuth callback shows reader's avatar in profile
    Given a reader has granted consent on Google as "John Doe" with avatar "https://googleusercontent.com/john.jpg"
    When the OAuth callback is received with a valid authorization code for "google"
    Then the reader's profile shows their avatar "https://googleusercontent.com/john.jpg"

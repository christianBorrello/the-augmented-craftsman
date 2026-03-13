@epic5 @oauth @github
Feature: GitHub OAuth Flow
  As a blog reader
  I want to sign in with my GitHub account
  So that I can comment on posts without creating a new account

  # AC2: GitHub OAuth Flow (initiate → callback → session)

  Background:
    Given the blog system is running

  Scenario: Initiate GitHub sign-in redirects to GitHub
    When a reader initiates sign-in with "github" for post "outside-in-tdd"
    Then the reader is redirected to the authorization page
    And the authorization URL contains GitHub

  Scenario: GitHub OAuth callback creates session with user profile
    Given a reader has granted consent on GitHub as "Tomasz Kowalski"
    When the OAuth callback is received with a valid authorization code for "github"
    Then a reader session is created
    And the session contains display name "Tomasz Kowalski"
    And the session contains provider "GitHub"

  Scenario: GitHub OAuth callback redirects to original post
    Given a reader has granted consent on GitHub as "Test User"
    When the OAuth callback is received with a valid authorization code for "github" with return URL "/blog/test-post"
    Then the reader is redirected back to "/blog/test-post"

  Scenario: GitHub OAuth callback with avatar includes avatar URL
    Given a reader has granted consent on GitHub as "Maria Santos" with avatar "https://github.com/maria.png"
    When the OAuth callback is received with a valid authorization code for "github"
    Then the session contains avatar URL "https://github.com/maria.png"

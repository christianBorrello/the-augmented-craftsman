@epic5 @oauth @session
Feature: OAuth Session Management
  As a blog reader
  I want my session to persist across visits
  So that I stay signed in without re-authenticating

  # AC5: Session Management

  Background:
    Given the blog system is running

  Scenario: Session check with valid session returns authenticated
    Given a reader session exists for "Tomasz Kowalski" via "GitHub"
    When the reader checks their session status
    Then the session status indicates authenticated
    And the response contains display name "Tomasz Kowalski"
    And the response contains provider "GitHub"

  Scenario: Session check with no session returns not authenticated
    When a reader with no session checks their session status
    Then the session status indicates not authenticated

  Scenario: Session check with expired session returns not authenticated
    Given a reader session exists for "Tomasz Kowalski" via "GitHub" that has expired
    When the reader checks their session status
    Then the session status indicates not authenticated

  Scenario: Sign out clears the reader session
    Given a reader session exists for "Maria Santos" via "Google"
    When the reader signs out
    Then the session is cleared
    And checking session status shows not authenticated

  Scenario: Sign out with no active session succeeds silently
    When a reader with no session signs out
    Then the operation succeeds

  Scenario: Session persists across requests within validity period
    Given a reader session exists for "Test Reader" via "GitHub"
    When the reader checks their session status
    Then the session status indicates authenticated
    When the reader checks their session status again
    Then the session status indicates authenticated

  Scenario: Session contains avatar URL when available
    Given a reader session exists for "Jane Doe" via "Google" with avatar "https://example.com/avatar.jpg"
    When the reader checks their session status
    Then the response contains avatar URL "https://example.com/avatar.jpg"

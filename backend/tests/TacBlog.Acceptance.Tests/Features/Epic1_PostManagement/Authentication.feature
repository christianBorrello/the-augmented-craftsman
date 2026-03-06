@epic1 @skip @api
Feature: Authenticate as admin
  As Christian (the author)
  I want to log in to the admin area
  So that I can manage blog content securely

  # US-010: Authenticate as admin

  Scenario: Successful login with valid credentials
    When Christian logs in with email "christian.borrello@live.it" and password "valid-password"
    Then the response status is 200
    And the response contains a valid authentication token

  Scenario: Failed login with incorrect password
    When Christian logs in with email "christian.borrello@live.it" and password "wrong-password"
    Then the response status is 401
    And the response contains "Invalid email or password"
    And no authentication token is issued

  Scenario: Failed login with unknown email
    When Christian logs in with email "unknown@example.com" and password "any-password"
    Then the response status is 401
    And the response contains "Invalid email or password"
    And no authentication token is issued

  Scenario: Account locked after 5 failed attempts
    Given Christian has failed login 5 times in the last 10 minutes
    When Christian logs in with email "christian.borrello@live.it" and password "any-password"
    Then the response status is 429
    And the response contains "Too many attempts. Try again in 15 minutes."

  Scenario: Unauthenticated access to protected features is rejected
    Given no authentication is provided
    When a POST request is sent to "/api/posts" with:
      | title   | Unauthorized Post |
      | content | Should be blocked |
    Then the response status is 401

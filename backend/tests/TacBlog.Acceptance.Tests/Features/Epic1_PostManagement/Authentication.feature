@epic1 @api
Feature: Authenticate as admin
  As Christian (the author)
  I want to access admin endpoints with an API key
  So that I can manage blog content securely without a login UI

  # US-010: Authenticate as admin

  Scenario: API key grants access to protected endpoints
    Given Christian provides the correct API key
    Then Christian is authenticated as admin

  Scenario: Unauthenticated access to protected features is rejected
    Given no authentication is provided
    When a POST request is sent to "/api/posts" with:
      | title   | Unauthorized Post |
      | content | Should be blocked |
    Then the response status is 401

  Scenario: Wrong API key is rejected
    When Christian provides a wrong API key
    When a POST request is sent to "/api/posts" with:
      | title   | Unauthorized Post |
      | content | Should be blocked |
    Then the response status is 401

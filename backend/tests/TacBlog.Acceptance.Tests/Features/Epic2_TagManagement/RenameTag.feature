@epic2 @skip @api
Feature: Rename a tag
  As Christian (the author)
  I want to rename tags
  So that I can improve naming without losing post associations

  # US-022: Rename a tag

  Background:
    Given Christian is authenticated

  Scenario: Rename a tag
    Given a tag "TDD" exists linked to 5 posts
    When Christian renames the tag "TDD" to "Test-Driven Development"
    Then the response status is 200
    And the tag name is updated to "Test-Driven Development"
    And the tag slug is updated to "test-driven-development"
    And all 5 linked posts now show "Test-Driven Development"

  Scenario: Reject rename to existing tag name
    Given tags "TDD" and "Clean Code" exist
    When Christian renames "TDD" to "Clean Code"
    Then the response status is 409
    And the response contains "A tag named 'Clean Code' already exists"

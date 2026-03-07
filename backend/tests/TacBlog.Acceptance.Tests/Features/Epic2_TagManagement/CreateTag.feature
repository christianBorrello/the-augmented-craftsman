@epic2 @api
Feature: Create a new tag
  As Christian (the author)
  I want to create tags for organizing posts
  So that readers can browse content by topic

  # US-020: Create a new tag

  Background:
    Given Christian is authenticated

  Scenario: Create a new tag
    When Christian creates a tag with name "Refactoring"
    Then the response status is 201
    And the tag "Refactoring" is created with slug "refactoring"

  Scenario: Reject duplicate tag name
    Given a tag "TDD" already exists
    When Christian creates a tag with name "TDD"
    Then the response status is 409
    And the response contains "A tag named 'TDD' already exists"

  Scenario: Reject empty tag name
    When Christian creates a tag with an empty name
    Then the response status is 400
    And the response contains "Tag name is required"

  Scenario: Reject tag name exceeding maximum length
    When Christian creates a tag with a name of 51 characters
    Then the response status is 400
    And the response contains "Tag name must be 50 characters or fewer"

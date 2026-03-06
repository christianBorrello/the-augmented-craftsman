@epic2 @skip @api
Feature: Delete a tag
  As Christian (the author)
  I want to delete tags I no longer need
  So that I keep the taxonomy clean

  # US-023: Delete a tag

  Background:
    Given Christian is authenticated

  Scenario: Delete a tag with linked posts
    Given a tag "Legacy" exists linked to 3 posts
    When Christian deletes the tag "Legacy"
    Then the response status is 204
    And the tag "Legacy" is removed
    And the 3 previously linked posts no longer have the tag "Legacy"
    And the 3 posts still exist

  Scenario: Delete a tag with no linked posts
    Given a tag "Unused" exists linked to 0 posts
    When Christian deletes the tag "Unused"
    Then the response status is 204
    And the tag "Unused" is removed

  Scenario: Delete a non-existent tag
    When Christian tries to delete a non-existent tag
    Then the response status is 404
    And the response contains "Tag not found"

@epic1 @skip @api
Feature: Delete a blog post
  As Christian (the author)
  I want to delete posts I no longer need
  So that I keep the blog clean

  # US-016: Delete a blog post

  Background:
    Given Christian is authenticated

  Scenario: Delete a post
    Given a post "Old Draft Post" exists
    When Christian deletes the post "Old Draft Post"
    Then the response status is 204
    And "Old Draft Post" no longer appears in the post list

  Scenario: Delete a post with tags preserves the tags
    Given a post "Old Draft Post" exists with tags "TDD" and "Clean Code"
    When Christian deletes the post "Old Draft Post"
    Then the response status is 204
    And the tags "TDD" and "Clean Code" still exist

  Scenario: Delete a non-existent post
    When Christian tries to delete a non-existent post
    Then the response status is 404
    And the response contains "Post not found"

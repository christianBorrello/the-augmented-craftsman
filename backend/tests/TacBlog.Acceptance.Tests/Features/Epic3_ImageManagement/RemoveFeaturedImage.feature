@epic3 @api
Feature: Remove featured image from a blog post
  As Christian (the author)
  I want to remove a featured image from a post
  So that I can update the post's visual presentation

  # US-032: Remove featured image from a blog post

  Background:
    Given Christian is authenticated

  Scenario: Remove featured image from a post
    Given a draft post "TDD Is Not About Testing" exists with a featured image
    When Christian removes the featured image from the post
    Then the response status is 200
    And the post has no featured image

  Scenario: Remove image from a post that has no image
    Given a draft post "TDD Is Not About Testing" exists without a featured image
    When Christian removes the featured image from the post
    Then the response status is 200
    And the post has no featured image

  Scenario: Remove featured image from a non-existent post
    When Christian removes the featured image from a non-existent post
    Then the response status is 404
    And the response contains "Post not found"

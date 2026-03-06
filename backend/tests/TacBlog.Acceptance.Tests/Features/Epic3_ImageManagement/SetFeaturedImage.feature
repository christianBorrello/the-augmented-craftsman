@epic3 @skip @api
Feature: Set featured image for a blog post
  As Christian (the author)
  I want to set a featured image on my posts
  So that posts have a visual identity

  # US-031: Set featured image for a blog post

  Background:
    Given Christian is authenticated

  Scenario: Set featured image on a post
    Given a draft post "TDD Is Not About Testing" exists
    And an image has been uploaded with URL "https://ik.imagekit.io/augmented/tdd-cycle.png"
    When Christian sets the featured image on the post
    Then the response status is 200
    And the post has the featured image URL

  Scenario: Post is saveable without a featured image
    When Christian creates a post with title "Quick Note on Refactoring" and no image
    Then the response status is 201
    And the post has no featured image

  Scenario: Reject invalid image URL format
    Given a draft post "TDD Is Not About Testing" exists
    When Christian sets the featured image URL to "not-a-valid-url"
    Then the response status is 400
    And the response contains "Invalid image URL"

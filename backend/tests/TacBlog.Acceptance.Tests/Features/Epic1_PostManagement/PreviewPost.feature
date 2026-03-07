@epic1 @api
Feature: Preview blog post before publishing
  As Christian (the author)
  I want to preview how my post will look
  So that I can verify formatting before publishing

  # US-013: Preview blog post before publishing

  Background:
    Given Christian is authenticated

  Scenario: Preview renders formatted content with code examples
    Given a draft post "TDD Is Not About Testing" exists with content containing a code example
    When Christian requests a preview of the post
    Then the preview shows formatted content with highlighted code examples

  Scenario: Preview shows featured image
    Given a draft post "TDD Is Not About Testing" exists with a featured image
    When Christian requests a preview of the post
    Then the preview displays the featured image

  Scenario: Preview shows tags
    Given a draft post "TDD Is Not About Testing" exists with tags "TDD" and "Clean Code"
    When Christian requests a preview of the post
    Then the preview displays tag badges for "TDD" and "Clean Code"

  Scenario: Preview a post that no longer exists
    When Christian requests a preview of a non-existent post
    Then the response status is 404
    And the response contains "Post not found"

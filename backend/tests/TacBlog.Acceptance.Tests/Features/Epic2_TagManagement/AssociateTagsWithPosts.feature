@epic2 @api
Feature: Associate and disassociate tags with posts
  As Christian (the author)
  I want to add and remove tags from posts
  So that I can categorize content accurately

  # US-024: Associate and disassociate tags with posts

  Background:
    Given Christian is authenticated

  Scenario: Add existing tags to a post
    Given a draft post "TDD Is Not About Testing" exists
    And tags "TDD", "Clean Code", and "XP" exist
    When Christian adds tags "TDD" and "Clean Code" to the post
    Then the post has tags "TDD" and "Clean Code"

  Scenario: Create a new tag while tagging a post
    Given a draft post "TDD Is Not About Testing" exists
    And the tag "Software Design" does not exist
    When Christian adds a new tag "Software Design" to the post
    Then the tag "Software Design" is created with slug "software-design"
    And the tag is associated with the post

  Scenario: Remove a tag from a post
    Given a draft post "TDD Is Not About Testing" exists with tags "TDD" and "Clean Code"
    When Christian removes the tag "Clean Code" from the post
    Then the post has only the tag "TDD"
    And the tag "Clean Code" still exists

  Scenario: Post is valid after removing all tags
    Given a draft post "TDD Is Not About Testing" exists with tags "TDD"
    When Christian removes the tag "TDD" from the post
    Then the post has no tags
    And the post is still accessible

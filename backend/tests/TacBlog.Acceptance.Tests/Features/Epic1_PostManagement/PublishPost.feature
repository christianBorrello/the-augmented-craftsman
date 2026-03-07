@epic1 @api
Feature: Publish a draft blog post
  As Christian (the author)
  I want to publish draft posts
  So that readers can see my content on the public site

  # US-014: Publish a draft blog post

  Background:
    Given Christian is authenticated

  Scenario: Publish a draft post
    Given a draft post "TDD Is Not About Testing" exists
    When Christian publishes the post
    Then the response status is 200
    And the post status changes to "Published"
    And the post has a publish date of today

  Scenario: Published post retains its content
    Given a draft post "The Walking Skeleton Pattern" exists with tags "Architecture" and content about walking skeletons
    When Christian publishes the post
    Then the post title is "The Walking Skeleton Pattern"
    And the post tags include "Architecture"
    And the post content is unchanged

  Scenario: Cannot publish an already published post
    Given a published post "TDD Is Not About Testing" exists
    When Christian attempts to publish the post again
    Then the response status is 409
    And the response contains "Post is already published"

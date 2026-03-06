@epic4 @skip @api
Feature: Browse all posts
  As a reader
  I want to browse all published blog posts
  So that I can find content that interests me

  # US-041: Browse all posts

  Scenario: List all published posts
    Given 5 published posts and 2 draft posts exist
    When a reader requests all published posts
    Then the response status is 200
    And only the 5 published posts are returned
    And draft posts are not included

  Scenario: Posts include summary fields
    Given a published post "TDD Is Not About Testing" exists with tags "TDD" and "Clean Code"
    When a reader requests all published posts
    Then each post contains title, slug, publishedAt, tags, and excerpt
    And post content is not included in the list response

@epic0 @walking-skeleton @smoke @api
Feature: Create and retrieve a blog post
  As the development team
  I want to create and retrieve posts end-to-end
  So that I know the system works end-to-end

  # US-001: Create a blog post
  # US-002: Retrieve a blog post

  Background:
    Given the blog system is running

  Scenario: Create a blog post with title and content
    When a POST request is sent to "/api/posts" with:
      | title   | Hello World                 |
      | content | This is the **first** post. |
    Then the response status is 201
    And the response contains a post with slug "hello-world"
    And the post can be retrieved

  Scenario: Retrieve an existing post by slug
    Given a post exists with slug "hello-world" and title "Hello World"
    When a GET request is sent to "/api/posts/hello-world"
    Then the response status is 200
    And the response contains:
      | title   | Hello World                 |
      | slug    | hello-world                 |
      | content | This is the **first** post. |

  Scenario: Return 404 for non-existent slug
    Given no post exists with slug "nonexistent-post"
    When a GET request is sent to "/api/posts/nonexistent-post"
    Then the response status is 404

  Scenario: Reject a post with empty title
    When a POST request is sent to "/api/posts" with:
      | title   |                    |
      | content | Some content here. |
    Then the response status is 400
    And the response contains "Title is required"

  Scenario: Generate slug from title with special characters
    When a POST request is sent to "/api/posts" with:
      | title   | TDD Is Not About Testing! |
      | content | Design, not verification. |
    Then the response contains a post with slug "tdd-is-not-about-testing"

  Scenario: Create and retrieve a post end-to-end
    When a POST request is sent to "/api/posts" with:
      | title   | Hello World                 |
      | content | This is the **first** post. |
    Then the response status is 201
    And the response contains a post with slug "hello-world"
    When a GET request is sent to "/api/posts/hello-world"
    Then the response status is 200
    And the response contains title "Hello World"
    And the response contains content "This is the **first** post."

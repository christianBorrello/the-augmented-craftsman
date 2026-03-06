@epic3 @skip @api
Feature: Upload an image for a blog post
  As Christian (the author)
  I want to upload images for my posts
  So that I can illustrate technical concepts visually

  # US-030: Upload an image

  Background:
    Given Christian is authenticated

  Scenario: Upload a valid image
    When Christian uploads an image "tdd-cycle.png"
    Then the response status is 201
    And the response contains a URL for the uploaded image

  Scenario: Reject upload of non-image file
    When Christian uploads a file "document.pdf" as an image
    Then the response status is 400
    And the response contains "Only image files are allowed"

  Scenario: Image upload fails when storage service is unavailable
    Given the image storage service is temporarily unavailable
    When Christian uploads an image "tdd-cycle.png"
    Then the response status is 503
    And the response contains "Upload failed. Try again."

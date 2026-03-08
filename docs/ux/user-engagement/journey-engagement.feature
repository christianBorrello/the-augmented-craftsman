Feature: User Engagement — Likes, Comments & Sharing
  As a reader of The Augmented Craftsman blog
  I want to like, comment on, and share posts
  So that I can express appreciation, engage in discussion, and spread useful content

  # =====================================================================
  # LIKES — Anonymous, one per post, cookie/localStorage persisted
  # =====================================================================

  Scenario: Reader likes a post
    Given Ana Ferreira is reading "TDD Is Not About Testing" which has 12 likes
    And Ana has not previously liked this post
    When Ana taps the heart icon
    Then the heart icon fills with color
    And the like count displays 13
    And a brief scale animation plays on the heart icon
    And the visitor_liked flag for "tdd-is-not-about-testing" is stored in localStorage

  Scenario: Like persists on return visit
    Given Ana Ferreira previously liked "TDD Is Not About Testing"
    And the visitor_liked flag for "tdd-is-not-about-testing" exists in localStorage
    When Ana visits "TDD Is Not About Testing" again
    Then the heart icon is displayed as filled
    And the current like count is shown

  Scenario: Reader cannot like the same post twice
    Given Ana Ferreira already liked "TDD Is Not About Testing"
    When Ana taps the heart icon again
    Then the like count does not change
    And the heart icon remains filled

  Scenario: Like uses optimistic UI update
    Given Ana Ferreira is reading "Value Objects in Practice" which has 5 likes
    When Ana taps the heart icon
    Then the heart icon fills immediately without waiting for the API response
    And the count displays 6 immediately
    And a POST request is sent to the API in the background

  Scenario: Like survives network failure gracefully
    Given Ana Ferreira is reading "Clean Architecture Basics" which has 8 likes
    And the API is temporarily unreachable
    When Ana taps the heart icon
    Then the heart icon fills optimistically
    And the count displays 9
    And on the next page load the heart state reconciles with the server

  Scenario: Like resets when localStorage is cleared
    Given Ana Ferreira previously liked "TDD Is Not About Testing"
    And Ana clears her browser localStorage
    When Ana visits "TDD Is Not About Testing"
    Then the heart icon is displayed as outlined (not filled)
    And Ana can like the post again

  Scenario: Like button hidden when JavaScript is disabled
    Given a reader visits "TDD Is Not About Testing" with JavaScript disabled
    Then the like button is not visible
    And the post content is fully readable

  # =====================================================================
  # COMMENTS — Social login required (Google/GitHub OAuth)
  # =====================================================================

  Scenario: Reader sees existing comments
    Given "Outside-In TDD" has 3 comments
    When Tomasz Kowalski scrolls to the comments section
    Then he sees 3 comments displayed
    And each comment shows the commenter's avatar, display name, provider badge, timestamp, and text
    And the section header reads "Comments (3)"

  Scenario: Empty comments section shows invitation
    Given "New Post About DDD" has no comments
    When Maria Santos scrolls to the comments section
    Then she sees the message "No comments yet. Be the first to share your thoughts."

  Scenario: Reader signs in with GitHub to comment
    Given Tomasz Kowalski is not signed in
    And he is viewing "Outside-In TDD"
    When Tomasz clicks "Sign in with GitHub"
    Then he is redirected to the GitHub OAuth consent screen
    And after granting consent he is redirected back to "Outside-In TDD"
    And the comment form shows "Signed in as Tomasz Kowalski (GitHub)"
    And a comment textarea is displayed

  Scenario: Reader signs in with Google to comment
    Given Maria Santos is not signed in
    And she is viewing "Clean Architecture Basics"
    When Maria clicks "Sign in with Google"
    Then she is redirected to the Google OAuth consent screen
    And after granting consent she is redirected back to "Clean Architecture Basics"
    And the comment form shows "Signed in as Maria Santos (Google)"
    And a comment textarea is displayed

  Scenario: Reader posts a comment
    Given Tomasz Kowalski is signed in via GitHub
    And he is viewing "Outside-In TDD"
    When Tomasz types "Great breakdown of the double loop!" in the comment textarea
    And clicks "Post Comment"
    Then his comment appears in the comments list
    And the comment shows his GitHub avatar, "Tomasz Kowalski", "via GitHub", and "just now"
    And a toast message "Comment posted." is displayed
    And the comment count increments by 1

  Scenario: Reader cannot post an empty comment
    Given Maria Santos is signed in via Google
    And she is viewing "Value Objects in Practice"
    Then the "Post Comment" button is disabled when the comment textarea is empty

  Scenario: Comment character limit enforced
    Given Tomasz Kowalski is signed in via GitHub
    And he is writing a comment on "Outside-In TDD"
    When his comment reaches 1800 characters
    Then a character counter appears showing remaining characters
    And at 2000 characters the message "Comment is too long (2000 character limit)" is shown
    And the "Post Comment" button is disabled

  Scenario: Comment text preserved on network failure
    Given Maria Santos is signed in via Google
    And she has typed a comment on "Clean Architecture Basics"
    And the network connection drops
    When Maria clicks "Post Comment"
    Then the message "Could not post your comment. Check your connection and try again." is shown
    And her comment text is preserved in the textarea

  Scenario: Session expired during comment writing
    Given Tomasz Kowalski was signed in via GitHub
    And his session has expired while writing a comment
    When Tomasz clicks "Post Comment"
    Then the message "Your session has expired. Please sign in again." is shown
    And his comment text is preserved in localStorage
    And after signing in again the textarea is pre-populated with his draft

  Scenario: OAuth flow cancelled by user
    Given Ana Ferreira is not signed in
    When Ana clicks "Sign in with GitHub"
    And denies consent on the GitHub OAuth screen
    Then she is returned to the post page
    And the sign-in prompt is still displayed
    And no error message is shown

  Scenario: OAuth flow fails due to provider error
    Given Maria Santos is not signed in
    When Maria clicks "Sign in with Google"
    And the Google OAuth service returns an error
    Then Maria is returned to the post page
    And the message "Sign in failed. Please try again." is displayed

  Scenario: Comment session persists on return visit
    Given Tomasz Kowalski signed in via GitHub yesterday
    When Tomasz visits "Outside-In TDD" today
    Then the comment form is ready (no sign-in required)
    And it shows "Signed in as Tomasz Kowalski (GitHub)"

  Scenario: Reader signs out
    Given Maria Santos is signed in via Google
    When Maria clicks "Sign out"
    Then the sign-in prompt replaces the comment form
    And she sees "Sign in with Google" and "Sign in with GitHub" buttons

  # --- Author Moderation ---

  Scenario: Author deletes an inappropriate comment
    Given Christian is authenticated as admin
    And "Outside-In TDD" has a spam comment by "SpamBot" reading "Buy cheap watches"
    When Christian views comments in the admin panel
    And clicks delete on the spam comment
    And confirms the deletion
    Then the comment is removed from the public post view
    And the comment count for "Outside-In TDD" decrements by 1

  # =====================================================================
  # SHARING — Web Share API with clipboard fallback
  # =====================================================================

  Scenario: Reader shares a post on mobile via native share sheet
    Given Tomasz Kowalski is reading "TDD Myths" on his mobile phone
    And his browser supports the Web Share API
    When Tomasz taps the share icon
    Then the native OS share sheet appears
    And the share sheet is pre-filled with the title "TDD Myths" and the post URL

  Scenario: Reader shares a post on desktop via clipboard
    Given Ana Ferreira is reading "Value Objects in Practice" on her desktop
    And her browser does not support the Web Share API
    When Ana clicks the share icon
    Then the post URL is copied to her clipboard
    And a toast message "Link copied!" appears
    And the toast auto-dismisses after 3 seconds

  Scenario: Share falls back to clipboard when Web Share API fails
    Given a reader is viewing "Clean Architecture Basics" on a browser
    And the Web Share API call fails with a browser error
    When the reader clicks the share icon
    Then the post URL is silently copied to the clipboard
    And a toast message "Link copied!" appears

  Scenario: Share fallback for browsers without clipboard API
    Given a reader is viewing "TDD Is Not About Testing"
    And their browser supports neither Web Share API nor Clipboard API
    When the reader clicks the share icon
    Then a small popover appears with the post URL as selectable text
    And the reader can manually select and copy the URL

  Scenario: Share button hidden when JavaScript is disabled
    Given a reader visits "TDD Is Not About Testing" with JavaScript disabled
    Then the share button is not visible
    And the post URL is available in the browser address bar

  Scenario: Shared post URL displays correct Open Graph metadata
    Given "Value Objects in Practice" has been published with a featured image
    When the post URL is shared on a social platform
    Then the platform displays the Open Graph title "Value Objects in Practice"
    And the platform displays the Open Graph description
    And the platform displays the featured image as the Open Graph image

  Scenario: Shared post without featured image has fallback OG image
    Given "TDD Myths" has been published without a featured image
    When the post URL is shared on a social platform
    Then the platform displays a default blog Open Graph image

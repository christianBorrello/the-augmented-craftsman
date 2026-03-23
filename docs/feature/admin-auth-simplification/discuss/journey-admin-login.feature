# Feature: Admin Authentication via Email/Password
#
# Context: The Augmented Craftsman blog — single-admin platform.
# Christian (sole author) authenticates to manage blog posts.
# This feature REPLACES the former OAuth 4-step flow with a
# direct email/password login backed by the existing LoginHandler.
#
# Platform: Web (Astro frontend + .NET 10 Minimal API)
# Persona: Christian — sole author and administrator
# Journey: admin-auth-simplification

Feature: Admin Login with Email and Password

  Background:
    Given the admin credentials are configured as:
      | field    | value                                    |
      | email    | christian@theaugmentedcraftsman.dev      |
      | password | (bcrypt hash stored in appsettings)      |
    And the admin JWT secret is configured in IAdminSettings.JwtSecret
    And no OAuth admin application exists in the system

  # ---------------------------------------------------------------------------
  # STEP 1 — Login Page Rendering
  # ---------------------------------------------------------------------------

  Scenario: Admin login page renders with email and password fields
    Given Christian navigates to /admin/login
    When the page loads
    Then a form with an email field and a password field is visible
    And a "Sign In" button is visible
    And no GitHub OAuth button or link is present
    And no redirect to an external OAuth provider occurs

  Scenario: Admin login page is accessible by keyboard
    Given Christian navigates to /admin/login
    When Christian navigates the page using only the keyboard (Tab key)
    Then the email field receives focus first
    And tabbing moves focus to the password field
    And tabbing moves focus to the "Sign In" button
    And pressing Enter on the button submits the form

  # ---------------------------------------------------------------------------
  # STEP 2 / STEP 3a — Successful Authentication
  # ---------------------------------------------------------------------------

  Scenario: Correct credentials produce an admin JWT and redirect to dashboard
    Given Christian is on the admin login page
    And the account is not locked out
    When Christian enters his correct email and password
    And clicks "Sign In"
    Then the API responds with a success status
    And a JWT token is issued
    And the JWT token contains the claim role = "admin"
    And the JWT token is signed with IAdminSettings.JwtSecret
    And the JWT token has a lifetime of 480 minutes
    And the browser is redirected to /admin/dashboard

  Scenario: Admin dashboard is accessible with a valid admin JWT
    Given Christian has successfully authenticated and holds a valid admin JWT
    When Christian accesses /admin/dashboard
    Then the admin dashboard renders without re-authentication
    And Christian can navigate to post management features

  Scenario: Admin session persists for the full JWT lifetime without re-login
    Given Christian authenticated 7 hours ago (420 minutes)
    And the JWT lifetime is configured to 480 minutes
    When Christian makes a request to an admin endpoint
    Then the request is accepted (token has not expired)
    And Christian does not need to re-authenticate

  Scenario: Admin session requires re-login after JWT expiry
    Given Christian authenticated 9 hours ago (540 minutes)
    And the JWT lifetime is configured to 480 minutes
    When Christian attempts to access /admin/dashboard
    Then the request is rejected (token expired)
    And Christian is redirected to /admin/login

  # ---------------------------------------------------------------------------
  # STEP 2 / STEP 3b — Authentication Failure
  # ---------------------------------------------------------------------------

  Scenario: Wrong password returns a generic error message
    Given Christian is on the admin login page
    And the account is not locked out
    When Christian enters his correct email and an incorrect password
    And clicks "Sign In"
    Then the API responds with a 401 status
    And the error message displayed is "Invalid email or password"
    And the error message does not reveal whether the email exists
    And the failed attempt is recorded in FailureTracker
    And Christian remains on the login page

  Scenario: Wrong email returns the same generic error message
    Given Christian is on the admin login page
    And the account is not locked out
    When Christian enters an incorrect email address and any password
    And clicks "Sign In"
    Then the API responds with a 401 status
    And the error message displayed is "Invalid email or password"
    And no information about the correct email is disclosed

  # ---------------------------------------------------------------------------
  # STEP 3b / STEP 4b — Brute Force Protection
  # ---------------------------------------------------------------------------

  Scenario: Fourth failed attempt warns about one remaining attempt
    Given Christian has failed 4 consecutive login attempts
    When Christian views the login form
    Then a warning is visible: "1 attempt remaining before account lockout"

  Scenario: Fifth consecutive failed attempt triggers a 15-minute lockout
    Given Christian has failed 4 consecutive login attempts
    When Christian attempts to log in with incorrect credentials again
    Then the account is locked for 15 minutes
    And the API responds with a 429 (or 401 lockout) status
    And the response indicates the account is locked and states the lockout duration
    And no password comparison is performed during the lockout check

  Scenario: Locked account rejects login attempts without checking password
    Given the account is locked out due to 5 failed attempts
    When Christian submits any credentials (even the correct ones)
    Then the request is rejected immediately
    And no bcrypt comparison is performed
    And the error message indicates the account is temporarily locked

  Scenario: Login form is disabled and shows lockout message during lockout
    Given the account is currently locked out
    When Christian navigates to /admin/login
    Then the "Sign In" button is disabled
    And a message is displayed: "Too many failed attempts. Try again in 15 minutes."
    And the remaining lockout time is shown (if technically feasible)

  Scenario: Lockout clears after 15 minutes and login succeeds
    Given Christian was locked out 15 minutes ago
    And the lockout period has elapsed
    When Christian enters his correct email and password
    And clicks "Sign In"
    Then the login succeeds
    And a valid admin JWT is issued
    And the browser redirects to /admin/dashboard

  # ---------------------------------------------------------------------------
  # CLEANUP — OAuth artifacts removed
  # ---------------------------------------------------------------------------

  Scenario: Former OAuth admin endpoints no longer exist
    Given the admin-auth-simplification feature is deployed
    When a request is made to /admin/oauth/initiate
    Then the response is 404 (endpoint removed)

  Scenario: Former OAuth admin callback no longer exists
    Given the admin-auth-simplification feature is deployed
    When a request is made to /admin/oauth/callback
    Then the response is 404 (endpoint removed)

  Scenario: Reader OAuth flow remains unaffected
    Given a reader navigates to the reader OAuth login page
    When the reader initiates the OAuth flow
    Then the reader OAuth flow proceeds normally
    And no admin authentication changes affect the reader flow

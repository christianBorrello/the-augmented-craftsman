# OAuth Authentication - User Stories

## Feature ID: oauth-authentication

<!-- markdownlint-disable MD024 -->

## US-OAuth-001: Fix OAuth Configuration Loading

### Problem
The blog's OAuth authentication returns HTTP 500 errors because required OAuth settings (GitHub Client ID/Secret) are not validated at startup. When environment variables are not set, empty strings are used, causing API calls to fail with unhandled exceptions.

### Who
- **System Administrator** who configures OAuth credentials via environment variables
- **Blog Reader** who wants to sign in with GitHub or Google to comment

### Solution
Add startup validation for OAuth settings with clear error messages, and ensure the ProductionOAuthClient returns proper error results instead of throwing exceptions.

### Domain Examples

#### Example 1: Missing GitHub Credentials — Developer Deploys Without Configuration
- **Given**: Developer deploys to production without setting `OAuth__GitHub__ClientId` environment variable
- **When**: First reader attempts GitHub sign-in
- **Then**: System returns HTTP 400 with message "GitHub OAuth not configured" instead of 500
- **And**: Error is logged at startup with clear instructions

#### Example 2: Valid Configuration — Reader Signs In
- **Given**: System administrator has set `OAuth__GitHub__ClientId` and `OAuth__GitHub__ClientSecret`
- **When**: Reader clicks "Sign in with GitHub" on blog post
- **Then**: Reader redirected to GitHub authorization page
- **And**: After approval, reader returns as authenticated user

### UAT Scenarios (BDD)

#### Scenario: GitHub OAuth not configured returns proper error
Given the system administrator has not configured GitHub OAuth credentials
When a reader initiates sign-in with GitHub
Then the response status is 400
And the response contains error message "GitHub OAuth not configured"

#### Scenario: Google OAuth not configured returns proper error
Given the system administrator has not configured Google OAuth credentials
When a reader initiates sign-in with Google
Then the response status is 400
And the response contains error message "Google OAuth not configured"

#### Scenario: All OAuth providers configured
Given GitHub and Google OAuth credentials are configured in environment variables
When the system starts
Then no validation errors are logged
And OAuth endpoints are available

### Acceptance Criteria
- [ ] OAuth settings validated at application startup
- [ ] Missing required settings cause startup failure with clear message
- [ ] Unsupported provider returns HTTP 400 (not 500)
- [ ] Error responses use standard ProblemDetails format

### Outcome KPIs
- **Who**: Blog readers attempting OAuth sign-in
- **Does what**: Successfully authenticate via GitHub or Google
- **By how much**: Reduce OAuth 500 errors from 100% to 0%
- **Measured by**: HTTP status code monitoring on /api/auth/oauth/* endpoints
- **Baseline**: Currently all OAuth requests return 500

---

## US-OAuth-002: Handle OAuth Callback and Create Session

### Problem
When OAuth providers redirect back with an authorization code, the callback endpoint fails to exchange the code for a token and create a reader session. This prevents authenticated users from commenting.

### Who
- **Blog Reader** who has authorized GitHub or Google access
- **Wants to**: Participate in discussions by signing in with existing social account

### Solution
Implement the complete OAuth callback flow: exchange authorization code for access token, retrieve user profile, create session in database, and set session cookie.

### Domain Examples

#### Example 1: Successful GitHub Sign-In — Tomasz Kowalski
- **Given**: Tomasz Kowalski clicks "Sign in with GitHub" on "Outside-In TDD" post
- **When**: GitHub redirects to /api/auth/oauth/github/callback?code=CODE&state=/posts/outside-in-tdd
- **Then**: System exchanges code for access token
- **And**: Retrieves Tomasz's profile from GitHub (name: "Tomasz Kowalski", avatar from github.com)
- **And**: Creates ReaderSession in database with 30-day expiry
- **And**: Sets reader_session cookie
- **And**: Redirects to /posts/outside-in-tdd as authenticated reader

#### Example 2: Successful Google Sign-In — Maria Santos
- **Given**: Maria Santos clicks "Sign in with Google" on any post
- **When**: Google redirects with valid authorization code
- **Then**: System creates session with Maria's Google profile
- **And**: Session cookie set for 30 days

#### Example 3: User Denies Consent
- **Given**: Reader authorizes on GitHub but denies consent
- **When**: GitHub redirects with error=access_denied
- **Then**: No session created
- **And**: Reader redirected to original post without error indicator

### UAT Scenarios (BDD)

#### Scenario: GitHub OAuth callback creates reader session
Given a reader has granted consent on GitHub as "Tomasz Kowalski"
When the OAuth callback is received with a valid authorization code for "github"
Then a reader session is created
And the session contains display name "Tomasz Kowalski"
And the session contains provider "GitHub"
And the reader is redirected back to the original post

#### Scenario: Google OAuth callback creates reader session
Given a reader has granted consent on Google as "Maria Santos"
When the OAuth callback is received with a valid authorization code for "google"
Then a reader session is created
And the session contains display name "Maria Santos"
And the session contains provider "Google"

#### Scenario: OAuth consent denied returns to post without error
When the OAuth callback is received with consent denied for "github"
Then the reader is redirected back to the original post
And no session is created

#### Scenario: OAuth provider error returns to post with error indicator
When the OAuth callback is received with a provider error for "google"
Then the reader is redirected back to the original post with an error indicator
And no session is created

### Acceptance Criteria
- [ ] Authorization code exchanged for access token
- [ ] User profile retrieved from OAuth provider
- [ ] ReaderSession entity created with correct data
- [ ] Session persisted to database
- [ ] Session cookie set with HttpOnly, Secure, 30-day expiry
- [ ] Redirect preserves original return URL
- [ ] Error responses handled gracefully without 500

### Outcome KPIs
- **Who**: Blog readers wanting to comment
- **Does what**: Complete OAuth sign-in flow successfully
- **By how much**: 95% of OAuth callbacks create valid sessions
- **Measured by**: Session creation rate vs OAuth callback rate
- **Baseline**: Currently 0% - all callbacks fail

---

## US-OAuth-003: Validate OAuth Session and Sign Out

### Problem
Readers need to verify their authentication status and be able to sign out. The current session validation and sign-out endpoints may have issues preventing proper session management.

### Who
- **Authenticated Blog Reader** who wants to check login status or sign out

### Solution
Implement session validation endpoint that checks cookie and returns user info, and sign-out endpoint that clears the session cookie.

### Domain Examples

#### Example 1: Check Session — Authenticated Reader
- **Given**: Reader has valid session cookie from previous GitHub sign-in
- **When**: GET /api/auth/session
- **Then**: Returns { authenticated: true, displayName: "Tomasz Kowalski", provider: "GitHub", avatarUrl: "..." }

#### Example 2: Check Session — No Session
- **Given**: Reader has no session cookie
- **When**: GET /api/auth/session
- **Then**: Returns { authenticated: false, displayName: null, provider: null, avatarUrl: null }

#### Example 3: Sign Out
- **Given**: Reader is authenticated
- **When**: POST /api/auth/signout
- **Then**: Session cookie deleted
- **And**: Subsequent /api/auth/session returns not authenticated

### UAT Scenarios (BDD)

#### Scenario: Session check returns authenticated status
Given a reader session exists for "Tomasz Kowalski" via "GitHub"
When the reader checks their session status
Then the session status indicates authenticated
And the response contains display name "Tomasz Kowalski"
And the response contains provider "GitHub"

#### Scenario: Session check with no session returns not authenticated
When a reader with no session checks their session status
Then the session status indicates not authenticated

#### Scenario: Session check with expired session returns not authenticated
Given a reader session exists for "Tomasz Kowalski" via "GitHub" that has expired
When the reader checks their session status
Then the session status indicates not authenticated

#### Scenario: Sign out clears the reader session
Given a reader session exists for "Maria Santos" via "Google"
When the reader signs out
Then the session is cleared
And checking session status shows not authenticated

#### Scenario: Sign out with no active session succeeds silently
When a reader with no session signs out
Then the response status is 204

### Acceptance Criteria
- [ ] Session check returns authenticated user info from valid cookie
- [ ] Session check returns not authenticated for missing/expired cookies
- [ ] Sign-out clears session cookie
- [ ] Sign-out succeeds even with no active session (idempotent)
- [ ] Expired sessions treated as not authenticated

### Outcome KPIs
- **Who**: Authenticated blog readers
- **Does what**: Verify authentication status and sign out
- **By how much**: 100% of session checks return correct status
- **Measured by**: Comparison of session state vs returned status
- **Baseline**: Unknown - needs validation

---

## US-OAuth-004: Support Redirect URI Configuration

### Problem
The redirect URI built by the backend (`{scheme}://{host}/api/auth/oauth/{provider}/callback`) must exactly match the redirect URI registered in GitHub and Google OAuth apps. Different environments (local development vs production) have different URLs, causing mismatches.

### Who
- **System Administrator** configuring OAuth apps
- **Blog Reader** signing in from different environments

### Solution
Allow redirect URI to be configurable via environment variable, with validation that it matches OAuth app registration.

### Domain Examples

#### Example 1: Local Development
- **Given**: Running locally on http://localhost:5000
- **And**: GitHub OAuth app registered with callback http://localhost:5000/api/auth/oauth/github/callback
- **When**: Reader initiates GitHub sign-in
- **Then**: Redirect URI in authorization request matches registered callback

#### Example 2: Production
- **Given**: Deployed to https://api.theaugmentedcraftsman.christianborrello.dev
- **And**: GitHub OAuth app registered with production callback
- **When**: Reader initiates GitHub sign-in
- **Then**: Redirect URI uses production URL

#### Example 3: Mismatch Detection
- **Given**: Configured redirect URI does not match OAuth app registration
- **When**: OAuth callback received
- **Then**: Error logged with clear message about mismatch
- **And**: User-friendly error displayed

### UAT Scenarios (BDD)

#### Scenario: Redirect URI matches GitHub app registration
Given the system is configured with redirect URI matching GitHub app
When a reader initiates GitHub sign-in
Then the authorization URL contains the correct redirect URI
And GitHub redirects back successfully

#### Scenario: Redirect URI configuration supports production
Given the system is deployed to production domain
When OAuth is initiated
Then the redirect URI uses the production domain

### Acceptance Criteria
- [ ] Redirect URI configurable via environment variable
- [ ] Default redirect URI works for standard deployments
- [ ] Redirect URI built correctly for both HTTP and HTTPS
- [ ] Works with custom ports in development

### Outcome KPIs
- **Who**: OAuth sign-in attempts
- **Does what**: Receive valid redirect URI matching OAuth app
- **By how much**: 100% of OAuth flows use correct redirect URI
- **Measured by**: OAuth provider redirect success rate
- **Baseline**: Currently failing due to mismatch

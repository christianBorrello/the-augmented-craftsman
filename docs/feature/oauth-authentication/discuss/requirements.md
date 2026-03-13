# OAuth Authentication - Requirements

## Feature ID: oauth-authentication

## Problem Statement

OAuth authentication (GitHub/Google) on the blog returns HTTP 500 errors, preventing readers from signing in with social accounts to comment on posts.

## Technical Context

- **Project**: The Augmented Craftsman Blog
- **Backend**: .NET 10 Minimal API
- **Database**: PostgreSQL (Neon)
- **Affected Endpoints**:
  - `GET /api/auth/oauth/{provider}` - Initiates OAuth flow
  - `GET /api/auth/oauth/{provider}/callback` - Handles OAuth callback

## Root Cause Analysis

### Issue 1: Missing OAuth Configuration Validation
- OAuth settings loaded from environment variables (`OAuth:GitHub:ClientId`, etc.)
- If not set, defaults to empty strings in `Program.cs` (lines 75-80)
- `ProductionOAuthClient` uses empty credentials, causing API failures

### Issue 2: Unhandled Exceptions in ProductionOAuthClient
- `GetAuthorizationUrlAsync` throws `ArgumentOutOfRangeException` for unknown providers
- `BuildGoogleAuthUrl` throws `InvalidOperationException` when Google not configured
- These exceptions bubble up as 500 errors instead of returning proper error responses

### Issue 3: Redirect URI Mismatch
- Built redirect URI: `{scheme}://{host}/api/auth/oauth/{provider}/callback`
- Must exactly match OAuth app registered redirect URI
- Local development vs production URLs differ

## Requirements

### R1: OAuth Configuration Validation
- Validate required OAuth settings at startup
- Return clear error messages for missing configuration
- Prevent runtime failures due to misconfiguration

### R2: Graceful Error Handling
- Handle unsupported provider requests with HTTP 400 (not 500)
- Handle OAuth provider errors gracefully
- Return user-friendly error messages in redirect

### R3: Redirect URI Validation
- Ensure redirect URI matches OAuth app configuration
- Support both local development and production URLs

### R4: Session Management
- Create reader session on successful OAuth callback
- Persist session to database with 30-day expiry
- Support session validation and sign-out

## Domain Examples

### Example 1: GitHub OAuth Success Flow
- **Given**: Reader clicks "Sign in with GitHub" on post
- **When**: Navigates to `/api/auth/oauth/github?returnUrl=/posts/outside-in-tdd`
- **Then**: Redirected to GitHub authorization page
- **After Callback**: Session cookie set, redirected to post with authenticated state

### Example 2: Missing GitHub Credentials
- **Given**: GitHubClientId environment variable not set
- **When**: Reader initiates GitHub sign-in
- **Then**: HTTP 400 returned with clear error message (not 500)

### Example 3: OAuth Consent Denied
- **Given**: Reader denies GitHub consent
- **When**: OAuth callback received with `error=access_denied`
- **Then**: Redirected to post without creating session (no error displayed)

## Acceptance Criteria

### AC1: GitHub OAuth Initiates Successfully
- [ ] GET /api/auth/oauth/github returns 302 redirect to GitHub
- [ ] Redirect URI contains client_id and proper scope (read:user)
- [ ] State parameter preserved for return URL

### AC2: GitHub OAuth Callback Creates Session
- [ ] Valid authorization code exchanged for access token
- [ ] User profile retrieved from GitHub API
- [ ] ReaderSession created in database
- [ ] Session cookie set with 30-day expiry
- [ ] Redirected to returnUrl with session

### AC3: Google OAuth Initiates Successfully
- [ ] GET /api/auth/oauth/google returns 302 redirect to Google
- [ ] Redirect URI contains client_id and proper scope (openid email profile)
- [ ] Works when Google credentials configured

### AC4: Error Handling
- [ ] Unsupported provider returns HTTP 400 (not 500)
- [ ] Missing credentials returns HTTP 400 with error message
- [ ] OAuth provider error redirects with error indicator
- [ ] access_denied redirects without error indicator

### AC5: Session Management
- [ ] /api/auth/session returns authenticated status with user info
- [ ] /api/auth/signout clears session cookie
- [ ] Expired sessions return not authenticated

## Technical Constraints

- Must work with existing OAuth apps (GitHub and Google already created)
- Session storage uses existing ReaderSession entity
- Must integrate with existing endpoint registration pattern
- No changes to frontend required

# OAuth Authentication - Acceptance Criteria

## Feature ID: oauth-authentication

## Overview

These acceptance criteria validate the OAuth authentication fix for The Augmented Craftsman Blog. The primary issue is that OAuth endpoints return HTTP 500 errors instead of properly handling authentication flows.

## Test Environment

- **Backend**: .NET 10 Minimal API
- **Database**: PostgreSQL (Neon)
- **OAuth Providers**: GitHub, Google
- **Endpoints Under Test**:
  - `GET /api/auth/oauth/{provider}` - Initiate OAuth
  - `GET /api/auth/oauth/{provider}/callback` - Handle OAuth callback
  - `GET /api/auth/session` - Check session status
  - `POST /api/auth/signout` - Sign out

---

## AC1: OAuth Configuration Validation

### Criterion AC1.1: Missing GitHub credentials returns 400
**Test**: `GET /api/auth/oauth/github` without GitHub OAuth configured

**Given**: GitHubClientId and/or GitHubClientSecret environment variables not set  
**When**: Reader initiates GitHub sign-in  
**Then**: Response status is HTTP 400 (Bad Request)  
**And**: Response body contains error message about GitHub OAuth not configured

### Criterion AC1.2: Missing Google credentials returns 400
**Test**: `GET /api/auth/oauth/google` without Google OAuth configured

**Given**: GoogleClientId and/or GoogleClientSecret environment variables not set  
**When**: Reader initiates Google sign-in  
**Then**: Response status is HTTP 400 (Bad Request)  
**And**: Response body contains error message about Google OAuth not configured

### Criterion AC1.3: Unsupported provider returns 400
**Test**: `GET /api/auth/oauth/twitter`

**Given**: Provider "twitter" is not supported  
**When**: Reader initiates sign-in with unsupported provider  
**Then**: Response status is HTTP 400 (Bad Request)  
**And**: Response body contains error "Unsupported provider"

---

## AC2: GitHub OAuth Flow

### Criterion AC2.1: Initiate GitHub OAuth redirects to GitHub
**Test**: `GET /api/auth/oauth/github?returnUrl=/posts/outside-in-tdd`

**Given**: GitHub OAuth credentials are configured  
**When**: Reader initiates GitHub sign-in with return URL  
**Then**: Response status is HTTP 302 (Redirect)  
**And**: Location header points to https://github.com/login/oauth/authorize  
**And**: URL contains client_id parameter  
**And**: URL contains redirect_uri parameter  
**And**: URL contains scope=read:user  
**And**: URL contains state parameter with return URL

### Criterion AC2.2: GitHub callback creates session
**Test**: `GET /api/auth/oauth/github/callback?code=VALID_CODE&state=/posts/outside-in-tdd`

**Given**: Valid authorization code from GitHub  
**And**: Reader has previously initiated OAuth flow  
**When**: GitHub redirects with authorization code  
**Then**: Authorization code exchanged for access token  
**And**: User profile retrieved from GitHub API  
**And**: ReaderSession created in database  
**And**: Response contains Set-Cookie header for reader_session  
**And**: Cookie has HttpOnly=true, Secure=true (if HTTPS), SameSite=Lax  
**And**: Cookie Max-Age is 30 days  
**And**: Response redirects to /posts/outside-in-tdd

### Criterion AC2.3: GitHub session contains correct data
**Test**: Verify session data after GitHub OAuth

**Given**: Successful GitHub OAuth callback  
**When**: Session is created  
**Then**: Session displayName matches GitHub profile name  
**And**: Session avatarUrl matches GitHub avatar_url  
**And**: Session provider is "GitHub"  
**And**: Session providerId matches GitHub user ID  
**And**: Session expiry is 30 days from creation

---

## AC3: Google OAuth Flow

### Criterion AC3.1: Initiate Google OAuth redirects to Google
**Test**: `GET /api/auth/oauth/google`

**Given**: Google OAuth credentials are configured  
**When**: Reader initiates Google sign-in  
**Then**: Response status is HTTP 302 (Redirect)  
**And**: Location header points to https://accounts.google.com/o/oauth2/v2/auth  
**And**: URL contains client_id parameter  
**And**: URL contains redirect_uri parameter  
**And**: URL contains scope=openid%20email%20profile  
**And**: URL contains response_type=code

### Criterion AC3.2: Google callback creates session
**Test**: `GET /api/auth/oauth/google/callback?code=VALID_CODE&state=/`

**Given**: Valid authorization code from Google  
**When**: Google redirects with authorization code  
**Then**: Session created with Google profile data  
**And**: Session contains display name from Google profile  
**And**: Session contains provider "Google"

---

## AC4: Error Handling

### Criterion AC4.1: OAuth consent denied handled gracefully
**Test**: `GET /api/auth/oauth/github/callback?error=access_denied&state=/posts/tdd-post`

**Given**: Reader denied GitHub consent  
**When**: OAuth callback received with access_denied error  
**Then**: No session created in database  
**And**: Response redirects to /posts/tdd-post (without error indicator)

### Criterion AC4.2: OAuth provider error handled gracefully
**Test**: `GET /api/auth/oauth/github/callback?error=invalid_request&state=/posts/tdd-post`

**Given**: OAuth provider returns error  
**When**: OAuth callback received with error parameter  
**Then**: No session created  
**And**: Response redirects to /posts/tdd-post?error=invalid_request

### Criterion AC4.3: Invalid state parameter handled
**Test**: `GET /api/auth/oauth/github/callback?code=CODE` (no state)

**Given**: OAuth callback without state parameter  
**When**: Request received  
**Then**: Response redirects to /?error=invalid_state

### Criterion AC4.4: Invalid authorization code handled
**Test**: `GET /api/auth/oauth/github/callback?code=INVALID_CODE&state=/`

**Given**: Invalid authorization code  
**When**: Token exchange attempted  
**Then**: Error returned from GitHub token endpoint  
**And**: Response redirects with error indicator  
**And**: No session created

---

## AC5: Session Management

### Criterion AC5.1: Session check returns authenticated for valid session
**Test**: `GET /api/auth/session` with valid cookie

**Given**: Valid reader_session cookie exists  
**When**: Reader checks session status  
**Then**: Response status is HTTP 200  
**And**: Response body contains authenticated: true  
**And**: Response body contains displayName  
**And**: Response body contains provider  
**And**: Response body contains avatarUrl

### Criterion AC5.2: Session check returns not authenticated for no session
**Test**: `GET /api/auth/session` without cookie

**Given**: No reader_session cookie  
**When**: Reader checks session status  
**Then**: Response status is HTTP 200  
**And**: Response body contains authenticated: false  
**And**: Response body contains null for displayName, provider, avatarUrl

### Criterion AC5.3: Session check returns not authenticated for expired session
**Test**: `GET /api/auth/session` with expired cookie

**Given**: Expired reader_session cookie  
**When**: Reader checks session status  
**Then**: Response body contains authenticated: false

### Criterion AC5.4: Sign out clears session
**Test**: `POST /api/auth/signout`

**Given**: Authenticated reader with valid session cookie  
**When**: Reader signs out  
**Then**: Response status is HTTP 204 (No Content)  
**And**: Set-Cookie header deletes reader_session  
**And**: Subsequent session check returns authenticated: false

### Criterion AC5.5: Sign out with no session succeeds silently
**Test**: `POST /api/auth/signout` without session

**Given**: No session cookie  
**When**: Reader signs out  
**Then**: Response status is HTTP 204  
**And**: No error returned

---

## AC6: Redirect URI Handling

### Criterion AC6.1: Redirect URI uses HTTPS in production
**Test**: OAuth initiation in production (HTTPS)

**Given**: Application running on HTTPS  
**When**: OAuth initiated  
**Then**: Redirect URI uses https:// scheme

### Criterion AC6.2: Redirect URI uses correct host
**Test**: OAuth initiation with custom host

**Given**: Application running on custom host  
**When**: OAuth initiated  
**Then**: Redirect URI uses request's Host header

### Criterion AC6.3: Redirect URI matches callback endpoint format
**Test**: Verify redirect URI format

**Given**: OAuth initiated for provider "github"  
**When**: Authorization URL built  
**Then**: redirect_uri parameter is {scheme}://{host}/api/auth/oauth/github/callback

---

## Test Data

### GitHub Test User
- **ID**: 12345678
- **Login**: tomasz-kowalski
- **Name**: Tomasz Kowalski
- **Avatar**: https://avatars.githubusercontent.com/u/12345678?v=4

### Google Test User
- **ID**: 112233445566778899001
- **Name**: Maria Santos
- **Email**: maria.santos@example.com
- **Avatar**: https://lh3.googleusercontent.com/a/ABC123

### Test URLs
- **Local Development**: http://localhost:5000
- **Production**: https://api.theaugmentedcraftsman.christianborrello.dev
- **Blog Frontend**: https://theaugmentedcraftsman.christianborrello.dev

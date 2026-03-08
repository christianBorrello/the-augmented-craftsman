# Test Infrastructure -- Epic 5: User Engagement

## Overview

Epic 5 tests build on the existing acceptance test infrastructure (WebApplicationFactory + Testcontainers). New infrastructure is needed for engagement-specific API drivers, OAuth mocking, and reader session simulation.

---

## Existing Infrastructure (reused as-is)

| Component | Location | Purpose |
|-----------|----------|---------|
| TacBlogWebApplicationFactory | Support/TacBlogWebApplicationFactory.cs | In-process API host with Testcontainers PostgreSQL |
| DependencyConfig | Support/DependencyConfig.cs | Reqnroll DI container setup |
| TestHooks | Hooks/TestHooks.cs | Database cleanup between scenarios |
| ApiContext | Contexts/ApiContext.cs | Captures HTTP response for assertions |
| AuthContext | Contexts/AuthContext.cs | Stores admin JWT token |
| AuthApiDriver | Drivers/AuthApiDriver.cs | Admin login for test data setup |
| PostApiDriver | Drivers/PostApiDriver.cs | Post creation for test data setup |
| CommonSteps | StepDefinitions/CommonSteps.cs | Shared Given/Then steps (auth, status codes) |

---

## New Infrastructure Required

### 1. API Drivers

#### LikeApiDriver

```
Drivers/LikeApiDriver.cs

Methods:
  LikePost(slug, visitorId)      -> POST /api/posts/{slug}/likes
  UnlikePost(slug, visitorId)    -> DELETE /api/posts/{slug}/likes/{visitorId}
  GetLikeCount(slug)             -> GET /api/posts/{slug}/likes/count
  CheckIfLiked(slug, visitorId)  -> GET /api/posts/{slug}/likes/check/{visitorId}

Dependencies: HttpClient, ApiContext
Auth: None (likes are anonymous)
```

#### CommentApiDriver

```
Drivers/CommentApiDriver.cs

Methods:
  PostComment(slug, text, sessionCookie?)     -> POST /api/posts/{slug}/comments
  GetComments(slug)                           -> GET /api/posts/{slug}/comments
  GetCommentCount(slug)                       -> GET /api/posts/{slug}/comments/count
  DeleteComment(slug, commentId, jwtToken?)   -> DELETE /api/posts/{slug}/comments/{id}
  GetAdminComments(jwtToken)                  -> GET /api/admin/comments

Dependencies: HttpClient, ApiContext, ReaderSessionContext
Auth: Reader session cookie for POST, Admin JWT for DELETE and admin GET
```

#### OAuthApiDriver

```
Drivers/OAuthApiDriver.cs

Methods:
  InitiateOAuth(provider, returnUrl)        -> GET /api/auth/oauth/{provider}
  SimulateCallback(provider, code, state)   -> GET /api/auth/oauth/{provider}/callback
  CheckSession(sessionCookie?)              -> GET /api/auth/session
  SignOut(sessionCookie?)                   -> POST /api/auth/signout

Dependencies: HttpClient, ApiContext, ReaderSessionContext
Auth: Reader session cookie for session check and sign out
```

### 2. Contexts

#### ReaderSessionContext

```
Contexts/ReaderSessionContext.cs

Properties:
  SessionCookie: string?      -- httpOnly session cookie value
  IsAuthenticated: bool       -- derived from SessionCookie presence
  DisplayName: string?        -- from OAuth callback
  Provider: string?           -- "GitHub" or "Google"

Purpose: Stores reader session state across steps within a scenario.
Scope: Per-scenario (scoped in DI).
```

### 3. OAuth Mocking Strategy

OAuth providers (Google, GitHub) must be mockable at the driven port boundary. The acceptance tests should NOT call real OAuth providers.

#### Approach: Stub IOAuthClient

```
Support/StubOAuthClient.cs

Implements: IOAuthClient (driven port)

Configurable behaviors:
  - ConsentGranted(displayName, avatarUrl, provider)  -- simulates successful OAuth
  - ConsentDenied()                                    -- simulates user denying consent
  - ProviderError()                                    -- simulates provider failure

Registered in WebApplicationFactory:
  Replace IOAuthClient registration with StubOAuthClient (singleton)
  Same pattern as existing StubImageStorage
```

#### WebApplicationFactory Extension

```csharp
// In ConfigureWebHost:
var oauthDescriptor = services.SingleOrDefault(
    d => d.ServiceType == typeof(IOAuthClient));
if (oauthDescriptor is not null)
    services.Remove(oauthDescriptor);

services.AddSingleton<StubOAuthClient>();
services.AddSingleton<IOAuthClient>(sp => sp.GetRequiredService<StubOAuthClient>());
```

### 4. Database Cleanup

#### TestHooks Extension

Add cleanup for new tables in the BeforeScenario hook:

```sql
-- Add to existing cleanup SQL:
IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'likes') THEN
    DELETE FROM likes;
END IF;
IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'comments') THEN
    DELETE FROM comments;
END IF;
IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'reader_sessions') THEN
    DELETE FROM reader_sessions;
END IF;
```

Also reset StubOAuthClient state between scenarios (same pattern as StubImageStorage).

### 5. Test Data Builders

#### CommentTestDataBuilder

```
Support/CommentTestDataBuilder.cs

Purpose: Create comments directly in the database for Given steps
         (bypasses API to avoid needing full OAuth flow for test setup)

Methods:
  WithDisplayName(name)
  WithProvider(provider)
  WithText(text)
  OnPost(slug)
  Build() -> inserts directly via EF Core

Usage in Given steps:
  "Given 'Outside-In TDD' has a comment by 'SpamBot' via 'GitHub'"
  -> Uses builder to insert directly, avoiding OAuth+session+comment API chain
```

#### ReaderSessionTestDataBuilder

```
Support/ReaderSessionTestDataBuilder.cs

Purpose: Create reader sessions directly in the database for Given steps
         (bypasses OAuth flow for scenarios that just need an authenticated reader)

Methods:
  ForReader(displayName)
  WithProvider(provider)
  WithAvatarUrl(url)
  ExpiresAt(dateTime)          -- for expired session scenarios
  Build() -> inserts session, returns session cookie value

Usage in Given steps:
  "Given a reader session exists for 'Tomasz Kowalski' via 'GitHub'"
  -> Creates session in DB, stores cookie in ReaderSessionContext
```

### 6. DependencyConfig Updates

```csharp
// Add to CreateServices():
services.AddScoped<LikeApiDriver>();
services.AddScoped<CommentApiDriver>();
services.AddScoped<OAuthApiDriver>();
services.AddScoped<ReaderSessionContext>();
services.AddScoped<CommentTestDataBuilder>();
services.AddScoped<ReaderSessionTestDataBuilder>();
```

### 7. Step Definition Files

| File | Domain | Steps Covered |
|------|--------|---------------|
| LikeSteps.cs | Likes | Like/unlike, count check, like status |
| CommentSteps.cs | Comments | Post comment, list comments, count, validation |
| OAuthSteps.cs | OAuth/Sessions | Sign-in flow, session check, sign out |
| ModerationSteps.cs | Moderation | Admin delete, admin list |

Each step definition class follows the existing pattern: constructor injection of drivers and contexts, [Binding] attribute, organized by domain concept.

---

## Migration Requirements

A single EF Core migration (`AddEngagementTables`) creates:
- `likes` table with composite PK (post_slug, visitor_id)
- `comments` table with UUID PK
- `reader_sessions` table with UUID PK

The migration must be applied before acceptance tests run. The existing `EnsureMigratedAsync()` in TacBlogWebApplicationFactory handles this automatically.

---

## Test Execution Strategy

### Running Order

1. Walking skeleton scenarios run first (no @skip tag)
2. Enable one @skip scenario at a time
3. Implement production code to make it pass
4. Commit on green
5. Enable next scenario

### Test Categories

| Tag | Purpose | CI Gate |
|-----|---------|---------|
| @walking-skeleton | Must pass before any other work | Yes |
| @skip | Not yet implemented | Skipped |
| @property | Implement as property-based test | Inner loop |
| @epic5 | All Epic 5 scenarios | Filter |

### Performance Considerations

- Testcontainers PostgreSQL started once per test run (BeforeTestRun hook)
- Database cleaned between scenarios (not recreated)
- OAuth stubbed (no external HTTP calls)
- All tests run in-process via WebApplicationFactory
- Expected total execution time: < 30 seconds for all Epic 5 scenarios

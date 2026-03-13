# OAuth Authentication - Wave Decisions

## Feature ID: oauth-authentication

## Prior Wave: DISCUSS

The DISCUSS wave identified the following issues through analysis:

1. **Root Cause 1**: OAuthSettings with empty strings — when environment variables are not set, empty strings are used instead of proper validation
2. **Root Cause 2**: Unhandled exceptions — ProductionOAuthClient throws ArgumentOutOfRangeException instead of returning graceful errors
3. **Root Cause 3**: Redirect URI mismatch — URI constructed dynamically from request.Host may not match registered OAuth redirect URIs

## Requirements Analysis

### R1: OAuth Configuration Validation

**Requirement**: Validate OAuth settings at application startup and fail fast with clear error messages.

**Acceptance Criteria**:
- [ ] Application throws InvalidOperationException if required OAuth credentials are missing
- [ ] Error message indicates which environment variable is missing
- [ ] Partial Google OAuth configuration is rejected (both or neither)

### R2: Graceful Error Handling

**Requirement**: Return HTTP 400 (Bad Request) instead of HTTP 500 (Internal Server Error) when OAuth validation fails.

**Acceptance Criteria**:
- [ ] Unknown OAuth provider returns 400 with "Unsupported provider" message
- [ ] Missing configuration returns 400 (not 500) when attempted
- [ ] Error responses include actionable message for debugging

### R3: Redirect URI Validation

**Requirement**: Support both development and production redirect URIs with explicit configuration.

**Acceptance Criteria**:
- [ ] Configurable `OAuth:RedirectBaseUrl` for explicit redirect URI control
- [ ] Default behavior uses request scheme + host (backward compatible)
- [ ] Works with GitHub OAuth app registered redirect URIs

### R4: Session Management (Existing)

**Requirement**: Existing session management continues to work.

**Acceptance Criteria**:
- [ ] Session cookie set after successful OAuth callback
- [ ] Session validation endpoint returns authenticated status
- [ ] Sign out clears session cookie

## Technology Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Validation approach | Startup validation with IHostEnvironment | Fail fast, clear errors, differentiate dev vs prod |
| Error handling pattern | Result objects (existing pattern) | Consistent with existing use cases (InitiateOAuthResult, HandleOAuthCallbackResult) |
| Configuration storage | appsettings.json + environment variables | Standard .NET pattern, secure secrets management |
| Redirect URI config | Explicit base URL option | Eliminates ambiguity between dev/prod |

## Design Pattern Choices

### Hexagonal Architecture

**Selected**: Yes — existing architecture

The OAuth feature already follows hexagonal architecture:
- `IOAuthClient` is the driven port (secondary)
- `InitiateOAuth`, `HandleOAuthCallback` are use cases in the application core
- `ProductionOAuthClient`, `DevOAuthClient` are driven adapters
- `OAuthEndpoints` is the driving adapter

The fix extends this pattern by adding:
- `OAuthSettingsValidator` as infrastructure component
- Validation at the adapter boundary

### Error Handling Strategy

**Selected**: Result objects + startup validation

Alternative considered: Exception handling middleware
- Rejected because: Result objects are already used in the codebase, maintains consistency

## Component Decomposition

### New Components

| Component | Type | Responsibility |
|-----------|------|---------------|
| OAuthSettingsValidator | Infrastructure | Validates OAuth config at startup |

### Modified Components

| Component | Change Type | Description |
|-----------|-------------|-------------|
| OAuthSettings | Enhancement | Add validation method, ensure null-empty distinction |
| ProductionOAuthClient | Fix | Return results instead of throwing |
| OAuthEndpoints | Enhancement | Support configurable redirect base URL |
| Program.cs | Enhancement | Call validator, handle startup errors |

## Integration Points

### External Systems

| System | Integration Type | Contract |
|--------|-----------------|----------|
| GitHub OAuth | OAuth 2.0 Authorization Code | Standard OAuth 2.0 |
| Google OAuth | OAuth 2.0 Authorization Code | Standard OAuth 2.0 |

**Contract Testing Note**: OAuth providers (GitHub, Google) don't support consumer-driven contracts directly. However, the OAuth 2.0 flow is well-documented and stable. Testing should focus on:
1. Integration tests with mock OAuth server
2. Verification of redirect URI construction

## Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| OAuth credentials in source | Low | High | Environment variables only, no hardcoded values |
| Redirect URI mismatch in prod | Medium | High | Configuration validation at startup |
| Unconfigured OAuth in prod | Low | Medium | Fail-fast validation prevents 500 errors |

## Handoff to Acceptance Designer

The acceptance-designer should create BDD scenarios for:

1. **Happy path**: User authenticates via GitHub successfully
2. **Missing config**: Application fails to start with clear error
3. **Unsupported provider**: User gets 400 instead of 500
4. **Redirect URI mismatch**: Configuration validation catches mismatch

### Example Scenario Structure

```gherkin
Scenario: Successful GitHub OAuth login
  Given the OAuth configuration is valid
  And GitHub OAuth credentials are configured
  When the reader initiates OAuth with "github"
  Then they are redirected to GitHub authorization page
  And after approving, they are returned to the blog as authenticated
  
Scenario: Missing OAuth configuration
  Given the application is starting in production
  And OAuth:GitHub:ClientId is not set
  When the application starts
  Then it throws InvalidOperationException
  And the error message mentions "OAuth:GitHub:ClientId"
```

## Next Wave: DELIVER

The software-crafter will implement:
1. OAuthSettingsValidator
2. OAuthSettings validation method
3. ProductionOAuthClient result handling
4. OAuthEndpoints redirect URI configuration
5. Program.cs startup validation
6. Unit and integration tests

Each implementation should follow the existing TDD approach with:
- Red (write failing test)
- Green (implement minimal solution)
- Refactor (improve design while green)
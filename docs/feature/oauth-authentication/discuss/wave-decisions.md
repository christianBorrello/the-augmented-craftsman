# OAuth Authentication - Wave Decisions

## Feature ID: oauth-authentication

## Decision Framework

### Feature Type: Backend
- **Classification**: Bug fix / Technical implementation
- **Rationale**: OAuth is an API authentication mechanism; no new user-facing features

### Walking Skeleton: No
- **Classification**: Brownfield fix (existing code)
- **Rationale**: OAuth code already exists (ProductionOAuthClient.cs, OAuthEndpoints.cs); this is a fix for existing functionality

### UX Research Depth: Lightweight
- **Classification**: Technical investigation
- **Rationale**: No new user experience - fixing existing broken authentication flow

### JTBD Analysis: No
- **Classification**: Bug fix, not new feature
- **Rationale**: The job to be done (reader authentication) is already understood; this is fixing a broken implementation

---

## Wave Routing

### DISCOVER Wave
- **Status**: ✅ Complete
- **Output**: This DISCUSS folder with all artifacts

### DESIGN Wave
- **Route**: To Solution Architect
- **Scope**: 
  - Error handling improvements to ProductionOAuthClient
  - Configuration validation approach
  - Redirect URI handling

### DISTILL Wave
- **Route**: To Acceptance Designer
- **Scope**: 
  - BDD scenarios for OAuth flow
  - Acceptance test implementation

### DELIVER Wave
- **Route**: To Software Crafter
- **Scope**:
  - Fix OAuth configuration loading
  - Improve error handling
  - Test with real OAuth providers

---

## Architecture Decisions

### AD1: Error Handling Strategy
**Decision**: Return error results instead of throwing exceptions

**Options Considered**:
1. Throw exceptions (current) → Results in 500 errors
2. Return error results → Results in 400/redirect with error

**Selected**: Option 2
**Rationale**: Provides better user experience; aligns with RESTful API best practices

### AD2: Configuration Validation Timing
**Decision**: Validate at startup

**Options Considered**:
1. Validate at startup → Fail fast with clear message
2. Validate on first request → Delayed failure

**Selected**: Option 1
**Rationale**: Fail fast principle; clear error messages for administrators

### AD3: Redirect URI Handling
**Decision**: Build from request, allow override

**Options Considered**:
1. Hard-coded → Inflexible
2. Build from request → Works for most cases
3. Environment variable override → Flexible

**Selected**: Option 3
**Rationale**: Supports local development and production; explicit control

---

## Prior Art

### Existing Implementation
- **Files**:
  - `TacBlog.Api/Endpoints/OAuthEndpoints.cs` - API endpoints
  - `TacBlog.Application/Features/OAuth/InitiateOAuth.cs` - OAuth initiation use case
  - `TacBlog.Application/Features/OAuth/HandleOAuthCallback.cs` - Callback handling
  - `TacBlog.Infrastructure/Identity/ProductionOAuthClient.cs` - OAuth client
  - `TacBlog.Infrastructure/Identity/OAuthSettings.cs` - Configuration

- **Existing Tests**:
  - `TacBlog.Acceptance.Tests/Features/Epic5_UserEngagement/OAuth.feature` - BDD scenarios
  - `TacBlog.Application.Tests/Features/OAuth/HandleOAuthCallbackShould.cs` - Unit tests

### Integration Points
- `IOAuthClient` port - Already defined
- `IReaderSessionRepository` - Already defined
- `IClock` - Already defined

---

## Implementation Notes

### Code Changes Required

1. **Program.cs** (lines 75-86):
   - Add OAuth settings validation at startup
   - Consider throwing clear exception if required settings missing in production

2. **ProductionOAuthClient.cs**:
   - Modify `GetAuthorizationUrlAsync` to return error result for unsupported provider
   - Modify `BuildGoogleAuthUrl` to return error result instead of throwing
   - Add validation before making API calls

3. **OAuthEndpoints.cs**:
   - Already has good error handling pattern
   - May need adjustment based on ProductionOAuthClient changes

### Environment Variables Required

| Variable | Required | Description |
|----------|----------|-------------|
| OAuth__GitHub__ClientId | Yes | GitHub OAuth App Client ID |
| OAuth__GitHub__ClientSecret | Yes | GitHub OAuth App Client Secret |
| OAuth__Google__ClientId | No* | Google OAuth App Client ID |
| OAuth__Google__ClientSecret | No* | Google OAuth App Client Secret |

*Google OAuth optional - can be disabled by not setting these variables

### OAuth App Configuration

**GitHub**:
- App created and configured
- Need to verify callback URL matches deployment

**Google**:
- App created and configured
- Need to verify callback URL matches deployment

---

## Risk Assessment

### Risk 1: OAuth Provider Configuration Mismatch
**Severity**: High
**Likelihood**: Medium
**Mitigation**: 
- Document required redirect URIs
- Validate at startup
- Add clear error messages

### Risk 2: Token Exchange Failures
**Severity**: Medium
**Likelihood**: Low
**Mitigation**:
- Graceful error handling
- User-friendly redirect with error indicator

### Risk 3: Session Management Issues
**Severity**: Medium
**Likelihood**: Low
**Mitigation**:
- Existing implementation tested
- Add acceptance tests for edge cases

---

## Dependencies

### Blocking
- None

### Dependent
- Frontend authentication UI (already implemented in Astro)

### External
- GitHub OAuth App configuration
- Google OAuth App configuration

---

## File Manifest

| File | Purpose |
|------|---------|
| `discuss/requirements.md` | Feature requirements and root cause analysis |
| `discuss/user-stories.md` | 4 user stories with domain examples |
| `discuss/acceptance-criteria.md` | 20+ acceptance criteria in Gherkin format |
| `discuss/dor-checklist.md` | Definition of Ready validation |
| `discuss/wave-decisions.md` | This file - wave routing and decisions |

---

*Wave decisions documented: 2026-03-13*

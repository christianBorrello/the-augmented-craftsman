# Root Cause Analysis: OAuth Login Fails with redirect_uri Mismatch

**Date**: 2026-03-14
**Analyst**: Rex (Root Cause Analysis Specialist)
**Methodology**: Toyota 5 Whys, Multi-Causal
**Scope**: OAuth login flow for GitHub and Google providers on production backend (Koyeb, Frankfurt)

---

## Problem Statement

OAuth login fails for both GitHub and Google providers with `redirect_uri mismatch` errors, even after registering the correct callback URLs in both OAuth application consoles. The registered callbacks use `https://` but the `redirect_uri` parameter sent to the provider authorization endpoint is constructed with `http://`.

**Observed Errors:**
- GitHub: `"The redirect_uri is not associated with this application"`
- Google: `redirect_uri=http://api.theaugmentedcraftsman.christianborrello.dev/api/auth/oauth/google/callback` (note `http://`, not `https://`)

**Registered Callbacks:**
- GitHub: `https://api.theaugmentedcraftsman.christianborrello.dev/api/auth/oauth/github/callback`
- Google: `https://api.theaugmentedcraftsman.christianborrello.dev/api/auth/oauth/google/callback`

**Scope boundary:** This analysis covers the scheme mismatch in `redirect_uri` construction only. Unrelated concerns (session management, CORS, token validation) are out of scope.

---

## Phase 1: Evidence Collection

### Source Files Examined

| File | Relevant Finding |
|------|-----------------|
| `backend/src/TacBlog.Api/Program.cs` | `UseForwardedHeaders` added at commit `2c7a43d`, placed after exception handler block, before `UseSerilogRequestLogging`, `UseCors`, `UseAuthentication` |
| `backend/src/TacBlog.Api/Endpoints/OAuthEndpoints.cs` | `BuildRedirectUri` uses `request.Scheme` and `request.Host` directly from `HttpContext` |
| `backend/src/TacBlog.Infrastructure/Identity/ProductionOAuthClient.cs` | Passes `redirectUri` string verbatim in both authorization URL construction and token exchange |

### `BuildRedirectUri` — the scheme origin point

```csharp
private static string BuildRedirectUri(HttpContext httpContext, string provider)
{
    var request = httpContext.Request;
    return $"{request.Scheme}://{request.Host}/api/auth/oauth/{provider}/callback";
}
```

`request.Scheme` reflects Koyeb's internal HTTP transport, not the external HTTPS connection. `BuildRedirectUri` is called at two points in the flow: `InitiateOAuthAsync` (constructs the authorization URL sent to the provider) and `HandleCallbackAsync` (used again during code exchange, must match the value sent to the provider).

### `UseForwardedHeaders` as added in commit `2c7a43d`

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});
```

No `KnownProxies` or `KnownNetworks` configured. `ForwardedHeadersOptions` defaults `KnownProxies` and `KnownNetworks` to loopback addresses only.

### Middleware Order (post-commit `2c7a43d`)

```
1. UseExceptionHandler          (production only, before UseForwardedHeaders)
2. [startup scope: migrations, validator]
3. UseForwardedHeaders          ← added at 2c7a43d
4. UseSerilogRequestLogging
5. UseCors
6. UseAuthentication
7. UseAuthorization
8. [route handlers]
```

The exception handler is registered conditionally (`if (!app.Environment.IsDevelopment())`), before the startup scope block and before `UseForwardedHeaders`. This ordering is acceptable — the concern is that `UseForwardedHeaders` runs before all middleware that reads `request.Scheme` (Serilog, CORS, Auth, and route handlers). It does.

### CI/CD Pipeline Analysis

Commit `2c7a43d` is on `main` and modifies `backend/**`. The CI workflow (`ci.yml`) triggers on `push` to `main` with path filter `backend/**`. The workflow runs tests, builds and pushes a Docker image to GHCR, then Koyeb auto-deploys from GHCR (confirmed by `smoke-test` job which waits 90 seconds for Koyeb auto-deploy). No CI failure was found in the git log context — commit `2c7a43d` is followed by later commits (`380ad16`, `b94c4cc`, `c2c5547`, `8b1e42d`) in the same session, suggesting CI ran cleanly. However, those subsequent commits all modify `frontend/**` only, meaning they do not re-trigger the backend CI pipeline.

**Deploy status of `2c7a43d`**: The backend CI pipeline would have been triggered by commit `2c7a43d`. Whether it completed successfully cannot be confirmed from git history alone — CI run status requires checking GitHub Actions directly. The smoke test waits only 90 seconds for Koyeb auto-deploy, which may be insufficient for cold starts.

### Koyeb Proxy Header Evidence

From Koyeb official documentation (edge network reference):
- Koyeb explicitly documents injecting `x-forwarded-for` and `x-forwarded-host`.
- **`X-Forwarded-Proto` is NOT listed as an automatically injected header by Koyeb.**
- Koyeb terminates TLS at the edge and proxies to the container over HTTP internally (confirmed by known facts in problem statement).
- The proxy IP connecting to the container is not a loopback address — it is Koyeb's internal service mesh IP.

### ASP.NET Core ForwardedHeaders Breaking Change (.NET 8.0.17 / 9.0.6)

From Microsoft documentation (breaking change announcement):
> Starting in ASP.NET Core 8.0.17 and 9.0.6, the Forwarded Headers Middleware ignores all X-Forwarded-* headers from proxies that aren't explicitly configured as trusted. KnownProxies and KnownNetworks default to loopback addresses only.

This project targets .NET 10, which inherits this behavior. The current `UseForwardedHeaders` call does not configure `KnownProxies` or `KnownNetworks`. Koyeb's proxy connects from a non-loopback IP. Therefore, even if Koyeb sends `X-Forwarded-Proto`, the middleware will silently ignore it.

---

## Phase 2: Toyota 5 Whys Analysis

### BRANCH A — `redirect_uri` built with `http://` scheme

**WHY 1A:** The `redirect_uri` parameter sent to GitHub and Google contains `http://` instead of `https://`.
*Evidence: Google error message shows `redirect_uri=http://api.theaugmentedcraftsman.christianborrello.dev/...`; GitHub error confirms the value does not match the registered `https://` URL.*

**WHY 2A:** `BuildRedirectUri` constructs the URL from `request.Scheme`, which returns `"http"`.
*Evidence: `OAuthEndpoints.cs` line 126: `$"{request.Scheme}://{request.Host}/api/auth/oauth/{provider}/callback"`. No scheme override or hardcoding present.*

**WHY 3A:** `request.Scheme` returns the internal transport scheme because the connection from Koyeb's proxy to the container is plain HTTP.
*Evidence: Known fact from problem statement — "Koyeb terminates TLS and proxies to the container internally via HTTP". Koyeb's architecture performs TLS termination at the edge; the container only ever sees HTTP connections.*

**WHY 4A:** The `UseForwardedHeaders` fix intended to correct `request.Scheme` via `X-Forwarded-Proto` has two independent failure modes that prevent it from working:
*Evidence: See sub-branches A1 and A2 below.*

---

#### SUB-BRANCH A1 — Koyeb does not inject `X-Forwarded-Proto`

**WHY 4A1:** Even if the middleware were correctly configured, Koyeb does not inject an `X-Forwarded-Proto` header.
*Evidence: Koyeb official documentation (edge network reference) explicitly lists only `x-forwarded-for` and `x-forwarded-host` as automatically injected headers. `X-Forwarded-Proto` is absent from the documented header set.*

**WHY 5A1:** Koyeb's design choice is to forward client IP identity (`x-forwarded-for`) and host routing (`x-forwarded-host`) but not scheme — possibly because their internal mesh uses a different signaling mechanism, or the documentation is incomplete.
*Evidence: Koyeb documentation does not document `X-Forwarded-Proto`. No community threads confirm Koyeb injects it. This is consistent with the observed behavior — the scheme was never corrected even before the fix was attempted.*

-> **ROOT CAUSE A1: Koyeb does not inject `X-Forwarded-Proto` into requests forwarded to the container. The fundamental assumption behind the `UseForwardedHeaders` fix is incorrect — there is no header to read.**

---

#### SUB-BRANCH A2 — `UseForwardedHeaders` ignores non-loopback proxies (even if header existed)

**WHY 4A2:** Even if Koyeb sent `X-Forwarded-Proto`, the `ForwardedHeadersOptions` as currently configured would silently ignore it.
*Evidence: `Program.cs` — `UseForwardedHeaders` is called with only `ForwardedHeaders = ForwardedHeaders.XForwardedProto`. No `KnownProxies` or `KnownNetworks` is set. The Microsoft breaking change document states: "only headers sent by known, trusted proxies (as configured via KnownProxies and KnownNetworks) are processed. Headers from unknown sources are ignored." KnownProxies defaults to loopback. Koyeb's proxy connects from a non-loopback IP.*

**WHY 5A2:** The ASP.NET Core security hardening change introduced in .NET 8.0.17 (backported to .NET 10) requires explicit proxy trust configuration. The developer was unaware of this requirement when writing the fix, or was using documentation from before the breaking change.
*Evidence: The fix commit (`2c7a43d`) configures `ForwardedHeaders.XForwardedProto` but omits `KnownProxies`/`KnownNetworks`. The breaking change was introduced in June 2025 (8.0.17 / 9.0.6 release cycle) and applies to .NET 10.*

-> **ROOT CAUSE A2: `UseForwardedHeaders` is configured without `KnownProxies` or `KnownNetworks`, causing ASP.NET Core to silently discard any `X-Forwarded-Proto` header from Koyeb's non-loopback proxy IP. The middleware does nothing.**

---

### BRANCH B — Middleware order relative to exception handler

**WHY 1B:** `UseForwardedHeaders` is placed after the exception handler block in `Program.cs`.
*Evidence: `Program.cs` lines 177–211 — `UseExceptionHandler` is registered conditionally before the startup scope, and `UseForwardedHeaders` follows after the startup scope.*

**WHY 2B:** The Microsoft documentation recommends placing `UseForwardedHeaders` either before or after diagnostics/error handling, but before all other middleware.
*Evidence: Microsoft docs example — `app.UseExceptionHandler("/Error"); app.UseForwardedHeaders(); app.UseHsts();` — shows exception handler first, then forwarded headers.*

**WHY 3B:** The current ordering is consistent with the documented pattern. `UseForwardedHeaders` runs before `UseCors`, `UseAuthentication`, and all route handlers that call `BuildRedirectUri`.
*Evidence: `Program.cs` lines 207–241 — `UseForwardedHeaders` (line 207) precedes `UseSerilogRequestLogging` (212), `UseCors` (213), `UseAuthentication` (214), route handler registration (234–240). No middleware before line 207 reads `request.Scheme`.*

-> **ROOT CAUSE B: NOT a root cause. Middleware order is correct per Microsoft documentation. This branch closes here.**

---

### BRANCH C — Cookie `Secure` flag dependent on scheme

**WHY 1C:** `HandleCallbackAsync` sets `Secure = httpContext.Request.IsHttps` on the session cookie.
*Evidence: `OAuthEndpoints.cs` lines 61–68 — `Secure = httpContext.Request.IsHttps`. If `request.Scheme` is `"http"`, `IsHttps` is `false`, and the cookie is set without `Secure` flag in production.*

**WHY 2C:** `request.IsHttps` derives from `request.Scheme`, which is `"http"` for the same reason as Branch A.
*Evidence: ASP.NET Core source — `IsHttps` is equivalent to `string.Equals(Scheme, "https", StringComparison.OrdinalIgnoreCase)`.*

**WHY 3C:** The `Secure` flag being absent means the session cookie will be transmitted over HTTP connections, making it vulnerable to interception. However, Koyeb enforces HTTPS externally, so in practice the browser only ever connects via HTTPS — but the cookie lacks the security attribute that prevents browser transmission over HTTP.

**WHY 4C:** The same root causes A1 and A2 that prevent scheme correction for `redirect_uri` also prevent scheme correction for `IsHttps`.

-> **ROOT CAUSE C (derived from A1+A2): Session cookie is missing `Secure` flag in production as a secondary consequence of the uncorrected `request.Scheme`. This is a security weakness that becomes relevant once the primary OAuth mismatch is resolved.**

---

### BRANCH D — Deployment status uncertainty

**WHY 1D:** Commit `2c7a43d` may or may not be deployed. The problem may persist on the live service regardless of code correctness.
*Evidence: CI pipeline triggers on `push` to `main` with `backend/**` path filter. Commit `2c7a43d` satisfies this. However, CI run status cannot be confirmed from git history. The smoke test only validates liveness, not scheme behavior.*

**WHY 2D:** Koyeb auto-deploys from GHCR `:latest` after CI pushes the image. There is a 90-second wait in the smoke test — potentially insufficient for cold startup under free-tier nano instance constraints.
*Evidence: `ci.yml` — `sleep 90` before smoke test. Free-tier nano instances may have slower container image pulls and startup.*

**WHY 3D:** Even if `2c7a43d` is deployed, roots A1 and A2 mean the deployed fix is insufficient regardless.

-> **ROOT CAUSE D: Deployment status is uncertain, but moot — the fix as written does not resolve the root causes even when deployed.**

---

## Phase 3: Cross-Validation

### Backward chain validation

| Root Cause | Forward trace | Validates? |
|------------|--------------|------------|
| A1: Koyeb omits `X-Forwarded-Proto` | No header -> middleware reads nothing -> `request.Scheme` stays `"http"` -> `BuildRedirectUri` produces `http://` -> mismatch | Yes |
| A2: No `KnownProxies` configured | Header present but from non-loopback IP -> middleware discards it -> `request.Scheme` stays `"http"` -> same result | Yes |
| A1 + A2 together | Both independently produce the same observable symptom. A1 is the deeper platform limitation; A2 is the code defect. | Consistent |
| C (cookie) | `request.Scheme == "http"` -> `IsHttps == false` -> `Secure = false` on cookie | Yes |

### Completeness check

All observed symptoms are explained:
- `redirect_uri` mismatch for GitHub: explained by A1+A2
- `redirect_uri` mismatch for Google: same branch
- Both providers affected simultaneously: expected — `BuildRedirectUri` is shared code with no provider-specific scheme handling
- `UseForwardedHeaders` fix did not resolve the issue: explained by A1 (no header to read) and A2 (middleware would ignore it anyway)

No contradictions between branches.

---

## Phase 4: Solution Development

### Solution A1 — Correct the scheme source (permanent fix, replaces `UseForwardedHeaders`)

**Problem:** Koyeb does not send `X-Forwarded-Proto`, so `UseForwardedHeaders` cannot work.

**Fix:** Do not rely on a header Koyeb does not send. Instead, derive `https` from a reliable source. Two viable approaches:

**Option 1 — Hardcode scheme from configuration (recommended for this deployment)**

Add a configuration key `OAuth:BaseUrl` (set via Koyeb environment variable) and use it in `BuildRedirectUri`:

```csharp
// In OAuthEndpoints.cs
private static string BuildRedirectUri(HttpContext httpContext, string provider)
{
    var baseUrl = httpContext.RequestServices
        .GetRequiredService<IConfiguration>()["OAuth:BaseUrl"]
        ?.TrimEnd('/');

    if (string.IsNullOrEmpty(baseUrl))
    {
        // Fallback to request-derived URL (development)
        var request = httpContext.Request;
        baseUrl = $"{request.Scheme}://{request.Host}";
    }

    return $"{baseUrl}/api/auth/oauth/{provider}/callback";
}
```

Set `OAuth__BaseUrl=https://api.theaugmentedcraftsman.christianborrello.dev` as a Koyeb environment variable.

This approach: deterministic, no proxy header dependency, testable, matches the registered callback URLs exactly.

**Option 2 — `ASPNETCORE_FORWARDEDHEADERS_ENABLED` environment variable**

Set this env var to `true` on Koyeb. This makes ASP.NET Core clear `KnownProxies`/`KnownNetworks` and enable `XForwardedFor | XForwardedProto`. However, this only helps if Koyeb actually sends `X-Forwarded-Proto`. Evidence shows Koyeb only documents `x-forwarded-for` and `x-forwarded-host`. Unless Koyeb confirmed to send `X-Forwarded-Proto`, this option is unreliable.

**Recommendation: Option 1. Configuration-driven base URL is the only approach with guaranteed correctness on Koyeb.**

Remove the `UseForwardedHeaders` call (it does nothing and is misleading):
```csharp
// Remove this block — Koyeb does not send X-Forwarded-Proto
// app.UseForwardedHeaders(new ForwardedHeadersOptions
// {
//     ForwardedHeaders = ForwardedHeaders.XForwardedProto
// });
```

---

### Solution A2 — If `UseForwardedHeaders` is kept, configure `KnownProxies` correctly

**Condition:** Only relevant if Koyeb is confirmed to send `X-Forwarded-Proto` (not currently evidenced).

If a future confirmation shows Koyeb does send the header, configure trusted proxy networks:

```csharp
// Option: trust all private networks (only if Koyeb proxy IPs are in RFC 1918 ranges)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto,
    KnownNetworks =
    {
        new IPNetwork(IPAddress.Parse("10.0.0.0"), 8),
        new IPNetwork(IPAddress.Parse("172.16.0.0"), 12),
        new IPNetwork(IPAddress.Parse("192.168.0.0"), 16)
    }
});
```

Or use `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` environment variable which clears the lists entirely.

**Note:** Without Solution A1, Solution A2 alone is insufficient because the header may not exist.

---

### Solution C — Fix `Secure` cookie flag (permanent fix)

**Problem:** `Secure = httpContext.Request.IsHttps` evaluates to `false` when scheme is not corrected.

**Fix:** Hardcode `Secure = true` in production, or derive it from configuration:

```csharp
httpContext.Response.Cookies.Append(SessionCookieName, result.SessionId!.Value.ToString(), new CookieOptions
{
    HttpOnly = true,
    Secure = !httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
    SameSite = SameSiteMode.Lax,
    MaxAge = TimeSpan.FromDays(30),
    Path = "/"
});
```

This is independent of scheme detection. The cookie is always `Secure` in non-Development environments.

---

### Solution D — Deploy verification (immediate action)

**Immediate mitigation:** Verify CI for commit `2c7a43d` completed on GitHub Actions. If it failed, trigger `workflow_dispatch` manually. After deploying Solution A1, verify with:

```bash
curl -v "https://api.theaugmentedcraftsman.christianborrello.dev/api/auth/oauth/github" 2>&1 | grep -i "location"
```

The `Location` header on the redirect must begin with `https://github.com/login/oauth/authorize?...&redirect_uri=https%3A%2F%2Fapi.theaugmentedcraftsman...`.

---

### Solution — Early detection

Add a startup validation that logs the effective base URL for OAuth to make scheme issues immediately visible:

```csharp
// In startup, after app is built
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var oauthBase = app.Configuration["OAuth:BaseUrl"] ?? "(not configured — using request scheme)";
logger.LogInformation("OAuth redirect base URL: {BaseUrl}", oauthBase);
```

---

## Phase 5: Prevention Strategy

### Systemic factors

| Factor | Prevention |
|--------|-----------|
| Assumption that reverse proxies forward `X-Forwarded-Proto` | Always verify per-platform header documentation before relying on forwarded headers. Koyeb only documents `x-forwarded-for` and `x-forwarded-host`. |
| ASP.NET Core breaking change in 8.0.17 (proxy trust) | Include KnownProxies configuration review in the deployment checklist for any new hosting environment |
| Scheme-dependent logic using `request.Scheme` without explicit override | Add a test that asserts the OAuth callback URL starts with `https://` when the `OAuth:BaseUrl` config is set |
| Cookie `Secure` flag depending on runtime scheme detection | Production cookies must always set `Secure = true` explicitly — never derive from `IsHttps` when scheme may not reflect reality |

### Recommended action items (prioritized)

| Priority | Action | Type |
|----------|--------|------|
| P0 | Add `OAuth__BaseUrl` env var to Koyeb, update `BuildRedirectUri` to use it | Permanent fix |
| P0 | Hardcode `Secure = true` for session cookie in non-Development | Permanent fix (security) |
| P1 | Remove or replace `UseForwardedHeaders` call (dead code, misleading) | Cleanup |
| P1 | Confirm Koyeb CI for `2c7a43d` completed; redeploy with solution | Immediate mitigation |
| P2 | Write integration test asserting callback URL scheme equals configured base URL scheme | Prevention |
| P3 | Investigate whether Koyeb sends `X-Forwarded-Proto` with a request inspector endpoint | Knowledge gap |

---

## Summary

Three root causes are identified:

**ROOT CAUSE A1 (primary):** Koyeb does not inject `X-Forwarded-Proto` into requests forwarded to the container. The platform only documents `x-forwarded-for` and `x-forwarded-host`. The `UseForwardedHeaders` fix is built on a false premise — there is no header to read.

**ROOT CAUSE A2 (compounding):** Even if Koyeb did send `X-Forwarded-Proto`, the current `UseForwardedHeaders` configuration omits `KnownProxies`/`KnownNetworks`. Since .NET 8.0.17, ASP.NET Core silently discards forwarded headers from non-loopback proxy IPs unless explicitly trusted. The middleware does nothing for Koyeb's non-loopback proxy.

**ROOT CAUSE C (secondary, security):** `request.IsHttps` returns `false` for the same underlying reason, causing session cookies to be set without the `Secure` flag in production.

The fix in commit `2c7a43d` is insufficient on its own. The correct resolution is to introduce a configuration-driven `OAuth:BaseUrl` that explicitly provides the `https://` scheme, independent of any proxy header.

---

*Sources consulted:*
- [Microsoft: Configure ASP.NET Core to work with proxy servers and load balancers](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
- [Microsoft: Breaking change — Forwarded Headers Middleware ignores X-Forwarded-* headers from unknown proxies](https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/8.0/forwarded-headers-unknown-proxies)
- [Koyeb: Edge Network documentation](https://www.koyeb.com/docs/reference/edge-network)
- [Koyeb Community: WebSocket/HTTP-2 thread (forwarded headers context)](https://community.koyeb.com/t/websocket-connection-failed-when-protocol-is-http-2/3101)
- [Nestenius: Configuring ASP.NET Core Forwarded Headers Middleware](https://nestenius.se/net/configuring-asp-net-core-forwarded-headers-middleware/)
- [Auth0: ASP.NET Core Authentication Behind Proxies](https://auth0.com/blog/aspnet-core-authentication-behind-proxies/)

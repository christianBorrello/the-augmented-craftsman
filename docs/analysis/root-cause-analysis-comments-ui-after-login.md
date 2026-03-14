# Root Cause Analysis: Comments UI Does Not Update After OAuth Login

**Date**: 2026-03-14
**Analyst**: Rex (Root Cause Analysis Specialist)
**Methodology**: Toyota 5 Whys, Multi-Causal
**Scope**: Frontend comments section UI state after successful OAuth login on the blog post page

---

## Problem Statement

After a user successfully completes OAuth login (GitHub or Google), the browser is redirected back to the blog post URL (e.g., `https://theaugmentedcraftsman.christianborrello.dev/blog/some-slug#comments`). However, the comments section continues to show the unauthenticated view — the OAuth login buttons are still visible instead of the compose form that should appear for authenticated users.

The OAuth login itself succeeds: the session cookie is set correctly by the backend. The symptom is a UI rendering failure, not an authentication failure.

**Scope boundary:** This analysis covers the frontend rendering path after a successful OAuth round-trip. It does not re-examine OAuth redirect_uri mismatch (covered in `root-cause-analysis-oauth-redirect-uri.md`) or session cookie security properties.

---

## Phase 1: Evidence Collection

### Source Files Examined

| File | Relevant Finding |
|------|-----------------|
| `frontend/astro.config.mjs` | `output: 'static'`, `prefetch: true`, `adapter: vercel()` — entire site is SSG by default |
| `frontend/src/pages/blog/[slug].astro` | Uses `getStaticPaths()` — no `export const prerender = false`. Page is a static HTML file. |
| `frontend/src/layouts/BaseLayout.astro` | Includes `<ClientRouter />` — Astro View Transitions are active site-wide |
| `frontend/src/components/CommentList.astro` | `export const prerender = false` — server island, fetched at request time via `/_server-islands/CommentList` |
| `frontend/src/components/CommentList.astro` | Reads session from `Astro.request.headers.get('cookie')` and calls `GET /api/auth/session` with that cookie |
| `frontend/src/components/Header.astro` | `initSession()` calls `GET /api/auth/session` with `credentials: 'include'` via client-side JS; re-runs on `astro:after-swap` |
| `frontend/src/components/CommentList.astro` | Has a `<script>` that scrolls to `#comments` on load; no script that triggers a re-fetch or re-render of the island |
| Git commit `dec2401` | Fixed wrong `returnUrl` by passing `canonicalUrl` as prop (was using `Astro.url.href` which resolved to the island endpoint URL) |
| Git commit `0de45bb` | Fixed `#comments` fragment not being passed through `returnUrl` by using `encodeURIComponent` |

### Rendering Architecture

```
[slug].astro  (output: static, getStaticPaths)
  └── CommentList server:defer  (prerender = false, server island)
        └── Fetched via: /_server-islands/CommentList?slug=...&pageUrl=...
              └── Reads: Astro.request.headers.get('cookie')
              └── Calls: GET /api/auth/session { headers: { cookie } }
```

### OAuth Flow (post-fix state, commits dec2401 + 0de45bb)

```
1. User on /blog/some-slug (static page loaded)
2. Server Island fetched: /_server-islands/CommentList (no session cookie yet)
   → renders: unauthenticated view (OAuth login buttons)
3. User clicks GitHub login button:
   href = ${API}/api/auth/oauth/github?returnUrl=https://.../#comments
4. Backend: GET /api/auth/oauth/github → redirects to GitHub
5. GitHub: user authenticates → callback to backend
6. Backend: GET /api/auth/oauth/github/callback
   → creates session, sets reader_session cookie
   → Results.Redirect(returnUrl) = redirects to https://theaugmentedcraftsman.../blog/some-slug#comments
7. Browser lands on /blog/some-slug (now with reader_session cookie)
```

### Critical: How the Browser Loads the Page in Step 7

`astro.config.mjs` has `prefetch: true` and `BaseLayout.astro` includes `<ClientRouter />`. With Astro View Transitions (the `ClientRouter`), internal navigation is intercepted and performed client-side via `fetch` + DOM swap, without a full browser navigation.

However, step 7 is a **redirect from the backend** — the backend sends `HTTP 302 Location: /blog/some-slug#comments`. This is a **cross-origin redirect** (from `api.theaugmentedcraftsman.christianborrello.dev` to `theaugmentedcraftsman.christianborrello.dev`). The browser performs a **full page load**, not a client-side navigation, because:
- The redirect originates from a different origin than the current page
- The browser has no View Transition intercept active for cross-origin redirects

Therefore, in step 7 the browser performs a full navigation to `/blog/some-slug`.

### What Happens When the Static Page Loads (Step 7)

The static HTML for `/blog/some-slug` is served from Vercel's CDN. This HTML contains:

```html
<astro-server-island uid="CommentList" ...>
  <template><!-- fallback skeleton --></template>
</astro-server-island>
```

The Astro client runtime then fetches the server island:
```
GET /_server-islands/CommentList?slug=some-slug&pageUrl=https://...&...
Cookie: reader_session=<uuid>
```

The `CommentList` server island reads the `reader_session` cookie via `Astro.request.headers.get('cookie')`, calls `/api/auth/session`, and should receive `authenticated: true`. It then renders the compose form.

**This should work.** The fundamental mechanism is sound. The server island re-renders on every request because it has `prerender = false`.

### Identifying the Actual Failure Mode(s)

To narrow the actual failure mode, the following conditions must be checked:

#### Condition A: Does the Server Island request include the session cookie?

The `reader_session` cookie has:
- `SameSite=Lax`
- `Secure=<depends on isProduction flag>`
- `Path=/`
- `Domain` not set (defaults to backend API domain: `api.theaugmentedcraftsman.christianborrello.dev`)

**Critical finding:** The cookie is set by the **backend** (`api.theaugmentedcraftsman.christianborrello.dev`) via `Set-Cookie`. The server island is fetched from the **frontend** (`theaugmentedcraftsman.christianborrello.dev`) at the endpoint `/_server-islands/CommentList`. The frontend's SSR runtime (Vercel) makes a **server-to-server request** to itself to fetch the island. The cookie lives in the **browser's cookie jar** for the backend domain, not the frontend domain.

The Astro server island mechanism works as follows: the **browser** requests `/_server-islands/CommentList` from the Vercel edge. The Vercel edge runs the Astro server island code. The island reads `Astro.request.headers.get('cookie')` — which is the cookie header **the browser sends in its request to Vercel**, not the backend cookie jar.

Since `reader_session` is scoped to `api.theaugmentedcraftsman.christianborrello.dev` (set via `Set-Cookie` from that domain), the browser will **not** include it in requests to `theaugmentedcraftsman.christianborrello.dev`. The browser's Same-Origin cookie policy prevents cross-domain cookie sharing.

This means `Astro.request.headers.get('cookie')` inside `CommentList` will not include `reader_session` — it will be empty or contain only cookies set by the frontend domain.

**Evidence:** `OAuthEndpoints.cs` line 67–74 — `httpContext.Response.Cookies.Append(SessionCookieName, ...)` does not set the `Domain` attribute. Without an explicit `Domain`, the browser scopes the cookie to the exact domain that set the response header — `api.theaugmentedcraftsman.christianborrello.dev`.

#### Condition B: The Header session indicator does update

`Header.astro` uses client-side JavaScript (`fetch(api + '/api/auth/session', { credentials: 'include' })`). This works because:
- It is a **cross-origin fetch** from `theaugmentedcraftsman.christianborrello.dev` to `api.theaugmentedcraftsman.christianborrello.dev`
- `credentials: 'include'` sends cookies scoped to the target domain
- CORS allows credentials from the allowed origin

So the header navbar correctly shows the authenticated state (avatar, Sign Out button). The comments section does not update because it uses a different mechanism.

#### Condition C: The `Secure` cookie flag issue

From the prior RCA (`root-cause-analysis-oauth-redirect-uri.md`), the session cookie may be set with `Secure = false` in production due to the uncorrected `request.Scheme`. If `Secure = false` is present on a cookie sent over HTTPS, the browser will store it but the behavior around cross-origin requests may vary by browser. This is a secondary concern — the primary block is the domain mismatch.

---

## Phase 2: Toyota 5 Whys Analysis

### BRANCH A — Server island does not receive the session cookie

**WHY 1A:** After OAuth login, `CommentList` renders the unauthenticated view despite the session cookie being set.
*Evidence: `CommentList.astro` line 18 — `fetch(\`${API}/api/auth/session\`, { headers: { cookie } })`. If `cookie` is empty, `CheckSession` returns `authenticated: false`. The unauthenticated branch renders the login buttons.*

**WHY 2A:** The `cookie` variable inside the server island is empty (or does not contain `reader_session`).
*Evidence: `CommentList.astro` line 14 — `const cookie = Astro.request.headers.get('cookie') || ''`. The request to `/_server-islands/CommentList` is made by the browser to the Vercel frontend domain. The browser's cookie jar for the frontend domain does not contain `reader_session`.*

**WHY 3A:** `reader_session` is absent from the browser's cookie jar for the frontend domain because it was set by the backend domain.
*Evidence: `OAuthEndpoints.cs` lines 67–74 — `httpContext.Response.Cookies.Append(SessionCookieName, result.SessionId!.Value.ToString(), new CookieOptions { ... })` — no `Domain` attribute specified. Without `Domain`, the browser scopes the cookie to the exact origin of the response: `api.theaugmentedcraftsman.christianborrello.dev`. Browser Same-Origin policy prevents this cookie from being sent in requests to `theaugmentedcraftsman.christianborrello.dev`.*

**WHY 4A:** The session cookie domain was not designed to be shared across frontend and backend origins.
*Evidence: The two services run on separate domains. The frontend is `theaugmentedcraftsman.christianborrello.dev` (Vercel) and the backend is `api.theaugmentedcraftsman.christianborrello.dev` (Koyeb). The session cookie strategy was designed around server-side rendering where the frontend server would forward the cookie to the backend — but the cookie must first reach the frontend server, which requires it to be visible in the browser's request to the frontend domain.*

**WHY 5A:** The architecture assumes the browser will have the `reader_session` cookie available to the frontend Vercel edge, but no mechanism was implemented to bridge the cookie from the backend domain to the frontend domain. The server island session-check strategy requires the session cookie to be readable on the frontend domain, which was never satisfied by the current domain-siloed cookie setup.
*Evidence: No cross-domain cookie sharing mechanism (shared parent domain, session token in URL query parameter, or separate session check endpoint on the frontend domain) exists in the codebase.*

-> **ROOT CAUSE A: The `reader_session` cookie is set by `api.theaugmentedcraftsman.christianborrello.dev` without a `Domain` attribute, scoping it to the backend origin only. The Astro server island (`CommentList`) runs on the Vercel frontend and reads `Astro.request.headers.get('cookie')` — which will never contain `reader_session` because the browser does not send backend-domain cookies in requests to the frontend domain. The server island always sees an unauthenticated state.**

---

### BRANCH B — Header works but comments don't: two different session-check mechanisms

**WHY 1B:** The navbar header correctly updates to show the authenticated user after login, but the comments section shows the unauthenticated view.
*Evidence: `Header.astro` — `initSession()` fetches `api + '/api/auth/session'` with `credentials: 'include'` from the browser. This is a cross-origin fetch *to the backend domain* — the browser sends the `reader_session` cookie to its owner domain, which is correct. `CommentList` fetches session from within the Vercel edge environment — `Astro.request.headers.get('cookie')` captures what the browser sends to the frontend domain, not the backend domain.*

**WHY 2B:** Two distinct session-check patterns exist in the codebase: one client-side (Header, correct) and one server-island-side (CommentList, broken by domain mismatch).
*Evidence: Header uses `credentials: 'include'` in a browser fetch to the backend API. CommentList uses `Astro.request.headers.get('cookie')` + server-side fetch forwarding. The first pattern works because the browser sends backend cookies to the backend. The second pattern fails because the browser does not send backend cookies to the frontend edge.*

**WHY 3B:** The two patterns were developed without recognizing that they operate under different cookie visibility constraints.
*Evidence: The CommentList server-island pattern was likely modeled on a same-domain or monolithic deployment assumption, where the frontend and backend share a domain (and therefore a cookie jar). The actual deployment uses separate domains.*

**WHY 4B:** No architectural review compared the session-check mechanisms across components before the server island approach was adopted.
*Evidence: `Header.astro` works; `CommentList.astro` does not. Both were introduced together in commit `b6eab89` ("feat(frontend): add likes, comments, share, and session UI"). The inconsistency was not caught because the header visually appeared to work (client-side fetch to backend domain is correct), masking the underlying cross-domain incompatibility in the server island path.*

**WHY 5B:** The server island session propagation pattern was adopted without validating that the browser would include the session cookie in the request to the Vercel edge. This is a design assumption gap: server islands in a split-domain architecture require session information to arrive through the browser request to the island's host domain, which is not guaranteed when the session cookie belongs to a different domain.
*Evidence: Astro server island documentation notes that server islands render on the server using the incoming request context (`Astro.request`). The incoming request to the island endpoint comes from the browser to Vercel — the browser will only attach cookies for the Vercel domain, not the Koyeb backend domain.*

-> **ROOT CAUSE B: The server island session-check pattern (`Astro.request.headers.get('cookie')`) is architecturally incompatible with a split-domain deployment where the session cookie belongs to the backend domain. This design gap was not identified when the pattern was adopted, because the header (which uses the correct client-side fetch pattern) created a false impression that session state was accessible everywhere.**

---

### BRANCH C — Static page caching: could Vercel serve a stale island?

**WHY 1C:** After the OAuth redirect, the browser performs a full page load. Could Vercel's CDN serve a cached version of the server island that was rendered before login?
*Evidence: `CommentList.astro` has `export const prerender = false`. This marks the island as server-rendered, not statically cacheable by default.*

**WHY 2C:** Vercel's edge caches server-rendered responses based on response headers (`Cache-Control`, `Vary`). Astro server islands do not emit `Cache-Control: public` by default; the response is typically `no-store` or uncached.
*Evidence: Astro documentation — `server:defer` islands are rendered per-request; the framework does not add caching headers that would cause CDN caching of personalized content. Hypothesis requires verification via response headers in a live request.*

**WHY 3C:** Even if Vercel cached an unauthenticated island response, the cache key would need to vary on the `Cookie` header (or lack thereof). However, since the cookie is absent from the request to the frontend domain (Root Cause A), the cache key would be the same regardless of whether the user is logged in — the request looks identical to Vercel.
*Evidence: Without the `reader_session` cookie reaching the Vercel edge (Root Cause A), the island request is indistinguishable from an anonymous request. Even a correctly implemented cache `Vary: Cookie` header would not help because the cookie never arrives.*

-> **ROOT CAUSE C (derived from A): CDN caching is not an independent root cause. Even if caching were present, it is downstream of Root Cause A — the cookie absence makes the island request look anonymous in all cases. This branch does not produce an independent root cause but confirms that Root Cause A is the fundamental blocker.**

---

### BRANCH D — `encodeURIComponent` fix: was the returnUrl actually reaching the page?

**WHY 1D:** After commits `dec2401` and `0de45bb`, the `returnUrl` and `#comments` anchor should reach the correct page. Is the redirect working and landing the user on the post page?
*Evidence: Commit `0de45bb` — the `#comments` fragment is now encoded inside `returnUrl` via `encodeURIComponent(pageUrl + '#comments')`. The backend `HandleCallbackAsync` uses `state` as the raw `returnUrl` and calls `Results.Redirect(returnUrl)`. If `returnUrl` includes the encoded `#comments`, the backend will redirect to the URL with the hash.*

**WHY 2D:** HTTP 302 redirects do not preserve URL fragments (hashes) — the fragment is a client-side construct. When the backend redirects to `https://.../#comments`, the `#comments` hash is preserved in the Location header and the browser honors it. This is standard HTTP behavior.
*Evidence: The `#comments` fragment is a URL component that the browser retains from the Location header. The backend is redirecting to the literal string containing `#comments` after decoding `state`. The scroll-to-comments script in `CommentList` runs on island injection to handle this.*

**WHY 3D:** The user does land on the correct page. The scroll behavior is fixed by `0de45bb`. The remaining issue is that the comments UI does not show the authenticated state — which is Root Cause A/B, not a redirect failure.

-> **ROOT CAUSE D: NOT an independent root cause. The redirect mechanism is now correct after commits dec2401 and 0de45bb. The failure to show authenticated state is attributable to Root Causes A and B only.**

---

## Phase 3: Cross-Validation

### Backward chain validation

| Root Cause | Forward trace | Validates? |
|------------|--------------|------------|
| A: Cookie scoped to backend domain, not sent to frontend | Browser requests `/_server-islands/CommentList` → no `reader_session` cookie in request → `Astro.request.headers.get('cookie')` is empty → `/api/auth/session` called without session cookie → returns `authenticated: false` → unauthenticated view rendered | Yes |
| B: Design gap — server island uses wrong pattern for cross-domain auth | Same forward chain as A — no cookie → no authenticated state → login buttons shown | Yes (same observable symptom, different analytical level) |
| A + B together | A explains the mechanism; B explains why the mechanism was chosen despite being incompatible with the deployment | Consistent, no contradiction |
| C (caching) | Downstream of A; confirmed non-independent | Consistent |
| D (redirect) | Already fixed; confirmed non-contributing | Consistent |

### Completeness check

All observed symptoms explained:
- Comments show unauthenticated UI after login: explained by A (cookie not forwarded) + B (wrong pattern)
- Header navbar correctly updates: not a contradiction — header uses client-side fetch to backend domain (correct pattern), island uses server-side cookie forwarding (broken by domain mismatch)
- Both GitHub and Google OAuth affected: expected — both use the same `CommentList` island and same cookie mechanism
- Issue persists after commits dec2401 and 0de45bb: expected — those commits fixed redirect URL construction and scroll behavior, not the cookie propagation problem

No contradictions between branches.

---

## Phase 4: Solution Development

### Solution A — Set the session cookie on the shared parent domain (permanent fix, recommended)

**Problem:** `reader_session` is scoped to `api.theaugmentedcraftsman.christianborrello.dev` and never reaches the browser's cookie jar for `theaugmentedcraftsman.christianborrello.dev`.

**Fix:** Set the `Domain` attribute on the session cookie to the shared parent domain (`.christianborrello.dev`), making the cookie visible to both the frontend and backend subdomains.

In `OAuthEndpoints.cs`:
```csharp
httpContext.Response.Cookies.Append(SessionCookieName, result.SessionId!.Value.ToString(), new CookieOptions
{
    HttpOnly = true,
    Secure = isProduction,
    SameSite = SameSiteMode.Lax,
    MaxAge = TimeSpan.FromDays(30),
    Path = "/",
    Domain = isProduction ? ".christianborrello.dev" : null  // shared parent domain in production
});
```

With this change:
- The browser stores `reader_session` for `.christianborrello.dev`
- It sends the cookie to both `theaugmentedcraftsman.christianborrello.dev` (frontend) and `api.theaugmentedcraftsman.christianborrello.dev` (backend)
- The server island's `Astro.request.headers.get('cookie')` receives `reader_session`
- `/api/auth/session` returns `authenticated: true`
- The compose form renders

**Note:** `SameSite=Lax` is compatible with cross-subdomain requests from the same eTLD+1. `HttpOnly` is preserved. This is the minimal-change, architecturally correct fix.

**Also requires:** The `Secure` cookie flag must be `true` in production (independent of scheme detection — see Root Cause C from `root-cause-analysis-oauth-redirect-uri.md`). The current code already sets `Secure = isProduction` (using the `IWebHostEnvironment.IsDevelopment()` check), so this is already correct in the current code.

---

### Solution B — Replace server island session check with client-side fetch (alternative permanent fix)

**Problem:** The server-island session check pattern is architecturally mismatched with the split-domain deployment.

**Fix:** Remove the server-side session fetch from `CommentList`. Render the island without authentication state, then hydrate the authentication-dependent UI client-side using the same `credentials: 'include'` pattern as `Header.astro`.

This approach:
- Makes `CommentList` always render the unauthenticated shell (skeleton + login buttons) on the server
- Adds a `<script>` inside the island that calls `/api/auth/session` with `credentials: 'include'` on mount
- Updates the DOM to show the compose form if authenticated

**Trade-off:** This adds client-side JS to `CommentList` (currently zero JS in the island body, only a scroll script). It adds a second round-trip on authenticated page load. However, it eliminates the cross-domain cookie dependency entirely.

**Recommendation:** Solution A is preferred — it is a single-line backend change that fixes the root cause without altering the frontend rendering architecture.

---

### Solution C — Session token in URL query parameter (not recommended)

**Problem:** Same as above.

**Approach:** After OAuth login, include a short-lived session token as a URL query parameter, which the static page extracts and passes as a prop to the island endpoint.

**Rejected:** This approach exposes session tokens in URLs (browser history, server logs, referrer headers). Security risk outweighs simplicity benefit.

---

### Early detection

Add a startup validation log in `OAuthEndpoints` that warns when the cookie domain is unconfigured in production:

```csharp
// In Program.cs startup section
if (!app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Session cookie will be set for domain: {Domain}",
        app.Configuration["OAuth:CookieDomain"] ?? "(not configured — backend domain only)");
}
```

A smoke test after deployment could verify the `Set-Cookie` response header includes the correct `Domain` attribute.

---

## Phase 5: Prevention Strategy

### Systemic factors

| Factor | Prevention |
|--------|-----------|
| Split-domain deployment assumption gap | Document in `BEST_PRACTICES.md` or architecture notes: session cookies in split-domain deployments must set `Domain` to the shared parent eTLD+1, or use a client-side auth check pattern |
| Two inconsistent session-check patterns (Header vs CommentList) | Establish a single, documented session-check pattern for the project; audit all new components against it during code review |
| Server island session propagation untested | Add an integration/E2E test that: loads a blog post page after OAuth login and asserts the comment compose form is visible (not the login buttons) |
| Cookie `Domain` omission in cross-subdomain architectures | Add a deployment checklist item: "Verify session cookie `Domain` attribute is set for all subdomains that need to read it" |

### Recommended action items (prioritized)

| Priority | Action | Type |
|----------|--------|------|
| P0 | Set `Domain = ".christianborrello.dev"` on `reader_session` cookie in production (`OAuthEndpoints.cs`) | Permanent fix |
| P0 | Verify session cookie `Secure` is `true` in production (already set via `isProduction` check — confirm this is deployed) | Verification |
| P1 | Add E2E test asserting comment compose form is visible after OAuth login | Prevention |
| P2 | Document split-domain cookie architecture decision in project docs | Knowledge transfer |
| P3 | Audit all components for session-check pattern consistency (Header vs CommentList approaches) | Systemic improvement |

---

## Summary

Two root causes identified:

**ROOT CAUSE A (primary, mechanism):** The `reader_session` cookie is set by `api.theaugmentedcraftsman.christianborrello.dev` without a `Domain` attribute, scoping it to the backend origin. The Astro server island (`CommentList`) runs on the Vercel frontend edge and reads session state from the incoming request cookie header. The browser never sends the backend-scoped cookie in requests to the frontend domain. The server island therefore always sees an unauthenticated request and renders the login buttons — regardless of whether the user is actually logged in.

**ROOT CAUSE B (design gap, why the pattern was chosen):** The server island session propagation pattern (`Astro.request.headers.get('cookie')` + server-side session check) was adopted without recognizing that it requires the session cookie to be visible on the frontend domain. The Header navbar uses the architecturally correct pattern (client-side `credentials: 'include'` fetch to the backend domain) but this was not recognized as the canonical approach. The inconsistency masked the incompatibility during development.

**The fix in commits `dec2401` and `0de45bb` is correct** — they fixed the OAuth redirect URL construction and scroll behavior, which were real bugs. However, neither commit addresses the cookie domain issue. The user is now correctly redirected to the post page after login, but the server island still sees no cookie and renders the unauthenticated state.

**Resolution:** Set `Domain = ".christianborrello.dev"` on the `reader_session` cookie in production. This single backend change bridges the cookie from the backend domain to the frontend domain, allowing the server island to receive the session cookie and render the authenticated compose form.

---

*Analysis based on:*
- `frontend/src/components/CommentList.astro` — server island, session read, OAuth button hrefs
- `frontend/src/pages/blog/[slug].astro` — static page, `getStaticPaths`, `server:defer` usage
- `frontend/src/layouts/BaseLayout.astro` — `ClientRouter` (View Transitions) active
- `frontend/astro.config.mjs` — `output: 'static'`, `prefetch: true`
- `frontend/src/components/Header.astro` — client-side session check pattern (correct)
- `backend/src/TacBlog.Api/Endpoints/OAuthEndpoints.cs` — cookie set without `Domain`
- `backend/src/TacBlog.Api/Program.cs` — CORS, allowed origins
- Git commits: `b6eab89` (initial comments/session UI), `dec2401` (canonicalUrl fix), `0de45bb` (encodeURIComponent fix)
- Prior RCA: `docs/analysis/root-cause-analysis-oauth-redirect-uri.md`

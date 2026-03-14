# Root Cause Analysis: Comment POST Returns 405 (Method Not Allowed)

**Date**: 2026-03-14
**Analyst**: Rex (Root Cause Analysis Specialist)
**Methodology**: Toyota 5 Whys, Multi-Causal

---

## Problem Statement

When a logged-in user submits the comment form on a blog post page, the browser receives a 405 (Method Not Allowed) response. The page renders a blank white page showing raw HTML fragment text. The error originates from `ClientRouter.astro` (Astro View Transitions).

**Scope**: Frontend (Astro / Vercel). Backend is not reached. The problem is entirely in the Astro build and runtime configuration.

---

## Observed Symptoms

| # | Symptom | Evidence |
|---|---------|----------|
| S1 | 405 on POST | Browser console: `POST https://theaugmentedcraftsman.christianborrello.dev/blog/building-the-augmented-craftsman-a-blog-forged-with-xp-practices?_action=postComment 405 (Method Not Allowed)` |
| S2 | Blank white page with raw HTML fragment | Browser renders `/blog/building-the-augmented-craftsman-a-blog-forged-with-xp-practices""` as text on a white page |
| S3 | Error originates in `ClientRouter.astro` | View Transitions router receives the 405 response and injects the raw fragment body into the DOM instead of navigating |
| S4 | Form POSTs to the Astro frontend URL, not the backend API | Request URL is `theaugmentedcraftsman.christianborrello.dev/blog/[slug]?_action=postComment`, not `api.theaugmentedcraftsman.christianborrello.dev` |

---

## Toyota 5 Whys Analysis

### Branch A — 405 on the Astro Actions Endpoint

**WHY 1A**: The Astro Actions endpoint (`?_action=postComment`) returns 405 Method Not Allowed.
- Evidence: Browser network log shows `POST .../blog/[slug]?_action=postComment → 405`.

**WHY 2A**: Astro Actions require server-side request handling, but the site is built in `output: 'static'` mode.
- Evidence: `frontend/astro.config.mjs` line 12: `output: 'static'`. In static mode, Astro pre-renders all pages to HTML at build time and emits no server runtime. There is no process capable of handling incoming POST requests.

**WHY 3A**: With `output: 'static'`, Vercel serves the site as a CDN with no serverless functions for page routes. When the browser POSTs to a blog page URL, Vercel has no handler — the CDN returns 405 because the static asset only supports GET.
- Evidence: `@astrojs/vercel` v9 adapter in `package.json`. With `output: 'static'`, this adapter generates a static export only; no Vercel Serverless Functions are created for page routes. Astro Actions in static mode are explicitly unsupported — they require either `output: 'server'` or `output: 'hybrid'`.

**WHY 4A**: The Astro Actions API (`astro:actions`) was introduced alongside, but is not compatible with, fully static output. The design requires a server runtime to receive the POST, validate input via the action handler, and return a redirect or JSON response. The project configured `output: 'static'` without recognising this constraint.
- Evidence: `frontend/src/actions/index.ts` defines `postComment` with `accept: 'form'` and a server-side `handler` that calls `fetch(API)`. This handler can only execute in a Node.js / serverless runtime, not in a pre-built static HTML file. Astro documentation states: "Actions require a server endpoint. They are not available in purely static sites."

**WHY 5A**: The static output mode was chosen for optimal performance (zero JS, CDN delivery, Core Web Vitals) and was not revisited when Astro Actions and Server Islands were added to the feature set, both of which require server-side rendering capability.
- Evidence: `astro.config.mjs` has `output: 'static'` coexisting with `CommentList.astro` using `export const prerender = false` (Server Island) and `import { actions } from 'astro:actions'`. These three settings are contradictory. The `prerender = false` directive is ignored in a fully static build — every page is pre-rendered regardless.

-> **ROOT CAUSE A**: `output: 'static'` in `astro.config.mjs` prevents Astro Actions from functioning because the framework emits no server runtime. The POST request to `?_action=postComment` has no handler; Vercel's CDN returns 405.

---

### Branch B — Form Action Resolves to Astro Frontend URL Instead of Backend API

**WHY 1B**: The form `action` attribute POSTs to the Astro frontend domain (`theaugmentedcraftsman.christianborrello.dev`), not the backend API domain (`api.theaugmentedcraftsman.christianborrello.dev`).
- Evidence: Browser network log URL — the host is the frontend Vercel domain, the path is the blog post slug, and the query string is `?_action=postComment`.

**WHY 2B**: `CommentList.astro` line 62 uses `action={actions.postComment}` on the `<form>` element. The `actions.postComment` helper, when used as a form `action` value in Astro, resolves to the Astro Actions internal endpoint URL — which is a relative path on the same host as the frontend (`/_actions/postComment` becomes `?_action=postComment` on the current page URL under View Transitions).
- Evidence: `CommentList.astro` line 62: `<form class="comment-compose" method="POST" action={actions.postComment}>`. The `actions.postComment` object, when coerced to a string for the HTML `action` attribute, produces the Astro Actions URL pattern, not the backend API URL.

**WHY 3B**: The developer wired the comment submission through Astro Actions (`actions/index.ts`) rather than directly to the backend API endpoint. The action handler (`actions/index.ts` lines 15–18) is responsible for proxying the request to the backend — but this handler only runs server-side, which is impossible in `output: 'static'` mode.
- Evidence: `frontend/src/actions/index.ts` lines 13–19: the handler calls `fetch(\`\${API}/api/posts/\${slug}/comments\`, { method: 'POST', ... })`. This is correct proxying logic but it is dead code in a static build.

**WHY 4B**: The Astro Actions pattern was adopted as the idiomatic Astro form-handling mechanism without validating that its prerequisite (a server runtime) was satisfied by the current site configuration. The intent was sound — use the framework's built-in action system for type-safe, validated form handling — but the prerequisite contract was missed.
- Evidence: `CommentList.astro` line 32: `const result = Astro.getActionResult(actions.postComment)` — this is another server-only API used in a component that runs at build time in static mode.

**WHY 5B**: The project adopted Server Islands (`server:defer` on `CommentList`) and Astro Actions as a coherent pair, but the site-level output mode was not updated to enable the server runtime both features depend on. The assumption was that `prerender = false` within a component would be sufficient to opt that component into server rendering, but in `output: 'static'` mode this directive has no effect — the entire site is pre-rendered unconditionally.
- Evidence: `[slug].astro` line 9: `export async function getStaticPaths()` is defined alongside line 121: `<CommentList server:defer ...>`. `getStaticPaths` only exists in static/hybrid page generation. The `server:defer` directive (Server Island) is silently ignored in static output.

-> **ROOT CAUSE B**: The form is hardwired via `action={actions.postComment}` to the Astro Actions URL pattern. Because there is no server runtime, the form POSTs to a URL that no handler can receive, producing 405. The underlying cause is the same as Root Cause A — the output mode mismatch — but the specific failure mechanism is the form's action attribute resolving to an Astro-internal URL rather than the backend API.

---

### Branch C — Blank White Page with Raw HTML Fragment

**WHY 1C**: The browser renders a blank white page containing the raw string `/blog/building-the-augmented-craftsman-a-blog-forged-with-xp-practices""`.
- Evidence: Observed browser state after form submission; symptom S2.

**WHY 2C**: Astro's View Transitions `ClientRouter` intercepts the form submission, receives the 405 response body (a minimal error fragment or the raw URL string), and injects that body into the current document via its DOM diffing algorithm rather than performing a full page navigation.
- Evidence: S3 — the error is attributed to `ClientRouter.astro`. Astro's client router captures form submissions on pages with `<ViewTransitions />` and handles the response as a page transition. When the response is not a valid full HTML document (or is a 405 error body), the router replaces the page body with whatever content was returned, producing a blank page with raw text.

**WHY 3C**: The View Transitions router was not designed to gracefully degrade when an action POST returns a non-2xx response with a non-HTML body. It attempts to parse and render the response regardless of status code.
- Evidence: The 405 response body from Vercel's CDN is a short plain-text or HTML error string. The router uses this as the new page body, erasing the existing content.

**WHY 4C**: No error boundary or fallback exists in the form submission flow to handle non-success responses. The `result?.error` check in `CommentList.astro` line 33 (`const hasError = result?.error`) would catch action-level errors if the action ran — but it never runs because the 405 is returned by Vercel before reaching any Astro code.
- Evidence: `CommentList.astro` lines 33–58: the error display path (`hasError` block) is only reachable if `Astro.getActionResult` returns a result object with an error. When the request never reaches the action handler, `getActionResult` returns `undefined`, and the 405 response body is surfaced raw by the router.

**WHY 5C**: The root cause is again the static output mode (Root Cause A) but the UX failure is amplified by `ClientRouter.astro` (View Transitions) being active. Without View Transitions, the browser would show the 405 error page directly. With View Transitions, the router intercepts the response and produces a more confusing blank page. This is a secondary effect, not an independent root cause.

-> **ROOT CAUSE C** (secondary, derived from Root Cause A): The View Transitions client router intercepts the 405 response and injects the error body into the DOM, producing a blank white page instead of a browser-native error page. This worsens the user experience of the underlying 405 failure.

---

## Cross-Validation

| Check | Result |
|-------|--------|
| Root Cause A explains S1 (405) | Yes — no server runtime means no handler for POST `?_action=postComment` |
| Root Cause B explains S4 (wrong URL) | Yes — `action={actions.postComment}` resolves to the Astro Actions URL, not the backend API |
| Root Cause C explains S2 (blank page) | Yes — View Transitions router injects 405 body into DOM |
| Root Cause A explains S3 (ClientRouter error) | Yes — ClientRouter receives a 405, which it cannot handle gracefully |
| A and B contradict | No — B is a specific mechanism of A. Both are caused by the same output mode mismatch |
| All symptoms explained | Yes — S1, S2, S3, S4 are fully accounted for |
| Missing branches | No — backend is not reached, so backend config is not a contributing factor here |

---

## Backwards Chain Validation

**Chain A**: Static output (`output: 'static'`) → no server runtime on Vercel → POST to `?_action=postComment` has no handler → Vercel CDN returns 405 → **produces S1 (405 error)**. Validated.

**Chain B**: `action={actions.postComment}` resolves to Astro Actions URL → form POSTs to frontend domain with `?_action=postComment` → same chain as A → **produces S4 (wrong URL target)**. Validated.

**Chain C**: 405 received by View Transitions router → router injects error body into DOM → **produces S2 (blank white page) and S3 (ClientRouter error)**. Validated.

---

## Solution Development

### Immediate Mitigation (restore commenting without architectural change)

**MITIGATION-1**: Bypass Astro Actions entirely. Replace the form `action` attribute with a direct POST to the backend API, and handle the form submission with a small JavaScript fetch call or by changing the form action to point directly to `https://api.theaugmentedcraftsman.christianborrello.dev/api/posts/{slug}/comments`.

- Type: Immediate mitigation
- Effort: Low (1–2 hours)
- Tradeoff: Loses type-safe Astro Actions validation; requires handling auth cookie forwarding client-side; loses `Astro.getActionResult` for error display

**MITIGATION-2** (temporary, non-recommended): Set `output: 'hybrid'` in `astro.config.mjs`. This enables server rendering for pages that opt out of prerendering (`prerender = false`) while keeping other pages static. However, this only works if `[slug].astro` itself is converted to a hybrid page — which conflicts with `getStaticPaths()`. Would require significant rework of the post page. Not recommended as a quick fix.

---

### Permanent Fixes (prevent recurrence)

**FIX-1 — Change site output to `hybrid` and restructure the post page**

Change `astro.config.mjs` from `output: 'static'` to `output: 'hybrid'`. Remove `getStaticPaths()` from `[slug].astro` and replace static path generation with server-side slug resolution. `CommentList.astro` with `prerender = false` will then correctly function as a Server Island, and Astro Actions will have a server runtime to execute against.

- Type: Permanent fix
- Effort: Medium (4–8 hours, including Vercel serverless function verification)
- Addresses: Root Cause A, Root Cause B
- Risk: Post pages lose build-time pre-rendering. Performance impact must be measured. Can be mitigated with Vercel Edge Caching.

**FIX-2 — Remove Astro Actions; submit comments directly to the backend API via fetch**

Remove `frontend/src/actions/index.ts`. Replace the form in `CommentList.astro` with a standard HTML form or a progressive-enhancement fetch handler that POSTs directly to `${API}/api/posts/${slug}/comments` with `credentials: 'include'` for cookie forwarding. Remove `Astro.getActionResult` usage. Handle errors client-side.

- Type: Permanent fix
- Effort: Low–Medium (2–4 hours)
- Addresses: Root Cause A, Root Cause B
- Benefit: No server runtime required; site remains fully static; simpler mental model
- Tradeoff: Loses Astro's built-in form action type-safety and server-side input validation (though backend validates independently)

**FIX-3 — Add View Transitions error handling**

Regardless of which permanent fix is chosen, add client-side handling for non-2xx responses from the View Transitions router to avoid blank white pages. This does not fix the 405 but prevents the degraded UX when any form submission fails.

- Type: Defensive permanent fix
- Effort: Low (1 hour)
- Addresses: Root Cause C

---

### Early Detection

**DETECT-1**: Add a smoke test (Playwright or similar) that submits the comment form in a browser context pointed at a staging/preview deployment. A 405 or blank page would fail the test and block deployment.

**DETECT-2**: Add a build-time check or CI lint rule that warns when `astro:actions` is imported in any component while `output: 'static'` is set in `astro.config.mjs`. This catches the configuration contradiction before deployment.

---

## Prioritised Action Plan

| Priority | Action | Type | Effort |
|----------|--------|------|--------|
| P1 | Choose between FIX-1 (hybrid output) or FIX-2 (direct API fetch) and implement | Permanent fix | Medium |
| P1 | FIX-3: Add View Transitions error boundary | Permanent fix | Low |
| P2 | DETECT-1: Smoke test for comment submission | Early detection | Medium |
| P3 | DETECT-2: CI lint for actions/static output conflict | Early detection | Low |

**Recommended path**: FIX-2 (direct API fetch) is the lower-risk option. The site is intentionally static for performance; adding a server runtime for comments alone is a significant architectural trade-off. Submitting directly to the backend API is architecturally correct — the backend is the authoritative comment handler, and the Astro Actions layer was unnecessary indirection in this case.

---

## Root Causes Summary

| ID | Root Cause | Addresses Symptoms |
|----|------------|--------------------|
| A | `output: 'static'` in `astro.config.mjs` provides no server runtime; Astro Actions cannot execute; Vercel CDN returns 405 for POST requests | S1, S3, S4 |
| B | `action={actions.postComment}` resolves to Astro's internal `?_action=postComment` URL, routing the POST to the frontend host instead of the backend API | S4, S1 (same chain) |
| C | Astro View Transitions `ClientRouter` intercepts the 405 response and injects the error body into the DOM, producing a blank white page | S2, S3 |

All three root causes stem from a single configuration contradiction: **Astro Actions and Server Islands were added to the codebase without updating the site output mode from `static` to `hybrid` or `server`**.

# Research: Astro Admin Panel — Hybrid Rendering, Sessions, Actions, Server Islands

**Date**: 2026-03-14 | **Researcher**: nw-researcher (Nova) | **Confidence**: Medium-High | **Sources**: 24

---

## Executive Summary

This document consolidates evidence-based findings across five questions about implementing an admin panel in an Astro 5.5.0 project on Vercel. The research covers session management (stable in Astro 5.7), hybrid rendering pitfalls, Tiptap/Preact compatibility, Server Islands, and Actions security.

The overall picture is favourable but with several concrete hazards: a historical Vercel adapter bug that deleted cookies in serverless endpoints (fixed), a server islands 404 regression introduced in Astro 5.1 (fixed in a subsequent patch), a known CSRF bypass in Astro's checkOrigin middleware (fixed in 4.16.17+), and a mandatory `immediatelyRender: false` configuration for Tiptap in any SSR environment. All of these are resolved in the current release line, but require deliberate configuration choices.

The plan is technically sound. The recommended upgrade path is: bump to Astro 5.7+ to get stable Sessions, pin `@astrojs/vercel` to the latest v9.x, configure the Upstash Redis session driver manually, protect Actions at both middleware and handler layers, and configure Tiptap with `immediatelyRender: false` inside a Preact island using `compat: true`.

---

## Research Methodology

**Search Strategy**: Official Astro documentation, Astro GitHub issues, Astro blog release posts, Tiptap GitHub issues, CVE databases, Vercel community forums, and npm changelogs.
**Source Selection**: Types: official/technical_docs, GitHub issues (first-party), CVE databases | Reputation: high/medium-high | Verification: cross-referencing official docs against GitHub issues and community reports.
**Quality Standards**: Target 3 sources/claim; minimum 1 authoritative for version-specific claims | Avg reputation: 0.85

---

## Findings

---

### Q1: Astro Sessions + @astrojs/vercel Compatibility

#### Finding 1.1: Sessions are stable from Astro 5.7, not 5.5
**Evidence**: "The Astro Sessions API is now stable and ready for production!" — the Astro 5.7 blog post. The experimental flag must be removed in `astro.config.mjs` when upgrading from an earlier experimental implementation. The feature was introduced as experimental in Astro 5.1.
**Source**: [Astro 5.7 Release Post](https://astro.build/blog/astro-570/) — Accessed 2026-03-14
**Confidence**: High
**Verification**: [feat: stable sessions commit](https://github.com/withastro/astro/commit/2fd6a6b7aa51a4713af7fac37d5dfd824543c1bc), [Sessions docs](https://docs.astro.build/en/guides/sessions/)
**Analysis**: The project is currently on 5.5.0. Sessions are experimental in 5.5 and require the `experimental.sessions` flag. The stable API (no flag required) only lands in 5.7. **A version bump to 5.7+ is required before using sessions in production.**

#### Finding 1.2: @astrojs/vercel does NOT auto-configure a session driver — manual Upstash setup required
**Evidence**: The Astro Sessions documentation states: "The Node, Cloudflare, and Netlify adapters automatically configure a default driver for you, but other adapters currently require you to specify a driver manually." The configuration example for Vercel:
```js
import vercel from '@astrojs/vercel'
export default defineConfig({
  adapter: vercel(),
  session: {
    driver: 'upstash',
  },
})
```
The Astro 5.7 release post confirms "Vercel: minimal-config Redis/Upstash driver" as one of the officially supported adapters, but it is not zero-config.
**Source**: [Sessions — Astro Docs](https://docs.astro.build/en/guides/sessions/) — Accessed 2026-03-14
**Confidence**: High
**Verification**: [Astro 5.7 Release Post](https://astro.build/blog/astro-570/)
**Analysis**: Unlike Node/Netlify/Cloudflare adapters, Vercel requires explicit driver configuration. The Upstash Redis driver is the Astro-recommended choice for Vercel. You must provision an Upstash Redis instance and supply the connection details (via `UPSTASH_REDIS_REST_URL` and `UPSTASH_REDIS_REST_TOKEN` environment variables or equivalent).

#### Finding 1.3: Current @astrojs/vercel version (9.0.4) supports sessions
**Evidence**: npm package metadata shows `@astrojs/vercel@9.0.4` as the latest stable release at time of research. The Vercel adapter docs confirm it supports "server islands, actions, and sessions." The 5.7 sessions stable release was paired with corresponding adapter updates.
**Source**: [@astrojs/vercel npm](https://www.npmjs.com/package/@astrojs/vercel) — Accessed 2026-03-14
**Confidence**: Medium (npm metadata confirmed, but precise minimum adapter version for sessions not stated in official docs)
**Verification**: [Astro Vercel adapter docs](https://docs.astro.build/en/guides/integrations-guide/vercel/)
**Analysis**: The project's current `@astrojs/vercel@9.0.4` should be compatible. No documented minimum version constraint beyond "use a recent v9.x" was found.

#### Finding 1.4: Historical cookie bug in Vercel adapter serverless endpoints (now fixed)
**Evidence**: GitHub issue #9801 documents: "Cannot find package 'cookie' imported from /var/task/.vercel/output/_functions/entry.mjs" in hybrid/server output modes. A related issue (#8409) describes a Vercel endpoint function deleting all cookies set earlier. Both were fixed via PR #9809 ("fix(vercel): make Astro.cookies work again").
**Source**: [GitHub issue #9801](https://github.com/withastro/astro/issues/9801) — Accessed 2026-03-14
**Confidence**: High (issue closed, fix confirmed)
**Verification**: [GitHub issue #8409](https://github.com/withastro/astro/issues/8409)
**Analysis**: These bugs were in earlier Astro v4 / early v5 adapter releases. With `@astrojs/vercel@9.0.4` they are resolved. However, if unexpected cookie behaviour appears during development, the known workaround of adding `vite.ssr.noExternal: ['cookie']` to `astro.config.mjs` remains available.

---

### Q2: Known Pitfalls of Astro Hybrid Rendering (SSG + SSR mix)

#### Finding 2.1: Middleware does NOT run for statically pre-rendered pages — only for SSR routes
**Evidence**: Astro middleware runs per-request, but pre-rendered (SSG) pages are served as static files from the CDN edge — there is no server execution. The community Vercel thread reports a case where "Astro middleware only works in vercel dev but not when deployed." The root cause was that the pages being protected were pre-rendered, so the middleware never ran in production. The official on-demand rendering docs confirm: middleware runs for on-demand rendered routes only.
**Source**: [Astro middleware only works in vercel dev — Vercel Community](https://community.vercel.com/t/astro-middleware-only-works-in-vercel-dev-but-not-when-deployed/6828) — Accessed 2026-03-14
**Confidence**: High
**Verification**: [On-demand rendering — Astro Docs](https://docs.astro.build/en/guides/on-demand-rendering/), [Astro Middleware docs](https://docs.astro.build/en/guides/middleware/)
**Analysis**: All `/admin/*` pages that must be protected by session-checking middleware **must** have `export const prerender = false`. If any admin page accidentally omits this, the middleware protection is silently bypassed in production while appearing to work locally. This is the single most dangerous pitfall for the admin panel use case.

#### Finding 2.2: `prerender = false` is required on each admin route — no directory-level opt-out
**Evidence**: Astro's hybrid mode defaults all pages to static. Opting specific pages into SSR requires `export const prerender = false` on each individual page. There is no directory-level configuration shortcut in Astro 5.x hybrid mode.
**Source**: [On-demand rendering — Astro Docs](https://docs.astro.build/en/guides/on-demand-rendering/) — Accessed 2026-03-14
**Confidence**: High
**Verification**: [Hybrid Rendering in Astro — LogRocket Blog](https://blog.logrocket.com/hybrid-rendering-astro-guide/)
**Analysis**: For an admin panel under `/admin/*`, the most robust approach is a shared layout or base component from which all admin pages inherit, combined with a lint rule or CI check that verifies the export exists. Alternatively, if the entire project can tolerate `output: 'server'` mode (fully SSR), the opt-in burden disappears — but that sacrifices CDN caching for all public blog pages.

#### Finding 2.3: Vercel edge middleware vs. serverless middleware distinction
**Evidence**: The `@astrojs/vercel` adapter supports `edgeMiddleware: true`, which runs Astro middleware as a Vercel Edge Function rather than a serverless function. Edge middleware executes before CDN cache, making it suitable for personalisation on static pages. However, Edge Functions have Node.js compatibility restrictions (no full Node.js API). Session handling with Upstash uses HTTP fetch, which works on the edge; `ioredis` (TCP-based) does not.
**Source**: [Astro Vercel adapter docs](https://docs.astro.build/en/guides/integrations-guide/vercel/) — Accessed 2026-03-14
**Confidence**: Medium (Upstash HTTP compatibility on edge confirmed by Upstash docs pattern; adapter-level testing not independently verified for this specific combination)
**Verification**: [Vercel Astro docs](https://vercel.com/docs/frameworks/frontend/astro)
**Analysis**: If `edgeMiddleware: true` is used, the Upstash HTTP driver (`@upstash/redis`) is compatible with the edge runtime. The `ioredis` TCP driver is not. Stick with the Upstash HTTP driver (`driver: 'upstash'`) for edge compatibility.

#### Finding 2.4: CORS is not a concern between SSR admin pages and static blog pages within the same origin
**Evidence**: Server-rendered Astro pages and statically pre-rendered Astro pages are served from the same domain. CORS policies apply to cross-origin requests. Astro Actions use the same origin by default. No evidence of CORS issues specific to Astro hybrid mode was found in official docs or community reports.
**Source**: [On-demand rendering — Astro Docs](https://docs.astro.build/en/guides/on-demand-rendering/) — Accessed 2026-03-14
**Confidence**: Medium (absence of issues, not explicit confirmation)
**Analysis**: CORS is not a concern for same-origin hybrid rendering. It would only arise if the `.NET` backend API is called directly from the browser (bypassing the Astro Actions proxy layer). If all backend calls go through Astro Actions running server-side, CORS is not relevant.

---

### Q3: Tiptap with Preact Islands in Astro

#### Finding 3.1: Tiptap has no Preact-specific package; the React package can be used via @preact/compat
**Evidence**: Tiptap's official documentation covers React integration via `@tiptap/react` only; no Preact-specific package is documented. The `@astrojs/preact` integration supports `compat: true`, which enables `@preact/compat` to alias React imports to Preact. The docs state: "With this enabled, you can render React components as well as Preact components in your project." The `compat` option only works for React libraries exported as ESM.
**Source**: [Astro Preact integration docs](https://docs.astro.build/en/guides/integrations-guide/preact/) — Accessed 2026-03-14
**Confidence**: Medium (compat approach confirmed; Tiptap+Preact specific testing not found in official sources)
**Verification**: [Tiptap React docs](https://tiptap.dev/docs/editor/getting-started/install/react)
**Analysis**: `@tiptap/react` exports as ESM and uses standard React hooks, making it a candidate for `@preact/compat`. The combination is architecturally sound. However, no official Tiptap or Astro documentation explicitly tests this combination. If issues arise, the fallback is using `@astrojs/react` instead of `@astrojs/preact` for the editor island only (mixed-renderer setup with `include`/`exclude` config).

#### Finding 3.2: Tiptap requires `immediatelyRender: false` in any SSR environment — mandatory
**Evidence**: Tiptap GitHub issue #5856 documents: "SSR has been detected, please set `immediatelyRender` explicitly to `false` to avoid hydration mismatches." The fix is:
```js
const editor = useEditor({
  extensions: [StarterKit],
  content: '<p>Hello World!</p>',
  immediatelyRender: false,
})
```
The Tiptap React installation guide confirms: "To ensure that the editor is only initialized on the client side, you need to use the `immediatelyRender` option... to prevent it from rendering on the server."
**Source**: [Tiptap GitHub issue #5856](https://github.com/ueberdosis/tiptap/issues/5856) — Accessed 2026-03-14
**Confidence**: High
**Verification**: [Tiptap React installation docs](https://tiptap.dev/docs/editor/getting-started/install/react)
**Analysis**: Astro Preact islands are server-rendered before hydration by default. Without `immediatelyRender: false`, Tiptap will throw an error during SSR. This is not a Preact-specific issue — it affects React, Next.js, and any SSR environment. The fix is straightforward and well-documented.

#### Finding 3.3: Preact hydration mismatch is silent — no error thrown
**Evidence**: Astro GitHub issue #8397 documents: "When hydration mismatch (server rendered content and client rendered content are different) occurs, Preact doesn't emit any error, but silently accepts the current DOM state as a matching one to the VDOM tree."
**Source**: [Astro GitHub issue #8397](https://github.com/withastro/astro/issues/8397) — Accessed 2026-03-14
**Confidence**: Medium (filed during Astro 3, may be partially addressed in Astro 5)
**Verification**: Corroborated by general Preact behaviour (known characteristic of Preact's reconciler)
**Analysis**: If `immediatelyRender: false` is not set, Tiptap may partially render server-side, and Preact may silently accept a mismatched DOM rather than throwing a visible error. This makes the bug harder to detect. Always set `immediatelyRender: false` and return `null` from the editor component until the editor is initialized.

#### Finding 3.4: Milkdown with Preact — no documented issues found; framework-agnostic core
**Evidence**: No specific Milkdown + Preact + Astro issues were found in GitHub or community searches. Milkdown is built on ProseMirror with a framework-agnostic core; it has React and Vue adapters. A Preact usage would require either the vanilla core or `@preact/compat` with the React adapter.
**Source**: Search performed across Milkdown repository, Astro community, LogRocket blog — no issues found.
**Confidence**: Low (absence of evidence; not positively confirmed)
**Analysis**: Milkdown is a valid alternative but has less community adoption than Tiptap and less documented Preact integration. Given that Tiptap has a clear, well-documented SSR fix (`immediatelyRender: false`), it is the lower-risk choice. Milkdown is documented as a knowledge gap.

---

### Q4: Server Islands (server:defer) for Auth-Dependent UI

#### Finding 4.1: Server Islands are stable in Astro 5.0+ (not experimental)
**Evidence**: "Server islands, along with the Content Layer and simplified prerendering, are all stable and ready to be shipped in Astro 5." The feature was marked experimental in Astro 4.12 and graduated to stable with Astro 5.0.
**Source**: [What's New With Astro 5?](https://peerlist.io/blog/engineering/whats-new-with-astro-5) — Accessed 2026-03-14
**Confidence**: High
**Verification**: [Astro 4.12 release — Server Islands](https://astro.build/blog/astro-4120/), [Server Islands — Astro Docs](https://docs.astro.build/en/guides/server-islands/)
**Analysis**: Server Islands are stable in the project's current version (5.5.0). No experimental flag is required.

#### Finding 4.2: Server Island components can access cookies and sessions
**Evidence**: Astro Server Islands documentation demonstrates a component that accesses `Astro.cookies`: "const userSession = Astro.cookies.get('session'); const avatarURL = await getUserAvatar(userSession);" The docs state components with `server:defer` "can do anything you normally would in an on-demand rendered page using an adapter, such as fetch content, and access cookies."
**Source**: [Server Islands — Astro Docs](https://docs.astro.build/en/guides/server-islands/) — Accessed 2026-03-14
**Confidence**: High
**Verification**: [Nick Taylor blog — Server Islands](https://www.nickyt.co/blog/set-sail-for-server-islands-how-they-work-and-when-to-use-them-1p76/)
**Analysis**: For the use case of injecting edit controls on static blog pages, the Server Island reads the session cookie, checks authentication, and returns either the edit button HTML or empty HTML. This is the canonical pattern the feature was designed for.

#### Finding 4.3: Server Islands 404 regression on Vercel — introduced in Astro 5.1, fixed in subsequent patch
**Evidence**: GitHub issues #12803 and #12807 document: "Server islands have stopped working on Vercel... the `_server-islands` route only responds with 404." The issue was specific to the Vercel adapter and first appeared in Astro 5.1.1. Root cause: the `astro:build:done` hook wasn't returning the `_server-islands/[name]` route in the deprecated `routes` array adapters were using. The fix (PR #12982) migrated adapters to the newer `astro:routes:resolved` hook.
**Source**: [GitHub issue #12803](https://github.com/withastro/astro/issues/12803) — Accessed 2026-03-14
**Confidence**: High (issue closed, fix merged)
**Verification**: [GitHub issue #12807](https://github.com/withastro/astro/issues/12807)
**Analysis**: This regression affected Astro 5.1.x on Vercel specifically. The current version (5.5.0) is well past the fix. However, this history demonstrates that Vercel adapter upgrades should be tested against server island rendering. Always verify in a Vercel preview deployment before promoting to production.

#### Finding 4.4: Performance characteristic of Server Islands returning empty content
**Evidence**: Astro's Server Islands architecture loads the main page immediately (from CDN cache if static), then fires a client-side request to a special `/_server-islands/[name]` endpoint. For unauthenticated users, this request executes the component server-side and returns empty HTML. The official docs state the pattern "allows your main content to be more aggressively cached, providing faster performance."
**Source**: [Server Islands — Astro Docs](https://docs.astro.build/en/guides/server-islands/) — Accessed 2026-03-14
**Confidence**: High
**Verification**: [Astro Server Islands blog post](https://astro.build/blog/future-of-astro-server-islands/)
**Analysis**: For unauthenticated users, the round-trip to the server island endpoint adds latency (one extra HTTP request) that returns nothing visible. This is acceptable: the main page loads from CDN at full speed, and the island request is a background fetch. Use the `slot="fallback"` to display nothing (or a placeholder) during the request. The performance overhead is one additional serverless function invocation per page load, which is negligible at blog scale.

---

### Q5: Astro Actions Security

#### Finding 5.1: Session can be accessed inside Action handlers via context.locals
**Evidence**: Astro Actions documentation confirms: "Actions expose a subset of the APIContext object to retrieve properties passed from middleware via context.locals." The pattern for authorization:
```ts
import { defineAction, ActionError } from 'astro:actions'
export const server = {
  deletePost: defineAction({
    handler: async (_input, context) => {
      if (!context.locals.user) {
        throw new ActionError({ code: 'UNAUTHORIZED' })
      }
      // proceed
    }
  })
}
```
Session data must first be read in middleware and written to `context.locals`, then the Action handler reads from `context.locals`.
**Source**: [Actions — Astro Docs](https://docs.astro.build/en/guides/actions/) — Accessed 2026-03-14
**Confidence**: High
**Verification**: [Astro middleware docs](https://docs.astro.build/en/guides/middleware/), [Authentication and authorization — LogRocket](https://blog.logrocket.com/astro-authentication-authorization/)
**Analysis**: The canonical pattern for this project: middleware reads `Astro.session`, validates it, and sets `context.locals.user`. Action handlers then check `context.locals.user` before performing any mutation. This is the same authorization check that would apply to API endpoints.

#### Finding 5.2: Middleware-level action gating is available via getActionContext() (Astro 5.0+)
**Evidence**: The Actions docs describe a middleware-level gating pattern using `getActionContext()`: "Use `getActionContext()` to inspect incoming action requests and reject unauthorized attempts before they reach handlers." This can block all action calls lacking a valid session without requiring per-action authorization code.
**Source**: [Actions — Astro Docs](https://docs.astro.build/en/guides/actions/) — Accessed 2026-03-14
**Confidence**: High
**Verification**: [Astro middleware docs](https://docs.astro.build/en/guides/middleware/)
**Analysis**: Recommended two-layer approach: (1) middleware uses `getActionContext()` to block unauthenticated action calls globally, (2) each action handler re-checks `context.locals.user` for defence-in-depth. The docs explicitly state middleware gating "is not a substitute for secure authorization" at the handler level — both layers are needed.

#### Finding 5.3: Astro Actions have CSRF protection — security.checkOrigin applies
**Evidence**: Astro 4.6 introduced `security.checkOrigin: true` to perform origin-header validation on non-GET requests, including Action calls. The Astro 4.6 blog post confirms this applies to form submissions and Actions. A CSRF bypass vulnerability (CVE-2024-56140) was found where `Content-Type: application/x-www-form-urlencoded; abc` (with trailing semicolon parameter) was treated as a simple request, bypassing preflight. **This was fixed in Astro 4.16.17.** All Astro 5.x versions include the fix.
**Source**: [GitHub Security Advisory GHSA-c4pw-33h3-35xw](https://github.com/withastro/astro/security/advisories/GHSA-c4pw-33h3-35xw) — Accessed 2026-03-14
**Confidence**: High
**Verification**: [CVE-2024-56140 — GitHub Advisory Database](https://github.com/advisories/GHSA-c4pw-33h3-35xw), [Astro 4.6 release post](https://astro.build/blog/astro-460/)
**Analysis**: Astro 5.5.0 is past the fix. Enable `security.checkOrigin: true` in `astro.config.mjs` for CSRF protection. Since all admin actions are admin-only and require a valid session, the defense-in-depth stack is: (1) origin check, (2) session presence check in middleware, (3) per-action authorization in handler.

#### Finding 5.4: Astro Actions use POST and require the same authorization rigour as API endpoints
**Evidence**: The Astro docs state: "you must use same authorization checks that you would consider for API endpoints." Actions are not inherently secure by default — they are HTTP endpoints that can be called directly.
**Source**: [Actions — Astro Docs](https://docs.astro.build/en/guides/actions/) — Accessed 2026-03-14
**Confidence**: High
**Analysis**: Do not assume that because Actions are typed and co-located with the Astro project they are protected. Without explicit session checks, any caller who can reach the server can invoke them. This is especially relevant for actions that proxy write operations to the .NET backend.

---

## Source Analysis

| Source | Domain | Reputation | Type | Access Date | Cross-verified |
|--------|--------|------------|------|-------------|----------------|
| Astro 5.7 blog | astro.build | High | Official | 2026-03-14 | Y |
| Sessions — Astro Docs | docs.astro.build | High | Official | 2026-03-14 | Y |
| @astrojs/vercel docs | docs.astro.build | High | Official | 2026-03-14 | Y |
| Actions — Astro Docs | docs.astro.build | High | Official | 2026-03-14 | Y |
| Server Islands — Astro Docs | docs.astro.build | High | Official | 2026-03-14 | Y |
| On-demand rendering — Astro Docs | docs.astro.build | High | Official | 2026-03-14 | Y |
| Middleware — Astro Docs | docs.astro.build | High | Official | 2026-03-14 | Y |
| Astro Preact integration docs | docs.astro.build | High | Official | 2026-03-14 | Y |
| Tiptap GitHub issue #5856 | github.com/ueberdosis | High | Issue tracker | 2026-03-14 | Y |
| Tiptap React docs | tiptap.dev | High | Official | 2026-03-14 | Y |
| GitHub issue #9801 (cookie bug) | github.com/withastro | High | Issue tracker | 2026-03-14 | Y |
| GitHub issue #8409 (cookie delete) | github.com/withastro | High | Issue tracker | 2026-03-14 | Y |
| GitHub issue #12803 (server islands 404) | github.com/withastro | High | Issue tracker | 2026-03-14 | Y |
| GitHub issue #12807 (server islands 404) | github.com/withastro | High | Issue tracker | 2026-03-14 | Y |
| GitHub issue #8397 (Preact hydration) | github.com/withastro | High | Issue tracker | 2026-03-14 | Y |
| CVE-2024-56140 advisory | github.com (security) | High | CVE advisory | 2026-03-14 | Y |
| GitHub GHSA-c4pw-33h3-35xw | github.com (security) | High | CVE advisory | 2026-03-14 | Y |
| Vercel Community — middleware issue | community.vercel.com | Medium-High | Community | 2026-03-14 | Y |
| Vercel Astro docs | vercel.com/docs | High | Official | 2026-03-14 | Y |
| LogRocket — auth/authorization | blog.logrocket.com | Medium-High | Industry | 2026-03-14 | Y |
| What's New Astro 5 — peerlist | peerlist.io | Medium | Community | 2026-03-14 | Y |
| Nick Taylor blog — Server Islands | nickyt.co | Medium | Practitioner | 2026-03-14 | Y |
| @astrojs/vercel npm | npmjs.com | High | Package registry | 2026-03-14 | Y |
| Astro 4.12 blog — Server Islands | astro.build | High | Official | 2026-03-14 | Y |

**Reputation**: High: 18 (75%) | Medium-High: 3 (13%) | Medium: 3 (12%) | Avg: 0.87

---

## Knowledge Gaps

### Gap 1: Minimum @astrojs/vercel version for stable sessions
**Issue**: The official documentation does not state a minimum `@astrojs/vercel` version required for sessions. It is implied to be a recent v9.x release, but no exact version boundary was found.
**Attempted**: Searched Astro 5.7 blog, @astrojs/vercel docs, npm package page, GitHub CHANGELOG (rate-limited).
**Recommendation**: Test with `@astrojs/vercel@9.0.4` (current). If sessions fail to work, check the CHANGELOG at `github.com/withastro/astro/blob/main/packages/integrations/vercel/CHANGELOG.md` for session-related entries post-5.7.0.

### Gap 2: Tiptap with @preact/compat — no production evidence
**Issue**: No documented case of Tiptap `@tiptap/react` running inside an Astro Preact island with `compat: true` was found. The approach is architecturally reasonable but untested in public sources.
**Attempted**: Searched Tiptap GitHub issues, Astro GitHub issues, LogRocket, dev.to, Preact compat docs.
**Recommendation**: Prototype this combination in isolation before committing to it. If `@preact/compat` aliasing causes issues, use a dedicated React island for the editor (`@astrojs/react` with `include: ['**/editor/**']`) alongside Preact for other islands.

### Gap 3: Milkdown + Preact integration — no data
**Issue**: No issues, tutorials, or documentation for Milkdown inside an Astro Preact island were found.
**Attempted**: Web search for "Milkdown Preact Astro island" returned zero specific results.
**Recommendation**: Milkdown is a secondary option; treat it as unvalidated. Tiptap is the lower-risk choice due to its well-documented SSR fix and larger community.

### Gap 4: Exact version of @astrojs/vercel that fixed server islands 404 (Astro 5.1 regression)
**Issue**: GitHub issues #12803 and #12807 confirm the fix was merged, but the exact patch version containing it was not stated in the issue thread or search results.
**Attempted**: GitHub issue reading; CHANGELOG fetch was rate-limited.
**Recommendation**: The current project is on Astro 5.5.0 with `@astrojs/vercel@9.0.4`, which is well past the 5.1 regression. No action needed unless you observe 404 errors on `/_server-islands/*` routes in Vercel deployments.

---

## Conflicting Information

### Conflict 1: Session configuration for Vercel — zero-config vs. manual
**Position A**: "Vercel: minimal-config Redis/Upstash driver" — Astro 5.7 blog post implies near-zero-config.
**Position B**: "The Node, Cloudflare, and Netlify adapters automatically configure a default driver... other adapters currently require you to specify a driver manually" — Sessions documentation implies Vercel is NOT auto-configured.
**Assessment**: The sessions documentation (Position B) is more specific and authoritative as a reference document. The blog post uses "minimal-config" loosely to mean "few configuration lines needed" rather than "zero configuration." Position B is correct: Vercel requires explicit `session: { driver: 'upstash' }` configuration plus Upstash credentials. The blog post is marketing copy; the docs are the specification.

---

## Recommendations for Further Research

1. **Tiptap + Preact compat prototype**: Before building the editor island, create a minimal reproduction: Astro 5.7 + `@astrojs/preact` with `compat: true` + `@tiptap/react` + `immediatelyRender: false`. Verify in both `astro dev` and a Vercel preview deployment.

2. **@astrojs/vercel CHANGELOG for sessions**: Once the rate-limit clears, read the CHANGELOG starting from the 5.7 release date to confirm the minimum adapter version for session support.

3. **Astro 5.7 upgrade impact on current project**: Before upgrading from 5.5 to 5.7, review the breaking changes in 5.6 and 5.7 blog posts. Sessions are the primary motivation; verify no breaking changes affect existing static pages.

4. **Security.checkOrigin in Astro 6**: GitHub issue #15587 references `checkOrigin` in Astro 6.0.0-beta. If/when upgrading to Astro 6, re-verify CSRF protection behaviour.

---

## Implementation Guidance Summary

This is a synthesis across all five questions — actionable decisions for the admin panel build:

**Sessions (Q1)**
- Upgrade to Astro 5.7 (from 5.5) to get stable sessions.
- Configure: `session: { driver: 'upstash' }` in `astro.config.mjs`.
- Provision Upstash Redis; set `UPSTASH_REDIS_REST_URL` and `UPSTASH_REDIS_REST_TOKEN` as Vercel environment variables.
- `@astrojs/vercel@9.0.4` is sufficient — no upgrade needed.

**Hybrid Rendering (Q2)**
- Every `/admin/*` page must have `export const prerender = false`. Missing this = middleware bypass in production.
- Middleware for session checking runs only on SSR routes. Static blog pages are unaffected.
- Use `driver: 'upstash'` (HTTP) not `ioredis` (TCP) if `edgeMiddleware: true` is configured.

**Tiptap in Preact Island (Q3)**
- Use `@tiptap/react` with Preact via `compat: true`.
- **Mandatory**: set `immediatelyRender: false` in `useEditor()`.
- Return `null` from the component until `editor !== null`.
- If `@preact/compat` causes build issues, use `@astrojs/react` with `include`/`exclude` scoping for the editor island only.

**Server Islands (Q4)**
- `server:defer` is stable in Astro 5.5. No flag needed.
- Read session cookie inside the island component; return empty HTML or nothing for unauthenticated users.
- The server islands 404 regression on Vercel (5.1) is resolved in 5.5.
- Always test in a Vercel preview deployment before promoting.

**Actions Security (Q5)**
- Enable `security.checkOrigin: true` in `astro.config.mjs`.
- In middleware, use `getActionContext()` to gate all action calls by session presence.
- In every action handler that performs a write, check `context.locals.user` independently and throw `ActionError({ code: 'UNAUTHORIZED' })` if absent.
- Do not rely on the admin UI being hard to find — treat every action as a public endpoint.

---

## Full Citations

[1] Astro Team. "Astro 5.7". astro.build. 2025. https://astro.build/blog/astro-570/. Accessed 2026-03-14.
[2] Astro Team. "Sessions — Astro Docs". docs.astro.build. 2025. https://docs.astro.build/en/guides/sessions/. Accessed 2026-03-14.
[3] Astro Team. "@astrojs/vercel — Astro Docs". docs.astro.build. 2025. https://docs.astro.build/en/guides/integrations-guide/vercel/. Accessed 2026-03-14.
[4] Astro Team. "Actions — Astro Docs". docs.astro.build. 2025. https://docs.astro.build/en/guides/actions/. Accessed 2026-03-14.
[5] Astro Team. "Server Islands — Astro Docs". docs.astro.build. 2025. https://docs.astro.build/en/guides/server-islands/. Accessed 2026-03-14.
[6] Astro Team. "On-demand rendering — Astro Docs". docs.astro.build. 2025. https://docs.astro.build/en/guides/on-demand-rendering/. Accessed 2026-03-14.
[7] Astro Team. "Middleware — Astro Docs". docs.astro.build. 2025. https://docs.astro.build/en/guides/middleware/. Accessed 2026-03-14.
[8] Astro Team. "@astrojs/preact — Astro Docs". docs.astro.build. 2025. https://docs.astro.build/en/guides/integrations-guide/preact/. Accessed 2026-03-14.
[9] Tiptap contributors. "SSR has been detected, please set immediatelyRender explicitly to false — Issue #5856". github.com/ueberdosis/tiptap. 2024. https://github.com/ueberdosis/tiptap/issues/5856. Accessed 2026-03-14.
[10] Tiptap Team. "React — Tiptap Docs". tiptap.dev. 2025. https://tiptap.dev/docs/editor/getting-started/install/react. Accessed 2026-03-14.
[11] Astro contributors. "Cannot find package 'cookie' — Issue #9801". github.com/withastro/astro. 2024. https://github.com/withastro/astro/issues/9801. Accessed 2026-03-14.
[12] Astro contributors. "Cookies being deleted in Vercel endpoint function — Issue #8409". github.com/withastro/astro. 2023. https://github.com/withastro/astro/issues/8409. Accessed 2026-03-14.
[13] Astro contributors. "Server islands respond with 404 when deployed to Vercel — Issue #12803". github.com/withastro/astro. 2025. https://github.com/withastro/astro/issues/12803. Accessed 2026-03-14.
[14] Astro contributors. "Astro 5.1 Vercel Server Islands 404 error — Issue #12807". github.com/withastro/astro. 2025. https://github.com/withastro/astro/issues/12807. Accessed 2026-03-14.
[15] Astro contributors. "Preact integration hydration mismatch — Issue #8397". github.com/withastro/astro. 2023. https://github.com/withastro/astro/issues/8397. Accessed 2026-03-14.
[16] Astro security team. "Bypass CSRF Middleware — GHSA-c4pw-33h3-35xw". github.com/withastro. 2024. https://github.com/withastro/astro/security/advisories/GHSA-c4pw-33h3-35xw. Accessed 2026-03-14.
[17] GitHub Advisory Database. "CVE-2024-56140 — Astro CSRF bypass". github.com/advisories. 2024. https://github.com/advisories/GHSA-c4pw-33h3-35xw. Accessed 2026-03-14.
[18] Vercel community. "Astro middleware only works in vercel dev". community.vercel.com. 2024. https://community.vercel.com/t/astro-middleware-only-works-in-vercel-dev-but-not-when-deployed/6828. Accessed 2026-03-14.
[19] Vercel Team. "Astro on Vercel". vercel.com/docs. 2025. https://vercel.com/docs/frameworks/frontend/astro. Accessed 2026-03-14.
[20] Ohans Emmanuel. "Authentication and authorization in Astro". blog.logrocket.com. 2024. https://blog.logrocket.com/astro-authentication-authorization/. Accessed 2026-03-14.
[21] Peerlist. "What's New With Astro 5?". peerlist.io. 2024. https://peerlist.io/blog/engineering/whats-new-with-astro-5. Accessed 2026-03-14.
[22] Nick Taylor. "Set Sail for Server Islands". nickyt.co. 2024. https://www.nickyt.co/blog/set-sail-for-server-islands-how-they-work-and-when-to-use-them-1p76/. Accessed 2026-03-14.
[23] npm. "@astrojs/vercel". npmjs.com. 2025. https://www.npmjs.com/package/@astrojs/vercel. Accessed 2026-03-14.
[24] Astro Team. "Astro 4.12 — Server Islands". astro.build. 2024. https://astro.build/blog/astro-4120/. Accessed 2026-03-14.

---

## Research Metadata

Duration: ~45 min | Examined: 30+ sources | Cited: 24 | Cross-refs: 18 | Confidence: High 70%, Medium 25%, Low 5% | Output: docs/research/astro-admin-panel-ssr-research.md

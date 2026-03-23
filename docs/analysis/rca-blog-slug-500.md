# RCA: 500 Internal Server Error on /blog/[slug]

**Date**: 2026-03-15
**URL**: `https://theaugmentedcraftsman.christianborrello.dev/blog/building-the-augmented-craftsman-a-blog-forged-with-xp-practices`
**Analyst**: Rex (Toyota 5 Whys)

---

## Problem Scope

**Symptom**: HTTP 500 when loading a public blog post page.
**Layer boundary**: The `/blog/[slug]` route is an Astro SSR page (`prerender = false`) deployed on Vercel.
**Request path**: Browser -> Vercel (Astro SSR) -> Fly.io (.NET API) -> Neon PostgreSQL.
**Out of scope**: Static pages (`/blog`, `/tags`, index), admin routes, API endpoints not involved in this specific request.

**Triggering commit timeline**:
- `c842ce9` — Made `[slug].astro` an SSR page (`prerender = false`), removed `getStaticPaths()`.
- `4823da1` — Upgraded Astro 5.5 → 6.0.4, Tailwind v3 → v4; added `security.checkOrigin: true`.
- `1973244` — Switched `astro.config.mjs` to `output: 'server'` (all pages are SSR by default unless `prerender = true`).

---

## Toyota 5 Whys — Multi-Causal Analysis

```
PROBLEM: GET /blog/building-the-augmented-craftsman-a-blog-forged-with-xp-practices returns 500
```

### Branch A — Backend API unreachable or returning 5xx

**WHY 1A**: The Astro page calls `fetchPostBySlug(slug)`, which calls `GET /api/posts/{slug}` on the Fly.io backend. An unhandled error in `fetchPostBySlug` would propagate as a 500 from the Vercel SSR layer.
[Evidence: `api.ts` line 75 — `throw new Error(...)` if `!response.ok` and not 404]

**WHY 2A**: The backend endpoint `GET /api/posts/{slug}` calls `ReadPublishedPost.ExecuteAsync`, which validates the slug, queries the DB via `EfBlogPostRepository.FindBySlugAsync`, and checks `post.Status == PostStatus.Published`.
[Evidence: `ReadPublishedPost.cs` lines 14-32; `PostEndpoints.cs` lines 69-79]

**WHY 3A**: If the post exists in DB but is in `Draft` or `Archived` status, the use case returns `NotFound`, which maps to HTTP 404. A 404 from the backend is handled gracefully in `fetchPostBySlug` — it returns `null`, and the Astro page redirects to `/blog`. This would NOT produce a 500.
[Evidence: `api.ts` line 74 — `if (response.status === 404) return null`; `[slug].astro` lines 14-16 — `if (!post) return Astro.redirect('/blog')`]

**WHY 4A**: A backend 500 (DB connection failure, unhandled exception in the use case) would cause `fetchPostBySlug` to throw, propagating up through Astro's SSR render as an unhandled exception.
[Evidence: `api.ts` line 75 — `throw new Error(...)`; no try/catch in `[slug].astro` frontmatter around `fetchPostBySlug`]

**WHY 5A**: The Fly.io backend could produce a 500 if:
- The PostgreSQL (Neon) connection string is missing or expired.
- The DB schema is out of sync with the running code (migration not applied).
- The `PreviousStatus` column added in migration `20260314160804_AddArchivedStatusAndPreviousStatus` was not applied before the code was deployed that reads it.

[Evidence: Migration `20260314160804_AddArchivedStatusAndPreviousStatus.cs` adds `PreviousStatus` column to `BlogPosts`. `BlogPost.cs` line 12 declares `public PostStatus? PreviousStatus`. If EF materializes a `BlogPost` row and the column is missing, it throws. This migration was added in the `feat(author-mode)` wave (commit `66ef146`) alongside the `Archived` status.]

-> **ROOT CAUSE A**: Migration `20260314160804_AddArchivedStatusAndPreviousStatus` may not have been applied to the production Neon database, causing EF Core to throw when hydrating a `BlogPost` entity that now includes `PreviousStatus`. [Hypothesis — requires verification via Fly.io logs and DB state]

-> **SOLUTION A (Permanent)**: Verify migration is applied. Run `dotnet ef database update` against production, or confirm that `Database:RunMigrationsAtStartup: true` (the default, per `Program.cs` line 240) ran successfully on the last deploy.

---

### Branch B — The post does not exist in the database

**WHY 1B**: The slug `building-the-augmented-craftsman-a-blog-forged-with-xp-practices` does not match any row in the `BlogPosts` table, or it exists but with `Status != Published`.

**WHY 2B**: If the backend returns 404, `fetchPostBySlug` returns `null`. The Astro page then calls `Astro.redirect('/blog')` — this produces a 302, not a 500.
[Evidence: `api.ts` line 74; `[slug].astro` lines 14-16]

**WHY 3B**: A 404 path cannot produce the observed 500. Branch B is ruled out as a standalone cause.
[Evidence: Code path is deterministic — 404 -> null -> redirect]

-> **Branch B ELIMINATED**: A missing post alone cannot cause a 500. It would produce a redirect to `/blog`.

---

### Branch C — Astro SSR render throws after a successful API response

**WHY 1C**: `fetchPostBySlug` succeeds and returns a `BlogPost`. The Astro frontmatter then calls `renderMarkdown(post.content)`, which initialises a Shiki syntax highlighter singleton. An exception in `renderMarkdown` is unhandled and propagates as a 500.
[Evidence: `[slug].astro` line 32 — `const contentHtml = await renderMarkdown(post.content)` — no try/catch]

**WHY 2C**: `renderMarkdown` calls `createHighlighter()` from Shiki, which attempts to load language grammars and custom themes. In a Vercel serverless environment, file system access patterns differ from local. Shiki resolves WASM and grammar files relative to the module location; if bundling changes these paths, initialisation fails.
[Evidence: `markdown.ts` lines 129-135 — `createHighlighter` with custom theme objects and language list; Astro 5 → 6 upgrade (commit `4823da1`) changed the bundler configuration significantly (Vite plugin change, Tailwind v4)]

**WHY 3C**: The Astro 5.5 → 6.0.4 upgrade (commit `4823da1`) changed from `@astrojs/tailwind` integration to `@tailwindcss/vite` plugin, and updated `@astrojs/vercel` from v9 to v10. These changes alter the Vite build output. Shiki's `createHighlighter` in serverless environments is known to require the `bundledLanguages` API rather than dynamic language loading in some bundler configurations.
[Evidence: `markdown.ts` line 132 — language array passed as strings to `createHighlighter`, not using `bundledLanguages` import; `package.json` shows `"astro": "^6.0.4"`, `"@astrojs/vercel": "^10.0.0"`]

**WHY 4C**: The `markdown.ts` module was written for Astro 5 + Vercel adapter v9, where the Vite bundling produced a different output. No change was made to `markdown.ts` during the Astro 6 upgrade (commit `4823da1` — diff does not include `markdown.ts`).
[Evidence: `git show 4823da1 --stat` — `src/data/markdown.ts` not in the changed file list]

**WHY 5C**: The Shiki highlighter initialisation pattern uses `createHighlighter` with a mutable singleton (`highlighterPromise`). If the first serverless invocation fails (e.g., WASM load fails in cold start), subsequent requests reuse the failed promise, causing every request to that function instance to 500 until the function is recycled.
[Evidence: `markdown.ts` lines 126-136 — `let highlighterPromise: Promise<Highlighter> | null = null` module-level singleton; no error handling on the promise itself]

-> **ROOT CAUSE C**: The Shiki highlighter singleton caches a failed initialisation promise in serverless cold starts after the Astro 6 / Vercel adapter v10 upgrade, causing every subsequent `renderMarkdown` call on that function instance to reject. [Hypothesis — requires verification via Vercel function logs]

-> **SOLUTION C (Immediate mitigation)**: Add error handling in `renderMarkdown` to catch Shiki initialisation failures and fall back to plain-text rendering, preventing a cascade 500. Reset `highlighterPromise = null` on failure to allow retry.

-> **SOLUTION C (Permanent)**: Migrate `createHighlighter` to use Shiki's `createHighlighterCore` with `createOnigurumaEngine` and explicit `bundledLanguages` imports, which is the pattern recommended for serverless/bundled environments and is stable across Vite bundler versions.

---

### Branch D — Astro 6 `security.checkOrigin` intercepting the SSR GET

**WHY 1D**: `security.checkOrigin: true` was added in the Astro 6 upgrade (commit `4823da1`). Could this CSRF protection intercept a browser GET to `/blog/[slug]`?

**WHY 2D**: Per Astro documentation and the GitHub issue #15587, `checkOrigin` only applies to `POST`, `PATCH`, `DELETE`, `PUT` requests with form content-type headers. Browser navigation GET requests are explicitly excluded.
[Evidence: Astro docs; GitHub issue #15587 — "the check is executed only for POST, PATCH, DELETE and PUT"]

-> **Branch D ELIMINATED**: `checkOrigin` does not affect GET requests. Not a contributing cause.

---

## Cross-Validation

| Root Cause | Explains 500? | Contradictions? |
|---|---|---|
| A — Migration not applied, EF Core hydration throws | Yes — backend 500 propagates through `throw new Error()` in api.ts | None |
| C — Shiki init fails in Vercel serverless, singleton caches failure | Yes — unhandled promise rejection in Astro frontmatter | None |
| A + C | Both independently can produce 500; they are not mutually exclusive | None — could both be present |

The two surviving root causes are **independent and non-contradictory**. Either or both could be active.

**All symptoms explained**: A 500 from either branch explains the browser `ERR_HTTP_RESPONSE_CODE_FAILURE 500`.

---

## Verification Steps (ordered by likelihood)

1. **Check Vercel function logs** for the failing request. Look for:
   - `Failed to fetch post {slug}: 500` — indicates Root Cause A (backend).
   - Stack trace from Shiki / `markdown.ts` — indicates Root Cause C (Shiki).

2. **Check Fly.io application logs** around the time of the failure:
   - EF Core column errors (`column "PreviousStatus" does not exist`) — confirms Root Cause A.
   - Normal 200 responses for the slug — rules out Root Cause A.

3. **Check DB migration state** via Fly.io console:
   ```
   fly ssh console --app <your-app-name>
   dotnet ef migrations list
   ```
   Or query: `SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;`

4. **Reproduce locally** by hitting the production backend directly:
   ```
   curl https://<fly-app>.fly.dev/api/posts/building-the-augmented-craftsman-a-blog-forged-with-xp-practices
   ```
   - 200 → Root Cause A ruled out; investigate Root Cause C.
   - 500 → Root Cause A confirmed; apply migration.

---

## Solutions Summary

### Immediate Mitigations (restore service)

| ID | Action | Addresses |
|---|---|---|
| M1 | Apply pending migration to production DB (`fly ssh console` + `dotnet ef database update` or redeploy with `Database:RunMigrationsAtStartup=true`) | Root Cause A |
| M2 | Add try/catch in `renderMarkdown` with plain-text fallback; reset `highlighterPromise = null` on failure | Root Cause C |

### Permanent Fixes (prevent recurrence)

| ID | Action | Addresses |
|---|---|---|
| P1 | Confirm `Database:RunMigrationsAtStartup: true` runs reliably on every Fly.io deploy; add a startup health-check gate that fails the deploy if migrations are pending | Root Cause A |
| P2 | Migrate Shiki usage to `createHighlighterCore` + `bundledLanguages` + `createOnigurumaEngine` — the serverless-safe API that bundles all assets at build time | Root Cause C |
| P3 | Add a try/catch in the `[slug].astro` frontmatter around the `fetchPostBySlug` + `renderMarkdown` calls, returning a structured error page rather than propagating an unhandled rejection to Astro's 500 handler | Root Causes A + C |

### Early Detection

| ID | Action |
|---|---|
| D1 | Add Vercel error alerting or integrate with an error tracking service (e.g., Sentry) — currently the 500 is only visible via browser console with no server-side alert |
| D2 | Add an end-to-end smoke test that GETs a known published post slug after every production deploy |
| D3 | Add the `/health/ready` endpoint to Fly.io deploy checks — it already exists in `Program.cs`; wire it to the deploy release command |

---

## Most Likely Root Cause (ranked)

**Primary suspect: Root Cause A — Migration not applied.**

Rationale: The `author-mode` wave added the `PreviousStatus` column via migration `20260314160804` and was merged through multiple commits on 2026-03-14. If the Fly.io instance was not redeployed after this migration was added, or if `MigrateAsync()` failed silently, EF Core will throw a `PostgresException` when loading any `BlogPost` row. This exception is not caught anywhere in the request path and becomes a 500.

The blog post in the URL was presumably published before this wave, meaning it exists in the DB — the 404 path is not triggered — but the EF hydration crashes on the new column.

**Secondary suspect: Root Cause C — Shiki singleton caching a failed promise.**

This is more likely to manifest as intermittent failures (cold starts only) rather than a consistent 500 on every request.

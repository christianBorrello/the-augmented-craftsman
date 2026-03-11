# Dynamic Content on a Static Astro Frontend: Research & Best Practices

> **Research scope**: How to display dynamic features (likes, comments, shares) on a statically-generated Astro blog, and how real-world sites solve this problem.
>
> **Project context**: "The Augmented Craftsman" — Astro SSG frontend (Vercel) + .NET 10 backend (Fly.io), Hexagonal Architecture.
>
> **Date**: 2026-03-09

---

## 1. Executive Summary

Static Site Generation (SSG) delivers unmatched performance for content-heavy sites: pre-built HTML served from CDN edges, zero server compute per request, perfect Core Web Vitals. But dynamic features — likes, comments, real-time counts — require fresh data that doesn't exist at build time.

The industry has converged on a **layered strategy**: no single pattern fits all dynamic features. The best blogs choose the rendering approach per-feature based on interactivity requirements, data freshness needs, and JavaScript budget.

**Key finding**: The Augmented Craftsman's current implementation — Preact islands for likes, Server Islands for comments, Astro Actions for forms — already aligns with industry best practices. This document validates that architecture and identifies potential improvements.

---

## 2. The Problem Space

### The Static-Dynamic Tension

| Concern | Static (SSG) | Dynamic (SSR/CSR) |
|---------|-------------|-------------------|
| Performance | Pre-built HTML, CDN-cached | Server compute per request |
| Data freshness | Stale until rebuild | Fresh per request |
| JavaScript | Zero by default | Framework runtime required |
| SEO | Perfect (content in HTML) | Depends on rendering strategy |
| Cost | Near zero (CDN only) | Server hosting costs |
| Scalability | Infinite (CDN) | Requires scaling infrastructure |

### What Needs to Be Dynamic

| Feature | Freshness Need | Interactivity Need | Auth Need |
|---------|---------------|-------------------|-----------|
| Like count | Low (seconds stale OK) | High (toggle, animation) | None (anonymous) |
| Comment list | Medium (new comments visible) | Low (read-only display) | None (public) |
| Comment form | N/A (mutation) | Medium (form submit) | Yes (OAuth) |
| Share | N/A (client-only) | High (native API, clipboard) | None |
| User session | High (auth state) | Low (display name/avatar) | Yes |

---

## 3. Available Patterns

### 3.1 Client Islands (`client:*` directives)

**How it works**: Astro renders static HTML at build time. Interactive components (React, Preact, Svelte, etc.) "hydrate" on the client when triggered by a directive.

**Directives**:
- `client:load` — hydrate immediately on page load
- `client:idle` — hydrate when browser is idle (requestIdleCallback)
- `client:visible` — hydrate when component enters viewport (IntersectionObserver)
- `client:media="(max-width: 768px)"` — hydrate on media query match
- `client:only="preact"` — skip SSR, render only on client

**Pros**:
- Zero JS until explicitly opted in
- Fine-grained control over hydration timing
- Framework-agnostic (mix Preact, Svelte, Vue in same page)
- Static HTML still present for SEO and no-JS fallback

**Cons**:
- Requires shipping a framework runtime (~3KB for Preact, ~44KB for React)
- Data fetching happens client-side, exposing API endpoints
- Hydration adds a delay between visible HTML and interactive component

**Best for**: Features requiring instant user feedback — like buttons, share menus, interactive widgets.

**Sources**: [Astro Islands Documentation](https://docs.astro.build/en/concepts/islands/), [Astro Client Directives](https://docs.astro.build/en/reference/directives-reference/#client-directives)

---

### 3.2 Server Islands (`server:defer`)

**How it works**: Available since Astro 5. Components are excluded from the initial static build. At request time, a small inline script fetches the server-rendered HTML fragment and swaps it into a placeholder/fallback slot.

```astro
<CommentList server:defer slug={post.slug}>
  <div slot="fallback">Loading comments...</div>
</CommentList>
```

**Pros**:
- Zero client-side JavaScript (the swap script is minimal, framework-agnostic)
- Server has full access to databases, sessions, secrets
- API endpoints are never exposed to the client
- Static page shell loads instantly; dynamic content streams in
- Fallback slot provides immediate visual feedback

**Cons**:
- Requires a server-side adapter (Vercel, Node, Cloudflare)
- Adds a network round-trip per island at request time
- Cannot be used for client-side interactivity (no event handlers, no state)
- The deferred component is not in the initial HTML (affects SEO for that fragment)

**Best for**: Read-heavy dynamic content — comment lists, user-specific dashboards, personalized recommendations.

**Sources**: [Astro Server Islands](https://docs.astro.build/en/guides/server-islands/), [Astro 5 Blog Post](https://astro.build/blog/astro-5/)

---

### 3.3 Hybrid Rendering (`prerender = false`)

**How it works**: Astro 5 unified static and hybrid modes. Pages default to static prerendering, but individual pages can opt into server-side rendering.

```astro
---
// This page will be server-rendered on every request
export const prerender = false;
---
```

**Pros**:
- Full server access (sessions, cookies, database queries)
- Dynamic data in the initial HTML response (no flash of loading state)
- SEO-friendly (content is in the HTML)

**Cons**:
- The entire page becomes server-rendered, losing CDN edge caching
- Every request hits the server (higher latency, higher cost)
- Mixes rendering modes can be confusing to maintain

**Best for**: Pages that are predominantly dynamic (admin dashboards, user profiles). Not ideal for blog posts where 95% of the page is static content.

**Sources**: [Astro On-Demand Rendering](https://docs.astro.build/en/guides/on-demand-rendering/)

---

### 3.4 Astro Actions (Type-Safe Server Functions)

**How it works**: Define server-side functions with Zod-validated input schemas. Callable from client-side JavaScript or HTML forms with progressive enhancement.

```typescript
// src/actions/index.ts
export const server = {
  postComment: defineAction({
    accept: "form",
    input: z.object({
      slug: z.string(),
      text: z.string().min(1).max(2000),
    }),
    handler: async ({ slug, text }, context) => {
      // Server-side: access cookies, call APIs, write to DB
    },
  }),
};
```

**Progressive enhancement**: When used with `accept: "form"`, the action works as a standard HTML form POST — no JavaScript required. If JS is available, Astro enhances the form with client-side submission.

**Pros**:
- Type-safe end-to-end (Zod schema → TypeScript types)
- Progressive enhancement built in (works without JS)
- Server-side execution (access cookies, sessions, secrets)
- No need to build separate API endpoints

**Cons**:
- Pages using form-based actions must be server-rendered (`prerender = false`)
- Limited to form submissions (not suitable for real-time interactions)
- Relatively new API — evolving documentation

**Best for**: Form mutations — comment submission, contact forms, newsletter signup.

**Sources**: [Astro Actions](https://docs.astro.build/en/guides/actions/)

---

### 3.5 ISR — Incremental Static Regeneration (Vercel)

**How it works**: Server-rendered pages are cached at the CDN like static pages. Cache is invalidated either time-based (every N seconds) or on-demand via a revalidation API call.

```astro
---
// Cache for 60 seconds, revalidate in background
export const prerender = false;
Astro.response.headers.set(
  'Cache-Control',
  's-maxage=60, stale-while-revalidate=600'
);
---
```

**On-demand revalidation** (Vercel-specific):
```bash
# Invalidate a specific path when a new comment is approved
curl -X POST "https://yoursite.vercel.app/api/revalidate?path=/blog/my-post&secret=TOKEN"
```

**Pros**:
- Near-static performance for dynamic pages
- Fresh content without full rebuilds
- CDN-cached globally (low latency)
- On-demand invalidation for event-driven updates

**Cons**:
- Vercel-specific (vendor lock-in for on-demand revalidation)
- Cache invalidation is inherently complex ("two hard things in CS")
- Stale content during revalidation window
- Requires server-side adapter

**Best for**: Pages that change infrequently but need eventual freshness — blog posts with approved comments, product pages with reviews.

**Sources**: [Vercel ISR Documentation](https://vercel.com/docs/incremental-static-regeneration), [Vercel Astro Adapter](https://docs.astro.build/en/guides/deploy/vercel/)

---

### 3.6 Stale-While-Revalidate (SWR)

**How it works**: Show cached/stale data immediately, fetch fresh data in the background. Once fresh data arrives, update the UI. Applicable at two levels:

1. **HTTP cache level**: `Cache-Control: s-maxage=60, stale-while-revalidate=600`
2. **Client-side level**: Show cached count from localStorage/memory, fetch fresh via API

**Pros**:
- Instant perceived performance (no loading spinner)
- Fresh data arrives shortly after
- Reduces server load (serves stale during revalidation)

**Cons**:
- Users briefly see stale data (like count from last visit)
- Implementation complexity (cache management, race conditions)
- Not suitable for features requiring real-time accuracy

**Best for**: Like counts, view counts, reaction totals — where brief staleness (1-60 seconds) is acceptable.

**Sources**: [SWR RFC 5861](https://datatracker.ietf.org/doc/html/rfc5861), [HTTP Caching (MDN)](https://developer.mozilla.org/en-US/docs/Web/HTTP/Caching)

---

### 3.7 Optimistic UI Updates

**How it works**: Update the UI immediately assuming the server operation will succeed. If it fails, roll back to the previous state.

```typescript
// Pseudocode: optimistic like toggle
const handleLike = () => {
  setLiked(!liked);           // Optimistic: update UI immediately
  setCount(liked ? count - 1 : count + 1);

  api.toggleLike(slug)
    .catch(() => {
      setLiked(liked);        // Rollback on failure
      setCount(count);
    });
};
```

**Pros**:
- Feels instant to the user (zero perceived latency)
- Standard UX pattern (Instagram, Twitter, GitHub all use it)
- Reduces perceived wait time for network-dependent operations

**Cons**:
- Rollback logic adds complexity
- Brief inconsistency if server fails
- Not suitable for operations with complex server-side validation

**Best for**: Like/unlike toggles, bookmark actions, simple reactions — idempotent operations with predictable outcomes.

**Sources**: [Optimistic UI Patterns (Smashing Magazine)](https://www.smashingmagazine.com/2016/11/true-lies-of-optimistic-user-interfaces/), industry standard practice at Meta, GitHub, Twitter

---

### 3.8 View Transitions with Persistent Islands

**How it works**: Astro's `<ClientRouter />` enables SPA-like page transitions. `transition:persist` keeps island state (and DOM) across navigations.

```astro
<!-- Layout -->
<ClientRouter />

<!-- Component with persistent state -->
<LikeButton transition:persist client:visible slug={post.slug} />
```

**Pros**:
- Seamless page-to-page navigation (no full reload)
- Island state preserved (like button doesn't re-fetch on navigation)
- CSS-only transition animations
- Progressive enhancement (works without JS, just no transitions)

**Cons**:
- Adds client-side routing JavaScript
- Persistent islands can show stale data if the underlying page changes
- Complex interaction with Server Islands during transitions

**Best for**: Multi-page blogs where users navigate between posts and expect app-like fluidity.

**Sources**: [Astro View Transitions](https://docs.astro.build/en/guides/view-transitions/), [Astro ClientRouter](https://docs.astro.build/en/guides/view-transitions/#adding-view-transitions-to-a-page)

---

## 4. Real-World Site Analysis

### 4.1 Josh W. Comeau (joshwcomeau.com)

| Aspect | Implementation |
|--------|---------------|
| **Stack** | Next.js with SSG (`getStaticProps`) |
| **Likes** | Hearts system: up to 16 per visitor per post |
| **Storage** | MongoDB (likes), originally localStorage (was exploited) |
| **Deduplication** | Hashed IP + secret salt (moved from localStorage after abuse) |
| **JS payload** | React + React Spring (19.4KB) + Framer Motion (44.6KB) for animations |
| **API pattern** | Next.js API Routes (client-side fetch) |
| **Comments** | None — discussions externalized to Twitter/X |

**Key lesson**: Even a senior React developer chose to externalize comments. The hearts system (up to 16 per post) creates a more engaging interaction than a binary like/unlike. IP-based deduplication proved more robust than localStorage.

**Sources**: [How I Built My Blog](https://www.joshwcomeau.com/blog/how-i-built-my-blog/), [How I Built My Blog v2](https://www.joshwcomeau.com/blog/how-i-built-my-blog-v2/)

---

### 4.2 Lee Robinson (leerob.com)

| Aspect | Implementation |
|--------|---------------|
| **Stack** | Next.js App Router + Server Components |
| **Dynamic features** | View counts, guestbook |
| **Storage** | Postgres (Drizzle ORM), previously PlanetScale + Prisma |
| **Auth** | OAuth for guestbook entries |
| **Rendering** | Server Components for data fetching (zero client JS for reads) |

**Key lesson**: Migrated from PlanetScale to Postgres + Drizzle for better type safety. Server Components eliminated client-side data fetching for read operations — similar to Astro's Server Islands approach.

**Sources**: [GitHub repo](https://github.com/leerob/leerob.io), [My Stack](https://leerob.com/stack)

---

### 4.3 Dan Abramov (overreacted.io)

| Aspect | Implementation |
|--------|---------------|
| **Stack** | Gatsby → custom static site |
| **Dynamic features** | None — deliberately zero |
| **JavaScript** | Works 100% without JS |
| **Comments** | Externalized to Twitter/X |

**Key lesson**: A deliberate architectural choice. Not every blog needs dynamic features. The reading experience is prioritized over engagement metrics. Discussions happen where the audience already is (Twitter).

**Sources**: [GitHub repo](https://github.com/gaearon/overreacted.io)

---

### 4.4 Kent C. Dodds (kentcdodds.com)

| Aspect | Implementation |
|--------|---------------|
| **Stack** | Remix (React Router v7), full-stack |
| **Likes** | Team points system — reading earns points for your team |
| **Storage** | Prisma + Postgres + Redis + Fly.io |
| **Rendering** | Full SSR with Remix loaders/actions |
| **Comments** | None traditional — engagement through team system |

**Key lesson**: Replaced traditional likes with a gamified team system. Full SSR means no static/dynamic tension — everything is server-rendered. This works because Remix is optimized for server rendering, but adds hosting complexity.

**Sources**: [GitHub repo](https://github.com/kentcdodds/kentcdodds.com)

---

### 4.5 Astro Official Blog (astro.build/blog)

| Aspect | Implementation |
|--------|---------------|
| **Stack** | Astro (naturally) |
| **Dynamic features** | None on the blog itself |
| **JavaScript** | Zero for blog pages |
| **Engagement** | Discord and Twitter/X |

**Key lesson**: Even Astro's own team doesn't add likes/comments to their blog. Engagement is channeled to community platforms (Discord). This reinforces that dynamic features on a static blog are a deliberate architectural choice, not a requirement.

---

### 4.6 Dev.to (Forem platform)

| Aspect | Implementation |
|--------|---------------|
| **Stack** | Ruby on Rails + Preact (transitioning frontend) |
| **Reactions** | Multi-reaction: heart, unicorn, fire, mind-blown, raised-hands |
| **Deduplication** | One reaction per type per authenticated user |
| **Comments** | Full threaded system, OAuth-authenticated |
| **Rendering** | Server-rendered with progressive enhancement |

**Key lesson**: Multi-reaction system (5 types vs binary like) increases engagement. Open source (Forem) — architecture is well-documented. Progressive enhancement ensures the site works without JavaScript.

**Sources**: [Forem GitHub](https://github.com/forem/forem)

---

### 4.7 Hashnode

| Aspect | Implementation |
|--------|---------------|
| **Stack** | Next.js + GraphQL API |
| **Reactions** | Multi-type reactions synced in real-time |
| **Caching** | Stellate edge caching on GraphQL queries |
| **Architecture** | Event-driven cache invalidation |
| **Sync pattern** | Articles stored in object keyed by ID (O(1) lookups/updates) |

**Key lesson**: At scale, edge caching (Stellate) is essential for reaction reads. Event-driven invalidation keeps data fresh without polling. The O(1) lookup pattern for reaction state is an efficient client-side optimization.

**Sources**: [Hashnode Architecture](https://engineering.hashnode.com/hashnodes-overall-architecture), [Reaction Sync](https://engineering.hashnode.com/how-do-we-sync-reactions-across-hashnode)

---

### 4.8 Ghost

| Aspect | Implementation |
|--------|---------------|
| **Stack** | Node.js with self-consuming REST API |
| **Comments** | Tied to membership system (requires authentication) |
| **Headless limitation** | Headless mode loses comments, membership, Stripe integration |
| **Frontend** | Default Handlebars.js themes, or headless with any framework |

**Key lesson**: Ghost demonstrates the cost of coupling comments to the platform. Going headless (like using Astro as frontend) means losing the comment system. This validates the choice of building a custom comment system rather than depending on a CMS-provided one.

**Sources**: [Ghost JAMstack Documentation](https://docs.ghost.org/jamstack)

---

### 4.9 Hacker News

| Aspect | Implementation |
|--------|---------------|
| **Stack** | Arc (custom Lisp on MzScheme), single FreeBSD server |
| **Scale** | ~6 million daily requests |
| **Caching** | Nginx reverse proxy with same-box caching |
| **Rendering** | Server-rendered HTML, no client framework |
| **Voting** | Per-user-per-story auth tokens embedded in URLs |
| **API** | Firebase API for near-real-time public data access |

**Key lesson**: Simplicity at scale. A single server handles millions of requests with in-memory caching. No microservices, no Kubernetes, no client-side framework. Server-rendered HTML with minimal JavaScript. Proof that architectural simplicity can outperform complex distributed systems.

**Sources**: [HN Architecture Thread](https://news.ycombinator.com/item?id=28478379)

---

### 4.10 Lobste.rs

| Aspect | Implementation |
|--------|---------------|
| **Stack** | Ruby on Rails + MariaDB |
| **Rendering** | Traditional server-rendered MVC |
| **Voting** | All user votes have equal weight |
| **Caching** | Local file caching, most caching off for authenticated users |
| **Open source** | 3-clause BSD license |

**Key lesson**: Traditional server rendering remains viable for community sites. No need for client-side frameworks when the interaction model is simple (vote and submit).

**Sources**: [Lobsters GitHub](https://github.com/lobsters/lobsters)

---

## 5. Comment Systems Comparison

| System | Backing Store | JS Impact | Auth Required | Reactions | Privacy | Cost |
|--------|-------------|-----------|---------------|-----------|---------|------|
| **Giscus** | GitHub Discussions | Iframe (isolated) | GitHub account | Yes (emoji) | Good (GitHub TOS) | Free |
| **Utterances** | GitHub Issues | Iframe (vanilla TS) | GitHub account | Limited | Good | Free |
| **Disqus** | Disqus servers | **2.49MB**, 34 files, ~3s execution | Optional (guest OK) | Yes | Poor (tracking, ads) | Free (with ads) / Paid |
| **Webmentions** | W3C standard + brid.gy | Custom JS for fetching | None (decentralized) | Via social bridging | Excellent (W3C standard) | Free |
| **Lyket** | Lyket API | Small widget | None | Like/clap/vote | Moderate | Free tier / Paid |
| **Custom** | Your own database | You control | You control | You build | Full control | Your hosting |

### Why Custom Wins for a Craftsman Blog

1. **Full architectural control**: Aligns with Hexagonal Architecture — comments are a driven port
2. **Zero bloat**: No 2.49MB Disqus payload, no third-party iframes
3. **Privacy**: No tracking pixels, no ad networks, no data sharing
4. **Portfolio value**: Demonstrates the ability to build the whole system
5. **Design consistency**: Comments match the "Forge & Ink" design system perfectly
6. **Progressive enhancement**: Astro Actions make forms work without JavaScript

The trade-off is maintenance burden, but for a craftsman blog, that maintenance IS the product.

---

## 6. Current Implementation Assessment

### Architecture Diagram

```
┌─────────────────────────────────────────────────┐
│  Blog Post Page (Static HTML, CDN-cached)       │
│                                                 │
│  ┌──────────────────────┐  ┌─────────────────┐  │
│  │  LikeButton.tsx      │  │ Article Content  │  │
│  │  (Preact Island)     │  │ (Static HTML)    │  │
│  │  client:visible      │  │                  │  │
│  │  ┌────────────────┐  │  │  Pre-rendered    │  │
│  │  │ Optimistic UI  │  │  │  Markdown → HTML │  │
│  │  │ Particle Anim  │  │  │                  │  │
│  │  │ Share Popover  │  │  │                  │  │
│  │  └────────────────┘  │  └─────────────────┘  │
│  └──────┬───────────────┘                       │
│         │ fetch()                               │
│  ┌──────┴───────────────────────────────────────┤
│  │  CommentList.astro (Server Island)           │
│  │  server:defer                                │
│  │  ┌─────────────┐  ┌──────────────────────┐   │
│  │  │ Comment List │  │ Comment Form         │   │
│  │  │ (HTML)       │  │ (Astro Action)       │   │
│  │  │ server-side  │  │ accept: "form"       │   │
│  │  │ rendered     │  │ progressive enhance  │   │
│  │  └─────────────┘  └──────────────────────┘   │
│  └──────┬───────────────────────────────────────┤
│         │                                       │
└─────────┼───────────────────────────────────────┘
          │ HTTP (server-side fetch / form POST)
          ▼
┌─────────────────────────┐
│  .NET 10 Backend API    │
│  (Fly.io)               │
│  /api/posts/{slug}/...  │
│  /api/auth/session      │
│  /api/auth/oauth/...    │
└─────────────────────────┘
```

### Per-Feature Assessment

| Feature | Pattern | JS Cost | Freshness | Assessment |
|---------|---------|---------|-----------|------------|
| **Like button** | Preact island `client:visible` | ~3KB (shared Preact runtime) | Real-time (client fetch) | **Optimal** — matches Josh Comeau's approach |
| **Like animation** | CSS + Preact state | 0KB extra | N/A | **Optimal** — particle animation in CSS/Preact, not a heavy library |
| **Share menu** | Web Share API + fallback | 0KB extra (in LikeButton) | N/A | **Optimal** — native-first with graceful degradation |
| **Comment list** | Server Island `server:defer` | 0KB | Fresh per request | **Optimal** — zero client JS, server has session access |
| **Comment form** | Astro Actions `accept: "form"` | 0KB | N/A (mutation) | **Optimal** — progressive enhancement, works without JS |
| **OAuth flow** | Server-side redirect | 0KB | N/A | **Optimal** — secure, no client-side tokens |
| **Visitor ID** | UUID in `window.__visitorId` | ~0.1KB (inline script) | N/A | **Good** — simple, anonymous, no tracking service |

**Overall**: The current implementation is well-architected. Total client JS for dynamic features is approximately **3-4KB** (shared Preact runtime), compared to 2.49MB for Disqus or 64KB+ for Josh Comeau's animation libraries.

---

## 7. Recommended Architecture

The current layered strategy is validated by this research. Here is the recommended architecture matrix:

| Feature | Rendering Pattern | Data Pattern | JS Budget | Why |
|---------|------------------|-------------|-----------|-----|
| **Blog content** | Static (SSG) | Build-time | 0KB | Content doesn't change between builds |
| **Like button** | Client Island (`client:visible`) | Client-side fetch + Optimistic UI | ~3KB shared | Needs instant feedback, animation, toggle state |
| **Like count** | Client Island (inside LikeButton) | Client-side fetch (consider SWR) | 0KB extra | Fetched as part of like button hydration |
| **Share** | Client Island (inside LikeButton) | Client-only (Web Share API) | 0KB extra | Needs clipboard and native share API access |
| **Comment list** | Server Island (`server:defer`) | Server-side fetch | 0KB | Read-heavy, server has session, no client JS needed |
| **Comment form** | Astro Action (`accept: "form"`) | Form POST (progressive enhancement) | 0KB | Works without JS, type-safe, server-side validation |
| **User session** | Server-side (cookie) | HTTP-only cookie | 0KB | Secure, never exposed to client |

---

## 8. Potential Improvements

### 8.1 Stale-While-Revalidate for Like Counts

**Current**: LikeButton fetches the like count on every hydration (when component becomes visible).

**Improvement**: Cache the last-known count in `localStorage`, display it immediately, then fetch the fresh count in the background.

```typescript
// Pseudocode
const cachedCount = localStorage.getItem(`likes-${slug}`);
setCount(cachedCount ? parseInt(cachedCount) : 0);  // Show cached immediately

fetchLikeCount(slug).then(freshCount => {
  setCount(freshCount);                              // Update with fresh
  localStorage.setItem(`likes-${slug}`, freshCount); // Cache for next visit
});
```

**Impact**: Eliminates the "0 → N" flash when like count loads. Users see a reasonable number immediately.

**Priority**: Low — only noticeable on slow connections.

---

### 8.2 ISR for Post Pages with Comments

**Current**: Post pages are fully static. Comments load via Server Island (separate request).

**Improvement**: Use Vercel ISR to cache the entire server-rendered page (including comments) at the CDN, with on-demand revalidation when a new comment is approved.

```
Backend approves comment
  → POST to Vercel Revalidation API
    → CDN cache invalidated for /blog/{slug}
      → Next visitor gets fresh page with new comment baked in
```

**Impact**: Comments appear in the initial HTML (better SEO, faster perceived load). But adds complexity in cache invalidation from external backend.

**Priority**: Medium — valuable if comment volume grows. Currently, Server Islands provide a good enough experience.

---

### 8.3 Webhook-Based Cache Invalidation

**Current**: No cache invalidation mechanism between .NET backend and Vercel frontend.

**Improvement**: When the backend approves a comment or a post is updated, fire a webhook to Vercel's ISR revalidation endpoint.

```csharp
// In .NET ApproveComment use case
await _vercelRevalidation.InvalidatePath($"/blog/{comment.PostSlug}");
```

**Impact**: Enables ISR (8.2). Requires a Vercel Deploy Hook or ISR bypass token stored in backend configuration.

**Priority**: Medium — dependent on ISR adoption.

---

### 8.4 View Transitions with Persistent Islands

**Current**: Each page navigation fully reloads the LikeButton island (re-fetch, re-hydrate).

**Improvement**: Add `<ClientRouter />` to the layout and `transition:persist` to the LikeButton.

**Impact**: Seamless navigation between posts. LikeButton state preserved (no re-fetch). But requires careful testing of Server Island interaction during transitions.

**Priority**: Low — enhances UX polish but adds complexity. Consider when the blog has enough content for frequent post-to-post navigation.

---

## 9. Knowledge Gaps & Open Questions

### 9.1 Server Islands + Astro Actions Interaction

**Question**: Can an Astro Action form live inside a `server:defer` component?

**Current behavior**: The CommentList uses `server:defer` and contains a form that submits via Astro Actions. This works in the current implementation, but official documentation doesn't explicitly address this pattern.

**Risk**: Future Astro updates could change how Actions interact with deferred components.

**Mitigation**: Monitor Astro release notes. Consider adding an integration test that validates this pattern survives upgrades.

---

### 9.2 ISR Cache Invalidation from External Backend

**Question**: How to reliably invalidate Vercel's ISR cache from a .NET backend running on Fly.io?

**Current documentation**: Vercel documents ISR for Next.js extensively, but Astro + external backend is less covered.

**Options to investigate**:
1. Vercel Deploy Hooks (triggers full rebuild — too heavy)
2. `@vercel/og` ISR bypass token (designed for Next.js)
3. Custom Vercel Serverless Function as invalidation endpoint
4. `Cache-Control: s-maxage=60, stale-while-revalidate` (time-based, no explicit invalidation)

---

### 9.3 View Transitions + Server Islands During Navigation

**Question**: When `<ClientRouter />` performs a client-side navigation, does `server:defer` content re-fetch correctly?

**Expected**: Yes — the swap script should re-execute for the new page. But persistent islands (`transition:persist`) might show stale data from the previous page.

**Mitigation**: Test thoroughly before adopting View Transitions. Consider not persisting the LikeButton if it shows data for the wrong post.

---

## 10. Sources & References

### Official Documentation
- [Astro Islands Architecture](https://docs.astro.build/en/concepts/islands/)
- [Astro Client Directives](https://docs.astro.build/en/reference/directives-reference/#client-directives)
- [Astro Server Islands](https://docs.astro.build/en/guides/server-islands/)
- [Astro On-Demand Rendering](https://docs.astro.build/en/guides/on-demand-rendering/)
- [Astro Actions](https://docs.astro.build/en/guides/actions/)
- [Astro View Transitions](https://docs.astro.build/en/guides/view-transitions/)
- [Astro 5 Release Blog](https://astro.build/blog/astro-5/)
- [Vercel ISR Documentation](https://vercel.com/docs/incremental-static-regeneration)
- [Vercel Astro Adapter](https://docs.astro.build/en/guides/deploy/vercel/)

### Real-World Implementations
- [Josh Comeau — How I Built My Blog](https://www.joshwcomeau.com/blog/how-i-built-my-blog/)
- [Josh Comeau — How I Built My Blog v2](https://www.joshwcomeau.com/blog/how-i-built-my-blog-v2/)
- [Lee Robinson — GitHub repo](https://github.com/leerob/leerob.io)
- [Dan Abramov — overreacted.io GitHub](https://github.com/gaearon/overreacted.io)
- [Kent C. Dodds — GitHub repo](https://github.com/kentcdodds/kentcdodds.com)
- [Forem (Dev.to) — GitHub repo](https://github.com/forem/forem)
- [Hashnode Architecture](https://engineering.hashnode.com/hashnodes-overall-architecture)
- [Hashnode Reaction Sync](https://engineering.hashnode.com/how-do-we-sync-reactions-across-hashnode)
- [Ghost JAMstack Documentation](https://docs.ghost.org/jamstack)
- [Lobsters — GitHub repo](https://github.com/lobsters/lobsters)
- [Hacker News Architecture Thread](https://news.ycombinator.com/item?id=28478379)

### Standards & Specifications
- [RFC 5861 — HTTP Cache-Control Extensions for Stale Content](https://datatracker.ietf.org/doc/html/rfc5861)
- [MDN — HTTP Caching](https://developer.mozilla.org/en-US/docs/Web/HTTP/Caching)
- [Webmentions W3C Recommendation](https://www.w3.org/TR/webmention/)

### Articles & Analysis
- [Smashing Magazine — Optimistic UI Patterns](https://www.smashingmagazine.com/2016/11/true-lies-of-optimistic-user-interfaces/)
- [Islands Architecture (patterns.dev)](https://www.patterns.dev/posts/islands-architecture)

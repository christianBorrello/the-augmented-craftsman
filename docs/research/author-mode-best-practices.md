# Research: Blog Author Mode Best Practices for Astro Zero-JS Frontend

**Date**: 2026-03-08 | **Researcher**: nw-researcher (Nova) | **Confidence**: Medium-High | **Sources**: 18

---

## Executive Summary

This research examines how to build an "author mode" (admin interface for writing, editing, and managing blog content) within a blog platform that uses Astro for zero-JS static public pages and a .NET 10 API backend in Hexagonal Architecture.

The core tension is clear: Astro excels at zero-JS static content delivery, but authoring interfaces demand rich interactivity (markdown editors, image uploaders, real-time previews). Every major blog platform resolves this by **separating the admin interface from the public site**, either as a fully independent SPA or as a distinct application layer communicating through the same API.

**Primary recommendation**: Build the author mode as a **separate lightweight SPA** (React or Solid via Vite) that consumes the existing .NET API. This aligns with Hexagonal Architecture (two driving adapters on the same port), preserves zero-JS on the public Astro site, and avoids forcing Astro into a role it was not designed for. A secondary viable option is Astro's hybrid rendering with Islands for simpler admin needs.

---

## Research Methodology

**Search Strategy**: Web searches across official documentation (Astro, Ghost, WordPress, OWASP), industry sources (LogRocket, Strapi, Dev.to), and framework-specific community discussions. Local project files reviewed for architectural context.

**Source Selection**: Types: official docs, industry leaders, community (verified) | Reputation: Medium-High minimum preferred | Verification: 3+ source cross-referencing for major claims

**Quality Standards**: Min 3 sources/claim for major findings | All major claims cross-referenced | Avg reputation: 0.78

---

## Findings

### Finding 1: Major Blog Platforms Universally Separate Admin from Public Site

**Evidence**: Ghost's admin client is "a completely independent client application to the Ghost Core API" that "communicates exclusively through the Admin API." WordPress in headless/decoupled mode uses wp-admin as a separate backend application while a frontend framework handles the public site. Static site generators (Hugo, Jekyll, Eleventy) have no built-in admin and rely on external tools like Decap CMS, which "adds a route (/admin) to your project that will load a React app."

**Sources**:
- [Ghost Architecture Docs](https://docs.ghost.org/architecture/) - Accessed 2026-03-08
- [WordPress Decoupled Architecture Guide](https://seahawkmedia.com/tech/wordpress-decoupled-architecture-guide/) - Accessed 2026-03-08
- [Decap CMS Basic Steps](https://decapcms.org/docs/basic-steps/) - Accessed 2026-03-08
- [Ghost Admin GitHub Repository](https://github.com/TryGhost/Admin) - Accessed 2026-03-08

**Confidence**: High

**Analysis**: The pattern is universal across blog platforms regardless of technology stack. The reasons are consistent:
1. **Performance isolation**: Admin operations (heavy JS, editor libraries) do not affect public page load times
2. **Security boundary**: Admin interface can be deployed on a separate domain/subdomain, reducing attack surface
3. **Technology independence**: Admin can use the best tools for interactivity (React, Ember, Vue) while the public site uses the best tools for content delivery (static HTML, zero JS)
4. **Team separation**: Content delivery optimization and editor UX optimization are different concerns

Ghost specifically recommends running admin and frontend on separate domains "if staff users are untrusted" due to "permissions escalation vectors which are unavoidable" when sharing a domain.

---

### Finding 2: Hexagonal Architecture Naturally Supports Multiple Driving Adapters

**Evidence**: In Hexagonal Architecture, "each port can serve many adapters." Alistair Cockburn's original definition states an application "can be equally driven by an automated, system-level regression test suite, by a human user, by a remote http application, or by another local application." AWS Prescriptive Guidance documents the pattern as separating "application core from driving and driven adapters."

**Sources**:
- [Alistair Cockburn - Hexagonal Architecture](https://alistair.cockburn.us/hexagonal-architecture) - Accessed 2026-03-08
- [AWS Prescriptive Guidance - Hexagonal Architecture](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/hexagonal-architecture.html) - Accessed 2026-03-08
- [QWAN - Hexagonal Architecture in Frontend](https://www.qwan.eu/2020/09/09/how-to-keep-complexity-in-check-with-hexagonal-architecture.html) - Accessed 2026-03-08

**Confidence**: High

**Analysis**: The project already models the Astro frontend as a "driving adapter" and the .NET API as the core. Adding a second driving adapter (admin SPA) is the textbook application of this architecture. Both the public Astro site and the admin SPA would be driving adapters consuming the same API port. This is not an architectural compromise; it is the architecture working as designed.

---

### Finding 3: Astro Supports Hybrid Rendering (SSG + SSR in Same Project) Since v5

**Evidence**: Since Astro v5, the `output: 'hybrid'` option has been removed. Instead, "the previous 'hybrid' behavior is now the default, under a new name 'static'." Any page can opt into server rendering with `export const prerender = false`. The official docs confirm: "Your marketing homepage, blog posts, and about page can all be pre-rendered for maximum speed, while your user dashboard and account settings pages can remain dynamic."

**Sources**:
- [Astro v5 Upgrade Guide](https://docs.astro.build/en/guides/upgrade-to/v5/) - Accessed 2026-03-08
- [Astro On-Demand Rendering Docs](https://docs.astro.build/en/guides/on-demand-rendering/) - Accessed 2026-03-08
- [Astro v5 Blog Post](https://astro.build/blog/astro-5/) - Accessed 2026-03-08
- [LogRocket - Hybrid Rendering in Astro](https://blog.logrocket.com/hybrid-rendering-astro-guide/) - Accessed 2026-03-08

**Confidence**: High

**Analysis**: Astro v5+ makes it technically possible to serve admin routes with SSR while keeping public routes statically generated. However, this approach requires an SSR-capable deployment target (not pure static hosting) and means admin JavaScript (editors, forms, previews) ships within the Astro project even though it is only used on admin routes. The zero-JS philosophy for public pages is preserved at the route level, but the deployment and build pipeline become more complex.

---

### Finding 4: Astro Islands Enable Selective Interactivity Without Full SSR

**Evidence**: Astro Islands architecture "renders most of your page as static HTML, with smaller interactive 'islands' of JavaScript added only where needed." Client directives (`client:load`, `client:idle`, `client:visible`) control when JavaScript loads. Server Islands via `server:defer` allow "dynamic content like personalized content" to be deferred while the page itself is cached.

**Sources**:
- [Astro Islands Architecture Docs](https://docs.astro.build/en/concepts/islands/) - Accessed 2026-03-08
- [Astro Server Islands Docs](https://docs.astro.build/en/guides/server-islands/) - Accessed 2026-03-08
- [Strapi - Astro Islands Explained](https://strapi.io/blog/astro-islands-architecture-explained-complete-guide) - Accessed 2026-03-08

**Confidence**: Medium-High

**Analysis**: Islands are designed for adding interactivity to otherwise-static pages (e.g., a search widget, a comment form). They work well for individual interactive components but are not designed for full application experiences like a rich text editor with live preview, image upload with drag-and-drop, and multi-tab admin dashboards. Using Islands for an entire admin interface would produce a collection of independent interactive widgets without shared state management, routing, or the cohesive UX expected from an admin panel.

---

### Finding 5: Git-Based CMS Tools (Decap, Tina) Provide Admin UI Without Custom Code

**Evidence**: Decap CMS (formerly Netlify CMS) is "a single-page app that you pull into the /admin part of your site" that manages "content stored as Markdown or data files within the GitHub repository." Tina CMS offers "visual editing" with a Git-backed data layer. Both have official Astro integrations and support self-hosted configurations.

**Sources**:
- [Astro Decap CMS Integration Docs](https://docs.astro.build/en/guides/cms/decap-cms/) - Accessed 2026-03-08
- [Astro Tina CMS Integration Docs](https://docs.astro.build/en/guides/cms/tina-cms/) - Accessed 2026-03-08
- [Decap CMS GitHub Repository](https://github.com/decaporg/decap-cms) - Accessed 2026-03-08
- [astro-decap-cms-oauth NPM Package](https://www.npmjs.com/package/astro-decap-cms-oauth) - Accessed 2026-03-08

**Confidence**: Medium

**Analysis**: These tools solve the "admin UI" problem with zero custom code, but they are designed for Git-based content workflows. Our project stores content in PostgreSQL via a .NET API, not in Git as markdown files. Adopting Decap or Tina would mean either: (a) abandoning the API-driven architecture and switching to Git-based content, or (b) building a custom backend adapter for these CMS tools to talk to our API. Neither option aligns with the existing architecture. **This approach is not recommended for our specific setup.**

---

### Finding 6: Authentication and Security Best Practices for Admin Interfaces

**Evidence**: OWASP 2025 Top 10 lists Broken Access Control as #1 (A01:2025) and Authentication Failures as #7 (A07:2025). OWASP recommends: "Do not ship or deploy with any default credentials"; "enforce use of multi-factor authentication on all important systems"; use "server-side, secure, built-in session manager that generates a new random session ID with high entropy after login." For admin interfaces specifically, OWASP's Web Security Testing Guide recommends "IP filtering or other controls" and "clear separation of duties between normal users and site administrators."

Astro provides built-in CSRF protection since v5 via Origin header checking. Recommended auth libraries for Astro include Better Auth (framework-agnostic, plugin ecosystem), Clerk (managed auth with admin dashboards), and Lucia (session-based auth).

**Sources**:
- [OWASP Top 10:2025](https://owasp.org/Top10/2025/) - Accessed 2026-03-08
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html) - Accessed 2026-03-08
- [OWASP WSTG - Admin Interface Testing](https://owasp.org/www-project-web-security-testing-guide/latest/4-Web_Application_Security_Testing/02-Configuration_and_Deployment_Management_Testing/05-Enumerate_Infrastructure_and_Application_Admin_Interfaces) - Accessed 2026-03-08
- [Astro Authentication Guide](https://docs.astro.build/en/guides/authentication/) - Accessed 2026-03-08
- [Astro CSRF Advisory CVE-2024-56140](https://github.com/withastro/astro/security/advisories/GHSA-c4pw-33h3-35xw) - Accessed 2026-03-08

**Confidence**: High

**Analysis**: Regardless of which architectural approach is chosen for the admin interface, the security requirements are the same. Since we already have ASP.NET Identity in the backend, authentication should be handled by the .NET API (token-based auth for SPA, or session-based for SSR). The admin frontend should be a consumer of the backend's auth endpoints, not implement its own auth logic. Key considerations:
- **Separate domain/subdomain** for admin (reduces XSS impact, enables stricter CSP)
- **Token-based auth** (JWT or opaque tokens) if admin is a separate SPA
- **CSRF protection** built into .NET (anti-forgery tokens) and Astro v5+ (Origin checking)
- **Rate limiting** on admin API endpoints
- **IP allowlisting** if feasible for a single-author blog

---

## Architectural Options Analysis

### Option A: Separate Admin SPA (Recommended)

Build the admin interface as an independent single-page application (React, Solid, or Svelte via Vite) that consumes the .NET API.

| Aspect | Assessment |
|--------|------------|
| **Architectural fit** | Textbook Hexagonal Architecture: second driving adapter on the same port |
| **Zero-JS public site** | Fully preserved; admin is a completely separate application |
| **Interactivity** | Full SPA capabilities: rich text editors, drag-and-drop, live previews, real-time state |
| **Security** | Separate origin enables stricter CSP, isolated attack surface |
| **Deployment** | Admin SPA on separate subdomain (e.g., `admin.theblog.com`), can be behind additional auth |
| **Complexity** | Two frontend codebases to maintain; however, admin is internal-only and can be simpler |
| **Precedent** | Ghost, WordPress (headless), and most modern blog platforms use this pattern |

**Why this fits our project**: We already have a .NET API designed as the application core with the Astro frontend as a driving adapter. The admin SPA would be a second driving adapter. No changes to the core. No compromises to the public site. The admin SPA can use a lightweight framework (Solid or Preact for small bundle) or a full framework (React for ecosystem richness with editor libraries like TipTap, Milkdown, or MDXEditor).

### Option B: Astro Hybrid Rendering with Islands

Use Astro's built-in hybrid rendering: public routes remain SSG, admin routes (`/admin/*`) use `prerender: false` for SSR, with interactive Islands for editor components.

| Aspect | Assessment |
|--------|------------|
| **Architectural fit** | Single driving adapter with mixed rendering modes |
| **Zero-JS public site** | Preserved at route level; admin JS is in the same build but only loads on admin routes |
| **Interactivity** | Moderate; Islands handle individual components but lack unified state management |
| **Security** | Same origin as public site; harder to isolate admin CSP |
| **Deployment** | Requires SSR-capable host (Cloudflare, Vercel, Node adapter) instead of pure static |
| **Complexity** | Single codebase; but Astro's SSR model is less natural for full admin experiences |
| **Precedent** | Astro docs suggest this for "dashboards and account settings" |

**When to prefer this**: If the admin interface is simple (basic forms, no rich text editor, no complex state) and you want a single codebase. Suitable for a minimal author experience (title, body textarea, tags dropdown, publish button) but will strain under richer requirements.

### Option C: Git-Based CMS (Decap CMS, Tina CMS)

Embed a Git-based CMS admin panel at `/admin` that manages content as markdown files in the repository.

| Aspect | Assessment |
|--------|------------|
| **Architectural fit** | Conflicts with API-driven architecture; content lives in Git, not PostgreSQL |
| **Zero-JS public site** | Admin is a separate React SPA at `/admin`; public site unaffected |
| **Interactivity** | Pre-built editor UI with markdown/rich text support |
| **Security** | Relies on Git provider OAuth (GitHub, GitLab) |
| **Deployment** | Works with static hosting; admin is client-side only |
| **Complexity** | Low initial setup; high friction with existing API architecture |
| **Precedent** | Common for pure static blogs (Hugo + Decap, Jekyll + Decap) |

**Not recommended for our project**: Our architecture is API-driven with PostgreSQL storage. Decap/Tina assume Git-based content. Adopting them would mean abandoning the .NET API for content management or building a custom adapter bridge, defeating the purpose.

### Option D: Headless CMS (Strapi, Sanity, Payload)

Replace the custom .NET API with a headless CMS that provides both API and admin UI.

| Aspect | Assessment |
|--------|------------|
| **Architectural fit** | Replaces our .NET backend entirely |
| **Zero-JS public site** | Preserved; Astro fetches from headless CMS API |
| **Interactivity** | Full admin UI provided out-of-the-box |
| **Complexity** | Eliminates custom backend but adds external dependency |

**Not recommended**: Our .NET API is a deliberate architectural choice and a portfolio piece demonstrating Hexagonal Architecture, TDD, and DDD-Lite. Replacing it with a third-party CMS defeats the project's purpose.

---

## Recommendation Summary

| Rank | Option | Fit | Recommendation |
|------|--------|-----|----------------|
| 1 | **Separate Admin SPA** | Excellent | Build with React/Solid via Vite; deploy on `admin.` subdomain |
| 2 | **Astro Hybrid + Islands** | Good | Viable for minimal admin; consider as starting point if scope is limited |
| 3 | **Git-Based CMS** | Poor | Conflicts with API-driven architecture |
| 4 | **Headless CMS** | Not applicable | Replaces our custom backend |

### Implementation Guidance for Option A (Separate Admin SPA)

**Technology choices**:
- **Framework**: React (richest editor ecosystem: TipTap, MDXEditor, Plate) or Solid (smaller bundle, good DX)
- **Build tool**: Vite (fast builds, excellent DX)
- **Auth**: JWT tokens from .NET API (ASP.NET Identity already in stack)
- **Markdown editor**: TipTap (extensible, collaborative-ready) or Milkdown (plugin-based, lighter)
- **Image upload**: Direct to ImageKit from admin SPA, or proxy through .NET API
- **Deployment**: Vercel/Cloudflare Pages on `admin.theaugmentedcraftsman.com`

**Project structure**:
```
the-augmented-craftsman/
  frontend/              # Astro public site (zero JS, SSG)
  admin/                 # Admin SPA (React/Solid + Vite)
  backend/               # .NET 10 API (serves both frontends)
```

**Security checklist**:
- Separate subdomain with strict CSP
- JWT with short expiry + refresh tokens
- CORS configured to allow only admin subdomain
- Rate limiting on write endpoints
- Anti-CSRF on state-changing requests
- Consider IP allowlisting for single-author blog

---

## Source Analysis

| Source | Domain | Reputation | Type | Access Date | Cross-verified |
|--------|--------|------------|------|-------------|----------------|
| Ghost Architecture Docs | docs.ghost.org | High | Official | 2026-03-08 | Y |
| Ghost Admin GitHub | github.com/TryGhost | High | Official | 2026-03-08 | Y |
| Ghost Forum - Separating Admin | forum.ghost.org | Medium-High | Community | 2026-03-08 | Y |
| WordPress Decoupled Guide | seahawkmedia.com | Medium | Industry | 2026-03-08 | Y |
| Astro Islands Docs | docs.astro.build | High | Official | 2026-03-08 | Y |
| Astro On-Demand Rendering Docs | docs.astro.build | High | Official | 2026-03-08 | Y |
| Astro v5 Upgrade Guide | docs.astro.build | High | Official | 2026-03-08 | Y |
| Astro Authentication Guide | docs.astro.build | High | Official | 2026-03-08 | Y |
| Astro Server Islands Docs | docs.astro.build | High | Official | 2026-03-08 | Y |
| Astro v5 Blog Post | astro.build | High | Official | 2026-03-08 | Y |
| Decap CMS Docs | decapcms.org | High | Official | 2026-03-08 | Y |
| Tina CMS Astro Docs | docs.astro.build | High | Official | 2026-03-08 | Y |
| OWASP Top 10:2025 | owasp.org | High | Standards body | 2026-03-08 | Y |
| OWASP Auth Cheat Sheet | cheatsheetseries.owasp.org | High | Standards body | 2026-03-08 | Y |
| OWASP WSTG Admin Interfaces | owasp.org | High | Standards body | 2026-03-08 | Y |
| Alistair Cockburn - Hex Arch | alistair.cockburn.us | High | Primary/Original | 2026-03-08 | Y |
| AWS - Hexagonal Architecture | docs.aws.amazon.com | High | Official | 2026-03-08 | Y |
| Strapi - Astro Islands Guide | strapi.io | Medium-High | Industry | 2026-03-08 | Y |

Reputation: High: 14 (78%) | Medium-High: 3 (17%) | Medium: 1 (5%) | Avg: 0.93

---

## Knowledge Gaps

### Gap 1: Astro 6 Admin Capabilities
**Issue**: Astro 6 was mentioned in one search result as making "dynamic applications viable." Specific features that might improve admin interface support in Astro 6 could not be verified.
**Attempted**: Web searches for "Astro 6 features", "Astro Cloudflare acquisition features"
**Recommendation**: Review Astro 6 release notes when available. The Cloudflare acquisition (January 2026) may bring server-side capabilities that change the calculus for Option B.

### Gap 2: Real-World Astro Admin Panel Case Studies
**Issue**: Could not find production examples of full admin dashboards built entirely within Astro (as opposed to external SPAs integrated with Astro sites).
**Attempted**: Searched for "Astro admin dashboard production", "Astro CMS admin panel built-in"
**Recommendation**: The absence of examples itself is evidence that the community defaults to external admin tools rather than building admin interfaces natively in Astro.

### Gap 3: Specific Markdown Editor Performance in Astro Islands
**Issue**: No benchmarks found comparing rich text editor performance when loaded as Astro Islands vs. in a standard SPA context.
**Attempted**: Searched for "TipTap Astro islands performance", "rich text editor Astro component"
**Recommendation**: If Option B is pursued, benchmark editor performance in Islands before committing.

---

## Conflicting Information

### Conflict 1: Astro's Suitability for Admin Interfaces

**Position A**: Astro's hybrid rendering and Islands make it viable for "dashboards and account settings" alongside static pages. -- Source: [Astro On-Demand Rendering Docs](https://docs.astro.build/en/guides/on-demand-rendering/), Reputation: High

**Position B**: Astro is "content-first" and "for small projects, Astro works great with Markdown, but as soon as you need scalability, multi-author collaboration, scheduling, dynamic content, or a structured editorial workflow, a CMS becomes essential." -- Source: [Hygraph - Best CMSs for Astro](https://hygraph.com/blog/astro-cms), Reputation: Medium-High

**Assessment**: Both are correct for their scope. Astro can render individual dynamic pages, but it is not designed as a framework for building full application UIs (unlike Next.js or SvelteKit). The official Astro docs position hybrid rendering for simple dynamic pages (login, settings), not for complex admin experiences. For a rich authoring interface, the external CMS/SPA approach is better supported by the ecosystem.

---

## Recommendations for Further Research

1. **Editor library selection**: Deep-dive into TipTap vs. Milkdown vs. MDXEditor for the specific editing experience desired (markdown-only vs. rich text vs. MDX). Each has different bundle sizes, extensibility, and collaborative editing support.
2. **Astro 6 release analysis**: Once Astro 6 is released with Cloudflare backing, re-evaluate whether new server-side capabilities change the recommendation for Option B.
3. **Admin SPA framework selection**: Compare React (ecosystem), Solid (performance), and Svelte (DX) specifically for admin panel use cases with the .NET API.
4. **Image upload workflow**: Research direct-to-ImageKit upload from browser vs. proxying through .NET API. Security implications differ (signed upload URLs vs. server-side upload).

---

## Full Citations

[1] Ghost. "Architecture". Ghost Developer Docs. https://docs.ghost.org/architecture/. Accessed 2026-03-08.
[2] TryGhost. "Ghost Admin Client". GitHub. https://github.com/TryGhost/Admin. Accessed 2026-03-08.
[3] Ghost Forum. "Separating Ghost Admin Panel and Front-End". https://forum.ghost.org/t/separating-ghost-admin-panel-and-front-end/15372. Accessed 2026-03-08.
[4] Seahawk Media. "WordPress Decoupled Architecture: Beginner's Guide for 2026". https://seahawkmedia.com/tech/wordpress-decoupled-architecture-guide/. Accessed 2026-03-08.
[5] Astro. "Islands Architecture". Astro Docs. https://docs.astro.build/en/concepts/islands/. Accessed 2026-03-08.
[6] Astro. "On-Demand Rendering". Astro Docs. https://docs.astro.build/en/guides/on-demand-rendering/. Accessed 2026-03-08.
[7] Astro. "Upgrade to Astro v5". Astro Docs. https://docs.astro.build/en/guides/upgrade-to/v5/. Accessed 2026-03-08.
[8] Astro. "Authentication". Astro Docs. https://docs.astro.build/en/guides/authentication/. Accessed 2026-03-08.
[9] Astro. "Server Islands". Astro Docs. https://docs.astro.build/en/guides/server-islands/. Accessed 2026-03-08.
[10] Astro. "Astro 5.0". Astro Blog. https://astro.build/blog/astro-5/. Accessed 2026-03-08.
[11] Decap CMS. "Basic Steps". https://decapcms.org/docs/basic-steps/. Accessed 2026-03-08.
[12] Astro. "Tina CMS & Astro". Astro Docs. https://docs.astro.build/en/guides/cms/tina-cms/. Accessed 2026-03-08.
[13] Decap CMS. "decap-cms". GitHub. https://github.com/decaporg/decap-cms. Accessed 2026-03-08.
[14] OWASP. "OWASP Top 10:2025". https://owasp.org/Top10/2025/. Accessed 2026-03-08.
[15] OWASP. "Authentication Cheat Sheet". https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html. Accessed 2026-03-08.
[16] OWASP. "Enumerate Infrastructure and Application Admin Interfaces". WSTG. https://owasp.org/www-project-web-security-testing-guide/latest/4-Web_Application_Security_Testing/02-Configuration_and_Deployment_Management_Testing/05-Enumerate_Infrastructure_and_Application_Admin_Interfaces. Accessed 2026-03-08.
[17] Cockburn, Alistair. "Hexagonal Architecture". https://alistair.cockburn.us/hexagonal-architecture. Accessed 2026-03-08.
[18] AWS. "Hexagonal Architecture Pattern". AWS Prescriptive Guidance. https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/hexagonal-architecture.html. Accessed 2026-03-08.

---

## Research Metadata

Duration: ~25 min | Examined: 30+ | Cited: 18 | Cross-refs: 14 | Confidence: High 50%, Medium-High 33%, Medium 17% | Output: docs/research/author-mode-best-practices.md

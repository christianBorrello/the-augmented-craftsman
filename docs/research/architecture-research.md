# TacBlog — Consolidated Architecture Research

**Date**: 2026-03-05
**Context**: Rebuild of a tutorial blog into a production-grade, portfolio-worthy platform
**Approach**: Outside-In TDD, Software Craftsmanship principles (BEST_PRACTICES.md)

---

## 1. Architecture Selection

### Recommendation: Hexagonal Architecture + Vertical Slice Feature Organization

**Confidence**: Medium-High | **Sources**: Cockburn, Mancuso, Freeman/Pryce, Bogard, Jovanovic, Comartin

#### Why This Hybrid

The blog has clear, real integration points that map directly to Hexagonal vocabulary:
- **Driving adapters**: REST API (frontend), acceptance tests, admin CLI (future)
- **Driven adapters**: SQL database (EF Core), image storage (Cloudinary), identity (ASP.NET Identity)

Within the application core, **Vertical Slice organization** groups code by feature (`CreatePost`, `GetPostById`, `ListPosts`) rather than by technical layer, avoiding the "3 files across 4 projects" anti-pattern that Clean Architecture creates for simple CRUD.

#### Comparative Matrix

| Criterion | Vertical Slice | Clean Arch | **Hexagonal** | Layered | CQRS |
|---|---|---|---|---|---|
| CRUD blog fit | Good | Adequate | **Natural** | Adequate | Poor |
| TDD Outside-In | Good | Excellent | **Excellent** | Poor | Good |
| SOLID demo | Mixed | Excellent | **Excellent** | Poor | Good |
| Complexity / value | Excellent | Concerning | **Good** | Excellent | Very Poor |
| Over-engineering risk | Low | High | **Low-Med** | None | Very High |
| Separate frontend fit | Neutral | Good | **Excellent** | Neutral | Neutral |

#### What NOT to Do

- **Full Clean Architecture + CQRS + MediatR** — demonstrates knowledge of patterns, not mastery of *when* to apply them. Greg Young (CQRS originator) and Fowler both warn against CQRS for simple CRUD.
- **Event Sourcing** — no audit trail or temporal query requirement
- **4+ solution projects for 6 entities** — disproportionate ceremony

#### Key Sources
- Alistair Cockburn — [Hexagonal Architecture](https://alistair.cockburn.us/hexagonal-architecture/) (originator)
- Sandro Mancuso — Interaction-Driven Design / Outside-In TDD (SC London talks)
- Freeman & Pryce — *Growing Object-Oriented Software, Guided by Tests* (Addison-Wesley, 2009)
- Jimmy Bogard — [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/)
- Derek Comartin — [Clean Architecture Is Not About Layers](https://codeopinion.com)
- Milan Jovanovic — Clean Architecture pragmatic analysis (milanjovanovic.tech)

---

## 2. DDD Applicability

### Recommendation: DDD-Lite — Value Objects + Lightweight Aggregate + Bounded Context Awareness

**Confidence**: HIGH | **Sources**: Evans, Vernon, Wlaschin, Fowler (5 independent book-length sources)

#### Core Finding

> *"If your system is truly just CRUD, then by all means you should use the simplest tool set available."*
> — Vaughn Vernon, IDDD (2013)

A blog domain does not meet the complexity threshold for full DDD tactical patterns. The judgment shown in **choosing what NOT to apply** is more impressive than pattern maximalism.

#### Adopt

| Pattern | How | Why |
|---|---|---|
| **Value Objects** | `Slug`, `Title`, `PostContent`, `TagName` | Encode rules in types; eliminate primitive obsession |
| **Lightweight Aggregate** | `BlogPost` as root, owns Tag collection + lifecycle | Natural boundary; enforces invariants |
| **Repository Interface** | `IBlogPostRepository` as domain port | Decouples domain from persistence; Hexagonal alignment |
| **Bounded Context Awareness** | Separate namespaces: Content, Identity, Media | Clean boundaries; independent evolution |
| **Ubiquitous Language** | Blog domain terms in code | Self-documenting |

#### Defer (Add When Needed)

| Pattern | Trigger |
|---|---|
| Domain Events | When a real consumer exists (RSS, webhooks) |
| Domain Services | When operation doesn't fit Entity or Value Object |
| Specifications | When query filtering becomes complex and reusable |

#### Skip

Factories, Event Sourcing, Anti-Corruption Layers, formal Context Maps, Saga/Process Managers.

#### Key Sources
- Eric Evans — *Domain-Driven Design* (2003): complexity threshold, Value Objects, Aggregates
- Vaughn Vernon — *Implementing DDD* (2013): CRUD assessment, Aggregate design rules
- Vaughn Vernon — *DDD Distilled* (2016): subdomain classification
- Scott Wlaschin — *Domain Modeling Made Functional* (2018): make illegal states unrepresentable
- Martin Fowler — *Patterns of Enterprise Application Architecture* (2002): Transaction Script vs Domain Model

---

## 3. Frontend Stack

### Recommendation: Astro (primary) | Next.js (fallback if high interactivity needed)

**Confidence**: Medium-High | **Sources**: Official docs, State of JS surveys, web.dev, HTTP Archive

#### Why Astro

| Strength | Detail |
|---|---|
| Zero JS by default | Content pages ship 0KB JavaScript — best possible Core Web Vitals |
| Content Collections | Zod schemas, built-in markdown/MDX, Shiki syntax highlighting |
| Island Architecture | Static HTML for content, interactive islands for dynamic features (comments, likes) |
| Static output | Deploys anywhere, no server required — minimal operational burden |
| API consumption | `fetch` in frontmatter at build time; trivial .NET API integration |

#### Architectural Alignment

Astro's island architecture maps directly to the Hexagonal model: the static HTML is the primary driving adapter, interactive islands are secondary adapters calling the .NET API client-side. **Because the .NET API holds all business logic, the frontend is a pure rendering adapter** — switching frameworks later is bounded to templates and fetch code.

#### Comparative Ranking

| Rank | Framework | Best For | Weakness for Blog |
|---|---|---|---|
| 1 | **Astro** | Content sites, zero JS | Smaller ecosystem for complex interactivity |
| 2 | **Next.js** | Versatility, ISR, largest ecosystem | React runtime overhead for read-only content |
| 3 | **SvelteKit** | Minimal JS via compilation | No built-in content schemas |
| 4 | **Nuxt** | Vue ecosystem, Nuxt Content | Smaller English community |
| 5 | **Remix** | Interactive apps, web standards | No SSG — requires server |

---

## 4. Deployment Strategy

### Recommendation: Vercel (frontend) + Railway or Fly.io (backend)

**Confidence**: Medium (pricing may have changed since last verification)

#### Comparison

| Platform | Best For | .NET Support | Cost (personal blog) |
|---|---|---|---|
| **Railway** | Simplest DX, push-to-deploy | Docker containers | Hobby $5/mo + usage |
| **Fly.io** | Global edge, low latency | Docker containers | ~$3.15/mo small VM |
| **Azure Container Apps** | Enterprise, .NET native | First-class | Free tier available, complex pricing |
| **Render** | Simple PaaS alternative | Docker containers | Free tier (limited) |

#### Recommended Architecture

```
[Astro static site] → Vercel (free tier, edge CDN, zero config)
         ↓ API calls
[.NET 10 API]       → Railway or Fly.io (Docker container)
         ↓
[SQL Database]      → Railway managed Postgres or Azure SQL
[Image Storage]     → Cloudinary (existing integration)
```

**Why this split**: Astro's static output on Vercel = free, edge-cached, instant. The .NET API on Railway/Fly.io keeps the backend simple. The separation proves the Hexagonal Architecture works.

#### Sources
- [Fly.io vs Railway 2026](https://thesoftwarescout.com/fly-io-vs-railway-2026-which-developer-platform-should-you-deploy-on/)
- [Railway vs Fly comparison](https://docs.railway.com/platform/compare-to-fly)
- [Deploying Full Stack Apps in 2026](https://www.nucamp.co/blog/deploying-full-stack-apps-in-2026-vercel-netlify-railway-and-cloud-options)

---

## 5. .NET 10 Features Relevant to This Project

### .NET 10 (LTS — released November 2025)

| Feature | Relevance |
|---|---|
| **Built-in Minimal API validation** | AddValidation with data annotations — removes custom validation code |
| **Server-Sent Events result** | Native SSE for streaming updates (future: live preview) |
| **Improved route groups** | Organize endpoints by feature (aligns with Vertical Slice) |
| **NativeAOT enhancements** | Faster startup, smaller containers for deployment |
| **OpenAPI 3.1 built-in** | First-class API documentation without Swashbuckle |
| **Performance: JIT inlining, devirtualization** | Lower latency across the board |
| **Stack allocation for small arrays** | Reduced GC pressure |

### C# 14 Features for Domain Modeling

| Feature | Use Case |
|---|---|
| **`field` keyword** | Cleaner property accessors for Value Objects with validation |
| **Extension members** | Extension properties for domain-rich APIs without inheritance |
| **Implicit Span conversions** | Performance for string-heavy blog content processing |
| **Null-conditional assignment (`?.=`)** | Cleaner optional field updates |
| **Partial constructors** | Better source generator support for domain types |

#### Sources
- [What's new in .NET 10 — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [Announcing .NET 10 — .NET Blog](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)
- [Introducing C# 14 — .NET Blog](https://devblogs.microsoft.com/dotnet/introducing-csharp-14/)
- [What's new in C# 14 — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [What's new in ASP.NET Core 10 — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0)

---

## 6. Proposed Project Structure

Based on all research findings:

```
TacBlog.sln
  src/
    TacBlog.Domain/               # Entities, Value Objects (Slug, Title, TagName...)
    TacBlog.Application/          # Use cases (feature-organized), Port definitions
      Features/
        Posts/
          CreatePost.cs
          GetPostById.cs
          UpdatePost.cs
          DeletePost.cs
          ListPosts.cs
        Tags/
        Images/
      Ports/
        Driven/
          IBlogPostRepository.cs
          IImageStorage.cs
    TacBlog.Infrastructure/       # Driven adapters
      Persistence/
        EfBlogPostRepository.cs
      Storage/
        CloudinaryImageStorage.cs
      Identity/
        AspNetIdentityService.cs
    TacBlog.Api/                  # Driving adapter (Minimal API endpoints)
  tests/
    TacBlog.Domain.Tests/         # Value Object and Entity unit tests
    TacBlog.Application.Tests/    # Use case tests (driven ports stubbed)
    TacBlog.Api.Tests/            # Integration tests (driving adapter)
    TacBlog.Acceptance.Tests/     # BDD acceptance tests (Outside-In entry point)
  frontend/
    astro-blog/                   # Astro frontend (separate driving adapter)
```

---

## 7. Open Decisions for Design Phase

| Decision | Options | Recommendation |
|---|---|---|
| MediatR for dispatching | MediatR vs direct injection | Direct injection — MediatR adds indirection without value for this scope |
| Domain + Application split | 2 projects vs 1 Core project | Start with 2 (cleaner boundary); collapse if overhead is felt |
| Minimal API vs Controllers | Minimal API endpoints vs MVC controllers | Minimal API — lighter, aligns with .NET 10 direction |
| BDD framework | SpecFlow vs simpler acceptance tests | Evaluate during implementation; SpecFlow adds value if Gherkin scenarios help clarify requirements |
| Database | SQL Server vs PostgreSQL | PostgreSQL — better Railway/Fly.io support, free tier, lighter |

---

## 8. Detailed Research Documents

For full analysis with source citations and confidence ratings, see:
- [Architecture Selection](architecture-selection-for-content-blog.md)
- [DDD Applicability](ddd-applicability-blog-domain.md)
- [Frontend Stack Selection](../../docs/research/frontend-stack-selection-blog-2026.md)

---

*Research consolidated from 4 parallel research agents. All major claims should be verified against cited sources before finalizing architectural decisions.*

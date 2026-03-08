# Architecture Selection for a Content-Driven Blog Platform

**Research Date**: 2026-03-05
**Researcher**: Nova (nw-researcher agent)
**Domain**: .NET 10 blog platform (TacBlog) -- CRUD-heavy content management
**Scope**: v1 -- CRUD blog posts, tags, image upload, authentication

---

## Sourcing Transparency Notice

WebSearch and WebFetch tools were unavailable during this research session. All citations reference well-established, publicly available writings from recognized authorities within the researcher's training data (cutoff: May 2025). URLs are provided for independent verification. **Every claim should be verified against the original sources before making architectural decisions.** Confidence ratings reflect this limitation.

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Domain Analysis](#2-domain-analysis)
3. [Architecture Evaluations](#3-architecture-evaluations)
   - 3.1 Vertical Slice Architecture
   - 3.2 Clean Architecture
   - 3.3 Hexagonal Architecture (Ports & Adapters)
   - 3.4 Simple Layered Architecture
   - 3.5 CQRS (with/without Event Sourcing)
4. [Hybrid Approaches](#4-hybrid-approaches)
5. [Comparative Matrix](#5-comparative-matrix)
6. [Recommendation](#6-recommendation)
7. [Knowledge Gaps and Conflicts](#7-knowledge-gaps-and-conflicts)
8. [Sources](#8-sources)

---

## 1. Executive Summary

**Finding**: For a CRUD-heavy blog platform that also serves as a Software Craftsmanship portfolio piece, a **Hexagonal Architecture (Ports & Adapters) with Vertical Slice influences** provides the best balance of:
- Demonstrable mastery of SOLID, DDD, and TDD principles (portfolio goal)
- Appropriate complexity for the domain (not over-engineered for CRUD)
- First-class testability via Outside-In TDD (the project's chosen development approach)
- Natural alignment with the separate frontend requirement (the frontend is literally a driving adapter)

**Confidence**: Medium-High. This recommendation is supported by consistent reasoning across multiple authoritative sources, but web verification was unavailable during this session.

---

## 2. Domain Analysis

### 2.1 Domain Complexity Assessment

The blog domain exhibits the following characteristics based on the existing TacBlog codebase:

| Characteristic | Assessment | Evidence |
|---|---|---|
| Behavioral complexity | Low | Operations are predominantly CRUD: create/read/update/delete posts, tags, images |
| Domain rules | Minimal | Visibility toggle, URL handle generation, tag association -- no complex invariants |
| Aggregate boundaries | Simple | BlogPost is the primary aggregate with Tags, Likes, Comments as value-like associations |
| Read/Write ratio | Read-heavy | Public blog is primarily read; admin area is write; classic content platform pattern |
| Integration points | Few | Image storage (Cloudinary), Authentication (Identity), Database (SQL Server) |
| Event-driven needs | None in v1 | No workflows, sagas, or eventual consistency requirements |

### 2.2 Implications for Architecture

The low domain complexity means:
1. Full DDD tactical patterns (Aggregates, Domain Events, Specifications) would be **ceremony without proportional value** for v1
2. CQRS with Event Sourcing would be **significant over-engineering** -- there is no complex query vs. command asymmetry
3. The architecture must justify its layers through **testability and separation of concerns**, not through handling domain complexity
4. The **portfolio demonstration goal** shifts the calculus: some additional structure is justified if it demonstrates principles clearly

This aligns with Martin Fowler's observation that architectural decisions should be driven by the forces present in the specific system, not by pattern enthusiasm [S1]. Jimmy Bogard has similarly argued that most applications are "forms over data" and that architectural patterns should be applied where they provide actual value [S2].

---

## 3. Architecture Evaluations

### 3.1 Vertical Slice Architecture

**Origin**: Popularized by Jimmy Bogard, building on feature-oriented development ideas.

**Core idea**: Organize code by feature/use-case rather than by technical layer. Each "slice" contains everything needed for a single operation (handler, validation, data access, mapping) in one cohesive unit.

#### Fit for CRUD Blog

| Criterion | Rating | Analysis |
|---|---|---|
| Domain fit | Good | CRUD operations map naturally to individual slices (CreatePost, GetPostById, UpdatePost, DeletePost, etc.) |
| TDD support | Good | Each slice is independently testable. However, Outside-In TDD starting from acceptance tests does not get a clear "port" boundary to mock against -- tests tend to be integration-heavy [S2, S3] |
| SOLID demonstration | Mixed | Strong on Single Responsibility (one slice = one operation). Weaker on demonstrating Dependency Inversion and Interface Segregation -- slices often access infrastructure directly via MediatR handlers [S2] |
| Complexity vs. value | Excellent | Minimal ceremony. Adding a new feature means adding one folder with handler + request/response. No cross-cutting layer changes needed [S2] |
| Testability | Good | Slices are isolated units. Risk: tests may become integration tests hitting the database rather than true unit tests, unless explicit port boundaries are added [S3] |
| Over-engineering risk | Low | This is one of the lightest architectural patterns available |

#### Key Strengths
- Minimal indirection: handler receives request, does work, returns response
- Feature cohesion: all code for "Create Blog Post" lives together
- Low coupling between features: changing one slice cannot break another
- Natural fit with MediatR in .NET ecosystem [S2, S4]

#### Key Weaknesses for This Project
- **Does not naturally demonstrate Ports & Adapters thinking** -- the separate frontend requirement is literally a driving adapter scenario, and VSA does not make this boundary explicit
- **Outside-In TDD is less natural** -- without explicit ports, the "outside" (acceptance test) tends to drive all the way to the database, making the inner loop (unit test) less distinct [S3, S5]
- **DDD demonstration is weaker** -- domain models in VSA are often just data containers within handlers, not rich domain objects [S2]

#### Sources
- [S2] Jimmy Bogard, "Vertical Slice Architecture" (jimmybogard.com/vertical-slice-architecture/)
- [S3] Jimmy Bogard, "Vertical Slice Architecture - How Does It Compare?" (NDC Conference talk, various years)
- [S4] MediatR documentation and examples (github.com/jbogard/MediatR)

---

### 3.2 Clean Architecture

**Origin**: Robert C. Martin ("Uncle Bob"), 2012. Formalized in "Clean Architecture" book (2017). .NET template by Jason Taylor.

**Core idea**: Concentric layers with the Dependency Rule: source code dependencies must point inward. Inner layers (Entities, Use Cases) know nothing about outer layers (Frameworks, UI, DB).

#### Fit for CRUD Blog

| Criterion | Rating | Analysis |
|---|---|---|
| Domain fit | Adequate but heavy | The blog domain has minimal business rules to protect in the inner circle. Clean Architecture's strength is isolating complex business rules from infrastructure -- but the blog's "rules" are mostly CRUD operations [S6, S7] |
| TDD support | Excellent | The Dependency Rule creates natural seams for mocking. Use Cases (Application layer) can be tested without any infrastructure. This directly supports Outside-In TDD [S6, S8] |
| SOLID demonstration | Excellent | Clean Architecture is essentially SOLID applied at the architectural level. Every principle is visibly embodied: ISP (small interfaces per use case), DIP (infrastructure depends on abstractions), SRP (each layer has one reason to change) [S6] |
| Complexity vs. value | Concerning for blog | Jason Taylor's template generates 4+ projects (Domain, Application, Infrastructure, Web). For a blog with ~6 domain entities and ~15 use cases, this creates significant structural overhead [S7, S9] |
| Testability | Excellent | Clear boundaries between layers make unit testing, integration testing, and acceptance testing straightforward with well-defined mock points [S6, S8] |
| Over-engineering risk | High | Multiple authors have noted that Clean Architecture's full ceremony is disproportionate for CRUD-dominant applications [S7, S9, S10] |

#### Key Strengths
- Industry recognition: well-understood by interviewers and reviewers (portfolio value)
- Testability is first-class by design
- Framework independence: switching from EF Core to Dapper, or ASP.NET to a different host, is structurally supported
- Jason Taylor's .NET template provides a concrete starting point [S9]

#### Key Weaknesses for This Project
- **The 4-project structure is heavy for 6 entities** -- Domain, Application, Infrastructure, WebAPI projects for a blog creates a lot of folders with very few files each [S7, S9]
- **Mapping fatigue** -- the existing TacBlog code already shows the pain of manual mapping (ViewModels to Domain to DB); Clean Architecture adds another layer of mapping (Domain -> Application DTOs -> Presentation) [S7]
- **The "Use Case" layer is thin** -- most blog use cases are "get entity from repo, map, return" or "validate input, map to entity, save". The Application layer becomes a pass-through [S7, S10]
- **Risk of Cargo Cult** -- implementing Clean Architecture for a blog can look like pattern application without justification, which is the opposite of the craftsmanship message [S10]

#### What Steve Smith (Ardalis) Says
Steve Smith's Clean Architecture template for .NET (Ardalis.CleanArchitecture) provides a more pragmatic take than Jason Taylor's, with fewer layers and more guidance on when to apply patterns. He emphasizes that Clean Architecture is about the dependency direction, not the number of projects [S8].

#### Sources
- [S6] Robert C. Martin, "The Clean Architecture" (blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [S7] Milan Jovanovic, "Clean Architecture: Is It Worth It?" and related posts (milanjovanovic.tech)
- [S8] Steve Smith (Ardalis), Clean Architecture template and documentation (github.com/ardalis/CleanArchitecture)
- [S9] Jason Taylor, "Clean Architecture with .NET" (github.com/jasontaylordev/CleanArchitecture, NDC talks)
- [S10] Derek Comartin, "Clean Architecture Is Not About Layers" (codeopinion.com)

---

### 3.3 Hexagonal Architecture (Ports & Adapters)

**Origin**: Alistair Cockburn, ~2005. Also called Ports & Adapters.

**Core idea**: The application is at the center. It exposes Ports (interfaces defining how the outside world can interact with it). Adapters implement those ports for specific technologies. Driving adapters (UI, API, CLI) call into the application. Driven adapters (DB, file storage, external APIs) are called by the application.

#### Fit for CRUD Blog

| Criterion | Rating | Analysis |
|---|---|---|
| Domain fit | Natural | The blog has clear driving adapters (REST API for frontend, admin UI) and driven adapters (SQL database, Cloudinary image storage). The port/adapter distinction maps directly to the project's real integration points [S11, S12] |
| TDD support | Excellent | Hexagonal was explicitly designed to support TDD. Cockburn's original motivation was "allow an application to equally be driven by users, programs, automated test or batch scripts" [S11]. Outside-In TDD naturally creates ports as the mock boundaries [S11, S13, S14] |
| SOLID demonstration | Excellent | Ports are ISP (small, focused interfaces). Adapters demonstrate DIP (infrastructure depends on port abstractions). The application core demonstrates SRP and OCP [S11, S13] |
| Complexity vs. value | Good | Does not prescribe a fixed number of layers or projects. You can implement it in a single project with clear namespace separation, or in multiple projects. The structure scales with actual complexity [S11, S12] |
| Testability | Excellent | Every adapter can be independently tested. The application core can be tested through its ports with test doubles. Acceptance tests use driving adapter test doubles. This is the gold standard for Outside-In TDD [S11, S13, S14] |
| Over-engineering risk | Low-Medium | Hexagonal is as complex as you make it. For a blog, it can be lightweight: a few ports (IPostRepository, IImageStorage, IAuthService) with their adapters. It does not require the ceremony of Use Case classes or MediatR [S11, S12] |

#### Key Strengths for This Project
- **Directly matches the project structure**: the separate frontend is a driving adapter; Cloudinary is a driven adapter; SQL Server is a driven adapter. These are real architectural boundaries, not artificial ones [S11]
- **TDD origin story**: Hexagonal was literally created to make applications testable. Outside-In TDD flows naturally: write an acceptance test (driving adapter boundary), then unit-test the application core through ports, then implement adapters [S11, S13]
- **Demonstrates principles without ceremony**: you can show DIP, ISP, and SRP through 5-8 well-designed ports rather than through 4 solution projects [S12]
- **Natural growth path**: if v2 adds complexity (e.g., newsletter integration, content scheduling), new driven adapter ports appear naturally [S11, S12]
- **Existing codebase already hints at this**: the current `IBlogPostRepository`, `IImageRepository`, `ITagRepository` interfaces are already driven port candidates [existing code analysis]

#### Key Weaknesses
- **Less industry name recognition than Clean Architecture** -- some interviewers may not know the term (though it is the same concept as Clean Architecture's dependency rule, just with different vocabulary) [S12, S15]
- **No canonical .NET template** -- unlike Clean Architecture (Jason Taylor, Ardalis), there is no widely-used Hexagonal .NET template to scaffold from [knowledge gap]
- **Can be confused with "just interfaces"** -- without clear documentation, it may look like repository pattern rather than an intentional architectural choice [S12]

#### The Cockburn-Martin Connection
Clean Architecture and Hexagonal Architecture share the same core insight: dependencies point inward toward the domain [S6, S11, S15]. Robert C. Martin explicitly acknowledges Hexagonal Architecture as a precursor to Clean Architecture [S6]. The difference is primarily in vocabulary and prescribed structure:
- Hexagonal: Ports, Adapters, Driving/Driven
- Clean: Entities, Use Cases, Interface Adapters, Frameworks

For a blog, Hexagonal gives you the same testability benefits with less prescribed structure.

#### Sources
- [S11] Alistair Cockburn, "Hexagonal Architecture" (alistair.cockburn.us/hexagonal-architecture/)
- [S12] Tom Hombergs, "Get Your Hands Dirty on Clean Architecture" (book, Leanpub/Packt) -- covers Hexagonal in .NET/Java context
- [S13] Sandro Mancuso, "Interaction-Driven Design" (talks at SC London, LNUG) -- Outside-In TDD with Ports & Adapters
- [S14] "Growing Object-Oriented Software, Guided by Tests" by Steve Freeman & Nat Pryce -- the foundational TDD book that aligns with Hexagonal thinking
- [S15] Martin Fowler, "Hexagonal Architecture" (martinfowler.com/bliki/HexagonalArchitecture.html, referenced in various Fowler writings on architecture)

---

### 3.4 Simple Layered Architecture (Traditional 3-Tier)

**Origin**: Traditional enterprise pattern. Presentation -> Business Logic -> Data Access.

**Core idea**: Horizontal layers where each layer can only call the layer below it. The current TacBlog codebase is essentially this pattern.

#### Fit for CRUD Blog

| Criterion | Rating | Analysis |
|---|---|---|
| Domain fit | Adequate | Layers work fine for CRUD. The current TacBlog code proves it: Controllers -> Repositories -> Database. It works [existing code] |
| TDD support | Poor | Dependencies point downward (Business -> Data Access), meaning the business layer depends on concrete data access. Without inverting this dependency, testing business logic requires a real database or heavy mocking of concrete classes [S1, S6, S16] |
| SOLID demonstration | Poor | Traditional layered architecture violates DIP by definition (upper layers depend on lower layers, not abstractions). It does not demonstrate ISP, OCP, or SRP at the architectural level [S6, S16] |
| Complexity vs. value | Good for simplicity, poor for portfolio | Minimal overhead, but demonstrates nothing architecturally interesting for a portfolio [S16] |
| Testability | Poor-Adequate | Can be improved with interfaces (as the current TacBlog code does with `IBlogPostRepository`), but this is bolting Hexagonal thinking onto a layered structure [existing code, S16] |
| Over-engineering risk | None | This is the simplest option |

#### Key Observation
The current TacBlog codebase is a layered architecture with repository interfaces. The interfaces (`IBlogPostRepository`, `ITagRepository`) are already driven ports in Hexagonal terminology. **The distance from "layered + repository interfaces" to "Hexagonal" is primarily one of intentionality and documentation**, not of code structure [S11, S16].

#### Why Not for This Project
- Does not demonstrate the architectural thinking the portfolio requires
- The existing codebase already is this pattern and is the starting point being abandoned
- TDD Outside-In naturally pushes you away from layered toward Hexagonal regardless [S13, S14]

#### Sources
- [S1] Martin Fowler, "Patterns of Enterprise Application Architecture" (martinfowler.com)
- [S6] Robert C. Martin, "The Clean Architecture" (as above)
- [S16] Mark Richards & Neal Ford, "Fundamentals of Software Architecture" (O'Reilly, 2020)

---

### 3.5 CQRS (with/without Event Sourcing)

**Origin**: Greg Young, Udi Dahan. CQRS pattern separates read and write models. Event Sourcing stores state as a sequence of events.

**Core idea**: Commands (writes) and Queries (reads) are handled by separate models, potentially with separate data stores.

#### Fit for CRUD Blog

| Criterion | Rating | Analysis |
|---|---|---|
| Domain fit | Poor | CQRS addresses read/write model asymmetry. A blog's read model and write model are nearly identical -- a blog post is a blog post whether you are reading it or writing it. There is no query complexity that would benefit from a separate read model [S17, S18] |
| TDD support | Good | Commands and queries are independently testable. However, this testability can be achieved more simply through Hexagonal ports [S17] |
| SOLID demonstration | Good | Clean separation of command and query responsibilities. But this is SRP overkill for "save a blog post" vs. "read a blog post" [S17, S18] |
| Complexity vs. value | Very poor | CQRS introduces command handlers, query handlers, potentially separate databases, eventual consistency concerns. For a blog, this is extreme over-engineering [S17, S18, S19] |
| Event Sourcing addition | Unjustifiable for blog | Storing "PostTitleChanged", "PostContentUpdated", "TagAdded" events when you only ever need the current state is a textbook example of unnecessary complexity [S17, S18] |
| Over-engineering risk | Very High | Multiple recognized authorities explicitly warn against CQRS for simple CRUD domains [S17, S18, S19] |

#### What the Authorities Say

**Greg Young** (CQRS originator): Has explicitly stated that CQRS should not be applied to an entire system and that most CRUD-focused bounded contexts do not need it [S17].

**Martin Fowler**: "For some, CQRS is the natural evolution of Clean Architecture. For most CRUD applications, it's unnecessary complexity" (paraphrased from various writings on CQRS) [S18].

**Udi Dahan**: Emphasizes that CQRS is for specific bounded contexts with complex business rules, not for simple data management [S19].

#### The One Exception
If the blog were to grow into a content management system with complex publishing workflows, approval chains, content scheduling, and multi-author collaboration -- CQRS for the publishing bounded context might become justified. But that is not v1 scope.

#### Sources
- [S17] Greg Young, "CQRS Documents" (cqrs.files.wordpress.com/2010/11/cqrs_documents.pdf)
- [S18] Martin Fowler, "CQRS" (martinfowler.com/bliki/CQRS.html)
- [S19] Udi Dahan, various talks on CQRS applicability (particular.net/blog)

---

## 4. Hybrid Approaches

### 4.1 Hexagonal + Vertical Slice Influences (Recommended Hybrid)

**Concept**: Use Hexagonal Architecture for the overall structure (ports, adapters, dependency direction), but organize the application core using feature-oriented grouping inspired by Vertical Slice Architecture.

**Structure**:
```
TacBlog.Domain/              -- Domain entities, value objects (inner hexagon)
TacBlog.Application/         -- Use cases organized by feature (application hexagon)
  Features/
    Posts/
      CreatePost.cs          -- Command + Handler
      GetPostById.cs         -- Query + Handler
      UpdatePost.cs
      DeletePost.cs
      ListPosts.cs
    Tags/
      ...
    Images/
      ...
  Ports/
    Driven/
      IPostRepository.cs
      IImageStorage.cs
    Driving/
      (defined by the use case interfaces themselves)
TacBlog.Infrastructure/      -- Driven adapter implementations
  Persistence/
    EfPostRepository.cs
  Storage/
    CloudinaryImageStorage.cs
  Identity/
    AspNetIdentityService.cs
TacBlog.Api/                 -- Driving adapter (REST API for frontend)
  Controllers/ or Endpoints/
```

**Why this hybrid works for this project**:

1. **Hexagonal gives the structural backbone**: Ports and adapters create real, testable boundaries. The separate frontend calling the API is a driving adapter. EF Core and Cloudinary are driven adapters. This is not theoretical -- these are the actual integration points [S11, S2].

2. **Vertical Slice gives the internal organization**: Within the application hexagon, features are grouped by use case rather than by technical concern. This avoids the "3 files in each of 4 layers" problem that Clean Architecture creates for simple CRUD [S2, S7].

3. **TDD flows naturally**: Acceptance test drives the API endpoint (driving adapter boundary) -> unit test drives the use case handler (application core) -> integration test validates the adapter (driven adapter). This is the Outside-In Double Loop from the idea brief [S13, S14].

4. **SOLID is demonstrated without ceremony**: ISP through focused ports, DIP through adapter inversion, SRP through feature grouping, OCP through new features as new slices [S6, S11].

5. **Portfolio value is high**: Shows intentional architecture, testability, and the ability to combine patterns pragmatically rather than dogmatically -- which is arguably a stronger signal of craftsmanship than slavish adherence to one template [S8, S10].

#### Sources Supporting Hybrids
- Jimmy Bogard has acknowledged that Vertical Slices can live within a Hexagonal structure [S2, S3]
- Milan Jovanovic has written about combining Clean Architecture concepts with Vertical Slices [S7]
- Derek Comartin (CodeOpinion) advocates for pragmatic combination of patterns based on domain needs [S10]

### 4.2 Clean Architecture Lite (Alternative Hybrid)

**Concept**: Use Clean Architecture's dependency rule and project structure but reduce to 2-3 projects instead of 4+. Collapse Domain and Application into a single Core project.

**Structure**:
```
TacBlog.Core/                -- Domain + Application combined
TacBlog.Infrastructure/      -- All external concerns
TacBlog.Api/                 -- Web API
```

This is essentially what Ardalis advocates for smaller applications [S8].

**Assessment**: This is a pragmatic approach that would also work well. The difference from the Hexagonal + VSA hybrid is primarily organizational vocabulary. Both result in similar code structure.

### 4.3 Anti-Pattern: Full Clean Architecture + CQRS + MediatR for a Blog

This combination appears frequently in .NET tutorial content and conference talks as a "best practice" template. For a blog, it produces:

- `CreateBlogPostCommand`, `CreateBlogPostCommandHandler`, `CreateBlogPostCommandValidator` for what is essentially `repository.Save(post)`
- A MediatR pipeline with behaviors for cross-cutting concerns that the blog does not have
- Separate read/write models for entities that are identical in both directions
- 50+ files of infrastructure for 6 entities

**This is the pattern to actively avoid**. It demonstrates knowledge of patterns, not mastery of when to apply them. A craftsmanship portfolio should show judgment, not ceremony [S10, S17, S18].

---

## 5. Comparative Matrix

| Criterion | Vertical Slice | Clean Arch | Hexagonal | Layered | CQRS | Hex+VSA Hybrid |
|---|---|---|---|---|---|---|
| **CRUD blog fit** | Good | Adequate | Natural | Adequate | Poor | Natural |
| **TDD Outside-In** | Good | Excellent | Excellent | Poor | Good | Excellent |
| **SOLID demo** | Mixed | Excellent | Excellent | Poor | Good | Excellent |
| **DDD demo** | Weak | Good | Good | Weak | Good | Good |
| **Complexity / value** | Excellent | Concerning | Good | Excellent | Very Poor | Good |
| **Testability** | Good | Excellent | Excellent | Poor | Good | Excellent |
| **Over-engineering risk** | Low | High | Low-Med | None | Very High | Low |
| **Portfolio value** | Medium | High | High | Low | Medium | High |
| **Separate frontend fit** | Neutral | Good | Excellent | Neutral | Neutral | Excellent |
| **Growth path to v2+** | Good | Good | Excellent | Poor | N/A | Excellent |

**Rating scale**: Poor / Weak / Adequate / Neutral / Mixed / Good / Excellent

---

## 6. Recommendation

### Primary Recommendation: Hexagonal Architecture with Vertical Slice Feature Organization

**Confidence**: Medium-High

**Rationale** (ranked by project priorities from the idea brief):

1. **TDD Outside-In** (highest priority practice): Hexagonal was designed for this. Ports are the natural mock/stub boundaries. The Double Loop (acceptance -> unit) maps directly to driving adapter boundary -> application core through ports -> driven adapter verification [S11, S13, S14].

2. **Portfolio demonstration**: Shows intentional architecture chosen for the domain (not a template), demonstrates all SOLID principles at the architectural level, and shows pragmatic pattern combination [S6, S8, S10, S11].

3. **Separate frontend alignment**: The frontend-as-driving-adapter is not a metaphor -- it is literally what Hexagonal Architecture describes. The REST API is the port; the ASP.NET controllers/endpoints are the adapter [S11].

4. **Right-sized for the domain**: Does not require 4+ projects for 6 entities. Does not require MediatR, command/query objects, or pipeline behaviors. A port interface, a domain entity, and an adapter implementation per concern is sufficient [S11, S12].

5. **Natural migration from existing code**: The current `IBlogPostRepository`, `IImageRepository` interfaces are already driven ports. The architectural shift is primarily about making the intent explicit and adding the driving side [existing code analysis].

### Implementation Guidance

**Project structure** (suggested starting point):
```
TacBlog.sln
  src/
    TacBlog.Domain/            -- Entities, Value Objects, Domain Services (if any)
    TacBlog.Application/       -- Use cases (feature-organized), Port definitions
    TacBlog.Infrastructure/    -- Driven adapters (EF Core repos, Cloudinary, Identity)
    TacBlog.Api/               -- Driving adapter (REST API, minimal API endpoints)
  tests/
    TacBlog.Domain.Tests/
    TacBlog.Application.Tests/ -- Unit tests through ports (driven ports stubbed)
    TacBlog.Api.Tests/         -- Integration tests (driving adapter -> full stack)
    TacBlog.Acceptance.Tests/  -- BDD/SpecFlow acceptance tests
```

**Key decisions to make next**:
- Whether to use MediatR for in-process dispatching or direct injection of use case classes (MediatR is optional, not required)
- Whether Domain and Application should be one project or two (for a blog, one "Core" project is defensible)
- Minimal API endpoints vs. Controllers for the driving adapter
- Whether to start with SpecFlow BDD or simpler acceptance test structure

### What NOT to Do
- Do not adopt the full Jason Taylor Clean Architecture template -- it is over-structured for this domain
- Do not add CQRS -- there is no read/write model asymmetry
- Do not add Event Sourcing -- there is no audit trail or temporal query requirement
- Do not add MediatR pipeline behaviors (validation, logging, caching) preemptively -- add them when a concrete need arises (YAGNI)
- Do not create a "SharedKernel" project for a single bounded context

---

## 7. Knowledge Gaps and Conflicts

### 7.1 Knowledge Gaps

| Gap | What Was Searched | Why Insufficient |
|---|---|---|
| .NET 10 architectural features | Could not access web sources for .NET 10-specific guidance | .NET 10 is post-training-cutoff (released late 2025). There may be new minimal API features, source generators, or architectural patterns that affect this recommendation |
| Hexagonal Architecture .NET templates 2025-2026 | Could not verify current state of .NET Hexagonal templates | There may now be well-maintained Hexagonal .NET templates that reduce scaffolding effort |
| Milan Jovanovic's latest posts on architecture selection | Could not access milanjovanovic.tech | Jovanovic has been actively writing about pragmatic .NET architecture and may have published relevant comparisons post-2025 |
| Community sentiment on Clean Architecture backlash | Could not search developer forums/blogs | There is an emerging "Clean Architecture is over-engineering" counter-movement in .NET; its current state and arguments could not be verified |
| Ardalis template v8+ changes | Could not access github.com/ardalis/CleanArchitecture | The Ardalis template may have evolved to address over-engineering concerns |

### 7.2 Conflicts Between Sources

| Conflict | Position A | Position B | Assessment |
|---|---|---|---|
| Clean Architecture universality | Robert C. Martin presents Clean Architecture as universally applicable regardless of domain complexity [S6] | Multiple practitioners (Jovanovic, Comartin, Bogard) argue it is disproportionate for CRUD-dominant domains [S7, S10, S2] | Position B is more aligned with the craftsmanship principle of choosing appropriate tools. Martin's position is about the dependency rule (which is universal), not the full 4-layer ceremony |
| Hexagonal vs. Clean naming | Some authors treat them as identical [S15] | Others emphasize meaningful differences (Hexagonal is about symmetry of driving/driven; Clean is about concentric layers with use cases) [S6, S11] | For this project, the distinction matters: Hexagonal's driving/driven vocabulary maps better to the blog's actual architecture (frontend drives, DB is driven) |
| VSA testability | Bogard argues slices are highly testable [S2] | Freeman/Pryce-style TDD requires explicit port boundaries that VSA does not naturally provide [S14] | Both are correct for different definitions of "testable". VSA enables integration testing; Hexagonal enables unit testing through ports. The project's Outside-In TDD requirement favors Hexagonal's approach |

### 7.3 Interpretive Claims (Researcher Analysis, Not Sourced)

The following conclusions are my synthesis, not directly stated by any single source:

1. **"The existing TacBlog interfaces are already driven ports"** -- This is my interpretation. The interfaces were likely created for DI convenience, not as intentional architectural ports. The difference is in documentation and intent, not in code.

2. **"Hexagonal + VSA hybrid is the best fit"** -- No single authority recommends this exact combination for a blog. This is my synthesis of multiple partially-overlapping recommendations.

3. **"Full Clean Architecture + CQRS + MediatR for a blog is an anti-pattern"** -- While multiple sources warn against over-engineering, calling it an "anti-pattern" is my editorial judgment. It would function correctly; it would just be disproportionate.

---

## 8. Sources

### Primary Sources (Directly Referenced)

| ID | Author | Title / Resource | URL | Tier |
|---|---|---|---|---|
| S1 | Martin Fowler | Patterns of Enterprise Application Architecture | martinfowler.com/eaaCatalog/ | Tier 1 - Authority |
| S2 | Jimmy Bogard | Vertical Slice Architecture | jimmybogard.com/vertical-slice-architecture/ | Tier 1 - Authority |
| S3 | Jimmy Bogard | NDC talks on Vertical Slice Architecture | Various NDC recordings | Tier 1 - Authority |
| S4 | Jimmy Bogard | MediatR documentation | github.com/jbogard/MediatR | Tier 1 - Primary Source |
| S5 | Various | Outside-In TDD discussion in .NET context | Multiple blog posts and talks | Tier 3 - Community |
| S6 | Robert C. Martin | The Clean Architecture (blog post, 2012) | blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html | Tier 1 - Authority |
| S7 | Milan Jovanovic | Clean Architecture analysis posts | milanjovanovic.tech | Tier 2 - Recognized Expert |
| S8 | Steve Smith (Ardalis) | Clean Architecture template + documentation | github.com/ardalis/CleanArchitecture | Tier 1 - Authority |
| S9 | Jason Taylor | Clean Architecture with .NET template | github.com/jasontaylordev/CleanArchitecture | Tier 1 - Authority |
| S10 | Derek Comartin | Clean Architecture pragmatic analysis | codeopinion.com | Tier 2 - Recognized Expert |
| S11 | Alistair Cockburn | Hexagonal Architecture (original article) | alistair.cockburn.us/hexagonal-architecture/ | Tier 1 - Authority (Originator) |
| S12 | Tom Hombergs | Get Your Hands Dirty on Clean Architecture | Book (Packt/Leanpub) | Tier 2 - Published Author |
| S13 | Sandro Mancuso | Interaction-Driven Design / Outside-In TDD | SC London talks, codurance.com | Tier 1 - Authority (Craftsmanship) |
| S14 | Steve Freeman, Nat Pryce | Growing Object-Oriented Software, Guided by Tests | Book (Addison-Wesley, 2009) | Tier 1 - Authority |
| S15 | Martin Fowler | References to Hexagonal Architecture | martinfowler.com | Tier 1 - Authority |
| S16 | Mark Richards, Neal Ford | Fundamentals of Software Architecture | Book (O'Reilly, 2020) | Tier 1 - Published Authority |
| S17 | Greg Young | CQRS Documents | cqrs.files.wordpress.com/2010/11/cqrs_documents.pdf | Tier 1 - Authority (Originator) |
| S18 | Martin Fowler | CQRS | martinfowler.com/bliki/CQRS.html | Tier 1 - Authority |
| S19 | Udi Dahan | CQRS applicability guidance | particular.net/blog | Tier 1 - Authority |

### Verification Priority

**Must verify before proceeding** (web access needed):
1. .NET 10 architectural features and any new patterns
2. Current state of Ardalis and Jason Taylor templates (may have simplified)
3. Milan Jovanovic's latest architecture selection guidance
4. Whether a canonical Hexagonal .NET template now exists

---

*Research produced by nw-researcher agent. All major claims should be independently verified against the cited sources before architectural decisions are finalized.*

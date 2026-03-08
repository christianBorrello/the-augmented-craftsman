# Architecture Design -- The Augmented Craftsman v1

**Architecture Style**: Hexagonal Architecture + Vertical Slice Feature Organization
**System Design**: Monolith -- single .NET 10 API + static Astro frontend
**Communication**: Synchronous REST
**Data**: Single PostgreSQL database, no CQRS

---

## 1. C4 Model

### 1.1 Context Diagram (Level 1)

```
+---------------------------+
|     The Augmented         |
|     Craftsman System      |
+---------------------------+
      ^            ^
      |            |
  [Author]     [Reader]
  Christian    Anonymous
  (admin)      Developer
      |            |
      v            v
+---------------------------+       +-------------------+
| .NET 10 API               |------>| ImageKit          |
| (blog management)         |       | (image storage)   |
+---------------------------+       +-------------------+
      |            ^
      |            |
      v            |
+---------------------------+       +-------------------+
| PostgreSQL                |       | Astro Frontend    |
| (data persistence)       |       | (static site)     |
+---------------------------+       +-------------------+
                                          |
                                          v
                                    [Reader Browser]
```

**Actors:**
- **Author (Christian)**: Single admin user. Creates, edits, publishes, deletes posts and tags. Uploads images. Authenticates via JWT.
- **Reader**: Anonymous developer. Browses published posts, filters by tag, reads articles. Interacts only with Astro static HTML.

**External Systems:**
- **PostgreSQL**: Primary data store for posts, tags, and post-tag associations.
- **ImageKit**: Third-party image hosting. Server-side upload via private key authentication.

### 1.2 Container Diagram (Level 2)

```
                    [Author Browser]              [Reader Browser]
                         |                              |
                    (REST/JSON)                    (HTTPS/HTML)
                         |                              |
                         v                              v
                 +---------------+              +---------------+
                 | .NET 10 API   |              | Astro Frontend|
                 | (Minimal API) |              | (SSG on       |
                 | Fly.io        |              |  Vercel)      |
                 +---------------+              +---------------+
                    |         |                        |
                    |         |                   (fetch at
                    |         |                    build time)
                    v         v                        |
          +-----------+  +-----------+                 |
          | PostgreSQL|  | ImageKit  |                 |
          | (Neon)    |  | (SaaS)   |                 |
          +-----------+  +-----------+                 |
                ^                                      |
                |                                      |
                +---------- (GET /api/posts) ----------+
```

**Container responsibilities:**

| Container | Technology | Responsibility |
|-----------|-----------|----------------|
| .NET 10 API | ASP.NET Minimal API, C# 14 | All business logic, authentication, CRUD operations, image upload orchestration |
| Astro Frontend | Astro 5.x, SSG | Render published content as static HTML at build time. Zero JS for content pages. |
| PostgreSQL | PostgreSQL 16+ | Persist blog posts, tags, post-tag associations |
| ImageKit | SaaS | Store and serve uploaded images via CDN |

**Relationships:**
- Author Browser --> .NET API: REST/JSON over HTTPS (JWT auth for write operations)
- Astro Frontend --> .NET API: fetch at build time (GET endpoints only, no auth)
- .NET API --> PostgreSQL: EF Core (parameterized queries)
- .NET API --> ImageKit: Server-side upload via SDK (private key auth)
- Reader Browser --> Astro Frontend: static HTML over HTTPS (Vercel CDN)

### 1.3 Component Diagram (Level 3) -- Inside the .NET API

```
+========================================================================+
|                           .NET 10 API Container                         |
|                                                                         |
|  DRIVING ADAPTERS              APPLICATION CORE           DRIVEN ADAPTERS|
|  (Primary/Input)               (Domain + Use Cases)       (Secondary)   |
|                                                                         |
|  +------------------+          +--------------------+                   |
|  | Minimal API      |--------->| Driving Ports      |                   |
|  | Endpoints        |          | (interfaces)       |                   |
|  +------------------+          +--------------------+                   |
|                                        |                                |
|  +------------------+                  v                                |
|  | Acceptance Tests |--------->+--------------------+                   |
|  | (xUnit)          |          | Feature Handlers   |                   |
|  +------------------+          | (Vertical Slices)  |                   |
|                                |                    |                   |
|                                | Posts/             |                   |
|                                |   CreatePost       |                   |
|                                |   GetPostBySlug    |                   |
|                                |   ListPosts        |                   |
|                                |   UpdatePost       |                   |
|                                |   DeletePost       |                   |
|                                |   PublishPost      |                   |
|                                |   ListAdminPosts   |                   |
|                                | Tags/              |                   |
|                                |   CreateTag        |                   |
|                                |   ListTags         |                   |
|                                |   RenameTag        |                   |
|                                |   DeleteTag        |                   |
|                                | Images/            |                   |
|                                |   UploadImage      |                   |
|                                | Auth/              |                   |
|                                |   Login            |                   |
|                                +--------------------+                   |
|                                        |                                |
|                                        v                                |
|                                +--------------------+                   |
|                                | Driven Ports       |                   |
|                                | (interfaces)       |                   |
|                                +--------------------+                   |
|                                   |       |       |                     |
|                                   v       v       v                     |
|                          +--------+ +--------+ +--------+               |
|                          |EF Core | |Image-  | |ASP.NET |               |
|                          |Repos   | |Kit     | |Identity|               |
|                          |(PgSQL) | |Storage | |        |               |
|                          +--------+ +--------+ +--------+               |
|                                                                         |
|  +------------------+     +-------------------+                         |
|  | Domain           |     | Shared            |                         |
|  | BlogPost         |     | Value Objects     |                         |
|  | Tag              |     | Title, Slug,      |                         |
|  |                  |     | PostContent,      |                         |
|  |                  |     | TagName, TagSlug, |                         |
|  |                  |     | PostStatus, etc.  |                         |
|  +------------------+     +-------------------+                         |
+=========================================================================+
```

---

## 2. Hexagonal Architecture Mapping

```
DRIVING ADAPTERS              APPLICATION CORE                DRIVEN ADAPTERS
(primary/input)               (domain + use cases)            (secondary/output)

Minimal API    ──────>    Ports (Driving)
Acceptance Tests ────>      |
                            v
                          Use Cases (Vertical Slices)
                          CreatePost
                          GetPostBySlug
                          ListPosts
                          ListAdminPosts
                          UpdatePost
                          DeletePost
                          PublishPost
                          CreateTag
                          ListTags
                          RenameTag
                          DeleteTag
                          UploadImage
                          Login
                            |
                            v
                          Ports (Driven)
                            |
                   +--------+----------+--------+
                   v        v          v        v
              EF Core    ImageKit     Identity  Clock
              (PgSQL)    (Images)    (ASP.NET) (DateTime)
```

### Dependency Rule

**All dependencies point inward. No exceptions.**

```
Domain (center) <-- Application <-- Infrastructure
                                <-- Api
```

| Layer | Depends On | Never Depends On |
|-------|-----------|------------------|
| **Domain** | Nothing | Application, Infrastructure, Api, any NuGet package |
| **Application** | Domain | Infrastructure, Api |
| **Infrastructure** | Application, Domain | Api |
| **Api** | Application | Infrastructure (resolved via DI at composition root) |

The **Api project** is the composition root. It registers Infrastructure implementations against Application port interfaces via dependency injection.

### Port Definitions

**Driven Ports (Application defines, Infrastructure implements):**

| Port | Purpose | Adapter |
|------|---------|---------|
| `IBlogPostRepository` | Persist and query blog posts | EfBlogPostRepository (PostgreSQL) |
| `ITagRepository` | Persist and query tags with post counts | EfTagRepository (PostgreSQL) |
| `IImageStorage` | Upload images, return URL | ImageKitImageStorage |
| `IPasswordHasher` | Hash and verify passwords | AspNetPasswordHasher |
| `ITokenGenerator` | Generate JWT tokens | JwtTokenGenerator |
| `IClock` | Current UTC time (testable) | SystemClock |

**Port Phasing by Epic:**

| Port | Walking Skeleton (F0) | Epic 1 (Posts) | Epic 2 (Tags) | Epic 3 (Images) |
|------|----------------------|----------------|----------------|-----------------|
| `IBlogPostRepository` | Add, FindBySlug, SlugExists | + FindById, ListPublished, ListAll, Update, Delete | | |
| `ITagRepository` | | | All methods | |
| `IImageStorage` | | | | Upload |
| `IPasswordHasher` | | Hash, Verify | | |
| `ITokenGenerator` | | Generate | | |
| `IClock` | UtcNow | | | |

**Walking Skeleton ports**: Only `IBlogPostRepository` (Add, FindBySlug, SlugExists) and `IClock` (UtcNow). Authentication ports (`IPasswordHasher`, `ITokenGenerator`) are introduced in Epic 1.

**Driving Ports (Application exposes, Api/Tests consume):**
Feature handlers are the driving ports. The Api layer calls handlers directly via DI. No MediatR. No dispatcher.

---

## 3. Vertical Slice Organization

Each feature slice is a single file containing the request record (Command/Query) and the Handler class. The slice IS the unit of work.

```
Application/
  Features/
    Posts/
      CreatePost.cs          -- CreatePostCommand + CreatePostHandler
      GetPostBySlug.cs       -- GetPostBySlugQuery + GetPostBySlugHandler
      ListPosts.cs           -- ListPostsQuery + ListPostsHandler
      ListAdminPosts.cs      -- ListAdminPostsQuery + ListAdminPostsHandler
      UpdatePost.cs          -- UpdatePostCommand + UpdatePostHandler
      DeletePost.cs          -- DeletePostCommand + DeletePostHandler
      PublishPost.cs         -- PublishPostCommand + PublishPostHandler
    Tags/
      CreateTag.cs           -- CreateTagCommand + CreateTagHandler
      ListTags.cs            -- ListTagsQuery + ListTagsHandler
      RenameTag.cs           -- RenameTagCommand + RenameTagHandler
      DeleteTag.cs           -- DeleteTagCommand + DeleteTagHandler
    Images/
      UploadImage.cs         -- UploadImageCommand + UploadImageHandler
    Auth/
      Login.cs               -- LoginCommand + LoginHandler
  Ports/
    Driven/
      IBlogPostRepository.cs
      ITagRepository.cs
      IImageStorage.cs
      IPasswordHasher.cs
      ITokenGenerator.cs
      IClock.cs
```

**Anti-pattern avoided**: No separate `Commands/`, `Handlers/`, `Queries/` folders. No technical-layer slicing within the application core.

---

## 4. Solution Structure

```
backend/
  TacBlog.sln
  src/
    TacBlog.Domain/            -- Entities, Value Objects. ZERO external dependencies.
      Entities/
        BlogPost.cs
        Tag.cs
      ValueObjects/
        PostId.cs
        Title.cs
        Slug.cs
        PostContent.cs
        PostStatus.cs
        TagId.cs
        TagName.cs
        TagSlug.cs
        ImageUrl.cs

    TacBlog.Application/       -- Use cases, Port definitions. Depends on Domain only.
      Features/
        Posts/
        Tags/
        Images/
        Auth/
      Ports/
        Driven/

    TacBlog.Infrastructure/    -- Driven adapter implementations. Depends on Application.
      Persistence/
        TacBlogDbContext.cs
        EfBlogPostRepository.cs
        EfTagRepository.cs
        Configurations/
          BlogPostConfiguration.cs
          TagConfiguration.cs
      Storage/
        ImageKitImageStorage.cs
      Identity/
        AspNetPasswordHasher.cs
        JwtTokenGenerator.cs
      Clock/
        SystemClock.cs

    TacBlog.Api/               -- Driving adapter. Composition root. Depends on Application.
      Endpoints/
        PostEndpoints.cs
        TagEndpoints.cs
        ImageEndpoints.cs
        AuthEndpoints.cs
      Program.cs

  tests/
    TacBlog.Domain.Tests/      -- Value Object and Entity unit tests
    TacBlog.Application.Tests/ -- Use case tests (driven ports stubbed via NSubstitute)
    TacBlog.Api.Tests/         -- Integration tests (WebApplicationFactory + Testcontainers)
    TacBlog.Acceptance.Tests/  -- BDD acceptance tests (Outside-In entry point)

frontend/
  (Astro project -- separate driving adapter)
```

### Project References

```
TacBlog.Domain          --> (nothing)
TacBlog.Application     --> TacBlog.Domain
TacBlog.Infrastructure  --> TacBlog.Application
TacBlog.Api             --> TacBlog.Application, TacBlog.Infrastructure (DI registration only)
TacBlog.Domain.Tests    --> TacBlog.Domain
TacBlog.Application.Tests --> TacBlog.Application
TacBlog.Api.Tests       --> TacBlog.Api
TacBlog.Acceptance.Tests --> TacBlog.Api
```

---

## 5. Technology Stack

| Component | Technology | License | Rationale |
|-----------|-----------|---------|-----------|
| Runtime | .NET 10 (LTS) | MIT | C# 14 features (field keyword), mature ecosystem, strong type system |
| Language | C# 14 | MIT | Value Objects with `field` keyword, records for Commands/Queries |
| API Framework | ASP.NET Minimal API | MIT | Lightweight, .NET 10 direction, route groups for vertical slices |
| ORM | Entity Framework Core 10 | MIT | Convention-based mapping, value converters, PostgreSQL provider |
| Database | PostgreSQL 16+ | PostgreSQL License (MIT-like) | Free, Neon serverless, strong JSON support |
| EF Provider | Npgsql.EntityFrameworkCore.PostgreSQL | PostgreSQL License | Official PostgreSQL provider for EF Core |
| Image Storage | Imagekit | MIT | ImageKit .NET SDK for server-side uploads (private key auth) |
| Auth | ASP.NET Identity (password hashing) + custom JWT | MIT | Single admin user; full Identity framework is overkill but password hashing is solid |
| JWT | Microsoft.AspNetCore.Authentication.JwtBearer | MIT | Standard JWT validation middleware |
| Testing | xUnit | Apache 2.0 | De facto .NET test runner |
| Assertions | FluentAssertions | Apache 2.0 | Readable, expressive assertion syntax |
| Mocking | NSubstitute | BSD | Clean syntax for driven port stubbing |
| Integration DB | Testcontainers | MIT | Real PostgreSQL in integration tests |
| BDD Framework | Reqnroll 2.4 | BSD | Gherkin feature files → C# step bindings for acceptance tests |
| API Docs | Built-in OpenAPI 3.1 (.NET 10) | MIT | No Swashbuckle needed |
| Frontend | Astro 5.x | MIT | Zero JS, SSG, Shiki code highlighting, Content Collections |
| Frontend Host | Vercel | Free tier | Edge CDN, zero-config Astro deployment |
| Backend Host | Fly.io | Free tier | Docker container hosting, auto-stop/start, 256MB RAM |

---

## 6. Cross-Cutting Concerns

### Authentication Flow

```
Author --> POST /api/auth/login (email + password)
       <-- 200 { token, expiresAt }

Author --> POST /api/posts (Authorization: Bearer {token})
       <-- 201 { post }
```

- Single admin user. Credentials seeded via environment variables.
- JWT with configurable expiration. No refresh tokens in v1.
- Account lockout: 5 failed attempts in 10 minutes locks for 15 minutes.
- All admin endpoints decorated with `RequireAuthorization()`.

### Error Handling

| Status | Meaning | When |
|--------|---------|------|
| 200 | OK | Successful GET, PUT |
| 201 | Created | Successful POST (create) |
| 204 | No Content | Successful DELETE |
| 400 | Bad Request | Validation failure (empty title, tag name too long, etc.) |
| 401 | Unauthorized | Missing or invalid JWT |
| 404 | Not Found | Post/tag not found by ID or slug |
| 409 | Conflict | Duplicate slug on post creation, duplicate tag name |
| 429 | Too Many Requests | Account locked after 5 failed login attempts |
| 500 | Internal Server Error | Unhandled exception (ImageKit down, DB down) |

Error response body:

```json
{
  "error": "A post with this URL already exists"
}
```

### Build-Time SSG Boundary

The Astro frontend fetches data from the API at build time only. Changes are NOT immediately visible to readers. A Vercel deploy hook (webhook triggered on publish) rebuilds the site. Acceptable delay: under 5 minutes.

```
Author publishes post --> API updates DB
                     --> API calls Vercel deploy hook (POST)
                     --> Vercel rebuilds Astro site (30-90 seconds)
                     --> New static HTML deployed to CDN
```

---

## 7. Bounded Context Mapping

All contexts live in a single monolith but are separated by namespace.

```
TacBlog.Domain/
  Content/     -- BlogPost entity, post-related Value Objects
  Tags/        -- Tag entity, tag-related Value Objects
  Identity/    -- (minimal -- password hashing is infrastructure)
  Media/       -- ImageUrl Value Object

TacBlog.Application/
  Features/
    Posts/      -- Content context use cases
    Tags/       -- Tags context use cases
    Images/     -- Media context use cases
    Auth/       -- Identity context use cases
```

**Context relationships:**
- Identity gates write access to Content, Tags, and Media (JWT middleware)
- Content consumes Tags (many-to-many) and Media (featured image URL)
- Reading context is the Astro frontend consuming Content + Tags via GET endpoints

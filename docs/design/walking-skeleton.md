# Walking Skeleton -- The Augmented Craftsman v1 (Feature 0)

**Goal**: Prove every architectural layer connects end-to-end before building features.
**Duration**: 1 day.
**Scope**: Minimal slice through Domain, Application, Infrastructure, API, and Frontend.

---

## 1. What the Walking Skeleton Proves

| Layer | Validation | Artifact |
|-------|-----------|----------|
| Domain | BlogPost entity created with Title and Slug value objects | `TacBlog.Domain` project compiles |
| Application | CreatePost and GetPostBySlug use cases wired | `TacBlog.Application` project compiles |
| Infrastructure | EF Core persists and retrieves a BlogPost from PostgreSQL | `TacBlog.Infrastructure` project compiles, migrations run |
| API | Minimal API POST and GET endpoints accept/return JSON | `TacBlog.Api` project starts, serves HTTP |
| Frontend | Astro fetches post from API at build time, renders HTML | Astro builds, page at `/posts/{slug}` contains post content |

---

## 2. What the Walking Skeleton Does NOT Include

- Authentication (no JWT, no login)
- Tag management (no tags on posts)
- Image upload (no ImageKit)
- Draft/Published transition (posts are created as Draft but there is no Publish endpoint — the `status` field exists in the schema from day 1, but the Walking Skeleton does not implement the Publish use case; GET /api/posts returns all posts regardless of status)
- Admin UI
- Error handling beyond basic 400/404
- Markdown rendering (raw content returned)
- Excerpt derivation

---

## 3. Minimal Scope

### Domain

- `Title` Value Object (non-empty validation only)
- `Slug` Value Object (generated from Title)
- `PostContent` Value Object (non-empty validation only)
- `PostId` Value Object
- `BlogPost` entity with factory method `Create(Title, PostContent, IClock)`

### Application

- `CreatePost.cs` -- CreatePostCommand record + CreatePostHandler
- `GetPostBySlug.cs` -- GetPostBySlugQuery record + GetPostBySlugHandler
- `IBlogPostRepository` port (Add, FindBySlug, SlugExists)
- `IClock` port

### Infrastructure

- `TacBlogDbContext` with BlogPost DbSet
- `BlogPostConfiguration` with value converters and slug unique index
- `EfBlogPostRepository` implementing IBlogPostRepository
- `SystemClock` implementing IClock
- Initial EF Core migration

### API

- `POST /api/posts` endpoint (accepts title + content only — no tagIds, no featuredImageUrl)
- `GET /api/posts/{slug}` endpoint (returns post by slug or 404)
- `Program.cs` composition root (DI registration, route mapping)

**Walking Skeleton POST /api/posts contract** (subset of full v1 contract in [api-contracts.md](api-contracts.md)):

Request:
```json
{
  "title": "Hello World",
  "content": "This is the **first** post."
}
```

Response 201:
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "title": "Hello World",
  "slug": "hello-world",
  "content": "This is the **first** post.",
  "status": "Draft",
  "publishedAt": null,
  "featuredImageUrl": null,
  "tags": [],
  "createdAt": "2026-03-05T08:00:00Z",
  "updatedAt": "2026-03-05T08:00:00Z"
}
```

**Note**: The full v1 POST /api/posts contract (in [api-contracts.md](api-contracts.md)) includes `tagIds` and `featuredImageUrl`. These fields are added in Epic 1 and Epic 3 respectively. The Walking Skeleton only accepts `title` and `content`.

### Frontend

- Astro page at `src/pages/posts/[slug].astro` that fetches from API at build time
- Minimal layout rendering title and content

---

## 4. Solution Structure to Scaffold

```
backend/
  TacBlog.sln
  src/
    TacBlog.Domain/
      TacBlog.Domain.csproj         -- TargetFramework: net10.0, NO dependencies
      Entities/
        BlogPost.cs
      ValueObjects/
        PostId.cs
        Title.cs
        Slug.cs
        PostContent.cs

    TacBlog.Application/
      TacBlog.Application.csproj    -- References: TacBlog.Domain
      Features/
        Posts/
          CreatePost.cs
          GetPostBySlug.cs
      Ports/
        Driven/
          IBlogPostRepository.cs
          IClock.cs

    TacBlog.Infrastructure/
      TacBlog.Infrastructure.csproj  -- References: TacBlog.Application
      Persistence/                    -- NuGet: Npgsql.EntityFrameworkCore.PostgreSQL
        TacBlogDbContext.cs
        EfBlogPostRepository.cs
        Configurations/
          BlogPostConfiguration.cs
      Clock/
        SystemClock.cs

    TacBlog.Api/
      TacBlog.Api.csproj             -- References: TacBlog.Application, TacBlog.Infrastructure
      Endpoints/
        PostEndpoints.cs
      Program.cs

  tests/
    TacBlog.Domain.Tests/
      TacBlog.Domain.Tests.csproj    -- NuGet: xUnit, FluentAssertions
      ValueObjects/
        TitleShould.cs
        SlugShould.cs

    TacBlog.Application.Tests/
      TacBlog.Application.Tests.csproj -- NuGet: xUnit, FluentAssertions, NSubstitute
      Features/
        Posts/
          CreatePostShould.cs
          GetPostBySlugShould.cs

    TacBlog.Api.Tests/
      TacBlog.Api.Tests.csproj       -- NuGet: xUnit, FluentAssertions, Testcontainers
      Endpoints/
        PostEndpointsShould.cs

    TacBlog.Acceptance.Tests/
      TacBlog.Acceptance.Tests.csproj -- NuGet: xUnit, FluentAssertions, Testcontainers, Reqnroll 2.4
      Features/
        Epic0_WalkingSkeleton/
          CreateAndRetrievePost.feature
      StepDefinitions/
        PostSteps.cs
        CommonSteps.cs
      Drivers/
        PostApiDriver.cs
      Contexts/
        ApiContext.cs
      Support/
        TacBlogWebApplicationFactory.cs

frontend/
  package.json
  astro.config.mjs
  src/
    pages/
      posts/
        [slug].astro
    layouts/
      BaseLayout.astro
```

---

## 5. Test Strategy for Walking Skeleton

### Acceptance Test (Outer Loop)

The first test written. Drives everything else.

```
Test: CreateAndRetrievePostShould

Scenario: Create a post and retrieve it by slug
  Given the API is running with a real PostgreSQL database
  When a POST request is sent to "/api/posts" with:
    | title   | Hello World                 |
    | content | This is the **first** post. |
  Then the response status is 201
  And the response contains a post with slug "hello-world"
  When a GET request is sent to "/api/posts/hello-world"
  Then the response status is 200
  And the response contains title "Hello World"
  And the response contains content "This is the **first** post."
```

**Implementation**: xUnit test using `WebApplicationFactory<Program>` with Testcontainers for PostgreSQL. This test stays RED until all layers are connected.

### Unit Tests (Inner Loop)

Written as the acceptance test drives us inward.

**TitleShould:**
- `reject_empty_value`
- `reject_null_value`
- `accept_valid_title`
- `trim_whitespace`

**SlugShould:**
- `generate_from_simple_title` ("Hello World" --> "hello-world")
- `lowercase_all_characters` ("SOLID Principles" --> "solid-principles")
- `replace_special_characters_with_hyphens` ("TDD Is Not About Testing!" --> "tdd-is-not-about-testing")
- `collapse_multiple_spaces` ("The  Walking   Skeleton" --> "the-walking-skeleton")
- `reject_empty_value`

**CreatePostShould:**
- `create_draft_post_with_generated_slug`
- `reject_empty_title`
- `reject_duplicate_slug`
- `persist_post_via_repository`

**GetPostBySlugShould:**
- `return_post_when_found`
- `return_null_when_not_found`

### Integration Test

**PostEndpointsShould:**
- `return_201_for_valid_post_creation`
- `return_400_for_empty_title`
- `return_200_for_existing_slug`
- `return_404_for_nonexistent_slug`

---

## 6. Day 1 Plan

### Morning: Scaffold and Domain (3-4 hours)

1. **Scaffold solution structure**
   - Create `TacBlog.sln` with 4 src projects and 4 test projects
   - Set up project references (dependency rule enforced)
   - Add NuGet packages to each project

2. **Write first acceptance test (RED)**
   - `CreateAndRetrievePostShould.create_a_post_and_retrieve_it_by_slug`
   - Configure WebApplicationFactory with Testcontainers
   - Test will not compile yet (no endpoint, no handler)

3. **TDD Domain Value Objects (inner loop)**
   - `TitleShould` tests --> implement `Title`
   - `SlugShould` tests --> implement `Slug`
   - `PostContent` (trivial, minimal tests)
   - `PostId` (trivial, minimal tests)

4. **TDD BlogPost entity**
   - `BlogPostShould.create_with_title_and_generated_slug`
   - Implement `BlogPost.Create()` factory method

### Afternoon: Application and Infrastructure (3-4 hours)

5. **TDD CreatePost use case**
   - `CreatePostShould` tests with NSubstitute for IBlogPostRepository
   - Implement CreatePostCommand + CreatePostHandler

6. **TDD GetPostBySlug use case**
   - `GetPostBySlugShould` tests with NSubstitute
   - Implement GetPostBySlugQuery + GetPostBySlugHandler

7. **Implement Infrastructure**
   - `TacBlogDbContext` with BlogPost DbSet
   - `BlogPostConfiguration` with value converters
   - `EfBlogPostRepository` implementing IBlogPostRepository
   - `SystemClock` implementing IClock
   - Create initial EF Core migration
   - Verify migration applies to Testcontainers PostgreSQL

8. **Wire API endpoints**
   - `PostEndpoints.cs` with MapPost and MapGet
   - `Program.cs` composition root (DI, routing)
   - Run integration tests

### Evening: Frontend and Green (1-2 hours)

9. **Acceptance test goes GREEN**
   - All layers connected
   - POST creates, GET retrieves, assertion passes

10. **Astro frontend**
    - `[slug].astro` page fetching from API
    - `BaseLayout.astro` minimal layout
    - Verify `astro build` fetches from running API
    - Page at `/posts/hello-world` renders title and content

11. **REFACTOR while green**
    - Clean up any code smells
    - Verify all tests still pass
    - Walking Skeleton complete

---

## 7. Walking Skeleton Success Criteria

- [ ] `dotnet test` passes all tests (acceptance, integration, unit)
- [ ] POST /api/posts with `{"title": "Hello World", "content": "First post"}` returns 201 with slug "hello-world"
- [ ] GET /api/posts/hello-world returns 200 with title and content
- [ ] GET /api/posts/nonexistent returns 404
- [ ] POST /api/posts with empty title returns 400
- [ ] EF Core migration creates blog_posts table in PostgreSQL
- [ ] Astro builds and renders /posts/hello-world with post content
- [ ] All dependencies point inward (Domain has zero project references)

---

## 8. Environment Requirements

| Requirement | Details |
|------------|---------|
| .NET 10 SDK | `dotnet --version` >= 10.0.100 |
| Docker | Required for Testcontainers (PostgreSQL) |
| Node.js | >= 20.x for Astro |
| PostgreSQL | Via Docker/Testcontainers only (no local install needed) |

### Environment Variables (development)

```env
# .NET API (backend/appsettings.Development.json or environment)
ConnectionStrings__DefaultConnection=Host=localhost;Database=tacblog;Username=postgres;Password=postgres

# Astro frontend (frontend/.env)
PUBLIC_API_URL=http://localhost:5000
```

**Setup requirements:**
- Backend: `ConnectionStrings__DefaultConnection` is required. The API fails fast at startup if missing (no silent fallback).
- Frontend: `PUBLIC_API_URL` is required for Astro build. Build fails with a clear error if not set.
- Testcontainers: No manual PostgreSQL setup needed. Tests use Testcontainers which creates its own connection string.

### Frontend Error Handling (Walking Skeleton)

The Astro frontend's `[slug].astro` page must handle API failures at build time:

| Scenario | Behavior |
|----------|----------|
| API unreachable | Build fails with clear error: "Cannot fetch from API at {PUBLIC_API_URL}. Is the backend running?" |
| GET /api/posts/{slug} returns 404 | Page is not generated (Astro `getStaticPaths` excludes it) |
| GET /api/posts returns empty array | Homepage renders empty state: "Coming soon. The first post is being forged." |
| Malformed JSON response | Build fails with parse error (standard Astro behavior) |

**Implementation note**: Astro's `getStaticPaths()` fetches the post list from `GET /api/posts` and generates a page for each slug. If the API is down, the build itself fails — there is no runtime fallback because all pages are static HTML.

---

## 9. After the Walking Skeleton

With Feature 0 complete, the team moves to Epic 1 (Blog Post Management) starting with authentication (US-010). Every subsequent feature follows the same Outside-In flow:

1. Write failing acceptance test
2. Drop to unit tests for domain and application logic
3. Implement until acceptance test passes
4. Refactor while green
5. Next feature

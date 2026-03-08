# Test Strategy -- The Augmented Craftsman v1

**Approach**: Outside-In TDD with Double Loop
**Framework**: xUnit + FluentAssertions + NSubstitute + Testcontainers + Reqnroll 2.4
**Philosophy**: Test behavior through public APIs, never implementation details

---

## 1. Test Pyramid

```
            /\
           /  \         Acceptance Tests (outer loop)
          /    \        BDD scenarios, WebApplicationFactory + Testcontainers
         /------\       ~20 tests, slowest, highest confidence
        /        \
       /          \     Integration Tests
      /            \    API endpoints with real PostgreSQL
     /--------------\   ~30 tests, moderate speed
    /                \
   /                  \  Unit Tests (inner loop)
  /                    \ Domain logic, Value Objects, Handlers with stubbed ports
 /______________________\~80 tests, fastest, most granular
```

| Level | Count (est.) | Speed | What It Tests | Database |
|-------|-------------|-------|---------------|----------|
| Acceptance | ~20 | Slow (seconds) | Full feature scenarios end-to-end | Testcontainers PostgreSQL |
| Integration | ~30 | Medium (ms) | API endpoints, HTTP contracts, serialization | Testcontainers PostgreSQL |
| Unit | ~80 | Fast (ms) | Value Objects, entities, use case handlers | None (ports stubbed) |

---

## 2. Outside-In Double Loop Flow

```
OUTER LOOP (Acceptance Test):
  RED: Write failing acceptance test for the feature
       |
       +---> Does not compile? Fix compilation minimally.
       |
       +---> Compiles but fails? Drop to inner loop.
       |
       v
  INNER LOOP (Unit Tests):
    RED: Write failing unit test for domain/application logic
    GREEN: Implement minimal code to pass
    REFACTOR: Clean up while green
    REPEAT: Next unit test until feature logic is complete
       |
       v
  GREEN: Acceptance test passes (all layers connected)
  REFACTOR: Clean up across layers while all tests green
  NEXT: Write next acceptance test
```

### Concrete Example: CreatePost Feature

1. **Acceptance test (RED)**: POST /api/posts with title and content --> expect 201 with slug
2. **Drop to unit test**: `TitleShould.reject_empty_value` --> implement Title Value Object
3. **Unit test**: `SlugShould.generate_from_simple_title` --> implement Slug Value Object
4. **Unit test**: `CreatePostShould.create_draft_post_with_generated_slug` --> implement handler
5. **Unit test**: `CreatePostShould.reject_duplicate_slug` --> add SlugExists check
6. **Implement infrastructure**: EfBlogPostRepository, BlogPostConfiguration
7. **Wire API**: POST endpoint in PostEndpoints.cs, DI in Program.cs
8. **Acceptance test (GREEN)**: All layers connected, assertion passes
9. **REFACTOR**: Clean up code smells while green

---

## 3. Test Project Structure

```
tests/
  TacBlog.Domain.Tests/           -- Unit tests for Value Objects and Entities
    ValueObjects/
      TitleShould.cs
      SlugShould.cs
      PostContentShould.cs
      TagNameShould.cs
      TagSlugShould.cs
      ImageUrlShould.cs
    Entities/
      BlogPostShould.cs
      TagShould.cs

  TacBlog.Application.Tests/      -- Unit tests for use case handlers
    Features/
      Posts/
        CreatePostShould.cs
        GetPostBySlugShould.cs
        ListPostsShould.cs
        UpdatePostShould.cs         -- includes: keep_original_slug_when_title_changes
        DeletePostShould.cs
        PublishPostShould.cs
        ListAdminPostsShould.cs
      Tags/
        CreateTagShould.cs
        ListTagsShould.cs
        RenameTagShould.cs
        DeleteTagShould.cs
      Images/
        UploadImageShould.cs
      Auth/
        LoginShould.cs

  TacBlog.Api.Tests/              -- Integration tests for HTTP endpoints
    Endpoints/
      PostEndpointsShould.cs
      TagEndpointsShould.cs
      ImageEndpointsShould.cs
      AuthEndpointsShould.cs
    Infrastructure/
      TacBlogWebApplicationFactory.cs

  TacBlog.Acceptance.Tests/       -- BDD acceptance tests (Reqnroll 2.4 + Gherkin)
    Features/
      Epic0_WalkingSkeleton/
        CreateAndRetrievePost.feature
        RenderPostInAstro.feature
      Epic1_PostManagement/
        Authentication.feature
        CreatePost.feature
        PreviewPost.feature
        PublishPost.feature
        EditPost.feature
        DeletePost.feature
        ListPosts.feature
      Epic2_TagManagement/
        CreateTag.feature
        ListTags.feature
        RenameTag.feature
        DeleteTag.feature
        AssociateTagsWithPosts.feature
      Epic3_ImageManagement/
        UploadImage.feature
        SetFeaturedImage.feature
        RemoveFeaturedImage.feature
      Epic4_PublicReading/
        Homepage.feature
        BrowseAllPosts.feature
        FilterPostsByTag.feature
        ReadSinglePost.feature
        RelatedPosts.feature
        BrowseTags.feature
    StepDefinitions/
      CommonSteps.cs
      PostSteps.cs
      AuthSteps.cs
      TagSteps.cs
      ImageSteps.cs
    Drivers/
      PostApiDriver.cs
      TagApiDriver.cs
      AuthApiDriver.cs
      ImageApiDriver.cs
    Contexts/
      ApiContext.cs
      AuthContext.cs
    Hooks/
      TestHooks.cs
    Support/
      TacBlogWebApplicationFactory.cs
      DependencyConfig.cs
```

---

## 4. Test Naming Convention

```csharp
// Pattern: ClassNameShould + snake_case method
// Read as: "[Class] should [behavior]"

class BlogPostShould
{
    void generate_slug_from_title() {}
    void reject_empty_title() {}
    void publish_when_in_draft_state() {}
    void reject_publish_when_already_published() {}
}

class CreatePostShould
{
    void create_draft_post_with_generated_slug() {}
    void reject_empty_title() {}
    void reject_duplicate_slug() {}
    void persist_post_via_repository() {}
}

class UpdatePostShould
{
    void update_title_and_content() {}
    void keep_original_slug_when_title_changes() {}  // Slug immutability invariant
    void reject_empty_title() {}
    void replace_tag_associations() {}
    void remove_featured_image_when_set_to_null() {}
}

class TitleShould
{
    void accept_valid_title() {}
    void reject_empty_value() {}
    void reject_null_value() {}
    void reject_value_exceeding_200_characters() {}
    void trim_whitespace() {}
}
```

**Rules:**
- No technical names (no `TestMethod1`, no `WhenTitleIsNull_ThrowsException`)
- No implementation details in test names (no `CallsRepositoryAdd`)
- Behavior-oriented, readable as English sentences
- snake_case for readability (C# convention for test methods in many XP shops)

---

## 5. Test Doubles Strategy

### Command-Query Separation

| Type | Test Double | Phase |
|------|------------|-------|
| **Queries** (return data, no side effects) | **Stubs** | Arrange |
| **Commands** (change state, return void) | **Mocks** | Assert |

### Tool: NSubstitute

```csharp
// Stub example (Arrange): repository returns a known post
var repository = Substitute.For<IBlogPostRepository>();
repository.FindBySlug(Arg.Any<Slug>())
    .Returns(existingPost);

// Mock example (Assert): verify repository was called
await repository.Received(1).Add(Arg.Any<BlogPost>());
```

### What to Stub/Mock

| Port | When Stubbed | When Mocked |
|------|-------------|-------------|
| `IBlogPostRepository` | FindBySlug, FindById, ListPublished, SlugExists | Add, Update, Delete |
| `ITagRepository` | FindById, ListAllWithPostCounts, NameExists | Add, Update, Delete |
| `IImageStorage` | (returns URL) | Upload |
| `IPasswordHasher` | Verify (returns bool) | Hash |
| `ITokenGenerator` | Generate (returns string) | (none -- always a query) |
| `IClock` | UtcNow (returns fixed time) | (none -- always a query) |

### Rules

- Only stub/mock interfaces you own (driven ports)
- Never stub/mock third-party libraries directly (ImageKit SDK is wrapped behind IImageStorage)
- Never add behavior to test doubles
- Verify as little as possible -- assert outcomes, not interactions (prefer stubs over mocks when possible)

---

## 6. Integration Test Infrastructure

### WebApplicationFactory

```csharp
// TacBlogWebApplicationFactory configures:
// - Testcontainers PostgreSQL (real database)
// - EF Core migrations applied automatically
// - No authentication bypass (tests use real JWT flow)
// - Shared across all integration tests in the assembly
```

### Testcontainers PostgreSQL

```csharp
// Container sharing strategy: xUnit IAsyncLifetime collection fixture
//
// 1. A single PostgresContainer is shared across all tests in an assembly
//    via [CollectionDefinition] and IAsyncLifetime:
//
//    [CollectionDefinition("Database")]
//    public class DatabaseCollection : ICollectionFixture<PostgresFixture> { }
//
//    public class PostgresFixture : IAsyncLifetime
//    {
//        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
//            .WithImage("postgres:16-alpine")
//            .WithDatabase("tacblog_test")
//            .WithUsername("test")
//            .WithPassword("test")
//            .Build();
//
//        public string ConnectionString => _container.GetConnectionString();
//        public Task InitializeAsync() => _container.StartAsync();
//        public Task DisposeAsync() => _container.DisposeAsync().AsTask();
//    }
//
// 2. Each test class uses [Collection("Database")] to share the container.
//
// 3. Database cleanup between tests: DELETE FROM post_tags; DELETE FROM blog_posts;
//    DELETE FROM tags; in a base class Setup() method. Transaction rollback is an
//    alternative but harder to debug.
//
// 4. EF Core migrations are applied once during fixture InitializeAsync,
//    after container starts.
//
// 5. Migration idempotency: EF Core migrations are idempotent by design
//    (CREATE TABLE IF NOT EXISTS semantics). The container starts fresh
//    each test session, so there is no stale schema risk. If a migration
//    fails, the test run fails fast — no partial schema to clean up.
//
// NuGet: Testcontainers.PostgreSql (not the base Testcontainers package)
```

### Test Data Builders

```csharp
// Builder pattern for creating test entities with sensible defaults:
// - ABlogPost.WithTitle("TDD Is Not About Testing").Build()
// - ATag.WithName("Clean Code").Build()
// - Avoids repetitive Arrange sections
// - Single place to maintain test data creation
```

---

## 7. Acceptance Test Design

### Base Class

```csharp
// AcceptanceTestBase provides:
// - HttpClient configured with WebApplicationFactory
// - Helper methods: PostAsync<T>, GetAsync<T>, AuthenticateAsync
// - Database cleanup between tests
// - Shared Testcontainers PostgreSQL
```

### Scenario Structure

```csharp
// Each acceptance test maps to one or more Gherkin scenarios
// from docs/requirements/acceptance-criteria.md
//
// Structure: Arrange (Given) - Act (When) - Assert (Then)
// One scenario per test method
// Test name describes the scenario in snake_case

class CreatePostWithFullContentShould : AcceptanceTestBase
{
    async Task create_draft_with_title_and_content()
    {
        // Given: authenticated
        // When: POST /api/posts with title and content
        // Then: 201, slug generated, status Draft
    }

    async Task reject_post_with_empty_title()
    {
        // Given: authenticated
        // When: POST /api/posts with empty title
        // Then: 400, "Title is required"
    }

    async Task reject_post_with_duplicate_slug()
    {
        // Given: authenticated, post with slug "tdd-is-not-about-testing" exists
        // When: POST /api/posts with title "TDD Is Not About Testing"
        // Then: 400, "A post with this URL already exists"
    }
}
```

---

## 8. Test Coverage Goals

| Layer | Coverage Target | What Is Covered |
|-------|----------------|-----------------|
| Domain | All Value Object validation rules, all entity invariants | Title bounds, Slug generation, PostStatus transitions, TagName uniqueness |
| Application | All use case happy paths and error paths | CreatePost, GetPostBySlug, PublishPost, UpdatePost, DeletePost, all tag/image handlers |
| API | All endpoint contracts (status codes, response shapes) | 201/200/400/401/404 for every endpoint |
| Acceptance | All Must-priority Gherkin scenarios from acceptance-criteria.md | ~20 scenarios covering full user journeys |

**Not covered by automated tests:**
- Astro frontend rendering (verified manually or via build success)
- ImageKit actual upload (IImageStorage is stubbed in unit tests; integration test uses a fake adapter)
- Visual design and CSS (not testable via xUnit)

---

## 9. Property-Based Testing Candidates

For critical domain logic where edge cases matter:

| Logic | Property |
|-------|----------|
| Slug generation | For any valid title string, the generated slug contains only lowercase alphanumeric characters and hyphens |
| Slug generation | Slugify is idempotent: `Slugify(Slugify(x)) == Slugify(x)` |
| Slug generation | Generated slug is never empty for any non-empty title |
| Title validation | Any string of 1-200 non-whitespace characters is a valid Title |
| Title validation | Any string exceeding 200 characters is rejected |
| TagName validation | Any string of 1-50 non-whitespace characters is a valid TagName |
| PostStatus transition | A Draft post can always be published exactly once |
| PostStatus transition | A Published post can never transition to any other state |

**Library**: FsCheck.xUnit or AutoFixture for property-based tests in .NET.

---

## 10. Test Execution

### Local Development

```bash
cd backend

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/TacBlog.Domain.Tests/

# Run tests matching a filter
dotnet test --filter "SlugShould"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

### CI Pipeline

```bash
# 1. Unit tests (fast feedback)
dotnet test tests/TacBlog.Domain.Tests/
dotnet test tests/TacBlog.Application.Tests/

# 2. Integration tests (requires Docker)
dotnet test tests/TacBlog.Api.Tests/

# 3. Acceptance tests (requires Docker)
dotnet test tests/TacBlog.Acceptance.Tests/
```

**Docker requirement**: Integration and acceptance tests use Testcontainers, which requires Docker to be available. CI must have Docker-in-Docker or a Docker socket mount.

---

## 11. Key Testing Principles

1. **Write one test at a time.** Wait for it to fail for the right reason before implementing.
2. **Test behavior, not implementation.** Assert what the system does, not how it does it.
3. **Tests should tell you when you are done.** When all tests pass, the feature is complete.
4. **Never break tests during refactoring.** If a test breaks during refactor, undo and try again.
5. **Red-Green-Refactor.** No shortcuts. No skipping the red phase.
6. **Assert first, work backward.** Write the Then clause first, then figure out the Given and When.
7. **Only mock/stub interfaces you own.** All third-party libraries are behind driven port interfaces.
8. **No test should depend on another test's state.** Each test is independent and repeatable.

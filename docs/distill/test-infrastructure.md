# Test Infrastructure Design -- The Augmented Craftsman v1

**Framework**: Reqnroll 2.4 + xUnit 2.9 + Testcontainers + WebApplicationFactory
**Project**: `backend/tests/TacBlog.Acceptance.Tests/`

---

## 1. Project Structure

```
backend/tests/TacBlog.Acceptance.Tests/
├── TacBlog.Acceptance.Tests.csproj
├── reqnroll.json
│
├── Features/                          # Gherkin feature files by epic
│   ├── Epic0_WalkingSkeleton/
│   │   ├── CreateAndRetrievePost.feature
│   │   └── RenderPostInAstro.feature
│   ├── Epic1_PostManagement/
│   │   ├── Authentication.feature
│   │   ├── CreatePost.feature
│   │   ├── PreviewPost.feature
│   │   ├── PublishPost.feature
│   │   ├── EditPost.feature
│   │   ├── DeletePost.feature
│   │   └── ListPosts.feature
│   ├── Epic2_TagManagement/
│   │   ├── CreateTag.feature
│   │   ├── ListTags.feature
│   │   ├── RenameTag.feature
│   │   ├── DeleteTag.feature
│   │   └── AssociateTagsWithPosts.feature
│   ├── Epic3_ImageManagement/
│   │   ├── UploadImage.feature
│   │   ├── SetFeaturedImage.feature
│   │   └── RemoveFeaturedImage.feature
│   └── Epic4_PublicReading/
│       ├── Homepage.feature
│       ├── BrowseAllPosts.feature
│       ├── FilterPostsByTag.feature
│       ├── ReadSinglePost.feature
│       ├── RelatedPosts.feature
│       └── BrowseTags.feature
│
├── StepDefinitions/                   # Step bindings (one per feature area)
│   ├── PostSteps.cs
│   ├── TagSteps.cs
│   ├── AuthSteps.cs
│   ├── ImageSteps.cs
│   └── CommonSteps.cs                # Shared Given/Then steps
│
├── Contexts/                          # Scenario-scoped state
│   ├── ApiContext.cs                  # HTTP response state
│   └── AuthContext.cs                 # JWT token state
│
├── Drivers/                           # API interaction layer
│   ├── PostApiDriver.cs
│   ├── TagApiDriver.cs
│   ├── AuthApiDriver.cs
│   └── ImageApiDriver.cs
│
├── Hooks/                             # Reqnroll lifecycle hooks
│   └── TestHooks.cs                   # Database cleanup
│
└── Support/                           # Infrastructure
    ├── DependencyConfig.cs            # Reqnroll DI registration
    └── TacBlogWebApplicationFactory.cs # WebApplicationFactory + Testcontainers
```

---

## 2. NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.NET.Test.Sdk | 17.12.0 | Test SDK |
| xunit | 2.9.3 | Test runner |
| xunit.runner.visualstudio | 2.8.2 | VS/CLI integration |
| Reqnroll.xUnit | 2.4.1 | Gherkin → xUnit binding |
| Reqnroll.Microsoft.Extensions.DependencyInjection | 2.4.1 | Scenario-scoped DI |
| FluentAssertions | 7.1.0 | Readable assertions |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 | WebApplicationFactory |
| Testcontainers.PostgreSql | 4.3.0 | Ephemeral PostgreSQL |

---

## 3. Architecture Layers

```
┌─────────────────────┐
│  .feature files     │  Gherkin scenarios (business language)
├─────────────────────┤
│  StepDefinitions/   │  Thin bindings: translate Gherkin → Driver calls
├─────────────────────┤
│  Drivers/           │  HTTP interaction: build requests, capture responses
├─────────────────────┤
│  Contexts/          │  Scenario state: ApiContext, AuthContext
├─────────────────────┤
│  Support/           │  Infrastructure: WebApplicationFactory, DI, Hooks
├─────────────────────┤
│  WebApplicationFactory  │  In-process ASP.NET Core host
├─────────────────────┤
│  Testcontainers     │  Real PostgreSQL 16 in Docker
└─────────────────────┘
```

### Design Principles

1. **Step definitions are thin**: No HTTP calls, no assertions beyond delegation. They call Drivers and read Contexts.
2. **Drivers encapsulate HTTP**: Each Driver builds requests, applies auth headers, captures responses into ApiContext.
3. **Contexts are scenario-scoped POCOs**: Reqnroll creates fresh instances per scenario via DI.
4. **Single WebApplicationFactory**: Shared singleton. Testcontainers PostgreSQL starts once per test run.
5. **Real authentication**: No auth bypass. Tests use `AuthApiDriver.Authenticate()` which calls `POST /api/auth/login`.

---

## 4. Database Strategy

### Container Lifecycle

```
Test Run Start
  └── TacBlogWebApplicationFactory.InitializeAsync()
        └── PostgreSqlContainer.StartAsync()
              └── postgres:16-alpine container starts
              └── EF Core migrations applied

  Scenario 1:
    └── @BeforeScenario: DELETE FROM post_tags, blog_posts, tags
    └── Steps execute
    └── @AfterScenario: (no-op, cleanup happens before next scenario)

  Scenario 2:
    └── @BeforeScenario: DELETE FROM ...
    └── Steps execute

  ...

Test Run End
  └── TacBlogWebApplicationFactory.DisposeAsync()
        └── PostgreSqlContainer.DisposeAsync()
              └── Container removed
```

### Why DELETE vs. Transaction Rollback

- DELETE is simpler to debug (data visible during test execution)
- Transaction rollback hides data from breakpoint inspection
- DELETE order respects FK constraints: `post_tags → blog_posts, tags`
- Acceptable performance for ~71 scenarios

---

## 5. Reqnroll DI Configuration

```csharp
[ScenarioDependencies]
public static IServiceCollection CreateServices()
{
    var services = new ServiceCollection();

    // Singleton: one factory per test run
    services.AddSingleton(Factory);

    // Scoped: fresh per scenario
    services.AddScoped(_ => Factory.CreateClient(...));
    services.AddScoped<ApiContext>();
    services.AddScoped<AuthContext>();
    services.AddScoped<PostApiDriver>();
    services.AddScoped<TagApiDriver>();
    services.AddScoped<AuthApiDriver>();
    services.AddScoped<ImageApiDriver>();

    return services;
}
```

### Lifecycle

| Registration | Scope | Instances |
|-------------|-------|-----------|
| TacBlogWebApplicationFactory | Singleton | 1 per test run |
| HttpClient | Scoped | Fresh per scenario |
| ApiContext | Scoped | Fresh per scenario |
| AuthContext | Scoped | Fresh per scenario |
| Drivers | Scoped | Fresh per scenario |

---

## 6. Docker Requirements

| Requirement | Details |
|------------|---------|
| Docker Engine | Must be running for Testcontainers |
| Image | `postgres:16-alpine` (pulled automatically) |
| Port | Random port assigned by Testcontainers |
| CI compatibility | GitHub Actions ubuntu-latest includes Docker |

### CI command

```bash
dotnet test backend/tests/TacBlog.Acceptance.Tests/ \
  --logger "console;verbosity=detailed"
```

No manual Docker setup needed. Testcontainers manages everything.

---

## 7. Running Tests

```bash
# All acceptance tests
dotnet test backend/tests/TacBlog.Acceptance.Tests/

# Walking Skeleton only
dotnet test --filter "Category=epic0"

# Skip unimplemented
dotnet test --filter "Category!=skip"

# Specific feature
dotnet test --filter "FullyQualifiedName~CreateAndRetrievePost"

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

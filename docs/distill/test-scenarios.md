# Test Scenarios -- The Augmented Craftsman v1

**Wave**: DISTILL
**Framework**: Reqnroll 2.4 + xUnit + Testcontainers + WebApplicationFactory
**Approach**: Outside-In TDD, BDD acceptance tests as outer loop

---

## 1. Scenario Inventory

| Epic | Feature File | User Stories | Scenarios | Tag |
|------|-------------|-------------|-----------|-----|
| 0 - Walking Skeleton | CreateAndRetrievePost.feature | US-001, US-002 | 6 | `@epic0` |
| 0 - Walking Skeleton | RenderPostInAstro.feature | US-003 | 2 | `@epic0 @frontend @manual` |
| 1 - Post Management | Authentication.feature | US-010 | 5 | `@epic1 @skip` |
| 1 - Post Management | CreatePost.feature | US-011, US-012 | 9 | `@epic1 @skip` |
| 1 - Post Management | PreviewPost.feature | US-013 | 4 | `@epic1 @skip` |
| 1 - Post Management | PublishPost.feature | US-014 | 3 | `@epic1 @skip` |
| 1 - Post Management | EditPost.feature | US-015 | 5 | `@epic1 @skip` |
| 1 - Post Management | DeletePost.feature | US-016 | 3 | `@epic1 @skip` |
| 1 - Post Management | ListPosts.feature | US-017 | 3 | `@epic1 @skip` |
| 2 - Tag Management | CreateTag.feature | US-020 | 4 | `@epic2 @skip` |
| 2 - Tag Management | ListTags.feature | US-021 | 2 | `@epic2 @skip` |
| 2 - Tag Management | RenameTag.feature | US-022 | 2 | `@epic2 @skip` |
| 2 - Tag Management | DeleteTag.feature | US-023 | 3 | `@epic2 @skip` |
| 2 - Tag Management | AssociateTagsWithPosts.feature | US-024 | 4 | `@epic2 @skip` |
| 3 - Image Management | UploadImage.feature | US-030 | 3 | `@epic3 @skip` |
| 3 - Image Management | SetFeaturedImage.feature | US-031 | 3 | `@epic3 @skip` |
| 3 - Image Management | RemoveFeaturedImage.feature | US-032 | 3 | `@epic3 @skip` |
| 4 - Public Reading | Homepage.feature | US-040 | 2 | `@epic4 @skip` |
| 4 - Public Reading | BrowseAllPosts.feature | US-041 | 2 | `@epic4 @skip` |
| 4 - Public Reading | FilterPostsByTag.feature | US-042 | 3 | `@epic4 @skip` |
| 4 - Public Reading | ReadSinglePost.feature | US-043 | 3 | `@epic4 @skip` |
| 4 - Public Reading | RelatedPosts.feature | US-044 | 7 | `@epic4 @skip` |
| 4 - Public Reading | BrowseTags.feature | US-045, US-046 | 3 | `@epic4 @skip` |
| **Total** | **23 feature files** | **26 user stories** | **84 scenarios** | |

---

## 2. Implementation Order

The `@skip` tag marks scenarios not yet ready for implementation. Remove `@skip` one epic at a time.

### Phase 1: Walking Skeleton (Epic 0) -- No @skip

| Order | Feature File | Key Scenarios |
|-------|-------------|---------------|
| 1 | CreateAndRetrievePost.feature | Create post, retrieve by slug, 404, validation |
| 2 | RenderPostInAstro.feature | Astro builds with API data (manual verification) |

**Success gate**: `dotnet test --filter "Category=epic0"` all green.

### Phase 2: Authentication (Epic 1 partial)

| Order | Feature File | Key Scenarios |
|-------|-------------|---------------|
| 3 | Authentication.feature | Login, lockout, JWT issuance, 401 |

**Success gate**: Auth scenarios green + existing Epic 0 still green.

### Phase 3: Post CRUD (Epic 1 remainder)

| Order | Feature File | Key Scenarios |
|-------|-------------|---------------|
| 4 | CreatePost.feature | Full creation with tags, slug generation, validation |
| 5 | PublishPost.feature | Draft → Published transition |
| 6 | EditPost.feature | Update, slug immutability |
| 7 | DeletePost.feature | Deletion with tag preservation |
| 8 | ListPosts.feature | Admin list with status filters |
| 9 | PreviewPost.feature | Markdown rendering |

### Phase 4: Tag Management (Epic 2)

| Order | Feature File | Key Scenarios |
|-------|-------------|---------------|
| 10 | CreateTag.feature | Tag CRUD |
| 11 | ListTags.feature | Tags with post counts |
| 12 | RenameTag.feature | Rename propagation |
| 13 | DeleteTag.feature | Cascade handling |
| 14 | AssociateTagsWithPosts.feature | Tag ↔ post associations |

### Phase 5: Image Management (Epic 3)

| Order | Feature File | Key Scenarios |
|-------|-------------|---------------|
| 15 | UploadImage.feature | Image upload and validation |
| 16 | SetFeaturedImage.feature | Post image association |
| 17 | RemoveFeaturedImage.feature | Image removal |

### Phase 6: Public Reading (Epic 4)

| Order | Feature File | Key Scenarios |
|-------|-------------|---------------|
| 18 | Homepage.feature | Latest posts list |
| 19 | BrowseAllPosts.feature | Published-only filtering |
| 20 | FilterPostsByTag.feature | Tag-based filtering |
| 21 | ReadSinglePost.feature | Single post endpoint + draft visibility |
| 22 | RelatedPosts.feature | Tag-based recommendations |
| 23 | BrowseTags.feature | Public tag browsing |

---

## 3. Story-to-Scenario Traceability

| User Story | Feature File | Scenario Count |
|-----------|-------------|---------------|
| US-001 | CreateAndRetrievePost.feature | 4 |
| US-002 | CreateAndRetrievePost.feature | 2 |
| US-003 | RenderPostInAstro.feature | 2 |
| US-010 | Authentication.feature | 5 |
| US-011 | CreatePost.feature | 5 |
| US-012 | CreatePost.feature | 4 |
| US-013 | PreviewPost.feature | 4 |
| US-014 | PublishPost.feature | 3 |
| US-015 | EditPost.feature | 5 |
| US-016 | DeletePost.feature | 3 |
| US-017 | ListPosts.feature | 3 |
| US-020 | CreateTag.feature | 4 |
| US-021 | ListTags.feature | 2 |
| US-022 | RenameTag.feature | 2 |
| US-023 | DeleteTag.feature | 3 |
| US-024 | AssociateTagsWithPosts.feature | 4 |
| US-030 | UploadImage.feature | 3 |
| US-031 | SetFeaturedImage.feature | 3 |
| US-032 | RemoveFeaturedImage.feature | 3 |
| US-040 | Homepage.feature | 2 |
| US-041 | BrowseAllPosts.feature | 2 |
| US-042 | FilterPostsByTag.feature | 3 |
| US-043 | ReadSinglePost.feature | 3 |
| US-044 | RelatedPosts.feature | 7 |
| US-045 | BrowseTags.feature | 2 |
| US-046 | BrowseTags.feature | 1 |

All 26 user stories have corresponding acceptance tests. 100% coverage.

---

## 4. Tag Strategy

| Tag | Purpose | Usage |
|-----|---------|-------|
| `@epic0` through `@epic4` | Epic identification | Filter by epic |
| `@skip` | Not yet implemented | Exclude from test runs until ready |
| `@manual` | Requires manual/build-script verification | Not runnable via dotnet test |
| `@smoke` | Critical path, fast feedback | CI fast gate |
| `@api` | API-only tests (WebApplicationFactory) | Majority of tests |
| `@frontend` | Frontend rendering tests | Manual or build-script verification |

### Running tests by phase

```bash
# Walking Skeleton only (Phase 1)
dotnet test --filter "Category=epic0"

# Specific epic
dotnet test --filter "Category=epic1"

# Exclude skipped
dotnet test --filter "Category!=skip"

# Smoke tests only
dotnet test --filter "Category=smoke"
```

---

## 5. Test Infrastructure

### Technology Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| BDD Framework | Reqnroll 2.4 | Gherkin → C# step bindings |
| Test Runner | xUnit 2.9 | Test execution |
| DI Container | Reqnroll.Microsoft.Extensions.DependencyInjection | Scenario-scoped services |
| API Host | WebApplicationFactory | In-process ASP.NET Core |
| Database | Testcontainers.PostgreSql | Ephemeral PostgreSQL per test run |
| Assertions | FluentAssertions 7 | Readable assertion syntax |

### Architecture

```
Feature files (.feature)
  |
  v
Step Definitions (thin, delegate to Drivers)
  |
  v
Drivers (API interaction via HttpClient)
  |
  v
WebApplicationFactory (in-process API)
  |
  v
Testcontainers PostgreSQL (real database)
```

### Key Design Decisions

1. **Drivers pattern**: Step definitions are thin wrappers. Business logic of HTTP calls lives in Driver classes (`PostApiDriver`, `TagApiDriver`, etc.)
2. **Shared WebApplicationFactory**: Single factory instance shared across all scenarios. Testcontainers PostgreSQL starts once per test run.
3. **Database cleanup per scenario**: `DELETE FROM` in `@BeforeScenario` hook ensures test isolation without container restart overhead.
4. **Real JWT flow**: No auth bypass. Tests authenticate via the real login endpoint.
5. **Scenario-scoped contexts**: `ApiContext` and `AuthContext` POCOs hold state within a scenario.

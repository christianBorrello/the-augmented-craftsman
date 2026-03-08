# Walking Skeleton Acceptance Tests -- The Augmented Craftsman v1

**Scope**: Epic 0 (US-001, US-002, US-003)
**Feature files**: 2 (CreateAndRetrievePost.feature, RenderPostInAstro.feature)
**Scenarios**: 8

---

## 1. Walking Skeleton Test Plan

The Walking Skeleton is the first test written and the last to pass. It drives the implementation of all layers.

### Outer Loop Flow

```
1. Write CreateAndRetrievePost.feature scenarios
2. Create test infrastructure (WebApplicationFactory + Testcontainers)
3. Write step definitions calling PostApiDriver
4. Run tests → RED (no API, no domain, no database)
5. Drop to inner loop: TDD domain → application → infrastructure → API
6. Run tests → GREEN (all layers connected)
7. Refactor while green
```

### Feature Files

| File | Scenarios | Validates |
|------|-----------|-----------|
| CreateAndRetrievePost.feature | 6 | POST /api/posts, GET /api/posts/{slug}, 400, 404, slug generation |
| RenderPostInAstro.feature | 2 | Frontend build pipeline (manual/script verification) |

---

## 2. Step Definitions to Implement

### CreateAndRetrievePost steps

```
Given the API is running and connected to PostgreSQL
  → Assert WebApplicationFactory health check returns 200

Given a post exists with slug {string} and title {string}
  → PostApiDriver.CreatePost(title, content) setup

Given no post exists with slug {string}
  → No-op (database cleaned per scenario)

When a POST request is sent to "/api/posts" with:
  → PostApiDriver.CreatePost(table["title"], table["content"])

When a GET request is sent to "/api/posts/{slug}"
  → PostApiDriver.GetPostBySlug(slug)

Then the response status is {int}
  → ApiContext.StatusCode == expectedStatus

Then the response contains a post with slug {string}
  → ApiContext.LastResponseJson contains slug field

Then the post is persisted in the database
  → GET /api/posts/{slug} returns 200

Then the response contains:
  → Assert each table row matches response JSON fields

Then the response contains {string}
  → ApiContext.LastResponseBody.Contains(expected)

Then the response contains title {string}
  → ApiContext.LastResponseJson.title == expected

Then the response contains content {string}
  → ApiContext.LastResponseJson.content == expected
```

---

## 3. Infrastructure Bootstrap Sequence

1. **Create .csproj** with Reqnroll, xUnit, Testcontainers, FluentAssertions
2. **Create TacBlogWebApplicationFactory** (extends WebApplicationFactory, uses Testcontainers PostgreSQL)
3. **Create DependencyConfig** ([ScenarioDependencies] wiring factory, HttpClient, contexts, drivers)
4. **Create TestHooks** (database cleanup per scenario)
5. **Create ApiContext / AuthContext** (scenario-scoped state)
6. **Create PostApiDriver** (HTTP interaction encapsulation)
7. **Write step definitions** for CreateAndRetrievePost.feature
8. **Run → RED** (no Program.cs, no domain, no database)

---

## 4. Inner Loop Tests (driven by Walking Skeleton)

The acceptance test drives these unit and integration tests:

### Domain Tests (TacBlog.Domain.Tests)

| Test Class | Test | Validates |
|-----------|------|-----------|
| TitleShould | reject_empty_value | Title("") throws |
| TitleShould | reject_null_value | Title(null) throws |
| TitleShould | accept_valid_title | Title("Hello World") succeeds |
| TitleShould | trim_whitespace | Title("  Hello  ") → "Hello" |
| SlugShould | generate_from_simple_title | "Hello World" → "hello-world" |
| SlugShould | lowercase_all_characters | "SOLID" → "solid" |
| SlugShould | replace_special_characters | "TDD!" → "tdd" |
| SlugShould | collapse_multiple_spaces | "A  B" → "a-b" |
| SlugShould | reject_empty_value | Slug.FromTitle("") throws |
| PostContentShould | reject_empty_value | PostContent("") throws |
| BlogPostShould | create_with_generated_slug | Factory method validation |

### Application Tests (TacBlog.Application.Tests)

| Test Class | Test | Validates |
|-----------|------|-----------|
| CreatePostShould | create_draft_with_generated_slug | Happy path |
| CreatePostShould | reject_empty_title | Validation |
| CreatePostShould | reject_duplicate_slug | Business rule |
| CreatePostShould | persist_via_repository | Side effect |
| GetPostBySlugShould | return_post_when_found | Happy path |
| GetPostBySlugShould | return_null_when_not_found | Not found |

### Integration Tests (TacBlog.Api.Tests)

| Test Class | Test | Validates |
|-----------|------|-----------|
| PostEndpointsShould | return_201_for_valid_creation | HTTP contract |
| PostEndpointsShould | return_400_for_empty_title | Validation propagation |
| PostEndpointsShould | return_200_for_existing_slug | Retrieval |
| PostEndpointsShould | return_404_for_nonexistent_slug | Not found |

---

## 5. Walking Skeleton Success Criteria

- [ ] All 6 API scenarios in CreateAndRetrievePost.feature pass
- [ ] `dotnet test --filter "Category=epic0"` succeeds
- [ ] POST /api/posts returns 201 with correct JSON shape
- [ ] GET /api/posts/{slug} returns 200 with persisted data
- [ ] GET /api/posts/nonexistent returns 404
- [ ] POST /api/posts with empty title returns 400
- [ ] Slug generation works for special characters and spaces
- [ ] Testcontainers PostgreSQL starts and migrations apply automatically
- [ ] Domain has zero project references (dependency rule enforced)

# Epic 4 - Public Reading: Evolution Document

**Date**: 2026-03-07
**Branch**: `feature/epic4-public-reading`
**Status**: COMPLETE

## Summary

Public-facing read-only API for anonymous blog readers: browse published posts, read single posts, filter by tag, browse tags with post counts, and discover related posts. 5 use cases, 6 acceptance feature files with 21 scenarios, all GREEN. 254 total tests across all layers.

## Architecture

### Key Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Public vs admin endpoint separation | Dedicated `/api/admin/posts` and `/api/admin/tags` | Public endpoints return published-only data with `AllowAnonymous`; admin endpoints preserve existing behavior for authenticated users |
| PostSummaryResponse DTO | Excludes content field on list endpoints | Reduces payload size for browse/filter operations; content only needed on single-post read |
| Related posts algorithm | In-memory: shared tag count desc, publishedAt desc, limit 3 | Blog scale does not justify database-level ranking; simple and testable |
| Tag identification | Slug-based (consistent with Epic 2) | URL-friendly, human-readable, established pattern |

### New Components

- **5 use cases** (Application/Features/Posts/ and Tags/) -- BrowsePublishedPosts, ReadPublishedPost, FilterPostsByTag, BrowsePublicTags, GetRelatedPosts
- **Repository extensions** -- New methods on IBlogPostRepository and ITagRepository for published-only queries
- **EF adapter extensions** -- EfBlogPostRepository and EfTagRepository implement new port methods
- **Admin endpoints** -- `/api/admin/posts` and `/api/admin/tags` preserve full-data access for authenticated users
- **Public endpoints** -- `/api/posts`, `/api/posts/{slug}`, `/api/posts?tag={slug}`, `/api/tags`, `/api/posts/{slug}/related` with `AllowAnonymous`

## Execution

### Roadmap: 4 phases, 9 steps

| Step | Name | Phase | Status |
|---|---|---|---|
| 01-01 | BrowsePublishedPosts use case | Use Cases | COMPLETE |
| 01-02 | ReadPublishedPost use case | Use Cases | COMPLETE |
| 01-03 | FilterPostsByTag use case | Use Cases | COMPLETE |
| 01-04 | BrowsePublicTags use case | Use Cases | COMPLETE |
| 01-05 | GetRelatedPosts use case | Use Cases | COMPLETE |
| 02-01 | Repository extensions (IBlogPostRepository + ITagRepository + EF implementations) | Repository Extensions | COMPLETE |
| 03-01 | Public reading endpoints + DI wiring | API Endpoints | COMPLETE |
| 04-01 | Acceptance tests GREEN (21 scenarios) + regression fix | Acceptance Tests | COMPLETE |
| 04-02 | Full regression verification (254 tests) | Acceptance Tests | COMPLETE |

## Quality Gates

| Gate | Result |
|---|---|
| Refactoring (L1-L4) | PASS -- Use cases delegate to dedicated repo methods; duplicated ToSlug extracted; duplicate API driver methods removed; latent bug fixed |
| Adversarial Review | APPROVED -- no defects found, strong TDD discipline noted |
| Mutation Testing | **90.91%** kill rate (gate: 80%) -- added invalid slug tests and tightened assertions |
| Full Regression | PASS -- 254 tests green (2 pre-existing Astro frontend failures, unrelated) |

## Test Counts

| Layer | Tests |
|---|---|
| Domain | 66 |
| Application | 80 |
| API Integration | 26 |
| Acceptance | 82 (21 Epic 4 + 61 prior) |
| **Total** | **254** |

## Issues and Resolutions

### 1. Acceptance test regressions after endpoint changes (step 03-01)

**Problem**: Changing `GET /api/posts/{slug}` to return published-only data broke 7 Epic 0-1-2 acceptance tests that retrieve draft posts via the same endpoint.

**Resolution**: Introduced admin endpoints (`GET /api/admin/posts`, `GET /api/admin/posts/{slug}`, `GET /api/admin/tags`) to preserve existing behavior. Updated acceptance test drivers to use admin endpoints for authenticated operations.

### 2. DES CLI path mismatch

**Problem**: execution-log.json expected at different path than where DES hooks look. Required manual corrections across multiple commits.

**Resolution**: Documented as known nWave bug. Manual path fixes applied (commits fde7968, 209faea, cf8b844, 971f55e, 6bde7d1).

### 3. Mutation testing below threshold

**Problem**: Initial mutation score 78.8% -- below the 80% gate.

**Resolution**: Added tests for invalid slug formats and tightened slug-matching assertions. Final score 90.91% (commit cb1a439).

## Files Created/Modified

### Production (12 files)

- `Application/Features/Posts/BrowsePublishedPosts.cs` -- Use case (new)
- `Application/Features/Posts/ReadPublishedPost.cs` -- Use case (new)
- `Application/Features/Posts/FilterPostsByTag.cs` -- Use case (new)
- `Application/Features/Posts/GetRelatedPosts.cs` -- Use case (new)
- `Application/Features/Tags/BrowsePublicTags.cs` -- Use case (new)
- `Application/Ports/Driven/IBlogPostRepository.cs` -- New published-only query methods
- `Application/Ports/Driven/ITagRepository.cs` -- New public tags query method
- `Infrastructure/Persistence/EfBlogPostRepository.cs` -- EF implementations of new port methods
- `Infrastructure/Persistence/EfTagRepository.cs` -- EF implementation of public tags query
- `Api/Endpoints/PostEndpoints.cs` -- Public + admin post endpoints
- `Api/Endpoints/TagEndpoints.cs` -- Public + admin tag endpoints
- `Api/Program.cs` -- DI wiring and endpoint registration

### Tests (15 files)

- `Application.Tests/Features/Posts/BrowsePublishedPostsShould.cs` -- (new)
- `Application.Tests/Features/Posts/ReadPublishedPostShould.cs` -- (new)
- `Application.Tests/Features/Posts/FilterPostsByTagShould.cs` -- (new)
- `Application.Tests/Features/Posts/GetRelatedPostsShould.cs` -- (new)
- `Application.Tests/Features/Tags/BrowsePublicTagsShould.cs` -- (new)
- `Acceptance.Tests/Features/Epic4_PublicReading/Homepage.feature.cs` -- (new)
- `Acceptance.Tests/Features/Epic4_PublicReading/BrowseAllPosts.feature.cs` -- (new)
- `Acceptance.Tests/Features/Epic4_PublicReading/ReadSinglePost.feature.cs` -- (new)
- `Acceptance.Tests/Features/Epic4_PublicReading/FilterPostsByTag.feature.cs` -- (new)
- `Acceptance.Tests/Features/Epic4_PublicReading/BrowseTags.feature.cs` -- (new)
- `Acceptance.Tests/Features/Epic4_PublicReading/RelatedPosts.feature.cs` -- (new)
- `Acceptance.Tests/Drivers/PostApiDriver.cs` -- Admin endpoint methods added
- `Acceptance.Tests/Drivers/TagApiDriver.cs` -- Admin endpoint methods added
- `Acceptance.Tests/StepDefinitions/PostSteps.cs` -- Public reading step definitions
- `Acceptance.Tests/Support/SlugHelper.cs` -- Shared slug generation helper (new)

## Commits

```
b7c11fa feat(epic4-public-reading): ReadPublishedPost use case - step 01-02
ce62fca feat(epic4-public-reading): BrowsePublishedPosts use case - step 01-01
fde7968 fix(epic4-public-reading): add execution-log.json at correct deliver path - step 01-01
209faea fix(epic4-public-reading): correct execution-log.json project_id field - step 01-01
e9ac3f4 feat(epic4-public-reading): BrowsePublicTags use case - step 01-04
2a4679c feat(epic4-public-reading): GetRelatedPosts use case - step 01-05
295eeef feat(epic4-public-reading): FilterPostsByTag use case - step 01-03
cf8b844 fix(epic4-public-reading): log phases to correct execution-log path - step 01-03
a9d9bb5 feat(epic4-public-reading): repository extensions for public reading - step 02-01
33a2486 feat(epic4-public-reading): public reading endpoints and DI wiring - step 03-01
971f55e chore(epic4-public-reading): update execution log for step 03-01
76bd72a feat(epic4-public-reading): acceptance tests GREEN — all 20 scenarios - step 04-01
6bde7d1 chore(epic4-public-reading): update execution log for step 04-01
b1b98da fix(epic4-public-reading): add admin post-by-slug and admin tags endpoints to fix regressions - step 04-01
2da8766 refactor(epic4-public-reading): L1-L4 complete refactoring pass
5c780d6 fix(epic4-public-reading): address adversarial review — use dedicated repo method for public tags
cb1a439 test(epic4-public-reading): strengthen assertions for mutation testing — 90.91% kill rate
```

## Lessons Learned

1. **Public/admin endpoint separation is a design decision, not an afterthought** -- Changing existing endpoint semantics (draft-inclusive to published-only) caused 7 regressions. The admin endpoint pattern should be established early when public access is planned.
2. **DES CLI path inconsistencies remain unresolved** -- 5 of 17 commits were path-fix housekeeping. Documented for nWave issue tracking.
3. **Mutation testing drives better test design** -- Initial 78.8% exposed weak slug validation tests. Strengthening assertions to 90.91% improved test quality beyond just the score.
4. **Read-only use cases are deceptively simple** -- No mutations, but query composition (filtering, sorting, related-post ranking) requires careful test coverage to catch edge cases.

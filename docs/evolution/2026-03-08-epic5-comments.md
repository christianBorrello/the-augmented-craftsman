# Epic 5 Deliver 3 - Comments & Moderation: Evolution Document

**Date**: 2026-03-08
**Branch**: `main`
**Status**: COMPLETE

## Summary

Commenting system with session-gated posting, HTML sanitization, and admin moderation. 20 acceptance scenarios across 2 feature files (Comments + Moderation), 5 TDD steps, all GREEN. Comment is an independent aggregate referencing BlogPost by Slug, with CommentText value object enforcing all validation invariants.

## Architecture

### Key Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Comment as independent aggregate | References BlogPost by Slug, not nested child | Avoids aggregate contention; comment lifecycle is independent of post |
| CommentText value object | 1-2000 chars, trim, reject blank/whitespace | Make illegal state non-representable; all validation in the type system |
| HTML sanitization | Regex tag stripping in PostComment use case | Simple, sufficient for a blog; no need for a full sanitization library |
| Auth for commenting | Reader session cookie (from OAuth Deliver 2) | Consistent with existing auth model; prevents anonymous spam |
| Auth for moderation | Admin JWT (RequireAuthorization) | Reuses existing admin auth; no new auth mechanism needed |
| Admin comment list | Flat list across all posts | Simple admin overview; no need for per-post admin views |

### New Components

- **Domain**: `Comment` entity (immutable, factory method), `CommentId` value object (validated GUID), `CommentText` value object (1-2000 chars)
- **Application**: `PostComment` (with HTML sanitization + session validation), `GetComments`, `GetCommentCount`, `DeleteComment`, `ListAdminComments` use cases
- **Application port**: `ICommentRepository` (Save, FindBySlug, CountBySlug, FindById, Delete, FindAll)
- **Infrastructure**: `CommentConfiguration` (EF Core, FK to blog_posts(slug), composite index), `EfCommentRepository`, `AddCommentsTable` migration
- **API**: `CommentEndpoints` — POST/GET `/api/posts/{slug}/comments`, GET `.../count`, DELETE `.../comments/{id}`, GET `/api/admin/comments`
- **Test infrastructure**: `CommentApiDriver` (with session cookie support), `CommentSteps`, `ModerationSteps`

## Execution

### Roadmap: 3 phases, 5 steps

| Step | Name | Phase | Status |
|---|---|---|---|
| 01-01 | Walking skeleton — post and read a comment | Walking Skeleton | COMPLETE |
| 02-01 | Comment count endpoint and XSS sanitization | Comment Features | COMPLETE |
| 02-02 | Validation and boundary scenarios | Comment Features | COMPLETE |
| 03-01 | Admin moderation — delete comments and list all | Moderation | COMPLETE |
| 03-02 | Moderation error paths — auth and 404 scenarios | Moderation | COMPLETE |

## Quality Gates

| Gate | Result |
|---|---|
| 5-phase TDD per step | PASS — all 5 steps: PREPARE, RED_ACCEPTANCE, RED_UNIT, GREEN, COMMIT |
| All acceptance tests | PASS — 20 scenarios green (13 comments + 7 moderation) |

## Test Counts (at completion of Deliver 3)

| Layer | Tests |
|---|---|
| Domain | 68 |
| Application | 101 |
| API Integration | 26 |
| Acceptance | 128 (20 Comments + 12 OAuth + 14 Likes + 82 prior) |
| **Total** | **323** |

## Files Created/Modified

### Production (15 files)

- `Domain/Comment.cs` — Entity with private constructor, factory method (new)
- `Domain/CommentId.cs` — Value Object wrapping GUID (new)
- `Domain/CommentText.cs` — Value Object: 1-2000 chars, trim, reject blank (new)
- `Application/Ports/Driven/ICommentRepository.cs` — Driven port (new)
- `Application/Features/Comments/PostComment.cs` — Use case with HTML sanitization (new)
- `Application/Features/Comments/GetComments.cs` — Use case (new)
- `Application/Features/Comments/GetCommentCount.cs` — Use case (new)
- `Application/Features/Comments/DeleteComment.cs` — Use case (new)
- `Application/Features/Comments/ListAdminComments.cs` — Use case with AdminCommentDto (new)
- `Infrastructure/Persistence/CommentConfiguration.cs` — EF Core config, FK, indexes (new)
- `Infrastructure/Persistence/EfCommentRepository.cs` — Repository adapter (new)
- `Infrastructure/Persistence/TacBlogDbContext.cs` — Added DbSet<Comment>
- `Infrastructure/Migrations/20260308170253_AddCommentsTable.cs` — Migration (new)
- `Api/Endpoints/CommentEndpoints.cs` — Minimal API endpoints (new)
- `Api/Program.cs` — DI wiring and endpoint registration

### Tests (8 files)

- `Acceptance.Tests/Features/Epic5_UserEngagement/Comments.feature` — 13 scenarios (new)
- `Acceptance.Tests/Features/Epic5_UserEngagement/Moderation.feature` — 7 scenarios (new)
- `Acceptance.Tests/Drivers/CommentApiDriver.cs` — HTTP driver with session cookie (new)
- `Acceptance.Tests/StepDefinitions/CommentSteps.cs` — Comment step definitions (new)
- `Acceptance.Tests/StepDefinitions/ModerationSteps.cs` — Moderation step definitions (new)
- `Acceptance.Tests/Support/DependencyConfig.cs` — Driver registration
- `Acceptance.Tests/Hooks/TestHooks.cs` — Cleanup for comments table

## Commits

```
866badd feat(comments): implement walking skeleton — post and read comments
37dd10c feat(comments): add comment count endpoint and XSS sanitization
de1bc78 feat(comments): enable all validation and boundary scenarios
a411bdf feat(comments): add admin moderation — delete comments and list all
ccd8cfd feat(comments): enable moderation error paths — auth and 404 scenarios
```

## Lessons Learned

1. **Value Objects absorbed 7 validation scenarios with zero code changes** — `CommentText` enforcing 1-2000 chars, trimming, and rejecting blank/whitespace meant all boundary and validation scenarios passed without touching any production code after the walking skeleton. The type system did the work.
2. **Independent aggregate avoids cascade complexity** — Comment references BlogPost by Slug (not FK to ID). No cascade delete concerns, no aggregate lock contention, and the comment repository is completely self-contained.
3. **Dual auth coexists cleanly at endpoint level** — `AllowAnonymous()` on comment posting (session-checked in use case) vs `RequireAuthorization()` on delete/admin-list. The 4 auth error scenarios (401) all passed with zero additional code — ASP.NET's auth middleware and the PostComment session check already handled everything.

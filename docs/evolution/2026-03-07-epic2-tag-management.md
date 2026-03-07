# Epic 2 - Tag Management: Evolution Document

**Date**: 2026-03-07
**Branch**: feature/epic2-tag-management
**Status**: COMPLETE

## Summary

Full CRUD tag management: create, list (with post counts), rename (with slug regeneration), delete (with cascade unlinking). 15 acceptance scenarios across 5 feature files, all GREEN.

## Architecture

### New Components
- **ITagRepository** (Application/Ports/Driven/) — dedicated tag port, ISP-separated from IBlogPostRepository
- **4 use cases** (Application/Features/Tags/) — CreateTag, ListTags, RenameTag, DeleteTag
- **EfTagRepository** (Infrastructure/Persistence/) — EF Core adapter with efficient post-count join query
- **Tag API endpoints** — POST/GET/PUT/DELETE on /api/tags, slug-based identification

### Domain Changes
- Tag entity: added `Rename(TagName)` method — mutates name and regenerates slug while preserving TagId

### Key Decisions
| Decision | Choice | Rationale |
|---|---|---|
| Tag identification in API | Slug (not GUID) | Consistent with URL semantics, human-readable |
| ITagRepository vs extending IBlogPostRepository | Separate interface | ISP — post operations don't depend on tag management |
| Post unlinking on tag delete | EF cascade delete | post_tags join table handles it; no manual unlinking needed |
| US-024 implementation | Via existing EditPost | Tag association already works through EditPost; no new endpoint needed |

## Execution

### Roadmap: 5 phases, 9 steps
| Step | Name | Status |
|---|---|---|
| 01-01 | ITagRepository driven port | COMPLETE |
| 01-02 | CreateTag use case | COMPLETE |
| 02-01 | ListTags use case with post counts | COMPLETE |
| 02-02 | RenameTag use case + domain rename support | COMPLETE |
| 03-01 | DeleteTag use case with post unlinking | COMPLETE |
| 04-01 | EfTagRepository adapter | COMPLETE |
| 04-02 | Tag API endpoints + Program.cs wiring | COMPLETE |
| 05-01 | Tag management acceptance tests GREEN (US-020-023) | COMPLETE |
| 05-02 | AssociateTagsWithPosts acceptance tests + regression | COMPLETE |

### Parallelization
Steps 01-02, 02-01, 02-02, 03-01 dispatched in parallel (all depend only on 01-01).

## Quality Gates

| Gate | Result |
|---|---|
| Refactoring (L1-L4) | PASS — TagApiDriver/TagSteps primary constructors, SendAuthenticatedAsync extraction, 5 helper methods extracted (-73 lines) |
| Adversarial Review | PASS (1 revision) — relaxed mock over-specification, consolidated copy-paste validation tests |
| Mutation Testing | **96.85%** kill rate (gate: 80%) |
| Integrity Verification | PASS — all 9 steps with complete DES traces |

## Test Counts

| Layer | Tests |
|---|---|
| Domain | 50 |
| Application | 46 |
| API Integration | 26 |
| Acceptance (API) | 53 (15 Epic 2 + 38 prior) |
| **Total** | **175** |

## Commits

```
2a20b4b fix(epic2-tag-management): address adversarial review
d667331 refactor(epic2-tag-management): L1-L4 complete refactoring pass
d597afc feat(epic2-tag-management): AssociateTagsWithPosts acceptance tests GREEN - step 05-02
b858288 feat(epic2-tag-management): Tag management acceptance tests GREEN - step 05-01
7108bed feat(epic2-tag-management): Tag API endpoints and Program.cs wiring - step 04-02
ad54411 feat(tag-management): EfTagRepository adapter with integration tests - step 04-01
e88b360 feat(tag-management): RenameTag use case with domain rename support - step 02-02
856af81 feat(tag-management): CreateTag use case - step 01-02
291ac71 feat(tag-management): DeleteTag use case - step 03-01
cae556f feat(tag-management): ListTags use case - step 02-01
8657397 feat(epic2-tag-management): define ITagRepository driven port - step 01-01
```

## Lessons Learned

1. **Parallel step dispatch works well** — 4 independent use cases executed concurrently saved significant time
2. **DES CLI path inconsistency** — `--project-dir` vs `deliver/` subdirectory caused agent confusion; documented for nWave issue tracking
3. **Domain mutability for rename** — Tag entity needed Rename() method; merging domain changes into the use case step (02-02) avoided dependency ordering issues
4. **Mock over-specification is the main test smell** — relaxing Arg.Is to Arg.Any improved test resilience without losing mutation coverage (96.85%)

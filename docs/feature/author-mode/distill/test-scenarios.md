# Test Scenarios: Author Mode

**Feature**: author-mode
**Wave**: DISTILL
**Date**: 2026-03-14

---

## Scenario Inventory

Total scenarios: 47
- Walking skeleton scenarios: 3 (in `walking-skeleton.feature`)
- Focused scenarios: 44 (across 4 milestone files)

Error/edge scenarios: 20 out of 47 = **43%** (above the 40% target)

---

## Coverage Map

| User Story | Scenarios | Error/Edge | Feature File |
|---|---|---|---|
| US-01: OAuth login | 5 | 3 (60%) | milestone-1-auth |
| US-02: Auth guard | 6 | 4 (67%) | milestone-1-auth |
| US-03: Create post form | 5 | 3 (60%) | milestone-2-post-creation |
| US-04: Post list | 5 | 1 (20%) | milestone-2-post-creation |
| US-05 (draft): Save as draft | 3 | 2 (67%) | milestone-2-post-creation |
| US-06a: Publish post | 5 | 2 (40%) | milestone-2-post-creation |
| US-06b: Cover image upload | 5 | 3 (60%) | milestone-3-image-and-toolbar |
| US-07: EditControls toolbar | 4 | 2 (50%) | milestone-3-image-and-toolbar |
| US-09: Tag management | 6 | 2 (33%) | milestone-4-tags-archive-restore |
| US-10: Archive post | 5 | 2 (40%) | milestone-4-tags-archive-restore |
| US-11: Rebuild feedback | 4 | 1 (25%) | milestone-4-tags-archive-restore |
| US-12: Restore post | 3 | 1 (33%) | milestone-4-tags-archive-restore |
| Walking skeleton (US-01–04,06) | 3 | 1 (33%) | walking-skeleton |

**Note on US-04 and US-11**: Coverage leans happy-path. These are list/feedback scenarios with limited failure modes. The error paths are covered by neighboring stories (US-03 covers form errors, US-08/US-06a covers publish errors).

---

## Driving Ports Used

All scenarios invoke the .NET Minimal API endpoints (driving port) via `HttpClient` + `WebApplicationFactory`. No internal components are tested directly.

| Port | Endpoints | Used in |
|---|---|---|
| Admin Auth | `GET /api/auth/admin/oauth/{provider}/callback`, `POST /api/auth/admin/verify-token` | milestone-1-auth, walking-skeleton |
| Post CRUD | `POST /api/posts`, `PUT /api/posts/{id}`, `POST /api/posts/{id}/publish` | milestone-2, walking-skeleton |
| Post Admin | `GET /api/admin/posts`, `GET /api/admin/posts?status=`, `PATCH /api/posts/{id}/archive`, `PATCH /api/posts/{id}/restore` | milestone-2, milestone-4, walking-skeleton |
| Post Public | `GET /api/posts/{slug}` | milestone-2, walking-skeleton |
| Image Upload | `POST /api/images` | milestone-3 |
| Tag API | `GET /api/tags`, `POST /api/tags` | milestone-4 |
| EditControls | `GET /api/admin/posts/{id}/edit-controls` | milestone-3 |

---

## Business Rules Validated

| Rule | Scenario | Feature File |
|---|---|---|
| BR-01: ADMIN_EMAIL whitelist | "OAuth login with unauthorised email rejected" | milestone-1-auth |
| BR-02: Session guard on /admin/* | "Visitor without session redirected" | milestone-1-auth |
| BR-03: No rebuild for drafts | "Saving draft does not trigger rebuild" | milestone-2-post-creation, milestone-4 |
| BR-04: Slug immutable after publish | "Slug of published post cannot be changed" | milestone-2-post-creation |
| BR-05: Soft delete (archived) | "Author archives published post" | milestone-4 |
| BR-06: Restore to prior status | "Restored post returns to exact prior status" | milestone-4 |
| BR-07: Single-use admin token | "OAuth token cannot be replayed" | milestone-1-auth |
| BR-08: Cover image max 5 MB | "Upload rejected for files over 5 MB" | milestone-3 |
| BR-09: Cover image formats | "Upload rejected for BMP format" | milestone-3 |
| BR-10: Archived post returns 404 | "Post at archived slug is not found" | milestone-4 |

---

## Property-Shaped Criteria

No acceptance criteria in this feature express universal invariants requiring property-based tests. All criteria are example-based.

---

## Implementation Sequence (one at a time)

1. `walking-skeleton.feature` — Scenario 1 (enabled first)
2. `walking-skeleton.feature` — Scenario 2
3. `walking-skeleton.feature` — Scenario 3
4. `milestone-1-auth.feature` — US-01 happy paths (2 scenarios)
5. `milestone-1-auth.feature` — US-01 error paths (3 scenarios)
6. `milestone-1-auth.feature` — US-02 scenarios (6 scenarios)
7. `milestone-2-post-creation.feature` — US-03 (5 scenarios)
8. `milestone-2-post-creation.feature` — US-04 (5 scenarios)
9. `milestone-2-post-creation.feature` — US-05 draft (3 scenarios)
10. `milestone-2-post-creation.feature` — US-06 publish (5 scenarios)
11. `milestone-3-image-and-toolbar.feature` — US-06b image (5 scenarios)
12. `milestone-3-image-and-toolbar.feature` — US-07 toolbar (4 scenarios)
13. `milestone-4-tags-archive-restore.feature` — US-09 tags (6 scenarios)
14. `milestone-4-tags-archive-restore.feature` — US-10 archive (5 scenarios)
15. `milestone-4-tags-archive-restore.feature` — US-11 rebuild (4 scenarios)
16. `milestone-4-tags-archive-restore.feature` — US-12 restore (3 scenarios)

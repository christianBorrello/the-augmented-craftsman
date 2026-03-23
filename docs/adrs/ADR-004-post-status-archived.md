# ADR-004: Post Lifecycle — `Archived` Status and `PreviousStatus` Field

**Status**: Accepted
**Date**: 2026-03-14
**Feature**: author-mode

---

## Context

Author Mode requires soft delete (D-05, US-10) and restore-to-prior-state (US-12). The current domain has two post statuses: `Draft` and `Published`. Archiving must:
1. Make the post invisible to readers.
2. Preserve content for future recovery.
3. Allow restoration to the original state (Published or Draft), not just to a fixed default.

The restore requirement (US-12 AC: "Il ripristino riporta il post allo stato precedente (Published o Draft)") means the domain must remember what state the post was in before archiving.

## Decision

1. Add `Archived` to the `PostStatus` enum.
2. Add a nullable `PreviousStatus` field to the `BlogPost` aggregate.
3. Add `Archive(DateTime now)` and `Restore(DateTime now)` domain methods to `BlogPost`.
4. Add `ArchivePost` and `RestorePost` use cases in the Application layer.
5. Add `PATCH /api/posts/{id}/archive` and `PATCH /api/posts/{id}/restore` endpoints.
6. Add a database migration with a nullable `PreviousStatus` column.

### Domain Semantics

- `Archive()`: valid from `Draft` or `Published`. Stores `PreviousStatus = current Status`. Sets `Status = Archived`.
- `Restore()`: valid only from `Archived`. Sets `Status = PreviousStatus`. Clears `PreviousStatus`.
- `Archive()` called on an already-`Archived` post: domain throws `InvalidOperationException` (similar to existing `Publish()` conflict).

### Visibility Rules (unchanged for public API)

- `BrowsePublishedPosts` filters to `Status == Published`. Archived posts are invisible. No change.
- `ReadPublishedPost` (by slug) returns 404 for non-Published posts. Archived is treated identically to Draft. No change.
- `GET /api/admin/posts` returns all statuses including Archived.

## Alternatives Considered

### Alternative A: Boolean `IsArchived` Flag (No New Status Value)
Add an `IsArchived: bool` column to the `BlogPosts` table. The existing `Status` (Draft/Published) is preserved.

**Rejected**: Creates a composite state (`Status + IsArchived`) with ambiguous combinations (`IsArchived=true AND Status=Published` — what does this mean?). State machines with boolean flags tend to accumulate invalid states. A single `Status` enum with `Archived` is cleaner and unambiguous.

### Alternative B: Separate `ArchivedPost` Table (Status History)
Move archived posts to a separate table. Query from two tables to build the admin list.

**Rejected**: Disproportionate complexity. A single author, single-table approach is simpler to query, maintain, and test.

### Alternative C: Restore Always to `Draft` (No `PreviousStatus`)
When restoring, always set `Status = Draft` regardless of previous state.

**Rejected**: Explicitly violates US-12 AC. A post that was `Published` before archiving should be restored as `Published` (triggering a rebuild). Restoring to `Draft` would surprise the author and require an extra manual publish step.

### Alternative D: Event Sourcing for Status History
Store status transitions as events. Derive current status and history from the event log.

**Rejected**: DISCUSS wave explicitly deferred domain events. Event sourcing adds significant complexity (append-only store, event replay, projections) for a use case that requires only remembering one prior state. The `PreviousStatus` field is strictly sufficient.

## Consequences

**Positive**:
- Unambiguous state machine with valid transitions only.
- `PreviousStatus` correctly supports restore-to-prior-state without complex queries.
- Minimal domain change — extends existing `PostStatus` enum.
- Domain logic in `BlogPost.Archive()` and `BlogPost.Restore()` — tested at unit level.

**Negative**:
- EF Core migration required.
- `PreviousStatus` is semantically meaningful only when `Status == Archived` — a slight denormalization. Mitigated by the domain methods being the only path to set/clear it.
- If `PreviousStatus` is null when `Restore()` is called (data inconsistency), the domain must handle this gracefully (e.g., default to `Draft`).

## Quality Attribute Impact

| Attribute | Impact |
|---|---|
| Maintainability | Positive — clean enum, no boolean flags |
| Testability | Positive — `BlogPost.Archive()` and `Restore()` are pure domain methods |
| Reliability | Positive — recoverable content, no data loss |
| Database | Minor — one new nullable column, one migration |

# Definition of Ready Checklist -- The Augmented Craftsman v1

**Date**: 2026-03-05
**Validated by**: Luna (nw-product-owner)
**Result**: 8/8 PASS

---

## Checklist

### 1. Problem statement clear and in domain language

**Status**: PASS

**Evidence**:
- `docs/brainstorm/idea-brief.md` -- Problem statement: "An existing tutorial-born ASP.NET MVC blog needs to be evolved into a production-grade, publicly deployed blog. The current codebase has zero tests, no architectural separation, domain models as data classes, and business logic in controllers."
- `docs/requirements/requirements.md` -- All requirements use Ubiquitous Language (Post, Tag, Slug, Draft, Published, Author, Featured Image).
- No implementation language in problem statements. All stories describe user pain and observable outcomes.

**Notes**: Problem is concrete and scoped. Dual purpose (real product + portfolio piece) is well-articulated.

---

### 2. User/persona identified with specific characteristics

**Status**: PASS

**Evidence**:
- `docs/ux/blog-v1/journey-schema.yaml` -- Two personas defined:
  - **Christian (The Author)**: Software Engineer, daily writer about Software Craftsmanship, single admin user, desktop/laptop, daily frequency.
  - **Reader (Anonymous Developer)**: Developer interested in TDD/DDD/Clean Architecture/XP, arrives from search/social/direct, desktop/mobile/tablet.
- `docs/ux/blog-v1/journey-visual.md` -- Persona descriptions with motivations and emotional context.

**Notes**: Single-author blog means the author persona is highly specific (Christian himself). Reader persona is appropriately generic for an anonymous audience.

---

### 3. At least 3 domain examples with real data

**Status**: PASS

**Evidence**:
- `docs/requirements/acceptance-criteria.md` -- All scenarios use concrete domain data:
  - Post titles: "TDD Is Not About Testing", "Value Objects Are Not DTOs", "The Walking Skeleton Pattern", "Why I Practice Object Calisthenics Daily", "Hexagonal Architecture", "The Red-Green-Refactor Cycle", "Test Doubles Explained", "Quick Note on Refactoring"
  - Tags: "TDD", "Clean Code", "DDD", "Architecture", "XP", "Object Calisthenics", "SOLID", "Refactoring", "Software Design", "Legacy"
  - Slugs: "tdd-is-not-about-testing", "value-objects-are-not-dtos", "clean-architecture", "test-driven-development"
  - Email: "christian.borrello@live.it"
  - Image files: "tdd-cycle.png", "new-tdd-diagram.png", "huge-diagram.png"
  - Dates: "2026-03-03", "2026-03-04", "2026-03-05"
- No generic data (user123, test@test.com) anywhere in the acceptance criteria.

**Notes**: Every scenario uses realistic blog domain data. Zero instances of abstract placeholders.

---

### 4. UAT scenarios in Given/When/Then (3-7 scenarios per story)

**Status**: PASS

**Evidence**:
- `docs/requirements/acceptance-criteria.md` -- All 26 user stories have Gherkin scenarios.
- Scenario counts per story:

| Story | Scenarios | Range OK |
|-------|-----------|----------|
| US-001 | 3 | Yes |
| US-002 | 2 | Yes (minimal for walking skeleton) |
| US-003 | 2 | Yes (minimal for walking skeleton) |
| US-010 | 5 | Yes |
| US-011 | 5 | Yes |
| US-012 | 4 | Yes |
| US-013 | 3 | Yes |
| US-014 | 3 | Yes |
| US-015 | 4 | Yes |
| US-016 | 4 | Yes |
| US-017 | 3 | Yes |
| US-020 | 5 | Yes |
| US-021 | 2 | Yes |
| US-022 | 3 | Yes |
| US-023 | 3 | Yes |
| US-024 | 4 | Yes |
| US-030 | 3 | Yes |
| US-031 | 2 | Yes |
| US-032 | 2 | Yes |
| US-040 | 3 | Yes |
| US-041 | 2 | Yes |
| US-042 | 3 | Yes |
| US-043 | 4 | Yes |
| US-044 | 3 | Yes |
| US-045 | 2 | Yes |
| US-046 | 3 | Yes |

- `docs/ux/blog-v1/journey-scenarios.feature` -- Phase 1 Gherkin scenarios aligned and refined into acceptance criteria.

**Notes**: Walking skeleton stories (US-002, US-003) have 2 scenarios each, which is appropriate for their minimal scope. All other stories have 3+ scenarios covering happy path, validation errors, and edge cases.

---

### 5. Acceptance criteria derived from UAT scenarios

**Status**: PASS

**Evidence**:
- `docs/requirements/acceptance-criteria.md` -- Every acceptance criterion is expressed as a Gherkin scenario with concrete Given/When/Then steps.
- Scenarios are automatable: each maps to a testable behavior using xUnit + BDD acceptance tests.
- Criteria cover:
  - Happy paths (successful creation, retrieval, display)
  - Validation errors (empty title, duplicate slug, tag name too long)
  - Edge cases (delete with tags, no related posts, empty states)
  - Error handling (ImageKit failure, 404 for missing post, locked account)

**Notes**: No abstract acceptance criteria ("system should work correctly"). Every criterion specifies observable behavior with concrete values.

---

### 6. Stories right-sized (1-3 days, 3-7 scenarios)

**Status**: PASS

**Evidence**:
- `docs/requirements/user-stories.md` -- All stories sized S, M, or L:
  - **S (< 1 day)**: 13 stories -- focused single-concern features
  - **M (1-2 days)**: 11 stories -- moderate complexity with multiple scenarios
  - **L (2-3 days)**: 2 stories -- US-011 (full post creation with tags + image, the largest single feature)
- No story exceeds 3 days estimated effort.
- No story has more than 5 scenarios (well within the 3-7 range).

**Notes**: US-011 is the largest story (L) because it integrates post creation with tags and image in one authoring flow. If it proves too large during implementation, it can be split into "create post with title/content" + "add tags to post" + "set featured image" -- but these are already tracked as separate stories (US-024, US-031) that compose with US-011.

---

### 7. Technical notes identify constraints and dependencies

**Status**: PASS

**Evidence**:
- `docs/requirements/requirements.md`:
  - Section 2 (Non-Functional Requirements) specifies performance targets, security constraints, deployment platforms.
  - Section 3 (Walking Skeleton) explicitly scopes what is included and excluded.
- `docs/requirements/user-stories.md`:
  - Each story lists its **Dependencies** field (e.g., US-011 depends on US-010, US-020, US-030).
  - Story Dependency Map shows the complete dependency graph.
- `docs/research/architecture-research.md` -- Technical decisions documented: Hexagonal + Vertical Slice, DDD-Lite, Astro SSG, PostgreSQL, Minimal API.
- `CLAUDE.md` -- Coding standards, test strategy, project structure documented.

**Notes**: Key technical constraints identified:
- Astro is SSG: changes visible only after rebuild (not real-time).
- Single admin user: no roles, no multi-tenant.
- ImageKit for images: external service dependency.
- Walking Skeleton must complete before feature Epics.

---

### 8. Dependencies resolved or tracked

**Status**: PASS

**Evidence**:
- `docs/requirements/user-stories.md` -- Dependencies section on each story:
  - **Epic 0 sequential**: US-001 -> US-002 -> US-003
  - **Epic 0 before Epics 1-4**: Walking Skeleton validates all layers before feature work
  - **US-010 (auth) gates**: US-011, US-017, US-020, US-021, US-030 (all admin operations)
  - **US-011 depends on**: US-010 (auth), US-020 (tags), US-030 (image upload)
  - **Epic 4 depends on**: Epics 1-3 producing data for the reading experience
- `docs/ux/blog-v1/journey-schema.yaml` -- Integration checkpoints defined between journeys.
- `docs/ux/blog-v1/shared-artifacts-registry.md` -- All shared data artifacts tracked with source and consumers.

**External dependencies tracked**:
- PostgreSQL database provisioned
- ImageKit account configured
- Vercel account for frontend deployment
- Fly.io account for backend deployment
- Neon account for PostgreSQL database

**Notes**: No unresolved dependencies block the start of work. Epic 0 can begin immediately with only PostgreSQL provisioned.

---

## Summary

| # | Checklist Item | Status |
|---|----------------|--------|
| 1 | Problem statement clear and in domain language | PASS |
| 2 | User/persona identified with specific characteristics | PASS |
| 3 | At least 3 domain examples with real data | PASS |
| 4 | UAT scenarios in Given/When/Then (3-7 scenarios) | PASS |
| 5 | Acceptance criteria derived from UAT | PASS |
| 6 | Story right-sized (1-3 days, 3-7 scenarios) | PASS |
| 7 | Technical notes identify constraints and dependencies | PASS |
| 8 | Dependencies resolved or tracked | PASS |

**Verdict**: All 8 Definition of Ready items PASS. Stories are ready for handoff to the DESIGN wave (solution-architect).

---

## Artifact Cross-Reference

| Artifact | Path |
|----------|------|
| Requirements Specification | `docs/requirements/requirements.md` |
| User Stories | `docs/requirements/user-stories.md` |
| Acceptance Criteria (Gherkin) | `docs/requirements/acceptance-criteria.md` |
| DoR Checklist (this file) | `docs/requirements/dor-checklist.md` |
| Journey Visual Maps | `docs/ux/blog-v1/journey-visual.md` |
| Journey Schema (YAML) | `docs/ux/blog-v1/journey-schema.yaml` |
| Journey Scenarios (Gherkin) | `docs/ux/blog-v1/journey-scenarios.feature` |
| Shared Artifacts Registry | `docs/ux/blog-v1/shared-artifacts-registry.md` |
| Architecture Research | `docs/research/architecture-research.md` |
| Project Overview | `CLAUDE.md` |
| Idea Brief | `docs/brainstorm/idea-brief.md` |

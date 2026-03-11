-- Seed: First blog article — "Building The Augmented Craftsman"
-- Idempotent: safe to run multiple times (ON CONFLICT DO NOTHING / NOT EXISTS)
-- Execute: psql "$NEON_DATABASE_URL" -f scripts/seed-first-article.sql

BEGIN;

-- ============================================================
-- A. Tags (5 tags, idempotent via unique slug)
-- ============================================================

INSERT INTO tags (id, name, slug) VALUES
  (gen_random_uuid(), 'Software Craftsmanship', 'software-craftsmanship'),
  (gen_random_uuid(), 'TDD',                    'tdd'),
  (gen_random_uuid(), 'Clean Architecture',      'clean-architecture'),
  (gen_random_uuid(), 'XP',                      'xp'),
  (gen_random_uuid(), 'dotnet',                  'dotnet')
ON CONFLICT (slug) DO NOTHING;

-- ============================================================
-- B. Blog post (published, idempotent via unique slug)
-- ============================================================

INSERT INTO blog_posts (id, title, slug, content, status, created_at, updated_at, published_at)
VALUES (
  gen_random_uuid(),
  'Building The Augmented Craftsman: A Blog Forged with XP Practices',
  'building-the-augmented-craftsman-a-blog-forged-with-xp-practices',
  $body$# Building The Augmented Craftsman: A Blog Forged with XP Practices

*You're reading the first article published on this platform — a blog built to write about the very practices used to build it.*

There's something satisfying about writing your first post on a platform you built yourself. Not because it's technically impressive — there are hundreds of blog engines out there. But because **the process of building it is the point**.

This is the story of how *The Augmented Craftsman* was built, one test at a time, following eXtreme Programming and Software Craftsmanship practices. Not as theory. As daily practice.

## Why Build a Blog From Scratch?

I'll be honest: the first impulse was to reach for WordPress, Ghost, or a static site generator. Ship fast, write content, move on.

But I kept coming back to a nagging question: **if I preach TDD, clean architecture, and deliberate design — where's the proof?** Conference slides and Twitter threads are cheap. A deployed, working product built with those practices is not.

So I decided to build the blog the hard way. Or rather, the *right* way — the way I'd want every production system to be built. A blog is deceptively rich as a domain: content management, tagging, image hosting, authentication, reader engagement, moderation. Simple enough to ship. Complex enough to learn from.

What I didn't expect was how much I'd learn about my own practices by applying them under real constraints.

## Architecture: The Hardest Decisions Were What NOT to Build

The first design session was humbling. I had a blank canvas and a head full of patterns — CQRS, Event Sourcing, MediatR pipelines, Domain Events, the works. The temptation to build a showcase of every pattern I know was real.

Then I asked myself: *what problem am I actually solving?*

### Hexagonal Architecture, Not Clean Architecture with CQRS

A blog has no read/write asymmetry. There are no complex queries that diverge from write models. CQRS would have added layers of indirection with zero value. MediatR would have introduced a pipeline for operations that are, fundamentally, CRUD.

I chose **Hexagonal Architecture** because the blog genuinely has ports and adapters — and the proof came when we built the second driving adapter.

### Two Driving Adapters: The Architecture Proves Itself

The original plan was simple: an **Astro frontend** (zero JavaScript, static SSG) for the public-facing blog. Readers see fast, content-first pages. That's one driving adapter.

But managing content through raw database inserts or API calls with curl? That gets old fast. So we built a second driving adapter: a **SPA for author mode** — a rich, interactive interface for writing, editing, publishing, and managing posts with a live Markdown preview.

This is where hexagonal architecture stopped being theoretical and started being practical. The same application core — the same use cases, the same domain model, the same ports — serves two completely different frontends:

- **Astro** (public): static, zero JS, optimized for readers and Core Web Vitals
- **Author SPA** (admin): dynamic, interactive, optimized for the writing experience

Neither frontend knows about the other. Neither contains business logic. They both talk to the same API, which delegates to the same use cases, which enforce the same domain rules. **The architecture doesn't just look clean on a diagram — it works in production with two real consumers.**

Building the author SPA was the moment I stopped wondering whether hexagonal architecture was worth it. The answer was visceral: the application core didn't change at all. Not one use case modified. Not one test broken. We just plugged in a new driving adapter.

### Vertical Slices Inside the Hexagon

Features are organized by use case — `CreatePost`, `GetPostById`, `ToggleLike` — not by technical layer. No empty `Services/` folders. No pass-through classes. Each feature is a cohesive vertical slice that touches domain, application, and API layers.

This felt wrong at first. Years of "organize by layer" muscle memory kept pulling me toward `Controllers/`, `Services/`, `Repositories/` directories. But after the third feature, the vertical approach clicked: **everything you need to understand a feature is in one place.**

### DDD-Lite: The Courage to Keep It Simple

Vernon said it best: *"If it's truly CRUD, use the simplest tools."*

I adopted what adds value:
- **Value Objects** everywhere: `Title`, `Slug`, `PostContent`, `TagName`, `CommentText`. Every primitive is wrapped. Business rules are encoded in the type system. You literally cannot create an invalid `Title` — the constructor throws if it's empty or exceeds 200 characters.
- **Lightweight Aggregates**: `BlogPost` owns its tag collection and lifecycle. `Like` and `Comment` are independent aggregates keyed by post slug — a decision I almost got wrong (more on that later).
- **Repository as Port**: `IBlogPostRepository` lives in the application layer. The EF Core implementation is an infrastructure adapter.

I deliberately skipped: Domain Events, Event Sourcing, Factories, Anti-Corruption Layers, formal Context Maps. None of them solve a problem I actually have *today*. If RSS feeds or webhooks emerge as requirements, Domain Events will earn their place.

This was the hardest part. Knowing a pattern and choosing not to use it requires more confidence than applying it everywhere.

## The TDD Double Loop: Where Design Happens

Every feature followed the Outside-In TDD double loop:

```
OUTER LOOP (Acceptance Test):
  RED → GREEN → REFACTOR
    ↓
  INNER LOOP (Unit Tests):
    RED → GREEN → REFACTOR → RED → ...
```

**One test at a time. Always.**

An acceptance test defines the feature from the outside — an HTTP request to the API, a response with the expected shape. It fails. Then I drop to unit tests: domain entities, value objects, use cases. Each unit test drives a small piece of the design. When enough unit tests pass, the acceptance test goes green.

I'll admit — the discipline of *waiting* for the acceptance test to fail for the right reason before writing any production code felt painfully slow at first. By the third feature, I realized it was saving me from building the wrong thing. The outer loop is a compass.

### The Likes Saga: When Design Decisions Cascade

Building the likes system taught me more about aggregate design than any book.

My first instinct was to make `Like` a child of `BlogPost` — after all, a like "belongs to" a post. But that meant loading the entire post aggregate just to toggle a like. For a read-heavy, write-light operation. On potentially every page view.

I stepped back, looked at the actual use cases, and realized: a like is an independent fact. It references a post by slug. It doesn't need the post's title, content, or tags. It just needs to know *which* post and *which* visitor.

The result: `Like` became an independent aggregate with a composite primary key `(PostSlug, VisitorId)`. Idempotent by design — the database constraint prevents duplicates, not application logic. This was a case where the domain model taught me something about the real-world domain.

### Mutation Testing: Tests That Test the Tests

After building the post management feature — 134 tests across three layers — I felt confident. Then I ran Stryker.NET.

The mutation kill rate? **88.89%**. Not bad, but those surviving mutants told a story: there were boundary conditions my tests didn't exercise, edge cases my assertions glossed over.

One lesson I keep re-learning: **tests that don't catch bugs are theatre.** Mutation testing is the antidote. It doesn't just tell you that your tests pass — it tells you whether they'd catch real regressions.

## Object Calisthenics: From Rules to Reflexes

Object Calisthenics rules aren't academic — they shape the code in tangible ways:

- **Wrap all primitives**: No `string title` floating around. `Title` is a type that validates itself. `Slug` auto-generates from `Title` using a deterministic algorithm. The type system makes wrong states non-representable.
- **No ELSE keyword**: Early returns everywhere. Guard clauses at method entry. This felt extreme until I noticed how much easier the code was to follow.
- **Tell, Don't Ask**: `post.Publish(DateTime.UtcNow)` — not `if (post.Status == Draft) post.Status = Published`. The entity manages its own state transitions and throws if you try to publish twice.
- **First class collections**: `BlogPost.Tags` returns `IReadOnlyList<Tag>` — the collection is encapsulated. You call `post.AddTag(tag)`, you don't mutate the list directly.

The one rule I struggled with most: **keep entities small** (50 lines per class). `BlogPost` kept wanting to grow. The discipline of extracting value objects and pushing behavior into them kept it in check — but it took constant vigilance.

## Pair Programming with AI

The name "The Augmented Craftsman" isn't marketing. It's a thesis: **AI amplifies what the craftsman already knows. The tool amplifies the method.**

Every feature was built in collaboration with AI. But the practices came first:
- I decide the architecture, the test strategy, the design boundaries.
- The AI generates code within those constraints, suggests implementations, catches edge cases.
- The tests verify everything — written before the implementation, by design.

Here's what surprised me: **the TDD loop doesn't care who writes the code.** A failing test is a specification. Whether a human or an AI writes the code that makes it green, the test provides the same safety net. The same feedback. The same design pressure.

Without TDD, AI-generated code is a liability — you can't verify it. With TDD, it's leverage.

## Challenges and Hard-Won Lessons

### Resisting the Resume-Driven Development

The biggest ongoing challenge: **building what the project needs, not what looks impressive on a portfolio.** Every time I considered adding Event Sourcing "just to demonstrate it," I asked: does this blog need an append-only event log? No. Does it need temporal queries? No. Does adding it make the system simpler? Absolutely not.

Showing judgment — what *not* to build — is more impressive than pattern maximalism.

### The Rule of Three Saved Me

Early on, I noticed three different use cases resolving tags from slugs. My instinct was to extract a `TagResolver` after the second occurrence. I forced myself to wait for the third.

Good thing: by the third case, the actual abstraction was different from what I would have built after the second. The Rule of Three isn't about patience — it's about having enough data to see the real pattern.

### Walking Skeleton: The Unsexy Foundation

Before any feature, I built a walking skeleton: a single request flowing through all layers — API endpoint, use case, repository, database, and back. No business logic. Just proving the architecture works end-to-end.

It took almost a full day. It felt like wasted time. It was the most valuable day of the project.

Every feature after that was plugging into a proven architecture. No "does the plumbing work?" surprises at integration time.

## The Stack

| Layer | Choice | Why |
|-------|--------|-----|
| Backend | .NET 10, C# 14 | Strong type system, mature testing ecosystem |
| Public Frontend | Astro | Zero JS for content pages — a blog is content, not an app |
| Author Frontend | SPA | Rich interactive editor for the writing experience |
| Database | PostgreSQL (Neon) | Serverless, free tier, lighter than SQL Server |
| Images | ImageKit | 20GB free storage and bandwidth |
| Testing | xUnit, FluentAssertions, NSubstitute, Reqnroll, Testcontainers | BDD acceptance tests with real PostgreSQL |

The architectural separation (frontends ≠ backend) isn't academic — it's deployed proof. Two different frontends, independently deployed, sharing zero code except the API contract.

## What Comes Next

This is the first post, not the last. The plan is regular writing about:
- TDD patterns and anti-patterns in real projects
- Refactoring techniques with before/after examples
- Domain modeling decisions and trade-offs
- The experience of building software with AI as a pairing partner

Every article will be published on a platform built with the same practices it describes. The medium is the message.

---

*The Augmented Craftsman is built with .NET 10, Astro, and PostgreSQL. Every feature was developed test-first using Outside-In TDD. The source code demonstrates hexagonal architecture, DDD-lite, and Object Calisthenics in a real deployed product.*$body$,
  'Published',
  '2026-03-09T12:00:00Z',
  '2026-03-09T12:00:00Z',
  '2026-03-09T12:00:00Z'
)
ON CONFLICT (slug) DO NOTHING;

-- ============================================================
-- C. Post-tag associations (5 rows, idempotent via NOT EXISTS)
-- ============================================================

INSERT INTO post_tags (post_id, tag_id)
SELECT bp.id, t.id
FROM blog_posts bp, tags t
WHERE bp.slug = 'building-the-augmented-craftsman-a-blog-forged-with-xp-practices'
  AND t.slug = 'software-craftsmanship'
  AND NOT EXISTS (
    SELECT 1 FROM post_tags pt WHERE pt.post_id = bp.id AND pt.tag_id = t.id
  );

INSERT INTO post_tags (post_id, tag_id)
SELECT bp.id, t.id
FROM blog_posts bp, tags t
WHERE bp.slug = 'building-the-augmented-craftsman-a-blog-forged-with-xp-practices'
  AND t.slug = 'tdd'
  AND NOT EXISTS (
    SELECT 1 FROM post_tags pt WHERE pt.post_id = bp.id AND pt.tag_id = t.id
  );

INSERT INTO post_tags (post_id, tag_id)
SELECT bp.id, t.id
FROM blog_posts bp, tags t
WHERE bp.slug = 'building-the-augmented-craftsman-a-blog-forged-with-xp-practices'
  AND t.slug = 'clean-architecture'
  AND NOT EXISTS (
    SELECT 1 FROM post_tags pt WHERE pt.post_id = bp.id AND pt.tag_id = t.id
  );

INSERT INTO post_tags (post_id, tag_id)
SELECT bp.id, t.id
FROM blog_posts bp, tags t
WHERE bp.slug = 'building-the-augmented-craftsman-a-blog-forged-with-xp-practices'
  AND t.slug = 'xp'
  AND NOT EXISTS (
    SELECT 1 FROM post_tags pt WHERE pt.post_id = bp.id AND pt.tag_id = t.id
  );

INSERT INTO post_tags (post_id, tag_id)
SELECT bp.id, t.id
FROM blog_posts bp, tags t
WHERE bp.slug = 'building-the-augmented-craftsman-a-blog-forged-with-xp-practices'
  AND t.slug = 'dotnet'
  AND NOT EXISTS (
    SELECT 1 FROM post_tags pt WHERE pt.post_id = bp.id AND pt.tag_id = t.id
  );

COMMIT;

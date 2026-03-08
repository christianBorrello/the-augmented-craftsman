# The Augmented Craftsman — Blog Platform

## Project Overview

A personal blog platform for daily posts about Software Craftsmanship, TDD, Clean Architecture, DDD, and XP practices. Built as both a **real deployed product** and a **portfolio piece** demonstrating deliberate, principled software engineering.

The blog name is **"The Augmented Craftsman"** — reflecting the philosophy that AI amplifies what the craftsman already knows. The tool amplifies the method.

## Philosophy

> "Software development is a learning process, working code is a side effect." — Alberto Brandolini

Technologies are tools chosen based on the problem. What remains is the method: TDD, Clean Architecture, DDD, continuous refactoring, incremental design. These practices shape code **before** writing it — even when the AI writes it.

**Guiding document**: `BEST_PRACTICES.md` — all coding decisions must align with its principles.

## Architecture

### Decision: Hexagonal Architecture + Vertical Slice Feature Organization

**NOT** Clean Architecture with CQRS/MediatR. The blog domain is CRUD-heavy and low-complexity. The architecture was chosen to match the problem, not to demonstrate pattern knowledge.

**Why Hexagonal**: The project has real ports and adapters — the separate frontend is a driving adapter, the database and ImageKit are driven adapters. This is not theoretical.

**Why Vertical Slice inside the hexagon**: Features organized by use case (`CreatePost`, `GetPostById`) rather than by technical layer, avoiding empty layers and pass-through classes.

### Project Structure

```
the-augmented-craftsman/
  frontend/              # Astro — driving adapter (zero JS, static, SSG)
  backend/               # .NET 10 — application core + API
    src/
      Domain/            # Entities, Value Objects (Slug, Title, TagName...)
      Application/       # Use cases (feature-organized), Port definitions
        Features/
          Posts/
          Tags/
          Images/
        Ports/
          Driven/        # IBlogPostRepository, IImageStorage
      Infrastructure/    # Driven adapters (EF Core, ImageKit, Identity)
      Api/               # Driving adapter (Minimal API endpoints)
    tests/
      Domain.Tests/
      Application.Tests/ # Use case tests (driven ports stubbed)
      Api.Tests/         # Integration tests
      Acceptance.Tests/  # BDD acceptance tests (Outside-In entry point)
  docs/                  # Research, brainstorm, architecture decisions
  blog/                  # REFERENCE ONLY — old tutorial code, remove after migration
```

## Tech Stack

| Layer | Technology | Rationale |
|---|---|---|
| Backend | .NET 10, C# 14 | Best fit for OOP craftsmanship practices; strong type system; mature testing ecosystem |
| Frontend | Astro | Zero JS by default; content-first; best Core Web Vitals for a blog |
| Database | PostgreSQL (Neon) | Serverless, free tier, lighter than SQL Server |
| Images | ImageKit | 20GB storage, 20GB bandwidth free; unlimited transforms; official .NET SDK (`Imagekit` v4) |
| Testing | xUnit + FluentAssertions + NSubstitute + Reqnroll 2.4 + Testcontainers | BDD acceptance tests (outer loop) + unit/integration with real PostgreSQL |
| API Style | Minimal API | Lightweight, aligns with .NET 10 direction |
| Deploy | Vercel (frontend) + Fly.io (backend) | $0/month, separation proves the architecture works |

## DDD Approach: DDD-Lite

The blog domain does NOT warrant full DDD tactical patterns. Apply proportionally:

### Adopt
- **Value Objects**: `Slug`, `Title`, `PostContent`, `TagName` — encode rules in the type system
- **Lightweight Aggregate**: `BlogPost` as root, owns Tag collection and lifecycle
- **Repository Interface**: `IBlogPostRepository` as domain port
- **Bounded Context Awareness**: Content, Identity, Media as separate namespaces
- **Ubiquitous Language**: blog domain terms in code

### Defer (add when a real need emerges)
- Domain Events (trigger: RSS feed, webhooks)
- Domain Services (trigger: operation doesn't fit Entity or Value Object)

### Skip
- Factories, Event Sourcing, Anti-Corruption Layers, formal Context Maps, CQRS, MediatR pipeline

## TDD Approach: Outside-In Double Loop

```
OUTER LOOP (Acceptance Test):
  RED → GREEN → REFACTOR
    ↓
  INNER LOOP (Unit Tests):
    RED → GREEN → REFACTOR → RED → ...
```

1. Write a failing acceptance test (driving adapter boundary)
2. Drop to unit tests (application core through ports)
3. TDD until acceptance test passes
4. Refactor while green
5. Next acceptance test

**Always write one test at a time. Wait for it to fail for the right reason before implementing.**

## Coding Standards

All code must follow `BEST_PRACTICES.md`. Key rules:

### Object Calisthenics (enforced during code review)
- One level of indentation per method
- No ELSE keyword — use early returns, polymorphism
- Wrap all primitives and strings (Value Objects)
- First class collections
- One dot per line (Law of Demeter)
- Don't abbreviate
- Keep entities small (5 lines/method, 50 lines/class, 2 args/method)
- No classes with more than two instance variables
- No getters/setters — Tell, Don't Ask
- All classes must have state

### SOLID (always)
- SRP: one reason to change per class
- OCP: extend through abstractions, not modification
- LSP: subtypes substitutable for base types
- ISP: focused interfaces, no unused dependencies
- DIP: depend on abstractions (ports), not concretions (adapters)

### Refactoring
- Stay on green — never break tests during refactoring
- Readability first (80% of value): rename, delete dead code, extract constants
- Then design: extract methods, return early, encapsulate, remove duplication
- Rule of Three before extracting duplication
- Parallel Change (Expand-Migrate-Contract) for breaking changes

### Test Naming
```csharp
// Pattern: ClassNameShould
class BlogPostShould
{
    void generate_slug_from_title() {}
    void reject_empty_title() {}
    void publish_when_in_draft_state() {}
}
```

## Frontend Design System: "Forge & Ink"

- **Aesthetic**: Warm, editorial, textured — a craftsman's notebook
- **Typography**: Fraunces (display), Literata (body), JetBrains Mono (code)
- **Palette**: Warm parchment/forge black + burnt amber accent
- **Animations**: CSS only (View Transitions, scroll reveals, staggered entry) — zero JS
- **Dark/light mode**: CSS variables with `prefers-color-scheme` detection

## Migration Reference

The `blog/` folder contains the original tutorial code (TacBlog legacy). Use it as reference for:
- Understanding existing feature scope (CRUD posts, tags, images, auth)
- Data model structure (BlogPost, Tag, BlogImage, Comment, Like)
- ImageKit integration pattern (legacy code used Cloudinary)
- ASP.NET Identity setup

**Do NOT copy code from `blog/` directly.** Every feature must be rebuilt test-first with the new architecture.

## Key Decisions Log

| Decision | Choice | Rationale |
|---|---|---|
| Architecture | Hexagonal + Vertical Slice | Matches real integration points; designed for TDD |
| Not CQRS | Skipped | No read/write asymmetry in a blog |
| Not MediatR | Skipped | Added indirection without value at this scale |
| DDD depth | DDD-lite | Vernon: "If truly CRUD, use simplest tools" |
| Frontend | Astro over Next.js | Zero JS for content pages; blog is content, not app |
| Database | PostgreSQL over SQL Server | Better cloud hosting (Neon free tier), lighter |
| API style | Minimal API over Controllers | Lighter ceremony, .NET 10 direction |
| No generic repo | Specific repositories | Generic `IRepository<T>` is a leaky abstraction |
| Image hosting | ImageKit over Cloudinary | 20GB storage + 20GB bandwidth free tier; unlimited transforms; no suspension risk; official .NET SDK |

## nWave Error Tracking

When encountering any problem, bug, or unexpected behavior related to **nWave** (skills, agents, workflows), document it in `nwave-errors.md` at the project root. This file serves as a log for opening issues on the nWave repository.

Each entry should include:
- **Date** of occurrence
- **Skill/Agent** involved (e.g., `nw:deliver`, `nw:execute`)
- **Description** of the problem
- **Steps to reproduce** (if identifiable)
- **Error output** or unexpected behavior observed
- **Workaround** (if any was found)

## Commands

```bash
# Frontend
cd frontend && npm run dev     # Start Astro dev server
cd frontend && npm run build   # Build static site

# Backend (once created)
cd backend && dotnet test      # Run all tests
cd backend && dotnet run --project src/Api  # Start API
```

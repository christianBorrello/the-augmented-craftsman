# The Augmented Craftsman

**A craftsman's blog on Software Craftsmanship — built with the practices it preaches.**

> "Software development is a learning process, working code is a side effect." — Alberto Brandolini

A personal blog platform for daily posts about TDD, Clean Architecture, DDD, and XP practices. Built as both a **real deployed product** and a **portfolio piece** demonstrating deliberate, principled software engineering. The tool amplifies the method — AI assists, but the craftsman decides.

## Live

| | URL |
|---|---|
| Blog | [theaugmentedcraftsman.christianborrello.dev](https://theaugmentedcraftsman.christianborrello.dev) |
| API | [api.theaugmentedcraftsman.christianborrello.dev](https://api.theaugmentedcraftsman.christianborrello.dev) |

## Features

- Full post lifecycle: create, edit, publish, schedule, feature images
- Tag taxonomy with slug-based routing
- Image management via ImageKit (transforms, CDN delivery)
- Public reading experience with SSG-rendered pages
- Reader sessions via GitHub/Google OAuth
- Like system (anonymous + authenticated, with visitor deduplication)
- Comment moderation (admin approval workflow)
- Admin authentication with JWT
- Zero-JS frontend (single Preact island for the like button)

## Architecture

### Hexagonal + Vertical Slice

The architecture was chosen to **match the problem**, not to demonstrate pattern knowledge.

**Why Hexagonal**: The project has real ports and adapters — the Astro frontend is a driving adapter consuming the API, PostgreSQL and ImageKit are driven adapters behind port interfaces. This is not theoretical.

**Why Vertical Slice inside the hexagon**: Features organized by use case (`CreatePost`, `GetPostById`) rather than by technical layer, avoiding empty layers and pass-through classes.

**Why NOT Clean Architecture with CQRS/MediatR**: A blog has no read/write asymmetry. MediatR adds indirection without value at this scale. The decision to skip patterns is as deliberate as the decision to adopt them.

```
the-augmented-craftsman/
├── frontend/                  # Astro 5 — driving adapter (SSG, zero JS)
│   └── src/
│       ├── pages/             # Routes (SSG-rendered)
│       ├── components/        # Astro + 1 Preact island
│       ├── layouts/           # Page templates
│       └── styles/            # Tailwind CSS
├── backend/                   # .NET 10 — application core + API
│   ├── src/
│   │   ├── Domain/            # Entities, Value Objects (pure, no dependencies)
│   │   ├── Application/       # Use cases + Port definitions
│   │   │   ├── Features/      # Posts, Tags, Images, Likes, Comments, Auth, OAuth
│   │   │   └── Ports/Driven/  # IBlogPostRepository, IImageStorage, ...
│   │   ├── Infrastructure/    # Driven adapters (EF Core, ImageKit, Identity)
│   │   └── Api/               # Driving adapter (Minimal API endpoints)
│   └── tests/
│       ├── Domain.Tests/      # Value Object and Entity unit tests
│       ├── Application.Tests/ # Use case tests (driven ports stubbed)
│       ├── Api.Tests/         # Integration tests (Testcontainers + real PostgreSQL)
│       └── Acceptance.Tests/  # BDD acceptance tests (Reqnroll, Outside-In entry)
└── docs/                      # Research, design, architecture evolution
```

## Tech Stack

| Layer | Technology | Rationale |
|---|---|---|
| Backend | .NET 10, C# 14 | Strong type system; mature testing ecosystem; OOP craftsmanship fit |
| Frontend | Astro 5 | Zero JS by default; content-first; best Core Web Vitals for a blog |
| Database | PostgreSQL (Neon) | Serverless free tier; lighter than SQL Server for cloud hosting |
| Images | ImageKit | 20GB storage + 20GB bandwidth free; unlimited transforms; .NET SDK |
| ORM | EF Core 10 | PostgreSQL via Npgsql; migrations; LINQ queries |
| Auth | JWT + OAuth | Admin JWT; reader sessions via GitHub/Google OAuth |
| Testing | xUnit + FluentAssertions + NSubstitute + Reqnroll + Testcontainers | BDD outer loop + unit/integration with real PostgreSQL |
| CI/CD | GitHub Actions | Format check → build → test pyramid → Docker push → smoke tests |
| Deploy | Koyeb (backend) + Vercel (frontend) | $0/month on free tiers; separation proves the architecture |

## DDD-Lite

The blog domain does NOT warrant full DDD tactical patterns. Vernon: *"If truly CRUD, use simplest tools."* Applied proportionally:

**Adopt**
- **Value Objects**: `Slug`, `Title`, `PostContent`, `TagName`, `CommentText`, `VisitorId` — rules encoded in the type system
- **Aggregate**: `BlogPost` as root, owns Tags, Likes, Comments and their lifecycle
- **Repository Interface**: `IBlogPostRepository` as domain port (no generic `IRepository<T>`)
- **Ubiquitous Language**: blog domain terms in code (`publish`, `draft`, `moderate`)

**Defer** — Domain Events (trigger: RSS feed), Domain Services (trigger: cross-entity operations)

**Skip** — Factories, Event Sourcing, Anti-Corruption Layers, CQRS, MediatR pipeline

## TDD: Outside-In Double Loop

```
OUTER LOOP (Acceptance Test — Reqnroll BDD):
  RED → GREEN → REFACTOR
    │
    └─► INNER LOOP (Unit Tests — xUnit):
          RED → GREEN → REFACTOR → RED → ...
```

1. Write a failing acceptance test at the driving adapter boundary
2. Drop to unit tests for application core (driven ports stubbed with NSubstitute)
3. TDD until the acceptance test passes
4. Refactor while green
5. Next acceptance test

## Coding Standards

All code follows [BEST_PRACTICES.md](BEST_PRACTICES.md). Highlights:

**Object Calisthenics** — One indentation level per method. No `else` keyword. All primitives wrapped as Value Objects. First-class collections. One dot per line. No abbreviations. Small entities (~50 lines/class).

**SOLID** — SRP, OCP (extend through ports), LSP, ISP (focused interfaces), DIP (depend on abstractions).

**Refactoring** — Stay on green. Readability first. Rule of Three before extracting duplication. Parallel Change for breaking changes.

**Test naming**: `BlogPostShould.generate_slug_from_title()`

## Design System: "Forge & Ink"

A warm, editorial aesthetic — a craftsman's notebook, not a tech blog template.

- **Typography**: Fraunces (display), Literata (body), JetBrains Mono (code)
- **Palette**: Warm parchment / forge black + burnt amber accent
- **Animations**: CSS-only (View Transitions, scroll reveals) — zero JavaScript
- **Dark/light mode**: CSS variables with `prefers-color-scheme`

## Testing

**325 tests** across 4 test projects. **88.89% mutation kill rate** (Stryker.NET).

| Project | Tests | What it covers |
|---|---|---|
| Domain.Tests | 68 | Value Objects, Entity behavior, invariants |
| Application.Tests | 101 | Use cases with stubbed driven ports |
| Api.Tests | 26 | Integration tests with real PostgreSQL (Testcontainers) |
| Acceptance.Tests | 130 | BDD scenarios (Given-When-Then) end-to-end |

Mutation testing with **Stryker.NET** verifies test quality — not just coverage, but whether tests actually catch regressions.

## CI/CD & Deployment

**GitHub Actions** runs the full pipeline on every push:

1. `dotnet format --verify-no-changes` — code style gate
2. `dotnet build` — compilation check
3. Domain Tests → Application Tests → Integration Tests → Acceptance Tests
4. Docker multi-stage build → push to GHCR
5. Smoke tests (health endpoint verification)

**$0/month deployment**: Koyeb (Docker, Frankfurt) + Vercel (static SSG) + Neon (serverless PostgreSQL) + ImageKit (CDN).

See [DEPLOYMENT.md](DEPLOYMENT.md) for the full setup guide.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (for integration/acceptance tests — Testcontainers)

### Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet test                              # Runs all 325 tests
dotnet run --project src/TacBlog.Api     # Starts API on http://localhost:5000
```

### Frontend

```bash
cd frontend
npm install
npm run dev       # Starts Astro dev server
npm run build     # Builds static site
```

## Key Decisions

| Decision | Choice | Why |
|---|---|---|
| Architecture | Hexagonal + Vertical Slice | Real ports/adapters; TDD-friendly |
| Not CQRS | Skipped | No read/write asymmetry in a blog |
| Not MediatR | Skipped | Indirection without value at this scale |
| DDD depth | DDD-Lite | Proportional to domain complexity |
| Frontend | Astro over Next.js | Zero JS for content; blog is content, not app |
| Database | PostgreSQL over SQL Server | Neon free tier; better cloud hosting |
| API style | Minimal API over Controllers | Lighter ceremony, .NET 10 direction |
| No generic repository | Specific repositories | Generic `IRepository<T>` is a leaky abstraction |
| Images | ImageKit over Cloudinary | 20GB free; no suspension risk; official .NET SDK |
| Deploy | Koyeb over Fly.io | Simpler free tier; Docker-native |

## Documentation

The `docs/` folder tracks the project's evolution:

```
docs/
├── evolution/     # Architecture decisions and epic retrospectives
├── design/        # UI/UX design specs
├── feature/       # Feature specifications
├── distill/       # Requirements distillation (BDD scenarios)
├── research/      # Technology evaluations
├── ux/            # User experience analysis
└── brainstorm/    # Early-stage ideas
```

Each epic has a delivery archive documenting what was built, tested, and learned.

---

Built with deliberate practice. Every pattern adopted — and every pattern skipped — was a conscious choice.

# The Augmented Craftsman

**A craftsman's blog on Software Craftsmanship — built with the practices it preaches.**

> "Software development is a learning process, working code is a side effect." — Alberto Brandolini

A personal blog platform for daily posts about TDD, Clean Architecture, DDD, and XP practices. Built as both a **real deployed product** and a **portfolio piece** demonstrating deliberate, principled software engineering. The tool amplifies the method — AI assists, but the craftsman decides.

**Origin**: This project started as a tutorial-born ASP.NET MVC blog with zero tests, no architectural separation, and business logic in controllers. It was rebuilt from scratch using Outside-In TDD, proving that craftsmanship principles apply even when AI writes the code.

## Live

| | URL |
|---|---|
| Blog | [theaugmentedcraftsman.christianborrello.dev](https://theaugmentedcraftsman.christianborrello.dev) |
| API | [api.theaugmentedcraftsman.christianborrello.dev](https://api.theaugmentedcraftsman.christianborrello.dev) |

## Features

- Full post lifecycle: create, edit, publish, schedule, archive, feature images
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
└── backend/                   # .NET 10 — application core + API
    ├── src/
    │   ├── Domain/            # Entities, Value Objects (pure, no dependencies)
    │   ├── Application/       # Use cases + Port definitions
    │   │   ├── Features/      # Posts, Tags, Images, Likes, Comments, Auth, OAuth
    │   │   └── Ports/Driven/  # IBlogPostRepository, IImageStorage, ...
    │   ├── Infrastructure/    # Driven adapters (EF Core, ImageKit, Identity)
    │   └── Api/               # Driving adapter (Minimal API endpoints)
    └── tests/
        ├── Domain.Tests/      # Value Object and Entity unit tests
        ├── Application.Tests/ # Use case tests (driven ports stubbed)
        ├── Api.Tests/         # Integration tests (Testcontainers + real PostgreSQL)
        └── Acceptance.Tests/  # BDD acceptance tests (Reqnroll, Outside-In entry)
```

## Development Journey

The blog was built incrementally, one vertical slice at a time. Each step extended the system while keeping all tests green. This timeline shows the deliberate progression from a minimal skeleton to a full-featured platform.

### 1. Walking Skeleton (Mar 6)

First commit: one `POST /api/posts` + one `GET /api/posts/{id}` through all layers — Domain, Application, Infrastructure, API. No real features, but the hexagonal structure is proven end-to-end. Acceptance tests drive the outer loop, unit tests the inner loop. Testcontainers spin up a real PostgreSQL instance.

### 2. Post Management + Admin Auth (Mar 6–7)

Full CRUD for blog posts with draft/published lifecycle. Admin JWT authentication protects write endpoints. Value Objects emerge: `Title` (non-empty, max length), `Slug` (URL-safe, immutable after creation), `PostContent` (Markdown body). The Outside-In TDD rhythm is established — every feature starts with a failing Reqnroll scenario.

### 3. Tag Management (Mar 7)

Many-to-many tag associations with slug routing. `TagName` as Value Object enforces naming rules at the type level. The repository port (`IBlogPostRepository`) grows — no generic `IRepository<T>`, because specific repositories express domain intent better than generic abstractions.

### 4. Image Management (Mar 7)

ImageKit integration as a driven adapter behind the `IImageStorage` port. Server-side upload with file validation (10MB max, type checking). This is where ports and adapters prove their value — the use case tests stub `IImageStorage`, the integration tests hit the real ImageKit API.

### 5. Public Reading Experience (Mar 8)

The Astro frontend arrives as a **second driving adapter** consuming the same API. Static Site Generation renders published posts at build time — zero JavaScript, perfect Core Web Vitals. The "Forge & Ink" design system gives the blog its editorial, craftsman's-notebook aesthetic.

### 6. User Engagement — Likes, OAuth, Comments (Mar 8–13)

Three sub-features delivered as vertical slices:

- **Likes**: Anonymous likes with `VisitorId` deduplication (fingerprint hash). Authenticated users get one like per post. A single Preact island — the only JavaScript on the public site.
- **OAuth**: GitHub/Google sign-in for readers. `IOAuthClient` port, with session management via cookies.
- **Comments**: Reader-submitted comments with admin moderation workflow (pending → approved/rejected). `CommentText` Value Object enforces length and content rules.

### 7. Author Mode (Mar 14)

Admin panel within the Astro frontend using hybrid rendering — SSR for `/admin/*` pages, SSG for the public blog. Astro Sessions with Upstash Redis. Post lifecycle extended with `Archived` status and restore-to-previous-state. Three-layer authorization: Astro middleware → JWT Bearer → endpoint-level checks.

### 8. Admin Auth Simplification (Mar 15)

A deliberate simplification: replaced the 4-step OAuth+Redis admin flow with email/password + inline JWT issuance. **Deleted ~400 lines of code** — three use cases, two ports, three adapters, one endpoint file. The original OAuth flow had caused a production crash bug (`SingleOrDefault` failure from dual `IOAuthClient` registrations). This step proves that simplifying is as much a craft as building.

### 9. Code Mode (Mar 23)

IDE-style syntax-highlighted code blocks with Shiki. A lean feature — direct implementation without the full wave ceremony, because the scope was small and well-understood.

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

## Architecture Decision Records

| ADR | Decision | Context |
|---|---|---|
| ADR-002 | Astro hybrid output + Upstash Redis sessions | Admin SSR pages need sessions; Astro 5.7+ Sessions are stable with Redis driver on Vercel |
| ADR-003 | Three-layer authorization | Astro middleware → JWT Bearer → endpoint-level checks. No single layer is the sole auth gate. |
| ADR-004 | Post lifecycle with `Archived` status | Soft delete with `PreviousStatus` field to restore to Draft or Published state |
| ADR-005 | Admin JWT inline in LoginHandler | Replaced 4-step OAuth+Redis flow. Deleted `IAdminTokenStore`, `ITokenGenerator`, 3 use cases. Supersedes the original OAuth approach (ADR-001, deleted). |
| ADR-006 | Remove `AdminEmail` from `IAdminSettings` | After OAuth removal, `AdminEmail` had no remaining consumers. ISP: don't keep unused properties. |
| ADR-007 | Delete `ITokenGenerator` port | After inlining JWT in `LoginHandler`, the port had zero consumers. Dead code deleted. |

## CI/CD & Deployment

**GitHub Actions** runs the full pipeline on every push:

1. `dotnet format --verify-no-changes` — code style gate
2. `dotnet build` — compilation check
3. Domain Tests → Application Tests → Integration Tests → Acceptance Tests
4. Docker multi-stage build → push to GHCR
5. Smoke tests (health endpoint verification)

**$0/month deployment**: Koyeb (Docker, Frankfurt) + Vercel (static SSG) + Neon (serverless PostgreSQL) + ImageKit (CDN).

### Production Stack Setup

**Neon Database**: Create project "tacblog" in EU region (AWS Frankfurt). Copy the connection string.

**ImageKit**: Create account, note URL Endpoint, Public Key, Private Key.

**Koyeb Backend**:
- Source: Docker image `ghcr.io/<owner>/tacblog-api:latest`
- Instance: Free (nano, 256MB RAM), Frankfurt region, port 8080
- Health check: HTTP GET `/health`
- Auto-deploy: Koyeb watches GHCR `:latest` tag — CI pushes trigger redeploy

**Vercel Frontend**:
- Import repo, set root directory to `frontend`
- Set `PUBLIC_API_URL` to the backend URL
- Custom domain configuration

### Required Environment Variables (Koyeb)

| Variable | Source | Required |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Neon dashboard | Yes |
| `Jwt__Secret` | Generated secret | Yes |
| `AdminCredentials__Email` | Fixed value | Yes |
| `AdminCredentials__HashedPassword` | `dotnet run -- --generate-password-hash` | Yes |
| `OAuth__GitHub__ClientId` | GitHub OAuth Apps | Yes |
| `OAuth__GitHub__ClientSecret` | GitHub OAuth Apps | Yes |
| `ImageKit__UrlEndpoint` | ImageKit dashboard | Recommended |
| `ImageKit__PublicKey` | ImageKit dashboard | Recommended |
| `ImageKit__PrivateKey` | ImageKit dashboard | Recommended |

### Generating Admin Password Hash

```bash
dotnet run --project src/TacBlog.Api -- --generate-password-hash "your-secure-password"
```

### DNS Configuration

| Record | Name | Value |
|---|---|---|
| CNAME | `api.theaugmentedcraftsman` | `<cname-target-from-koyeb>` |
| CNAME | `theaugmentedcraftsman` | `cname.vercel-dns.com` |

### Rollback

```bash
# Tag the previous working image as :latest and push
docker pull ghcr.io/<owner>/tacblog-api:<previous-commit-sha>
docker tag ghcr.io/<owner>/tacblog-api:<previous-commit-sha> ghcr.io/<owner>/tacblog-api:latest
docker push ghcr.io/<owner>/tacblog-api:latest
```

### Smoke Tests

```bash
curl -s https://api.theaugmentedcraftsman.christianborrello.dev/health
curl -s https://api.theaugmentedcraftsman.christianborrello.dev/health/ready
curl -s https://api.theaugmentedcraftsman.christianborrello.dev/api/posts | head -c 200
curl -s -o /dev/null -w "%{http_code}" https://theaugmentedcraftsman.christianborrello.dev
```

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

---

Built with deliberate practice. Every pattern adopted — and every pattern skipped — was a conscious choice.

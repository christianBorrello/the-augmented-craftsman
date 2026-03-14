# Contributing to The Augmented Craftsman

This project follows deliberate, principled software engineering. Before contributing, read [README.md](README.md) to understand the architecture, DDD approach, and coding standards. This guide explains _how_ to work on it.

## Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) — required for integration and acceptance tests (Testcontainers)

### Environment Variables

Copy the example file and fill in your credentials:

```bash
cp backend/src/TacBlog.Api/appsettings.Development.json.example \
   backend/src/TacBlog.Api/appsettings.Development.json
```

Required values:

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string (local or Neon) |
| `Jwt:Secret` | Secret for signing admin JWTs (any random 32+ char string locally) |
| `ImageKit:PublicKey`, `PrivateKey`, `UrlEndpoint` | ImageKit credentials |
| `GitHub:ClientId`, `ClientSecret` | OAuth app credentials |
| `Google:ClientId`, `ClientSecret` | OAuth app credentials |

### Running Locally

```bash
# Backend
cd backend
dotnet restore
dotnet build
dotnet run --project src/TacBlog.Api     # API on http://localhost:5000

# All tests (requires Docker)
dotnet test

# Frontend
cd frontend
npm install
npm run dev     # Astro dev server on http://localhost:4321
```

## TDD: Outside-In Double Loop

All changes must follow the Outside-In TDD workflow. **Do not submit code without tests.**

```
OUTER LOOP (Acceptance Test — Reqnroll BDD):
  RED → GREEN → REFACTOR
    │
    └─► INNER LOOP (Unit Tests — xUnit):
          RED → GREEN → REFACTOR → RED → ...
```

1. Write a **failing acceptance test** at `backend/tests/TacBlog.Acceptance.Tests/Features/`
2. Drop to unit tests; drive the application core through ports
3. TDD until the acceptance test passes
4. Refactor while green — never break tests during refactoring
5. One acceptance test at a time; wait for it to fail for the right reason before implementing

## Test Naming Convention

```csharp
// Pattern: ClassNameShould
class BlogPostShould
{
    void generate_slug_from_title() { }
    void reject_empty_title() { }
    void publish_when_in_draft_state() { }
}
```

- Class name: `<Subject>Should`
- Method name: snake_case, describes behaviour
- No "test" prefix

## Project Structure

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
│       ├── Application.Tests/ # Use case tests (driven ports stubbed with NSubstitute)
│       ├── Api.Tests/         # Integration tests (Testcontainers + real PostgreSQL)
│       └── Acceptance.Tests/  # BDD scenarios (Given-When-Then) end-to-end
└── docs/                      # Research, design, architecture evolution
```

Where to add new code:

- **New domain rule** → `Domain/` — Value Object or Entity method, with a unit test in `Domain.Tests/`
- **New use case** → `Application/Features/<FeatureName>/` — one class per use case
- **New endpoint** → `Api/` — Minimal API endpoint, wired to the use case
- **New driven port** → `Application/Ports/Driven/` (interface) + `Infrastructure/` (adapter)

## Code Style

All code follows [BEST_PRACTICES.md](BEST_PRACTICES.md). The CI pipeline runs `dotnet format --verify-no-changes` on every push — fix formatting before pushing:

```bash
cd backend && dotnet format
```

Key rules enforced during review:

- **Object Calisthenics**: one indentation level per method, no `else`, all primitives wrapped as Value Objects, one dot per line
- **SOLID**: depend on port interfaces, not concrete adapters
- **Refactoring**: stay on green; readability before design

## CI Pipeline

Every push runs:

1. `dotnet format --verify-no-changes`
2. `dotnet build`
3. Domain Tests → Application Tests → Integration Tests → Acceptance Tests
4. Docker multi-stage build
5. Smoke tests

All checks must pass before merge.

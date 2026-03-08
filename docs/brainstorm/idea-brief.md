# Idea Brief: TacBlog — Personal Blog Platform

## One-Sentence Summary

A personal blog platform rebuilt from scratch with Software Craftsmanship principles, serving as both a daily content publishing tool and a portfolio piece demonstrating TDD, DDD, SOLID, and clean architectural practices.

## Problem Statement

An existing tutorial-born ASP.NET MVC blog (TacBlog) needs to be evolved into a production-grade, publicly deployed blog. The current codebase has zero tests, no architectural separation, domain models as data classes, and business logic in controllers — it cannot serve as either a reliable product or a credible portfolio piece.

## Context & Constraints

### User
- Software Engineer building career on XP and Software Craftsmanship traditions
- Daily practitioner of TDD, Clean Architecture, DDD, continuous refactoring
- Uses AI-assisted development (Claude Code + nWave pipeline) with discipline: tests, architecture, and principles guide the AI, not the other way around

### Project Dual Purpose
1. **Real product**: deployed blog for daily post publishing, public-facing
2. **Portfolio piece**: code quality must demonstrate mastery of craftsmanship practices to recruiters and peers

### Guiding Principles (from BEST_PRACTICES.md)
- TDD (Red-Green-Refactor, Three Laws, Outside-In Double Loop)
- SOLID Principles
- Object Calisthenics (10 design rules)
- DDD tactical patterns (where appropriate to domain complexity)
- Four Elements of Simple Design
- Code Smells awareness and continuous refactoring
- Connascence as unified design metric
- BDD / Acceptance Testing for feature validation

## Technical Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Backend | .NET 10 (C#) | Best fit for OOP craftsmanship practices; strong type system; mature testing ecosystem |
| Frontend | Separate app (React/Next.js or equivalent) | Proves API layer is well-designed; frontend is a driving adapter |
| Architecture | **To be analyzed** | Must fit the blog domain, not chosen dogmatically |
| Approach | Rebuild Outside-In | No existing tests to lean on; start clean with TDD from first line |
| Testing | xUnit + FluentAssertions + NSubstitute + SpecFlow (BDD) | Expressive, mature .NET testing pipeline |

## Scope — v1

- CRUD blog posts (create, read, update, delete)
- Tag management
- Image upload
- Authentication (admin area)
- Public blog reading experience

## Alternatives Explored & Discarded

### Strangler Fig Refactoring
- **Considered**: incremental refactoring of existing codebase
- **Discarded because**: no existing tests as safety net, tutorial code patterns too far from target architecture, rebuild is cleaner for a project of this size

### Hexagonal Architecture as Default
- **Considered**: adopting Hexagonal as the evergreen architectural choice
- **Discarded because**: architecture must be analyzed against the specific domain and use case, not chosen dogmatically

### Alternative Stacks (TypeScript, Kotlin, Go)
- **Considered**: full stack evaluation
- **Discarded because**: C#/.NET best expresses the OOP craftsmanship practices (Object Calisthenics, DDD patterns, SOLID) that are core to the project's identity

## Open Questions for Research

1. **Architecture selection**: Which architectural style best fits a content-driven blog with this scope? (Vertical Slice, Clean Architecture, Hexagonal, Layered, CQRS, or hybrid)
2. **Frontend stack**: React/Next.js vs Astro vs other SSR/SSG options for SEO-optimized blog frontend
3. **DDD applicability**: Is the blog domain complex enough to warrant full DDD tactical patterns, or is a simpler domain model sufficient?
4. **Deployment strategy**: Best hosting options for .NET 10 API + separate frontend in 2026
5. **.NET 10 features**: What new .NET 10 capabilities are relevant for this project?

## Next Step

Research wave (`nw:research`) to investigate open questions before architectural design.

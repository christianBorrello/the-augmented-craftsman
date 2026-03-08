# Idea Brief: User Engagement — Likes, Comments & Sharing

## One-Sentence Summary

Add three forms of user interaction to the blog — anonymous likes (one per post, cookie-persisted), social-login-gated comments (Google/GitHub), and native device sharing — to create a feedback loop, enable organic traffic growth, and build a developer community.

## Problem Statement

The blog is a one-way channel: the author writes, readers consume, and nothing comes back. This creates three concrete problems:

1. **No feedback loop** — impossible to tell which posts resonate with readers
2. **No organic traffic** — readers cannot share content or generate engagement that brings new visitors
3. **No community** — readers are isolated from each other and from the author

## Context & Constraints

### Audience
- Software developers interested in Craftsmanship, TDD, DDD, XP practices
- Technically literate (comfortable with GitHub/Google login)
- Visit from multiple devices (desktop at work, mobile on commute)

### Budget
- €0/month — no paid third-party services
- Must use existing infrastructure: PostgreSQL (Neon free tier), .NET 10 backend, Astro frontend

### Architectural Constraints
- Hexagonal Architecture + Vertical Slice feature organization
- Outside-In TDD (double loop)
- Astro frontend is static/SSG with zero JS by default — interactive features require targeted JS

## Feature Specification

### 1. Likes (Anonymous)
- **Authentication**: none required
- **Limit**: one like per person per post
- **Persistence**: cookie/localStorage to remember the visitor across sessions
- **Behavior**: if same visitor returns, the like button shows as already liked
- **Acceptance**: clearing cookies or switching device resets identity (acceptable trade-off)

### 2. Comments (Social Login Required)
- **Authentication**: Google or GitHub OAuth (one-click, no custom account)
- **Display name**: from social profile (no manual entry needed)
- **Moderation**: post-moderation — comments appear immediately, author removes inappropriate ones manually
- **Cross-device**: social login provides consistent identity across devices and browsers

### 3. Sharing (No Authentication)
- **Mechanism**: Web Share API — native device share menu (OS/browser share sheet)
- **Fallback**: copy-to-clipboard for browsers without Web Share API support
- **No third-party SDKs**: no Facebook/Twitter/LinkedIn buttons or tracking scripts

## Decisions Made During Brainstorm

| Decision | Choice | Rationale |
|---|---|---|
| Comment auth | Social login (Google/GitHub) | Zero friction for developer audience; solves identity, spam, cross-device |
| Like auth | Anonymous (cookie) | Lowest barrier for lightest interaction; acceptable that cookies can be cleared |
| Custom account | No | Too much friction; adds complexity without value for a personal blog |
| Comment moderation | Post-moderation | Author reviews after publication; avoids blocking legitimate comments |
| Share mechanism | Web Share API | Native, zero dependencies, respects user's installed apps |
| Display name source | Social profile | No manual entry; consistent identity from Google/GitHub |

## Alternatives Explored & Discarded

### Third-Party Comment Services (Disqus, Giscus)
- **Considered**: integrate existing comment platforms
- **Discarded because**: Disqus has ads on free tier; Giscus requires GitHub account for ALL interactions (not just comments); both add external dependencies and reduce control

### Fully Anonymous Comments
- **Considered**: comments with just a visitor-chosen display name, no login
- **Discarded because**: high spam risk, no cross-device identity, no way to associate comments with a consistent person

### Custom Account (Username/Password)
- **Considered**: traditional registration flow
- **Discarded because**: too much friction for a personal blog; readers won't create yet another account just to leave a comment

### Pre-Moderation (Approve Before Publish)
- **Considered**: require author approval before comments become visible
- **Discarded because**: delays conversation, discourages interaction, adds operational burden for a single-author blog

## Open Questions for Next Phase

1. **OAuth provider setup**: How to configure Google and GitHub OAuth within the €0 budget? (both offer free tiers for OAuth)
2. **Like deduplication**: Cookie vs localStorage vs fingerprinting — which approach best balances privacy and effectiveness?
3. **Web Share API coverage**: What percentage of target audience browsers support it? What's the best fallback?
4. **Comment data model**: How does Comment relate to BlogPost aggregate? Separate aggregate or child entity?
5. **Frontend interactivity**: How to add targeted JS (likes, comments, share) to an otherwise static Astro site? (Astro Islands)

## Next Step

Product discovery (`nw:discover`) or architecture design (`nw:design`) to validate assumptions and define the technical approach.

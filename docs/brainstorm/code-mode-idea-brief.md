# Idea Brief: Code Mode — IDE-style Code Blocks

## One-Sentence Summary

Replace the broken regex markdown parser with a proper renderer + Shiki syntax highlighter to display code blocks as IDE-like panels with syntax coloring, line numbers, language badge, file title bar, and copy button.

## Problem Statement

Code blocks in blog posts are **indistinguishable from surrounding text**. The current regex-based markdown-to-HTML parser in `[slug].astro` fails to properly render fenced code blocks (` ``` `), leaving them as inline monospace text with visible backtick markers. For a Software Craftsmanship blog where code *is* the primary argument, this makes posts unreadable and unprofessional.

### Evidence

Screenshot from `/blog/the-three-laws-of-tdd-why-order-matters`:
- C# code appears inline, mixed with prose
- Backtick markers (` ``` `) are visible as raw text
- No visual separation between code and explanation
- No syntax highlighting — keywords, strings, types all same color
- Reader cannot scan the post and immediately locate code examples

## User & Context

### Who reads this blog
- **Software developers** interested in TDD, DDD, Clean Architecture, XP
- They read code as fluently as prose — code blocks are the core content
- They expect the same code reading experience as their IDE or GitHub

### Technical context
- Frontend: **Astro 5** (SSR-capable, Shiki built-in for its markdown pipeline)
- Content source: raw markdown strings from the **.NET backend API** (not Astro content collections)
- Design system: **Forge & Ink** — warm editorial aesthetic, JetBrains Mono already in font stack
- Current parser: hand-rolled regex in `[slug].astro` frontmatter — fragile, incomplete

### Constraints
- No dogmatic "zero JS" constraint — JS is acceptable where it adds value (e.g. copy button)
- Must work with content fetched from API at request time (not static markdown files)
- Should respect the Forge & Ink dark/light theme system (CSS variables)

## Desired Experience

When a reader encounters a code block in a post, they should experience a clear **context switch** — from reading prose to reading code. The code block should feel like a panel from an IDE embedded in the article.

### Must Have
| Feature | Description |
|---|---|
| **Syntax highlighting** | Language-aware coloring (keywords, strings, comments, types) via Shiki |
| **Line numbers** | Subtle, non-selectable, left-aligned |
| **Language badge** | Small label showing "C#", "bash", "yaml", etc. |
| **Window chrome** | Title bar with macOS-style dots (red/yellow/green) and optional filename |
| **Copy button** | One-click copy to clipboard (the only JS needed) |
| **Proper parsing** | Fenced code blocks correctly converted to `<pre><code>` |

### Nice to Have (defer to later)
- Line highlighting (highlight specific lines to draw attention)
- Diff view (showing before/after in refactoring posts)
- Collapsible long blocks
- "Open in IDE" link

## Alternatives Explored

### A. Shiki via Astro's built-in markdown pipeline
- **Discarded because**: content comes from API as raw strings, not from Astro content collections or `.md` files. The built-in pipeline doesn't apply.

### B. Client-side highlighting (Prism.js / Highlight.js)
- **Discarded because**: flash of unstyled content, unnecessary JS payload for something that can be done server-side. Shiki produces pre-colored HTML at build/request time.

### C. Shiki called manually + keep regex parser
- **Discarded because**: the regex parser is fundamentally broken (can't handle multi-line fenced blocks, backtick escaping, nested formatting). Patching it is a losing game.

### D. Replace regex parser with proper markdown renderer + Shiki (CHOSEN)
- **Rationale**: fixes the parsing bug *and* adds the IDE experience in one coherent change. Uses `marked` (fast, well-maintained) for markdown-to-HTML, and `shiki` (already an Astro dependency) for syntax highlighting. Both run server-side in Astro's frontmatter.

## Technical Approach (high-level)

```
Current:  API → raw markdown string → regex parser → broken HTML
Proposed: API → raw markdown string → marked (with shiki plugin) → rich HTML with syntax-highlighted code blocks
```

### Key components
1. **`marked` + custom renderer** — replaces the regex parser in `[slug].astro`
2. **`shiki` highlighter** — called by the custom renderer for fenced code blocks
3. **CSS for IDE chrome** — window dots, language badge, line numbers (pure CSS)
4. **Copy button** — small inline `<script>` (the only client JS)
5. **Theme integration** — Shiki theme that matches Forge & Ink CSS variables (or a custom theme)

### Files affected
- `frontend/src/pages/blog/[slug].astro` — replace regex parser with marked + shiki
- `frontend/src/styles/global.css` — update `.prose-forge pre` styles for IDE chrome
- `frontend/package.json` — add `marked` dependency (shiki likely already present via Astro)

## Scope Boundary

**In scope**: rendering code blocks beautifully in blog post detail pages.
**Out of scope**: code editing, interactive execution, REPL, syntax validation, code block in blog list excerpts.

## Success Criteria

- [ ] Fenced code blocks render as distinct IDE-like panels
- [ ] Syntax highlighting works for C#, TypeScript, bash, yaml, json, sql
- [ ] Line numbers visible and non-selectable
- [ ] Language badge shown in top-right corner
- [ ] Window chrome (dots + optional filename) in title bar
- [ ] Copy button copies code content to clipboard
- [ ] Dark/light theme supported
- [ ] No flash of unstyled content (highlighting is server-side)
- [ ] Existing prose formatting (bold, italic, blockquotes, headings) still works correctly

## Next Step

Implementation — either directly or via `nw:design` for the visual component, then `nw:deliver` for TDD execution.

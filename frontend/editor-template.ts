// editor-template.ts — Standalone editor HTML template (Forge & Ink design, no Astro/Tailwind)

export interface BuildHtmlOptions {
  filePath: string;
  title: string;
  tags: string[];
  postId: string | null;
  scheduledAt: string | null;
  initialContent: string;    // raw (with frontmatter) → textarea
  renderedHtml: string;      // rendered HTML of the body
  headings: Array<{text: string, id: string}>;
  readingTime: string;
  editMode: boolean;
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

// ── Tag badge helper ──────────────────────────────────

function buildTagBadge(tag: string): string {
  const slug = tag.toLowerCase().replace(/\s+/g, '-');
  return `<span class="tag-badge-md tag--${escapeHtml(slug)}">${escapeHtml(tag)}</span>`;
}

// ── TOC helper ────────────────────────────────────────

function buildTocHtml(headings: Array<{text: string, id: string}>): string {
  if (headings.length <= 1) return '';
  const links = headings
    .map(h => `<a href="#${escapeHtml(h.id)}" class="toc-link">${escapeHtml(h.text)}</a>`)
    .join('\n              ');
  return `<aside class="toc-sidebar">
            <nav class="toc-nav" aria-label="Table of contents">
              <p class="toc-label">On this page</p>
              ${links}
            </nav>
          </aside>`;
}

// ── Site header HTML ──────────────────────────────────

function buildNavHtml(): string {
  return `<header class="site-header">
    <nav class="site-nav">
      <a href="/" class="logo-link" aria-label="Home">
        <div class="craft-mark">
          <div class="craft-mark-outer"></div>
          <div class="craft-mark-inner"></div>
        </div>
        <span class="logo-text">The Augmented Craftsman</span>
      </a>
      <div class="nav-links">
        <a href="/" class="nav-link">Home</a>
        <a href="/blog" class="nav-link nav-link--active">Writing</a>
        <a href="/tags" class="nav-link">Topics</a>
        <a href="/about" class="nav-link">About</a>
        <button id="theme-toggle" class="theme-toggle" aria-label="Toggle dark mode">
          <svg class="theme-icon-sun" width="20" height="20" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
            <circle cx="12" cy="12" r="5"/>
            <path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/>
          </svg>
          <svg class="theme-icon-moon" width="20" height="20" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
            <path d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z"/>
          </svg>
        </button>
      </div>
    </nav>
  </header>`;
}

// ── Site footer HTML ──────────────────────────────────

function buildFooterHtml(year: number): string {
  return `<footer class="site-footer">
    <div class="site-footer-inner">
      <div class="site-footer-grid">
        <div class="footer-brand">
          <div class="footer-logo">
            <div class="footer-craft-mark">
              <div class="footer-craft-mark-outer"></div>
              <div class="footer-craft-mark-inner"></div>
            </div>
            <span class="footer-logo-text">The Augmented Craftsman</span>
          </div>
          <p class="footer-tagline">Crafting software with intention.<br/>TDD. Clean Architecture. Deliberate practice.</p>
        </div>
        <div class="footer-links">
          <div class="footer-links-col">
            <p class="footer-links-heading">Navigate</p>
            <a href="/blog" class="footer-link">Writing</a>
            <a href="/tags" class="footer-link">Topics</a>
            <a href="/about" class="footer-link">About</a>
          </div>
          <div class="footer-links-col">
            <p class="footer-links-heading">Connect</p>
            <a href="https://github.com/christianBorrello" target="_blank" rel="noopener" class="footer-link footer-link-icon">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0 0 24 12c0-6.63-5.37-12-12-12Z"/></svg>
              GitHub
            </a>
            <a href="https://www.linkedin.com/in/christianborrello99/" target="_blank" rel="noopener" class="footer-link footer-link-icon">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286ZM5.337 7.433a2.062 2.062 0 0 1-2.063-2.065 2.064 2.064 0 1 1 2.063 2.065Zm1.782 13.019H3.555V9h3.564v11.452ZM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003Z"/></svg>
              LinkedIn
            </a>
            <a href="https://christianborrello.dev/en" target="_blank" rel="noopener" class="footer-link footer-link-icon">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="8" r="4"/><path d="M4 20c0-4 3.6-7 8-7s8 3 8 7"/></svg>
              Who I Am
            </a>
            <a href="/rss.xml" class="footer-link footer-link-icon">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 11a9 9 0 0 1 9 9"/><path d="M4 4a16 16 0 0 1 16 16"/><circle cx="5" cy="19" r="1"/></svg>
              RSS Feed
            </a>
          </div>
        </div>
      </div>
      <div class="footer-bottom">
        <p>&copy; ${year} Christian Borrello. Built with craft.</p>
        <p class="footer-bottom-right">Made with <span class="footer-craft-icon"><span class="footer-craft-icon-inner"></span></span> and Astro</p>
      </div>
    </div>
  </footer>`;
}

// ── CSS (self-contained, class-based dark mode) ───────

const CSS = `
@import url('https://fonts.googleapis.com/css2?family=Fraunces:ital,opsz,wght@0,9..144,300..900;1,9..144,300..900&family=JetBrains+Mono:wght@400;500;600&family=Literata:ital,opsz,wght@0,7..72,300..800;1,7..72,300..800&display=swap');

/* ── Design Tokens (light) ─────────────────────────── */
:root {
  --color-bg: #FDFAF6;
  --color-bg-surface: #F5F0E8;
  --color-text: #1C1917;
  --color-text-muted: #6B5F4F;
  --color-accent: #B45309;
  --color-accent-hover: #D97706;
  --color-border: #D4C8B8;
  --grain-opacity: 0.03;
  --color-bg-alpha80: rgba(253,250,246,0.8);
}

/* ── Design Tokens (dark) — class-based ────────────── */
.dark {
  --color-bg: #0F0E0C;
  --color-bg-surface: #1A1816;
  --color-text: #E7E0D6;
  --color-text-muted: #B0A08A;
  --color-accent: #E5A84B;
  --color-accent-hover: #F0C060;
  --color-border: #2E2A25;
  --grain-opacity: 0.04;
  --color-bg-alpha80: rgba(15,14,12,0.8);
}

/* ── Base ──────────────────────────────────────────── */
*, *::before, *::after { box-sizing: border-box; }

html {
  scroll-behavior: smooth;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

body {
  font-family: 'Literata', Georgia, serif;
  background-color: var(--color-bg);
  color: var(--color-text);
  margin: 0;
  padding-top: 36px;
  transition: background-color 0.3s ease, color 0.3s ease;
}

body::before {
  content: '';
  position: fixed;
  inset: 0;
  z-index: 9999;
  pointer-events: none;
  opacity: var(--grain-opacity);
  background-image: url("data:image/svg+xml,%3Csvg viewBox='0 0 256 256' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='noise'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23noise)'/%3E%3C/svg%3E");
  background-repeat: repeat;
  background-size: 256px 256px;
}

::selection { background-color: var(--color-accent); color: white; }

/* ── Editor Strip (fixed, always visible) ──────────── */
.editor-strip {
  position: fixed;
  top: 0; left: 0; right: 0;
  height: 36px;
  z-index: 200;
  background: var(--color-accent);
  display: flex;
  align-items: center;
  gap: 0.625rem;
  padding: 0 1rem;
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.6875rem;
}

.strip-draft {
  font-weight: 700;
  color: white;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  flex-shrink: 0;
}

.strip-filepath {
  color: rgba(255,255,255,0.8);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
  max-width: 50vw;
}

.strip-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-left: auto;
  flex-shrink: 0;
}

.strip-btn {
  padding: 0.2rem 0.6rem;
  background: rgba(255,255,255,0.15);
  color: white;
  border: 1px solid rgba(255,255,255,0.3);
  border-radius: 3px;
  cursor: pointer;
  font-family: inherit;
  font-size: inherit;
  font-weight: 500;
  transition: background 0.15s ease;
  white-space: nowrap;
}

.strip-btn:hover { background: rgba(255,255,255,0.25); }
.strip-btn--active { background: rgba(255,255,255,0.3); font-weight: 700; }

.strip-save-status { color: white; min-width: 4ch; }
.strip-cmd-hint { color: rgba(255,255,255,0.7); }

/* ── Site Header (sticky below strip) ─────────────── */
.site-header {
  position: sticky;
  top: 36px;
  z-index: 50;
  background: var(--color-bg-alpha80);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border-bottom: 1px solid var(--color-border);
}

.site-nav {
  max-width: 80rem;
  margin: 0 auto;
  padding: 0 1.5rem;
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

@media (min-width: 768px) { .site-nav { padding: 0 2.5rem; } }

.logo-link {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  text-decoration: none;
}

.craft-mark { width: 32px; height: 32px; position: relative; flex-shrink: 0; }
.craft-mark-outer { position: absolute; inset: 0; border: 2px solid var(--color-accent); border-radius: 2px; }
.craft-mark-inner { position: absolute; inset: 4px; border: 2px solid var(--color-text); border-radius: 2px; opacity: 0.4; }

.logo-text {
  font-family: 'Fraunces', Georgia, serif;
  font-weight: 600;
  font-size: 1.125rem;
  letter-spacing: -0.02em;
  color: var(--color-text);
}

.nav-links { display: flex; align-items: center; gap: 0.25rem; }
@media (min-width: 768px) { .nav-links { gap: 0.5rem; } }

.nav-link {
  padding: 0.375rem 0.75rem;
  border-radius: 8px;
  font-family: 'Literata', Georgia, serif;
  font-size: 0.9375rem;
  color: var(--color-text-muted);
  text-decoration: none;
  transition: all 0.2s ease;
}

.nav-link:hover { color: var(--color-text); background: var(--color-bg-surface); }
.nav-link--active { color: var(--color-accent); background: var(--color-bg-surface); }

.theme-toggle {
  margin-left: 0.5rem;
  padding: 0.5rem;
  border-radius: 8px;
  background: transparent;
  border: none;
  color: var(--color-text-muted);
  cursor: pointer;
  display: flex;
  align-items: center;
  transition: all 0.2s ease;
}

.theme-toggle:hover { color: var(--color-text); background: var(--color-bg-surface); }

.theme-icon-sun  { display: none; }
.dark .theme-icon-sun  { display: block; }
.theme-icon-moon { display: block; }
.dark .theme-icon-moon { display: none; }

/* ── Post Article ──────────────────────────────────── */
@keyframes fadeUp {
  from { opacity: 0; transform: translateY(24px); }
  to   { opacity: 1; transform: none; }
}

.post-article {
  padding: 3rem 1.5rem 0;
  max-width: 80rem;
  margin: 0 auto;
}

@media (min-width: 768px) {
  .post-article { padding-top: 5rem; padding-left: 2.5rem; padding-right: 2.5rem; }
}

.post-inner { max-width: 56rem; margin: 0 auto; }

.post-header {
  margin-bottom: 3rem;
  animation: fadeUp 0.6s ease both;
}

@media (min-width: 768px) { .post-header { margin-bottom: 4rem; } }

.back-link {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.9375rem;
  color: var(--color-text-muted);
  text-decoration: none;
  margin-bottom: 2rem;
  transition: color 0.2s ease;
}

.back-link:hover { color: var(--color-accent); }

.post-meta {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.9375rem;
  color: var(--color-text-muted);
  margin-bottom: 1rem;
}

.post-meta-dot {
  width: 4px; height: 4px;
  border-radius: 50%;
  background: currentColor;
  display: inline-block;
  flex-shrink: 0;
}

.post-title-display {
  font-family: 'Fraunces', Georgia, serif;
  font-weight: 600;
  font-size: clamp(2rem, 4vw, 3.5rem);
  line-height: 1.1;
  letter-spacing: -0.025em;
  color: var(--color-text);
  margin: 0;
}

@media (min-width: 768px) {
  .post-title-display {
    font-size: clamp(3rem, 6vw, 5.5rem);
    line-height: 1.05;
    letter-spacing: -0.03em;
  }
}

/* ── Tag Badges ────────────────────────────────────── */
.post-tags { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-top: 1.5rem; }

.tag-badge-md {
  display: inline-flex;
  align-items: center;
  border-radius: 6px;
  border: 1px solid var(--color-border);
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.875rem;
  padding: 0.375rem 0.75rem;
  background: var(--color-bg-surface);
  color: var(--color-text-muted);
  text-decoration: none;
  transition: all 0.2s ease;
}

.tag--tdd               { background:rgba(217,119,6,.1);   color:#D97706; border-color:rgba(217,119,6,.2); }
.dark .tag--tdd         { background:rgba(251,191,36,.1);  color:#FBBF24; border-color:rgba(251,191,36,.2); }
.tag--clean-architecture               { background:rgba(5,150,105,.1);   color:#059669; border-color:rgba(5,150,105,.2); }
.dark .tag--clean-architecture         { background:rgba(52,211,153,.1);  color:#34D399; border-color:rgba(52,211,153,.2); }
.tag--ddd               { background:rgba(124,58,237,.1);  color:#7C3AED; border-color:rgba(124,58,237,.2); }
.dark .tag--ddd         { background:rgba(167,139,250,.1); color:#A78BFA; border-color:rgba(167,139,250,.2); }
.tag--solid             { background:rgba(2,132,199,.1);   color:#0284C7; border-color:rgba(2,132,199,.2); }
.dark .tag--solid       { background:rgba(56,189,248,.1);  color:#38BDF8; border-color:rgba(56,189,248,.2); }
.tag--refactoring       { background:rgba(225,29,72,.1);   color:#E11D48; border-color:rgba(225,29,72,.2); }
.dark .tag--refactoring { background:rgba(251,113,133,.1); color:#FB7185; border-color:rgba(251,113,133,.2); }
.tag--csharp            { background:rgba(79,70,229,.1);   color:#4F46E5; border-color:rgba(79,70,229,.2); }
.dark .tag--csharp      { background:rgba(129,140,248,.1); color:#818CF8; border-color:rgba(129,140,248,.2); }
.tag--testing           { background:rgba(13,148,136,.1);  color:#0D9488; border-color:rgba(13,148,136,.2); }
.dark .tag--testing     { background:rgba(45,212,204,.1);  color:#2DD4BF; border-color:rgba(45,212,204,.2); }
.tag--xp                { background:rgba(234,88,12,.1);   color:#EA580C; border-color:rgba(234,88,12,.2); }
.dark .tag--xp          { background:rgba(251,146,60,.1);  color:#FB923C; border-color:rgba(251,146,60,.2); }

/* ── Two-column layout ─────────────────────────────── */
.post-content { display: flex; gap: 3rem; }
@media (min-width: 1024px) { .post-content { gap: 4rem; } }

.prose-wrapper {
  flex: 1;
  min-width: 0;
  animation: fadeUp 0.6s ease both;
  animation-delay: 150ms;
}

/* ── TOC Sidebar ───────────────────────────────────── */
.toc-sidebar { display: none; width: 14rem; flex-shrink: 0; }
@media (min-width: 1024px) { .toc-sidebar { display: block; } }

.toc-nav {
  position: sticky;
  top: 124px; /* 36px strip + 64px header + 24px padding */
}

.toc-label {
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: var(--color-text-muted);
  margin: 0 0 0.75rem;
}

.toc-link {
  display: block;
  font-family: 'Literata', Georgia, serif;
  font-size: 0.9375rem;
  line-height: 1.7;
  color: var(--color-text-muted);
  text-decoration: none;
  padding: 0.25rem 0.75rem;
  border-left: 2px solid var(--color-border);
  transition: color 0.2s ease, border-color 0.2s ease;
}

.toc-link:hover { color: var(--color-accent); border-left-color: var(--color-accent); }

/* ── Post Footer ───────────────────────────────────── */
.post-footer {
  margin-top: 4rem;
  padding-top: 2rem;
  border-top: 1px solid var(--color-border);
}

.post-footer-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.post-author-title {
  font-family: 'Literata', Georgia, serif;
  font-size: 0.9375rem;
  color: var(--color-text-muted);
  margin: 0;
}

.post-author-title strong { color: var(--color-text); }

.post-author-role {
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.75rem;
  color: var(--color-text-muted);
  margin: 0.25rem 0 0;
}

.more-posts-link {
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.9375rem;
  color: var(--color-accent);
  text-decoration: none;
}

.more-posts-link:hover { text-decoration: underline; }

/* ── Comments Placeholder ──────────────────────────── */
.comments-section { margin-top: 3rem; padding-bottom: 2rem; }

.comments-heading {
  font-family: 'Fraunces', Georgia, serif;
  font-weight: 600;
  font-size: 1.5rem;
  letter-spacing: -0.02em;
  color: var(--color-text);
  margin: 0 0 1.5rem;
}

.comment-signin {
  background: var(--color-bg-surface);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: 2rem;
  text-align: center;
}

.comments-subtitle {
  font-family: 'Fraunces', Georgia, serif;
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--color-text);
  margin: 0 0 0.5rem;
}

.comments-note {
  font-family: 'Literata', Georgia, serif;
  font-size: 0.9375rem;
  color: var(--color-text-muted);
  margin: 0 0 1.5rem;
}

.oauth-buttons { display: flex; justify-content: center; gap: 0.75rem; flex-wrap: wrap; }

.oauth-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1.25rem;
  border-radius: 6px;
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: not-allowed;
  opacity: 0.5;
  border: 1px solid var(--color-border);
  background: var(--color-bg);
  color: var(--color-text);
}

/* ── Site Footer ───────────────────────────────────── */
.site-footer { border-top: 1px solid var(--color-border); margin-top: 6rem; }

.site-footer-inner { max-width: 80rem; margin: 0 auto; padding: 3rem 1.5rem; }
@media (min-width: 768px) { .site-footer-inner { padding: 4rem 2.5rem; } }

.site-footer-grid { display: flex; flex-direction: column; align-items: flex-start; gap: 2rem; }
@media (min-width: 768px) {
  .site-footer-grid { flex-direction: row; align-items: center; justify-content: space-between; }
}

.footer-brand { display: flex; flex-direction: column; gap: 0.75rem; }
.footer-logo { display: flex; align-items: center; gap: 0.75rem; }

.footer-craft-mark { width: 24px; height: 24px; position: relative; flex-shrink: 0; }
.footer-craft-mark-outer { position: absolute; inset: 0; border: 2px solid var(--color-accent); border-radius: 2px; }
.footer-craft-mark-inner { position: absolute; inset: 2px; border: 1px solid var(--color-text); border-radius: 2px; opacity: 0.3; }

.footer-logo-text {
  font-family: 'Fraunces', Georgia, serif;
  font-weight: 600;
  letter-spacing: -0.02em;
  color: var(--color-text);
}

.footer-tagline {
  font-family: 'Literata', Georgia, serif;
  font-size: 0.9375rem;
  line-height: 1.7;
  color: var(--color-text-muted);
  max-width: 20rem;
  margin: 0;
}

.footer-links { display: flex; gap: 3rem; font-size: 0.9375rem; }
.footer-links-col { display: flex; flex-direction: column; gap: 0.5rem; }

.footer-links-heading {
  font-family: 'Fraunces', Georgia, serif;
  font-weight: 600;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: var(--color-text-muted);
  margin: 0 0 0.25rem;
}

.footer-link { color: var(--color-text-muted); text-decoration: none; transition: color 0.2s ease; }
.footer-link:hover { color: var(--color-accent); }
.footer-link-icon { display: flex; align-items: center; gap: 0.5rem; }

.footer-bottom {
  margin-top: 3rem;
  padding-top: 1.5rem;
  border-top: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.75rem;
  color: var(--color-text-muted);
}

@media (min-width: 640px) { .footer-bottom { flex-direction: row; } }
.footer-bottom p { margin: 0; }
.footer-bottom-right { display: flex; align-items: center; gap: 0.375rem; }
.footer-craft-icon { display: inline-block; width: 12px; height: 12px; position: relative; }
.footer-craft-icon-inner { position: absolute; inset: 0; border: 1px solid var(--color-accent); border-radius: 2px; transform: rotate(45deg); }

/* ── Edit Container ────────────────────────────────── */
.edit-container { min-height: calc(100vh - 36px); display: flex; flex-direction: column; }

.editor-textarea {
  flex: 1;
  width: 100%;
  padding: 1.5rem 2rem;
  background: var(--color-bg-surface);
  color: var(--color-text);
  border: none;
  font-family: 'JetBrains Mono', monospace;
  font-size: 1rem;
  line-height: 1.7;
  resize: none;
  outline: none;
  box-sizing: border-box;
  caret-color: var(--color-accent);
  tab-size: 2;
}
.editor-textarea:focus {
  box-shadow: inset 3px 0 0 var(--color-accent);
}

/* ── Markdown Toolbar ──────────────────────────────── */
.md-toolbar {
  flex-shrink: 0;
  height: 32px;
  display: flex;
  align-items: center;
  padding: 0 0.75rem;
  background: var(--color-bg-surface);
  border-bottom: 1px solid var(--color-border);
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.75rem;
  overflow-x: auto;
  scrollbar-width: none;
  user-select: none;
  gap: 0;
}
.md-toolbar::-webkit-scrollbar { display: none; }
.md-toolbar-group { display: flex; align-items: center; gap: 1px; }
.md-toolbar-sep { width: 1px; height: 16px; background: var(--color-border); margin: 0 0.5rem; flex-shrink: 0; }

.md-btn {
  padding: 0.25rem 0.5rem;
  background: transparent;
  color: var(--color-text-muted);
  border: 1px solid transparent;
  border-radius: 4px;
  cursor: pointer;
  font-family: inherit;
  font-size: inherit;
  line-height: 1;
  transition: background 0.1s, color 0.1s, border-color 0.1s;
  white-space: nowrap;
}
.md-btn:hover { background: var(--color-bg); color: var(--color-accent); border-color: var(--color-border); }
.md-btn:active { background: var(--color-accent); color: white; border-color: var(--color-accent); }

/* ── Editor Statusbar ──────────────────────────────── */
.editor-statusbar {
  flex-shrink: 0;
  height: 26px;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0 1rem;
  background: var(--color-bg);
  border-top: 1px solid var(--color-border);
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.6875rem;
  color: var(--color-text-muted);
}
.status-sep { opacity: 0.4; }
.status-right { margin-left: auto; opacity: 0.5; font-size: 0.625rem; }

/* ── Editor Meta Row ───────────────────────────────── */
.editor-meta-row {
  flex-shrink: 0;
  height: 30px;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0 0.75rem;
  background: var(--color-bg-surface);
  border-bottom: 1px solid var(--color-border);
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.75rem;
  color: var(--color-text-muted);
}
.meta-label { opacity: 0.65; flex-shrink: 0; }
.meta-sep   { opacity: 0.35; flex-shrink: 0; }
.meta-postid {
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.75rem;
  color: var(--color-text-muted);
  letter-spacing: 0.02em;
  transition: color 0.15s ease;
}
.meta-postid:hover { color: var(--color-accent); }
.meta-datetime {
  height: 22px;
  padding: 0 0.4rem;
  background: var(--color-bg);
  color: var(--color-text);
  border: 1px solid var(--color-border);
  border-radius: 3px;
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.75rem;
  outline: none;
  cursor: pointer;
}
.meta-datetime:focus { border-color: var(--color-accent); }
.dark .meta-datetime { color-scheme: dark; }

/* ── Prose Forge ───────────────────────────────────── */
.prose-forge {
  font-family: 'Literata', Georgia, serif;
  font-size: 1.0625rem;
  line-height: 1.8;
  color: var(--color-text);
  max-width: 68ch;
}

.prose-forge h2 {
  font-family: 'Fraunces', Georgia, serif;
  font-weight: 600;
  font-size: clamp(1.5rem, 3vw, 2rem);
  letter-spacing: -0.02em;
  margin-top: 3rem;
  margin-bottom: 1rem;
  color: var(--color-text);
}

.prose-forge h3 {
  font-family: 'Fraunces', Georgia, serif;
  font-weight: 500;
  font-size: 1.35rem;
  letter-spacing: -0.015em;
  margin-top: 2.5rem;
  margin-bottom: 0.75rem;
}

.prose-forge p { margin-bottom: 1.5rem; }

.prose-forge a {
  color: var(--color-accent);
  text-decoration: underline;
  text-decoration-color: var(--color-accent);
  text-decoration-thickness: 1px;
  text-underline-offset: 3px;
  transition: text-decoration-color 0.2s ease;
}

.prose-forge a:hover { text-decoration-thickness: 2px; }

.prose-forge blockquote {
  border-left: 3px solid var(--color-accent);
  padding-left: 1.5rem;
  margin: 2rem 0;
  font-style: italic;
  color: var(--color-text-muted);
  font-size: 1.125rem;
}

.prose-forge ul, .prose-forge ol { margin-bottom: 1.5rem; padding-left: 1.5rem; }
.prose-forge li { margin-bottom: 0.5rem; }
.prose-forge li::marker { color: var(--color-accent); }
.prose-forge strong { font-weight: 700; color: var(--color-text); }
.prose-forge em { font-style: italic; }

.prose-forge hr { border: none; height: 1px; background: var(--color-border); margin: 3rem 0; }

.prose-forge code:not(pre code) {
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.875em;
  padding: 0.15em 0.4em;
  border-radius: 4px;
  background-color: var(--color-bg-surface);
  border: 1px solid var(--color-border);
  color: var(--color-accent);
}

.prose-forge img { border-radius: 12px; margin: 2rem 0; border: 1px solid var(--color-border); max-width: 100%; }

/* ── Shiki Dual Theme (class-based) ────────────────── */
.shiki, .shiki span {
  color: var(--shiki-light) !important;
  font-style: var(--shiki-light-font-style) !important;
}

.dark .shiki, .dark .shiki span {
  color: var(--shiki-dark) !important;
  font-style: var(--shiki-dark-font-style) !important;
}

/* ── Code Blocks ───────────────────────────────────── */
.code-block {
  margin: 1.75rem 0;
  border-radius: 12px;
  border: 1px solid var(--color-border);
  background: var(--code-block-bg);
  overflow: hidden;
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.8125rem;
  line-height: 1.7;
  --code-block-bg: #F5F0E8;
  --code-block-chrome-bg: #EDE7DB;
  --code-block-gutter-text: #B0A08A;
  --code-block-gutter-border: #D4C8B8;
  --code-block-code-text: #3C3836;
  --code-block-badge-bg: #D4C8B8;
  --code-block-badge-text: #6B5F4F;
  --code-block-copy-text: #8A7E72;
  --code-block-filename-text: #6B5F4F;
}

.dark .code-block {
  --code-block-bg: #141210;
  --code-block-chrome-bg: #141210;
  --code-block-gutter-text: #4A4139;
  --code-block-gutter-border: #2E2A25;
  --code-block-code-text: #A9B7C6;
  --code-block-badge-bg: #2E2A25;
  --code-block-badge-text: #B0A08A;
  --code-block-copy-text: #6B5F4F;
  --code-block-filename-text: #B0A08A;
}

.code-block__chrome {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.625rem 1rem;
  background: var(--code-block-chrome-bg);
  border-bottom: 1px solid var(--color-border);
  user-select: none;
}

.code-block__dots { display: flex; gap: 6px; }
.code-block__dot { width: 10px; height: 10px; border-radius: 50%; }
.code-block__dot--close    { background: #FF5F57; }
.code-block__dot--minimize { background: #FEBC2E; }
.code-block__dot--maximize { background: #28C840; }

.code-block__filename { font-size: 0.6875rem; color: var(--code-block-filename-text); letter-spacing: 0.02em; }
.code-block__actions { display: flex; align-items: center; gap: 0.75rem; }

.code-block__badge {
  font-size: 0.625rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  padding: 0.15rem 0.5rem;
  border-radius: 4px;
  background: var(--code-block-badge-bg);
  color: var(--code-block-badge-text);
}

.code-block__copy {
  background: transparent;
  border: none;
  color: var(--code-block-copy-text);
  cursor: pointer;
  padding: 0.25rem;
  border-radius: 4px;
  display: flex;
  align-items: center;
  transition: color 0.15s ease, background 0.15s ease;
}

.code-block__copy:hover { color: var(--color-accent); background: var(--code-block-badge-bg); }
.code-block__copy svg { width: 14px; height: 14px; }
.code-block__copy--copied { color: #6A8759 !important; }
.code-block__body { display: flex; overflow-x: auto; }

.code-block__gutter {
  flex-shrink: 0;
  padding: 1rem 0;
  text-align: right;
  color: var(--code-block-gutter-text);
  background: var(--code-block-bg);
  border-right: 1px solid var(--code-block-gutter-border);
  user-select: none;
  -webkit-user-select: none;
  min-width: 3rem;
}

.code-block__gutter span { display: block; padding: 0 0.75rem; font-size: 0.75rem; line-height: 1.7; }
.code-block__code { flex: 1; padding: 1rem 1.25rem; overflow-x: auto; color: var(--code-block-code-text); }

.code-block__code pre {
  margin: 0 !important; padding: 0 !important;
  background: transparent !important; border: none !important; border-radius: 0 !important;
  font-family: inherit; font-size: inherit; line-height: inherit;
}

.code-block__code pre code {
  font-family: inherit; font-size: inherit; line-height: inherit;
  background: transparent !important; padding: 0 !important; border: none !important; color: inherit;
}

.code-block--minimal .code-block__chrome   { padding: 0.5rem 1rem; }
.code-block--minimal .code-block__dots     { display: none; }
.code-block--minimal .code-block__filename { display: none; }
`;

// ── Vanilla JS ────────────────────────────────────────

function buildScript(filePath: string, initialMode: string, postId: string | null, scheduledAt: string | null): string {
  return `(function () {
  var FILE_PATH = ${JSON.stringify(filePath)};
  var DEBOUNCE_MS = 1500;
  var mode = ${JSON.stringify(initialMode)};
  var debounceTimer = null;

  var modeToggle   = document.getElementById('mode-toggle');
  var refreshBtn   = document.getElementById('refresh-btn');
  var saveStatus   = document.getElementById('save-status');
  var cmdHint      = document.getElementById('cmd-hint');
  var pageChrome   = document.getElementById('page-chrome');
  var editContainer= document.getElementById('edit-container');
  var proseContent = document.getElementById('prose-content');
  var editArea     = document.getElementById('edit-area');

  // ── Theme toggle ───────────────────────────────────
  var themeToggle = document.getElementById('theme-toggle');
  if (themeToggle) {
    themeToggle.addEventListener('click', function () {
      var isDark = document.documentElement.classList.toggle('dark');
      localStorage.setItem('theme', isDark ? 'dark' : 'light');
    });
  }

  // ── Mode switching ─────────────────────────────────
  function setMode(newMode) {
    mode = newMode;
    if (newMode === 'edit') {
      pageChrome.style.display    = 'none';
      editContainer.style.display = 'flex';
      modeToggle.textContent      = '\\u2190 Preview';
      modeToggle.classList.add('strip-btn--active');
      refreshBtn.style.display    = '';
      cmdHint.style.display       = '';
    } else {
      pageChrome.style.display    = 'block';
      editContainer.style.display = 'none';
      modeToggle.textContent      = 'Edit';
      modeToggle.classList.remove('strip-btn--active');
      refreshBtn.style.display    = 'none';
      cmdHint.style.display       = 'none';
      saveStatus.textContent      = '';
    }
  }

  modeToggle.addEventListener('click', function () {
    setMode(mode === 'edit' ? 'preview' : 'edit');
  });

  // ── Auto-save ──────────────────────────────────────
  function doSave() {
    saveStatus.textContent = 'Saving\\u2026';
    saveStatus.style.color = 'rgba(255,255,255,0.7)';
    fetch('/file', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ filePath: FILE_PATH, content: editArea.value }),
    }).then(function (res) {
      if (res.ok) {
        saveStatus.textContent = 'Saved \\u2713';
        saveStatus.style.color = 'white';
      } else {
        saveStatus.textContent = 'Error \\u2717';
        saveStatus.style.color = 'white';
      }
    }).catch(function () {
      saveStatus.textContent = 'Error \\u2717';
      saveStatus.style.color = 'white';
    });
  }

  editArea.addEventListener('input', function () {
    saveStatus.textContent = '';
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(doSave, DEBOUNCE_MS);
  });

  // ── Copy buttons ───────────────────────────────────
  function wireCopyButtons() {
    document.querySelectorAll('.code-block__copy').forEach(function (btn) {
      var fresh = btn.cloneNode(true);
      btn.parentNode.replaceChild(fresh, btn);
      fresh.addEventListener('click', function () {
        var block  = fresh.closest('.code-block');
        var codeEl = block && block.querySelector('.code-block__code');
        var text   = codeEl ? (codeEl.textContent || '') : '';
        navigator.clipboard.writeText(text).then(function () {
          fresh.classList.add('code-block__copy--copied');
          setTimeout(function () { fresh.classList.remove('code-block__copy--copied'); }, 1500);
        }).catch(function () {});
      });
    });
  }

  // ── Preview refresh ────────────────────────────────
  function applyRenderedHtml(html) {
    var parser = new DOMParser();
    var doc    = parser.parseFromString(html, 'text/html');
    var nodes  = Array.from(doc.body.childNodes);
    proseContent.replaceChildren.apply(proseContent, nodes);
  }

  function doRefreshPreview() {
    fetch('/render', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ content: editArea.value }),
    }).then(function (res) {
      return res.ok ? res.json() : null;
    }).then(function (data) {
      if (!data) return;
      applyRenderedHtml(data.html);
      wireCopyButtons();
      setMode('preview');
    }).catch(function () {});
  }

  refreshBtn.addEventListener('click', doRefreshPreview);

  // ── Markdown toolbar helpers ────────────────────────
  function wrapOrInsert(before, after, placeholder) {
    var start = editArea.selectionStart;
    var end   = editArea.selectionEnd;
    var sel   = editArea.value.substring(start, end) || placeholder;
    editArea.focus();
    document.execCommand('insertText', false, before + sel + after);
    var newStart = start + before.length;
    editArea.setSelectionRange(newStart, newStart + sel.length);
    editArea.dispatchEvent(new Event('input'));
  }

  var TOOLBAR_ACTIONS = {
    h1:        function() { wrapOrInsert('\\n# ',   '',    'Heading 1'); },
    h2:        function() { wrapOrInsert('\\n## ',  '',    'Heading 2'); },
    h3:        function() { wrapOrInsert('\\n### ', '',    'Heading 3'); },
    bold:      function() { wrapOrInsert('**',  '**',    'bold text'); },
    italic:    function() { wrapOrInsert('*',   '*',     'italic text'); },
    code:      function() { wrapOrInsert('\`',   '\`',     'code'); },
    codeblock: function() { wrapOrInsert('\\n\`\`\`\\n', '\\n\`\`\`\\n', 'language'); },
    link:      function() {
      var url = prompt('URL:', 'https://');
      if (url) wrapOrInsert('[', '](' + url + ')', 'link text');
    },
    quote:     function() { wrapOrInsert('\\n> ', '', 'quoted text'); },
    hr:        function() { wrapOrInsert('\\n\\n---\\n\\n', '', ''); },
  };

  document.getElementById('md-toolbar').addEventListener('click', function(e) {
    var btn = e.target.closest('.md-btn');
    if (!btn) return;
    var action = btn.dataset.action;
    if (TOOLBAR_ACTIONS[action]) TOOLBAR_ACTIONS[action]();
  });

  // ── Tab → 2 spaces ─────────────────────────────────
  editArea.addEventListener('keydown', function(e) {
    if (e.key === 'Tab') {
      e.preventDefault();
      document.execCommand('insertText', false, '  ');
    }
  });

  // ── Status bar ──────────────────────────────────────
  function updateStatus() {
    var text  = editArea.value;
    var words = text.trim() === '' ? 0 : text.trim().split(/\\s+/).length;
    var mins  = Math.max(1, Math.round(words / 200));
    var beforeCursor = text.substring(0, editArea.selectionStart);
    var line  = beforeCursor.split('\\n').length;
    var col   = editArea.selectionStart - beforeCursor.lastIndexOf('\\n');
    document.getElementById('status-words').textContent     = words + ' words';
    document.getElementById('status-read-time').textContent = '~' + mins + ' min read';
    document.getElementById('status-cursor').textContent    = line + ':' + col;
  }
  editArea.addEventListener('input', updateStatus);
  editArea.addEventListener('click', updateStatus);
  editArea.addEventListener('keyup', updateStatus);
  updateStatus();

  // ── Keyboard shortcuts ──────────────────────────────
  document.addEventListener('keydown', function(e) {
    if (mode !== 'edit') return;
    var mod = e.metaKey || e.ctrlKey;
    if (!mod) return;
    switch (e.key) {
      case 's': e.preventDefault(); doRefreshPreview(); break;
      case 'b': e.preventDefault(); TOOLBAR_ACTIONS.bold();   break;
      case 'i': e.preventDefault(); TOOLBAR_ACTIONS.italic(); break;
      case 'e': e.preventDefault(); TOOLBAR_ACTIONS.code();   break;
      case 'k': e.preventDefault(); TOOLBAR_ACTIONS.link();   break;
      case '1': e.preventDefault(); TOOLBAR_ACTIONS.h1();     break;
      case '2': e.preventDefault(); TOOLBAR_ACTIONS.h2();     break;
      case '3': e.preventDefault(); TOOLBAR_ACTIONS.h3();     break;
    }
  });

  // ── Meta row: scheduledAt picker ───────────────────
  var SCHEDULED_AT = ${JSON.stringify(scheduledAt)};
  var scheduledInput = document.getElementById('scheduled-input');

  if (scheduledInput) {
    function defaultScheduledLocal() {
      var d = new Date();
      d.setDate(d.getDate() + 1);
      d.setHours(9, 0, 0, 0);
      var p = function(n) { return n.toString().padStart(2, '0'); };
      return d.getFullYear()+'-'+p(d.getMonth()+1)+'-'+p(d.getDate())+'T09:00';
    }

    scheduledInput.value = SCHEDULED_AT
      ? SCHEDULED_AT.replace(/Z$/, '').substring(0, 16)
      : defaultScheduledLocal();

    scheduledInput.addEventListener('change', function () {
      var isoVal = scheduledInput.value + ':00Z';
      var text = editArea.value;
      if (/^scheduledAt:/m.test(text)) {
        editArea.value = text.replace(/^scheduledAt:.*$/m, 'scheduledAt: ' + isoVal);
      } else {
        editArea.value = text.replace(/^(---\\n[\\s\\S]*?)(\\n---)/, '$1\\nscheduledAt: ' + isoVal + '$2');
      }
      editArea.dispatchEvent(new Event('input'));
    });
  }

  // ── Meta row: postId copy ──────────────────────────
  var POST_ID_TEXT = ${JSON.stringify(postId ?? '—')};
  var postIdEl = document.getElementById('meta-postid');
  if (postIdEl && POST_ID_TEXT !== '—') {
    postIdEl.style.cursor = 'pointer';
    postIdEl.addEventListener('click', function () {
      navigator.clipboard.writeText(POST_ID_TEXT).then(function () {
        postIdEl.textContent = 'Copied!';
        setTimeout(function () { postIdEl.textContent = POST_ID_TEXT; }, 1200);
      }).catch(function () {});
    });
  }

  setMode(mode);
  wireCopyButtons();
}());`;
}

// ── Public API ────────────────────────────────────────

export function buildHtml(opts: BuildHtmlOptions): string {
  const { filePath, title, tags, postId, scheduledAt, initialContent, renderedHtml, headings, readingTime, editMode } = opts;

  const initialMode     = editMode ? 'edit' : 'preview';
  const previewDisplay  = editMode ? 'none' : 'block';
  const editDisplay     = editMode ? 'flex' : 'none';
  const toggleText      = editMode ? '\u2190 Preview' : 'Edit';
  const toggleActive    = editMode ? ' strip-btn--active' : '';
  const refreshDisplay  = editMode ? '' : 'none';
  const hintDisplay     = editMode ? '' : 'none';

  const year     = new Date().getFullYear();
  const basename = filePath.split('/').pop() ?? filePath;

  const tagsHtml   = tags.length > 0
    ? `<div class="post-tags">${tags.map(buildTagBadge).join('')}</div>`
    : '';
  const tocHtml    = buildTocHtml(headings);
  const navHtml    = buildNavHtml();
  const footerHtml = buildFooterHtml(year);

  // Inline theme script in <head> prevents FOUC on reload
  const themeScript = `(function(){var t=localStorage.getItem('theme')||(window.matchMedia('(prefers-color-scheme:dark)').matches?'dark':'light');document.documentElement.classList.toggle('dark',t==='dark');})();`;

  return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>[DRAFT] ${escapeHtml(title)} \u2014 The Augmented Craftsman</title>
<style>${CSS}</style>
<script>${themeScript}<\/script>
</head>
<body>

<!-- Editor Strip (fixed, always visible) -->
<div class="editor-strip">
  <span class="strip-draft">\u26a0 DRAFT</span>
  <span class="strip-filepath">${escapeHtml(basename)}</span>
  <div class="strip-actions">
    <button id="mode-toggle" class="strip-btn${toggleActive}">${escapeHtml(toggleText)}</button>
    <button id="refresh-btn" class="strip-btn" style="display:${refreshDisplay}">Refresh</button>
    <span id="save-status" class="strip-save-status"></span>
    <span id="cmd-hint" class="strip-cmd-hint" style="display:${hintDisplay}">Cmd+S to refresh</span>
  </div>
</div>

<!-- Page Chrome (preview mode) -->
<div id="page-chrome" style="display:${previewDisplay}">

  ${navHtml}

  <article class="post-article">
    <div class="post-inner">

      <header class="post-header">
        <a href="/blog" class="back-link">
          <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path d="M19 12H5M12 19l-7-7 7-7"/>
          </svg>
          Back to all posts
        </a>

        <div class="post-meta">
          <span>Draft</span>
          <span class="post-meta-dot"></span>
          <span>${escapeHtml(readingTime)}</span>
        </div>

        <h1 class="post-title-display">${escapeHtml(title)}</h1>

        ${tagsHtml}
      </header>

      <div class="post-content">
        <div class="prose-wrapper">
          <div id="prose-content" class="prose-forge">${renderedHtml}</div>
        </div>
        ${tocHtml}
      </div>

      <footer class="post-footer">
        <div class="post-footer-inner">
          <div>
            <p class="post-author-title">Written by <strong>Christian Borrello</strong></p>
            <p class="post-author-role">Software Engineer &middot; Craftsman &middot; Lifelong Learner</p>
          </div>
          <a href="/blog" class="more-posts-link">More posts &rarr;</a>
        </div>
      </footer>

      <div class="comments-section">
        <h3 class="comments-heading">Comments</h3>
        <div class="comment-signin">
          <p class="comments-subtitle">Join the conversation</p>
          <p class="comments-note">Sign in to leave a comment on this post.</p>
          <div class="oauth-buttons">
            <button class="oauth-btn" disabled>
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="currentColor"><path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0 0 24 12c0-6.63-5.37-12-12-12Z"/></svg>
              Sign in with GitHub
            </button>
          </div>
        </div>
      </div>

    </div>
  </article>

  ${footerHtml}

</div>

<!-- Edit Container -->
<div id="edit-container" class="edit-container" style="display:${editDisplay}">

  <div class="md-toolbar" id="md-toolbar">
    <div class="md-toolbar-group">
      <button class="md-btn" data-action="h1" title="Heading 1 (Cmd+1)">H1</button>
      <button class="md-btn" data-action="h2" title="Heading 2 (Cmd+2)">H2</button>
      <button class="md-btn" data-action="h3" title="Heading 3 (Cmd+3)">H3</button>
    </div>
    <span class="md-toolbar-sep"></span>
    <div class="md-toolbar-group">
      <button class="md-btn" data-action="bold"   title="Bold (Cmd+B)"><strong>B</strong></button>
      <button class="md-btn" data-action="italic" title="Italic (Cmd+I)"><em>I</em></button>
      <button class="md-btn" data-action="code"   title="Inline code (Cmd+E)">&#96;c&#96;</button>
    </div>
    <span class="md-toolbar-sep"></span>
    <div class="md-toolbar-group">
      <button class="md-btn" data-action="codeblock" title="Code block">&#96;&#96;&#96;</button>
      <button class="md-btn" data-action="link"      title="Link (Cmd+K)">[L]</button>
      <button class="md-btn" data-action="quote"     title="Blockquote">&gt;</button>
      <button class="md-btn" data-action="hr"        title="Horizontal rule">—</button>
    </div>
  </div>

  <div class="editor-meta-row" id="editor-meta-row">
    <span class="meta-label">Post ID</span>
    <span class="meta-postid" id="meta-postid" title="${postId ? 'Click to copy' : ''}">${escapeHtml(postId ?? '—')}</span>
    <span class="meta-sep">·</span>
    <label class="meta-label" for="scheduled-input">Scheduled</label>
    <input type="datetime-local" id="scheduled-input" class="meta-datetime">
  </div>

  <textarea id="edit-area" class="editor-textarea" spellcheck="false"
  >${escapeHtml(initialContent)}</textarea>

  <div class="editor-statusbar" id="editor-statusbar">
    <span id="status-words">0 words</span>
    <span class="status-sep">·</span>
    <span id="status-read-time">~1 min read</span>
    <span class="status-sep">·</span>
    <span id="status-cursor">1:1</span>
    <span class="status-right">Cmd+B/I/E · Cmd+S = preview</span>
  </div>

</div>

<script>${buildScript(filePath, initialMode, postId, scheduledAt)}<\/script>
</body>
</html>`;
}

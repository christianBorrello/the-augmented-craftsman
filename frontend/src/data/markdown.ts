import { Marked } from 'marked';
import { createHighlighter, type Highlighter } from 'shiki';

// ── JetBrains Darcula Theme (Dark) ───────────────────

const darculaTheme = {
  name: 'darcula',
  type: 'dark' as const,
  colors: {
    'editor.background': '#141210',
    'editor.foreground': '#A9B7C6',
  },
  settings: [
    { settings: { foreground: '#A9B7C6' } },
    {
      scope: ['keyword', 'storage', 'storage.type', 'storage.modifier', 'constant.language', 'variable.language'],
      settings: { foreground: '#CC7832' },
    },
    {
      scope: ['string', 'string.quoted'],
      settings: { foreground: '#6A8759' },
    },
    {
      scope: ['comment', 'punctuation.definition.comment'],
      settings: { foreground: '#808080', fontStyle: 'italic' },
    },
    {
      scope: ['entity.name.function', 'support.function'],
      settings: { foreground: '#FFC66D' },
    },
    {
      scope: ['constant.numeric'],
      settings: { foreground: '#6897BB' },
    },
    {
      scope: ['entity.name.tag', 'meta.attribute', 'entity.other.attribute-name', 'punctuation.definition.annotation', 'storage.type.annotation'],
      settings: { foreground: '#BBB529' },
    },
    {
      scope: ['variable.other.property', 'support.type.property-name', 'variable.other.object.property'],
      settings: { foreground: '#9876AA' },
    },
    {
      scope: ['entity.name.type', 'entity.name.class', 'support.type', 'support.class'],
      settings: { foreground: '#A9B7C6' },
    },
    {
      scope: ['keyword.operator'],
      settings: { foreground: '#A9B7C6' },
    },
    {
      scope: ['punctuation'],
      settings: { foreground: '#A9B7C6' },
    },
  ],
};

// ── Forge & Ink Light Theme ──────────────────────────

const forgeInkLight = {
  name: 'forge-ink-light',
  type: 'light' as const,
  colors: {
    'editor.background': '#F5F0E8',
    'editor.foreground': '#3C3836',
  },
  settings: [
    { settings: { foreground: '#3C3836' } },
    {
      scope: ['keyword', 'storage', 'storage.type', 'storage.modifier', 'constant.language', 'variable.language'],
      settings: { foreground: '#9D3A14' },
    },
    {
      scope: ['string', 'string.quoted'],
      settings: { foreground: '#527A2C' },
    },
    {
      scope: ['comment', 'punctuation.definition.comment'],
      settings: { foreground: '#8A7E72', fontStyle: 'italic' },
    },
    {
      scope: ['entity.name.function', 'support.function'],
      settings: { foreground: '#B5762A' },
    },
    {
      scope: ['constant.numeric'],
      settings: { foreground: '#2E6B8A' },
    },
    {
      scope: ['entity.name.tag', 'meta.attribute', 'entity.other.attribute-name', 'punctuation.definition.annotation', 'storage.type.annotation'],
      settings: { foreground: '#7B6C1A' },
    },
    {
      scope: ['variable.other.property', 'support.type.property-name', 'variable.other.object.property'],
      settings: { foreground: '#7B5EA7' },
    },
    {
      scope: ['entity.name.type', 'entity.name.class', 'support.type', 'support.class'],
      settings: { foreground: '#3C3836' },
    },
    {
      scope: ['keyword.operator'],
      settings: { foreground: '#3C3836' },
    },
    {
      scope: ['punctuation'],
      settings: { foreground: '#5C5550' },
    },
  ],
};

// ── Language Display Names ───────────────────────────

const LANG_DISPLAY: Record<string, string> = {
  csharp: 'C#', cs: 'C#',
  typescript: 'TypeScript', ts: 'TypeScript',
  javascript: 'JavaScript', js: 'JavaScript',
  bash: 'Bash', shell: 'Bash', sh: 'Bash',
  json: 'JSON', yaml: 'YAML', yml: 'YAML',
  sql: 'SQL', html: 'HTML', css: 'CSS',
  text: 'Text', plaintext: 'Text', txt: 'Text',
};

// ── Highlighter Singleton ────────────────────────────

let highlighterPromise: Promise<Highlighter> | null = null;

function getHighlighter(): Promise<Highlighter> {
  if (!highlighterPromise) {
    highlighterPromise = createHighlighter({
      themes: [darculaTheme, forgeInkLight],
      langs: ['csharp', 'typescript', 'javascript', 'bash', 'json', 'yaml', 'sql', 'html', 'css'],
    });
  }
  return highlighterPromise;
}

// ── Code Block HTML Builder ──────────────────────────

const COPY_ICON = `<svg fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect><path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1"></path></svg>`;

function buildCodeBlock(highlightedHtml: string, lang: string, filename: string, lineCount: number): string {
  const displayLang = LANG_DISPLAY[lang] ?? lang.toUpperCase();
  const isMinimal = !lang || lang === 'text' || lang === 'plaintext' || lang === 'txt';

  const gutter = Array.from({ length: lineCount }, (_, i) => `<span>${i + 1}</span>`).join('');

  const dotsHtml = isMinimal ? '' : `
    <div class="code-block__dots">
      <span class="code-block__dot code-block__dot--close"></span>
      <span class="code-block__dot code-block__dot--minimize"></span>
      <span class="code-block__dot code-block__dot--maximize"></span>
    </div>`;

  const filenameHtml = filename ? `<span class="code-block__filename">${filename}</span>` : '<span class="code-block__filename"></span>';

  return `<div class="code-block${isMinimal ? ' code-block--minimal' : ''}">
  <div class="code-block__chrome">
    ${dotsHtml}
    ${filenameHtml}
    <div class="code-block__actions">
      <span class="code-block__badge">${displayLang}</span>
      <button class="code-block__copy" title="Copy code">${COPY_ICON}</button>
    </div>
  </div>
  <div class="code-block__body">
    <div class="code-block__gutter">${gutter}</div>
    <div class="code-block__code">${highlightedHtml}</div>
  </div>
</div>`;
}

// ── Parse lang:filename Convention ───────────────────

function parseLangTag(raw: string): { lang: string; filename: string } {
  if (!raw) return { lang: 'text', filename: '' };
  const colonIndex = raw.indexOf(':');
  if (colonIndex === -1) return { lang: raw, filename: '' };
  return { lang: raw.slice(0, colonIndex), filename: raw.slice(colonIndex + 1) };
}

// ── Shared Slug Function ─────────────────────────────

export function slugifyHeading(text: string): string {
  return text.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
}

// ── Public API ───────────────────────────────────────

export async function renderMarkdown(content: string): Promise<string> {
  const highlighter = await getHighlighter();
  const supportedLangs = highlighter.getLoadedLanguages();

  const marked = new Marked();

  marked.use({
    renderer: {
      heading({ text, depth }) {
        const id = slugifyHeading(text);
        return `<h${depth} id="${id}">${text}</h${depth}>\n`;
      },
      code({ text, lang: rawLang }) {
        const { lang, filename } = parseLangTag(rawLang ?? '');
        const lineCount = text.split('\n').length;
        const effectiveLang = supportedLangs.includes(lang) ? lang : 'text';

        let highlighted: string;
        if (effectiveLang !== 'text') {
          highlighted = highlighter.codeToHtml(text, {
            lang: effectiveLang,
            themes: { light: 'forge-ink-light', dark: 'darcula' },
            defaultColor: false,
          });
        } else {
          const escaped = text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
          highlighted = `<pre class="shiki" style="background-color:transparent"><code>${escaped}</code></pre>`;
        }

        return buildCodeBlock(highlighted, lang, filename, lineCount);
      },
    },
  });

  return marked.parse(content);
}

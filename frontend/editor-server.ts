// editor-server.ts — Standalone tac-editor HTTP server (no Astro dev server required)
// Binds to 127.0.0.1:3456 (loopback only — not reachable from the network)

import * as http from 'node:http';
import { readFile, writeFile, mkdir, rename, unlink } from 'node:fs/promises';
import { isAbsolute, basename, dirname, join } from 'node:path';
import { renderMarkdown, slugifyHeading } from './src/data/markdown';
import { buildHtml } from './editor-template';

const PORT = 3456;
const HOST = '127.0.0.1';

// ── Frontmatter helpers ─────────────────────────────

interface Frontmatter {
  body: string;
  title: string;
  tags: string[];
  postId: string | null;
  scheduledAt: string | null;
}

function parseFrontmatter(raw: string): Frontmatter {
  const match = raw.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n?([\s\S]*)$/);
  if (!match) return { body: raw, title: 'Untitled', tags: [], postId: null, scheduledAt: null };

  const yaml = match[1];
  const body = match[2];

  const titleMatch       = yaml.match(/^title:\s*["']?(.+?)["']?\s*$/m);
  const tagsMatch        = yaml.match(/^tags:\s*\[([^\]]*)\]/m);
  const postIdMatch      = yaml.match(/^postId:\s*(.+?)\s*$/m);
  const scheduledAtMatch = yaml.match(/^scheduledAt:\s*(.+?)\s*$/m);

  const title       = titleMatch?.[1] ?? 'Untitled Preview';
  const tags        = tagsMatch?.[1]
    ? tagsMatch[1].split(',').map(t => t.trim().replace(/^["']|["']$/g, '')).filter(Boolean)
    : [];
  const postId      = postIdMatch?.[1] ?? null;
  const scheduledAt = scheduledAtMatch?.[1] ?? null;

  return { body, title, tags, postId, scheduledAt };
}

// ── Security validation ─────────────────────────────

function isValidPath(filePath: unknown): filePath is string {
  if (typeof filePath !== 'string') return false;
  if (!isAbsolute(filePath)) return false;
  if (filePath.includes('..')) return false;
  if (!filePath.endsWith('.md')) return false;
  return true;
}

// ── Request body reader ─────────────────────────────

function readBody(req: http.IncomingMessage): Promise<string> {
  return new Promise((resolve, reject) => {
    const chunks: Buffer[] = [];
    req.on('data', (chunk: Buffer) => chunks.push(chunk));
    req.on('end', () => resolve(Buffer.concat(chunks).toString('utf-8')));
    req.on('error', reject);
  });
}

// ── Route handlers ──────────────────────────────────

async function handleEdit(req: http.IncomingMessage, res: http.ServerResponse, url: URL): Promise<void> {
  const filePath = url.searchParams.get('file');
  if (!isValidPath(filePath)) {
    res.writeHead(400, { 'Content-Type': 'text/plain' });
    res.end('Invalid file path');
    return;
  }

  let raw: string;
  try {
    raw = await readFile(filePath, 'utf-8');
  } catch {
    res.writeHead(404, { 'Content-Type': 'text/plain' });
    res.end('File not found');
    return;
  }

  const { body, title, tags, postId, scheduledAt } = parseFrontmatter(raw);
  const renderedHtml = await renderMarkdown(body);
  const editMode = url.searchParams.has('edit');

  const headings = body.split('\n')
    .filter(line => line.startsWith('## ') || line.startsWith('### '))
    .map(line => {
      const text = line.replace(/^#+\s+/, '');
      return { text, id: slugifyHeading(text) };
    });

  const words = body.split(/\s+/).filter(Boolean).length;
  const readingTime = `${Math.max(1, Math.ceil(words / 200))} min read`;

  const isDraft = filePath.includes('/drafts/');
  const html = buildHtml({ filePath, title, tags, postId, scheduledAt, initialContent: raw, renderedHtml, headings, readingTime, editMode, isDraft });
  res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
  res.end(html);
}

async function handleRender(req: http.IncomingMessage, res: http.ServerResponse): Promise<void> {
  const bodyText = await readBody(req);
  let content: string;
  try {
    content = JSON.parse(bodyText)?.content ?? '';
  } catch {
    res.writeHead(400, { 'Content-Type': 'text/plain' });
    res.end('Invalid JSON');
    return;
  }

  const { body } = parseFrontmatter(content);
  const html = await renderMarkdown(body);
  res.writeHead(200, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({ html }));
}

async function handleFilePut(req: http.IncomingMessage, res: http.ServerResponse): Promise<void> {
  const bodyText = await readBody(req);
  let parsed: { filePath?: unknown; content?: unknown };
  try {
    parsed = JSON.parse(bodyText);
  } catch {
    res.writeHead(400, { 'Content-Type': 'text/plain' });
    res.end('Invalid JSON');
    return;
  }

  if (!isValidPath(parsed.filePath)) {
    res.writeHead(400, { 'Content-Type': 'text/plain' });
    res.end('Invalid file path');
    return;
  }

  const content = typeof parsed.content === 'string' ? parsed.content : '';
  await writeFile(parsed.filePath, content, 'utf-8');
  res.writeHead(200, { 'Content-Type': 'text/plain' });
  res.end('ok');
}

async function handleMarkReady(req: http.IncomingMessage, res: http.ServerResponse): Promise<void> {
  const bodyText = await readBody(req);
  let parsed: { filePath?: unknown };
  try {
    parsed = JSON.parse(bodyText);
  } catch {
    res.writeHead(400, { 'Content-Type': 'text/plain' });
    res.end('Invalid JSON');
    return;
  }

  if (!isValidPath(parsed.filePath) || !parsed.filePath.includes('/drafts/')) {
    res.writeHead(400, { 'Content-Type': 'text/plain' });
    res.end('File must be in drafts/');
    return;
  }

  const readyDir = dirname(parsed.filePath).replace('/drafts', '/ready');
  await mkdir(readyDir, { recursive: true });
  const newPath = join(readyDir, basename(parsed.filePath));
  await rename(parsed.filePath, newPath);

  res.writeHead(200, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({ newPath }));
}

async function handleDeleteFile(req: http.IncomingMessage, res: http.ServerResponse): Promise<void> {
  const bodyText = await readBody(req);
  let parsed: { filePath?: unknown };
  try {
    parsed = JSON.parse(bodyText);
  } catch {
    res.writeHead(400, { 'Content-Type': 'text/plain' });
    res.end('Invalid JSON');
    return;
  }

  if (!isValidPath(parsed.filePath) || !parsed.filePath.includes('/drafts/')) {
    res.writeHead(400, { 'Content-Type': 'text/plain' });
    res.end('Only draft files can be deleted');
    return;
  }

  await unlink(parsed.filePath);
  res.writeHead(200, { 'Content-Type': 'text/plain' });
  res.end('ok');
}

// ── Server ──────────────────────────────────────────

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url ?? '/', `http://${HOST}:${PORT}`);

  try {
    if (req.method === 'GET' && url.pathname === '/health') {
      res.writeHead(200, { 'Content-Type': 'text/plain' });
      res.end('ok');
      return;
    }

    if (req.method === 'GET' && url.pathname === '/edit') {
      await handleEdit(req, res, url);
      return;
    }

    if (req.method === 'POST' && url.pathname === '/render') {
      await handleRender(req, res);
      return;
    }

    if (req.method === 'PUT' && url.pathname === '/file') {
      await handleFilePut(req, res);
      return;
    }

    if (req.method === 'POST' && url.pathname === '/mark-ready') {
      await handleMarkReady(req, res);
      return;
    }

    if (req.method === 'POST' && url.pathname === '/delete-file') {
      await handleDeleteFile(req, res);
      return;
    }

    res.writeHead(404, { 'Content-Type': 'text/plain' });
    res.end('Not found');
  } catch (err) {
    console.error('[tac-editor] Unhandled error:', err);
    res.writeHead(500, { 'Content-Type': 'text/plain' });
    res.end('Internal server error');
  }
});

server.listen(PORT, HOST, () => {
  console.log(`tac-editor @ http://${HOST}:${PORT}`);
  // Eager warm-up: trigger Shiki highlighter initialisation so the first /edit
  // request returns immediately instead of waiting for language bundle loading.
  renderMarkdown('```ts\nconst x = 1;\n```').catch(() => {});
});

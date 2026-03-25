export const prerender = false;

import type { APIRoute } from 'astro';
import { renderMarkdown } from '../../data/markdown';

export const POST: APIRoute = async ({ request }) => {
  if (import.meta.env.PROD) {
    return new Response('Not available in production', { status: 403 });
  }

  const { content } = await request.json();

  // Strip YAML frontmatter — same logic as preview.astro
  const match = (content as string).match(/^---\r?\n[\s\S]*?\r?\n---\r?\n?([\s\S]*)$/);
  const body = match?.[1] ?? (content as string);

  const html = await renderMarkdown(body);
  return new Response(JSON.stringify({ html }), {
    headers: { 'Content-Type': 'application/json' },
  });
};

import { defineAction, ActionError } from 'astro:actions';
import { z } from 'astro:schema';

const API = import.meta.env.PUBLIC_API_URL || 'http://localhost:5063';

export const server = {
  postComment: defineAction({
    accept: 'form',
    input: z.object({
      slug: z.string(),
      text: z.string().min(1, 'Comment cannot be empty').max(2000),
    }),
    handler: async ({ slug, text }, context) => {
      const cookie = context.request.headers.get('cookie') || '';
      const res = await fetch(`${API}/api/posts/${slug}/comments`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', cookie },
        body: JSON.stringify({ text }),
      });

      if (res.status === 401) {
        throw new ActionError({ code: 'UNAUTHORIZED', message: 'Session expired. Please sign in again.' });
      }

      if (!res.ok) {
        throw new ActionError({ code: 'BAD_REQUEST', message: 'Failed to post comment.' });
      }

      return await res.json();
    },
  }),
};

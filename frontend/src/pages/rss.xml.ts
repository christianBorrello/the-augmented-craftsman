import type { APIContext } from 'astro';
import rss from '@astrojs/rss';
import { fetchPosts } from '../data/api';

export async function GET(context: APIContext) {
  const posts = await fetchPosts();

  return rss({
    title: 'The Augmented Craftsman',
    description: 'Crafting software with intention — TDD, Clean Architecture, DDD, and the pursuit of elegant code.',
    site: context.site!.href,
    items: posts.map(post => ({
      title: post.title,
      description: post.excerpt,
      pubDate: new Date(post.date),
      link: `/blog/${post.slug}`,
      categories: post.tags.map(tag => tag.name),
    })),
  });
}

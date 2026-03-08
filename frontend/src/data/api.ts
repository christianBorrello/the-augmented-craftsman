const API_BASE = import.meta.env.PUBLIC_API_URL || 'http://localhost:5063';

interface ApiPost {
  id?: string;
  title: string;
  slug: string;
  content?: string;
  status?: string;
  createdAt?: string;
  updatedAt?: string;
  publishedAt: string | null;
  featuredImageUrl: string | null;
  tags: string[] | null;
}

export interface BlogPost {
  slug: string;
  title: string;
  excerpt: string;
  content: string;
  date: string;
  tags: string[];
  readingTime: string;
  featured: boolean;
}

export interface TagInfo {
  name: string;
  slug: string;
  postCount: number;
}

function estimateReadingTime(content: string): string {
  const words = content.split(/\s+/).length;
  const minutes = Math.max(1, Math.ceil(words / 200));
  return `${minutes} min read`;
}

function extractExcerpt(content: string): string {
  const plain = content
    .replace(/```[\s\S]*?```/g, '')
    .replace(/#{1,3}\s.+/g, '')
    .replace(/\*\*(.+?)\*\*/g, '$1')
    .replace(/`([^`]+)`/g, '$1')
    .replace(/>\s.+/g, '')
    .replace(/\n{2,}/g, '\n')
    .trim();
  const firstParagraph = plain.split('\n').find(line => line.trim().length > 30) || plain;
  return firstParagraph.slice(0, 200).trim() + (firstParagraph.length > 200 ? '...' : '');
}

function toFrontendPost(post: ApiPost): BlogPost {
  return {
    slug: post.slug,
    title: post.title,
    content: post.content ?? '',
    excerpt: post.content ? extractExcerpt(post.content) : '',
    date: (post.publishedAt ?? post.createdAt ?? '').split('T')[0],
    tags: post.tags ?? [],
    readingTime: post.content ? estimateReadingTime(post.content) : '',
    featured: false,
  };
}

export async function fetchPosts(): Promise<BlogPost[]> {
  const response = await fetch(`${API_BASE}/api/posts`);
  if (!response.ok) throw new Error(`Failed to fetch posts: ${response.status} ${response.statusText}`);
  const posts: ApiPost[] = await response.json();
  return posts.map(toFrontendPost);
}

export async function fetchPostBySlug(slug: string): Promise<BlogPost | null> {
  const response = await fetch(`${API_BASE}/api/posts/${slug}`);
  if (response.status === 404) return null;
  if (!response.ok) throw new Error(`Failed to fetch post ${slug}: ${response.status} ${response.statusText}`);
  const post: ApiPost = await response.json();
  return toFrontendPost(post);
}

export async function fetchTags(): Promise<TagInfo[]> {
  const response = await fetch(`${API_BASE}/api/tags`);
  if (!response.ok) throw new Error(`Failed to fetch tags: ${response.status} ${response.statusText}`);
  return response.json();
}

export async function fetchPostsByTag(tagSlug: string): Promise<BlogPost[]> {
  const response = await fetch(`${API_BASE}/api/posts?tag=${encodeURIComponent(tagSlug)}`);
  if (!response.ok) throw new Error(`Failed to fetch posts for tag ${tagSlug}: ${response.status} ${response.statusText}`);
  const posts: ApiPost[] = await response.json();
  return posts.map(toFrontendPost);
}

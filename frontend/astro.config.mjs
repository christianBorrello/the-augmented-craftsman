import { defineConfig } from 'astro/config';
import tailwind from '@astrojs/tailwind';

import preact from '@astrojs/preact';

import vercel from '@astrojs/vercel';

export default defineConfig({
  site: 'https://theaugmentedcraftsman.christianborrello.dev',
  integrations: [tailwind(), preact()],
  prefetch: true,
  output: 'static',
  adapter: vercel(),
});
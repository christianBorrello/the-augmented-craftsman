import { defineConfig } from 'astro/config';
import tailwindcss from '@tailwindcss/vite';
import preact from '@astrojs/preact';
import vercel from '@astrojs/vercel';

export default defineConfig({
  site: 'https://theaugmentedcraftsman.christianborrello.dev',
  integrations: [preact()],
  prefetch: true,
  output: 'static',
  adapter: vercel(),
  security: {
    checkOrigin: true,
  },
  vite: {
    plugins: [tailwindcss()],
  },
});
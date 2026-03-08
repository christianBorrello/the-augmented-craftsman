import { defineConfig } from 'astro/config';
import tailwind from '@astrojs/tailwind';

export default defineConfig({
  site: 'https://theaugmentedcraftsman.christianborrello.dev',
  integrations: [tailwind()],
  prefetch: true,
  output: 'static',
});

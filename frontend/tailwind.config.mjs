/** @type {import('tailwindcss').Config} */
export default {
  content: ['./src/**/*.{astro,html,js,jsx,md,mdx,svelte,ts,tsx,vue}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        forge: {
          50: '#FDFAF6',
          100: '#F5F0E8',
          200: '#E8DFD0',
          300: '#D4C8B8',
          400: '#B0A08A',
          500: '#8A7A66',
          600: '#6B5F4F',
          700: '#4A4139',
          800: '#2E2A25',
          900: '#1A1816',
          950: '#0F0E0C',
        },
        amber: {
          400: '#E5A84B',
          500: '#D97706',
          600: '#B45309',
          700: '#92400E',
        },
        ink: {
          DEFAULT: '#1C1917',
          light: '#E7E0D6',
        },
      },
      fontFamily: {
        display: ['"Fraunces"', '"Playfair Display"', 'Georgia', 'serif'],
        body: ['"Literata"', '"Source Serif 4"', 'Georgia', 'serif'],
        mono: ['"JetBrains Mono"', '"Fira Code"', 'monospace'],
      },
      fontSize: {
        'display-xl': ['clamp(3rem, 6vw, 5.5rem)', { lineHeight: '1.05', letterSpacing: '-0.03em' }],
        'display-lg': ['clamp(2rem, 4vw, 3.5rem)', { lineHeight: '1.1', letterSpacing: '-0.025em' }],
        'display-md': ['clamp(1.5rem, 3vw, 2.25rem)', { lineHeight: '1.15', letterSpacing: '-0.02em' }],
        'body-lg': ['1.25rem', { lineHeight: '1.8' }],
        'body': ['1.0625rem', { lineHeight: '1.8' }],
        'body-sm': ['0.9375rem', { lineHeight: '1.7' }],
      },
      maxWidth: {
        'prose': '68ch',
        'wide': '80rem',
      },
      spacing: {
        '18': '4.5rem',
        '22': '5.5rem',
      },
      animation: {
        'fade-up': 'fadeUp 0.6s ease-out both',
        'fade-in': 'fadeIn 0.5s ease-out both',
        'slide-in': 'slideIn 0.4s ease-out both',
      },
      keyframes: {
        fadeUp: {
          '0%': { opacity: '0', transform: 'translateY(24px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideIn: {
          '0%': { opacity: '0', transform: 'translateX(-12px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
      },
    },
  },
  plugins: [],
};

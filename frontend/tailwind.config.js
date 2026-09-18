import typography from '@tailwindcss/typography'

/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  future: {
    hoverOnlyWhenSupported: true
  },
  content: [
    './components/**/*.{js,vue,ts}',
    './layouts/**/*.vue',
    './pages/**/*.vue',
    './plugins/**/*.{js,ts}',
    './app.vue',
    './error.vue'
  ],
  theme: {
    extend: {
      colors: {
        canvas: {
          DEFAULT: '#09090b',
          subtle: '#121215',
          elevated: '#18181b',
          border: 'rgba(255, 255, 255, 0.08)'
        },
        brand: {
          50: '#f5f3ff',
          100: '#ede9fe',
          200: '#ddd6fe',
          300: '#c4b5fd',
          400: '#a78bfa',
          500: '#7c3aed',
          600: '#6d28d9',
          700: '#5b21b6',
          800: '#4c1d95',
          900: '#3b0764',
          950: '#2e1065',
          glow: 'rgba(124, 58, 237, 0.25)'
        },
        streak: {
          amber: '#f59e0b',
          glow: 'rgba(245, 158, 11, 0.4)'
        },
        cyber: {
          400: '#22d3ee',
          500: '#06b6d4'
        },
        surface: {
          50: '#f8fafc',
          100: '#f1f5f9',
          200: '#e2e8f0',
          300: '#cbd5e1',
          400: '#94a3b8',
          500: '#64748b',
          600: '#475569',
          700: '#334155',
          800: '#1e293b',
          900: '#0f172a',
          950: '#020617'
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
        serif: ['Merriweather', 'Georgia', 'Cambria', 'Times New Roman', 'serif'],
        mono: ['JetBrains Mono', 'Fira Code', 'monospace']
      },
      spacing: {
        84: '21rem'
      },
      lineHeight: {
        inherit: 'inherit'
      }
    }
  },
  plugins: [typography]
}

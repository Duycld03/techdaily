// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  future: {
    compatibilityVersion: 4
  },
  compatibilityDate: '2024-11-01',
  devtools: { enabled: false },
  typescript: {
    shim: true
  },

  devServer: {
    host: '0.0.0.0',
    port: 3000
  },

  modules: [
    '@nuxtjs/tailwindcss',
    '@nuxtjs/color-mode',
    '@nuxtjs/i18n',
    '@pinia/nuxt',
    '@vueuse/nuxt'
  ],

  colorMode: {
    classSuffix: '',
    preference: 'dark', // Dark mode first by default
    fallback: 'dark'
  },

  i18n: {
    locales: [
      { code: 'en', iso: 'en-US', name: 'English', file: 'en.json' },
      { code: 'vi', iso: 'vi-VN', name: 'Tiếng Việt', file: 'vi.json' }
    ],
    defaultLocale: 'en',
    strategy: 'no_prefix',
    lazy: true,
    langDir: 'locales',
    bundle: {
      optimizeTranslationDirective: false
    }
  },

  // Cross-origin isolation on reader routes only, so on-device narration can
  // use multi-threaded WASM (SharedArrayBuffer). Scoped to /read/** so the
  // Google OAuth popup on /login (which relies on window.opener) is unaffected.
  // COEP `credentialless` keeps cross-origin no-credential assets (fonts, the
  // model CDN, public document images) loading without per-origin CORP headers.
  routeRules: {
    '/read/**': {
      headers: {
        'Cross-Origin-Opener-Policy': 'same-origin',
        'Cross-Origin-Embedder-Policy': 'credentialless'
      }
    },
    '/playground/**': {
      headers: {
        'Cross-Origin-Opener-Policy': 'same-origin',
        'Cross-Origin-Embedder-Policy': 'credentialless'
      }
    },
    // Under COEP the reader's ES module worker must itself opt into COEP and
    // carry CORP, or the browser blocks it (ERR_BLOCKED_BY_RESPONSE). This
    // covers production; the dev server sets the same headers via `vite` below.
    '/_nuxt/**': {
      headers: {
        'Cross-Origin-Embedder-Policy': 'credentialless',
        'Cross-Origin-Resource-Policy': 'cross-origin'
      }
    }
  },

  // Dev server (Vite) must send COEP + CORP on the served worker/asset responses
  // so the reader's module worker loads under COEP; route rules cover prod.
  vite: {
    server: {
      headers: {
        'Cross-Origin-Embedder-Policy': 'credentialless',
        'Cross-Origin-Resource-Policy': 'cross-origin'
      }
    }
  },

  runtimeConfig: {
    public: {
      apiBaseUrl: process.env.NUXT_PUBLIC_API_BASE_URL || 'http://localhost:5000',
      googleClientId: process.env.NUXT_PUBLIC_GOOGLE_CLIENT_ID || process.env.GOOGLE_CLIENT_ID || ''
    }
  },
  hooks: {
    'pages:extend'(pages) {
      if (process.env.NODE_ENV === 'production') {
        const devPrefixes = ['/showcase', '/playground']
        for (let i = pages.length - 1; i >= 0; i--) {
          const path = pages[i].path || ''
          if (devPrefixes.some(prefix => path === prefix || path.startsWith(`${prefix}/`))) {
            pages.splice(i, 1)
          }
        }
      }
    }
  },


  css: ['~/assets/css/main.css'],

  app: {
    pageTransition: false,
    layoutTransition: false,
    head: {
      title: 'TechDaily - Daily Senior Engineering & System Design Focus',
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        { name: 'description', content: 'Transform 30 mins into senior engineering mastery with daily architecture drills, SM-2 flashcards, and multimodal AI evaluation.' }
      ],
      link: [
        { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' },
        { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
        { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' },
        { rel: 'stylesheet', href: 'https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800;900&family=JetBrains+Mono:wght@400;500;600&display=swap' }
      ],
      script: [
        { src: 'https://accounts.google.com/gsi/client', async: true, defer: true }
      ]
    }
  }
})

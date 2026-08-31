// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2024-11-01',
  devtools: { enabled: false },

  modules: [
    '@nuxtjs/tailwindcss',
    '@nuxtjs/color-mode',
    '@nuxtjs/i18n',
    '@pinia/nuxt'
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

  runtimeConfig: {
    public: {
      apiBaseUrl: process.env.NUXT_PUBLIC_API_BASE_URL || 'http://localhost:5000',
      googleClientId: process.env.NUXT_PUBLIC_GOOGLE_CLIENT_ID || '982684500709-75solmbterlbdvut85btisallcsf83ef.apps.googleusercontent.com'
    }
  },

  css: ['~/assets/css/main.css'],

  app: {
    head: {
      title: 'TechDaily — Daily Senior Engineering & System Design Focus',
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        { name: 'description', content: 'Transform 30 mins into senior engineering mastery with daily architecture drills, SM-2 flashcards, and multimodal AI evaluation.' }
      ],
      link: [
        { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' }
      ],
      script: [
        { src: 'https://accounts.google.com/gsi/client', async: true, defer: true }
      ]
    }
  }
})

import { describe, it, expect } from 'vitest'
import nuxtConfig from '~/nuxt.config'

describe('nuxt.config pages:extend route pruning', () => {
  it('prunes showcase and playground routes when NODE_ENV is production', () => {
    const originalEnv = process.env.NODE_ENV
    try {
      process.env.NODE_ENV = 'production'

      const pages = [
        { path: '/', file: 'pages/index.vue' },
        { path: '/settings', file: 'pages/settings.vue' },
        { path: '/showcase', file: 'pages/showcase.vue' },
        { path: '/playground', file: 'pages/playground/index.vue' },
        { path: '/playground/dashboard', file: 'pages/playground/dashboard.vue' }
      ]

      const hook = (nuxtConfig as any).hooks?.['pages:extend']
      expect(hook).toBeDefined()
      hook(pages)

      expect(pages.map(p => p.path)).toEqual(['/', '/settings'])
      expect(pages.some(p => p.path.startsWith('/showcase'))).toBe(false)
      expect(pages.some(p => p.path.startsWith('/playground'))).toBe(false)
    } finally {
      process.env.NODE_ENV = originalEnv
    }
  })

  it('preserves showcase and playground routes in development mode', () => {
    const originalEnv = process.env.NODE_ENV
    try {
      process.env.NODE_ENV = 'development'

      const pages = [
        { path: '/', file: 'pages/index.vue' },
        { path: '/settings', file: 'pages/settings.vue' },
        { path: '/showcase', file: 'pages/showcase.vue' },
        { path: '/playground', file: 'pages/playground/index.vue' }
      ]

      const hook = (nuxtConfig as any).hooks?.['pages:extend']
      expect(hook).toBeDefined()
      hook(pages)

      expect(pages.map(p => p.path)).toEqual(['/', '/settings', '/showcase', '/playground'])
    } finally {
      process.env.NODE_ENV = originalEnv
    }
  })
})

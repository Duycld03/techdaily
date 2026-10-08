import { describe, it, expect } from 'vitest'
import nuxtConfig from '~~/nuxt.config'

describe('nuxt.config pages:extend route pruning', () => {
  it('prunes showcase routes when NODE_ENV is production', () => {
    const originalEnv = process.env.NODE_ENV
    try {
      process.env.NODE_ENV = 'production'

      const pages = [
        { path: '/', file: 'pages/index.vue' },
        { path: '/settings', file: 'pages/settings.vue' },
        { path: '/showcase', file: 'pages/showcase.vue' }
      ]

      const hook = (nuxtConfig as any).hooks?.['pages:extend']
      expect(hook).toBeDefined()
      hook(pages)

      expect(pages.map(p => p.path)).toEqual(['/', '/settings'])
      expect(pages.some(p => p.path.startsWith('/showcase'))).toBe(false)
    } finally {
      process.env.NODE_ENV = originalEnv
    }
  })

  it('preserves showcase routes in development mode', () => {
    const originalEnv = process.env.NODE_ENV
    try {
      process.env.NODE_ENV = 'development'

      const pages = [
        { path: '/', file: 'pages/index.vue' },
        { path: '/settings', file: 'pages/settings.vue' },
        { path: '/showcase', file: 'pages/showcase.vue' }
      ]

      const hook = (nuxtConfig as any).hooks?.['pages:extend']
      expect(hook).toBeDefined()
      hook(pages)

      expect(pages.map(p => p.path)).toEqual(['/', '/settings', '/showcase'])
    } finally {
      process.env.NODE_ENV = originalEnv
    }
  })
})

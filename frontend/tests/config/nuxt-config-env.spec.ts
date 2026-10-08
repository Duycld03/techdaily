import { describe, it, expect } from 'vitest'
import { validateApiBaseUrl } from '~~/config/validateEnv'

describe('validateApiBaseUrl', () => {
  it('throws an explicit configuration error when NUXT_PUBLIC_API_BASE_URL is undefined in development', () => {
    expect(() => {
      validateApiBaseUrl({ NODE_ENV: 'development' }, false)
    }).toThrow('❌ [Config Error] Missing NUXT_PUBLIC_API_BASE_URL in environment.')
  })

  it('allows empty string in production for same-origin relative API routing', () => {
    const result = validateApiBaseUrl({ NODE_ENV: 'production', NUXT_PUBLIC_API_BASE_URL: '' }, false)
    expect(result).toBe('')
  })

  it('returns configured API base URL when provided in development', () => {
    const result = validateApiBaseUrl({ NODE_ENV: 'development', NUXT_PUBLIC_API_BASE_URL: 'http://localhost:5000' }, false)
    expect(result).toBe('http://localhost:5000')
  })

  it('does not throw during prepare or typecheck lifecycle even if env var is missing', () => {
    const result = validateApiBaseUrl({ NODE_ENV: 'development' }, true)
    expect(result).toBe('')
  })
})

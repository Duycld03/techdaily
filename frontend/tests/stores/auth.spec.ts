import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '~/stores/useAuthStore'

// Mock useApiClient composable
const mockSessionToken = `e30.${btoa(JSON.stringify({ sub: 'u-1', exp: Math.floor(Date.now() / 1000) + 3600 }))}.signature`
const mockRefreshAuthToken = vi.fn()
const mockPost = vi.fn(async (url: string, body?: Record<string, unknown>) => {
  const email = typeof body?.email === 'string' ? body.email : 'engineer@deeppace.test'
  // Session-issuing endpoints
  if (url.includes('/register/verify') || url.includes('/login') || url.includes('/google')) {
    return {
      token: mockSessionToken,
      user: {
        id: 'u-1',
        email,
        name: 'Senior Engineer',
        preferredLocale: 'en'
      }
    }
  }
  // Registration step 1: challenge response, no session
  if (url.includes('/register')) {
    return { email }
  }
  if (url.includes('/forgot-password') || url.includes('/reset-password')) {
    return { message: 'If the email is registered, a code has been sent.' }
  }
  if (url.includes('/otp/resend')) {
    return { email }
  }
  throw new Error('Unknown endpoint')
})

vi.mock('~/composables/useApiClient', () => ({
  useApiClient: () => ({
    refreshAuthToken: mockRefreshAuthToken,
    post: mockPost
  })
}))

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    sessionStorage.clear()
    mockPost.mockClear()
    mockRefreshAuthToken.mockReset()
    useCookie('deeppace_token').value = null
    useCookie('deeppace_user').value = null
    useCookie('techdaily_token').value = null
    useCookie('techdaily_user').value = null
  })

  it('initializes with logged out state', () => {
    const auth = useAuthStore()
    expect(auth.isLoggedIn).toBe(false)
    expect(auth.token).toBeNull()
    expect(auth.user).toBeNull()
  })

  it('does not combine a canonical cookie identity with another stored profile', () => {
    const jwt = `e30.${btoa(JSON.stringify({ sub: 'cookie-account', exp: Math.floor(Date.now() / 1000) + 3600 }))}.signature`
    useCookie('deeppace_token').value = jwt
    useCookie('deeppace_user').value = { id: 'other-account', email: 'other@example.test' }
    localStorage.setItem('deeppace_user', JSON.stringify({ id: 'other-account', email: 'other@example.test' }))
    const auth = useAuthStore()
    auth.init()
    expect(auth.user?.id).toBe('cookie-account')
    expect(auth.user?.email).not.toBe('other@example.test')
    expect(auth.isPersistentSession).toBe(false)
  })

  it('successfully logs in and stores session in localStorage', async () => {
    const auth = useAuthStore()
    const res = await auth.login('engineer@deeppace.test', 'password123')

    expect(res.token).toBe(mockSessionToken)
    expect(auth.isLoggedIn).toBe(true)
    expect(auth.user?.email).toBe('engineer@deeppace.test')
    expect(localStorage.getItem('deeppace_token')).toBe(mockSessionToken)
  })

  it('registerRequest sends the documented payload and yields no session', async () => {
    const auth = useAuthStore()
    const res = await auth.registerRequest('newuser@deeppace.test', 'password123', 'New Architect', 'vi')

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/register', {
      email: 'newuser@deeppace.test',
      password: 'password123',
      name: 'New Architect',
      locale: 'vi'
    })
    expect(res.email).toBe('newuser@deeppace.test')
    expect(auth.isLoggedIn).toBe(false)
    expect(auth.token).toBeNull()
  })

  it('registerVerify sends the code payload and establishes a session', async () => {
    const auth = useAuthStore()
    const res = await auth.registerVerify('newuser@deeppace.test', '123456', true)

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/register/verify', {
      email: 'newuser@deeppace.test',
      code: '123456',
      rememberMe: true
    })
    expect(res.token).toBe(mockSessionToken)
    expect(auth.isLoggedIn).toBe(true)
    expect(auth.user?.email).toBe('newuser@deeppace.test')
  })

  it('forgotPassword posts only the email and yields no session', async () => {
    const auth = useAuthStore()
    await auth.forgotPassword('reset@deeppace.test')

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/forgot-password', { email: 'reset@deeppace.test' })
    expect(auth.isLoggedIn).toBe(false)
  })

  it('resetPassword posts email, code, and newPassword', async () => {
    const auth = useAuthStore()
    await auth.resetPassword('reset@deeppace.test', '654321', 'brandnew123')

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/reset-password', {
      email: 'reset@deeppace.test',
      code: '654321',
      newPassword: 'brandnew123'
    })
    expect(auth.isLoggedIn).toBe(false)
  })

  it('resendOtp posts email and purpose', async () => {
    const auth = useAuthStore()
    await auth.resendOtp('reset@deeppace.test', 'PasswordReset')

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/otp/resend', {
      email: 'reset@deeppace.test',
      purpose: 'PasswordReset'
    })
  })

  it('clears token and user on logout', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@deeppace.test', 'password123')
    expect(auth.isLoggedIn).toBe(true)

    auth.logout()
    expect(auth.isLoggedIn).toBe(false)
    expect(auth.token).toBeNull()
    expect(auth.user).toBeNull()
    expect(localStorage.getItem('deeppace_token')).toBeNull()
  })

  it('submits rememberMe in the login request body', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@deeppace.test', 'password123', false)

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/login', {
      email: 'engineer@deeppace.test',
      password: 'password123',
      rememberMe: false
    })
  })

  it('defaults rememberMe to true when the login argument is omitted', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@deeppace.test', 'password123')

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/login', expect.objectContaining({ rememberMe: true }))
  })

  it('stores a session-scoped login in sessionStorage and not localStorage', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@deeppace.test', 'password123', false)

    expect(sessionStorage.getItem('deeppace_token')).toBe(mockSessionToken)
    expect(localStorage.getItem('deeppace_token')).toBeNull()
  })

  it('stores a remembered login in localStorage and not sessionStorage', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@deeppace.test', 'password123', true)

    expect(localStorage.getItem('deeppace_token')).toBe(mockSessionToken)
    expect(sessionStorage.getItem('deeppace_token')).toBeNull()
  })

  it('clearSession clears both localStorage and sessionStorage', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@deeppace.test', 'password123', false)
    expect(sessionStorage.getItem('deeppace_token')).toBe(mockSessionToken)

    auth.clearSession()
    expect(localStorage.getItem('deeppace_token')).toBeNull()
    expect(sessionStorage.getItem('deeppace_token')).toBeNull()
    expect(sessionStorage.getItem('deeppace_user')).toBeNull()
  })

  it('re-resolves browser persistence after an initialized SSR snapshot', () => {
    const auth = useAuthStore()
    auth.init()
    localStorage.setItem('deeppace_token', mockSessionToken)
    localStorage.setItem('deeppace_user', JSON.stringify({ id: 'u-1', email: 'persistent@example.test', name: 'Persistent', preferredLocale: 'en' }))
    auth.init(true)
    expect(auth.token).toBe(mockSessionToken)
    expect(auth.isPersistentSession).toBe(true)
  })

  it('preserves session-only browser storage during refresh', async () => {
    const auth = useAuthStore()
    sessionStorage.setItem('deeppace_token', mockSessionToken)
    sessionStorage.setItem('deeppace_user', JSON.stringify({ id: 'u-1', email: 'session@example.test', name: 'Session', preferredLocale: 'en' }))
    auth.init()
    expect(auth.isPersistentSession).toBe(false)
    mockRefreshAuthToken.mockResolvedValueOnce(mockSessionToken)
    expect(await auth.tryRefreshToken()).toBe(true)
    expect(sessionStorage.getItem('deeppace_token')).toBe(mockSessionToken)
    expect(localStorage.getItem('deeppace_token')).toBeNull()
  })
  describe('JWT Expiration and Proactive Cleanup', () => {
    function createMockJwt(expOffsetSeconds: number): string {
      const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
      const payload = btoa(JSON.stringify({
        nameid: 'u-100',
        email: 'test@deeppace.test',
        unique_name: 'Test Engineer',
        exp: Math.floor(Date.now() / 1000) + expOffsetSeconds
      }))
      return `${header}.${payload}.mockSignature`
    }

    it('correctly identifies expired vs active tokens', () => {
      const auth = useAuthStore()
      const expiredToken = createMockJwt(-3600) // 1 hour ago
      const activeToken = createMockJwt(3600) // 1 hour in future

      expect(auth.isTokenExpired(expiredToken)).toBe(true)
      expect(auth.isTokenExpired(activeToken)).toBe(false)
      expect(auth.isTokenExpired(null)).toBe(true)
    })

    it('reports isLoggedIn as false when token is expired', () => {
      const auth = useAuthStore()
      const expiredToken = createMockJwt(-60)
      auth.token = expiredToken

      expect(auth.isLoggedIn).toBe(false)
      expect(auth.isAuthenticated).toBe(false)
    })

    it('reports isLoggedIn as true when token is valid', () => {
      const auth = useAuthStore()
      const activeToken = createMockJwt(3600)
      auth.token = activeToken

      expect(auth.isLoggedIn).toBe(true)
      expect(auth.isAuthenticated).toBe(true)
    })

    it('preserves user and token for refresh on init() when token is expired but reports isLoggedIn as false', () => {
      const auth = useAuthStore()
      const expiredToken = createMockJwt(-120)

      localStorage.setItem('deeppace_token', expiredToken)
      localStorage.setItem('deeppace_user', JSON.stringify({ id: 'u-100', email: 'test@deeppace.test', name: 'Test' }))

      auth.init()

      expect(auth.isLoggedIn).toBe(false)
      expect(auth.token).toBe(expiredToken)
      expect(auth.user?.email).toBe('test@deeppace.test')
      expect(localStorage.getItem('deeppace_token')).toBe(expiredToken)
    })

    it('successfully refreshes token via tryRefreshToken() and marks isLoggedIn true', async () => {
      const auth = useAuthStore()
      const expiredToken = createMockJwt(-120)
      const freshToken = createMockJwt(3600)
      auth.token = expiredToken
      auth.user = { id: 'u-100', email: 'test@deeppace.test', name: 'Test' } as any

      mockRefreshAuthToken.mockResolvedValueOnce(freshToken)

      const success = await auth.tryRefreshToken()

      expect(success).toBe(true)
      expect(mockRefreshAuthToken).toHaveBeenCalledTimes(1)
      expect(auth.token).toBe(freshToken)
      expect(auth.isLoggedIn).toBe(true)
    })

    it('clears session when token renewal is explicitly rejected', async () => {
      const auth = useAuthStore()
      const expiredToken = createMockJwt(-120)
      auth.token = expiredToken
      auth.user = { id: 'u-100', email: 'test@deeppace.test', name: 'Test' } as any

      mockRefreshAuthToken.mockRejectedValueOnce(Object.assign(new Error('Refresh token expired'), { status: 401 }))

      const success = await auth.tryRefreshToken()

      expect(success).toBe(false)
      expect(auth.token).toBeNull()
      expect(auth.user).toBeNull()
      expect(auth.isLoggedIn).toBe(false)
    })
    it('preserves valid token on init()', () => {
      const auth = useAuthStore()
      const activeToken = createMockJwt(7200)

      localStorage.setItem('deeppace_token', activeToken)
      localStorage.setItem('deeppace_user', JSON.stringify({ id: 'u-100', email: 'test@deeppace.test', name: 'Test' }))

      auth.init()

      expect(auth.isLoggedIn).toBe(true)
      expect(auth.token).toBe(activeToken)
      expect(auth.user?.email).toBe('test@deeppace.test')
    })

    it('uses canonical credentials and leaves legacy access to the migration boundary', () => {
      const auth = useAuthStore()
      const activeToken = createMockJwt(7200)

      localStorage.setItem('techdaily_token', activeToken)
      localStorage.setItem('techdaily_user', JSON.stringify({ id: 'u-100', email: 'legacy@deeppace.test', name: 'Legacy' }))
      localStorage.setItem('deeppace_token', activeToken)
      localStorage.setItem('deeppace_user', JSON.stringify({ id: 'u-100', email: 'canonical@deeppace.test', name: 'Canonical' }))

      auth.init()

      expect(auth.isLoggedIn).toBe(true)
      expect(auth.token).toBe(activeToken)
      expect(localStorage.getItem('deeppace_token')).toBe(activeToken)
      expect(auth.user?.email).toBe('canonical@deeppace.test')
      expect(localStorage.getItem('techdaily_token')).toBe(activeToken)
    })
  })
})

import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '~/stores/useAuthStore'

// Mock useApiClient composable
const mockRefreshAuthToken = vi.fn()
const mockPost = vi.fn(async (url: string, body?: Record<string, unknown>) => {
  const email = typeof body?.email === 'string' ? body.email : 'engineer@techdaily.local'
  // Session-issuing endpoints
  if (url.includes('/register/verify') || url.includes('/login') || url.includes('/google')) {
    return {
      token: 'mock-jwt-token-123',
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
    useCookie('techdaily_token').value = null
    useCookie('techdaily_user').value = null
  })

  it('initializes with logged out state', () => {
    const auth = useAuthStore()
    expect(auth.isLoggedIn).toBe(false)
    expect(auth.token).toBeNull()
    expect(auth.user).toBeNull()
  })

  it('successfully logs in and stores session in localStorage', async () => {
    const auth = useAuthStore()
    const res = await auth.login('engineer@techdaily.local', 'password123')

    expect(res.token).toBe('mock-jwt-token-123')
    expect(auth.isLoggedIn).toBe(true)
    expect(auth.user?.email).toBe('engineer@techdaily.local')
    expect(localStorage.getItem('techdaily_token')).toBe('mock-jwt-token-123')
  })

  it('registerRequest sends the documented payload and yields no session', async () => {
    const auth = useAuthStore()
    const res = await auth.registerRequest('newuser@techdaily.local', 'password123', 'New Architect', 'vi')

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/register', {
      email: 'newuser@techdaily.local',
      password: 'password123',
      name: 'New Architect',
      locale: 'vi'
    })
    expect(res.email).toBe('newuser@techdaily.local')
    expect(auth.isLoggedIn).toBe(false)
    expect(auth.token).toBeNull()
  })

  it('registerVerify sends the code payload and establishes a session', async () => {
    const auth = useAuthStore()
    const res = await auth.registerVerify('newuser@techdaily.local', '123456', true)

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/register/verify', {
      email: 'newuser@techdaily.local',
      code: '123456',
      rememberMe: true
    })
    expect(res.token).toBe('mock-jwt-token-123')
    expect(auth.isLoggedIn).toBe(true)
    expect(auth.user?.email).toBe('newuser@techdaily.local')
  })

  it('forgotPassword posts only the email and yields no session', async () => {
    const auth = useAuthStore()
    await auth.forgotPassword('reset@techdaily.local')

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/forgot-password', { email: 'reset@techdaily.local' })
    expect(auth.isLoggedIn).toBe(false)
  })

  it('resetPassword posts email, code, and newPassword', async () => {
    const auth = useAuthStore()
    await auth.resetPassword('reset@techdaily.local', '654321', 'brandnew123')

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/reset-password', {
      email: 'reset@techdaily.local',
      code: '654321',
      newPassword: 'brandnew123'
    })
    expect(auth.isLoggedIn).toBe(false)
  })

  it('resendOtp posts email and purpose', async () => {
    const auth = useAuthStore()
    await auth.resendOtp('reset@techdaily.local', 'PasswordReset')

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/otp/resend', {
      email: 'reset@techdaily.local',
      purpose: 'PasswordReset'
    })
  })

  it('clears token and user on logout', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@techdaily.local', 'password123')
    expect(auth.isLoggedIn).toBe(true)

    auth.logout()
    expect(auth.isLoggedIn).toBe(false)
    expect(auth.token).toBeNull()
    expect(auth.user).toBeNull()
    expect(localStorage.getItem('techdaily_token')).toBeNull()
  })

  it('submits rememberMe in the login request body', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@techdaily.local', 'password123', false)

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/login', {
      email: 'engineer@techdaily.local',
      password: 'password123',
      rememberMe: false
    })
  })

  it('defaults rememberMe to true when the login argument is omitted', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@techdaily.local', 'password123')

    expect(mockPost).toHaveBeenCalledWith('/api/v1/auth/login', expect.objectContaining({ rememberMe: true }))
  })

  it('stores a session-scoped login in sessionStorage and not localStorage', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@techdaily.local', 'password123', false)

    expect(sessionStorage.getItem('techdaily_token')).toBe('mock-jwt-token-123')
    expect(localStorage.getItem('techdaily_token')).toBeNull()
  })

  it('stores a remembered login in localStorage and not sessionStorage', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@techdaily.local', 'password123', true)

    expect(localStorage.getItem('techdaily_token')).toBe('mock-jwt-token-123')
    expect(sessionStorage.getItem('techdaily_token')).toBeNull()
  })

  it('clearSession clears both localStorage and sessionStorage', async () => {
    const auth = useAuthStore()
    await auth.login('engineer@techdaily.local', 'password123', false)
    expect(sessionStorage.getItem('techdaily_token')).toBe('mock-jwt-token-123')

    auth.clearSession()
    expect(localStorage.getItem('techdaily_token')).toBeNull()
    expect(sessionStorage.getItem('techdaily_token')).toBeNull()
    expect(sessionStorage.getItem('techdaily_user')).toBeNull()
  })

  describe('JWT Expiration and Proactive Cleanup', () => {
    function createMockJwt(expOffsetSeconds: number): string {
      const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
      const payload = btoa(JSON.stringify({
        nameid: 'u-100',
        email: 'test@techdaily.io',
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

      localStorage.setItem('techdaily_token', expiredToken)
      localStorage.setItem('techdaily_user', JSON.stringify({ id: 'u-100', email: 'test@techdaily.io', name: 'Test' }))

      auth.init()

      expect(auth.isLoggedIn).toBe(false)
      expect(auth.token).toBe(expiredToken)
      expect(auth.user?.email).toBe('test@techdaily.io')
      expect(localStorage.getItem('techdaily_token')).toBe(expiredToken)
    })

    it('successfully refreshes token via tryRefreshToken() and marks isLoggedIn true', async () => {
      const auth = useAuthStore()
      const expiredToken = createMockJwt(-120)
      const freshToken = createMockJwt(3600)
      auth.token = expiredToken
      auth.user = { id: 'u-100', email: 'test@techdaily.io', name: 'Test' } as any

      mockRefreshAuthToken.mockResolvedValueOnce(freshToken)

      const success = await auth.tryRefreshToken()

      expect(success).toBe(true)
      expect(mockRefreshAuthToken).toHaveBeenCalledTimes(1)
      expect(auth.token).toBe(freshToken)
      expect(auth.isLoggedIn).toBe(true)
    })

    it('clears session on tryRefreshToken() failure', async () => {
      const auth = useAuthStore()
      const expiredToken = createMockJwt(-120)
      auth.token = expiredToken
      auth.user = { id: 'u-100', email: 'test@techdaily.io', name: 'Test' } as any

      mockRefreshAuthToken.mockRejectedValueOnce(new Error('Refresh token expired'))

      const success = await auth.tryRefreshToken()

      expect(success).toBe(false)
      expect(auth.token).toBeNull()
      expect(auth.user).toBeNull()
      expect(auth.isLoggedIn).toBe(false)
    })
    it('preserves valid token on init()', () => {
      const auth = useAuthStore()
      const activeToken = createMockJwt(7200)

      localStorage.setItem('techdaily_token', activeToken)
      localStorage.setItem('techdaily_user', JSON.stringify({ id: 'u-100', email: 'test@techdaily.io', name: 'Test' }))

      auth.init()

      expect(auth.isLoggedIn).toBe(true)
      expect(auth.token).toBe(activeToken)
      expect(auth.user?.email).toBe('test@techdaily.io')
    })
  })
})

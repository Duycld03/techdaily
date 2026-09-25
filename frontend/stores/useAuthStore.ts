import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useApiClient } from '~/composables/useApiClient'

export interface AuthUser {
  id: string
  email: string
  name: string
  preferredLocale: string
  avatarUrl?: string
}

export const useAuthStore = defineStore('auth', () => {
  const tokenCookie = useCookie<string | null>('techdaily_token', { maxAge: 60 * 60 * 24 * 30, path: '/' })
  const userCookie = useCookie<AuthUser | null>('techdaily_user', { maxAge: 60 * 60 * 24 * 30, path: '/' })

  function parseJwtPayload(jwt: string): any | null {
    try {
      const parts = jwt.split('.')
      const base64Url = parts[1]
      if (!base64Url) return null
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      )
      return JSON.parse(jsonPayload)
    } catch {
      return null
    }
  }

  function isTokenExpired(jwt: string | null): boolean {
    if (!jwt) return true
    const payload = parseJwtPayload(jwt)
    if (payload && typeof payload.exp === 'number') {
      return payload.exp * 1000 <= Date.now()
    }
    return false
  }

  function parseUserFromJwt(jwt: string): AuthUser | null {
    const payload = parseJwtPayload(jwt)
    if (!payload) return null
    return {
      id: payload.nameid || payload.sub || '',
      email: payload.email || '',
      name: payload.unique_name || payload.name || (payload.email ? payload.email.split('@')[0] : 'User'),
      preferredLocale: 'en'
    }
  }

  function clearSession() {
    token.value = null
    user.value = null
    tokenCookie.value = null
    userCookie.value = null
    if (typeof window !== 'undefined') {
      localStorage.removeItem('techdaily_token')
      localStorage.removeItem('techdaily_user')
      localStorage.removeItem('techdaily_refresh_token')
      sessionStorage.removeItem('techdaily_token')
      sessionStorage.removeItem('techdaily_user')
    }
  }

  const token = ref<string | null>(tokenCookie.value || null)
  const user = ref<AuthUser | null>(userCookie.value || (token.value ? parseUserFromJwt(token.value) : null))
  const isInitialized = ref(false)
  // Whether the current session is persistent (Remember me) or session-scoped
  const isPersistentSession = ref(true)

  const isLoggedIn = computed(() => !!token.value && !isTokenExpired(token.value))
  const isAuthenticated = computed(() => !!token.value && !isTokenExpired(token.value))

  function init() {
    if (!token.value && tokenCookie.value) {
      token.value = tokenCookie.value
    }
    if (!user.value && userCookie.value) {
      user.value = userCookie.value
    }

    if (typeof window !== 'undefined') {
      if (!token.value) {
        token.value = localStorage.getItem('techdaily_token') || sessionStorage.getItem('techdaily_token')
      }
      if (!user.value) {
        const storedUser = localStorage.getItem('techdaily_user') || sessionStorage.getItem('techdaily_user')
        if (storedUser) {
          try {
            user.value = JSON.parse(storedUser)
          } catch {
            user.value = null
          }
        }
      }
      // Session-scoped only when the token lives in sessionStorage and not localStorage
      isPersistentSession.value = !(sessionStorage.getItem('techdaily_token') !== null && localStorage.getItem('techdaily_token') === null)
    }


    if (token.value && !user.value) {
      const parsed = parseUserFromJwt(token.value)
      if (parsed) {
        user.value = parsed
        userCookie.value = parsed
      }
    }

    if (typeof window !== 'undefined' && !isInitialized.value) {
      if (token.value && !tokenCookie.value) {
        tokenCookie.value = token.value
      }
      if (user.value && !userCookie.value) {
        userCookie.value = user.value
      }
      isInitialized.value = true
    }
  }

  async function login(email: string, password: string, rememberMe: boolean = true) {
    const api = useApiClient()
    const response = await api.post<{ token?: string; accessToken?: string; user: AuthUser }>('/api/v1/auth/login', {
      email,
      password,
      rememberMe
    })
    const jwt = response.accessToken || response.token || ''
    setSession(jwt, response.user, rememberMe)
    return { ...response, token: jwt }
  }

  async function registerRequest(email: string, password: string, name?: string, locale: string = 'en') {
    const api = useApiClient()
    // Step 1: request an email verification code. No session is issued yet.
    return await api.post<{ email: string }>('/api/v1/auth/register', {
      email,
      password,
      name,
      locale
    })
  }

  async function registerVerify(email: string, code: string, rememberMe: boolean = true) {
    const api = useApiClient()
    // Step 2: verify the code, create the account, and sign in.
    const response = await api.post<{ token?: string; accessToken?: string; user: AuthUser }>('/api/v1/auth/register/verify', {
      email,
      code,
      rememberMe
    })
    const jwt = response.accessToken || response.token || ''
    setSession(jwt, response.user, rememberMe)
    return { ...response, token: jwt }
  }

  async function forgotPassword(email: string) {
    const api = useApiClient()
    // Always resolves 200 server-side (anti-enumeration); issues a reset code if the account exists.
    return await api.post<{ message?: string }>('/api/v1/auth/forgot-password', { email })
  }

  async function resetPassword(email: string, code: string, newPassword: string) {
    const api = useApiClient()
    return await api.post<{ message?: string }>('/api/v1/auth/reset-password', {
      email,
      code,
      newPassword
    })
  }

  async function resendOtp(email: string, purpose: 'EmailVerification' | 'PasswordReset') {
    const api = useApiClient()
    return await api.post<{ email?: string }>('/api/v1/auth/otp/resend', { email, purpose })
  }

  async function googleLogin(idToken: string) {
    const api = useApiClient()
    const response = await api.post<{ token?: string; accessToken?: string; user: AuthUser }>('/api/v1/auth/google', { idToken })
    const jwt = response.accessToken || response.token || ''
    setSession(jwt, response.user, true)
    return { ...response, token: jwt }
  }

  function setSession(newToken: string, newUser?: AuthUser | null, remember: boolean = isPersistentSession.value) {
    isPersistentSession.value = remember
    token.value = newToken

    const cookieOpts = remember ? { maxAge: 60 * 60 * 24 * 30, path: '/' } : { path: '/' }
    useCookie<string | null>('techdaily_token', cookieOpts).value = newToken

    if (typeof window !== 'undefined') {
      const primary = remember ? window.localStorage : window.sessionStorage
      const secondary = remember ? window.sessionStorage : window.localStorage
      primary.setItem('techdaily_token', newToken)
      secondary.removeItem('techdaily_token')
      localStorage.removeItem('techdaily_refresh_token')
    }

    if (newUser) {
      user.value = newUser
      useCookie<AuthUser | null>('techdaily_user', cookieOpts).value = newUser
      if (typeof window !== 'undefined') {
        const primary = remember ? window.localStorage : window.sessionStorage
        const secondary = remember ? window.sessionStorage : window.localStorage
        primary.setItem('techdaily_user', JSON.stringify(newUser))
        secondary.removeItem('techdaily_user')
      }
    }
  }

  async function logout(redirectPath: string = '/login') {
    clearSession()
    try {
      const api = useApiClient()
      await api.post('/api/v1/auth/revoke', undefined, { credentials: 'include' } as RequestInit)
    } catch {
      // Ignore network / revoke failure so client state is still cleared
    } finally {
      if (typeof navigateTo === 'function') {
        navigateTo(redirectPath)
      }
    }
  }

  function updateUser(updated: Partial<AuthUser>) {
    if (user.value) {
      user.value = { ...user.value, ...updated }
      const cookieOpts = isPersistentSession.value ? { maxAge: 60 * 60 * 24 * 30, path: '/' } : { path: '/' }
      useCookie<AuthUser | null>('techdaily_user', cookieOpts).value = user.value
      if (typeof window !== 'undefined') {
        const primary = isPersistentSession.value ? window.localStorage : window.sessionStorage
        primary.setItem('techdaily_user', JSON.stringify(user.value))
      }
    }
  }
  async function tryRefreshToken(): Promise<boolean> {
    try {
      const api = useApiClient()
      const newToken = await api.refreshAuthToken()
      if (newToken) {
        setSession(newToken, user.value || parseUserFromJwt(newToken))
        return true
      }
      clearSession()
      return false
    } catch {
      clearSession()
      return false
    }
  }


  return {
    token,
    user,
    isLoggedIn,
    isAuthenticated,
    isTokenExpired,
    clearSession,
    setSession,
    parseUserFromJwt,
    init,
    login,
    registerRequest,
    registerVerify,
    forgotPassword,
    resetPassword,
    resendOtp,
    googleLogin,
    updateUser,
    tryRefreshToken,
    logout
  }
})

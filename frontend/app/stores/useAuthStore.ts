import { defineStore } from 'pinia'
import { ref, computed, shallowRef } from 'vue'
import { useApiClient } from '~/composables/useApiClient'
import { CLIENT_STORAGE, browserStorage, isCurrentSessionToken, profileForToken, readCanonicalSession, type RoleDescriptor } from '~/utils/clientStorage'
import { browserMigrationEnvironment, clearLegacyCredentials, commitSessionChange, retireLegacyIdentity } from '~/utils/legacyStorageMigration'

export interface AuthUser {
  id: string
  email: string
  name: string
  preferredLocale: string
  avatarUrl?: string
  role?: RoleDescriptor
}

export const useAuthStore = defineStore('auth', () => {
  const tokenCookie = useCookie<string | null>('deeppace_token', { maxAge: 60 * 60 * 24 * 30, path: '/', sameSite: 'lax' })
  const userCookie = useCookie<AuthUser | null>('deeppace_user', { maxAge: 60 * 60 * 24 * 30, path: '/', sameSite: 'lax' })

  function isTokenExpired(jwt: string | null): boolean {
    return !isCurrentSessionToken(jwt)
  }

  function parseUserFromJwt(jwt: string): AuthUser | null {
    return profileForToken(jwt, null)
  }

  function clearSession() {
    sessionGeneration.value++
    adminGeneration.value++
    token.value = null
    user.value = null
    tokenCookie.value = null
    userCookie.value = null
    let pending = Promise.resolve()
    if (typeof window !== 'undefined') {
      const env = browserMigrationEnvironment()
      const epoch = retireLegacyIdentity(env)
      const cleanup = () => {
        tokenCookie.value = null
        userCookie.value = null
        for (const storage of [env.local, env.session]) {
          for (const key of [CLIENT_STORAGE.token, CLIENT_STORAGE.user]) {
            try { storage?.removeItem(key) } catch { /* Other storage scopes still need cleanup. */ }
          }
        }
        try { clearLegacyCredentials(env) } catch { /* The retirement fence prevents reimport. */ }
      }
      cleanup()
      pending = commitSessionChange(env, epoch, cleanup)
    }
    try { useCatalogStore().cleanup() } catch { /* Pinia may be unavailable in isolated tests. */ }
    return pending
  }

  const initialToken = tokenCookie.value || null
  const token = ref<string | null>(initialToken)
  const user = ref<AuthUser | null>(profileForToken(initialToken, userCookie.value))
  const isInitialized = ref(false)
  const sessionGeneration = shallowRef(0)
  const adminGeneration = shallowRef(0)
  const lastRefreshError = shallowRef<number | null>(null)

  function invalidateAdminAccess() {
    adminGeneration.value++
  }

  function invalidateStoredSession() {
    sessionGeneration.value++
    adminGeneration.value++
    token.value = null
    user.value = null
    isInitialized.value = true
  }
  const isPersistentSession = ref(false)

  const isLoggedIn = computed(() => !!token.value && !isTokenExpired(token.value))
  const isAuthenticated = computed(() => !!token.value && !isTokenExpired(token.value))

  function init(force = false) {
    if (isInitialized.value && !force) return
    const resolved = readCanonicalSession(tokenCookie.value, userCookie.value)
    token.value = resolved.token
    user.value = resolved.user
    isPersistentSession.value = resolved.persistent
    isInitialized.value = true
  }

  async function login(email: string, password: string, rememberMe: boolean = true) {
    const api = useApiClient()
    const response = await api.post<{ token?: string; accessToken?: string; user: AuthUser }>('/api/v1/auth/login', {
      email,
      password,
      rememberMe
    })
    const jwt = response.accessToken || response.token || ''
    await setSession(jwt, response.user, rememberMe)
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
    await setSession(jwt, response.user, rememberMe)
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

  async function googleLogin(token: string) {
    const api = useApiClient()
    const isAccessToken = token.startsWith('ya29.') || !token.includes('.')
    const payload = isAccessToken ? { accessToken: token } : { idToken: token }
    const response = await api.post<{ token?: string; accessToken?: string; user: AuthUser }>('/api/v1/auth/google', payload)
    const jwt = response.accessToken || response.token || ''
    await setSession(jwt, response.user, true)
    return { ...response, token: jwt }
  }

  async function setSession(newToken: string, newUser?: AuthUser | null, remember: boolean = isPersistentSession.value) {
    const resolvedUser = profileForToken(newToken, newUser)
    if (resolvedUser?.id !== user.value?.id) {
      sessionGeneration.value++
      adminGeneration.value++
    }
    const generation = sessionGeneration.value
    isPersistentSession.value = remember
    token.value = newToken
    user.value = resolvedUser
    isInitialized.value = true
    const cookieOpts = remember
      ? { maxAge: 60 * 60 * 24 * 30, path: '/', sameSite: 'lax' as const }
      : { path: '/', sameSite: 'lax' as const }
    // Capture Nuxt refs before awaiting the browser lock so context remains request scoped.
    const accessCookie = useCookie<string | null>(CLIENT_STORAGE.token, cookieOpts)
    const profileCookie = useCookie<AuthUser | null>(CLIENT_STORAGE.user, cookieOpts)
    const persist = () => {
      if (generation !== sessionGeneration.value) return
      accessCookie.value = newToken
      profileCookie.value = resolvedUser
      if (typeof window === 'undefined') return
      const primary = browserStorage(remember ? 'localStorage' : 'sessionStorage')
      const secondary = browserStorage(remember ? 'sessionStorage' : 'localStorage')
      for (const [key, value] of [[CLIENT_STORAGE.token, newToken], [CLIENT_STORAGE.user, JSON.stringify(resolvedUser)]]) {
        try { primary?.setItem(key!, value!) } catch { /* In-memory access remains usable when storage is denied. */ }
        try { secondary?.removeItem(key!) } catch { /* Scope cleanup is best effort behind the identity fence. */ }
      }
    }
    if (typeof window === 'undefined') persist()
    else {
      const env = browserMigrationEnvironment()
      const epoch = retireLegacyIdentity(env)
      await commitSessionChange(env, epoch, () => {
        persist()
        try { clearLegacyCredentials(env) } catch { /* An incomplete cleanup cannot reimport retired identity. */ }
      })
    }
  }

  async function logout(redirectPath: string = '/login') {
    await clearSession()
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
    if (!user.value) return
    user.value = { ...user.value, ...updated, id: user.value.id }
    const cookieOpts = isPersistentSession.value
      ? { maxAge: 60 * 60 * 24 * 30, path: '/', sameSite: 'lax' as const }
      : { path: '/', sameSite: 'lax' as const }
    useCookie<AuthUser | null>(CLIENT_STORAGE.user, cookieOpts).value = user.value
    try { browserStorage(isPersistentSession.value ? 'localStorage' : 'sessionStorage')?.setItem(CLIENT_STORAGE.user, JSON.stringify(user.value)) } catch { /* Storage can be unavailable. */ }
  }
  async function tryRefreshToken(): Promise<boolean> {
    lastRefreshError.value = null
    try {
      const api = useApiClient()
      const newToken = await api.refreshAuthToken()
      if (newToken) {
        await setSession(newToken, user.value || parseUserFromJwt(newToken))
        return true
      }
      if (isTokenExpired(token.value)) {
        await clearSession()
      }
      return false
    } catch (err: unknown) {
      const failure = err && typeof err === 'object' ? err : {}
      const status = 'status' in failure && typeof failure.status === 'number' ? failure.status : undefined
      // Network drops and server failures must not wipe local credentials.
      if (('isNetworkError' in failure && failure.isNetworkError) || (status !== undefined && status >= 500)) {
        lastRefreshError.value = 503
        return false
      }
      if (status === 401 || status === 403) {
        await clearSession()
        lastRefreshError.value = status
      } else {
        lastRefreshError.value = 503
      }
      return false
    }
  }

  return {
    sessionGeneration,
    adminGeneration,
    lastRefreshError,
    isPersistentSession,
    invalidateAdminAccess,
    invalidateStoredSession,
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

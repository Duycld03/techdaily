import { useAuthStore } from '~/stores/useAuthStore'

export function isJwtExpired(jwt: string | null): boolean {
  if (!jwt) return true
  try {
    const parts = jwt.split('.')
    if (parts.length < 2) return true
    const base64Url = parts[1]
    if (!base64Url) return true
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    let binaryStr = ''
    if (typeof atob !== 'undefined') {
      binaryStr = atob(base64)
    } else if (typeof Buffer !== 'undefined') {
      binaryStr = Buffer.from(base64, 'base64').toString('binary')
    } else {
      return true
    }
    const jsonPayload = decodeURIComponent(
      binaryStr
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    )
    const payload = JSON.parse(jsonPayload)
    if (payload && typeof payload.exp === 'number') {
      return payload.exp * 1000 <= Date.now()
    }
    return false
  } catch {
    return true
  }
}
export default defineNuxtRouteMiddleware(async (to) => {
  const authStore = useAuthStore()

  // Always initialize store state from cookies and local storage
  authStore.init()
  // Guest authentication paths
  const isGuestAuthPath =
    to.path === '/login' ||
    to.path === '/register' ||
    to.path === '/forgot-password' ||
    to.path === '/reset-password'

  // Local development / testing exemptions
  const isDevExempt = to.path.startsWith('/showcase')

  // Default-Deny: all routes require authentication unless explicitly exempted
  const isAuthRequired = !isGuestAuthPath && !isDevExempt

  // On SSR (server-side rendering), immediately redirect unauthenticated visitors or visitors
  // with expired access tokens (and no refresh cookie) to /login.
  // This prevents SSR from rendering the protected shell (<AppHeader>, <AppSidebar>) which then
  // causes severe DOM hydration mismatches when client hydration rejects the expired token.
  const isServer = Boolean(import.meta.server || (typeof process !== 'undefined' && 'server' in process && process.server))
  if (isServer && isAuthRequired) {
    const tokenCookie = useCookie<string | null>('techdaily_token')
    const refreshCookie = useCookie<string | null>('refreshToken')

    const hasValidToken = Boolean(tokenCookie.value && !isJwtExpired(tokenCookie.value))
    const hasRefreshCookie = Boolean(refreshCookie.value)

    if (!hasValidToken && !hasRefreshCookie) {
      return navigateTo({
        path: '/login',
        query: { redirect: to.fullPath }
      })
    }
    return
  }

  // Client-side: if auth is required and user is not currently logged in,
  // attempt transparent background refresh before deciding to reject navigation
  if (isAuthRequired && !authStore.isLoggedIn) {
    const refreshed = await authStore.tryRefreshToken()
    if (!refreshed) {
      return navigateTo({
        path: '/login',
        query: { redirect: to.fullPath }
      })
    }
  }

  const hasToken = !!authStore.isLoggedIn

  // Logged-in users cannot visit guest auth paths like /login
  if (isGuestAuthPath && hasToken) {
    const redirectTarget =
      typeof to.query?.redirect === 'string' &&
      to.query.redirect.startsWith('/') &&
      !to.query.redirect.startsWith('/login')
        ? to.query.redirect
        : '/today'
    return navigateTo(redirectTarget)
  }
})

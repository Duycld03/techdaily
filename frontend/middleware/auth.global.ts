import { useAuthStore } from '~/stores/useAuthStore'

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
  const isDevExempt =
    to.path.startsWith('/playground') ||
    to.path.startsWith('/showcase')

  // Default-Deny: all routes require authentication unless explicitly exempted
  const isAuthRequired = !isGuestAuthPath && !isDevExempt

  // On SSR (server-side rendering), immediately redirect guests without cookies to /login.
  // Only defer to client-side hydration when a session cookie (techdaily_token or refreshToken)
  // is present, allowing client silent-refresh to attempt renewing an expired access token.
  const isServer = Boolean(import.meta.server || (typeof process !== 'undefined' && 'server' in process && process.server))
  if (isServer && isAuthRequired) {
    const tokenCookie = useCookie('techdaily_token')
    const refreshCookie = useCookie('refreshToken')

    if (!tokenCookie.value && !refreshCookie.value) {
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

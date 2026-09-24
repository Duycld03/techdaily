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

  // On SSR (server-side rendering), if no token is present, redirect to login
  if (import.meta.server && isAuthRequired) {
    if (authStore.token) {
      return
    }
    return navigateTo({
      path: '/login',
      query: { redirect: to.fullPath }
    })
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

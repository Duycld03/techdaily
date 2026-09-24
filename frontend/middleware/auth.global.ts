import { useAuthStore } from '~/stores/useAuthStore'

export default defineNuxtRouteMiddleware(async (to) => {
  const authStore = useAuthStore()

  // Always initialize store state from cookies and local storage
  authStore.init()
  const isGuestOnly = to.path === '/login'
  const isAuthRequired =
    to.path === '/' ||
    to.path.startsWith('/today') ||
    to.path.startsWith('/insights') ||
    to.path.startsWith('/roadmap') ||
    to.path.startsWith('/review') ||
    to.path.startsWith('/notes') ||
    to.path.startsWith('/profile') ||
    to.path.startsWith('/settings') ||
    to.path.startsWith('/quiz') ||
    to.path.startsWith('/library') ||
    to.path.startsWith('/read')
  // On SSR (server-side rendering), the server environment does not have access
  // to the browser's HttpOnly refreshToken cookie stored for the backend origin.
  // If a token is present (even if expired), allow SSR to proceed so the client
  // can execute transparent refresh during hydration without bouncing the user.
  if (import.meta.server && isAuthRequired) {
    if (authStore.token) {
      return
    }
    return navigateTo({
      path: '/login',
      query: { redirect: to.fullPath }
    })
  }

  // Client-side: if auth is required and user is not currently logged in (e.g. token expired),
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

  // Logged-in users cannot visit /login
  if (isGuestOnly && hasToken) {
    return navigateTo('/')
  }
})

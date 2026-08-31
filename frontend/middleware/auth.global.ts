export default defineNuxtRouteMiddleware((to) => {
  // Defer auth routing checks to client where localStorage / session state lives
  if (import.meta.server) return

  const authStore = useAuthStore()
  if (!authStore.isLoggedIn) {
    authStore.init()
  }

  const isGuestOnly = to.path === '/login'
  const isAuthRequired =
    to.path.startsWith('/review') ||
    to.path.startsWith('/notes') ||
    to.path.startsWith('/profile') ||
    to.path.startsWith('/settings')

  // Logged-in users cannot visit /login
  if (isGuestOnly && authStore.isLoggedIn) {
    return navigateTo('/today')
  }

  // Unauthenticated visitors cannot access protected pages
  if (isAuthRequired && !authStore.isLoggedIn) {
    return navigateTo({
      path: '/login',
      query: { redirect: to.fullPath }
    })
  }
})

export default defineNuxtRouteMiddleware((to) => {
  const authStore = useAuthStore()

  // Always initialize and validate token expiration
  authStore.init()

  const hasToken = !!authStore.isLoggedIn

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
    to.path.startsWith('/quiz')

  // Logged-in users cannot visit /login
  if (isGuestOnly && hasToken) {
    return navigateTo('/today')
  }

  // Unauthenticated visitors cannot access protected pages (both on SSR and Client)
  if (isAuthRequired && !hasToken) {
    const targetRedirect = to.fullPath === '/' ? '/today' : to.fullPath
    return navigateTo({
      path: '/login',
      query: { redirect: targetRedirect }
    })
  }
})

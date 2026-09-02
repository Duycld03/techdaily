export default defineNuxtRouteMiddleware((to) => {
  const tokenCookie = useCookie<string | null>('techdaily_token')
  const authStore = useAuthStore()

  if (!authStore.isLoggedIn) {
    authStore.init()
  }

  const hasToken = !!authStore.isLoggedIn || !!tokenCookie.value

  const isGuestOnly = to.path === '/login'
  const isAuthRequired =
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
    return navigateTo({
      path: '/login',
      query: { redirect: to.fullPath }
    })
  }
})

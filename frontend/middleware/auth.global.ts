export default defineNuxtRouteMiddleware((to) => {
  const authStore = useAuthStore()

  // Initialize from storage on client if needed
  if (import.meta.client && !authStore.isLoggedIn) {
    authStore.init()
  }

  const isGuestOnly = to.path === '/login'

  if (isGuestOnly && authStore.isLoggedIn) {
    return navigateTo('/today')
  }
})

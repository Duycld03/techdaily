import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export interface AuthUser {
  id: string
  email: string
  name: string
  preferredLocale: string
  avatarUrl?: string
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(null)
  const user = ref<AuthUser | null>(null)
  const isInitialized = ref(false)

  const isLoggedIn = computed(() => !!token.value)

  function init() {
    if (import.meta.client && !isInitialized.value) {
      token.value = localStorage.getItem('techdaily_token')
      const storedUser = localStorage.getItem('techdaily_user')
      if (storedUser) {
        try {
          user.value = JSON.parse(storedUser)
        } catch {
          user.value = null
        }
      }
      isInitialized.value = true
    }
  }

  async function devLogin() {
    const api = useApiClient()
    const response = await api.post<{ token: string; user: AuthUser }>('/api/v1/auth/dev-login')
    setSession(response.token, response.user)
    return response
  }

  async function googleLogin(idToken: string) {
    const api = useApiClient()
    const response = await api.post<{ token: string; user: AuthUser }>('/api/v1/auth/google', { idToken })
    setSession(response.token, response.user)
    return response
  }

  function setSession(newToken: string, newUser: AuthUser) {
    token.value = newToken
    user.value = newUser
    if (import.meta.client) {
      localStorage.setItem('techdaily_token', newToken)
      localStorage.setItem('techdaily_user', JSON.stringify(newUser))
    }
  }

  function logout() {
    token.value = null
    user.value = null
    if (import.meta.client) {
      localStorage.removeItem('techdaily_token')
      localStorage.removeItem('techdaily_user')
    }
  }

  return {
    token,
    user,
    isLoggedIn,
    init,
    devLogin,
    googleLogin,
    logout
  }
})

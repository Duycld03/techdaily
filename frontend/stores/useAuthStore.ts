import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useApiClient } from '~/composables/useApiClient'

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
    if (typeof window !== 'undefined' && !isInitialized.value) {
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

  async function login(email: string, password: string) {
    const api = useApiClient()
    const response = await api.post<{ token: string; user: AuthUser }>('/api/v1/auth/login', {
      email,
      password
    })
    setSession(response.token, response.user)
    return response
  }

  async function register(email: string, password: string, name?: string, locale: string = 'en') {
    const api = useApiClient()
    const response = await api.post<{ token: string; user: AuthUser }>('/api/v1/auth/register', {
      email,
      password,
      name,
      locale
    })
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
    if (typeof window !== 'undefined') {
      localStorage.setItem('techdaily_token', newToken)
      localStorage.setItem('techdaily_user', JSON.stringify(newUser))
    }
  }

  function logout() {
    token.value = null
    user.value = null
    if (typeof window !== 'undefined') {
      localStorage.removeItem('techdaily_token')
      localStorage.removeItem('techdaily_user')
    }
    if (typeof navigateTo === 'function') {
      navigateTo('/login')
    }
  }

  return {
    token,
    user,
    isLoggedIn,
    init,
    login,
    register,
    googleLogin,
    logout
  }
})

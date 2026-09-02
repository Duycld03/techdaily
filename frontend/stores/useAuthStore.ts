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
  const tokenCookie = useCookie<string | null>('techdaily_token', { maxAge: 60 * 60 * 24 * 30, path: '/' })
  const userCookie = useCookie<AuthUser | null>('techdaily_user', { maxAge: 60 * 60 * 24 * 30, path: '/' })

  const token = ref<string | null>(tokenCookie.value || null)
  const user = ref<AuthUser | null>(userCookie.value || null)
  const isInitialized = ref(false)

  const isLoggedIn = computed(() => !!token.value)
  const isAuthenticated = computed(() => !!token.value)

  function init() {
    if (!token.value && tokenCookie.value) {
      token.value = tokenCookie.value
    }
    if (!user.value && userCookie.value) {
      user.value = userCookie.value
    }

    if (typeof window !== 'undefined' && !isInitialized.value) {
      if (!token.value) {
        token.value = localStorage.getItem('techdaily_token')
      }
      if (!user.value) {
        const storedUser = localStorage.getItem('techdaily_user')
        if (storedUser) {
          try {
            user.value = JSON.parse(storedUser)
          } catch {
            user.value = null
          }
        }
      }
      if (token.value && !tokenCookie.value) {
        tokenCookie.value = token.value
      }
      if (user.value && !userCookie.value) {
        userCookie.value = user.value
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
    tokenCookie.value = newToken
    userCookie.value = newUser
    if (typeof window !== 'undefined') {
      localStorage.setItem('techdaily_token', newToken)
      localStorage.setItem('techdaily_user', JSON.stringify(newUser))
    }
  }

  function logout() {
    token.value = null
    user.value = null
    tokenCookie.value = null
    userCookie.value = null
    if (typeof window !== 'undefined') {
      localStorage.removeItem('techdaily_token')
      localStorage.removeItem('techdaily_user')
    }
    if (typeof navigateTo === 'function') {
      navigateTo('/login')
    }
  }

  function updateUser(updated: Partial<AuthUser>) {
    if (user.value) {
      user.value = { ...user.value, ...updated }
      userCookie.value = user.value
      if (typeof window !== 'undefined') {
        localStorage.setItem('techdaily_user', JSON.stringify(user.value))
      }
    }
  }

  return {
    token,
    user,
    isLoggedIn,
    isAuthenticated,
    init,
    login,
    register,
    googleLogin,
    updateUser,
    logout
  }
})

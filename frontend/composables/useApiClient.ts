import { useAuthStore } from '~/stores/useAuthStore'
import { useToast } from '~/composables/useToast'

export class ApiError extends Error {
  code?: string
  status: number
  details?: unknown
  data?: unknown

  constructor(message: string, status: number, code?: string, details?: unknown, data?: unknown) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.details = details
    this.data = data
    Object.setPrototypeOf(this, ApiError.prototype)
  }
}
interface LockManagerLike {
  request: (name: string, callback: () => Promise<string | null>) => Promise<string | null>
}

interface NavigatorWithLocks {
  locks: LockManagerLike
}

let inFlightRefreshPromise: Promise<string | null> | null = null

function parseJwtExp(jwt: string): number | null {
  try {
    const parts = jwt.split('.')
    if (parts.length < 2) return null
    const base64Url = parts[1]
    if (!base64Url) return null
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    let binaryStr = ''
    if (typeof atob !== 'undefined') {
      binaryStr = atob(base64)
    } else if (typeof Buffer !== 'undefined') {
      binaryStr = Buffer.from(base64, 'base64').toString('binary')
    } else {
      return null
    }
    const jsonPayload = decodeURIComponent(
      binaryStr
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    )
    const payload = JSON.parse(jsonPayload)
    return typeof payload.exp === 'number' ? payload.exp : null
  } catch {
    return null
  }
}

export function useApiClient() {
  const config = useRuntimeConfig()

  function getBaseUrl(): string {
    const configuredUrl = config.public.apiBaseUrl as string
    // If a custom API URL is explicitly configured (e.g. in production: https://api.yourdomain.com), use it directly
    if (configuredUrl && configuredUrl !== 'http://localhost:5000') {
      return configuredUrl
    }
    // In browser environment
    if (import.meta.client && typeof window !== 'undefined') {
      // Local development on port 3000 -> Backend is on port 5000
      if (window.location?.port === '3000') {
        const protocol = window.location.protocol || 'http:'
        const hostname = window.location.hostname
        return `${protocol}//${hostname}:5000`
      }
      // In production behind Nginx reverse proxy (port 80 or 443) -> relative path
      return ''
    }
    // Server-side inside Docker / SSR
    if (process.env.API_INTERNAL_URL) {
      return process.env.API_INTERNAL_URL
    }
    return configuredUrl || 'http://localhost:5000'
  }

  const baseUrl = getBaseUrl()

  function getAuthToken(): string | null {
    const tokenCookie = useCookie<string | null>('techdaily_token')
    if (tokenCookie.value) {
      return tokenCookie.value
    }
    if (import.meta.client && typeof window !== 'undefined') {
      return localStorage.getItem('techdaily_token')
    }
    return null
  }

  async function executeRefresh(): Promise<string | null> {
    const refreshUrl = `${baseUrl}/api/v1/auth/refresh`
    const res = await fetch(refreshUrl, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (!res.ok) {
      throw new Error(`Token refresh failed with status ${res.status}`)
    }

    const rawData: unknown = await res.json()
    let newToken: string | null = null
    if (rawData && typeof rawData === 'object') {
      if ('accessToken' in rawData && typeof rawData.accessToken === 'string') {
        newToken = rawData.accessToken
      } else if ('token' in rawData && typeof rawData.token === 'string') {
        newToken = rawData.token
      }
    }
    if (!newToken) {
      throw new Error('No access token returned from refresh')
    }

    try {
      const authStore = useAuthStore()
      authStore.setSession(newToken, authStore.user)
    } catch {
      const tokenCookie = useCookie<string | null>('techdaily_token')
      tokenCookie.value = newToken
      if (typeof window !== 'undefined') {
        localStorage.setItem('techdaily_token', newToken)
      }
    }

    return newToken
  }

  async function refreshAuthToken(): Promise<string | null> {
    if (typeof navigator !== 'undefined' && 'locks' in navigator) {
      const nav = navigator as unknown as NavigatorWithLocks
      return await nav.locks.request('techdaily_auth_refresh', async () => {
        const token = getAuthToken()
        if (token) {
          const exp = parseJwtExp(token)
          if (exp && exp * 1000 - Date.now() > 30 * 1000) {
            return token
          }
        }
        return await executeRefresh()
      })
    }

    if (!inFlightRefreshPromise) {
      inFlightRefreshPromise = executeRefresh().finally(() => {
        inFlightRefreshPromise = null
      })
    }
    return await inFlightRefreshPromise
  }

  async function request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const isAuthEndpoint =
      endpoint.includes('/api/v1/auth/login') ||
      endpoint.includes('/api/v1/auth/register') ||
      endpoint.includes('/api/v1/auth/refresh') ||
      endpoint.includes('/api/v1/auth/revoke') ||
      endpoint.includes('/api/v1/auth/google')

    if (!isAuthEndpoint) {
      const currentToken = getAuthToken()
      if (currentToken) {
        const exp = parseJwtExp(currentToken)
        if (exp && exp * 1000 - Date.now() <= 30 * 1000) {
          try {
            await refreshAuthToken()
          } catch {
            // Proactive refresh failed, proceed and let 401 handler manage it
          }
        }
      }
    }

    const token = getAuthToken()
    const headers: Record<string, string> = {
      ...(options.headers as Record<string, string> || {})
    }

    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }

    if (!(options.body instanceof FormData) && !headers['Content-Type']) {
      headers['Content-Type'] = 'application/json'
    }

    const response = await fetch(`${baseUrl}${endpoint}`, {
      ...options,
      credentials: options.credentials || 'include',
      headers
    })
    if (!response.ok) {
      if (
        response.status === 401 &&
        !endpoint.includes('/api/v1/auth/login') &&
        !endpoint.includes('/api/v1/auth/register') &&
        !endpoint.includes('/api/v1/auth/refresh') &&
        !endpoint.includes('/api/v1/auth/revoke') &&
        !endpoint.includes('/api/v1/auth/google')
      ) {
        let refreshedToken: string | null = null
        try {
          refreshedToken = await refreshAuthToken()
        } catch {
          // Token refresh failed
        }

        if (refreshedToken) {
          const retryHeaders: Record<string, string> = {
            ...(options.headers as Record<string, string> || {}),
            Authorization: `Bearer ${refreshedToken}`
          }

          if (!(options.body instanceof FormData) && !retryHeaders['Content-Type']) {
            retryHeaders['Content-Type'] = 'application/json'
          }

          const retryResponse = await fetch(`${baseUrl}${endpoint}`, {
            ...options,
            credentials: options.credentials || 'include',
            headers: retryHeaders
          })

          if (retryResponse.ok) {
            if (retryResponse.status === 204 || retryResponse.headers.get('content-length') === '0') {
              return {} as T
            }
            const text = await retryResponse.text()
            if (!text || text.trim() === '') {
              return {} as T
            }
            return JSON.parse(text)
          }
        }

        try {
          const authStore = useAuthStore()
          authStore.clearSession()
        } catch {
          // ignore if pinia is not active
        }

        try {
          const toast = useToast()
          let message = 'Session expired. Please log in again.'
          try {
            const { t } = useI18n()
            const localized = t('auth.session_expired')
            if (localized) {
              message = localized
            }
          } catch {
            // fallback if i18n composable is not available
          }
          toast.warning(message)
        } catch {
          // ignore
        }

        if (typeof window !== 'undefined') {
          const currentPath = window.location.pathname + window.location.search
          if (!window.location.pathname.startsWith('/login') && typeof navigateTo === 'function') {
            navigateTo({
              path: '/login',
              query: { redirect: currentPath }
            })
          }
        }
      }

      let errorMessage = `HTTP Error ${response.status}`
      let errorCode: string | undefined = undefined
      let errorDetails: unknown = undefined
      let errorData: unknown = undefined

      try {
        const errorJson = await response.json()
        errorData = errorJson
        errorMessage = errorJson.detail || errorJson.error || errorJson.message || errorJson.title || errorMessage
        errorCode = errorJson.code
        errorDetails = errorJson.details || errorJson.errors || errorJson.detail
      } catch {
        // fallback
      }

      if (!errorCode) {
        if (response.status === 401) errorCode = 'UNAUTHORIZED'
        else if (response.status === 403) errorCode = 'FORBIDDEN'
        else if (response.status === 404) errorCode = 'RESOURCE_NOT_FOUND'
        else if (response.status >= 500) errorCode = 'SERVER_ERROR'
      }

      throw new ApiError(errorMessage, response.status, errorCode, errorDetails, errorData)
    }
    if (response.status === 204 || response.headers.get('content-length') === '0') {
      return {} as T
    }

    const text = await response.text()
    if (!text || text.trim() === '') {
      return {} as T
    }

    return JSON.parse(text)
  }

  return {
    get: <T>(url: string, options?: RequestInit) => request<T>(url, { method: 'GET', ...options }),
    post: <T>(url: string, body?: unknown, options?: RequestInit) =>
      request<T>(url, {
        method: 'POST',
        body: body ? JSON.stringify(body) : undefined,
        ...options
      }),
    postRaw: <T>(url: string, body: FormData, options?: RequestInit) =>
      request<T>(url, {
        method: 'POST',
        body,
        ...options
      }),
    put: <T>(url: string, body?: unknown, options?: RequestInit) =>
      request<T>(url, {
        method: 'PUT',
        body: body ? JSON.stringify(body) : undefined,
        ...options
      }),
    delete: <T>(url: string, options?: RequestInit) => request<T>(url, { method: 'DELETE', ...options }),
    download: async (url: string, defaultFileName = 'export.md'): Promise<void> => {
      const token = getAuthToken()
      const headers: Record<string, string> = {}
      if (token) {
        headers['Authorization'] = `Bearer ${token}`
      }
      const response = await fetch(`${baseUrl}${url}`, {
        method: 'GET',
        credentials: 'include',
        headers
      })
      if (!response.ok) {
        throw new Error(`Failed to download file: ${response.statusText}`)
      }
      const blob = await response.blob()
      let fileName = defaultFileName
      const disposition = response.headers.get('Content-Disposition')
      if (disposition && disposition.includes('filename=')) {
        const match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/)
        if (match && match[1]) {
          fileName = match[1].replace(/['"]/g, '')
        }
      }
      if (typeof window !== 'undefined') {
        const blobUrl = window.URL.createObjectURL(blob)
        const a = document.createElement('a')
        a.href = blobUrl
        a.download = fileName
        document.body.appendChild(a)
        a.click()
        window.URL.revokeObjectURL(blobUrl)
        document.body.removeChild(a)
      }
    },
  }
}

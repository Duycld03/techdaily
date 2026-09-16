import { useAuthStore } from '~/stores/useAuthStore'
import { useToast } from '~/composables/useToast'

export class ApiError extends Error {
  code?: string
  status: number
  details?: any

  constructor(message: string, status: number, code?: string, details?: any) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.details = details
    Object.setPrototypeOf(this, ApiError.prototype)
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

  async function request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
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
      headers
    })

    if (!response.ok) {
      if (response.status === 401 && !endpoint.includes('/api/v1/auth/login') && !endpoint.includes('/api/v1/auth/register')) {
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
      let errorDetails: any = undefined

      try {
        const errorJson = await response.json()
        errorMessage = errorJson.error || errorJson.message || errorJson.title || errorMessage
        errorCode = errorJson.code
        errorDetails = errorJson.details || errorJson.errors
      } catch {
        // fallback
      }

      if (!errorCode) {
        if (response.status === 401) errorCode = 'UNAUTHORIZED'
        else if (response.status === 403) errorCode = 'FORBIDDEN'
        else if (response.status === 404) errorCode = 'RESOURCE_NOT_FOUND'
        else if (response.status >= 500) errorCode = 'SERVER_ERROR'
      }

      throw new ApiError(errorMessage, response.status, errorCode, errorDetails)
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
    get: <T>(url: string) => request<T>(url, { method: 'GET' }),
    post: <T>(url: string, body?: any) =>
      request<T>(url, {
        method: 'POST',
        body: body ? JSON.stringify(body) : undefined
      }),
    postRaw: <T>(url: string, body: FormData) =>
      request<T>(url, {
        method: 'POST',
        body
      }),
    put: <T>(url: string, body?: any) =>
      request<T>(url, {
        method: 'PUT',
        body: body ? JSON.stringify(body) : undefined
      }),
    delete: <T>(url: string) => request<T>(url, { method: 'DELETE' }),
    download: async (url: string, defaultFileName = 'export.md'): Promise<void> => {
      const token = getAuthToken()
      const headers: Record<string, string> = {}
      if (token) {
        headers['Authorization'] = `Bearer ${token}`
      }
      const response = await fetch(`${baseUrl}${url}`, {
        method: 'GET',
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

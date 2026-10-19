import { useAuthStore } from '~/stores/useAuthStore'
import { useToast } from '~/composables/useToast'
import { watch } from 'vue'
import { isAdminLogin, isAdminRoute, loginRedirectFor } from '~/utils/adminRoutes'
import { profileForToken } from '~/utils/clientStorage'

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
export interface ApiRequestOptions extends RequestInit {
  query?: Record<string, string | number | boolean | null | undefined>
}


let inFlightRefreshPromise: Promise<string | null> | null = null
let isRedirectingToLogin = false
let refreshOwner: ReturnType<typeof useAuthStore> | null = null
let refreshGeneration = -1

export function _resetRedirectingToLogin() {
  isRedirectingToLogin = false
}

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
  const auth = useAuthStore()
  const proc: unknown = typeof process !== 'undefined' ? process : undefined
  const isServer = Boolean(
    import.meta.server ||
    (proc && typeof proc === 'object' && 'server' in proc && Boolean(proc.server))
  )
  const baseUrl = (isServer ? process.env.API_INTERNAL_URL : '') || (config?.public?.apiBaseUrl as string | undefined)?.trim() || ''
  let translate: ((key: string) => string) | null = null
  try { translate = useNuxtApp().$i18n.t } catch {
    try { translate = useI18n().t } catch { /* Translation is optional outside Nuxt. */ }
  }

  function getAuthToken(): string | null {
    auth.init()
    return auth.token
  }

  async function executeRefresh(): Promise<string | null> {
    if (isServer) throw new ApiError('Session renewal requires client verification.', 503, 'SSR_REFRESH_PENDING')
    const generation = auth.sessionGeneration
    const refreshUrl = `${baseUrl}/api/v1/auth/refresh`
    let res: Response
    try {
      res = await fetch(refreshUrl, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json'
        }
      })
    } catch (networkErr: unknown) {
      const message = networkErr instanceof Error ? networkErr.message : 'Failed to fetch'
      throw Object.assign(new Error('Token refresh network error: ' + message), { isNetworkError: true })
    }

    if (!res.ok) {
      throw Object.assign(new Error(`Token refresh failed with status ${res.status}`), { status: res.status })
    }
    const rawData: unknown = await res.json()
    let newToken: string | null = null
    let returnedUser: unknown = null
    if (rawData && typeof rawData === 'object') {
      if ('accessToken' in rawData && typeof rawData.accessToken === 'string') {
        newToken = rawData.accessToken
      } else if ('token' in rawData && typeof rawData.token === 'string') {
        newToken = rawData.token
      }
      if ('user' in rawData && typeof rawData.user === 'object' && rawData.user !== null) {
        returnedUser = rawData.user
      }
    }
    if (!newToken) {
      throw new Error('No access token returned from refresh')
    }

    if (generation !== auth.sessionGeneration) throw new ApiError('The account changed during renewal.', 409, 'SESSION_CHANGED')
    await auth.setSession(newToken, profileForToken(newToken, returnedUser || auth.user))
    if (generation !== auth.sessionGeneration) throw new ApiError('The account changed during renewal.', 409, 'SESSION_CHANGED')

    return newToken
  }

  async function refreshAuthToken(): Promise<string | null> {
    if (typeof navigator !== 'undefined' && navigator.locks && typeof navigator.locks.request === 'function') {
      return await navigator.locks.request('deeppace_auth_refresh', async () => {
        const token = getAuthToken()
        if (token) {
          const exp = parseJwtExp(token)
          if (exp && exp * 1000 - Date.now() > 5 * 60 * 1000) {
            return token
          }
        }
        return await executeRefresh()
      })
    }

    if (!inFlightRefreshPromise || refreshOwner !== auth || refreshGeneration !== auth.sessionGeneration) {
      refreshOwner = auth
      refreshGeneration = auth.sessionGeneration
      const flight = executeRefresh().finally(() => {
        if (inFlightRefreshPromise === flight) inFlightRefreshPromise = null
      })
      inFlightRefreshPromise = flight
    }
    return await inFlightRefreshPromise
  }

  function redirectToLogin() {
    if (isServer || typeof window === 'undefined' || isRedirectingToLogin || typeof navigateTo !== 'function') return
    const currentPath = window.location.pathname + window.location.search
    if (isAdminLogin(window.location.pathname) || window.location.pathname === '/login') return
    isRedirectingToLogin = true
    Promise.resolve(navigateTo(loginRedirectFor(currentPath))).finally(() => {
      setTimeout(() => { isRedirectingToLogin = false }, 500)
    })
  }

  async function parseError(response: Response): Promise<ApiError> {
    let message = `HTTP Error ${response.status}`
    let code: string | undefined
    let details: unknown
    let data: unknown
    try {
      const body: unknown = await response.json()
      data = body
      if (body && typeof body === 'object') {
        const problem = body as Record<string, unknown>
        for (const field of ['detail', 'message', 'title'] as const) {
          const value = problem[field]
          if (typeof value === 'string' && value) {
            message = value
            break
          }
        }
        if ('code' in body && typeof body.code === 'string') code = body.code
        if (!code && 'extensions' in body && body.extensions && typeof body.extensions === 'object' &&
          'code' in body.extensions && typeof body.extensions.code === 'string') code = body.extensions.code
        if ('errors' in body) details = body.errors
      }
    } catch { /* Empty and non-JSON error bodies use the HTTP status. */ }
    code ||= response.status === 401 ? 'UNAUTHORIZED' : response.status === 403 ? 'FORBIDDEN'
      : response.status === 404 ? 'RESOURCE_NOT_FOUND' : response.status >= 500 ? 'SERVER_ERROR' : undefined
    return new ApiError(message, response.status, code, details, data)
  }

  async function performRequest<T>(endpoint: string, options: ApiRequestOptions, generation: number): Promise<T> {
    const isAuthEndpoint = endpoint.startsWith('/api/v1/auth/')
    if (!isAuthEndpoint && !isServer) {
      const token = getAuthToken()
      const exp = token ? parseJwtExp(token) : null
      if (exp && exp * 1000 - Date.now() <= 300_000) {
        try { await refreshAuthToken() } catch { /* A response will determine whether the session is rejected. */ }
      }
    }
    options.signal?.throwIfAborted()
    const headers: Record<string, string> = { ...(options.headers as Record<string, string> || {}) }
    const token = getAuthToken()
    if (token) headers.Authorization = `Bearer ${token}`
    if (!(options.body instanceof FormData) && !headers['Content-Type']) headers['Content-Type'] = 'application/json'
    let requestUrl = `${baseUrl}${endpoint}`
    if (options.query) {
      const searchParams = new URLSearchParams()
      for (const [key, val] of Object.entries(options.query)) {
        if (val !== undefined && val !== null && val !== '') {
          searchParams.set(key, String(val))
        }
      }
      const qs = searchParams.toString()
      if (qs) {
        requestUrl += (requestUrl.includes('?') ? '&' : '?') + qs
      }
    }
    let response = await fetch(requestUrl, { ...options, credentials: options.credentials || 'include', headers })
    if (generation !== auth.sessionGeneration) throw new ApiError('The account changed during the request.', 409, 'SESSION_CHANGED')

    if (response.status === 401 && !isAuthEndpoint && !isServer) {
      try {
        const renewed = await refreshAuthToken()
        options.signal?.throwIfAborted()
        if (renewed) response = await fetch(requestUrl, {
          ...options, credentials: options.credentials || 'include',
          headers: { ...headers, Authorization: `Bearer ${renewed}` }
        })
      } catch (error: unknown) {
        const failure = error as { status?: number; isNetworkError?: boolean; code?: string }
        if (failure.code === 'SESSION_CHANGED' || generation !== auth.sessionGeneration) throw error
        if (options.signal?.aborted) throw error
        if (failure.isNetworkError || (failure.status !== 401 && failure.status !== 403)) {
          throw new ApiError('Authentication service temporarily unavailable', 503, 'SERVICE_UNAVAILABLE')
        }
      }
      if (generation !== auth.sessionGeneration) throw new ApiError('The account changed during the request.', 409, 'SESSION_CHANGED')
      if (response.status === 401) {
        await auth.clearSession()
        try { useToast().warning(translate?.('auth.session_expired') || 'Session expired. Please log in again.') } catch { /* No toast outside UI context. */ }
        redirectToLogin()
      }
    }
    if (response.status === 403 && endpoint.startsWith('/api/v1/admin/') && !isServer) {
      auth.invalidateAdminAccess()
      if (typeof window !== 'undefined' && isAdminRoute(window.location.pathname)) redirectToLogin()
    }
    if (!response.ok) throw await parseError(response)
    if (response.status === 204 || response.headers.get('content-length') === '0') return {} as T
    const text = await response.text()
    return text.trim() ? JSON.parse(text) : {} as T
  }

  async function request<T>(endpoint: string, options: ApiRequestOptions = {}): Promise<T> {
    const generation = auth.sessionGeneration
    const privileged = endpoint.startsWith('/api/v1/admin/') || endpoint === '/api/v1/catalog/capabilities'
    if (!privileged) return performRequest<T>(endpoint, options, generation)
    const controller = new AbortController()
    const stop = watch([() => auth.user?.id, () => auth.sessionGeneration, () => auth.adminGeneration],
      () => controller.abort(), { flush: 'sync' })
    const onAbort = () => controller.abort()
    if (options.signal?.aborted) controller.abort()
    options.signal?.addEventListener('abort', onAbort, { once: true })
    try {
      const result = await performRequest<T>(endpoint, { ...options, signal: controller.signal }, generation)
      controller.signal.throwIfAborted()
      return result
    } finally {
      stop()
      options.signal?.removeEventListener('abort', onAbort)
    }
  }

  return {
    get: <T>(url: string, options?: ApiRequestOptions) => request<T>(url, { method: 'GET', ...options }),
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
    patch: <T>(url: string, body?: unknown, options?: RequestInit) =>
      request<T>(url, {
        method: 'PATCH',
        body: body ? JSON.stringify(body) : undefined,
        ...options
      }),
    delete: <T>(url: string, options?: ApiRequestOptions) => request<T>(url, { method: 'DELETE', ...options }),
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
    refreshAuthToken
  }
}

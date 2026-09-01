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
    if (import.meta.client) {
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
      let errorMessage = `HTTP Error ${response.status}`
      try {
        const errorJson = await response.json()
        errorMessage = errorJson.error || errorJson.title || errorMessage
      } catch {
        // fallback
      }
      throw new Error(errorMessage)
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
    delete: <T>(url: string) => request<T>(url, { method: 'DELETE' })
  }
}

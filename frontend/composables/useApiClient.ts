export function useApiClient() {
  const config = useRuntimeConfig()
  const baseUrl = config.public.apiBaseUrl as string

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

    return response.json()
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
    delete: <T>(url: string) => request<T>(url, { method: 'DELETE' })
  }
}

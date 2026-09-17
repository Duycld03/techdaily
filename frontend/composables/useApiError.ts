export function useApiError() {
  let i18n: { t: (key: string, ...args: unknown[]) => string; te?: (key: string) => boolean } | null = null
  try {
    i18n = useI18n()
  } catch {
    // ignore if i18n is not loaded in current context
  }

  const t = (key: string, ...args: unknown[]) => (i18n ? i18n.t(key, ...args) : key)
  const te = (key: string) => {
    if (!i18n) return false
    if (typeof i18n.te === 'function') return i18n.te(key)
    const val = i18n.t(key)
    return Boolean(val && val !== key)
  }

  function formatError(err: unknown, fallbackKey?: string): string {
    if (!err) {
      return fallbackKey && te(fallbackKey) ? t(fallbackKey) : ''
    }

    const resolveFallback = (): string => {
      if (fallbackKey && te(fallbackKey)) {
        return t(fallbackKey)
      }
      if (fallbackKey && !i18n) {
        return fallbackKey
      }
      if (te('api_errors.SERVER_ERROR')) {
        return t('api_errors.SERVER_ERROR')
      }
      return 'An unexpected error occurred. Please try again.'
    }

    if (typeof err === 'string') {
      if (/^\d+$/.test(err.trim())) {
        return resolveFallback()
      }
      if (te(err)) {
        return t(err)
      }
      return err
    }

    const errorObj = typeof err === 'object' && err !== null ? (err as Record<string, unknown>) : null

    const message = errorObj && typeof errorObj.message === 'string' ? errorObj.message : undefined
    const isNumericMessage = typeof message === 'string' && /^\d+$/.test(message.trim())

    // Network / offline failure detection
    const isNetworkError =
      errorObj?.name === 'TypeError' &&
      typeof message === 'string' &&
      (message.includes('fetch') ||
        message.includes('network') ||
        message.includes('Failed to fetch') ||
        message.includes('NetworkError'))

    if (isNetworkError && te('api_errors.NETWORK_ERROR')) {
      return t('api_errors.NETWORK_ERROR')
    }

    // Extract response data (from ofetch, axios, or error itself)
    const responseData = (errorObj?.data || (errorObj?.response as Record<string, unknown> | undefined)?._data || errorObj) as Record<string, unknown> | undefined

    const statusCode =
      (typeof responseData?.status === 'number' ? responseData.status : undefined) ||
      (typeof errorObj?.status === 'number' ? errorObj.status : undefined) ||
      (typeof errorObj?.statusCode === 'number' ? errorObj.statusCode : undefined) ||
      (typeof (errorObj?.response as Record<string, unknown> | undefined)?.status === 'number'
        ? ((errorObj?.response as Record<string, unknown>).status as number)
        : undefined)

    if (statusCode === 500) {
      if (fallbackKey && te(fallbackKey)) {
        return t(fallbackKey)
      }
      if (fallbackKey && !i18n) {
        return fallbackKey
      }
      if (te('api_errors.SERVER_ERROR')) {
        return t('api_errors.SERVER_ERROR')
      }
      return i18n ? 'An unexpected server error occurred. Please try again.' : 'api_errors.SERVER_ERROR'
    }

    // 1. Check for machine-readable error code FIRST against i18n
    const code =
      (typeof responseData?.code === 'string' ? responseData.code : undefined) ||
      (typeof errorObj?.code === 'string' ? errorObj.code : undefined)

    if (code && te(`api_errors.${code}`)) {
      return t(`api_errors.${code}`)
    }

    // 2. Check for backend detail and error messages BEFORE falling back to fallbackKey:
    if (responseData && typeof responseData === 'object') {
      // If responseData?.title and responseData?.detail, return `${responseData.title}: ${responseData.detail}`
      if (typeof responseData.title === 'string' && typeof responseData.detail === 'string') {
        return `${responseData.title}: ${responseData.detail}`
      }
      // If responseData?.detail (RFC 7807 standard problem details), return responseData.detail
      if (typeof responseData.detail === 'string') {
        return responseData.detail
      }
      // If responseData?.error (string), return responseData.error
      if (typeof responseData.error === 'string') {
        return responseData.error
      }
    }

    // Only if none of the above are present, fall back to fallbackKey ? t(fallbackKey) : t('common.error')
    if (fallbackKey && te(fallbackKey)) {
      return t(fallbackKey)
    }
    if (fallbackKey && !i18n) {
      return fallbackKey
    }

    // Meaningful developer / validation message if not a raw HTTP error code and not pure numeric
    if (
      message &&
      !message.startsWith('HTTP Error') &&
      !isNumericMessage
    ) {
      return message
    }

    // Final fallback: generic server error or English default
    if (te('api_errors.SERVER_ERROR')) {
      return t('api_errors.SERVER_ERROR')
    }
    if (te('common.error')) {
      return t('common.error')
    }

    return (
      (message && !isNumericMessage ? message : '') ||
      (fallbackKey ? t(fallbackKey) : (te('common.error') ? t('common.error') : 'An unexpected error occurred. Please try again.'))
    )
  }

  return {
    formatError,
    t
  }
}

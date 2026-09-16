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

    // Check machine-readable error code (e.g. AUTH_INVALID_CREDENTIALS, RESOURCE_NOT_FOUND)
    if (errorObj && typeof errorObj.code === 'string') {
      const i18nKey = `api_errors.${errorObj.code}`
      if (te(i18nKey)) {
        return t(i18nKey)
      }
    }

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

    // Specific fallback key passed by caller
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

    return (
      (message && !isNumericMessage ? message : '') ||
      'An unexpected error occurred. Please try again.'
    )
  }

  return {
    formatError,
    t
  }
}

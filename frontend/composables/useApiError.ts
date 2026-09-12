export function useApiError() {
  let i18n: { t: (key: string, ...args: any[]) => string; te?: (key: string) => boolean } | null = null
  try {
    i18n = useI18n()
  } catch {
    // ignore if i18n is not loaded in current context
  }

  const t = (key: string, ...args: any[]) => (i18n ? i18n.t(key, ...args) : key)
  const te = (key: string) => {
    if (!i18n) return false
    if (typeof i18n.te === 'function') return i18n.te(key)
    const val = i18n.t(key)
    return Boolean(val && val !== key)
  }

  function formatError(err: any, fallbackKey?: string): string {
    if (!err) {
      return fallbackKey && te(fallbackKey) ? t(fallbackKey) : ''
    }

    if (typeof err === 'string') {
      if (te(err)) {
        return t(err)
      }
      return err
    }

    // Check machine-readable error code (e.g. AUTH_INVALID_CREDENTIALS, RESOURCE_NOT_FOUND)
    if (err.code && typeof err.code === 'string') {
      const i18nKey = `api_errors.${err.code}`
      if (te(i18nKey)) {
        return t(i18nKey)
      }
    }

    // Network / offline failure detection
    const isNetworkError =
      err.name === 'TypeError' &&
      typeof err.message === 'string' &&
      (err.message.includes('fetch') ||
        err.message.includes('network') ||
        err.message.includes('Failed to fetch') ||
        err.message.includes('NetworkError'))

    if (isNetworkError && te('api_errors.NETWORK_ERROR')) {
      return t('api_errors.NETWORK_ERROR')
    }

    // Specific fallback key passed by caller
    if (fallbackKey && te(fallbackKey)) {
      return t(fallbackKey)
    }

    // Meaningful developer / validation message if not a raw HTTP error code
    if (err.message && typeof err.message === 'string' && !err.message.startsWith('HTTP Error')) {
      return err.message
    }

    // Final fallback: generic server error or English default
    if (te('api_errors.SERVER_ERROR')) {
      return t('api_errors.SERVER_ERROR')
    }

    return err.message || 'An unexpected error occurred. Please try again.'
  }

  return {
    formatError,
    t
  }
}

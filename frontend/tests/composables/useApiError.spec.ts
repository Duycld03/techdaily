import { describe, it, expect, vi } from 'vitest'
import { useApiError } from '~/composables/useApiError'
import { ApiError } from '~/composables/useApiClient'

describe('useApiError', () => {
  it('resolves machine-readable error code via api_errors namespace', () => {
    const { formatError } = useApiError()
    const error = new ApiError('Invalid email or password.', 400, 'AUTH_INVALID_CREDENTIALS')
    const result = formatError(error)
    expect(result).toBe('api_errors.AUTH_INVALID_CREDENTIALS')
  })

  it('handles network / fetch failure errors gracefully', () => {
    const { formatError } = useApiError()
    const networkError = new TypeError('Failed to fetch')
    const result = formatError(networkError)
    expect(result).toBe('api_errors.NETWORK_ERROR')
  })

  it('uses fallbackKey when error code is missing or not translated', () => {
    const { formatError } = useApiError()
    const error = new Error('HTTP Error 500')
    const result = formatError(error, 'today.error_submit_failed')
    expect(result).toBe('today.error_submit_failed')
  })

  it('returns custom error message if provided and not generic HTTP error', () => {
    const { formatError } = useApiError()
    const error = new Error('Custom validation detail')
    const result = formatError(error)
    expect(result).toBe('Custom validation detail')
  })

  it('returns empty string for null or undefined error without fallback', () => {
    const { formatError } = useApiError()
    expect(formatError(null)).toBe('')
    expect(formatError(undefined)).toBe('')
  })

  it('returns string directly if error is passed as string', () => {
    const { formatError } = useApiError()
    expect(formatError('Direct string error')).toBe('Direct string error')
  })

  it('sanitizes pure numeric error string "26" with fallbackKey or default', () => {
    const { formatError } = useApiError()
    expect(formatError('26', 'notes.toast_flashcard_error')).toBe('notes.toast_flashcard_error')
    expect(formatError('26')).toBe('api_errors.SERVER_ERROR')
  })

  it('sanitizes Error with pure numeric message "26" with fallbackKey or default', () => {
    const { formatError } = useApiError()
    const err = new Error('26')
    expect(formatError(err, 'notes.toast_flashcard_error')).toBe('notes.toast_flashcard_error')
    expect(formatError(err)).toBe('api_errors.SERVER_ERROR')
  })

  it('returns responseData.detail (RFC 7807) even when fallbackKey is provided', () => {
    const { formatError } = useApiError()
    const err = { data: { detail: 'AI term explanation is temporarily unavailable' } }
    expect(formatError(err, 'today.error_explain_failed')).toBe('AI term explanation is temporarily unavailable')
  })

  it('returns responseData.error string when present', () => {
    const { formatError } = useApiError()
    const err = { response: { _data: { error: 'Invalid payload provided' } } }
    expect(formatError(err, 'today.error_submit_failed')).toBe('Invalid payload provided')
  })

  it('returns combined title and detail when both are present', () => {
    const { formatError } = useApiError()
    const err = { data: { title: 'Service Unavailable', detail: 'Gemini model is overloaded' } }
    expect(formatError(err, 'today.error_submit_failed')).toBe('Service Unavailable: Gemini model is overloaded')
  })

  it('translates responseData.code when present in response body', () => {
    const { formatError } = useApiError()
    const err = { data: { code: 'RESOURCE_NOT_FOUND' } }
    expect(formatError(err, 'today.error_submit_failed')).toBe('api_errors.RESOURCE_NOT_FOUND')
  })

  it('maps HTTP 500 ProblemDetails to fallbackKey when fallbackKey is provided', () => {
    const { formatError } = useApiError()
    const err = {
      status: 500,
      data: { title: 'Server Error', detail: 'An unhandled exception occurred.', status: 500 }
    }
    expect(formatError(err, 'today.explain_error')).toBe('today.explain_error')
  })

  it('maps HTTP 500 ProblemDetails to api_errors.SERVER_ERROR when no fallbackKey provided', () => {
    const { formatError } = useApiError()
    const err = {
      status: 500,
      data: { title: 'Server Error', detail: 'An unhandled exception occurred.', status: 500 }
    }
    expect(formatError(err)).toBe('api_errors.SERVER_ERROR')
  })

  it('does not return raw English title: detail string on HTTP 500', () => {
    const { formatError } = useApiError()
    const err = {
      response: { status: 500, _data: { title: 'Internal Server Error', detail: 'Database connection failed' } }
    }
    const result = formatError(err, 'today.explain_error')
    expect(result).not.toContain('Database connection failed')
    expect(result).toBe('today.explain_error')
  })

  it('prioritizes api_errors.<CODE> localized translation when apiError.code matches an existing translation key', () => {
    const { formatError } = useApiError()
    const error = new ApiError(
      'All push notification subscriptions for this device have expired.',
      400,
      'PUSH_SUBSCRIPTION_EXPIRED',
      null,
      {
        code: 'PUSH_SUBSCRIPTION_EXPIRED',
        error: 'All push notification subscriptions for this device have expired.'
      }
    )
    const result = formatError(error, 'settings.web_push_test_error')
    expect(result).toBe('api_errors.PUSH_SUBSCRIPTION_EXPIRED')
  })

  it('prioritizes api_errors.<CODE> over responseData.detail and responseData.error', () => {
    const { formatError } = useApiError()
    const error = {
      data: {
        code: 'PUSH_NO_SUBSCRIPTIONS',
        error: 'No registered devices found for notifications.',
        detail: 'Detailed backend message'
      }
    }
    const result = formatError(error, 'settings.web_push_test_error')
    expect(result).toBe('api_errors.PUSH_NO_SUBSCRIPTIONS')
  })
})

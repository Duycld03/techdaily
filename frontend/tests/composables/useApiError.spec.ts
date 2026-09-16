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
})

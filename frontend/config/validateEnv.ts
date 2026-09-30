/**
 * Validates the frontend API base URL environment variable.
 * Enforces fail-fast startup in development if missing from .env.
 */
export function validateApiBaseUrl(
  env: { NODE_ENV?: string; NUXT_PUBLIC_API_BASE_URL?: string },
  isPrepareOrTypecheck: boolean = false
): string {
  const apiBaseUrl = env.NUXT_PUBLIC_API_BASE_URL
  if (!isPrepareOrTypecheck && env.NODE_ENV !== 'production' && typeof apiBaseUrl === 'undefined') {
    throw new Error(
      '❌ [Config Error] Missing NUXT_PUBLIC_API_BASE_URL in environment. Please define it in your root .env file (e.g. NUXT_PUBLIC_API_BASE_URL=http://localhost:5000).'
    )
  }
  return apiBaseUrl ?? ''
}

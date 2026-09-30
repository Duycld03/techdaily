# Design

## Context

See `proposal.md` for motivation. Currently, `frontend/nuxt.config.ts`, `frontend/composables/useApiClient.ts`, `frontend/composables/useSliceAudio.ts`, and `run-dev.sh` contain redundant fallback logic (`|| 'http://localhost:5000'`) and heuristic checks (`window.location.port === '3000'`). This obscures missing environment configuration and breaks local network / mobile testing.

## Goals / Non-Goals

**Goals:**
- Enforce `.env` as the single source of truth for the API base URL in development.
- Implement fail-fast validation in `nuxt.config.ts` during server startup and build.
- Remove all port-guessing heuristics (`port === '3000'`) and hardcoded fallback strings from composables.
- Ensure production Docker Compose deployments with relative proxying (`NUXT_PUBLIC_API_BASE_URL=""`) continue to function cleanly.

**Non-Goals:**
- Modifying backend endpoint URLs or routing.
- Changing test mocks in `frontend/tests/setup.ts` where fixed URLs are intentionally provided for isolated Happy-DOM tests.

## Decisions

### 1. Fail-Fast Validation in `nuxt.config.ts`
Validate `process.env.NUXT_PUBLIC_API_BASE_URL` when `nuxt.config.ts` is parsed:
```ts
const apiBaseUrl = process.env.NUXT_PUBLIC_API_BASE_URL

if (process.env.NODE_ENV !== 'production' && typeof apiBaseUrl === 'undefined') {
  throw new Error(
    '❌ [Config Error] Missing NUXT_PUBLIC_API_BASE_URL in environment. Please define it in your root .env file (e.g. NUXT_PUBLIC_API_BASE_URL=http://localhost:5000).'
  )
}
```
And set `public.apiBaseUrl = apiBaseUrl ?? ''`.

### 2. Composable URL Simplification
In `frontend/composables/useApiClient.ts` and `frontend/composables/useSliceAudio.ts`:
- Delete the `configuredUrl !== 'http://localhost:5000'` check.
- Delete the `window.location.port === '3000'` heuristic.
- Delete the `return configuredUrl || 'http://localhost:5000'` fallback.
- Streamline `getBaseUrl()`:
```ts
function getBaseUrl(): string {
  // SSR container-to-container internal routing override if present
  if (process.env.API_INTERNAL_URL) {
    return process.env.API_INTERNAL_URL
  }
  return (config.public.apiBaseUrl as string) || ''
}
```

### 3. Environment Variable Ingestion in `run-dev.sh` and `.env`
- Add `NUXT_PUBLIC_API_BASE_URL=http://localhost:5000` to `.env`.
- In `run-dev.sh`, change:
  ```bash
  export NUXT_PUBLIC_API_BASE_URL="${NUXT_PUBLIC_API_BASE_URL}"
  ```
  removing the implicit `:-http://localhost:5000` default.

## Risks / Trade-offs

- **Risk:** Developers running `npm run dev` directly within the `frontend/` directory without running `run-dev.sh` or having exported `NUXT_PUBLIC_API_BASE_URL` will see a build error.
  - **Mitigation:** The error message explicitly directs them to add the variable to the root `.env` or run `./run-dev.sh`.
- **Trade-off:** Strictness over convenience: prevents silent misconfiguration at the cost of requiring `.env` presence.

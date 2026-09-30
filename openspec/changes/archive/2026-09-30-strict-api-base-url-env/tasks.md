# Tasks

## 1. Environment & Scripts

- [x] 1.1 Add `NUXT_PUBLIC_API_BASE_URL=http://localhost:5000` to `.env` and `.env.example`
- [x] 1.2 Update `run-dev.sh` to remove default fallback `:-http://localhost:5000` and source directly from `.env`

## 2. Frontend Configuration & Fail-Fast Validation

- [x] 2.1 Update `frontend/nuxt.config.ts` to validate presence of `NUXT_PUBLIC_API_BASE_URL` in non-production environments and throw an explicit configuration error if missing or undefined
- [x] 2.2 Remove all hardcoded `|| 'http://localhost:5000'` fallbacks from `frontend/nuxt.config.ts`

## 3. Frontend Composables Cleanup

- [x] 3.1 Streamline `frontend/composables/useApiClient.ts` `getBaseUrl()` by removing hardcoded localhost fallbacks, custom string checks, and port-3000 inspection
- [x] 3.2 Streamline `frontend/composables/useSliceAudio.ts` `getFetchClient()` by removing redundant port heuristics and hardcoded fallback strings
- [x] 3.3 Verify and update unit test assertions in `frontend/tests/composables/` to confirm base URL resolution strictly follows `runtimeConfig.public.apiBaseUrl`

## 4. Verification & Regression Testing

- [x] 4.1 Run full frontend test suite (`npm --prefix frontend test`) to verify all data contracts, composables, and stores pass
- [x] 4.2 Verify dev server fail-fast behavior when `NUXT_PUBLIC_API_BASE_URL` is unset and successful boot when set via `.env`

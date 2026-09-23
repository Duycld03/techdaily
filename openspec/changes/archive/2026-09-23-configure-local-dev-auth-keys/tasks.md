# Tasks

## 1. Frontend - Runtime Configuration & Auth Guards

- [x] 1.1 Update `frontend/nuxt.config.ts` to support dual environment variable resolution for Google Client ID (`process.env.NUXT_PUBLIC_GOOGLE_CLIENT_ID || process.env.GOOGLE_CLIENT_ID || ''`)
- [x] 1.2 Add defensive initialization guard in `frontend/pages/login.vue` to avoid calling `google.accounts.id.initialize` when Client ID is empty and log an informative developer warning
- [x] 1.3 Ensure unit tests in `frontend/tests/setup.ts` and `frontend/tests/` verify both configured and unconfigured Google Client ID runtime paths

## 2. Infrastructure & Developer Scripting

- [x] 2.1 Update `run-dev.sh` to explicitly export `NUXT_PUBLIC_GOOGLE_CLIENT_ID="${NUXT_PUBLIC_GOOGLE_CLIENT_ID:-$GOOGLE_CLIENT_ID}"` and `NUXT_PUBLIC_API_BASE_URL` when sourcing `.env`
- [x] 2.2 Update `.env.example` to document `NUXT_PUBLIC_GOOGLE_CLIENT_ID` and cross-platform local development environment variables
- [x] 2.3 Verify local `.env` configuration contains active developer credentials matching `appsettings.Development.json`
## 3. Verification & End-to-End Testing

- [x] 3.1 Run frontend unit tests (`npm --prefix frontend test`) to ensure zero regressions across auth and store suites
- [x] 3.2 Verify Google Sign-In button initializes on `http://localhost:3000/login` with the resolved Client ID without `client_id` missing parameter errors
- [x] 3.3 Verify full login flow, JWT token issuance, and protected page access (`/today`, `/review`, `/library`) on localhost

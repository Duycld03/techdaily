# Proposal

## Why

Hardcoded `|| 'http://localhost:5000'` fallbacks and dynamic port-guessing heuristics (`window.location.port === '3000'`) in frontend runtime config and composables obscure missing environment variables, violate the single-source-of-truth configuration pillar, and cause network failures when testing across local area networks (LAN/mobile IPs) or custom domains. Transitioning to explicit `.env` variable ingestion with fail-fast initialization ensures missing configuration halts the process immediately with actionable diagnostics rather than silently routing API traffic to localhost.

## What Changes

- **Strict Environment Ingestion**: Add `NUXT_PUBLIC_API_BASE_URL` explicitly to the root `.env` template and file.
- **Fail-Fast Configuration Validation**: In `frontend/nuxt.config.ts`, eliminate all `|| 'http://localhost:5000'` fallback chains and enforce fail-fast validation that throws an explicit configuration error during build or dev server boot if `NUXT_PUBLIC_API_BASE_URL` is undefined or empty outside production.
- **Composable Heuristic Removal**: Remove hardcoded port-3000 inspection and `localhost:5000` fallback branches from `frontend/composables/useApiClient.ts` and `frontend/composables/useSliceAudio.ts`, delegating 100% of the base URL resolution to validated `config.public.apiBaseUrl`.
- **Development Script Cleanup**: Update `run-dev.sh` to remove the default fallback value (`:-http://localhost:5000`) so it strictly sources `NUXT_PUBLIC_API_BASE_URL` from `.env`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Add strict frontend API base URL environment variable validation requirement and eliminate implicit localhost fallbacks.

## Impact

- **Frontend Configuration**: Requires `NUXT_PUBLIC_API_BASE_URL` in `.env` for development. In production environments (Docker Compose with Nginx reverse proxy), `NUXT_PUBLIC_API_BASE_URL=""` remains valid for same-origin relative `/api/v1` routing.
- **Developer Experience**: Eliminates silent fallbacks and network misdirection when developing across devices on local networks.

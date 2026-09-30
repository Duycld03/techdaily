# Spec Delta: Core Platform

## ADDED Requirements

### Requirement: Strict API Base URL Environment Configuration
The frontend application SHALL strictly require the API base URL to be configured via the environment (`NUXT_PUBLIC_API_BASE_URL`). The system SHALL NOT use hardcoded localhost fallbacks (`http://localhost:5000`) or client-side port inspection heuristics (`window.location.port === '3000'`). If `NUXT_PUBLIC_API_BASE_URL` is undefined or missing in non-production environments, the frontend configuration SHALL throw an explicit error at startup, halting execution before serving requests.

#### Scenario: Frontend starts with defined API base URL in development
- **GIVEN** `NUXT_PUBLIC_API_BASE_URL` is set to `http://localhost:5000` in `.env`
- **WHEN** the frontend development server initializes
- **THEN** runtime configuration populates `apiBaseUrl` with `http://localhost:5000` without throwing an error
- **AND** all client and server API requests use `http://localhost:5000` as the target origin.

#### Scenario: Frontend starts without API base URL in development
- **GIVEN** `NUXT_PUBLIC_API_BASE_URL` is not set or undefined in the environment
- **AND** `NODE_ENV` is not `production`
- **WHEN** the frontend development server boots or evaluates `nuxt.config.ts`
- **THEN** the application throws an explicit configuration error identifying the missing `NUXT_PUBLIC_API_BASE_URL` variable
- **AND** the server halts startup immediately without falling back to `http://localhost:5000`.

#### Scenario: Production build with same-origin reverse proxy
- **GIVEN** `NODE_ENV` is `production` and `NUXT_PUBLIC_API_BASE_URL` is set to an empty string (`""`)
- **WHEN** the frontend boots in a containerized environment behind Nginx
- **THEN** the configuration accepts the empty string as valid
- **AND** API requests resolve to relative paths (`/api/v1/...`) on the same origin.

# Proposal: Auth-First Routing & Full-Screen Studio Auth Canvas

## Why

Currently, the web frontend utilizes an ad-hoc route allowlist that leaves several pages and guest pathways ambiguous, and the root URL `/` renders an executive dashboard even before a user authenticates. Furthermore, backend document reading endpoints (`/api/v1/library/books/*`) permit anonymous access, creating an inconsistent security perimeter. On the frontend, `/login` still renders within the primary application shell chrome (showing `AppHeader` controls meant for authenticated users), creating visual clutter. Transitioning to a strict **Auth-First architecture** ensures that unauthenticated visitors are immediately greeted with a dedicated, full-screen Studio Auth Canvas, eliminating guest browsing and requiring users to authenticate before accessing any platform capabilities.

## What Changes

- **Default-Deny Frontend Route Guard (`frontend/middleware/auth.global.ts`)**:
  - Replace the brittle route allowlist with a strict **Default-Deny** gate: all platform routes require authentication by default, with only explicit guest authentication routes (`/login`, `/register`, `/forgot-password`, `/reset-password`) and local development playgrounds (`/playground/*`, `/showcase`) exempted.
  - Redirect all unauthenticated visits to `/login` while preserving the requested target URL in the `redirect` query parameter.
  - Redirect authenticated users attempting to access `/login` directly to their intended destination or `/today`.
- **Backend Endpoints Authorization Lock-Down (`backend/src/TechDaily.Api/Endpoints/LibraryEndpoints.cs`)**:
  - **BREAKING**: Enforce `.RequireAuthorization()` on all library endpoints (`/api/v1/library/books`, `/api/v1/library/books/{id}`, `/api/v1/library/books/{id}/status`, `/api/v1/library/books/{id}/slices/{order}`, and curate endpoints). All anonymous requests will now return `401 Unauthorized`.
- **Full-Screen Studio Auth Canvas (`frontend/pages/login.vue` & `frontend/app.vue`)**:
  - Completely suppress `AppHeader` and `AppSidebar` on authentication routes (`isAuthPage`), granting `/login` a dedicated full-screen viewport (`h-[100dvh] w-screen overflow-hidden`).
  - Implement top-right ambient utilities (`ThemeToggle.vue` and `LocaleSelector.vue`) directly on the Auth Canvas, enabling instant theme and language switching without depending on the internal application header.
  - Implement a clean state machine for `authMode` (`login`, `register`, `forgot-password`) with smooth mode transitions and zero layout shifts.
  - Implement the desktop 2-column Studio Auth Archetype: Left Column displays Platform Identity & Core Learning Pillars; Right Column displays the interactive glass card (`.glass-panel`).
- **Testing & Quality Assurance Following Pillar 3**:
  - Unit tests in Vitest strictly validate data contracts: redirect on unauthenticated access, form submission payload serialization (`authStore.login`, `authStore.register`), and validation constraints.
  - Zero brittle CSS class assertions (`expect(el.classes()).toContain(...)`).
  - Visual layout and responsive geometry verified via browser screenshot preview.

## Capabilities

### New Capabilities
*None.*

### Modified Capabilities
- `auth`: Define default-deny routing rules, unauthenticated redirection to `/login?redirect=...`, and the full-screen Studio Auth Canvas specification.
- `library`: Enforce authentication across all catalog browsing, book retrieval, ingestion status, and slice reading endpoints.

## Impact

- **Frontend Code**: `frontend/middleware/auth.global.ts`, `frontend/app.vue`, `frontend/pages/login.vue`, `frontend/tests/middleware/auth.spec.ts`, `frontend/tests/pages/login.spec.ts`.
- **Backend Code**: `backend/src/TechDaily.Api/Endpoints/LibraryEndpoints.cs`, `backend/tests/TechDaily.Tests/Endpoints/LibraryEndpointsTests.cs`.
- **Breaking Changes**: Anonymous access to `GET /api/v1/library/books/*` is terminated; all API requests require a valid JWT Bearer token.

# Tasks: Auth-First Routing & Full-Screen Studio Auth Canvas

## 1. Backend - Api Security Lock-down

- [x] 1.1 In `backend/src/TechDaily.Api/Endpoints/LibraryEndpoints.cs`, apply `.RequireAuthorization()` across all library endpoints (`/books`, `/books/{id}`, `/books/{id}/status`, `/books/{id}/slices/{order}`, `/curate`), ensuring anonymous requests return `401 Unauthorized`
- [x] 1.2 In backend unit/integration tests, verify that anonymous requests to `GET /api/v1/library/books` and `GET /api/v1/library/books/{id}` are rejected with `401 Unauthorized`

## 2. Frontend - Route Protection & Default-Deny Middleware

- [x] 2.1 In `frontend/middleware/auth.global.ts`, replace the route allowlist with a strict Default-Deny gate: all routes require authentication except guest auth paths (`/login`, `/register`, `/forgot-password`, `/reset-password`) and local development exemptions (`/playground/*`, `/showcase`)
- [x] 2.2 In `frontend/middleware/auth.global.ts`, ensure unauthenticated visitors accessing `/` or any protected route are redirected to `/login?redirect=...`, and authenticated users accessing `/login` are forwarded to their intended redirect destination or `/today`

## 3. Frontend - Shell Decoupling & Full-Screen Studio Auth Canvas

- [x] 3.1 In `frontend/app.vue`, suppress `AppHeader` on authentication routes (`v-if="!isReaderMode && !isAuthPage"`), providing `/login` with an uncluttered, full-screen canvas
- [x] 3.2 In `frontend/pages/login.vue`, implement a top ambient utility bar featuring the TechDaily emblem/title and self-contained `LocaleSelector.vue` and `ThemeToggle.vue` controls
- [x] 3.3 In `frontend/pages/login.vue`, implement the full-screen Studio Auth Archetype with a multi-mode state machine supporting `login`, `register`, and `forgot-password` with zero vertical jumping
- [x] 3.4 In `frontend/i18n/locales/en.json` and `vi.json`, add localized keys for the forgot-password flow, mode switcher, and full-screen auth canvas

## 4. Testing & Verification Following Pillar 3

- [x] 4.1 In `frontend/tests/middleware/auth.spec.ts`, update unit tests to verify default-deny routing and unauthenticated redirection to `/login?redirect=...`
- [x] 4.2 In `frontend/tests/pages/login.spec.ts`, update unit tests to assert data contracts (form payload submission for login, register, and forgot password) and validation constraints without asserting CSS utility classes
- [x] 4.3 Execute full test suites (`dotnet test` for backend, `npm test` for frontend) and run production build (`npm run build`)
- [x] 4.4 Capture browser preview screenshots of the Full-Screen Studio Auth Canvas at 1920x1080 (Desktop) and 390x844 (Mobile) in Dark and Light modes for visual inspection

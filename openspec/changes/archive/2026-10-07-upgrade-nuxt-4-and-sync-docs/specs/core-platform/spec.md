# core-platform Specification Delta

## MODIFIED Requirements

### Requirement: Nuxt 4 Frontend Framework Runtime & Build Pipeline
The web frontend SHALL operate on the official Nuxt 4 framework runtime (`nuxt: ^4.6.0`) with Vue 3.5, Vite builder, and SSR/PWA capabilities. The build pipeline SHALL generate valid TypeScript runtime shims (`npx nuxi prepare`) and preserve existing route rules, including strict cross-origin isolation headers on `/read/**` routes (`Cross-Origin-Opener-Policy: same-origin`, `Cross-Origin-Embedder-Policy: credentialless`) required for on-device multi-threaded WASM neural narration.

#### Scenario: Clean Nuxt 4 application preparation and compilation
- **WHEN** the frontend preparation script (`npx nuxi prepare` or `npm run build`) is executed
- **THEN** the project builds cleanly without unresolvable package peer dependency warnings or runtime compilation errors under Nuxt 4.

#### Scenario: Route cross-origin isolation maintained under Nuxt 4
- **WHEN** a client requests a reader document route (`/read/**`)
- **THEN** the response delivers `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` without header collisions or regressions.

#### Scenario: Full suite regression integrity across frontend unit tests
- **WHEN** the automated Vitest test suite (`npm --prefix frontend test`) is executed
- **THEN** 100% of the 664 unit and component test cases pass successfully.

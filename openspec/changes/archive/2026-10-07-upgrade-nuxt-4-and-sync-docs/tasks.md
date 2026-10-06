# Tasks

## 1. Frontend Runtime Upgrade (Nuxt 4.6.0)

- [x] 1.1 Upgrade Nuxt dependency in `frontend/package.json` to `nuxt@^4.6.0` and audit satellite packages (`@nuxtjs/i18n`, `@pinia/nuxt`, `@nuxtjs/color-mode`)
- [x] 1.2 Run `npm install` and `npx nuxi prepare` in `frontend/` to generate updated Nuxt 4 TypeScript runtime declarations and shims
- [x] 1.3 Verify `frontend/nuxt.config.ts` compatibility settings and build pipeline under Nuxt 4

## 2. Frontend Test Suite & Route Rules Verification

- [x] 2.1 Execute `npm --prefix frontend test` and verify that all 664 unit, component, store, and composable tests pass with zero failures
- [x] 2.2 Verify that cross-origin isolation headers on `/read/**` routes (`Cross-Origin-Opener-Policy`, `Cross-Origin-Embedder-Policy`) remain active for on-device WASM TTS narration
- [x] 2.3 Run `npm --prefix frontend build` to verify production SSR/PWA bundle compilation succeeds cleanly

## 3. TechDaily Repository Documentation Synchronization

- [x] 3.1 Update `README.md` test metrics to reflect the accurate 1,015 total test count (351 backend .NET tests + 664 frontend Vitest tests)
- [x] 3.2 Add dedicated feature section for On-Device & Multi-Engine Audio Narration (`/read/[bookId]`, `/today`) detailing Web Worker ONNX TTS, IndexedDB caching, and cascading fallbacks
- [x] 3.3 Add dedicated feature section for Interactive 2D/3D Associative Knowledge Graph (`/graph`) detailing relational extraction and Three.js/Cytoscape visualizers
- [x] 3.4 Update Daily Focus Hub documentation with recent calendar auto-advance and streak decay mechanics

## 4. Portfolio Synchronization (duycld03.github.io)

- [x] 4.1 Update `Duycld03.github.io/src/content/projects/techdaily.md` with `demoUrl: "https://techdaily.duckdns.org"`
- [x] 4.2 Verify `Nuxt 4` framework tag and refine project description to accurately describe Gemini structured synthesis and on-device neural TTS narration
- [x] 4.3 Verify `Duycld03.github.io/src/data/profile.ts` experience entry aligns with Nuxt 4 and technical capabilities
- [x] 4.4 Run `npm run build` in `Duycld03.github.io` to ensure clean static site generation

## 5. Career Vault & Interview Prep Synchronization (career-vault)

- [x] 5.1 Update `career-vault/profile.json` with Nuxt 4 and live production domain (`techdaily.duckdns.org`)
- [x] 5.2 Update `career-vault/template.html` with Nuxt 4 tech stack and live link while preserving the single-page layout
- [x] 5.3 Render `Nguyen-Truong-Duy-CV.pdf` and verify strict compliance with the single-page A4 invariant (`Total pages == 1`)
- [x] 5.4 Expand `career-vault/interview-prep.md` with technical defense scenarios for SM-2 spaced repetition, on-device neural TTS Web Workers, and PostgreSQL graph relational projections

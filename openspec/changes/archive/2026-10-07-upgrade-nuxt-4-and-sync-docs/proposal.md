# Proposal

## Why
TechDaily's web frontend is currently configured with Nuxt 4 forward compatibility flags (`future.compatibilityVersion = 4`), but its core dependency remains pinned to `nuxt@^3.15.4` (resolving to 3.21.11). Furthermore, project documentation, live demo links, test counts (1,015 automated tests vs. 811 recorded), and portfolio/CV showcases across `duycld03.github.io` and `career-vault` are desynchronized with the actual codebase features (such as on-device neural TTS narration and interactive 3D knowledge graphs). Upgrading officially to `nuxt@^4.6.0` and synchronizing all documentation establishes full framework modernity, architectural accuracy, and career showcase integrity.

## What Changes
- **Frontend Core Upgrade**: Upgrade `frontend/package.json` from `nuxt@^3.15.4` to `nuxt@^4.6.0`, verify satellite module compatibility (`@nuxtjs/i18n`, `@pinia/nuxt`, `@nuxtjs/color-mode`), run `nuxi prepare`, and ensure all 664 frontend Vitest tests pass cleanly.
- **Repository Documentation Synchronization**: Update `README.md` to reflect the current test counts (351 backend + 664 frontend = 1,015 tests), document the On-Device Neural Audio Narration (TTS) engine (`/read/[bookId]`, `/today`), document the Interactive 2D/3D Associative Knowledge Graph (`/graph`), and record recent Daily Focus enhancements.
- **Portfolio Synchronization (`duycld03.github.io`)**: Add `demoUrl: "https://techdaily.duckdns.org"` to `src/content/projects/techdaily.md`, validate the `Nuxt 4` framework tag, clarify AI scenario synthesis and on-device neural TTS narration in the description, and verify `src/data/profile.ts`.
- **Career Vault Alignment (`career-vault`)**: Align `profile.json` and `template.html` with Nuxt 4 and the live production URL (`techdaily.duckdns.org`), re-render the single-page A4 PDF adhering strictly to `cv-rules.md`, and expand `interview-prep.md` with technical defense questions for SM-2 Spaced Repetition, On-Device Neural Audio Web Workers, and PostgreSQL relational knowledge graph extraction.

## Capabilities

### Modified Capabilities
- `core-platform`: Updates the web frontend runtime standard from Nuxt 3 with compatibility flags to official Nuxt 4.x runtime (`nuxt: ^4.6.0`), preserving cross-origin isolation, SSR/PWA capabilities, and full module integration.

## Impact
- **Codebase Dependencies**: `frontend/package.json`, `frontend/package-lock.json`, `frontend/nuxt.config.ts`.
- **Documentation**: `README.md`, `openspec/specs/core-platform/spec.md`.
- **External Portfolios & Resumes**: `/home/duycld03/workspace/Duycld03.github.io`, `/home/duycld03/workspace/career-vault`.
- **Testing**: Zero regression across all 664 frontend Vitest tests and 351 backend .NET tests.

## Context

On 2026-09-18, critical user-facing graph and roadmap capabilities were merged and deployed to production. Verifying these visual surfaces manually on production across Desktop, Tablet, and Mobile devices in both English and Vietnamese is labor-intensive and difficult to repeat consistently.

The frontend repository already contains Playwright (`^1.62.1`) and uses a lightweight runner pattern in `frontend/e2e/test-navigation-locales.mjs`. Extending this architecture with a dedicated responsive smoke runner gives developers and operators immediate, automated verification of today's visual features with optional high-resolution screenshot exports.

## Goals / Non-Goals

**Goals:**
- Provide an automated Playwright E2E smoke runner (`frontend/e2e/test-graph-roadmap-responsive.mjs`) testing `/graph` and `/roadmap`.
- Verify 3 canonical viewports: Desktop ($1920\times 1080$), Tablet ($768\times 1024$), and Mobile ($375\times 812$).
- Verify both English (`en`) and Vietnamese (`vi`) locales.
- Verify 2D and 3D mode switching, Auto-Rotate HUD toggle, Show-All-Labels LOD toggle, `GraphLegend` presence, and non-clipping `flex-wrap` category pills.
- Support testing both local development (`http://localhost:3000`) and live production (`TARGET_URL=https://...`).
- Provide optional visual screenshot capture with `--screenshot` saving PNG files to `frontend/e2e/screenshots/`.

**Non-Goals:**
- Replacing unit tests (Vitest with 316 unit tests remains the primary correctness gate).
- Implementing full pixel-by-pixel diffing for WebGL (WebGL render buffers vary by GPU and drivers, causing false positive diff failures).

## Decisions

### 1. Standalone Playwright Script Pattern
- **Decision**: Implement `frontend/e2e/test-graph-roadmap-responsive.mjs` matching the established pattern in `frontend/e2e/test-navigation-locales.mjs`.
- **Rationale**: Requires no extra test frameworks, runs instantly with `node`, and leverages the installed `playwright` dependency.

### 2. Configurable Target Environment
- **Decision**: Read `process.env.TARGET_URL || 'http://localhost:3000'`.
- **Rationale**: Allows running `TARGET_URL=https://production-domain.com npm run test:e2e:graph` against the live production deployment or locally.

### 3. Optional Visual Screenshot Generation
- **Decision**: When `--screenshot` is passed as a CLI argument, the script creates `frontend/e2e/screenshots/` and captures PNG screenshots for each viewport and language combination:
  - `graph-desktop-2d-vi.png`, `graph-desktop-3d-vi.png`
  - `graph-tablet-3d-en.png`, `graph-mobile-3d-vi.png`
  - `roadmap-desktop-en.png`, `roadmap-mobile-vi.png`
- **Rationale**: Answers the user's question: taking screenshots is completely optional and automated. Developers don't need to manually take screenshots, but can generate an entire album in 15 seconds when visual auditing is desired.

## Risks / Trade-offs

- **[Risk] WebGL canvas blank in headless browser**:
  → **Mitigation**: Playwright Chromium supports `--enable-webgl` and `--use-gl=angle` / SwiftShader out of the box in modern versions, allowing 3D canvas testing in headless mode.

## Migration Plan

1. Create `frontend/e2e/test-graph-roadmap-responsive.mjs`.
2. Add `"test:e2e:graph": "node e2e/test-graph-roadmap-responsive.mjs"` to `frontend/package.json`.
3. Add `.gitignore` rule for `frontend/e2e/screenshots/` to avoid committing large binary image files.

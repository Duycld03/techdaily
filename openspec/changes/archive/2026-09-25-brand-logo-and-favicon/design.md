# Design: Brand Logo & Favicon Redesign with Stitch Developer Emblem

## Context
See `proposal.md` for motivation. The canonical SVG vector markup is established in `local://paste-4.md`, providing a 512x512 resolution-independent asset with balanced geometry:
- Squircle background container with obsidian gradient (`#151419` to `#08080a`) and subtle violet perimeter stroke.
- Symmetrical regular hexagon outer ring with vertical violet gradient (`#c4b5fd` -> `#8b5cf6` -> `#7c3aed` -> `#5b21b6`).
- Inner architectural hairline hexagon (`rgba(196, 181, 253, 0.25)`).
- High-contrast pure white symmetrical code brackets (`< >`) with rounded line joins.
- Symmetrical core memory nucleus with radiant violet glow halo.
- Axial vertical balance dots (`#a78bfa`).

## Goals / Non-Goals

**Goals:**
- Provide a dedicated, reusable Vue component `frontend/components/common/AppLogo.vue` supporting flexible sizing (`size="sm" | "md" | "lg" | number`) and dark/light color mode adaptation.
- Replace `frontend/public/favicon.svg` with the full-fidelity Stitch SVG asset, ensuring immediate recognition in browser tabs.
- Update global navigation surfaces (`AppHeader.vue` desktop topbar and mobile navigation drawer) to render `AppLogo.vue`.
- Update the Studio Auth Cockpit (`login.vue`) brand header to render `AppLogo.vue`.
- Maintain strict architectural invariants: clicking the brand emblem navigates to `/` (Home Bento Dashboard), and document metaphors (`BookOpen` in Library/Reader) remain unchanged.

**Non-Goals:**
- Replacing contextual content icons (e.g. `BookOpen` used in Library items, reading progress bars, or chapter outlines continues to represent technical documentation).
- Changing backend API schemas or database models.

## Decisions

### 1. Dedicated Reusable `AppLogo.vue` Component vs Inline SVG Duplication
- **Problem**: Embedding inline SVG markup across `AppHeader.vue` and `login.vue` leads to duplicated gradient IDs (`#symmHexGrad`, `#bgGrad`) which cause rendering conflicts when multiple instances appear in the DOM.
- **Decision**: Create `frontend/components/common/AppLogo.vue`:
  - Accepts a `size` prop (`'sm'` = 28px, `'md'` = 32px, `'lg'` = 40px, or custom numeric pixel dimensions).
  - Encapsulates clean SVG attributes with unique scoped gradient IDs (or standard deterministic IDs).
  - Includes accessible `aria-label="TechDaily"` and `role="img"`.

### 2. Standalone Scalable Vector Favicon (`frontend/public/favicon.svg`)
- **Decision**: Replace `frontend/public/favicon.svg` with the raw vector markup from `local://paste-4.md`.
- **Rationale**: Modern web browsers (Chrome, Firefox, Safari 16+, Edge) natively support SVG favicons via `<link rel="icon" type="image/svg+xml" href="/favicon.svg">`. The Stitch design's high-contrast white brackets and violet hexagon pop distinctly against both dark browser themes and light browser chrome.

### 3. Header & Navigation Integration
- **`AppHeader.vue`**:
  - Replace the old `w-7 h-7 sm:w-8 sm:h-8 rounded-lg bg-gradient-to-tr ... <BookOpen>` with `<AppLogo size="md" />` in both the desktop topbar and mobile drawer header.
  - Preserve the text brand title "TechDaily" with high-contrast gradient text alongside the emblem.
- **`login.vue`**:
  - Replace the nested `BookOpen` container in the topbar with `<AppLogo size="md" />`.

## Risks / Trade-offs

- **Small Viewport Scalability (16x16 Favicon)**:
  - *Risk*: At 16x16 pixels in dense tab strips, micro hairline details can blur.
  - *Mitigation*: The thick white brackets (stroke width 22) and prominent hexagon ring (stroke width 18) provide strong macro silhouette contrast that remains legible even at sub-32px favicon dimensions.

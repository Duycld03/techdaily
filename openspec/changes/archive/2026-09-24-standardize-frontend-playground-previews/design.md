# Design: Standardize Frontend Playground Previews

## Context

Prototyping prospective UI layouts using standalone `.html` files creates severe design system divergence. The external HTML files rely on third-party CDNs (Tailwind CDN, Google Fonts) and manual inline styling. When migrating preview designs into real Nuxt SFCs, subtle discrepancies emerge:
1. **Typography & Font Mismatches**: Local Nuxt configurations may lack fonts loaded via external HTML CDN (e.g. `JetBrains Mono` causing broken Vietnamese diacritics in code blocks).
2. **Component Nesting Discrepancies**: Hand-crafted HTML containers clash with Vue layout archetype primitives (e.g. adding an extra outer card around a `BoardLayout`).
3. **Double Work & Parity Regressions**: Once a static HTML file is approved, the entire page has to be re-implemented in Vue from scratch.

Nuxt 4 / Nuxt 3 provides a built-in `pages:extend` hook in `nuxt.config.ts` that automatically excludes development routes matching `/playground` and `/showcase` during production builds.

## Goals / Non-Goals

**Goals:**
- Mandate that all prospective UI previews be built directly as Vue Single File Components under `frontend/pages/playground/*.vue`.
- Codify Rule 19 in `AGENTS.md` (Section 3 Anti-Patterns & Invariants) prohibiting disconnected `.html` preview files.
- Enable instant developer review via `http://localhost:3000/playground/<feature>` with Vite Hot Module Replacement (HMR).
- Guarantee zero production bundle contamination through Nuxt's `pages:extend` strip hook.

**Non-Goals:**
- Creating a persistent database or backend API integration for playground pages; mock data remains hardcoded in component setup scripts.
- Replacing automated Vitest tests with playground previews; playground is strictly for human visual inspection.

## Decisions

### 1. Dedicated Dev Playground Directory Structure

- All prospective previews are placed in `frontend/pages/playground/<feature-name>.vue` (e.g. `frontend/pages/playground/phase-2-practice.vue`).
- Playground pages consume real application components (`BoardLayout`, `StudioLayout`, `ShikiCodeBlock`, `OptionCard`) and project CSS tokens.
- Mock state is localized using Vue `ref()` / `reactive()` fixtures, allowing rich interactions (e.g. choice selection, tab switching, card flipping) without server dependencies.

### 2. Route Protection & Auth Exemption

In `frontend/middleware/auth.global.ts`, paths starting with `/playground` and `/showcase` are explicitly excluded from `isAuthRequired`. Developers and reviewers can navigate directly to preview URLs without login barriers.

### 3. Production Build Pruning

In `frontend/nuxt.config.ts`, the `pages:extend` hook is verified to ensure all `/playground` and `/showcase` routes are excised from production bundles when `process.env.NODE_ENV === 'production'`:

```ts
hooks: {
  'pages:extend'(pages) {
    if (process.env.NODE_ENV === 'production') {
      const devPrefixes = ['/showcase', '/playground']
      for (let i = pages.length - 1; i >= 0; i--) {
        const path = pages[i].path || ''
        if (devPrefixes.some(prefix => path === prefix || path.startsWith(`${prefix}/`))) {
          pages.splice(i, 1)
        }
      }
    }
  }
}
```

### 4. Authoritative Rule Addition in AGENTS.md

Add Rule 19 under Section 3 (*Strict Rules & Anti-Patterns to NEVER Repeat*):
- **Rule 19: Never Use Disconnected HTML Files for UI Previews.** All prospective feature previews, layout prototypes, and design reviews MUST be constructed as Vue Single File Components in `frontend/pages/playground/*.vue` sharing the canonical Tailwind, Vite, and component system.

## Risks / Trade-offs

- **Risk: Unused playground files cluttering the repository**:
  - *Mitigation*: Playground files are explicitly temporary; once a phase is approved and applied to its destination page (e.g. `pages/quiz.vue`), the corresponding playground file is cleaned up or moved to archive.

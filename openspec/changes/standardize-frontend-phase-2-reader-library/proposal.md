# Proposal: Standardize Frontend Phase 2 — Reader Studio & Document Library

## Why

The Reader Studio (`pages/read/[bookId].vue`), Document Library (`pages/library.vue`), and Study Notes (`pages/notes.vue`) represent the high-density reading and documentation curation surface of TechDaily. Under `antfu/skills` guidelines, these surfaces require standardization to:
1. Guarantee comfortable long-form reading on mobile viewports without horizontal clipping, font layout shifts, or sticky header/footer obscuration.
2. Refactor typography management (`useReaderTypography.ts`) to eliminate manual `window.addEventListener('storage')` in favor of VueUse `useEventListener`.
3. Standardize the document ingestion modals and PDF upload flows on mobile screens ($\le 375\text{px}$).
4. Ensure Markdown rendering (via markdown-it and Shiki) handles code blocks, callouts, and inline tags with responsive scrolling containers.

## What Changes

- **Immersive Reader (`pages/read/[bookId].vue`)**:
  - Constrain layout to dynamic viewport height (`h-dvh`) with `pb-[max(1rem,env(safe-area-inset-bottom))]` for bottom pagination controls.
  - Standardize segmented typography settings popover (`grid-cols-3` for font family, line height, reading width) to ensure zero clipping on 320px screens.
  - Verify external doc links open in new tab (`target="_blank" rel="noopener noreferrer"`) while internal slice anchors remain in-page.
- **Composable Event Hygiene (`useReaderTypography.ts`)**:
  - Replace raw `window.addEventListener('storage', ...)` with VueUse `useEventListener` to prevent detached listener leaks across route transitions.
- **Document Library (`pages/library.vue`)**:
  - Audit document cards grid (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6`) for mobile responsiveness.
  - Standardize Web Crawler and PDF Ingestion modals: ensure full-width inputs, touch-friendly file drop zone, and mobile-responsive action buttons.
- **Study Notes Vault (`pages/notes.vue`)**:
  - Ensure filter chips (tag pills) use horizontal scrolling with `whitespace-nowrap shrink-0` on mobile.
  - Standardize flashcard creation shortcut trigger with clean toast notifications.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `reader`: Standardize dynamic viewport height, typography popovers, and mobile pagination footer invariants.
- `library`: Refine mobile document ingestion modals and responsive card grid specifications.
- `notes`: Audit note card tag pills and mobile touch targets.

## Impact

- **Affected Files**: `frontend/pages/read/[bookId].vue`, `frontend/pages/library.vue`, `frontend/pages/notes.vue`, `frontend/composables/useReaderTypography.ts`.
- **Testing**: Unit tests in `frontend/tests/pages/read.spec.ts`, `frontend/tests/pages/library.spec.ts`, and Playwright reader tests.

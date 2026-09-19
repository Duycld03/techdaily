# Design: Standardize UI Lifecycle and Event Hygiene (Batch 3)

## Context

TechDaily's UI performance and event standardization efforts (Batches 1 and 2) modernized key components with `@vueuse/nuxt` composables. Five remaining areas in the frontend codebase still retain manual `addEventListener` / `removeEventListener` boilerplate:
- `components/common/AppSelect.vue` manages scroll and resize repositioning manually.
- `components/graph/GraphCanvas.vue` binds a manual window resize listener.
- `components/today/DocReaderPane.vue` binds manual document click and keydown listeners for typography popovers.
- `pages/insights.vue` and `pages/review.vue` bind manual window keydown navigation shortcuts.

Standardizing these components against `antfu/skills` and `@vueuse/nuxt` declarative patterns guarantees leak-free lifecycle teardowns during client routing and SSR hydration.

## Goals / Non-Goals

**Goals:**
- Replace all manual window/document `addEventListener` and `removeEventListener` calls in the 5 identified files with VueUse `useEventListener` and `onClickOutside`.
- Eliminate manual event listener teardown boilerplate in `onUnmounted` / `onBeforeUnmount` where VueUse handles lifecycle detachment automatically.
- Preserve 100% existing functionality, keyboard shortcuts, and positioning accuracy across all components.
- Maintain a 100% pass rate across the Vitest frontend suite (55 test files / 378 tests).

**Non-Goals:**
- Redesign or modify any component visual layout, color palette, or DOM structure.
- Alter business logic or API data fetching across stores (`useInsightsStore`, `useReviewStore`, `useKnowledgeGraphStore`).

## Decisions

### 1. Declarative Scroll & Resize in `AppSelect.vue`
- **Decision**: In `AppSelect.vue`, bind `useEventListener(typeof window !== 'undefined' ? window : null, 'scroll', (e) => { if (!isOpen.value) return; handleScroll(e); }, { capture: true, passive: true })` and `useEventListener(typeof window !== 'undefined' ? window : null, 'resize', () => { if (!isOpen.value) return; updateFloatingPosition(); }, { passive: true })`.
- **Rationale**: Currently, `openDropdown` and `closeDropdown` manually invoke `addEventListener` and `removeEventListener`. Declarative listeners eliminate manual cleanup branches and prevent dangling listeners if component unmounts while open.

### 2. Canvas Resize Teardown in `GraphCanvas.vue`
- **Decision**: Replace `window.addEventListener('resize', handleResize)` with `useEventListener(typeof window !== 'undefined' ? window : null, 'resize', handleResize)`.
- **Rationale**: Removes manual `removeEventListener` from `onBeforeUnmount`, keeping only Cytoscape canvas destruction (`cy.destroy()`) in the unmount hook.

### 3. Declarative Popover & Document Listeners in `DocReaderPane.vue`
- **Decision**:
  - Use `onClickOutside(typographyDropdownRef, () => { if (isTypographyOpen.value) isTypographyOpen.value = false; })` for closing the typography modal.
  - Bind Escape key dismissal via `useEventListener(typeof window !== 'undefined' ? window : null, 'keydown', handleKeyDown)`.
  - Bind document click for selection menu via `useEventListener(typeof document !== 'undefined' ? document : null, 'click', handleDocumentClick)`.
- **Rationale**: Eliminates manual `document.addEventListener` / `removeEventListener` in `onMounted` / `onUnmounted`.

### 4. Route-Level Keydown Shortcuts in `insights.vue` and `review.vue`
- **Decision**: Replace `window.addEventListener('keydown', handleKeyDown)` with `useEventListener(typeof window !== 'undefined' ? window : null, 'keydown', handleKeyDown)` in both pages, removing `onUnmounted` boilerplate.
- **Rationale**: VueUse automatically unregisters the listener when the page component is destroyed upon route navigation.

## Risks / Trade-offs

- **Scroll Event Performance in `AppSelect.vue`**:
  - *Risk*: Running scroll checks on every window scroll could impact FPS if unthrottled.
  - *Mitigation*: The listener guards with `if (!isOpen.value) return;` at the very top and specifies `{ passive: true }`, ensuring zero main-thread blocking when closed.
- **Test Runner Mocking (`happy-dom`)**:
  - *Risk*: Happy-DOM synthetic window event dispatching in Vitest unit tests.
  - *Mitigation*: VueUse `useEventListener` natively resolves `window` / `document` targets in Happy-DOM and handles mocked events identically to standard `addEventListener`.

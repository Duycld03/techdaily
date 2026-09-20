# Tasks

## 1. Header Z-Index Elevation

- [ ] 1.1 Update `AppHeader.vue` sticky header from `z-40` to `z-50`
- [ ] 1.2 Update `AppHeader.vue` mobile navigation drawer backdrop from current z-index to `z-60`
- [ ] 1.3 Verify `AppCommandPalette.vue` remains at `z-60` or higher (currently `z-60`)
- [ ] 1.4 Verify `AppToastContainer.vue` remains at `z-[9999]`

## 2. Page-Level Z-Index Remediation

- [ ] 2.1 Audit and cap `today.vue` studio control bar and book switcher popover to `z-30` / `z-35`
- [ ] 2.2 Audit and cap `roadmap.vue` track selector sticky header and popovers to `z-30` / `z-35`
- [ ] 2.3 Audit and cap `graph.vue` loading/error overlays and `GraphDetailDrawer.vue` to `z-35` / `z-40`
- [ ] 2.4 Audit and cap `library.vue` search toolbar and filter popovers to `z-30` / `z-35`
- [ ] 2.5 Audit and cap `notes.vue`, `review.vue`, `insights.vue` sticky bars and modal backdrops
- [ ] 2.6 Audit `read/[bookId].vue` floating selection toolbar and mobile TOC drawer to `z-40` max (below header when header is visible)

## 3. Responsive Header Layout Fix

- [ ] 3.1 Tighten header action button spacing on narrow mobile (`gap-1` below `sm`, `gap-2` at `sm+`)
- [ ] 3.2 Add `min-w-0` and `truncate` to the centered ⌘K search bar container for elastic scaling
- [ ] 3.3 Hide keyboard shortcut `<kbd>` indicator on viewports below `md` (`hidden md:inline-flex`)
- [ ] 3.4 Ensure mobile search trigger button has guaranteed touch target (≥ 40px × 40px)

## 4. Verification

- [ ] 4.1 Visual verification on 320px, 375px, 640px, 768px, and 1024px viewports across today, roadmap, graph, library pages
- [ ] 4.2 Verify header search trigger is clickable and opens command palette on all tested viewports
- [ ] 4.3 Run frontend Vitest suite (`npm test`) to confirm no regressions

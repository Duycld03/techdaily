# Tasks: Library Card Status Badge Bottom Alignment

## 1. Template Structure Refactoring

- [X] 1.1 In `frontend/pages/library.vue`, update the book card upper content wrapper from `<div>` to `<div class="flex flex-col flex-1">` so it fills available vertical space inside the card.
- [X] 1.2 In `frontend/pages/library.vue`, wrap the bookmark badge (`bookmarks[book.id]`), ready badge (`book.status === 'Ready'`), and background ingestion status indicator (`book.status === 'Processing'`) inside a bottom-anchored container `<div class="mt-auto pt-3">`.
- [X] 1.3 In `frontend/pages/library.vue`, remove redundant top margin utility classes (`mt-3`, `mt-3.5`) on the child badge elements so that the parent container's `pt-3` uniformly dictates clearance from the title and subtitle.
- [X] 1.4 In `frontend/pages/library.vue`, verify that all conditional bindings (`v-if`, `v-else-if`), icon components (`Bookmark`, `CheckCircle2`, `Loader2`), and translation interpolations remain intact.

## 2. Unit & Integration Test Updates

- [X] 2.1 In `frontend/tests/pages/library.spec.ts`, add test cases asserting that each rendered book card's upper content container contains the CSS classes `flex`, `flex-col`, and `flex-1`.
- [X] 2.2 In `frontend/tests/pages/library.spec.ts`, add test cases asserting that the status badge wrapper div contains the CSS classes `mt-auto` and `pt-3`.
- [X] 2.3 In `frontend/tests/pages/library.spec.ts`, create a multi-card test scenario with a 1-line title card and a 2-line title card, asserting consistent bottom-aligned container structure across both cards.
- [X] 2.4 In `frontend/tests/pages/library.spec.ts`, assert that cards without `authorOrSourceUrl` maintain the `mt-auto pt-3` badge wrapper structure without layout shift.

## 3. Build & Visual Verification

- [X] 3.1 Execute `npm --prefix frontend test` to ensure all frontend unit and integration tests pass without failures or regressions.
- [X] 3.2 Verify visual alignment across grid layout breakpoints (mobile 1-column, tablet 2-column, desktop 3-column) in both light and dark color modes.
- [X] 3.3 Run `openspec validate --strict library-card-badge-bottom-alignment` to verify complete OpenSpec standard compliance.

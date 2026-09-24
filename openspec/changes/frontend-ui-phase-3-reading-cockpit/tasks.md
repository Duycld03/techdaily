# Tasks: Frontend UI Phase 3 - Reading Cockpit & Document Studio

## 1. Modular Reader Components Extraction

- [x] 1.1 Create `frontend/components/reader/ReaderHeaderBar.vue` extracting library return link, title/chapter badges, slice progress, novel-style `Aa` typography popover, theme toggle, and quiz launcher
- [x] 1.2 Create `frontend/components/reader/ReaderTocSidebar.vue` extracting desktop collapsible TOC sidebar and mobile touch-dismiss drawer with chapter search filter and completion indicators
- [x] 1.3 Create `frontend/components/reader/ReaderNavigationCards.vue` extracting symmetric bottom slice navigation cards with English/Vietnamese anti-clipping safeguards

## 2. Reader Studio Orchestrator Refactor & Centered Prose Standard

- [x] 2.1 Refactor `frontend/pages/read/[bookId].vue` to integrate `ReaderHeaderBar`, `ReaderTocSidebar`, and `ReaderNavigationCards`, removing duplicated template markup
- [x] 2.2 Standardize `#main` reading container layout in `read/[bookId].vue` with centered prose constraints (`max-w-3xl`, `max-w-4xl`, `max-w-full`) and symmetric gutter margins
- [x] 2.3 Ensure text selection floating toolbar, note popover, and term explainer modal integrate smoothly with the refactored reader DOM structure

## 3. Daily Reading Cockpit Layout Density

- [x] 3.1 Standardize grid gap spacing and container padding across Left Rail, Center Canvas (`DocReaderPane.vue`), and Right Dock (`InterviewChallengePane.vue`) in `frontend/pages/today.vue`
- [x] 3.2 Harmonize mobile tab switcher button sizing (>= 44px) and badge padding in `frontend/pages/today.vue` to prevent layout shift between viewports

## 4. Testing & Verification

- [x] 4.1 Write Vitest unit tests for `ReaderHeaderBar.vue`, `ReaderTocSidebar.vue`, and `ReaderNavigationCards.vue` in `frontend/tests/components/reader/`
- [x] 4.2 Verify full frontend test suite passes (`npm test`) and type check passes (`npx vue-tsc --noEmit`)

# Proposal

## Why

The page headers for `library.vue` ("Thư Viện Tài Liệu") and `notes.vue` ("Trích Dẫn & Ghi Chú") deviate from each other and from the platform design system standard (`quiz.vue`, `insights.vue`):
1. **Size & Typography Inconsistencies**: Icon badge containers vary across three conflicting scales (`w-8 h-8` in Notes, `w-9 h-9` in Library, vs `w-10 h-10 rounded-2xl` in Quiz), icon glyphs are undersized (`w-4` / `w-5` vs `w-5 sm:w-6`), and titles are disproportionately small (`text-sm` in Notes, `text-base` in Library, vs `text-xl sm:text-2xl font-black tracking-tight`).
2. **Structural Layout Disparity**: Notes leaves the entire right side of its top header blank while crowding both the search input and tag chips into a cramped second row. Library places search on Row 1 and categories on Row 2, but uses non-standard container and heading dimensions.

## What Changes

- **Unified 2-Tier Layout Pattern Across Both Pages**:
  - **Row 1 (Header Top Bar)**:
    - **Left**: Standard Icon badge (`w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20`), standard `<h1>` title (`text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white`), and subtitle (`text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium`).
    - **Right**: Search input (`h-10 rounded-xl`) with shortcut hint (`⌘K`) plus primary action controls (e.g., "+ Thêm Tài Liệu" in Library). In `notes.vue`, search moves to Row 1 right, resolving the empty void.
  - **Row 2 (Full-Width Filter Chips Bar)**:
    - Dedicated full container width for horizontal filter chips (Categories in `library.vue`, Tags `#tag` in `notes.vue`) with smooth horizontal scrolling, preventing layout collisions between search inputs and tag pills.
- **Standardized Component Dimensions**:
  - Icon glyphs: `w-5 h-5 sm:w-6 sm:h-6` with `:stroke-width="1.5"` (`<BookOpen>` in Library, `<Highlighter>` in Notes).
  - Search input: `rounded-xl h-10` with hairline border `border-slate-200/80 dark:border-white/[0.08]`.
- **Layout Integration Consistency**:
  - Maintain `notes.vue` flat `BoardLayout` integration while correctly populating `#header` right with the search bar and `#filters` with tag chips.
  - Maintain `library.vue` direct-canvas 2-tier structure with standardized typography and icon geometry.

## Capabilities

### Modified Capabilities
- `notes`: Update `Requirement: Technical Notes Board Layout Integration` to enforce the 2-tier layout standard (Row 1: title + search; Row 2: full-width tag filters) and standardized `w-10 h-10 rounded-2xl` header typography.
- `library`: Update `Requirement: Dev-Learning Studio Visual Language Integration for Library & Ingestion` to standardize top header typography, icon container dimensions (`w-10 h-10 rounded-2xl`), and 2-tier catalog header geometry.
## Impact

- **Frontend Code**: `frontend/pages/library.vue`, `frontend/pages/notes.vue`.
- **Frontend Tests**: `frontend/tests/pages/library.spec.ts`, `frontend/tests/pages/notes.spec.ts`.
- **Backend / Database**: Zero impact. No endpoints, DTOs, or database schemas are altered.

# Design

## Context

TechDaily's frontend runs on Nuxt 4, Vue 3.5, and Tailwind CSS. During initial exploration, a comprehensive design system was prototyped and verified via v0.dev on branch `v0/design-system-showcase` (commits `91327e0` and `4c69f93`).

The existing codebase suffers from visual bloat due to mobile-first spacing tokens (`p-8`, `p-10`, `space-y-6`, option buttons ~85px tall). On standard Windows 11 desktops with 125% DPI display scaling (available height ~700px–850px), interactive options and modal action buttons are frequently pushed below the fold.

## Goals / Non-Goals

**Goals:**
- Extract and standardize reusable design primitives from `v0/design-system-showcase` into `frontend/components/ui/`:
  - `OptionCard.vue`
  - `AppModal.vue`
  - `BentoStatCard.vue`
  - `CodeBlock.vue`
  - `SkeletonShimmer.vue`
  - `EmptyState.vue`
  - `FloatingSelectionToolbar.vue`
- Provide an interactive living styleguide at `/showcase` (`frontend/pages/showcase.vue`).
- Refactor `InterviewChallengePane.vue` and `quiz.vue` to adopt `OptionCard.vue`, ensuring all 4 multiple-choice options and submit buttons fit above the 1080p desktop fold.
- Refactor `library.vue` import dialog to adopt `AppModal.vue`, ensuring "Cancel" and "Save" buttons are permanently pinned in a sticky footer.
- Normalize outer page padding to `py-4 sm:py-5 px-4 sm:px-6` across `settings.vue`, `profile.vue`, and `insights.vue`.
- Cleanly import translations without introducing UTF-8 encoding corruption into `frontend/i18n/locales/vi.json`.
- Maintain 100% test pass rate across Vitest suites.

**Non-Goals:**
- Modifying backend C# API endpoints, database schema, or domain entities.
- Decreasing reading prose typography in `DocReaderPane.vue` (must preserve `text-base md:text-lg`).
- Redesigning the dark canvas color palette (`#09090b`, `#7c3aed`).

## Decisions

### 1. Component Placement & Architecture
- **Decision**: Place reusable design system primitives in `frontend/components/ui/` with clean TypeScript props and emits:
  - `OptionCard.vue`: Standardized choice button with `letter`, `text`, `state` (`default` | `selected` | `correct` | `incorrect`), and `disabled`.
  - `AppModal.vue`: 3-tier shell with `open`, `title`, `#default` (scrollable body), and `#footer` (sticky actions).
  - `CodeBlock.vue`: Code container with `code`, `filename`, `language`, and clipboard copy state machine.
  - `SkeletonShimmer.vue`: Preset skeleton placeholders (`card`, `quiz`, `metric`).
  - `EmptyState.vue`: Container with `icon`, `heading`, `description`, `cta`, and semantic `accent`.
  - `FloatingSelectionToolbar.vue`: Floating pill toolbar for text selection actions.
- **Showcase Integration**: Place showcase-specific demo wrappers in `frontend/components/showcase/` and showcase page at `frontend/pages/showcase.vue`.

### 2. Geometry & Spacing Compression
- **Decision**: Standardize OptionCard height to ~44px (`px-3 py-2.5 rounded-lg border text-sm`).
- **Rationale**: Reduces 4 options from ~360px vertical height down to ~200px. In `InterviewChallengePane.vue` (with 50px header, 100px question, 30px spacing), total dock content consumes ~430px, fitting comfortably within the ~780px usable split-view height with zero vertical scrolling.

### 3. Modal Dialog Architecture
- **Decision**: Build `AppModal.vue` using a vertical flex column capped at `max-h-[85vh]`:
  - Header: `shrink-0 px-4 py-3 border-b`
  - Body: `flex-1 overflow-y-auto max-h-[60vh] px-4 py-4 [scrollbar-gutter:stable]`
  - Footer: `shrink-0 px-4 py-3 border-t bg-slate-50/80 dark:bg-canvas-subtle/60`
- **Rationale**: Prevents submission controls from being scrolled off-screen when long forms or dropzones are displayed.

### 4. Selective Cherry-Pick to Prevent i18n Encoding Corruption
- **Decision**: Copy code files from branch `v0/design-system-showcase` directly via git archive/checkout of component paths, while hand-editing `vi.json` and `en.json` to insert `nav.showcase`.
- **Rationale**: Commit `91327e0` on the v0 branch corrupted multi-byte UTF-8 Vietnamese strings in `vi.json`. Selective component import eliminates this risk completely.

## Risks / Trade-offs

- **[Risk] Test Fixtures Broken by DOM Structure Changes**:
  - *Impact*: `InterviewChallengePane.spec.ts` or `quiz.spec.ts` asserting on legacy `p-4 sm:p-5 rounded-2xl` classes or button selectors.
  - *Mitigation*: Ensure `OptionCard` preserves `data-testid="quiz-option"` or relevant test selectors. Update test fixtures where class-name assertions were pinned to legacy bloated classes.
- **[Risk] Regression on Mobile Screen Touch Targets**:
  - *Impact*: Compact buttons may be harder to tap on small mobile screens.
  - *Mitigation*: OptionCard maintains `py-2.5` (~44px touch height), exceeding WCAG 2.1 AA 24px minimum and matching Apple Human Interface Guidelines 44px touch target recommendation.

# Tasks

## 1. Frontend: Design System Primitives & Components

- [x] 1.1 Extract and port `OptionCard.vue` from `origin/v0/design-system-showcase` into `frontend/components/ui/OptionCard.vue` supporting states (`default`, `selected`, `correct`, `incorrect`) and compact ~44px height.
- [x] 1.2 Extract and port `AppModal.vue` from `origin/v0/design-system-showcase` (based on `ProductionModal.vue`) into `frontend/components/ui/AppModal.vue` with 3-tier architecture (fixed header, scrollable body, sticky footer).
- [x] 1.3 Extract and port `BentoStatCard.vue` from `origin/v0/design-system-showcase` into `frontend/components/ui/BentoStatCard.vue` with tabular numerals and compact progress bar.
- [x] 1.4 Extract and port `CodeBlock.vue` from `origin/v0/design-system-showcase` into `frontend/components/ui/CodeBlock.vue` with file/language badge and copy state machine.
- [x] 1.5 Extract and port `SkeletonShimmer.vue` into `frontend/components/ui/SkeletonShimmer.vue` with `card`, `quiz`, and `metric` presets.
- [x] 1.6 Extract and port `EmptyState.vue` into `frontend/components/ui/EmptyState.vue` with icon ring and primary CTA.
- [x] 1.7 Extract and port `FloatingSelectionToolbar.vue` into `frontend/components/ui/FloatingSelectionToolbar.vue` with compact pill geometry.

## 2. Frontend: Cockpit Density Refactoring (Today Scenario Dock & Quiz Arena)

- [x] 2.1 Refactor `frontend/components/today/InterviewChallengePane.vue` to adopt `OptionCard.vue` for all scenario choices.
- [x] 2.2 Compact dock container padding in `InterviewChallengePane.vue` to `p-3.5 sm:p-4 md:p-5` and reduce stack spacing to `space-y-3 sm:space-y-4`, ensuring all 4 options and the submit button fit on 1080p desktop split view.
- [x] 2.3 Refactor `frontend/pages/quiz.vue` Arena tab to adopt `OptionCard.vue`, replacing legacy `p-4 sm:p-5 rounded-2xl` buttons.
- [x] 2.4 Compact question card padding in `frontend/pages/quiz.vue` to `p-4 sm:p-5` with `space-y-4`, eliminating vertical fold overflow for Option D and submission controls.

## 3. Frontend: Modal Refactoring & Page Container Standardization

- [x] 3.1 Refactor the document import modal in `frontend/pages/library.vue` to utilize `AppModal.vue`, pinning "Hủy" (Cancel) and "Lưu Tài Liệu" (Submit) in the sticky footer.
- [x] 3.2 Compact dropzone and form vertical spacing in `frontend/pages/library.vue` so PDF and Markdown import flows fit cleanly within `max-h-[60vh]`.
- [x] 3.3 Normalize outer page container padding in `frontend/pages/settings.vue` from `md:p-10` to `py-4 sm:py-5 px-4 sm:px-6` and standardize card padding to `p-4 sm:p-5`.
- [x] 3.4 Normalize outer page container padding in `frontend/pages/profile.vue` to `py-4 sm:py-5 px-4 sm:px-6` and reduce decorative passport avatar/banner bloat.
- [x] 3.5 Normalize outer page container padding in `frontend/pages/insights.vue` to `py-4 sm:py-5 px-4 sm:px-6` and reduce hero banner height so insight cards remain visible above the fold.

## 4. Frontend: Living Showcase Page & Navigation

- [x] 4.1 Port showcase components from `origin/v0/design-system-showcase` into `frontend/components/showcase/` (`PrimitivesShowcase.vue`, `ShowcaseSection.vue`).
- [x] 4.2 Port and wire `frontend/pages/showcase.vue` to reference `frontend/components/ui/` primitives across all 8 showcase sections.
- [x] 4.3 Add Showcase link to navigation in `frontend/composables/useNavigationMenu.ts` with `Palette` icon.
- [x] 4.4 Add `nav.showcase` to `frontend/i18n/locales/en.json` ("Design System") and `frontend/i18n/locales/vi.json` ("Hệ Thống Thiết Kế") while strictly preserving UTF-8 character encoding.

## 5. Verification & Test Suite Parity

- [x] 5.1 Run `npm test` across all frontend Vitest suites and update any test assertions affected by legacy CSS class removals.
- [x] 5.2 Verify layout density and fold visibility across 1080p desktop viewports in dark and light modes.

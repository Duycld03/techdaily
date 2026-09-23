# Proposal

## Why

TechDaily suffers from noticeable visual bloat and vertical clipping on desktop displays—specifically on Windows 11 with default 125% DPI display scaling, taskbars, and browser toolbars, where usable vertical viewport height is constrained to ~700px–850px.

While an earlier iteration aligned webfont metrics, the underlying layout suffered from excessive spacing (outer padding `md:p-10`, card padding `p-6` to `p-9`, `space-y-6` gaps), oversized control buttons (~85px per choice), unpinned modal footers that push critical "Save" and "Cancel" buttons below the screen fold, and towering hero banners. Consequently, Option D in multiple-choice scenarios, Quiz Arena submit buttons, and modal submission controls are routinely cut off.

Integrating the production-tested v0.dev design system and standardizing an Engineering Cockpit Density (Density 8/10) resolves these issues permanently, providing a cohesive UI component library and living styleguide for current and future feature development.

## What Changes

- **Integrate Reusable v0 Design System Components (`frontend/components/ui/`)**:
  - `OptionCard.vue`: Compact (~44px) multiple-choice interactive card with letter badge, status indicators, and 4 states (`default`, `selected`, `correct`, `incorrect`).
  - `AppModal.vue`: 3-tier modal dialog shell with Fixed Header, Scrollable Body (`max-h-[60vh]`), and Sticky Footer, ensuring action buttons never scroll off-screen.
  - `BentoStatCard.vue`: Compact metric card with tabular numerals, accent icons, and progress indicator.
  - `CodeBlock.vue`: Clean dark code container with file/language header, clipboard copy feedback, and custom 6px scrollbar.
  - `SkeletonShimmer.vue`: Pulse loading placeholders for cards, 4-option quiz rows, and metrics.
  - `EmptyState.vue`: Compact (< 220px) empty and error state card with icon ring and primary CTA.
  - `FloatingSelectionToolbar.vue`: Floating pill toolbar (~30px) for reader term selection actions.
- **Living Styleguide Page**:
  - Retain `frontend/pages/showcase.vue` at `/showcase` as an interactive design system catalog for developers and agents.
- **Cockpit Density Cutover across Affected Screens**:
  - `frontend/components/today/InterviewChallengePane.vue`: Adopt `OptionCard`, reduce dock padding from `md:p-8` to `p-3.5 sm:p-4 md:p-5`, and reduce internal gaps so all 4 options and the submit button fit within standard 1080p split view.
  - `frontend/pages/quiz.vue`: Adopt `OptionCard` in Quiz Arena, compact question card padding to `p-4 sm:p-5`, and eliminate option overflow.
  - `frontend/pages/library.vue`: Migrate `isImportModalOpen` to use `AppModal` with sticky footer, preventing submit/cancel truncation.
  - `frontend/pages/settings.vue`, `frontend/pages/profile.vue`, `frontend/pages/insights.vue`: Normalize outer container padding from `md:p-10` to `py-4 sm:py-5 px-4 sm:px-6` and reduce oversized decorative hero banners.
- **Encoding & i18n Parity**:
  - Add `nav.showcase` to `en.json` and `vi.json` while strictly preserving valid UTF-8 character encoding across all Vietnamese translations.

## Capabilities

### New Capabilities
_None._

### Modified Capabilities
- `core-platform`: Define Engineering Cockpit Density standards (Density 8/10), `h-9` control heights, concentric border radii, and 3-tier modal shell requirements.
- `today`: Require Scenario Challenge Dock to fit all 4 options and submission controls within the desktop viewport fold.
- `quiz`: Require Quiz Arena questions and options to remain fully visible above the fold on desktop viewports.
- `library`: Require document import and configuration dialogs to pin action buttons in a sticky footer.

## Impact

- **Frontend Components & Pages**:
  - New primitives in `frontend/components/ui/`
  - Living showcase in `frontend/pages/showcase.vue`
  - Updates to `InterviewChallengePane.vue`, `quiz.vue`, `library.vue`, `settings.vue`, `profile.vue`, `insights.vue`, `AppSidebar.vue`, and `useNavigationMenu.ts`
- **Backend / Database**: Zero impact. Pure frontend design system and layout density standardization.
- **Automated Tests**: Vitest suite (`npm test`) must continue to pass 100%.

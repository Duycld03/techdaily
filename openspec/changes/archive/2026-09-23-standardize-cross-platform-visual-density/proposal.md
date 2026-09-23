# Proposal

## Why

TechDaily suffers from a noticeable cross-platform visual density regression on Windows 11 Chromium browsers (Brave/Chrome) compared to Ubuntu Linux. Components such as the Scenario Challenge Dock (`InterviewChallengePane.vue`), Flashcard Deck (`FlashcardDeck.vue`), Dashboard Bento (`HomeBentoDashboard.vue`), and Library cards (`library.vue`) suffer from vertical bloat, oversized interactive text (`md:text-lg` / 18px on controls), excessive card padding (`p-6` to `p-8`, `md:p-10`), and unimported webfonts falling back to wider `Segoe UI` metrics.

On standard 1080p displays (1920x1080) with browser chrome and the Windows taskbar (available inner height ~856px), these oversized elements push crucial options and widgets below the viewport fold, requiring unnecessary scrolling and making the interface feel cramped despite large margins.

## What Changes

- **Uniform Cross-Platform Font Metrics**: Ensure the `Inter` webfont is properly imported in `frontend/nuxt.config.ts` via `@font-face` / Google Fonts so typography metrics (x-height, glyph width, bounding boxes) remain consistent across Windows, Linux, and macOS.
- **Decoupled Typography Hierarchy**:
  - **Reading/Prose Scale**: Preserve comfortable large text (`text-base md:text-lg` / 16px–18px) for long technical documentation in `DocReaderPane.vue` and `read/[bookId].vue`.
  - **UI/Control Scale**: Standardize interactive controls, scenario option buttons, input fields, flashcard questions, and metadata to `text-sm md:text-base` (14px–16px), eliminating `text-lg` bloat on controls.
- **Standardized Card & Container Spacing**:
  - Reduce bloated card padding (`p-6` to `p-8`) to `p-4 sm:p-5` across cards.
  - Reduce outer page padding in `review.vue` from `md:p-10` (40px) to `p-4 sm:p-6 md:p-8`.
- **Eliminate Viewport Locking Clipping**:
  - In `HomeBentoDashboard.vue`, replace `lg:h-[calc(100dvh-3.5rem)] lg:overflow-hidden` with `min-h-[calc(100dvh-3.5rem)] pb-8` so widgets (such as Card E: Knowledge Constellation) remain fully accessible and scroll smoothly if viewport height is reduced.
- **Compact Scenario Challenge Dock (`InterviewChallengePane.vue`)**:
  - Reduce question heading from `md:text-2xl` to `md:text-xl font-bold`.
  - Reduce option button padding from `sm:p-5` to `sm:p-3.5`.
  - Reduce option text from `md:text-lg` to `text-sm sm:text-base`.
  - Ensure all 4 multiple-choice options (A, B, C, D) remain fully visible within a 1080p desktop viewport without scrolling.
- **Compact Flashcard Deck (`FlashcardDeck.vue`)**:
  - Reduce card `min-h-[400px]` to `min-h-[300px] sm:min-h-[320px]`.
  - Reduce card padding from `p-8` to `p-5 sm:p-6`.
  - Reduce front challenge heading from `text-2xl font-extrabold` to `text-lg sm:text-xl font-bold`.
  - Reduce action CTA margin from `mt-8 pt-6` to `mt-5 pt-4`.
- **Streamline Library Cards (`library.vue`)**:
  - Reduce card padding from `p-6 sm:p-7 rounded-3xl` to `p-4 sm:p-5 rounded-2xl`.
  - Keep 3-column responsive grid with balanced whitespace.

## Capabilities

### New Capabilities
_None._

### Modified Capabilities
- `core-platform`: Standardize cross-platform typography metrics (Inter webfont loading), layout density tokens, and responsive container constraints.
- `today`: Optimize Scenario Challenge Dock typography and padding so all scenario choices fit within 1080p desktop viewport.
- `review`: Compact flashcard deck dimensions, padding, and question font scale to prevent viewport overflow.
- `library`: Optimize document card padding, sizing, and whitespace density.

## Impact

- **Frontend Assets & Config**:
  - `frontend/nuxt.config.ts`: Link Google Fonts for Inter or configure font preload.
  - `frontend/assets/css/main.css`: Standardize `.glass-card` base padding and component density tokens.
- **Frontend Components & Pages**:
  - `frontend/components/today/InterviewChallengePane.vue`
  - `frontend/components/review/FlashcardDeck.vue`
  - `frontend/components/dashboard/HomeBentoDashboard.vue`
  - `frontend/pages/library.vue`
  - `frontend/pages/review.vue`
  - `frontend/pages/today.vue`
  - `frontend/pages/insights.vue`
  - `frontend/pages/profile.vue`
  - `frontend/pages/settings.vue`
- **Zero Backend/Database Changes**: Pure CSS and Vue component layout/typography adjustments.
- **Testing**: Automated Vitest test suite (`npm test`) must continue to pass 100%.

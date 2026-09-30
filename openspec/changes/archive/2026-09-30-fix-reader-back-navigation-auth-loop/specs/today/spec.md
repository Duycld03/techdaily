# Spec Delta: today

## MODIFIED Requirements

### Requirement: Centered Padded Loading Banner on Mobile
The `/today` focus loading state SHALL have at least 24px (`p-6`) padding, centered alignment, and a constrained width (`max-w-sm sm:max-w-md`) on mobile viewports.

The `/today` study surface and auxiliary overlays SHALL strictly adhere to the Dev-Learning Studio design language and Responsive Typography Standard:

1. **Deterministic Loading State & Elimination of Black Canvas Voids**:
   - Whenever focus data is unresolved (`focusStore.isLoading || (!focusStore.data && !focusStore.error)`), the studio body SHALL render the centralized studio loading state with the spinner and descriptive subtitle (`pacer.ai_synthesis_desc`).
   - The page template SHALL NOT render an unconditioned empty container when `focusStore.isLoading` is false and `focusStore.data` is null during initial SSR or before the `onMounted` query completes, completely eliminating pitch-black screen voids on desktop (1080p) and mobile viewports.

2. **Interactive Challenge Option Typography**:
   - Option labels and choice text in `InterviewChallengePane.vue` SHALL render at a minimum of `text-sm` (14px) on mobile viewports and `text-base` (16px) on desktop viewports, strictly eliminating micro-text (`text-xs` / 12px) from body copy and option choices.

3. **Auxiliary Overlays & Modals**:
   - `AISynthesisCard.vue` and `TermExplainerModal.vue` SHALL utilize `.glass-panel`, `dark:bg-canvas-subtle`, and `dark:bg-canvas-elevated` with translucent hairline borders `dark:border-white/[0.08]`, completely eliminating legacy `dark:bg-slate-900` and `dark:border-slate-800`.

#### Scenario: User visits /today on mobile during synthesis
- **GIVEN** a user on a mobile viewport (<640px wide) opens `/today`
- **WHEN** the daily focus or scenario challenge is loading
- **THEN** the loading icon and explanatory text are vertically and horizontally centered with comfortable margins
- **AND** the explanatory text does not collide with or touch the device edges.

#### Scenario: Responsive scenario drill option typography on mobile
- **WHEN** user views the Senior Scenario Drill in `InterviewChallengePane.vue` on a mobile device (<640px)
- **THEN** option badges ("A", "B", "C", "D") and option description text render at `text-sm` (14px) or larger
- **AND** micro-text (`text-xs`) is not used for option text or explanation paragraphs.

#### Scenario: Glassmorphic studio synthesis and explainer overlays
- **WHEN** the AI synthesis card or term explainer modal renders in dark mode
- **THEN** they render with `dark:bg-canvas-subtle` and `dark:bg-canvas-elevated`
- **AND** borders display translucent hairline styling `dark:border-white/[0.08]`.

#### Scenario: User navigates to /today before data resolves
- **WHEN** the `/today` route is rendered on SSR or mounted before the initial `fetchTodayFocus` API call completes
- **THEN** the main workspace area SHALL display the centered loading indicator and subtitle
- **AND** the page SHALL NOT render a pitch-black empty void.

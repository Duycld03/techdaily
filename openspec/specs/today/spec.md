# today Specification

## Purpose
TBD - created by archiving change all-in-one-quiz-and-lazy-prefetch. Update Purpose after archive.

## Requirements

### Requirement: All-in-One Generation on Today Page
When a user opens `/today` on an uncurated slice, the system SHALL curate both the reading context and the Senior Scenario Drill simultaneously in a single AI invocation.

#### Scenario: User visits /today on an uncurated slice
- **GIVEN** a book pacer set to an uncurated slice (e.g. Slice 4)
- **WHEN** the user opens `/today`
- **THEN** a loading indicator indicates that AI is preparing today's focus and challenge
- **AND** both the formatted reading markdown and the Senior Scenario Drill populate simultaneously upon completion.

### Requirement: Next-Day Prefetching on Today Page
When the user is active on `/today` viewing Slice $N$, the system SHALL trigger a background prefetch for Slice $N+1$.

#### Scenario: Silent prefetch for tomorrow's reading
- **GIVEN** the user is viewing Slice 4 on `/today`
- **AND** Slice 5 is not yet AI-curated
- **WHEN** the `/today` page finishes mounting
- **THEN** an asynchronous background request curates Slice 5
- **AND** navigation to Slice 5 on the following day renders instantly with zero wait.

### Requirement: Centered Padded Loading Banner on Mobile
The `/today` focus loading state SHALL have at least 24px (`p-6`) padding, centered alignment, and a constrained width (`max-w-sm sm:max-w-md`) on mobile viewports.

The `/today` study surface and auxiliary overlays SHALL strictly adhere to the Dev-Learning Studio design language and Responsive Typography Standard:
1. **Interactive Challenge Option Typography:** Option labels and choice text in `InterviewChallengePane.vue` SHALL render at a minimum of `text-sm` (14px) on mobile viewports and `text-base` (16px) on desktop viewports, strictly eliminating micro-text (`text-xs` / 12px) from body copy and option choices.
2. **Auxiliary Overlays & Modals:** `AISynthesisCard.vue` and `TermExplainerModal.vue` SHALL utilize `.glass-panel`, `dark:bg-canvas-subtle`, and `dark:bg-canvas-elevated` with translucent hairline borders `dark:border-white/[0.08]`, completely eliminating legacy `dark:bg-slate-900` and `dark:border-slate-800`.

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

### Requirement: Strict Authentication on Today Page
The `/today` focus studio SHALL require an authenticated user session. Unauthenticated requests to `/today` SHALL redirect to `/login?redirect=/today`. The backend endpoint `GET /api/v1/daily/today` SHALL require authorization and return HTTP 401 Unauthorized when requested without a valid JWT token.

#### Scenario: Unauthenticated visitor visits /today
- **WHEN** unauthenticated visitor navigates to `/today`
- **THEN** route middleware redirects to `/login` with redirect query parameter `/today`.

#### Scenario: Unauthenticated visitor visits root url /
- **WHEN** unauthenticated visitor navigates to `/`
- **THEN** route middleware redirects to `/login` with redirect query parameter `/`.

#### Scenario: Unauthenticated API request to /api/v1/daily/today
- **WHEN** unauthenticated request is sent to `GET /api/v1/daily/today`
- **THEN** server returns HTTP 401 Unauthorized.

### Requirement: Scenario Drill Multiple-Choice Interface & Evaluation Feedback
The Scenario Challenge multiple-choice interface (`InterviewChallengePane.vue`) and auxiliary panels (`DocReaderPane.vue`) on `/today` SHALL style option cards, score badges, and evaluation feedback according to Dev-Learning Studio design tokens:
1. **Unselected & Inactive Options**: SHALL use studio subtle background (`dark:bg-canvas-subtle`), elevated surface (`dark:bg-canvas-elevated`), and hairline borders (`dark:border-white/[0.04]` to `dark:border-white/[0.08]`), eliminating all legacy dark slate classes (`dark:bg-slate-800`, `dark:border-slate-800/60`, `dark:bg-slate-950/20`).
2. **Semantic Success & Optimal Choice**: When reviewed, the optimal choice option card SHALL render with studio-grade translucent emerald styling (`border-emerald-500 dark:border-emerald-500/40 bg-emerald-50/80 dark:bg-emerald-500/10 text-emerald-950 dark:text-emerald-100 ring-2 ring-emerald-500/20`), preserving clear semantic success distinction without neon over-saturation.
3. **Score & Status Telemetry Badges**: Score telemetry badge (`+10 Pts`) and status pill badges SHALL render with translucent emerald pill styling (`dark:bg-emerald-950/40 dark:border-emerald-500/30 dark:text-emerald-300`).
4. **Authoritative Source Excerpt**: In `DocReaderPane.vue`, the source context box SHALL render as a `.glass-panel` container with `dark:bg-canvas-subtle/80`, `dark:border-white/[0.08]`, and brand telemetry header (`text-brand-600 dark:text-brand-400`), eliminating legacy green background tint (`bg-emerald-500/5 dark:bg-emerald-950/20`).

#### Scenario: Reviewed optimal choice rendering in dark mode
- **WHEN** user submits an answer to the Senior Scenario Drill on `/today` in dark mode
- **THEN** the optimal choice card displays with translucent emerald border (`dark:border-emerald-500/40`) and subtle tint (`dark:bg-emerald-500/10`)
- **AND** the letter badge displays with solid emerald fill (`bg-emerald-600 text-white`)
- **AND** the optimal choice pill badge renders with high-contrast text and crisp checkmark icon
- **AND** other unselected options display with neutral studio slate/white tokens (`dark:bg-slate-950/20` replaced with `dark:bg-canvas-subtle/30` and `dark:border-white/[0.04]`).

#### Scenario: Authoritative source excerpt rendering in dark mode
- **WHEN** user views a reading slice on `/today` that contains an authoritative source excerpt
- **THEN** the excerpt box renders with `.glass-panel` container styling with `dark:bg-canvas-subtle/80` and `dark:border-white/[0.08]`
- **AND** the header label renders with Deep Iris Violet telemetry color (`text-brand-600 dark:text-brand-400`) rather than emerald green.

### Requirement: Concentric Metric Card Review Deck Navigation
The concentric metric card (`ConcentricMetricCard.vue`) SHALL provide spaced repetition review deck navigation exclusively through its bottom action row banner. The card header SHALL NOT display duplicate review deck navigation links, maintaining a focused header layout consisting of the icon, title, and subtitle.

#### Scenario: User views concentric metric card with zero cards due
- **WHEN** user views the concentric metric card and `dueCards === 0`
- **THEN** the card header does NOT render any "View Deck" / "Xem Bộ Thẻ" link
- **AND** the bottom action banner renders a single button displaying `dashboard.view_deck` ("Xem Bộ Thẻ ↗") linking to `/review`.

#### Scenario: User views concentric metric card with cards due
- **WHEN** user views the concentric metric card and `dueCards > 0`
- **THEN** the card header does NOT render any "View Deck" / "Xem Bộ Thẻ" link
- **AND** the bottom action banner renders a primary brand button displaying `dashboard.review_now` ("Ôn Luyện Ngay (Space) ↗") linking to `/review`.

### Requirement: Home Dashboard & Daily Focus Studio Responsive Bento Layout
The Home Dashboard (`pages/index.vue`) and Today's Focus Studio (`pages/today.vue`) SHALL adapt seamlessly across screen widths from $320\text{px}$ to $1440\text{px}$, ensuring Bento cards stack cleanly, SVG concentric progress metrics scale without clipping, and auxiliary modals preserve full touch accessibility.

#### Scenario: Mobile Bento Grid Reflow
- **WHEN** user views `pages/index.vue` on a mobile device ($< 768\text{px}$)
- **THEN** multi-column Bento cards (`HomeBentoDashboard.vue`, `DomainConstellationCard.vue`) SHALL collapse into a single-column stacked layout with minimum gap of `0.875rem` (`gap-3.5`)
- **AND** zero horizontal overflow or clipping SHALL occur.

#### Scenario: Concentric Metric Card Responsive Scaling
- **WHEN** user views `ConcentricMetricCard.vue` on viewports between $320\text{px}$ and $480\text{px}$
- **THEN** the SVG rings SHALL scale proportionally within their `viewBox` container without overlapping numeric score labels or legend text.

#### Scenario: Term Explainer Modal Scroll Containment
- **WHEN** user triggers the term explanation popup (`TermExplainerModal.vue`) with lengthy AI-curated markdown on a mobile screen
- **THEN** the modal body SHALL provide smooth vertical scrolling with max height constrained to `85dvh` and safe area bottom clearance.

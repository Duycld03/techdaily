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

In addition, the Home Dashboard orientation header banner (`HomeBentoDashboard.vue`) SHALL maintain strict single-line text integrity for status badges:
1. **Single-Line Badge Integrity**: The "Executive Cockpit" orientation badge SHALL render on a single horizontal line with `whitespace-nowrap shrink-0` across all viewports down to $320\text{px}$, strictly prohibiting two-line vertical text wrapping or lopsided pill distortion.
2. **Responsive Metadata Row Alignment**: The orientation banner metadata container SHALL permit flexible wrapping (`flex-wrap`) and responsive gaps (`gap-1.5 sm:gap-2`), ensuring the target role and slice progress indicators never compress the status badge into an asymmetric vertical layout on narrow mobile viewports ($< 640\text{px}$).

#### Scenario: Mobile Bento Grid Reflow
- **WHEN** user views `pages/index.vue` on a mobile device ($< 768\text{px}$)
- **THEN** multi-column Bento cards (`HomeBentoDashboard.vue`, `DomainConstellationCard.vue`) SHALL collapse into a single-column stacked layout with minimum gap of `0.875rem` (`gap-3.5`)
- **AND** zero horizontal overflow or clipping SHALL occur.

#### Scenario: Orientation Header Badge Non-Wrapping on Mobile Viewports
- **GIVEN** a user accesses the Home Dashboard on a mobile viewport ($< 640\text{px}$, such as $360\text{px}$ or $390\text{px}$)
- **WHEN** the orientation header banner is rendered
- **THEN** the "Executive Cockpit" status badge displays strictly on a single horizontal line without word splitting or vertical line breaks
- **AND** the badge retains uniform rounded pill geometry and symmetric padding (`px-2.5 py-0.5 rounded-full`)
- **AND** the adjacent target role and slice reading metadata wrap to the next line or fit without forcing the badge to collapse.

#### Scenario: Concentric Metric Card Responsive Scaling
- **WHEN** user views `ConcentricMetricCard.vue` on viewports between $320\text{px}$ and $480\text{px}$
- **THEN** the SVG rings SHALL scale proportionally within their `viewBox` container without overlapping numeric score labels or legend text.

#### Scenario: Term Explainer Modal Scroll Containment
- **WHEN** user triggers the term explanation popup (`TermExplainerModal.vue`) with lengthy AI-curated markdown on a mobile screen
- **THEN** the modal body SHALL provide smooth vertical scrolling with max height constrained to `85dvh` and safe area bottom clearance.
### Requirement: Scenario Challenge Multiple-Choice Interface & Dock Layout Density
The Scenario Challenge multiple-choice interface (`InterviewChallengePane.vue`) on `/today` SHALL style option cards, score badges, and evaluation feedback according to the standardized `OptionCard` design primitive while guaranteeing complete 1080p desktop viewport visibility:
1. **Compact Option Primitive**: Option cards SHALL use `OptionCard.vue` with `px-3 py-2.5 rounded-lg border text-sm` (~44px height), letter badge `h-7 w-7 text-xs font-bold`, and 4 discrete states (`default`, `selected`, `correct`, `incorrect`).
2. **Dock Container Spacing**: The right Scenario Dock container padding SHALL standardize to `p-3.5 sm:p-4 md:p-5` (eliminating `md:p-8`), and internal section spacing SHALL standardize to `space-y-3 sm:space-y-4`.
3. **Zero-Scroll Desktop Choice Invariant**: On a 1080p desktop viewport (1920x1080) in side-by-side split view with browser chrome and OS taskbars, all four multiple-choice options (A, B, C, D) alongside the primary submit action button SHALL remain fully visible in the right Scenario Dock without requiring vertical scrolling.

#### Scenario: All multiple-choice options visible on 1080p desktop
- **WHEN** a user opens `/today` on a standard 1080p screen (1920x1080) with browser chrome (tabs, URL bar, bookmarks bar) and system taskbar visible
- **THEN** all 4 option cards (A, B, C, D) and the submission button are rendered completely above the bottom fold of the Scenario Dock without requiring scrolling
- **AND** option cards render at approximately 44px height with `px-3 py-2.5 rounded-lg`.

#### Scenario: Option choice text remains readable and compact
- **WHEN** reading scenario descriptions and option trade-offs in `InterviewChallengePane.vue`
- **THEN** option text renders at `text-sm sm:text-base` (14px–16px), maintaining sharp typography while preventing bloated button heights.

### Requirement: Authentic Learned Slices Metric in Domain Constellation Card

In the Home Bento Dashboard (`HomeBentoDashboard.vue`), the Domain Knowledge Constellation widget (`DomainConstellationCard.vue`) SHALL display the user's authentic cumulative count of learned and completed document slices (*Lát cắt đã học* / `stat_learned_chunks`), aggregated across the user's active and library document pacers (`Math.max(0, currentChunkOrder - 1)` for in-progress documents, and `totalChunks` for fully completed documents).

The dashboard SHALL NOT pass or display the total slice capacity of the largest document (e.g. `pacer.totalChunks`) as the learned slice count.

#### Scenario: Domain Constellation renders accurate learned slices count
- **WHEN** an authenticated user with active reading pacers views the Home Bento Dashboard
- **THEN** the Domain Constellation widget displays the authentic sum of completed slices across reading pacers
- **AND** the count reflects slices already read rather than the total slice count of the largest document.

---

### Requirement: Daily Pacer Automatic Slice Progression on Calendar Day Change
When accessing the Daily Focus studio without an explicit `chunkOrder` parameter on a subsequent calendar day (`today > LastReadDate`), the system SHALL increment the active pacer's slice order by one (`CurrentChunkOrder + 1`, bounded by `TotalChunks`) and update `LastReadDate` to today if the current slice's drill is completed. Within the same calendar day or when the prior drill is incomplete, the system SHALL preserve the current slice.

#### Scenario: User opens today studio on subsequent day after completing prior slice
- **GIVEN** an authenticated user whose active pacer is on Slice 1 of a book with 75 slices
- **AND** the scenario drill for Slice 1 was completed on 2026-09-28 (`Status == Reviewed`)
- **AND** the user opens `/today` on a subsequent date 2026-10-06 without a `chunkOrder` parameter
- **WHEN** the system resolves today's focus
- **THEN** the active pacer's `CurrentChunkOrder` advances to Slice 2
- **AND** `LastReadDate` updates to 2026-10-06
- **AND** the response provides reading content for Slice 2 and a fresh unattempted `Pending` scenario challenge drill.

#### Scenario: User visits today studio multiple times on same calendar day
- **GIVEN** an authenticated user completed the daily scenario challenge drill for Slice 2 on 2026-10-06
- **WHEN** the user reloads `/today` or revisits the page later on the same date (2026-10-06)
- **THEN** the system keeps the learner on Slice 2
- **AND** the scenario challenge pane displays the completed evaluation state (`Reviewed`), optimal answer highlighting, and architectural breakdown.

#### Scenario: User visits today studio on subsequent day with incomplete prior drill
- **GIVEN** an authenticated user's active pacer is on Slice 2 with an unattempted or pending drill
- **WHEN** the user opens `/today` on the following calendar day without completing the drill
- **THEN** the active pacer remains on Slice 2
- **AND** the system presents Slice 2 and its pending drill so the learner can complete the required study before advancing.

#### Scenario: User manually navigates slices via table of contents or pagination
- **GIVEN** an active pacer positioned at Slice 2 for today's study
- **WHEN** the user clicks pagination controls or table of contents to view Slice 5 (`?chunkOrder=5`)
- **THEN** the system serves the reading content for Slice 5
- **AND** returning to default daily focus without `chunkOrder` preserves the pacer's authentic daily progress position.

#### Scenario: User completes final slice of book
- **GIVEN** an active pacer positioned at the final slice of a book (`CurrentChunkOrder == TotalChunks`)
- **WHEN** the user completes the final slice's scenario challenge drill
- **THEN** the pacer marks `CompletedAt` with the current timestamp
- **AND** subsequent daily visits remain anchored at `TotalChunks` without out-of-bounds index errors.

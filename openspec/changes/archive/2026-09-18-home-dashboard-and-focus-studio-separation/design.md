# Design: Home Dashboard and Focus Studio Separation

## Context
Currently, the application mounts `frontend/pages/index.vue` with a simple programmatic redirect `navigateTo('/today', { replace: true })`. On `/today`, a composable `useTodayViewMode.ts` toggles between `'bento'` (Dashboard) and `'split'` (Focus Studio). Because the Top Pacer navigation bar (`< Prev`, `Next >`, `Slice 1/30`) is declared at the layout root of `today.vue`, it renders across both view modes. This causes slice navigation to render above the high-level Bento Dashboard, creating the confusing impression that each reading slice has an independent dashboard.

Additionally:
1. The Welcome Banner in `TodayBentoDashboard.vue` features an out-of-context `"Ask AI Explainer"` button before the user has engaged with any text or problem.
2. In `ConcentricMetricCard.vue`, `h-full` and `justify-between` stretch the card vertically, pushing the 7-day consistency calendar and knowledge radar off-screen on desktop/laptop displays.

## Goals / Non-Goals

**Goals:**
- Promote the Bento Dashboard to the root route `/` (`frontend/pages/index.vue`) as the primary **Home Command Center**.
- Re-architect `/today` (`frontend/pages/today.vue`) as the dedicated, distraction-free **Focus Studio** (pacer slice bar, document reader, scenario challenge).
- Constrain the desktop Home Dashboard to a **Zero-Scroll Single-Screen** layout (`lg:h-[calc(100vh-3.5rem)] lg:overflow-hidden`), ensuring all 5 cards are visible above the fold on desktop viewports.
- Remove the out-of-context "Ask AI Explainer" button from the Welcome Banner.
- Optimize `ConcentricMetricCard.vue` geometry ($R_1=46\text{px}, R_2=34\text{px}$) to prevent vertical stretching.
- Update `AppSidebar.vue` and `AppCommandPalette.vue` to represent `/` as "Dashboard" and `/today` as "Today's Focus".

**Non-Goals:**
- Modifying backend API contracts, database entities, or migration scripts.
- Modifying Markdown-it rendering in `DocReaderPane.vue` or grading logic in `InterviewChallengePane.vue`.
- Changing mobile responsiveness (mobile/tablet continues to use vertical scrolling).

## Decisions

### 1. Route-Level Architecture vs. View-Mode State
- **Decision:** Split the two modes into distinct routes (`/` for Dashboard, `/today` for Focus Studio) rather than toggling state within `/today`.
- **Rationale:** Route-level separation creates a clean mental model, simplifies URL sharing/bookmarking, and eliminates the awkward presence of the slice pacer bar on the executive dashboard.
- **Action Transition:** The `"Continue Reading"` and `"Solve Challenge"` buttons on the Home Dashboard will execute `navigateTo('/today')`.

### 2. Zero-Scroll Desktop Viewport Geometry
- **Decision:** Apply `h-[calc(100vh-3.5rem)]` on desktop (`lg:`) with `overflow-hidden` and `flex flex-col justify-between`.
- **Layout Budget on standard 800px-900px viewport:**
  - AppHeader: $56\text{px}$ (`h-14`)
  - Dashboard Container: $\sim 780\text{px} - 840\text{px}$
    - Welcome Banner: $\sim 68\text{px}$ (compact single-row orientation ribbon)
    - Grid gap: $16\text{px}$
    - Bento Grid: $\sim 540\text{px} - 580\text{px}$
      - Left Column (col-span-2): Active Reading Slice ($\sim 260\text{px}$) + Scenario Challenge ($\sim 260\text{px}$)
      - Right Column (col-span-1): Concentric Metric Card ($\sim 210\text{px}$) + 7-Day Consistency ($\sim 140\text{px}$) + Knowledge Radar ($\sim 140\text{px}$)
- **Fallback:** Viewports with vertical height $< 700\text{px}$ or mobile devices retain `overflow-y-auto` to prevent clipping.

### 3. Contextual AI Explainer Restraint
- **Decision:** Remove `Ask AI Explainer` button from the Welcome Banner.
- **Rationale:** Placing an AI explainer on the home banner is ungrounded because no technical term or document is active. AI explanation belongs in:
  1. The document reader (floating text selection toolbar).
  2. The interview scenario pane (hint/explainer assistant).

### 4. Global Navigation Structure (`AppSidebar.vue` & `AppCommandPalette.vue`)
- **Decision:** Restructure navigation links:
  - Top item in Practice group: `Dashboard` (`/`, icon: `LayoutGrid`)
  - Second item: `Today's Focus` (`/today`, icon: `Target`)
- **Active Route Detection:** `/` is active only when `route.path === '/'`. `/today` is active when `route.path.startsWith('/today')`.

## Risks / Trade-offs

- **Risk:** Existing users navigating to `/today` expecting a dashboard overview.
  - **Mitigation:** `/today` is dedicated to active practice. If users want the dashboard, the sidebar header and logo link to `/`.
- **Risk:** Dense visual packing on 13-inch laptops (e.g. 1366x768 or 1280x800).
  - **Mitigation:** Use `min-h-0` on cards and responsive padding (`p-4 sm:p-5`), with subtle `overflow-y-auto` on the grid container if the viewport height is unusually constrained.

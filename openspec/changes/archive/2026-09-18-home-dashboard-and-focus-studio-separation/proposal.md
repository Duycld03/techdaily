# Proposal: Home Dashboard and Focus Studio Separation

## Why
Currently, the `/today` route serves two conflicting purposes: a high-level executive Bento Dashboard and an intensive split-pane reading/challenge Focus Studio. This conflation places the granular slice navigation bar (`< Prev`, `Next >`, `Slice 1/30`) directly above the Bento Dashboard, creating user confusion as if each micro-slice had its own individual dashboard. Furthermore, the welcome banner places an out-of-context "Ask AI Explainer" button upon initial app launch before the user has encountered any technical concepts, while on desktop screens the dashboard suffers from vertical stretching that forces unnatural scrolling.

Separating the Home Dashboard (command center at `/`) from Today Focus Studio (deep reading workspace at `/today`) creates a clean mental model, eliminates redundant slice navigation from high-level metrics, removes the premature AI explainer button, and fits the executive dashboard onto a single desktop screen without scrolling.

## What Changes
- **Root Route (`/`) Redirection Replacement**: Replace the trivial `navigateTo('/today')` in `frontend/pages/index.vue` with a dedicated, top-level **Home Dashboard** page.
- **Remove AI Explainer from Welcome Banner**: Strip the out-of-context "Ask AI Explainer" button from `TodayBentoDashboard.vue`, keeping AI explanations strictly contextual inside the reading workspace, text selection toolbar, and scenario challenges.
- **Zero-Scroll Single-Screen Desktop Layout**: Constrain the desktop Bento Dashboard container (`h-[calc(100vh-3.5rem)]`) with optimized card heights and compact metrics so all 5 cards (Welcome, Active Slice, Scenario, Concentric Ring Metrics, 7-Day Consistency, Knowledge Radar) fit cleanly above the fold on desktop/laptop viewports.
- **Refocus `/today` as Pure Focus Studio**: Remove the Bento Dashboard toggle from `/today`. The `/today` route becomes the dedicated, distraction-free **Focus Studio** with the slice pacer navigation bar (`< Prev`, `Next >`), DocReaderPane, and InterviewChallengePane.
- **Action Transition**: Clicking `"Continue Reading"` or `"Solve Challenge"` on the Home Dashboard navigates directly to `/today`.
- **Sidebar & App Shell Navigation**: Update `frontend/components/layout/AppSidebar.vue` and `AppCommandPalette.vue` so that `/` is clearly represented as "Dashboard" / "Home", and `/today` is represented as "Today's Focus" / "Focus Studio".

## Capabilities

### New Capabilities
<!-- None: Re-architecting existing capabilities between root dashboard and focus studio. -->

### Modified Capabilities
- `today`: Spec change to focus `/today` exclusively on the Focus Studio workspace (pacer bar, document reader, scenario drill), removing the embedded view-switcher and dashboard rendering.
- `core-platform`: Spec change to establish the root route `/` as the executive Home Command Center with single-screen zero-scroll desktop constraints, contextual action routing to `/today`, and updated global navigation links.

## Impact
- **Frontend Pages & Components**:
  - `frontend/pages/index.vue` (renders Home Dashboard instead of redirecting)
  - `frontend/pages/today.vue` (focus studio workspace only, removes view switcher)
  - `frontend/components/today/TodayBentoDashboard.vue` (removes AI Explainer button, optimizes heights and padding for zero-scroll)
  - `frontend/components/today/ConcentricMetricCard.vue` (removes unbounded `h-full` stretching)
  - `frontend/components/layout/AppSidebar.vue` (updates navigation item paths and labels)
  - `frontend/components/app/AppCommandPalette.vue` (updates fuzzy search routes)
- **Unit Tests**:
  - Update `useTodayViewMode.spec.ts` or deprecate in favor of direct route separation.
  - Update page rendering tests for `index.vue` and `today.vue`.
  - Update `ConcentricMetricCard.spec.ts`.
- **Backend / Database**: Zero schema changes or API migrations required.

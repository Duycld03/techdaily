# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Home Command Center Dashboard & Zero-Scroll Desktop Layout
The root route `/` SHALL host the primary **Home Command Center Dashboard** (`frontend/pages/index.vue`), presenting an executive overview of daily momentum, active reading slice, scenario drill, retention metrics, 7-day consistency, and knowledge cosmos connectivity.

1. **Information Architecture & Contextual AI Restraint:**
   - **Welcome Banner:** Displays personalized greeting (`"Welcome Back, {Name}!"`) and curriculum progress pill (`"Curriculum Day {N} / 30"`). The banner SHALL NOT include an "Ask AI Explainer" button; AI explanation is strictly reserved for in-context reading text selection and scenario problem solving.
   - **Active Reading Hero:** Displays active curriculum slice title, summary, reading time estimate, progress percentage, and a prominent `"Continue Reading →"` CTA button navigating to `/today`.
   - **Scenario Challenge Card:** Displays the architectural interview scenario teaser, score reward (`+10 Points`), and a `"Solve Challenge →"` CTA button navigating to `/today`.
   - **Active Recall & Concentric Metrics:** Renders dual concentric SVG rings for daily study pace and SM-2 retention health, constrained in height to prevent stretching.
   - **7-Day Consistency Matrix:** Renders weekly completion dots and active streak flames with freeze credit indicators.
   - **Knowledge Graph Radar:** Displays connected concept counts and active relation counts with a direct link to the 3D Cosmos (`/graph`).

2. **Zero-Scroll Single-Screen Desktop Layout Invariant:**
   - On desktop screens ($\ge 1024\text{px}$), the dashboard container SHALL fit entirely within the viewport (`h-[calc(100vh-3.5rem)]`) using `overflow-hidden` so that all 5 cards are visible above the fold with zero required scrolling.
   - On mobile ($< 640\text{px}$) and tablet ($640\text{px} - 1023\text{px}$) viewports, the layout SHALL transition to a natural vertically scrollable stack.

3. **Global Navigation Alignment:**
   - The desktop sidebar (`AppSidebar.vue`) and mobile navigation drawer SHALL represent `/` as the primary `"Dashboard"` / `"Home"` entry and `/today` as `"Today's Focus"` / `"Focus Studio"`.
   - The command palette (`AppCommandPalette.vue`) SHALL register `/` as the primary Dashboard route.

#### Scenario: Authenticated user visits root route /
- **WHEN** an authenticated user navigates to `/`
- **THEN** the system renders the Home Command Center Dashboard
- **AND** the Welcome Banner displays the user's greeting without an AI explainer button
- **AND** all metrics and active curriculum cards populate.

#### Scenario: Single-screen desktop presentation
- **WHEN** the dashboard is viewed on a desktop viewport ($\ge 1024\text{px}$)
- **THEN** the entire dashboard container fits within the viewport height without vertical scrolling
- **AND** the Concentric Metric Card, 7-Day Consistency Matrix, and Knowledge Graph Radar are simultaneously visible above the fold.

#### Scenario: User clicks Continue Reading on Home Dashboard
- **WHEN** the user clicks "Continue Reading" on the active reading slice card
- **THEN** the router navigates to `/today`
- **AND** the Focus Studio reading pane renders the active slice.

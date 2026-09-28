# Spec Delta

## MODIFIED Requirements

### Requirement: Home Command Center Dashboard & Zero-Scroll Desktop Layout
The root route `/` SHALL host the primary **Home Command Center Dashboard** (`frontend/pages/index.vue`), presenting an executive overview of daily momentum, active reading slice, scenario drill, retention metrics, 7-day consistency, and knowledge cosmos connectivity with clean visual decluttering:

1. **Information Architecture & Action Stage Structure:**
   - **Component Organization & Decoupling:** The Home Dashboard component SHALL reside in `frontend/components/dashboard/HomeBentoDashboard.vue`, cleanly decoupled from the reading focus studio components in `frontend/components/today/`.
   - **Welcome Banner:** Displays personalized greeting (`"Welcome Back, {Name}!"`) and dynamic document progress pill (`"Slice {currentChunkOrder} / {totalChunks}"` / `"Lát cắt {currentChunkOrder} / {totalChunks}"`). The banner SHALL NOT include an "Ask AI Explainer" button and SHALL NOT display ambient radial blur glows.
   - **Active Reading Hero (Card A):** In the left 2-column Action Stage, Card A SHALL render the user's active document slice, displaying book title, chapter title, summary, estimated read time, slice progress percentage, and a prominent `"Continue Reading →"` CTA button navigating to `/read/${bookId}?slice=${currentChunkOrder}`. The header of Card A SHALL NOT display daily drill scores or quiz completion badges.
   - **Scenario Challenge & Practice Cockpit (Card B):** Below Card A in the Action Stage, Card B SHALL render the daily technical dilemma or architectural scenario, displaying practice badge, curriculum day, scenario question title, situation teaser, semantic drill status badge, and an actionable CTA button (`"Solve Challenge →"` when pending, or `"Review Solution →"` when completed).
   - **Semantic Drill Status Badges:** The daily drill status indicator SHALL enforce accurate semantic states:
     - When pending/unsubmitted: Renders an amber status badge indicating point reward (`+10 Points` / `+10 Điểm thưởng`) with a target icon.
     - When completed with correct answer (`Score > 0`): Renders an emerald success badge (`✓ Completed: 10/10` / `✓ Hoàn Thành: 10/10`) with a checkmark icon.
     - When completed with incorrect answer (`Score == 0`): Renders an amber/rose status badge (`✗ Needs Review: 0/10` / `✗ Chưa Đạt: 0/10`) with an alert or cross icon, strictly prohibiting green styling and checkmark icons for zero scores.
   - **Active Recall & Concentric Metrics:** Renders dual concentric SVG rings for daily study pace and SM-2 retention health, constrained in height to prevent stretching.
   - **7-Day Consistency Matrix:** Renders weekly completion dots and active streak flames with freeze credit indicators.
   - **Live Knowledge Graph Breakdown (Card E):** In the right telemetry dock, Card E (`DomainConstellationCard.vue`) SHALL render a dynamic personal knowledge breakdown table displaying real telemetry derived from the user's graph data (`graphStore.rawData`):
     - Total Spaced Repetition flashcards (`card` count).
     - Saved technical highlights & reflections (`highlight` count).
     - Completed document slices (`chunk` count).
     - Live connected concepts count (`TotalNodes`) and semantic relations count (`TotalEdges`).
     - A direct link to the interactive 3D Cosmos (`/graph`).
     - Card E SHALL NOT render hardcoded, static SVG vertices or unreactive placeholder pillar illustrations.

2. **Crash-Free Lifecycle & Store Hardening:**
   - When querying Spaced Repetition deck stats, the component SHALL safely invoke valid `useReviewStore` methods (`fetchDeckCards({ pageSize: 1 })` or `fetchReviewDeck()`) and read counts from `deckStatistics` and `totalCardsDue` with defensive fallbacks, NEVER calling non-existent methods or throwing unhandled synchronous exceptions.
   - When querying Knowledge Graph data, the component SHALL read from `graphStore.rawData` with defensive fallbacks.
   - Full page refreshes (F5) and direct URL navigation to `/` SHALL render the dashboard reliably with zero uncaught runtime errors and zero redirection to `error.vue` (500 Internal Server Error).

3. **Zero-Scroll Single-Screen Desktop Layout Invariant:**
   - On desktop screens ($\ge 1024\text{px}$), the dashboard container and column flexboxes SHALL be top-aligned (`justify-start`) with consistent, snug vertical gaps (`gap-3.5 sm:gap-4`), preventing cards from scattering or dispersing to the vertical extremes on tall displays while fitting entirely within the viewport (`h-[calc(100vh-3.5rem)]`) with zero required scrolling.
   - The dual-card Action Stage (Cards A & B) and the triple-card Telemetry Dock (Cards C, D & E) SHALL align vertically without large empty voids.
   - On mobile ($< 640\text{px}$) and tablet ($640\text{px} - 1023\text{px}$) viewports, the layout SHALL transition to a natural vertically scrollable stack.

4. **Global Navigation Alignment:**
   - The desktop sidebar (`AppSidebar.vue`) and mobile navigation drawer SHALL represent `/` as the primary `"Dashboard"` / `"Home"` entry and `/today` as `"Today's Focus"` / `"Focus Studio"`.
   - The command palette (`AppCommandPalette.vue`) SHALL register `/` as the primary Dashboard route.
   - The global top header (`AppHeader.vue`) SHALL render dynamic slice progress without hardcoded `/ 30` boundaries.

#### Scenario: Authenticated user visits root route /
- **WHEN** an authenticated user navigates to `/`
- **THEN** the system renders the Home Command Center Dashboard
- **AND** the Welcome Banner displays the user's greeting without an AI explainer button
- **AND** all metrics and active curriculum cards populate.

#### Scenario: Single-screen desktop presentation
- **WHEN** the dashboard is viewed on a desktop viewport ($\ge 1024\text{px}$)
- **THEN** the entire dashboard container fits within the viewport height without vertical scrolling
- **AND** the Concentric Metric Card, 7-Day Consistency Matrix, and Knowledge Graph Constellation are simultaneously visible above the fold.

#### Scenario: User clicks Continue Reading on Home Dashboard
- **WHEN** the user clicks "Continue Reading" on the active reading slice card
- **THEN** the router navigates to `/read/${bookId}?slice=${currentChunkOrder}`
- **AND** the GitBook reader renders the document positioned at that active slice.

#### Scenario: User clicks Solve Challenge on Home Dashboard
- **WHEN** the user clicks "Solve Challenge" on the scenario drill card
- **THEN** the router navigates to `/today?tab=challenge`
- **AND** the Focus Studio opens with the architectural challenge pane active.

#### Scenario: Authenticated user loads or refreshes the Home Dashboard
- **WHEN** an authenticated user navigates directly to `/` or performs a browser refresh (F5)
- **THEN** the server and client render the Home Bento Dashboard without triggering unhandled JavaScript lifecycle exceptions or navigating to the 500 error boundary page.

#### Scenario: User inspects the Domain Knowledge Constellation card
- **WHEN** user views Card E on the Home Dashboard
- **THEN** the card renders a live personal knowledge breakdown table displaying real user learning metrics (flashcards, notes, slices, connected concepts, relations)
- **AND** does NOT render a static hardcoded SVG constellation of predefined points
- **AND** the "Open 3D Cosmos" header link remains stable in position when hovering anywhere on the card.

#### Scenario: Store data is initially empty or loading
- **WHEN** store data is in a loading or empty state during dashboard initialization
- **THEN** the dashboard renders graceful fallbacks for metrics, cards, and constellation telemetry without throwing `TypeError` or breaking layout geometry.

#### Scenario: User views two distinct action cards in core action stage
- **WHEN** an authenticated user navigates to `/`
- **THEN** the Action Stage renders Card A (Active Reading Hero) containing chapter title, summary, reading time, progress bar, and "Continue Reading" button
- **AND** renders Card B (Today's Practice) containing scenario dilemma title, situation teaser, and practice CTA button
- **AND** the header of Card A contains zero daily drill status or score badges.

#### Scenario: Incorrect drill submission reflects non-success status badge
- **GIVEN** a user has submitted today's daily drill with an incorrect answer resulting in `Score = 0`
- **WHEN** the user views the Home Dashboard
- **THEN** Card B renders a non-success status badge displaying `0/10` with amber/rose styling and an alert/cross icon
- **AND** does NOT render an emerald green checkmark icon or success badge.

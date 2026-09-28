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
   - **Theme-Resilient Itinerary Strip Pill Contrast:** The itinerary strip pills in Card B (`1 Core Reading`, `1 Architecture Drill` / `1 Bài đọc cốt lõi`, `1 Thử thách tình huống`) SHALL enforce theme-resilient styling (`dark:bg-white/[0.04] dark:border-white/[0.06] dark:text-slate-200` in dark mode, and `bg-slate-100 text-slate-700 border-slate-200/80` in light mode). The pills SHALL NOT employ high-opacity light-mode backgrounds on dark canvases that produce unreadable washed-out light-gray boxes with low-contrast text during route transitions, theme synchronization delays, or browser history restoration.
   - **Active Recall & Concentric Metrics:** Renders dual concentric SVG rings for daily study pace and SM-2 retention health, constrained in height to prevent stretching.
   - **7-Day Consistency Matrix:** Renders weekly completion dots and active streak flames with freeze credit indicators.
   - **Live Knowledge Graph Breakdown (Card E):** In the right telemetry dock, Card E (`DomainConstellationCard.vue`) SHALL render a dynamic personal knowledge breakdown table displaying real telemetry derived from the user's graph data (`graphStore.rawData`): total Spaced Repetition flashcards (`card` count), saved technical highlights & reflections (`highlight` count), completed document slices (`chunk` count), live connected concepts count (`TotalNodes`), semantic relations count (`TotalEdges`), and a direct link to the interactive 3D Cosmos (`/graph`). Card E SHALL NOT render hardcoded, static SVG vertices or unreactive placeholder pillar illustrations.

2. **Crash-Free Lifecycle, Store Hardening & History Revalidation:**
   - When querying Spaced Repetition deck stats, the component SHALL safely invoke valid `useReviewStore` methods (`fetchDeckCards({ pageSize: 1 })` or `fetchReviewDeck()`) and read counts from `deckStatistics` and `totalCardsDue` with defensive fallbacks, NEVER calling non-existent methods or throwing unhandled synchronous exceptions.
   - When querying Knowledge Graph data, the component SHALL read from `graphStore.rawData` with defensive fallbacks.
   - Full page refreshes (F5) and direct URL navigation to `/` SHALL render the dashboard reliably with zero uncaught runtime errors and zero redirection to `error.vue` (500 Internal Server Error).
   - **Browser History & bfcache Revalidation:** The Home Dashboard SHALL automatically revalidate daily focus and drill state upon route activation and browser history popstate / `pageshow` navigation. When returning to `/` via forward/back navigation or mouse macro buttons, the dashboard SHALL update drill status, points reward badges, and active scenario text to match the latest user progress.
   - **Optimistic In-Place Revalidation:** When cached data is already present in memory, revalidation SHALL execute in the background without clearing the rendered UI, flashing skeleton placeholders, or rendering full-page loading spinners.

3. **Zero-Scroll Single-Screen Desktop Layout Invariant:**
   - On desktop screens ($\ge 1024\text{px}$), the dashboard container and column flexboxes SHALL be top-aligned (`justify-start`) with consistent, snug vertical gaps (`gap-3.5 sm:gap-4`), preventing cards from scattering or dispersing to the vertical extremes on tall displays while fitting entirely within the viewport (`h-[calc(100vh-3.5rem)]`) with zero required scrolling.
   - The dual-card Action Stage (Cards A & B) and the triple-card Telemetry Dock (Cards C, D & E) SHALL align vertically without large empty voids.
   - On mobile ($< 640\text{px}$) and tablet ($640\text{px} - 1023\text{px}$) viewports, the layout SHALL transition to a natural vertically scrollable stack.

4. **Global Navigation Alignment:**
   - The desktop sidebar (`AppSidebar.vue`) and mobile navigation drawer SHALL represent `/` as the primary `"Dashboard"` / `"Home"` entry and `/today` as `"Today's Focus"` / `"Focus Studio"`.
   - The command palette (`AppCommandPalette.vue`) SHALL register `/` as the primary Dashboard route.
   - The global top header (`AppHeader.vue`) SHALL render dynamic slice progress without hardcoded `/ 30` boundaries.

#### Scenario: User navigates back to Home Dashboard via browser history or mouse forward/back buttons
- **GIVEN** a user completes a daily drill or updates their study progress on `/today`
- **WHEN** the user navigates back to `/` using browser back/forward buttons or mouse macro keys
- **THEN** the system automatically triggers background revalidation of today's focus data
- **AND** updates Card B drill status badge to reflect completion without requiring a full page refresh
- **AND** maintains current dashboard layout without displaying full-screen loading spinners.

#### Scenario: Itinerary strip pills render high contrast across theme states
- **WHEN** Card B is rendered on the Home Dashboard in dark mode or light mode
- **THEN** the itinerary strip pills render with high-contrast text against their container background
- **AND** do NOT render washed-out light-gray backgrounds with matching light-gray text during transitions or bfcache restorations.

#### Scenario: Background revalidation preserves existing UI without layout shift
- **GIVEN** the dashboard is already displaying cached focus data
- **WHEN** background revalidation is executed
- **THEN** existing cards and metrics remain rendered in place
- **AND** update seamlessly once the revalidation response completes.

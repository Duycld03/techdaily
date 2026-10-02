# today-reader Specification

## Purpose
Provides shared reading typography management, reactive font and layout scaling, and cross-view preference synchronization between the daily focus reading pane (/today) and dedicated reader (/read/[bookId]).

## Requirements

### Requirement: Shared Reactive Typography Composable
The frontend application SHALL provide a centralized reactive typography composable `useReaderTypography()` located at `frontend/composables/useReaderTypography.ts`.

The composable SHALL:
1. **Reactive State Management:** Manage reactive, singleton-backed or shared state for:
   - `fontSize`: `'sm'` (14px / 85%), `'base'` (16px / 100%, default), `'lg'` (18px / 115%), `'xl'` (20px / 130%), `'2xl'` (22px / 145%).
   - `fontFamily`: `'sans'` (Inter, system-ui, default), `'serif'` (Merriweather, Georgia, serif), `'mono'` (JetBrains Mono, monospace).
   - `lineSpacing`: `'normal'` (1.5), `'relaxed'` (1.75, default), `'loose'` (2.05).
   - `readingWidth`: `'standard'` (`max-w-3xl`, default), `'wide'` (`max-w-4xl`), `'full'` (`max-w-full`).
2. **Stepped Font Size Helpers:** Provide stepped decrement (`decreaseFontSize`) and increment (`increaseFontSize`) helper functions with boundary bounds checking (`canDecreaseFontSize` and `canIncreaseFontSize`).
3. **Computed Typography Helpers:** Provide computed mappings for direct template binding:
   - `fontSizePx`: Returns active font size in pixel units (e.g. `'16px'`).
   - `lineHeightValue`: Returns active line height unitless string (e.g. `'1.75'`).
   - `fontFamilyClass`: Returns Tailwind font family class (`font-sans`, `font-serif`, `font-mono`).
   - `readingWidthClass`: Returns Tailwind max-width class (`max-w-3xl`, `max-w-4xl`, `max-w-full`).
   - `fontScalePercentages`: Maps size keys to scale percentages (`85%`, `100%`, `115%`, `130%`, `145%`).
4. **LocalStorage Persistence:** Automatically serialize and persist typography state to browser `localStorage` under key `techdaily_reader_typography` whenever any property changes.
5. **Safe Hydration:** Hydrate saved preferences on client-side mount, safely validating property values and falling back to defaults (`{ fontSize: 'base', fontFamily: 'sans', lineSpacing: 'relaxed', readingWidth: 'standard' }`) if storage is empty or contains malformed JSON.
6. **Cross-View Synchronization:** Ensure updating typography settings in either `/read/[bookId]` or `/today` updates the shared state and persists to `localStorage`, synchronizing reading preferences across all reader surfaces.

#### Scenario: User modifies typography via shared composable
- **GIVEN** a component consuming `useReaderTypography()`
- **WHEN** user invokes `increaseFontSize()` or sets `fontFamily = 'serif'`
- **THEN** reactive typography state updates immediately
- **AND** the new configuration is persisted to `localStorage.getItem('techdaily_reader_typography')`.

#### Scenario: Composable hydration from localStorage
- **GIVEN** a browser session with previously saved typography preferences `{ fontSize: 'lg', fontFamily: 'serif', lineSpacing: 'loose', readingWidth: 'wide' }`
- **WHEN** any reading view mounts and invokes `useReaderTypography()`
- **THEN** the composable initializes with the saved preferences rather than defaults.

---

### Requirement: Today Reading Pane Typography Controls
The daily reader pane (`frontend/components/today/DocReaderPane.vue`) on `/today` SHALL integrate a sleek `Aa` typography settings button and popover dropdown into its header, providing immediate in-place reading customization.

The typography interface in `DocReaderPane.vue` SHALL:
1. **Header Action Button:** Render a compact `Aa` button in the pane header with visual active-state toggle styling (`bg-brand-600 text-white` when open; neutral card styling when closed).
2. **Popover Dropdown:** Display a floating popover on click, containing:
   - **Font Size:** Stepped `A-` / `A+` controls with an active percentage indicator (85% to 145%) and visual indicator dots across the 5 size steps.
   - **Font Family:** Toggle buttons for Sans-Serif, Serif, and Monospace.
   - **Line Spacing:** Toggle buttons for Normal (1.5), Relaxed (1.75), and Loose (2.05).
3. **Dynamic Style & Class Binding:** Bind dynamic `:style="{ fontSize: fontSizePx, lineHeight: lineHeightValue }"` and `:class="[fontFamilyClass]"` to the reading content container (`.doc-reader-content`).
4. **Deep CSS Typography Inheritance:** Enforce complete typography inheritance across rendered markdown elements through scoped CSS:
   ```css
   :deep(.markdown-body p),
   :deep(.markdown-body li) {
     font-size: inherit !important;
     line-height: inherit !important;
   }
   ```
   preventing default markdown stylesheet rules from overriding user-selected font size and line height.
5. **Dismissal Handling:** Close the popover automatically when the user clicks outside the dropdown container or presses `Escape`.
6. **Cross-View Parity:** Persist settings through `useReaderTypography()` so preferences remain identical between `/read/[bookId]` and `/today`.

#### Scenario: User toggles typography popover in DocReaderPane
- **GIVEN** an authenticated user viewing the daily reading pane on `/today`
- **WHEN** user clicks the `Aa` button in the header of `DocReaderPane.vue`
- **THEN** a sleek typography settings popover opens showing font size, font family, and line spacing controls.

#### Scenario: User scales font size in DocReaderPane
- **GIVEN** the typography popover is open on `/today` with default font size `base` (16px / 100%)
- **WHEN** user clicks the `A+` button
- **THEN** the active font size advances to `lg` (18px / 115%)
- **AND** text in `.doc-reader-content`, including all `<p>` and `<li>` elements, immediately enlarges to 18px
- **AND** the updated size is saved to `localStorage`.

#### Scenario: User selects serif font family for narrative reading in DocReaderPane
- **GIVEN** the daily reader pane displaying technical narrative text
- **WHEN** user clicks the "Serif" font family button in the `Aa` popover
- **THEN** `.doc-reader-content` dynamically receives the `font-serif` class
- **AND** all rendered paragraphs and headings display in literary serif typography.

#### Scenario: User adjusts line spacing for relaxed reading in DocReaderPane
- **GIVEN** the daily reader pane on `/today`
- **WHEN** user selects "Loose" line spacing
- **THEN** line height on `.doc-reader-content` and child `<p>` / `<li>` elements expands to 2.05
- **AND** vertical spacing between lines expands smoothly without layout clipping.

#### Scenario: Cross-view synchronization between /today and /read/[bookId]
- **GIVEN** a user configures font size `xl` and font family `mono` while reading on `/today`
- **WHEN** the user subsequently navigates to `/read/[bookId]`
- **THEN** the dedicated book reader automatically applies font size `xl` and monospace typography
- **AND** any adjustments made in `/read/[bookId]` are instantly reflected when returning to `/today`.

### Requirement: Focus Studio 3-Column IDE Layout & Responsive Panels
The Focus Studio reading workspace (`frontend/pages/today.vue`) SHALL implement a 3-column IDE Concept Studio layout uniting document navigation, distraction-free reading, and architectural problem-solving in a single cohesive workspace.

1. **Workspace Architecture:**
   - **Left Rail (Outline & Slice Navigator):** Collapsible sidebar displaying active curriculum chapters/slices, progress percentages, completion status checkmarks, and duration badges with Electric Violet active accents.
   - **Center Canvas:** High-contrast reading surface (`DocReaderPane.vue`), studio control bar with breadcrumb trail (`Book Title > Chapter Title > Slice N/M`), time-to-read badge, and floating selection toolbar (Highlight, Note, Explain Term).
   - **Right Dock (Scenario Studio Copilot):** Docked architectural scenario challenge pane (`InterviewChallengePane.vue`) with collapsible drawer toggle button, allowing engineers to evaluate trade-offs and solve problems side-by-side with authoritative source documentation.

2. **Responsive Panel Invariants:**
   - On wide desktop viewports ($\ge 1280\text{px}$), the workspace SHALL support simultaneous 3-column display with independent panel collapse toggles.
   - On standard desktop viewports ($1024\text{px} - 1279\text{px}$), the outline navigator SHALL collapse into a slide-over drawer by default while the reader and scenario dock share the viewport (50/50 split).
   - On mobile viewports ($< 768\text{px}$), the workspace SHALL provide a sleek segmented tab switcher (`[ 📖 Reading Slice | ⚡ Scenario Challenge ]`) with touch-optimized targets.

3. **Visual Invariants:**
   - All panels SHALL adhere to the Dev-Learning Studio theme (`canvas` `#09080e`, hairline borders `border-white/[0.08]`, and subtle ambient glows).
   - Reading typography SHALL strictly enforce the platform standard ($\ge 16\text{px}$ body text on desktop, line height $1.75$).

#### Scenario: Wide desktop 3-column studio layout
- **WHEN** an authenticated user opens `/today` on a wide screen ($\ge 1280\text{px}$)
- **THEN** the system renders the 3-column studio layout
- **AND** the outline navigator, reading markdown pane, and scenario challenge pane are simultaneously accessible.

#### Scenario: User toggles outline rail
- **WHEN** the user clicks the outline toggle button in the studio control bar
- **THEN** the left outline rail collapses smoothly with a sliding transition
- **AND** the reading canvas expands to fill the remaining horizontal space.

#### Scenario: User toggles right scenario copilot dock
- **WHEN** the user clicks the copilot drawer toggle button
- **THEN** the right scenario challenge dock collapses into a compact edge tab
- **AND** the reading canvas enters full-width distraction-free immersion mode.

### Requirement: Unified Reading Studio Workspace Density
The daily reading studio workspace at `/today` SHALL enforce consistent padding (`p-3 sm:p-4 md:p-6`) and split container spacing across desktop and mobile layouts:
1. **Dock Margin Alignment**: Spacing between the Left Rail Outline, Center Reading Canvas (`DocReaderPane.vue`), and Right Dock (`InterviewChallengePane.vue`) SHALL maintain unified gap density (`gap-4 md:gap-5 lg:gap-6`), eliminating dead whitespace margins on Windows 11 and high-DPI desktop viewports.
2. **Responsive Mobile Tab Sizing**: On mobile viewports (< 1024px), tab switchers between reading and evaluation panels SHALL use uniform touch heights (>= 44px) with `whitespace-nowrap shrink-0` badges preventing text overflow across English and Vietnamese locales.

#### Scenario: User resizes browser window between desktop and tablet
- **WHEN** viewport transitions across the 1024px breakpoint on `/today`
- **THEN** the layout cleanly switches between the 3-column studio layout and tabbed mobile panes without layout jitter, clipped borders, or unbalanced horizontal margins.

---

### Requirement: Daily Focus Audio Narration Player Integration
The daily reading pane (`DocReaderPane.vue`) on `/today` SHALL integrate the complete dual-engine Audio Narration Player (`ReaderAudioPlayer.vue`), enabling software engineers to listen to AI-curated daily focus reading slices in-place without navigating away to `/read/[bookId]`.

1. **Gating on AI Curation (`isAiFormatted`):**
   - The audio player SHALL be available and render active playback controls whenever the active slice is AI-formatted (`chunk.isAiFormatted === true`).
   - If the active slice is not yet AI-formatted (`isAiFormatted === false`), the audio playback container SHALL NOT render active playback controls, preserving the distraction-free reading experience.

2. **Dual-Engine Audio Support (Device & Cloud):**
   - **On-Device Neural TTS:** The player SHALL support client-side on-device neural synthesis via Web Worker (WASM / WebGPU), streaming synthesis with a low-latency 2-sentence pre-roll buffer, and full-slice IndexedDB audio caching.
   - **Google Cloud TTS:** The player SHALL support cloud voice synthesis via the backend API, providing female and male voice options for English (`en`) and Vietnamese (`vi`), and displaying remaining quota feedback.
   - **Controls & Speed:** The player SHALL support speed adjustment ($0.75\times$ to $2.0\times$), play/pause toggle, and seekable progress timeline.

3. **Prominent Zero-Scroll Header Placement:**
   - The audio player container SHALL be positioned directly beneath the chapter title (`h1`), preceding the summary callout and markdown deep-dive content.
   - On both desktop dual-pane and mobile stacked layouts, the player controls SHALL be immediately accessible without requiring vertical scrolling past the executive summary or key takeaways.

4. **Slice Navigation & Track Switch Reset:**
   - When the user navigates between slices (e.g. Next Slice, Previous Slice) or switches document books via the Pacer menu, the audio player SHALL immediately pause active playback and reset the audio buffer state for the incoming slice.

5. **Resource Lifecycle & Route Exit Teardown:**
   - When the user navigates away from `/today` to any other view (`/review`, `/quiz`, `/notes`), the audio playback SHALL stop immediately, revoke any active object URLs, and terminate background Web Workers to prevent audio persistence or resource leakage.

#### Scenario: User listens to AI-formatted daily focus slice on /today
- **GIVEN** an authenticated user is viewing `/today` with an AI-curated reading slice (`chunk.isAiFormatted === true`)
- **WHEN** the user inspects the daily reading pane
- **THEN** the audio narration player renders directly below the chapter title heading
- **AND** the user can toggle playback between On-Device and Cloud TTS engines and listen to the slice.

#### Scenario: Audio player omitted for uncurated slice
- **GIVEN** a reading slice that has not completed AI formatting (`chunk.isAiFormatted === false` or undefined)
- **WHEN** the user views the reading pane on `/today`
- **THEN** the audio player controls are hidden or unavailable.

#### Scenario: Switching reading slices pauses audio and resets buffer
- **GIVEN** narration is currently playing for slice $N$ on `/today`
- **WHEN** the user navigates to slice $N+1$ or switches to a different book in the Pacer menu
- **THEN** audio playback for slice $N$ halts immediately
- **AND** the player resets its loaded state, ready to load or synthesize slice $N+1$.

#### Scenario: Leaving /today disposes audio playback and background workers
- **GIVEN** narration audio is actively playing or synthesizing on `/today`
- **WHEN** the user navigates to another page (such as `/review` or `/library`)
- **THEN** audio playback ceases immediately
- **AND** the synthesis Web Worker and media object URLs are disposed of cleanly.

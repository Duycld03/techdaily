## ADDED Requirements

### Requirement: Bento Grid Dashboard & Concentric Learning Metrics
The `/today` page SHALL provide a modular **Bento Grid Dashboard** mode alongside the existing split-pane focus studio, presenting the software engineer with an executive overview of daily learning momentum, spaced repetition memory retention, and upcoming curriculum milestones.

1. **Bento Grid Architecture & Cards:**
   - **Welcome & Orientation Banner:** Personalized greeting (`"Welcome Back, {Name}"`), active curriculum day badge (`"Day {N} / 30"`), and a primary `"Ask AI Explainer"` CTA button opening `TermExplainerModal.vue`.
   - **Today's Focus Bento Hero Card:** Displays active book cover badge, curriculum slice title, estimated reading duration (e.g. `"4 min read"`), progress bar with percentage, key concept chips, and a prominent `"Continue Reading (Enter) ➔"` button.
   - **Concentric Metric Card (`ConcentricMetricCard.vue`):** Pure SVG dual concentric ring visualization:
     - **Outer Ring (Daily Study Goal Pace):** Renders progress toward daily target minutes (e.g. $10\text{m}$ goal) with Electric Violet stroke (`#8b5cf6`).
     - **Inner Ring (SM-2 Spaced Repetition Retention Health):** Renders the ratio of mastered/retained cards to total active deck cards with Cyber Cyan stroke (`#06b6d4`).
     - Center readout displaying overall retention percentage or active card summary with clear contextual labels.
   - **Senior Scenario Drill Bento Card:** Previews the day's architectural interview challenge, potential score rewards (`+10 Pts`), and a quick CTA to launch the drill interface.
   - **7-Day Consistency Matrix:** Renders a 7-day dot/pillar matrix for the current week displaying daily study duration, active streak flame indicator, and freeze protection credit count.
   - **Knowledge Graph Radar Card:** Displays total connected concept count (nodes) and relationship count (edges) with an interactive shortcut jumping directly to the Knowledge Graph 3D Cosmos.

2. **Dual View Mode Toggling & Persistence:**
   - The `/today` header SHALL provide a segmented view mode toggle allowing the user to switch between **Dashboard View** (`bento`) and **Focus Studio View** (`split`).
   - The active view mode preference SHALL persist in `localStorage` under key `techdaily_today_view_mode`.
   - Clicking `"Continue Reading"` or `"Solve Challenge"` on the Bento Dashboard SHALL automatically transition the view into the active reading or challenge interface without page reload.

3. **Responsive Typography & Spacing Invariants:**
   - The Bento Grid SHALL format as a 3-column asymmetric layout on desktop ($\ge 1024\text{px}$), 2-column balanced grid on tablet ($640\text{px} - 1023\text{px}$), and single-column vertical stack on mobile ($< 640\text{px}$).
   - All card typography SHALL strictly enforce repository invariants (body text $\ge 14\text{px}$ on mobile, $\ge 16\text{px}$ on desktop). All action buttons and badges SHALL use `whitespace-nowrap shrink-0` to guarantee bilingual text integrity across English and Vietnamese locales.

#### Scenario: User views /today in default Bento Dashboard mode
- **WHEN** an authenticated user navigates to `/today` with no prior view mode override
- **THEN** the system renders the Bento Grid Dashboard layout
- **AND** the Welcome Banner displays the user's name and active curriculum Day
- **AND** the Today's Focus Hero Card displays the active reading slice with progress bar
- **AND** the Concentric Metric Card renders animated SVG rings for Daily Goal and SM-2 Retention.

#### Scenario: Concentric ring visualization rendering
- **WHEN** the user views the Concentric Metric Card on the dashboard
- **THEN** the outer SVG circle displays stroke-dashoffset proportional to the daily study minutes versus daily goal
- **AND** the inner SVG circle displays stroke-dashoffset proportional to SM-2 mastered cards versus total deck
- **AND** the center text displays the calculated retention health percentage.

#### Scenario: User switches between Dashboard and Focus Studio view modes
- **WHEN** the user clicks the "Focus Studio" view mode toggle in the header
- **THEN** the Bento Grid unmounts smoothly
- **AND** the split-pane DocReaderPane and InterviewChallengePane appear
- **AND** the view choice is persisted in `localStorage` (`techdaily_today_view_mode: 'split'`)
- **WHEN** the user reloads the page
- **THEN** the Focus Studio view remains active.

#### Scenario: User clicks Continue Reading on Bento Focus card
- **WHEN** the user clicks the "Continue Reading" button on the Today's Focus Bento Hero card
- **THEN** the view mode immediately flips to Focus Studio (or navigates to the active reading slice)
- **AND** the reader pane scrolls smoothly to the active reading position.

#### Scenario: Responsive Bento presentation on mobile viewport
- **WHEN** a user opens `/today` on a mobile device ($< 640\text{px}$)
- **THEN** the Bento cards stack in a single vertical column
- **AND** all action buttons and text elements maintain comfortable touch targets ($\ge 44\text{px}$)
- **AND** no horizontal scrolling occurs.

## Why

TechDaily's main landing experience (`/today` and `/`) currently drops users directly into a rigid two-pane layout split between `DocReaderPane` and `InterviewChallengePane`. While functional for focused reading, it lacks an executive learning dashboard overview that establishes daily momentum, highlights spaced repetition retention health, and gives engineers a comprehensive pulse of their progress.

Modern learning hubs and developer productivity tools (such as the benchmarks explored in Image #1 and Image #3) achieve superior user engagement through structured **Bento Grid Dashboards**:
1. **Welcome Banner & AI Interaction (Image #1):** A personalized header greeting engineers with their active curriculum day (e.g. Day 14 / 30), career role goal, and an immediate **"Ask AI Explainer"** quick-action button.
2. **Concentric Ring Retention Metrics (Image #3):** A clean dual concentric SVG ring chart visualizing:
   - Outer Ring: Daily Study Pace (e.g. 12/10 minutes completed toward daily goal).
   - Inner Ring: SM-2 Spaced Repetition Retention Health (e.g. 88% retention rate across active flashcards).
3. **Today's Focus Bento Hero:** A prominent card summarizing the day's curriculum slice, estimated reading duration, key concepts, and a primary CTA to launch the focused reading view.
4. **Senior Scenario Drill Card:** An interactive teaser for the day's architectural challenge with instant action trigger.
5. **7-Day Consistency Matrix (Image #1):** A gamified 7-day study history tracking daily active minutes, streak preservation status, and freeze credits.
6. **Knowledge Graph Telemetry (Image #5):** A high-tech radar card displaying total connected architectural concepts and relationships with a quick leap to the 3D Cosmos.

This change transforms `/today` into a dynamic Bento Dashboard while preserving one-click toggling into the full split-pane reading mode, giving engineers both high-level clarity and distraction-free deep work.

## What Changes

- **Bento Grid Dashboard Layout on `/today` (`frontend/components/today/TodayBentoDashboard.vue`):**
  - Modular responsive grid (2-column on desktop/tablet, single-column stack on mobile).
  - Welcome Banner with dynamic greeting, active Day counter, and quick trigger for `TermExplainerModal`.
  - Today's Focus Hero Card featuring document book badge, slice title, reading progress, and "Continue Reading" CTA.
  - Concentric Ring Metric Card (`ConcentricMetricCard.vue`) rendering dual animated SVG rings for Daily Goal and SM-2 Retention Rate.
  - Senior Scenario Drill Bento Card displaying the challenge teaser, points reward (+10 Pts), and instant solve trigger.
  - 7-Day Consistency Matrix rendering dot pillars for Monday–Sunday with study duration and streak indicators.
  - Knowledge Graph Radar Card previewing connected node count and 3D galaxy navigation link.
- **View Mode Switching on `/today` (`frontend/pages/today.vue`):**
  - Add a view mode switcher in the page header allowing users to toggle between **Dashboard View** (Bento Grid) and **Focus Studio View** (Split-Pane Reader & Challenge).
  - Persist the selected view mode in `localStorage` (`techdaily_today_view_mode: 'bento' | 'split'`).
- **Localization Updates:**
  - Add bilingual i18n keys for Bento dashboard titles, metrics, concentric ring tooltips, and consistency labels in `en.json` and `vi.json`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `today`: Extend `/today` with the Bento Grid Dashboard mode, concentric retention and pace metrics, 7-day consistency tracking, and persistent view mode toggling.

## Impact

- **Frontend Application:** New components `TodayBentoDashboard.vue` and `ConcentricMetricCard.vue`; enhanced `today.vue` container with view switcher.
- **User Experience:** Dramatically increased visual polish, clear daily orientation, and instant access to flashcard reviews and AI explanations.
- **API & Backend:** Zero backend modifications. Fully backed by existing endpoints `GET /api/v1/daily/today`, `GET /api/v1/review/forecast`, and `GET /api/v1/user/profile`.
- **Database:** Zero migrations or schema changes.

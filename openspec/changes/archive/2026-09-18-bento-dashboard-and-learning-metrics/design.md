## Context

This change implements Phase 2 of the UI Modernization roadmap: transforming `/today` into a **Bento Grid Dashboard** synthesizing visual and structural patterns from Image #1 (EdTech Learning Hub), Image #3 (Concentric Ring Chart Metrics), and Image #5 (Cyber Radar Telemetry).

The dashboard serves as the central command center for the software engineer's daily learning routine, surfacing active reading progress, memory retention health, and upcoming challenges in a cohesive visual hierarchy.

## Goals / Non-Goals

**Goals:**
- Provide an executive Bento Grid overview on `/today` featuring Welcome Banner, Today's Focus Hero Card, Concentric Ring Retention Metrics, Scenario Challenge Teaser, 7-Day Consistency Matrix, and Knowledge Graph Radar.
- Implement a lightweight, zero-dependency pure SVG concentric dual-ring chart component (`ConcentricMetricCard.vue`).
- Implement seamless view mode toggling between **Dashboard (Bento)** and **Focus Studio (Split-Pane)**, persisting user preference in `localStorage`.
- Support full dark/light mode compatibility using Dev-Learning Studio tokens (`canvas`, `brand-500`, `hairline-border`, `glass-card`).
- Guarantee mobile and tablet responsive layouts complying with repository typography invariants (body $\ge 14\text{px}$ on mobile, $\ge 16\text{px}$ on desktop).

**Non-Goals:**
- Changing backend API contracts or introducing new database tables.
- Rewriting the internal markdown reader renderer (`DocReaderPane.vue`) or the interview grading pipeline.

## Decisions

### 1. Component Architecture & Decomposition
```
frontend/pages/today.vue (View Switcher & Controller)
 ├── View Mode: 'bento' (Default)
 │    └── TodayBentoDashboard.vue
 │         ├── WelcomeBanner (Greeting, Day N/30 pill, Ask AI CTA)
 │         ├── TodayFocusCard (Active Book, Slice Title, Read CTA, Progress Bar)
 │         ├── ConcentricMetricCard.vue (SVG Dual Rings: Goal & Retention)
 │         ├── ScenarioDrillBentoCard (Scenario question preview, Points badge, Solve CTA)
 │         ├── ConsistencyMatrixCard (7-day activity dots, minutes, streak status)
 │         └── KnowledgeRadarCard (Graph node/edge telemetry, 3D Galaxy leap)
 └── View Mode: 'split' (Legacy/Focused Mode)
      ├── DocReaderPane.vue
      └── InterviewChallengePane.vue
```

### 2. Concentric Ring Math & SVG Architecture (Image #3)
- **Decision**: Construct a native SVG component without external chart libraries (`Chart.js` or `D3`):
  - **Outer Ring (Daily Study Goal):**
    - Radius $R_1 = 54\text{px}$, Circumference $C_1 = 2\pi R_1 \approx 339.29\text{px}$.
    - Stroke width: $8\text{px}$.
    - Stroke color: Brand Electric Violet (`#8b5cf6`).
    - Progress ratio: $\min(1.0, \text{actualMinutes} / \text{dailyGoalMinutes})$.
  - **Inner Ring (SM-2 Spaced Repetition Retention Rate):**
    - Radius $R_2 = 40\text{px}$, Circumference $C_2 = 2\pi R_2 \approx 251.33\text{px}$.
    - Stroke width: $8\text{px}$.
    - Stroke color: Cyber Cyan (`#06b6d4`) or Emerald (`#10b981`).
    - Progress ratio: $\text{masteredCards} / \max(1, \text{totalCards})$.
  - Center readout: Percentage or summary count with label.
  - Interactive tooltips explaining outer ring (Daily Goal Pace) and inner ring (Memory Retention Health).

### 3. Responsive Layout Grid
- **Desktop ($\ge 1024\text{px}$):**
  - Left column ($65\%$ width): Welcome Banner, Today's Focus Hero Card (large), Scenario Drill Card.
  - Right column ($35\%$ width): Concentric Retention Metric Card, 7-Day Consistency Matrix, Knowledge Radar Card.
- **Tablet ($640\text{px} - 1023\text{px}$):**
  - 2-column balanced grid with cards wrapping naturally.
- **Mobile ($< 640\text{px}$):**
  - Single-column vertical stack with generous touch targets ($\ge 44\text{px}$) and clear sectional spacing.

### 4. View Mode Persistence & Seamless Transitions
- View mode state managed by composable `useTodayViewMode()`:
  - Key: `techdaily_today_view_mode`.
  - Allowed values: `'bento' | 'split'`. Default: `'bento'`.
- Top header includes a clean segmented view toggle `[ ⊞ Dashboard | ◫ Focus Studio ]`.
- Clicking "Continue Reading" on the Focus Card automatically flips the view mode to `split` or navigates into the reading studio.

## Risks / Trade-offs

- **[Risk]** Slow initial data loading if multiple dashboard metrics depend on asynchronous requests.
  → **Mitigation**: Reuse existing Pinia store caches (`useDailyFocusStore`, `useReviewStore`, `useKnowledgeGraphStore`). If review or graph stores have not yet fetched data, trigger lazy non-blocking background fetches while displaying skeleton placeholders.
- **[Risk]** Layout overcrowding on small tablet screens.
  → **Mitigation**: Apply flexible Bento grid column spans (`col-span-1 md:col-span-2 lg:col-span-3`) and ensure card footers use `whitespace-nowrap shrink-0` per project invariants.

## Migration & Verification Plan

1. Implement `frontend/composables/useTodayViewMode.ts` with `localStorage` persistence.
2. Build `frontend/components/today/ConcentricMetricCard.vue` with pure SVG math and test-ready props.
3. Build `frontend/components/today/TodayBentoDashboard.vue` with all 6 modular cards.
4. Integrate view switcher into `frontend/pages/today.vue`.
5. Add unit tests for `ConcentricMetricCard.vue` and `useTodayViewMode.ts`.
6. Verify layout with `npm test` and run responsive E2E smoke tests across viewports.

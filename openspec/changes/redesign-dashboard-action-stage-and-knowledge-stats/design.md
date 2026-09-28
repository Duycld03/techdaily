# Design

## Context

See `proposal.md` for motivation.
The Home Command Center Dashboard (`frontend/pages/index.vue`) renders `HomeBentoDashboard.vue` hosted inside `BentoDashboardLayout.vue`.
The layout splits the desktop screen into:
- 2 columns for `action-stage` (Left)
- 1 column for `telemetry-dock` (Right)

Currently, `telemetry-dock` contains 3 cards (`ConcentricMetricCard`, 7-day consistency card, and `DomainConstellationCard`), with a cumulative height of ~580–620px. Meanwhile, `action-stage` was collapsed into a single `h-full justify-between` card that only holds the reading slice title, summary, and progress bar, creating a ~350px empty void and hosting a misplaced drill status badge that erroneously displays green `[✓] Completed: 0/10`. Furthermore, `DomainConstellationCard` renders a hardcoded 5-node static SVG that does not reflect user data.

## Goals / Non-Goals

**Goals:**
- Split `action-stage` into two balanced, high-utility cards:
  - Card A: Active Reading Hero (Slice details, read time, progress, "Continue Reading →").
  - Card B: Today's Practice Session Cockpit (Scenario dilemma title, situation teaser, reward/status, "Solve Challenge →").
- Correctly decouple reading metadata from drill completion metrics: remove drill badge from Card A.
- Enforce accurate semantic styling for daily drill badges on Card B based on submission state and correctness (`Score > 0` vs `Score == 0`).
- Replace the static SVG constellation in `DomainConstellationCard.vue` with a live breakdown of user knowledge artifacts (SM-2 cards, saved highlights, completed slices, connected concepts and relations).

**Non-Goals:**
- Altering the backend `/api/v1/today` or `/api/v1/graph` schemas or endpoints (all necessary data is already available in existing DTOs).
- Altering the 3D Cosmos viewer (`frontend/pages/graph.vue`).
- Redesigning the right-column concentric metric rings or 7-day consistency calendar.

## Decisions

### 1. Two-Card Action Stage Architecture

```
+-------------------------------------------------------------------------+
| CARD A: ACTIVE READING HERO                                             |
| [Book Icon] ASP.NET Core • Slice 1 / 1320              [4 min read]     |
| ASP.NET Core documentation                                              |
| An overview of ASP.NET Core documentation covering diverse...           |
| Progress (1/1320)  [======-----------------] 0%                         |
| ----------------------------------------------------------------------- |
| [ Continue Reading -> ]                                                 |
+-------------------------------------------------------------------------+
| CARD B: TODAY'S PRACTICE SESSION COCKPIT                                |
| [Target Icon] Today's Practice • Day 1      [+10 Points / Score Badge]  |
| Senior Dilemma: Microservice Distributed Transaction Rollback           |
| When managing distributed sagas across order and payment services...    |
| ----------------------------------------------------------------------- |
| [ Solve Challenge -> ] / [ Review Solution -> ]                         |
+-------------------------------------------------------------------------+
```

- **Rationale**: Daily learning in TechDaily centers on two complementary actions: ingesting technical knowledge (Reading) and applying it to senior-level architectural challenges (Scenario Drill). Having two dedicated cards fills the vertical height naturally without artificial flex stretching.
- **Card A Height & Spacing**: Card A occupies ~270px, providing space for title, summary, progress bar, and primary reading CTA.
- **Card B Height & Spacing**: Card B occupies ~270px, providing space for scenario dilemma title, situation excerpt, reward badge, and practice CTA.
- **Zero Empty Void**: Stacked with `gap-3.5 sm:gap-4`, the left column total height (~560–580px) aligns snugly with the right telemetry dock (~580px), eliminating the empty void entirely.

### 2. Semantic Drill Status Badge Mapping

On Card B, the status indicator is mapped as follows:
- **Pending / Unsubmitted** (`status === 0 || status === 'Pending'` and `!drill?.submittedAt`):
  - Badge: Amber `+10 Points` / `+10 Điểm thưởng`.
  - Icon: `Target` (amber).
  - Styling: `bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20`.
  - CTA Button: `"Solve Challenge →"` / `"Bắt đầu thử thách →"`.
- **Completed Correctly** (`drill?.isCorrect === true` or `drillScore > 0`):
  - Badge: Emerald `✓ Completed: 10/10` / `✓ Hoàn thành: 10/10`.
  - Icon: `CheckCircle2` (emerald).
  - Styling: `bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20`.
  - CTA Button: `"Review Solution →"` / `"Xem lại bài giải →"`.
- **Completed Incorrectly** (`drill?.isCorrect === false` and `drillScore === 0` and drill submitted):
  - Badge: Rose/Amber `✗ Needs Review: 0/10` / `✗ Chưa đạt: 0/10`.
  - Icon: `AlertCircle` (rose/amber).
  - Styling: `bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-500/20`.
  - CTA Button: `"Review Solution →"` / `"Xem lại bài giải →"`.

### 3. Live Knowledge Breakdown Widget (`DomainConstellationCard.vue`)

Replace the hardcoded SVG vertices with a clean telemetry breakdown table:
- **Header**: Keeps `Network` icon, title `Domain Constellation` / `Chòm sao tri thức`, and direct link `Open 3D Cosmos →` (`/graph`).
- **Body**: A structured telemetry list reading from `graphStore.rawData`:
  - `SM-2 Flashcards`: Total cards from `graphStore.rawData.stats.nodeTypeCounts['card']` or `reviewStore.deckStatistics.totalCards`.
  - `Saved Highlights & Notes`: Count from `graphStore.rawData.stats.nodeTypeCounts['highlight']`.
  - `Learned Slices`: Count from `graphStore.rawData.stats.nodeTypeCounts['chunk']`.
- **Footer**: Retains the two summary pill counters:
  - `[ TotalNodes ] Connected Concepts` (`Khái niệm liên kết`).
  - `[ TotalEdges ] Active Relations` (`Mối quan hệ`).

## Risks / Trade-offs

- **Risk**: Store data loading timing (e.g. `graphStore.rawData` is null on first render before `fetchGraph` resolves).
  - **Mitigation**: Implement defensive fallbacks (`graphStore.rawData?.stats?.nodeTypeCounts?.['card'] ?? 0`, with skeleton or placeholder numbers).
- **Risk**: On very small screens (mobile portrait), stacking two left cards plus three right cards creates a 5-card vertical stream.
  - **Mitigation**: BentoDashboardLayout already implements `grid-cols-1 lg:grid-cols-3` which naturally flows into a single column on mobile, with touch targets $\ge 44\text{px}$ and tight `gap-3.5` spacing.

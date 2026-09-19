# Proposal: Compact and Structure Insights Benchmark Telemetry Stats

## Why

In `/insights` (`frontend/pages/insights.vue`, as captured in user inspection [Image #1]), the benchmark telemetry badge currently renders long, unformatted raw strings (e.g. `⚡ Latency giảm từ 4.2ms xuống 0.3ms (14x faster) | Heap Fetches giảm từ 15,000 xuống 0 trên 1 triệu rows`) inside a single inline container placed in the card header row.

This creates several notable visual defects:
1. **Duplicate Lightning Bolt Icons**: Both the seed/AI database string contains a leading `⚡` emoji, and the Vue template renders a Lucide `<Zap>` icon component, causing two lightning bolts to appear side-by-side.
2. **Vertical Misalignment on Multi-Line Wrap**: Because the container uses `inline-flex items-center`, when lengthy benchmark text wraps to 2 or 3 lines, the Lucide `<Zap>` icon is pushed down to line 2 (vertically centered), while the raw `⚡` emoji stays on line 1, creating an awkward staggered effect.
3. **Cluttered Multi-Metric Blob**: When an insight contains multiple benchmarks joined by pipe delimiters (`|`), cramming them into a single header badge causes excessive text wrapping, pushing against the bookmark button and breaking responsive layout symmetry on mobile and desktop viewports.

Structuring benchmark telemetry into clean, distinct metric chips and stripping redundant emojis restores visual polish and enhances readability.

## What Changes

- **Benchmark Stats Parsing & Cleaning**:
  - In `frontend/pages/insights.vue`, add a computed property `parsedBenchmarkStats` that splits `currentInsight.benchmarkStats` on `|` delimiters.
  - Strip leading emojis (`⚡`, `🔥`, `🚀`) and excess whitespace from each metric string, preventing duplicate icons.
- **Dedicated Metric Chips Presentation**:
  - Render each benchmark metric as its own compact, standalone chip with a single Lucide `<Zap class="w-3.5 h-3.5 fill-brand-500 text-brand-500 shrink-0" />` icon and Dev-Learning Studio brand violet tokens (`bg-brand-50/80 dark:bg-brand-950/40 border border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-300`).
  - Position benchmark metrics cleanly as a dedicated metric row (below the title or below tags) so the top card header row (Category + Tags on the left, Bookmark on the right) remains balanced and uncluttered across all screen sizes.
- **Zero Breaking Changes**: Purely frontend presentation and layout optimization; no API, database, or schema modifications.

## Capabilities

### New Capabilities
<!-- None: Refinement of existing insights presentation capability -->

### Modified Capabilities
- `insights`: Update requirement for Glass Insight Card Reader to mandate structured benchmark telemetry display with emoji deduplication and compact individual metric chips.

## Impact

- Affected files:
  - `frontend/pages/insights.vue`
  - `frontend/tests/pages/insights.spec.ts`

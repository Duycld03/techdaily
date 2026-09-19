# Design: Compact and Structure Insights Benchmark Telemetry Stats

## Context

In `frontend/pages/insights.vue`, the technical insight card displays performance telemetry stored in `currentInsight.benchmarkStats`.

Currently:
1. `benchmarkStats` is rendered inside a single `inline-flex` badge located in the top-right header row alongside the bookmark button.
2. The data strings frequently begin with a `⚡` emoji and may contain multiple distinct benchmarks delimited by pipe characters (`|`) (e.g. `⚡ Latency giảm từ 4.2ms xuống 0.3ms (14x faster) | Heap Fetches giảm từ 15,000 xuống 0 trên 1 triệu rows`).
3. The badge template contains its own Lucide `<Zap>` icon component, resulting in duplicate side-by-side lightning bolts.
4. When long text wraps to multiple lines, `items-center` on the container pulls the Lucide icon down to line 2, while the raw emoji remains on line 1.
5. In responsive mobile views, the badge stretches across the top row and pushes against the bookmark button.

See `proposal.md` for complete user motivation and problem statement.

## Goals / Non-Goals

**Goals:**
- Add a computed property `parsedBenchmarkStats` in `frontend/pages/insights.vue` that splits `benchmarkStats` on `|`, strips leading emojis/symbols (`⚡`, `🔥`, `🚀`), and returns an array of clean metric strings.
- Render each metric as its own compact, standalone chip with a single Lucide `<Zap class="w-3.5 h-3.5 fill-brand-500 text-brand-500 shrink-0" />` icon and Dev-Learning Studio brand violet styling.
- Reposition benchmark telemetry into a clean, dedicated horizontal flex-wrap container directly beneath the insight title (or beneath tags), keeping the top card header row (Category + Tags on the left, Bookmark on the right) balanced and uncluttered.
- Ensure graceful wrapping across mobile (<640px) and desktop viewports without layout shifts or text collisions.
- Update unit tests in `frontend/tests/pages/insights.spec.ts` to assert correct parsing, emoji deduplication, and chip rendering.

**Non-Goals:**
- No backend database schema or API contract changes to `TechInsight.cs` or `BenchmarkStats` properties.
- No changes to AI generation prompts in `GeminiAiService.cs`.

## Decisions

### Decision 1: Computed Telemetry Parser & Emoji Normalization
- **Approach**: Add `parsedBenchmarkStats` to `insights.vue`:
  ```typescript
  const parsedBenchmarkStats = computed<string[]>(() => {
    const raw = insightsStore.currentInsight?.benchmarkStats
    if (!raw) return []
    return raw
      .split('|')
      .map(item => item.trim().replace(/^[\s⚡🔥🚀]+/, '').trim())
      .filter(item => item.length > 0)
  })
  ```
- **Rationale**: Stripping leading emojis ensures that only the Vue-rendered Lucide `<Zap>` icon is visible, eliminating duplicate icons. Splitting by `|` isolates multiple performance findings (e.g. Latency reduction and Heap Fetch elimination) into independent, easily readable chips.

### Decision 2: Dedicated Benchmark Metric Row Placement
- **Approach**: Restructure the top area of the insight card in `insights.vue`:
  - **Header Row**: Left side contains Category Badge and topic tags (`#PostgreSQL`, `#IndexOnlyScan`); right side contains only the Bookmark button (`shrink-0`).
  - **Title Row**: `<h2>` displaying `currentInsight.title`.
  - **Benchmark Telemetry Row**: Directly below the title:
    ```html
    <div v-if="parsedBenchmarkStats.length" class="flex flex-wrap items-center gap-2 pt-1">
      <div
        v-for="(stat, idx) in parsedBenchmarkStats"
        :key="idx"
        class="inline-flex items-center gap-1.5 px-3 py-1 rounded-xl bg-brand-50/80 dark:bg-brand-950/40 border border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-300 text-xs sm:text-sm font-semibold shadow-sm"
      >
        <Zap class="w-3.5 h-3.5 fill-brand-500 text-brand-500 shrink-0" />
        <span>{{ stat }}</span>
      </div>
    </div>
    ```
  - **Summary Row**: Markdown summary content.
- **Rationale**: Moving the benchmark metrics below the title allows each metric chip to render legibly at natural width without competing with the bookmark button or forcing premature multi-line text wrapping.

## Risks / Trade-offs

- **Risk**: Insights with a single short metric (e.g. `⚡ 10x faster`).
- **Mitigation**: `split('|')` on a single metric returns an array of length 1, rendering a single tidy pill badge beneath the title.

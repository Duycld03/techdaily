# Tasks

## 1. Frontend Benchmark Telemetry Parsing & Layout

- [x] 1.1 Add computed property `parsedBenchmarkStats` in `frontend/pages/insights.vue` to split `benchmarkStats` by `|` and strip leading emojis (`⚡`, `🔥`, `🚀`) and excess whitespace
- [x] 1.2 Reposition benchmark telemetry presentation in `frontend/pages/insights.vue` as a dedicated horizontal flex-wrap row below the insight title
- [x] 1.3 Render each parsed metric as an individual chip with a single Lucide `<Zap>` icon and Dev-Learning Studio brand violet styling (`bg-brand-50/80 dark:bg-brand-950/40 border-brand-200/80 dark:border-brand-500/20 text-brand-700 dark:text-brand-300`)
- [x] 1.4 Simplify top card header row in `frontend/pages/insights.vue` so category and tags align left and bookmark button aligns right without crowding

## 2. Automated Testing & Verification

- [x] 2.1 Update `frontend/tests/pages/insights.spec.ts` to assert `parsedBenchmarkStats` splits multi-metric strings on `|`, strips leading emojis, and renders individual metric chips
- [x] 2.2 Run frontend unit tests (`npm test`) and verify 100% passing test suites

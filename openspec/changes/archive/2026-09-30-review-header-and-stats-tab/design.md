# Design

## Context

See `proposal.md` - Why.
In `frontend/pages/review.vue`, the top-level navigation currently consists of an isolated tab bar constrained to `max-w-5xl`, lacking the page title, icon badge, and descriptive subtitle standardized across `/quiz` and `/insights`. To its right sits an empty void.

Below the header, the `management` tab (`activeTab === 'management'`) hosts both the 6-card analytics bento dashboard and the flashcard inventory. On a 1080p desktop display, these 6 stat cards consume nearly 500px of vertical space, pushing the flashcard cards below the fold.

Additionally, both `frontend/components/review/AtRiskLeechCard.vue` and `frontend/components/review/EaseFactorDistributionCard.vue` display the identical Vietnamese label "Khó nhớ" (`$t('review.atrisk_leech')` and `$t('review.ease_struggling')`), creating semantic ambiguity between repeatedly failed cards (Leeches) and low-ease concepts.

## Goals / Non-Goals

**Goals:**
- Implement the standardized header archetype matching `/quiz`: `<Layers>` icon badge, primary title (`$t('review.title')`), and subtitle (`$t('review.subtitle')`) on the left, paired with the tab switcher on the right.
- Enforce consistent container sizing (`max-w-7xl mx-auto px-4 sm:px-6 py-6 space-y-6`) across both header and content.
- Decouple analytics into a dedicated Tab 3 (`activeTab === 'stats'`), allowing Tab 2 (`management`) to showcase the flashcard inventory immediately at the top of the viewport.
- Disambiguate Vietnamese retention terminology into distinct semantic terms: "Hay quên" for Leech cards and "Chưa vững" for the $[1.30, 1.70]$ ease bucket.
- Maintain full bidirectional URL synchronization across all three tabs (`?tab=session`, `?tab=management`, `?tab=stats`).

**Non-Goals:**
- No alterations to backend APIs, DTOs, or database schemas (`/api/v1/review/*`).
- No modifications to the active flashcard player (`FlashcardDeck.vue`), 3D flip animations, or SM-2 grading logic.
- No changes to card editing, deletion, or progression reset modals.

## Decisions

### 1. Standardized Header Banner Layout
Adopt the exact header layout pattern from `pages/quiz.vue`:
```html
<div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 border-b border-slate-200 dark:border-white/[0.08] pb-4">
  <!-- Left: Icon badge + Title + Subtitle -->
  <div class="flex items-center gap-3">
    <div class="w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
      <Layers class="w-5 h-5 sm:w-6 sm:h-6" :stroke-width="1.5" />
    </div>
    <div class="space-y-0.5">
      <h1 class="text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white">
        {{ $t('review.title') }}
      </h1>
      <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium">
        {{ $t('review.subtitle') }}
      </p>
    </div>
  </div>

  <!-- Right: 3-Tab Switcher Pill Container -->
  <div class="flex items-center gap-1.5 p-1 bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] rounded-2xl shrink-0 overflow-x-auto">
    <!-- Tab 1: session -->
    <!-- Tab 2: management -->
    <!-- Tab 3: stats -->
  </div>
</div>
```
This guarantees visual harmony with `/quiz` and `/insights` while eliminating the empty void to the right.

### 2. Functional Tab Division & Deck Inventory Liberation
- **Tab 1 (`session`)**: Kept unchanged. Houses the active review player or the celebratory completion hero card.
- **Tab 2 (`management`)**: Stripped of the 6 bento cards. The quick filter bar, search input (⌘K), and flashcard bento card grid render directly at the top of the viewport. The user instantly sees their flashcards upon selecting "Kho thẻ của tôi".
- **Tab 3 (`stats`)**: Dedicated analytics tab containing:
  - **Row 1 (Bento Overview)**: `FlashcardHeroCard`, `MasteryGaugeCard`, `ReviewForecastChart` in `grid-cols-1 md:grid-cols-2 xl:grid-cols-3`.
  - **Row 2 (Retention Breakdown)**: `AtRiskLeechCard`, `SourceChannelRetentionCard`, `EaseFactorDistributionCard` in `grid-cols-1 lg:grid-cols-3`.

Cross-tab interactions:
- Clicking "Bắt đầu ôn tập ngay" on `FlashcardHeroCard` switches `activeTab = 'session'`.
- Clicking "Ôn thẻ cần chú ý" on `AtRiskLeechCard` switches `activeTab = 'management'` and sets filters.

### 3. Disambiguated Memory Retention Terminology
Update `frontend/i18n/locales/vi.json`:
- `review.atrisk_leech`: "Khó nhớ" $\rightarrow$ **"Hay quên"** (reflecting repeated failed reviews in SM-2 / Anki vernacular).
- `review.ease_struggling`: "Khó nhớ" $\rightarrow$ **"Chưa vững"**.
  Together with existing keys, this creates a clean 3-tier memory stability progression:
  $$\text{Chưa vững (1.30–1.70)} \;\longrightarrow\; \text{Đang củng cố (1.70–2.10)} \;\longrightarrow\; \text{Vững vàng (2.10–2.50)}$$
- Add `review.tab_stats`: `"Thống kê"` (vi) / `"Analytics"` (en).

### 4. URL State Synchronization Across 3 Tabs
Extend the activeTab watcher and onMounted hydration:
```ts
watch(activeTab, (tab) => {
  const query = { ...route.query }
  if (tab === 'management') {
    query.tab = 'management'
  } else if (tab === 'stats') {
    query.tab = 'stats'
  } else {
    delete query.tab
  }
  router.replace({ query })

  if (tab === 'management' || tab === 'stats') {
    reviewStore.fetchAnalytics()
  }
})
```
On component mount, restore `activeTab` if `route.query.tab === 'management'` or `route.query.tab === 'stats'`.

## Risks / Trade-offs

- **Component Tests**: `tests/pages/review.spec.ts` asserts that the top tab switcher renders 2 tabs. We will update the test suite to assert the new 3-tab layout and verify navigation between `session`, `management`, and `stats`.
- **Deep Links**: Existing bookmarked links to `/review?tab=management` will now display the uncluttered inventory directly, improving user experience without breaking backwards compatibility.

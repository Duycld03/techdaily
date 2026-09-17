# Design: Refine Insights, Review Session, Advanced Filter, and Quiz Bento UI

## Overview

This technical design document outlines the UI architecture, component refactoring, and state management updates for four user-facing refinements in TechDaily:
1. **Insights Dedicated View Mode Switcher & Tailored Empty State** (`frontend/pages/insights.vue`).
2. **Review Session Completion State Deduplication** (`frontend/pages/review.vue`).
3. **Advanced Filter Modal Button Normalization** (`frontend/components/review/AdvancedFilterModal.vue`).
4. **Symmetric 2-Column Bento Grid Alignment for Quiz Stats Tab** (`frontend/pages/quiz.vue`).

---

## 1. Insights Dedicated View Mode Switcher (`frontend/pages/insights.vue`)

### Architecture & Information Hierarchy

Previously, `/insights` combined view mode and category filtering in a single horizontal scroll row. This led to "Đã Lưu" being truncated off-screen and prevented users from filtering their bookmarked items by topic.

The new design introduces a clear three-tier visual hierarchy:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. Header Banner                                                            │
│    • Title, Subtitle, AI Insight Generator Button, Shuffle Button          │
└─────────────────────────────────────────────────────────────────────────────┘
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 2. Dedicated View Mode Switcher (NEW)                                       │
│    ┌──────────────────────────────┐  ┌──────────────────────────────────┐   │
│    │  🌐 Khám Phá (Explore Feed)  │  │  🔖 Đã Lưu (N) (Saved Bookmarks) │   │
│    └──────────────────────────────┘  └──────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 3. Independent Category Filter Bar                                          │
│    [ Tất Cả Chủ Đề ]  [ Frontend & Vue ]  [ .NET & C# ]  [ Postgres & DB ]  │
│    (Works independently in BOTH Explore and Saved modes)                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 4. Card Reader or Context-Aware Empty State                                 │
│    • Explore Mode Empty State: "Chưa có kiến thức phù hợp" + Generate AI   │
│    • Saved Mode Empty State:   "Bạn chưa lưu mẫu kiến thức nào"            │
│                                + BookmarkCheck icon + "Khám Phá Ngay" CTA   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Component State & Reactivity

```typescript
// View Mode Definition
type InsightsViewMode = 'explore' | 'saved'

const viewMode = ref<InsightsViewMode>('explore')

// Bookmarked count reactively derived from store
const bookmarkedCount = computed(() => {
  return insightsStore.bookmarkedInsights.length
})

// Switching View Modes
async function switchViewMode(mode: InsightsViewMode) {
  viewMode.value = mode
  if (mode === 'saved') {
    if (!authStore.isAuthenticated) {
      toast.warning(t('insights.toast_login_required_saved'))
      viewMode.value = 'explore'
      return
    }
    await insightsStore.fetchFeed(insightsStore.selectedCategory, null, true)
  } else {
    await insightsStore.fetchFeed(insightsStore.selectedCategory, null, false)
  }
}

// Category Selection (Independent of View Mode)
async function handleCategorySelect(catId: number | null) {
  const onlySaved = viewMode.value === 'saved'
  await insightsStore.fetchFeed(catId, null, onlySaved)
}
```

### Tailored Saved Empty State Specification

When `viewMode === 'saved'` and `!insightsStore.currentInsight`:
- **Icon Container**: 48x48px container with `bg-indigo-50 dark:bg-indigo-950/50 text-indigo-600 dark:text-indigo-400`. Renders `<BookmarkCheck class="w-6 h-6" />`.
- **Title**: `{{ $t('insights.saved_empty_title') }}` ("Bạn chưa lưu mẫu kiến thức nào").
- **Description**: `{{ $t('insights.saved_empty_desc') }}` ("Hãy bấm biểu tượng Bookmark trên các thẻ kiến thức khi khám phá để lưu lại xem sau.").
- **Action CTA**:
  ```html
  <button
    @click="switchViewMode('explore')"
    class="px-5 py-2.5 rounded-xl bg-indigo-600 text-white text-sm font-bold hover:bg-indigo-500 shadow-md shadow-indigo-500/20 transition-all inline-flex items-center gap-2"
  >
    <Sparkles class="w-4 h-4" />
    <span>{{ $t('insights.saved_empty_cta') }}</span>
  </button>
  ```

---

## 2. Review Session Completion State Deduplication (`frontend/pages/review.vue`)

### Problem & Solution

In `frontend/pages/review.vue`, completing the cards in Tab 1 ("Review Session") rendered:
1. Celebratory Hero Card
2. `MasteryGaugeCard`
3. `ReviewForecastChart`

Because `MasteryGaugeCard` and `ReviewForecastChart` already reside permanently in Tab 2 ("Deck Management"), repeating them in the completion view creates unnecessary DOM bloat, vertical stretching, and cognitive friction.

### Structural Refactoring

We eliminate the embedded bento analytics grid from the completion branch:

```html
<!-- BEFORE -->
<div v-else class="w-full max-w-4xl space-y-6 sm:space-y-8 animate-in fade-in zoom-in-95 duration-200">
  <div class="p-6 sm:p-8 rounded-3xl ...">
    <!-- Hero Banner with CTAs -->
  </div>
  <!-- DUPLICATE CARDS (REMOVED) -->
  <div class="grid grid-cols-1 md:grid-cols-2 gap-4 sm:gap-6">
    <MasteryGaugeCard ... />
    <ReviewForecastChart ... />
  </div>
</div>

<!-- AFTER -->
<div v-else class="w-full max-w-2xl mx-auto space-y-6 animate-in fade-in zoom-in-95 duration-200">
  <div class="p-6 sm:p-10 rounded-3xl bg-gradient-to-br from-emerald-500/10 via-brand-500/5 to-transparent border border-emerald-500/20 dark:border-emerald-500/30 text-center space-y-4 shadow-sm">
    <div class="w-16 h-16 rounded-2xl bg-emerald-100 dark:bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-500/30 flex items-center justify-center mx-auto shadow-sm">
      <CheckCircle class="w-8 h-8" />
    </div>
    <h2 class="text-2xl sm:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
      {{ $t('review.no_cards') }}
    </h2>
    <p class="text-sm sm:text-base text-slate-600 dark:text-slate-400 max-w-lg mx-auto leading-relaxed">
      {{ $t('review.no_cards_desc') }}
    </p>
    <div class="flex flex-col sm:flex-row items-center justify-center gap-3 pt-2">
      <button
        @click="activeTab = 'management'"
        class="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-5 py-3 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs sm:text-sm shadow-md shadow-brand-500/20 transition-all active:scale-95 whitespace-nowrap shrink-0"
      >
        <Library class="w-4 h-4" />
        <span>{{ $t('review.browse_deck_btn') }} ({{ reviewStore.deckStatistics.totalCards }} {{ $t('review.cards_unit') }})</span>
      </button>
      <NuxtLink
        to="/today"
        class="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-5 py-3 rounded-2xl bg-white dark:bg-slate-900 hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-200 font-semibold text-xs sm:text-sm border border-slate-200 dark:border-slate-800 transition-all shadow-sm whitespace-nowrap shrink-0"
      >
        <Sparkles class="w-4 h-4 text-brand-500" />
        <span>{{ $t('review.cram_practice_btn') }}</span>
      </NuxtLink>
    </div>
  </div>
</div>
```

---

## 3. Advanced Filter Modal Button Normalization (`frontend/components/review/AdvancedFilterModal.vue`)

### Layout & Style Standardization

The existing buttons suffer from two defects:
1. Presence of `<Check class="w-3.5 h-3.5" />` in every active button creates width asymmetry and causes multi-word labels to wrap onto 2–3 lines.
2. Inconsistent color schemes: Section 1 used Sky/Amber/Rose, Section 2 used Amber/Purple/Emerald, Section 3 used Amber/Rose/Purple.

### Unified Visual Contract

- **Icon Removal**: Delete `<Check class="w-3.5 h-3.5" />` from all buttons. Remove the `Check` import from `lucide-vue-next`.
- **Consistent Active State**: Apply `bg-brand-600 text-white font-bold border-transparent shadow-sm` across all selected filter buttons regardless of section.
- **Consistent Inactive State**: Apply `bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800`.
- **Text Wrapping Protection**: Apply `whitespace-nowrap` to every filter button class list.

Example refactored button:
```html
<button
  type="button"
  @click="localSourceType = 1"
  :class="[
    'px-3 py-2 rounded-xl text-xs font-semibold border transition-all text-center whitespace-nowrap',
    localSourceType === 1
      ? 'bg-brand-600 text-white font-bold border-transparent shadow-sm'
      : 'bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800'
  ]"
>
  <span>{{ $t('review.source_highlight') }}</span>
</button>
```

---

## 4. Symmetric Bento Grid Alignment for Quiz Stats Tab (`frontend/pages/quiz.vue`)

### CSS Grid Refactoring

The current grid uses an asymmetric 12-column layout on desktop:
- Row 1: `lg:col-span-7` (~58.3%) + `lg:col-span-5` (~41.7%)
- Row 2: `lg:col-span-6` (50%) + `lg:col-span-6` (50%)

This misaligns the vertical divider seam between Row 1 and Row 2.

### Balanced 2-Column Grid Architecture

We refactor the container to a direct 2-column grid on `lg:`:

```html
<!-- Grid Container -->
<div class="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6">
  <!-- Bento Card 1: Hero Performance Card (Col 1, 50% width) -->
  <div class="rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 p-5 sm:p-6 shadow-sm flex flex-col justify-between space-y-5">
    ...
  </div>

  <!-- Bento Card 2: Spaced Mastery Gauge Card (Col 2, 50% width) -->
  <div class="rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white p-5 sm:p-6 shadow-sm flex flex-col justify-between">
    ...
  </div>

  <!-- Bento Card 3: Seniority Matrix Card (Col 1, 50% width) -->
  <div class="rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 p-5 sm:p-6 shadow-sm space-y-4">
    ...
  </div>

  <!-- Bento Card 4: Topic Strengths & Weaknesses Radar Card (Col 2, 50% width) -->
  <div class="rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 p-5 sm:p-6 shadow-sm space-y-4">
    ...
  </div>
</div>
```

### Visual Symmetry & Divider Alignment

```
┌────────────────────────────────────────┬────────────────────────────────────────┐
│ Bento Card 1: Hero Performance (50%)   │ Bento Card 2: Mastery Gauge (50%)      │
│ Accuracy Rate %, Readiness Badge, CTA  │ Semi-Circular Radial SVG Arc, Tier     │
├────────────────────────────────────────┼────────────────────────────────────────┤  ◄── Straight 100% vertical seam
│ Bento Card 3: Seniority Matrix (50%)   │ Bento Card 4: Topic Radar (50%)        │
│ Fresher, Junior, Mid, Senior Bars      │ Topic Strengths & Weakness Badges      │
└────────────────────────────────────────┴────────────────────────────────────────┘
```

---

## 5. Production Deployment & Live Verification Flow

```
┌──────────────┐     git push      ┌────────────────────┐   Build & Test   ┌──────────────┐
│ Local Source │ ────────────────► │ origin/main GitHub │ ───────────────► │ GitHub Actions│
└──────────────┘                   └────────────────────┘                  └──────┬───────┘
                                                                                  │ Docker multi-arch
                                                                                  ▼
┌────────────────────────────┐    Deploy Hook    ┌────────────────────────────────────────┐
│ Production VPS             │ ◄──────────────── │ GitHub Container Registry (ghcr.io)    │
│ • Container Restart        │                   └────────────────────────────────────────┘
│ • Nginx Reverse Proxy Reload│
└─────────────┬──────────────┘
              │ Verified Live
              ▼
┌────────────────────────────────────────┐
│ Live Verification via MCP Tools        │
│ • https://techdaily.duckdns.org        │
│ • Validate /insights, /review, /quiz   │
└────────────────────────────────────────┘
```

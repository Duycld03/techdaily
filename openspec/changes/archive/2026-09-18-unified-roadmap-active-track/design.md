# Design: Unified Roadmap Active Track

## Context

See `proposal.md` for background, problem statement, and user experience rationale.

In TechDaily's architecture, `/roadmap` (`frontend/pages/roadmap.vue`) provides software engineers with a macro visualization of their learning trajectory. Originally built for a static 30-day senior curriculum, the platform has since evolved into a versatile document pacer system capable of parsing, slicing, and pacing uploaded PDFs and crawled documentation series.

However, `/roadmap` currently maintains two disconnected presentation modes toggled via a top-level tab bar:
1. `activeMode === 'book'`: Renders active document chapter milestones and slices.
2. `activeMode === 'curriculum'`: Renders the 30-day curriculum skill tree across 4 technical modules.

This dual-mode approach introduces duplicate header banners, redundant metric counter cards, and cognitive confusion. This design unifies `/roadmap` into a single, cohesive timeline view directly synchronized with the user's active learning track on `/today`.

---

## Goals / Non-Goals

### Goals
- **Single Cohesive Interface:** Eliminate the dual-mode tab switcher (`[ Active Book ]` vs `[ Switch to Curriculum ]`) and duplicate headers.
- **Synchronized Track Switcher Dropdown:** Introduce a clean dropdown in the header banner synchronized with `/today`'s book switcher, allowing users to switch between their active book, other library documents, and the 30-day curriculum.
- **Multi-Store State Harmony:** Synchronize reactive state between `useDailyFocusStore`, `useLibraryStore`, and `useRoadmapStore` without race conditions or redundant network requests.
- **1-Click Action Bridges:** Maintain and enhance seamless navigation bridges from roadmap nodes to `/today` (for scenario challenges) and `/read/[bookId]` (for immersive reading).
- **Responsive & Bilingual Invariants:** Enforce strict adherence to AGENTS.md invariant 37 (`whitespace-nowrap shrink-0`, responsive padding, and Vietnamese/English text length compatibility).
- **Automated Vitest Verification:** Provide end-to-end component testing in `frontend/tests/pages/roadmap.spec.ts`.

### Non-Goals
- Modifying backend API schemas or C# controller endpoints.
- Altering reading slice curation algorithms or the Spaced Repetition (SM-2) engine.
- Redesigning the reader (`/read/[bookId]`) or quiz (`/quiz`) layouts.
- Changing the content or structure of the 30-day senior curriculum.

---

## Architectural Breakdown

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       UNIFIED ROADMAP ARCHITECTURE                          │
│                                                                             │
│  1. Reactive State Layer (Pinia)                                            │
│  ┌────────────────────────┐ ┌───────────────────────┐ ┌───────────────────┐  │
│  │  useDailyFocusStore    │ │    useLibraryStore    │ │  useRoadmapStore  │  │
│  │  • pacer.bookId        │ │    • selectedBook     │ │  • roadmapData    │  │
│  │  • availableBooks      │ │    • fetchBookById()  │ │  • fetchRoadmap() │  │
│  │  • switchBook()        │ │                       │ │                   │  │
│  └───────────┬────────────┘ └───────────┬───────────┘ └─────────┬─────────┘  │
│              │                          │                       │            │
│              └──────────────────────────┼───────────────────────┘            │
│                                         ▼                                    │
│  2. Unified Header & Track Switcher (/roadmap)                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  [ 🗺️ LỘ TRÌNH HỌC ]   [ 📖 Designing Data-Intensive Applications ▾ ] │  │
│  │                                                                       │  │
│  │  Dropdown Popover:                                                    │  │
│  │  ├── Active: Designing Data-Intensive Applications [Active Badge]     │  │
│  │  ├── In-Progress Books: PostgreSQL 17 Internals (18/45 slices)        │  │
│  │  ├── Core Curriculum: 30-Day Senior Fullstack (12/30 days)            │  │
│  │  └── Action: + Khám phá Thư Viện (/library)                           │  │
│  │                                                                       │  │
│  │  Dynamic Metric Counter: 14 / 42 Lát cắt (33%) • Còn 28 ngày          │  │
│  │  Global Progress Bar: [=====================>                       ] │  │
│  └──────────────────────────────────────┬────────────────────────────────┘  │
│                                         │                                    │
│              ┌──────────────────────────┴──────────────────────────┐         │
│              ▼                                                     ▼         │
│  3A. Document Chapter Milestones                   3B. Core Curriculum Track │
│  ┌──────────────────────────────────────────────┐  ┌───────────────────────┐ │
│  │ [Search Chapters...] [Expand] [Collapse]     │  │ Module 1: Frontend    │ │
│  │                                              │  │ Module 2: Backend     │ │
│  │ ▾ Chapter 1: Foundations (3/3 Slices • Pass) │  │ Module 3: Database    │ │
│  │ ▾ Chapter 2: Storage Engines (Active Today 🔥)│  │ Module 4: Architecture│ │
│  │   ├── Slice 14: LSM-Trees [Start Today ⚡]   │  │                       │ │
│  │   └── Slice 15: B-Trees [Read Slice 📖]      │  │                       │ │
│  └──────────────────────────────────────────────┘  └───────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Detailed Decisions & Implementation Specs

### 1. Unified Header Banner & Track Switcher Dropdown

Instead of rendering two separate header sections, `frontend/pages/roadmap.vue` renders a single, unified header card:

```html
<div class="p-4 sm:p-8 rounded-3xl bg-gradient-to-br from-indigo-50/80 via-white to-brand-50/50 dark:from-slate-900 dark:via-slate-900 dark:to-brand-950 border border-slate-200/90 dark:border-slate-800 text-slate-900 dark:text-white shadow-md dark:shadow-xl relative overflow-hidden transition-all duration-300">
  <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-5 sm:gap-6">
    <div class="space-y-3 max-w-2xl">
      <!-- Track Switcher Pill & Dropdown Anchor -->
      <div class="flex items-center gap-2 flex-wrap">
        <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-brand-50 dark:bg-brand-500/20 border border-brand-200 dark:border-brand-500/30 text-brand-700 dark:text-brand-300 text-xs font-bold tracking-wide uppercase">
          <MapIcon class="w-3.5 h-3.5" />
          <span>{{ $t('roadmap.badge') }}</span>
        </div>

        <!-- Track Switcher Dropdown -->
        <div ref="trackMenuRef" class="relative">
          <button
            @click="isTrackMenuOpen = !isTrackMenuOpen"
            class="inline-flex items-center gap-2 px-3 py-1.5 rounded-xl bg-white/90 dark:bg-slate-800/90 hover:bg-white dark:hover:bg-slate-800 border border-slate-200/90 dark:border-slate-700/80 text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 transition-all shadow-sm active:scale-95 whitespace-nowrap shrink-0"
          >
            <component :is="isCurriculumSelected ? Compass : BookOpen" class="w-4 h-4 text-brand-600 dark:text-brand-400 shrink-0" />
            <span class="max-w-[140px] sm:max-w-[220px] md:max-w-[280px] truncate">
              {{ currentTrackTitle }}
            </span>
            <ChevronDown :class="['w-3.5 h-3.5 text-slate-400 transition-transform duration-200 shrink-0', isTrackMenuOpen ? 'rotate-180' : '']" />
          </button>

          <!-- Dropdown Popover Menu -->
          <div
            v-if="isTrackMenuOpen"
            class="absolute left-0 top-full mt-2 w-72 sm:w-84 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl p-2 z-50 animate-in fade-in zoom-in-95 duration-150 space-y-1"
          >
            <!-- In-Progress Document Tracks -->
            <div v-if="availableBookTracks.length > 0" class="px-3 py-1.5 text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
              {{ $t('roadmap.in_progress_tracks') }}
            </div>

            <div class="max-h-60 overflow-y-auto space-y-1">
              <button
                v-for="b in availableBookTracks"
                :key="b.id"
                @click="handleSelectBookTrack(b.id)"
                :class="[
                  'w-full text-left p-2.5 rounded-xl text-xs sm:text-sm transition-all flex flex-col gap-1.5 group',
                  selectedBookId === b.id && !isCurriculumSelected
                    ? 'bg-brand-50/80 dark:bg-brand-950/40 border border-brand-200 dark:border-brand-800/80 text-brand-950 dark:text-brand-100'
                    : 'hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300'
                ]"
              >
                <div class="flex items-center justify-between gap-2">
                  <span class="font-bold truncate group-hover:text-brand-600 dark:group-hover:text-brand-400">
                    {{ b.title }}
                  </span>
                  <span
                    v-if="focusStore.data?.pacer?.bookId === b.id"
                    class="px-2 py-0.5 rounded-full text-xs font-bold bg-brand-600 text-white whitespace-nowrap shrink-0"
                  >
                    {{ $t('roadmap.active_badge') }}
                  </span>
                </div>
                <div class="flex items-center justify-between gap-2 text-xs text-slate-400 dark:text-slate-500">
                  <div class="flex-1 h-1.5 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
                    <div class="h-full bg-brand-500 rounded-full transition-all duration-300" :style="{ width: `${b.progressPercentage}%` }"></div>
                  </div>
                  <span class="font-mono shrink-0">{{ b.currentChunkOrder }}/{{ b.totalChunks }} ({{ b.progressPercentage }}%)</span>
                </div>
              </button>
            </div>

            <!-- 30-Day Senior Curriculum Track -->
            <div class="pt-2 border-t border-slate-100 dark:border-slate-800">
              <button
                @click="handleSelectCurriculumTrack"
                :class="[
                  'w-full text-left p-2.5 rounded-xl text-xs sm:text-sm transition-all flex items-center justify-between group',
                  isCurriculumSelected
                    ? 'bg-brand-50/80 dark:bg-brand-950/40 border border-brand-200 dark:border-brand-800/80 text-brand-950 dark:text-brand-100 font-bold'
                    : 'hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300'
                ]"
              >
                <div class="flex items-center gap-2.5 min-w-0">
                  <Compass class="w-4 h-4 text-brand-500 shrink-0" />
                  <div class="min-w-0">
                    <div class="font-bold truncate">{{ $t('roadmap.curriculum_track') }}</div>
                    <div class="text-xs text-slate-400 dark:text-slate-500">{{ $t('roadmap.curriculum_track_desc') }}</div>
                  </div>
                </div>
                <span class="text-xs font-mono text-slate-400 shrink-0">
                  {{ roadmapStore.roadmapData?.completedDaysCount ?? 0 }}/30 ({{ roadmapStore.roadmapData?.overallProgressPercentage ?? 0 }}%)
                </span>
              </button>
            </div>

            <!-- Browse Library Action Bridge -->
            <div class="pt-2 border-t border-slate-100 dark:border-slate-800">
              <NuxtLink
                to="/library"
                @click="isTrackMenuOpen = false"
                class="flex items-center justify-between px-3 py-2 rounded-xl text-xs sm:text-sm font-bold text-brand-600 dark:text-brand-400 hover:bg-brand-50 dark:hover:bg-brand-950/40 transition-colors"
              >
                <span>+ {{ $t('roadmap.browse_library') }}</span>
                <ArrowRight class="w-3.5 h-3.5" />
              </NuxtLink>
            </div>
          </div>
        </div>
      </div>

      <!-- Title & Chapter/Module Subtitle -->
      <h1 class="text-xl sm:text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white">
        {{ headerTitle }}
      </h1>
      <p class="text-slate-600 dark:text-slate-300 text-sm md:text-lg leading-relaxed">
        {{ headerSubtitle }}
      </p>
    </div>

    <!-- Metric Counter Card -->
    <div class="flex items-center gap-3.5 sm:gap-4 bg-white/90 dark:bg-slate-800/80 backdrop-blur-md p-3.5 sm:p-5 rounded-2xl border border-slate-200/80 dark:border-slate-700/80 shadow-sm shrink-0">
      <div class="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-brand-50 dark:bg-brand-500/20 border border-brand-200 dark:border-brand-500/30 flex items-center justify-center text-brand-600 dark:text-brand-400 shrink-0">
        <Award class="w-5 h-5 sm:w-6 sm:h-6" />
      </div>
      <div>
        <div class="text-xs text-slate-500 dark:text-slate-400 uppercase tracking-wider font-semibold">
          {{ $t('roadmap.progress') }}
        </div>
        <div class="text-xl sm:text-2xl font-black text-slate-900 dark:text-white flex items-baseline gap-1.5">
          <span>{{ metricCompletedCount }}</span>
          <span class="text-xs text-slate-500 dark:text-slate-400 font-medium">/ {{ metricTotalCount }} {{ metricUnitLabel }}</span>
        </div>
        <div class="text-xs text-brand-600 dark:text-brand-400 font-bold mt-0.5 flex items-center gap-2">
          <span>{{ metricProgressPercentage }}% {{ $t('roadmap.completed') }}</span>
          <template v-if="!isCurriculumSelected && estDaysRemaining > 0">
            <span class="text-slate-400 dark:text-slate-500 font-normal">•</span>
            <span class="text-slate-600 dark:text-slate-300 font-medium">{{ $t('roadmap.est_days', { days: estDaysRemaining }) }}</span>
          </template>
        </div>
      </div>
    </div>
  </div>

  <!-- Global Progress Bar -->
  <div class="mt-5 sm:mt-6 space-y-1.5">
    <div class="w-full h-2.5 bg-slate-200/80 dark:bg-slate-800 rounded-full overflow-hidden p-0.5 border border-slate-200 dark:border-slate-700/50">
      <div
        class="h-full bg-gradient-to-r from-brand-500 to-emerald-400 rounded-full transition-all duration-500 shadow-sm"
        :style="{ width: `${metricProgressPercentage}%` }"
      ></div>
    </div>
  </div>
</div>
```

---

### 2. State Management & Multi-Store Synchronization

Three Pinia stores interact in this feature:
1. `useDailyFocusStore`: Holds the user's authoritative daily focus state (`data.pacer.bookId`, `data.pacer.availableBooks`, and method `switchBook(bookId)`).
2. `useLibraryStore`: Holds loaded book details with chunks (`selectedBook`, `fetchBookById(id)`).
3. `useRoadmapStore`: Holds the 30-day curriculum roadmap data (`roadmapData`, `fetchRoadmap()`).

#### Initialization Lifecycle:
```ts
const isCurriculumSelected = ref(false)
const selectedBookId = ref<string | null>(null)
const isTrackMenuOpen = ref(false)
const trackMenuRef = ref<HTMLElement | null>(null)

onMounted(async () => {
  // Fetch both stores in parallel to ensure zero-latency track switching
  await Promise.all([
    focusStore.fetchTodayFocus(undefined, undefined, locale.value),
    roadmapStore.fetchRoadmap()
  ])

  // If URL query specifies bookId, respect it; otherwise default to active pacer book
  const queryBookId = route.query.bookId as string | undefined
  const queryTrack = route.query.track as string | undefined

  if (queryTrack === 'curriculum') {
    isCurriculumSelected.value = true
  } else if (queryBookId) {
    selectedBookId.value = queryBookId
    await loadBookDetails(queryBookId)
  } else if (focusStore.data?.pacer?.bookId) {
    selectedBookId.value = focusStore.data.pacer.bookId
    await loadBookDetails(focusStore.data.pacer.bookId)
  } else {
    // Fallback to 30-day curriculum if user has no document books
    isCurriculumSelected.value = true
  }
})
```

#### Track Switching Actions:
- **Switch to an Alternative Book:**
  ```ts
  async function handleSelectBookTrack(bookId: string) {
    isTrackMenuOpen.value = false
    isCurriculumSelected.value = false
    selectedBookId.value = bookId

    // Synchronize active pacer on /today
    if (focusStore.data?.pacer?.bookId !== bookId) {
      await focusStore.switchBook(bookId, locale.value)
    }
    await loadBookDetails(bookId)
  }
  ```
- **Switch to 30-Day Senior Curriculum:**
  ```ts
  function handleSelectCurriculumTrack() {
    isTrackMenuOpen.value = false
    isCurriculumSelected.value = true
  }
  ```

#### Click-Outside Listener:
To ensure crisp UX, when the dropdown is open, clicks outside `trackMenuRef` automatically close it:
```ts
function onDocumentClick(e: MouseEvent) {
  if (trackMenuRef.value && !trackMenuRef.value.contains(e.target as Node)) {
    isTrackMenuOpen.value = false
  }
}
onMounted(() => window.addEventListener('click', onDocumentClick))
onUnmounted(() => window.removeEventListener('click', onDocumentClick))
```

---

### 3. Chapter Milestones, Slices & 1-Click Action Bridges

When viewing a document book track (`!isCurriculumSelected`):
- **Search Bar & Accordion Controls:** Retains the chapter and slice query filter (`chapterSearch`), filtering by title, slice name, or summary text, with `Expand All` and `Collapse All` buttons.
- **Milestone Cards:** Renders chapter milestone accordion cards with completion status, slice count indicators, and progress bars.
- **Slice Cards:**
  - **Active Today:** Highlights in amber border with pulsing flame badge (`Flame`), 1-click CTA: `Start Today's Drill` (`navigateToActiveSlice()`).
  - **Completed (Pass):** Highlights in emerald border with `CheckCircle2` badge, 1-click CTA: `Review Architecture` (`navigateToSlice(chunkOrder)`).
  - **Ready / Upcoming:** 1-click CTA: `Explore Lesson` navigating to `/read/{bookId}?slice={chunkOrder}`.

When viewing the 30-day curriculum track (`isCurriculumSelected`):
- Renders the 4 module cards (`FrontendWeb`, `BackendDotNet`, `DatabaseStorage`, `SystemDesign`) with day nodes.
- Unlocked nodes link directly to `/today?day={dayOrder}` in review mode.

---

### 4. Bilingual Localization & Layout Invariants

To comply with AGENTS.md invariant 37, all action buttons, pills, and badges must use `whitespace-nowrap shrink-0` and responsive gap layout.

#### New i18n Keys in `frontend/i18n/locales/en.json`:
```json
{
  "roadmap": {
    "track_switcher_label": "Active Learning Track",
    "in_progress_tracks": "In-Progress Documents",
    "curriculum_track": "30-Day Senior Curriculum",
    "curriculum_track_desc": "Core fullstack architecture skill tree",
    "browse_library": "Choose from Library",
    "active_badge": "Active"
  }
}
```

#### New i18n Keys in `frontend/i18n/locales/vi.json`:
```json
{
  "roadmap": {
    "track_switcher_label": "Lộ Trình Đang Học",
    "in_progress_tracks": "Tài Liệu Đang Đọc",
    "curriculum_track": "Lộ Trình 30 Ngày Chuẩn",
    "curriculum_track_desc": "Cây kỹ năng kiến trúc fullstack senior",
    "browse_library": "Khám phá Thư Viện",
    "active_badge": "Đang Học"
  }
}
```

---

## Risks & Mitigations

1. **[Risk] Slower initial load if fetching both stores on mount:**
   - *Mitigation:* `focusStore.fetchTodayFocus()` and `roadmapStore.fetchRoadmap()` run concurrently via `Promise.all`. The curriculum roadmap endpoint `/api/v1/curriculum/roadmap` is in-memory cached and responds in $<15\text{ms}$. Pre-fetching it ensures that switching to the curriculum track feels instantaneous without spinner flickers.
2. **[Risk] Book switching desynchronization between `/today` and `/roadmap`:**
   - *Mitigation:* Selecting a book in `/roadmap`'s track switcher invokes `focusStore.switchBook()`. This updates the server-side active book and reactive store state simultaneously. Navigating to `/today` immediately displays the newly selected book without any discrepancy.
3. **[Risk] Responsive dropdown clipping on mobile viewports (375px–390px):**
   - *Mitigation:* The dropdown popover is constrained to `w-72 sm:w-84 max-w-[calc(100vw-2rem)]` and positioned with `left-0 top-full mt-2`, guaranteeing it stays well within viewport boundaries.

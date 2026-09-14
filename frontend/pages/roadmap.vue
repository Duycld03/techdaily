<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { 
  Map as MapIcon, 
  CheckCircle2, 
  Flame, 
  Lock, 
  ArrowRight, 
  Eye, 
  Layers, 
  Cpu, 
  Database, 
  Network, 
  Sparkles,
  Calendar,
  Award,
  BookOpen,
  Clock,
  Compass,
  ChevronDown,
  ChevronRight,
  Search
} from 'lucide-vue-next'
import { useRoadmapStore } from '~/stores/useRoadmapStore'
import { useDailyFocusStore } from '~/stores/useDailyFocusStore'
import { useLibraryStore } from '~/stores/useLibraryStore'
import { useAuthStore } from '~/stores/useAuthStore'

const roadmapStore = useRoadmapStore()
const focusStore = useDailyFocusStore()
const libraryStore = useLibraryStore()
const authStore = useAuthStore()
const router = useRouter()
const { locale } = useI18n()

const activeMode = ref<'book' | 'curriculum'>('book')
const isLoadingBookDetails = ref(false)

onMounted(async () => {
  await Promise.all([
    roadmapStore.fetchRoadmap(),
    focusStore.fetchTodayFocus(undefined, undefined, locale.value)
  ])

  if (focusStore.data?.pacer?.bookId) {
    activeMode.value = 'book'
    isLoadingBookDetails.value = true
    try {
      await libraryStore.fetchBookById(focusStore.data.pacer.bookId)
    } finally {
      isLoadingBookDetails.value = false
    }
  } else {
    activeMode.value = 'curriculum'
  }
})

watch(() => focusStore.data?.pacer?.bookId, async (newBookId) => {
  if (newBookId) {
    isLoadingBookDetails.value = true
    try {
      await libraryStore.fetchBookById(newBookId)
    } finally {
      isLoadingBookDetails.value = false
    }
  }
})

interface ChapterSlice {
  id: string
  chunkOrder: number
  summaryMarkdown: string
  estimatedReadMinutes: number
  isCompleted: boolean
  isActiveToday: boolean
  isUpcoming: boolean
}

interface ChapterMilestone {
  chapterTitle: string
  chapterIndex: number
  slices: ChapterSlice[]
  isCompleted: boolean
  isActive: boolean
  completedSlicesCount: number
  totalSlicesCount: number
}

const chapterMilestones = computed<ChapterMilestone[]>(() => {
  if (!libraryStore.selectedBook?.chunks || !focusStore.data?.pacer) return []
  const pacer = focusStore.data.pacer
  const chunks = libraryStore.selectedBook.chunks

  const map = new Map<string, typeof chunks>()
  for (const chunk of chunks) {
    const title = chunk.chapterTitle || 'Chapter Overview'
    if (!map.has(title)) {
      map.set(title, [])
    }
    map.get(title)!.push(chunk)
  }

  let chapterIdx = 1
  const list: ChapterMilestone[] = []

  for (const [title, chapterChunks] of map.entries()) {
    const slices: ChapterSlice[] = chapterChunks.map(c => {
      const isCompleted = c.chunkOrder < pacer.currentChunkOrder
      const isActiveToday = c.chunkOrder === pacer.currentChunkOrder
      const isUpcoming = c.chunkOrder > pacer.currentChunkOrder
      return {
        id: c.id,
        chunkOrder: c.chunkOrder,
        summaryMarkdown: c.summaryMarkdown,
        estimatedReadMinutes: c.estimatedReadMinutes,
        isCompleted,
        isActiveToday,
        isUpcoming
      }
    })

    const completedSlicesCount = slices.filter(s => s.isCompleted).length
    const totalSlicesCount = slices.length
    const isCompleted = completedSlicesCount === totalSlicesCount
    const isActive = slices.some(s => s.isActiveToday)

    list.push({
      chapterTitle: title,
      chapterIndex: chapterIdx++,
      slices,
      isCompleted,
      isActive,
      completedSlicesCount,
      totalSlicesCount
    })
  }

  return list
})

const chapterSearch = ref('')
const visibleChaptersCount = ref(30)
const expandedChapters = ref<Set<number>>(new Set())

watch(chapterMilestones, (milestones) => {
  if (expandedChapters.value.size === 0) {
    const active = milestones.find(m => m.isActive)
    if (active) {
      expandedChapters.value.add(active.chapterIndex)
    } else if (milestones.length > 0) {
      expandedChapters.value.add(milestones[0].chapterIndex)
    }
  }
}, { immediate: true })

const filteredChapters = computed(() => {
  if (!chapterSearch.value.trim()) return chapterMilestones.value
  const query = chapterSearch.value.toLowerCase().trim()
  return chapterMilestones.value.filter(c => c.chapterTitle.toLowerCase().includes(query))
})

const displayedChapters = computed(() => {
  return filteredChapters.value.slice(0, visibleChaptersCount.value)
})

function toggleChapter(chapterIndex: number) {
  if (expandedChapters.value.has(chapterIndex)) {
    expandedChapters.value.delete(chapterIndex)
  } else {
    expandedChapters.value.add(chapterIndex)
  }
}

function expandAll() {
  for (const c of displayedChapters.value) {
    expandedChapters.value.add(c.chapterIndex)
  }
}

function collapseAll() {
  expandedChapters.value.clear()
}

function loadMoreChapters() {
  visibleChaptersCount.value += 30
}

const estDaysRemaining = computed(() => {
  if (!focusStore.data?.pacer) return 0
  const remaining = focusStore.data.pacer.totalChunks - focusStore.data.pacer.currentChunkOrder + 1
  return Math.max(0, remaining)
})

function navigateToSlice(chunkOrder: number) {
  if (focusStore.data?.pacer) {
    router.push(`/today?bookId=${focusStore.data.pacer.bookId}&chunkOrder=${chunkOrder}`)
  }
}

function navigateToActiveSlice() {
  if (focusStore.data?.pacer) {
    router.push(`/today?bookId=${focusStore.data.pacer.bookId}&chunkOrder=${focusStore.data.pacer.currentChunkOrder}`)
  }
}

function navigateToDay(dayOrder: number) {
  router.push(`/today?day=${dayOrder}`)
}

const categoryIcons = [
  Layers,    // 0: FrontendWeb
  Cpu,       // 1: BackendDotNet
  Database,  // 2: DatabaseStorage
  Network    // 3: SystemDesign
]

function getModuleIcon(category: number) {
  return categoryIcons[category] || Layers
}

function getDifficultyLabel(diff: number) {
  switch (diff) {
    case 0: return 'Intermediate'
    case 1: return 'Senior'
    case 2: return 'Lead Architect'
    default: return 'Senior'
  }
}

function getDifficultyColor(diff: number) {
  switch (diff) {
    case 0: return 'bg-sky-50 dark:bg-sky-950/40 text-sky-700 dark:text-sky-300 border-sky-200 dark:border-sky-800'
    case 1: return 'bg-brand-50 dark:bg-brand-950/40 text-brand-700 dark:text-brand-300 border-brand-200 dark:border-brand-800'
    case 2: return 'bg-purple-50 dark:bg-purple-950/40 text-purple-700 dark:text-purple-300 border-purple-200 dark:border-purple-800'
    default: return 'bg-slate-50 dark:bg-slate-900 text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-800'
  }
}
</script>

<template>
  <div class="max-w-6xl mx-auto px-3 sm:px-6 py-5 sm:py-8 space-y-6 sm:space-y-8 animate-in fade-in duration-300">
    <!-- View Switcher Tabs (If active book pacer exists) -->
    <div v-if="focusStore.data?.pacer" class="flex items-center gap-2 p-1.5 bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl w-fit">
      <button
        @click="activeMode = 'book'"
        :class="[
          'flex items-center gap-2 px-3.5 sm:px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all whitespace-nowrap shrink-0',
          activeMode === 'book'
            ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm'
            : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
        ]"
      >
        <BookOpen class="w-4 h-4" />
        <span>{{ $t('roadmap.active_book') }}</span>
      </button>

      <button
        @click="activeMode = 'curriculum'"
        :class="[
          'flex items-center gap-2 px-3.5 sm:px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all whitespace-nowrap shrink-0',
          activeMode === 'curriculum'
            ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm'
            : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
        ]"
      >
        <Compass class="w-4 h-4" />
        <span>{{ $t('roadmap.switch_to_curriculum') }}</span>
      </button>
    </div>

    <!-- ========================================================================= -->
    <!-- SECTION A: ACTIVE BOOK CHAPTER ROADMAP                                   -->
    <!-- ========================================================================= -->
    <div v-if="activeMode === 'book' && focusStore.data?.pacer" class="space-y-6 sm:space-y-8">
      <!-- Active Book Header Banner -->
      <div class="p-4 sm:p-8 rounded-3xl bg-gradient-to-br from-indigo-50/80 via-white to-brand-50/50 dark:from-slate-900 dark:via-slate-900 dark:to-brand-950 border border-slate-200/90 dark:border-slate-800 text-slate-900 dark:text-white shadow-md dark:shadow-xl relative overflow-hidden transition-all duration-300">
        <div class="absolute -right-10 -bottom-10 w-64 h-64 bg-brand-500/10 rounded-full blur-3xl pointer-events-none"></div>

        <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-5 sm:gap-6">
          <div class="space-y-2 max-w-2xl">
            <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-brand-50 dark:bg-brand-500/20 border border-brand-200 dark:border-brand-500/30 text-brand-700 dark:text-brand-300 text-xs font-bold tracking-wide uppercase">
              <BookOpen class="w-3.5 h-3.5" />
              <span>{{ $t('roadmap.active_book') }}</span>
            </div>
            <h1 class="text-xl sm:text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white">
              {{ focusStore.data.pacer.bookTitle }}
            </h1>
            <p class="text-slate-600 dark:text-slate-300 text-sm md:text-lg leading-relaxed">
              {{ focusStore.data.pacer.chapterTitle }}
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
                <span>{{ focusStore.data.pacer.currentChunkOrder - 1 }}</span>
                <span class="text-xs text-slate-500 dark:text-slate-400 font-medium">/ {{ focusStore.data.pacer.totalChunks }} {{ $t('roadmap.slices') }}</span>
              </div>
              <div class="text-xs text-brand-600 dark:text-brand-400 font-bold mt-0.5 flex items-center gap-2">
                <span>{{ focusStore.data.pacer.progressPercentage }}% {{ $t('roadmap.completed') }}</span>
                <span class="text-slate-400 dark:text-slate-500 font-normal">•</span>
                <span class="text-slate-600 dark:text-slate-300 font-medium">{{ $t('roadmap.est_days', { days: estDaysRemaining }) }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Global Progress Bar -->
        <div class="mt-5 sm:mt-6 space-y-1.5">
          <div class="w-full h-2.5 bg-slate-200/80 dark:bg-slate-800 rounded-full overflow-hidden p-0.5 border border-slate-200 dark:border-slate-700/50">
            <div
              class="h-full bg-gradient-to-r from-brand-500 to-emerald-400 rounded-full transition-all duration-500 shadow-sm"
              :style="{ width: `${focusStore.data.pacer.progressPercentage}%` }"
            ></div>
          </div>
        </div>
      </div>

      <!-- Book Chapter Milestones Loading State -->
      <div v-if="isLoadingBookDetails" class="flex flex-col items-center justify-center py-20 space-y-4">
        <div class="w-10 h-10 border-4 border-brand-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-slate-500 dark:text-slate-400 text-sm font-medium">{{ $t('roadmap.loading') }}</p>
      </div>

      <!-- Chapter Milestone Cards List -->
      <div v-else class="space-y-6 sm:space-y-8">
        <!-- Search & Controls Bar -->
        <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 p-2 bg-slate-50 dark:bg-slate-950/60 rounded-2xl border border-slate-200 dark:border-slate-800">
          <div class="relative flex-1 max-w-md">
            <Search class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="chapterSearch"
              type="text"
              :placeholder="$t('roadmap.search_chapters')"
              class="w-full pl-10 pr-4 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-xs sm:text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 focus:border-brand-500 focus:outline-none transition-colors"
            />
          </div>
          <div class="flex items-center gap-2 self-end sm:self-auto">
            <button
              @click="expandAll"
              type="button"
              class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800 text-xs font-semibold text-slate-600 dark:text-slate-400 transition-colors"
            >
              {{ $t('roadmap.expand_all') }}
            </button>
            <button
              @click="collapseAll"
              type="button"
              class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800 text-xs font-semibold text-slate-600 dark:text-slate-400 transition-colors"
            >
              {{ $t('roadmap.collapse_all') }}
            </button>
          </div>
        </div>

        <section
          v-for="chapter in displayedChapters"
          :key="chapter.chapterTitle"
          class="space-y-4 sm:space-y-5"
        >
          <!-- Chapter Milestone Header Card (Clickable Accordion) -->
          <div 
            @click="toggleChapter(chapter.chapterIndex)"
            class="flex flex-col sm:flex-row sm:items-center justify-between p-4 sm:p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm gap-3 sm:gap-4 cursor-pointer hover:border-brand-400 dark:hover:border-slate-700 transition-all select-none"
          >
            <div class="flex items-center gap-3 sm:gap-3.5">
              <div
                :class="[
                  'w-9 h-9 sm:w-10 sm:h-10 rounded-xl flex items-center justify-center font-bold text-sm shrink-0 transition-colors',
                  chapter.isCompleted
                    ? 'bg-emerald-50 dark:bg-emerald-950/60 border border-emerald-200 dark:border-emerald-800 text-emerald-600 dark:text-emerald-400'
                    : chapter.isActive
                      ? 'bg-amber-50 dark:bg-amber-950/60 border border-amber-200 dark:border-amber-800 text-amber-600 dark:text-amber-400'
                      : 'bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400'
                ]"
              >
                <span>{{ chapter.chapterIndex }}</span>
              </div>

              <div>
                <div class="flex items-center gap-2 flex-wrap">
                  <h2 class="text-sm sm:text-lg font-bold text-slate-900 dark:text-slate-100">
                    {{ chapter.chapterTitle }}
                  </h2>
                  <span
                    v-if="chapter.isActive"
                    class="text-xs px-2.5 py-0.5 rounded-full bg-amber-100 dark:bg-amber-950/60 border border-amber-300 dark:border-amber-800 text-amber-800 dark:text-amber-300 font-bold flex items-center gap-1"
                  >
                    <Flame class="w-3 h-3 text-amber-500" />
                    <span>{{ $t('roadmap.today') }}</span>
                  </span>
                </div>
                <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-0.5">
                  {{ chapter.completedSlicesCount }}/{{ chapter.totalSlicesCount }} {{ $t('roadmap.slices') }} {{ $t('roadmap.completed') }}
                </p>
              </div>
            </div>

            <!-- Chapter Progress & Jump Action -->
            <div class="flex items-center gap-3 shrink-0 self-end sm:self-auto" @click.stop>
              <div class="w-20 sm:w-28 h-2 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                <div
                  class="h-full bg-brand-500 rounded-full transition-all duration-300"
                  :style="{ width: `${(chapter.completedSlicesCount / chapter.totalSlicesCount) * 100}%` }"
                ></div>
              </div>

              <button
                v-if="chapter.isActive"
                @click="navigateToActiveSlice"
                class="px-3 py-1.5 rounded-xl bg-amber-500 hover:bg-amber-400 text-slate-950 text-xs font-bold transition-all shadow-sm active:scale-95 whitespace-nowrap shrink-0"
              >
                {{ $t('roadmap.start_today') }}
              </button>

              <button
                @click="toggleChapter(chapter.chapterIndex)"
                type="button"
                class="p-1 rounded-lg text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
              >
                <component :is="expandedChapters.has(chapter.chapterIndex) ? ChevronDown : ChevronRight" class="w-5 h-5 transition-transform" />
              </button>
            </div>
          </div>

          <!-- Slices Grid (Collapsible) -->
          <div v-show="expandedChapters.has(chapter.chapterIndex)" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 animate-in fade-in duration-200">
            <div
              v-for="slice in chapter.slices"
              :key="slice.id"
              @click="navigateToSlice(slice.chunkOrder)"
              :class="[
                'p-4 sm:p-5 rounded-2xl border transition-all duration-200 cursor-pointer flex flex-col justify-between group relative overflow-hidden select-none',
                slice.isActiveToday
                  ? 'bg-amber-500/5 dark:bg-amber-500/10 border-amber-500 dark:border-amber-400/80 shadow-md ring-2 ring-amber-500/20'
                  : slice.isCompleted
                    ? 'bg-white dark:bg-slate-900/90 border-emerald-500/30 dark:border-emerald-500/30 hover:border-emerald-500 hover:shadow-sm'
                    : 'bg-slate-50/70 dark:bg-slate-950/40 border-slate-200/80 dark:border-slate-800/60 hover:border-brand-500 hover:shadow-sm'
              ]"
            >
              <!-- Top Slice Indicator & Badges -->
              <div class="flex items-start justify-between gap-2 mb-3">
                <div class="flex items-center gap-2">
                  <span
                    :class="[
                      'w-7 h-7 rounded-lg text-xs font-black flex items-center justify-center shrink-0 transition-colors',
                      slice.isActiveToday
                        ? 'bg-amber-500 text-white shadow-sm'
                        : slice.isCompleted
                          ? 'bg-emerald-500 text-white'
                          : 'bg-slate-200 dark:bg-slate-800 text-slate-700 dark:text-slate-300'
                    ]"
                  >
                    #{{ slice.chunkOrder }}
                  </span>

                  <span class="text-xs px-2 py-0.5 rounded-md font-bold uppercase tracking-wider border bg-brand-50 dark:bg-brand-950/40 text-brand-700 dark:text-brand-300 border-brand-200 dark:border-brand-800">
                    {{ $t('roadmap.slice') }} {{ slice.chunkOrder }}
                  </span>
                </div>

                <!-- Status Icon Badge -->
                <div>
                  <span
                    v-if="slice.isActiveToday"
                    class="flex items-center gap-1 text-xs font-bold text-amber-600 dark:text-amber-400 bg-amber-100 dark:bg-amber-950/60 px-2.5 py-0.5 rounded-full border border-amber-300 dark:border-amber-700 animate-pulse whitespace-nowrap shrink-0"
                  >
                    <Flame class="w-3.5 h-3.5 text-amber-500" />
                    <span>{{ $t('roadmap.today') }}</span>
                  </span>
                  <span
                    v-else-if="slice.isCompleted"
                    class="flex items-center gap-1 text-xs font-bold text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/60 px-2.5 py-0.5 rounded-full border border-emerald-200 dark:border-emerald-800 whitespace-nowrap shrink-0"
                  >
                    <CheckCircle2 class="w-3.5 h-3.5 text-emerald-500" />
                    <span>Pass</span>
                  </span>
                  <span
                    v-else
                    class="text-xs font-semibold text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-slate-800 px-2.5 py-0.5 rounded-full whitespace-nowrap shrink-0"
                  >
                    <Eye class="w-3.5 h-3.5 inline mr-0.5" />
                    <span>{{ $t('roadmap.ready') }}</span>
                  </span>
                </div>
              </div>

              <!-- Summary Preview -->
              <div class="space-y-1.5 mb-4">
                <p class="text-sm text-slate-700 dark:text-slate-300 line-clamp-3 leading-relaxed">
                  {{ slice.summaryMarkdown || 'Architectural reading slice and trade-off scenario.' }}
                </p>
              </div>

              <!-- Action Link -->
              <div class="pt-2.5 border-t border-slate-100 dark:border-slate-800/80 flex items-center justify-between text-xs sm:text-sm font-semibold">
                <span
                  :class="[
                    slice.isActiveToday
                      ? 'text-amber-600 dark:text-amber-400 font-bold'
                      : slice.isCompleted
                        ? 'text-emerald-600 dark:text-emerald-400'
                        : 'text-slate-500 dark:text-slate-400 group-hover:text-brand-600 dark:group-hover:text-brand-400'
                  ]"
                >
                  {{ slice.isActiveToday ? $t('roadmap.start_today') : slice.isCompleted ? $t('roadmap.review_day') : $t('roadmap.view_lesson') }}
                </span>
                <ArrowRight class="w-3.5 h-3.5 text-slate-400 group-hover:text-brand-500 group-hover:translate-x-0.5 transition-transform" />
              </div>
            </div>
          </div>
        </section>

        <!-- Load More Chapters Button -->
        <div v-if="filteredChapters.length > visibleChaptersCount" class="flex justify-center pt-4 pb-6">
          <button
            @click="loadMoreChapters"
            type="button"
            class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-bold shadow-md shadow-brand-500/20 transition-all active:scale-95"
          >
            <span>{{ $t('roadmap.load_more_chapters', { count: Math.min(30, filteredChapters.length - visibleChaptersCount), remaining: filteredChapters.length - visibleChaptersCount }) }}</span>
            <ChevronDown class="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- SECTION B: 30-DAY CORE CURRICULUM ROADMAP (FALLBACK / TOGGLE)             -->
    <!-- ========================================================================= -->
    <div v-else-if="activeMode === 'curriculum' || !focusStore.data?.pacer" class="space-y-6 sm:space-y-8">
      <!-- Header Banner -->
      <div class="p-4 sm:p-8 rounded-3xl bg-gradient-to-br from-indigo-50/80 via-white to-brand-50/50 dark:from-slate-900 dark:via-slate-900 dark:to-brand-950 border border-slate-200/90 dark:border-slate-800 text-slate-900 dark:text-white shadow-md dark:shadow-xl relative overflow-hidden transition-all duration-300">
        <div class="absolute -right-10 -bottom-10 w-64 h-64 bg-brand-500/10 rounded-full blur-3xl pointer-events-none"></div>
        
        <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-5 sm:gap-6">
          <div class="space-y-2 max-w-2xl">
            <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-brand-50 dark:bg-brand-500/20 border border-brand-200 dark:border-brand-500/30 text-brand-700 dark:text-brand-300 text-xs font-bold tracking-wide uppercase">
              <MapIcon class="w-3.5 h-3.5" />
              <span>{{ $t('roadmap.badge') }}</span>
            </div>
            <h1 class="text-xl sm:text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white">
              {{ $t('roadmap.title') }}
            </h1>
            <p class="text-slate-600 dark:text-slate-300 text-sm md:text-lg leading-relaxed">
              {{ $t('roadmap.subtitle') }}
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
                <span>{{ roadmapStore.roadmapData?.completedDaysCount ?? 0 }}</span>
                <span class="text-xs text-slate-500 dark:text-slate-400 font-medium">/ 30 {{ $t('roadmap.days') }}</span>
              </div>
              <div class="text-xs text-brand-600 dark:text-brand-400 font-bold mt-0.5">
                {{ roadmapStore.roadmapData?.overallProgressPercentage ?? 0 }}% {{ $t('roadmap.completed') }}
              </div>
            </div>
          </div>
        </div>

        <!-- Global Progress Bar -->
        <div class="mt-5 sm:mt-6 space-y-1.5">
          <div class="w-full h-2.5 bg-slate-200/80 dark:bg-slate-800 rounded-full overflow-hidden p-0.5 border border-slate-200 dark:border-slate-700/50">
            <div
              class="h-full bg-gradient-to-r from-brand-500 to-emerald-400 rounded-full transition-all duration-500 shadow-sm"
              :style="{ width: `${roadmapStore.roadmapData?.overallProgressPercentage ?? 0}%` }"
            ></div>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="roadmapStore.isLoading" class="flex flex-col items-center justify-center py-20 space-y-4">
        <div class="w-10 h-10 border-4 border-brand-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-slate-500 dark:text-slate-400 text-sm font-medium">{{ $t('roadmap.loading') }}</p>
      </div>

      <!-- Modules List -->
      <div v-else-if="roadmapStore.roadmapData" class="space-y-8 sm:space-y-10">
        <section
          v-for="module in roadmapStore.roadmapData.modules"
          :key="module.category"
          class="space-y-4 sm:space-y-5"
        >
          <!-- Module Header Card -->
          <div class="flex flex-col sm:flex-row sm:items-center justify-between p-4 sm:p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm gap-3 sm:gap-4">
            <div class="flex items-center gap-3 sm:gap-3.5">
              <div class="w-9 h-9 sm:w-10 sm:h-10 rounded-xl bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800 flex items-center justify-center text-brand-600 dark:text-brand-400 shrink-0">
                <component :is="getModuleIcon(module.category)" class="w-5 h-5" />
              </div>
              <div>
                <div class="flex items-center gap-2 flex-wrap">
                  <h2 class="text-sm sm:text-lg font-bold text-slate-900 dark:text-slate-100">
                    {{ module.moduleTitle }}
                  </h2>
                  <span class="text-xs px-2.5 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 font-semibold">
                    Days {{ module.startDay }}–{{ module.endDay }}
                  </span>
                </div>
                <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 line-clamp-1 mt-0.5">
                  {{ module.description }}
                </p>
              </div>
            </div>

            <!-- Module Progress -->
            <div class="flex items-center gap-3 shrink-0 self-end sm:self-auto">
              <span class="text-xs font-bold text-slate-600 dark:text-slate-400">
                {{ module.completedCount }}/{{ module.totalCount }} {{ $t('roadmap.completed') }}
              </span>
              <div class="w-16 sm:w-20 h-2 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                <div
                  class="h-full bg-brand-500 rounded-full transition-all duration-300"
                  :style="{ width: `${(module.completedCount / module.totalCount) * 100}%` }"
                ></div>
              </div>
            </div>
          </div>

          <!-- Day Nodes Grid -->
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div
              v-for="day in module.days"
              :key="day.dayOrder"
              @click="navigateToDay(day.dayOrder)"
              :class="[
                'p-4 sm:p-5 rounded-2xl border transition-all duration-200 cursor-pointer flex flex-col justify-between group relative overflow-hidden select-none',
                day.isActiveToday
                  ? 'bg-amber-500/5 dark:bg-amber-500/10 border-amber-500 dark:border-amber-400/80 shadow-md ring-2 ring-amber-500/20'
                  : day.isCompleted
                    ? 'bg-white dark:bg-slate-900/90 border-emerald-500/30 dark:border-emerald-500/30 hover:border-emerald-500 hover:shadow-sm'
                    : day.isUnlocked
                      ? 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 hover:border-brand-500 hover:shadow-sm'
                      : 'bg-slate-50/70 dark:bg-slate-950/40 border-slate-200/80 dark:border-slate-800/60 opacity-75 hover:opacity-100'
              ]"
            >
              <!-- Top indicator & Badges -->
              <div class="flex items-start justify-between gap-2 mb-3">
                <div class="flex items-center gap-2">
                  <span
                    :class="[
                      'w-7 h-7 rounded-lg text-xs font-black flex items-center justify-center shrink-0 transition-colors',
                      day.isActiveToday
                        ? 'bg-amber-500 text-white shadow-sm'
                        : day.isCompleted
                          ? 'bg-emerald-500 text-white'
                          : 'bg-slate-200 dark:bg-slate-800 text-slate-700 dark:text-slate-300'
                    ]"
                  >
                    {{ day.dayOrder }}
                  </span>

                  <span
                    :class="[
                      'text-xs px-2 py-0.5 rounded-md font-bold uppercase tracking-wider border',
                      getDifficultyColor(day.difficulty)
                    ]"
                  >
                    {{ getDifficultyLabel(day.difficulty) }}
                  </span>
                </div>

                <!-- Status Icon Badge -->
                <div>
                  <span
                    v-if="day.isActiveToday"
                    class="flex items-center gap-1 text-xs font-bold text-amber-600 dark:text-amber-400 bg-amber-100 dark:bg-amber-950/60 px-2.5 py-0.5 rounded-full border border-amber-300 dark:border-amber-700 animate-pulse"
                  >
                    <Flame class="w-3.5 h-3.5 text-amber-500" />
                    <span>{{ $t('roadmap.today') }}</span>
                  </span>
                  <span
                    v-else-if="day.isCompleted"
                    class="flex items-center gap-1 text-xs font-bold text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/60 px-2.5 py-0.5 rounded-full border border-emerald-200 dark:border-emerald-800"
                  >
                    <CheckCircle2 class="w-3.5 h-3.5 text-emerald-500" />
                    <span>{{ day.drillScore !== null ? `+${day.drillScore}` : 'Pass' }}</span>
                  </span>
                  <span
                    v-else-if="day.isUnlocked"
                    class="text-xs font-semibold text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-slate-800 px-2.5 py-0.5 rounded-full"
                  >
                    <Eye class="w-3.5 h-3.5 inline mr-0.5" />
                    <span>{{ $t('roadmap.ready') }}</span>
                  </span>
                  <span
                    v-else
                    class="text-xs font-semibold text-slate-400 dark:text-slate-600 bg-slate-100/50 dark:bg-slate-800/40 px-2.5 py-0.5 rounded-full"
                  >
                    <Lock class="w-3 h-3 inline mr-0.5" />
                    <span>{{ $t('roadmap.locked') }}</span>
                  </span>
                </div>
              </div>

              <!-- Title & Summary -->
              <div class="space-y-1.5 mb-4">
                <h3 class="font-bold text-sm sm:text-base text-slate-900 dark:text-slate-100 group-hover:text-brand-600 dark:group-hover:text-brand-400 transition-colors line-clamp-1">
                  {{ day.title }}
                </h3>
                <p class="text-sm text-slate-600 dark:text-slate-400 line-clamp-2 leading-relaxed">
                  {{ day.summary }}
                </p>
              </div>

              <!-- Action Link -->
              <div class="pt-2.5 border-t border-slate-100 dark:border-slate-800/80 flex items-center justify-between text-xs sm:text-sm font-semibold">
                <span
                  :class="[
                    day.isActiveToday
                      ? 'text-amber-600 dark:text-amber-400 font-bold'
                      : day.isCompleted
                        ? 'text-emerald-600 dark:text-emerald-400'
                        : 'text-slate-500 dark:text-slate-400 group-hover:text-brand-600 dark:group-hover:text-brand-400'
                  ]"
                >
                  {{ day.isActiveToday ? $t('roadmap.start_today') : day.isCompleted ? $t('roadmap.review_day') : $t('roadmap.view_lesson') }}
                </span>
                <ArrowRight class="w-3.5 h-3.5 text-slate-400 group-hover:text-brand-500 group-hover:translate-x-0.5 transition-transform" />
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
  </div>
</template>


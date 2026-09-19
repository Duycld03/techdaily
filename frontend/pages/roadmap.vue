<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { onClickOutside } from '@vueuse/core'
import { useRouter, useRoute } from 'vue-router'
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
  Award,
  BookOpen,
  Compass,
  ChevronDown,
  ChevronRight,
  Search
} from 'lucide-vue-next'
import { useRoadmapStore } from '~/stores/useRoadmapStore'
import { useDailyFocusStore } from '~/stores/useDailyFocusStore'
import { useLibraryStore } from '~/stores/useLibraryStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { useRoadmapViewMode } from '~/composables/useRoadmapViewMode'
import RoadmapViewSwitcher from '~/components/roadmap/RoadmapViewSwitcher.vue'
import RoadmapMindmapCanvas from '~/components/roadmap/RoadmapMindmapCanvas.vue'

const roadmapStore = useRoadmapStore()
const focusStore = useDailyFocusStore()
const libraryStore = useLibraryStore()
const authStore = useAuthStore()
const router = useRouter()
const route = useRoute()
const { locale, t } = useI18n()
const { viewMode } = useRoadmapViewMode()

const isCurriculumSelected = ref(false)
const selectedBookId = ref<string | null>(null)
const isTrackMenuOpen = ref(false)
const trackMenuRef = ref<HTMLElement | null>(null)
const isLoadingBookDetails = ref(false)

async function loadBookDetails(bookId: string) {
  isLoadingBookDetails.value = true
  try {
    await libraryStore.fetchBookById(bookId)
  } finally {
    isLoadingBookDetails.value = false
  }
}

onClickOutside(trackMenuRef, () => {
  if (isTrackMenuOpen.value) {
    isTrackMenuOpen.value = false
  }
})

onMounted(async () => {

  await Promise.all([
    roadmapStore.fetchRoadmap(),
    focusStore.fetchTodayFocus(undefined, undefined, locale.value)
  ])

  const queryBookId = route.query?.bookId as string | undefined
  const queryTrack = route.query?.track as string | undefined

  if (queryTrack === 'curriculum') {
    isCurriculumSelected.value = true
  } else if (queryBookId) {
    selectedBookId.value = queryBookId
    await loadBookDetails(queryBookId)
  } else if (focusStore.data?.pacer?.bookId) {
    selectedBookId.value = focusStore.data.pacer.bookId
    await loadBookDetails(focusStore.data.pacer.bookId)
  } else {
    isCurriculumSelected.value = true
  }
})



watch(
  () => focusStore.data?.pacer?.bookId,
  async (newBookId) => {
    if (newBookId && !isCurriculumSelected.value) {
      selectedBookId.value = newBookId
      await loadBookDetails(newBookId)
    }
  }
)

async function handleSelectBookTrack(bookId: string) {
  isTrackMenuOpen.value = false
  isCurriculumSelected.value = false
  selectedBookId.value = bookId

  if (focusStore.data?.pacer?.bookId !== bookId) {
    await focusStore.switchBook(bookId, locale.value)
  }
  await loadBookDetails(bookId)
}

function handleSelectCurriculumTrack() {
  isTrackMenuOpen.value = false
  isCurriculumSelected.value = true
}

const availableBookTracks = computed(() => {
  if (focusStore.data?.pacer?.availableBooks && focusStore.data.pacer.availableBooks.length > 0) {
    return focusStore.data.pacer.availableBooks
  }
  if (libraryStore.selectedBook) {
    return [
      {
        id: libraryStore.selectedBook.id,
        title: libraryStore.selectedBook.title,
        progressPercentage: libraryStore.selectedBook.progressPercentage || 0,
        totalChunks: libraryStore.selectedBook.totalChunks || 0,
        currentChunkOrder: focusStore.data?.pacer?.currentChunkOrder || 1,
        isActive: true
      }
    ]
  }
  return []
})

const currentTrackTitle = computed(() => {
  if (isCurriculumSelected.value) {
    return t('roadmap.curriculum_track')
  }
  if (libraryStore.selectedBook?.title) {
    return libraryStore.selectedBook.title
  }
  if (focusStore.data?.pacer?.bookTitle) {
    return focusStore.data.pacer.bookTitle
  }
  return t('roadmap.curriculum_track')
})

const headerTitle = computed(() => {
  if (isCurriculumSelected.value) {
    return t('roadmap.title')
  }
  return currentTrackTitle.value
})

const headerSubtitle = computed(() => {
  if (isCurriculumSelected.value) {
    return t('roadmap.subtitle')
  }
  if (focusStore.data?.pacer?.chapterTitle) {
    return focusStore.data.pacer.chapterTitle
  }
  if (libraryStore.selectedBook?.authorOrSourceUrl) {
    return libraryStore.selectedBook.authorOrSourceUrl
  }
  return t('roadmap.active_book')
})

const metricCompletedCount = computed(() => {
  if (isCurriculumSelected.value) {
    return roadmapStore.roadmapData?.completedDaysCount ?? 0
  }
  if (focusStore.data?.pacer) {
    return Math.max(0, focusStore.data.pacer.currentChunkOrder - 1)
  }
  return 0
})

const metricTotalCount = computed(() => {
  if (isCurriculumSelected.value) {
    return 30
  }
  if (focusStore.data?.pacer) {
    return focusStore.data.pacer.totalChunks
  }
  return libraryStore.selectedBook?.totalChunks ?? 0
})

const metricUnitLabel = computed(() => {
  return isCurriculumSelected.value ? t('roadmap.days') : t('roadmap.slices')
})

const metricProgressPercentage = computed(() => {
  if (isCurriculumSelected.value) {
    return roadmapStore.roadmapData?.overallProgressPercentage ?? 0
  }
  if (focusStore.data?.pacer) {
    return focusStore.data.pacer.progressPercentage
  }
  if (metricTotalCount.value > 0) {
    return Math.round((metricCompletedCount.value / metricTotalCount.value) * 100)
  }
  return 0
})

interface ChapterSlice {
  id: string
  chunkOrder: number
  sliceTitle: string
  chapterTitle: string
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

function sanitizeSummary(text?: string): string {
  if (!text) return ''
  let clean = text.replace(/^[ \t]*#{1,6}\s+[^\r\n]*/gm, '')
  clean = clean.replace(/```[\s\S]*?```/g, '')
  clean = clean.replace(/\b\d{1,2}\/\d{1,2}\/\d{4}\b/g, '')
  clean = clean.replace(/(?:Important\s+)?This information relates to a pre-release product[^\n.]*\.[^\n.]*\./gi, '')
  clean = clean.replace(/\s+/g, ' ').trim()
  if (!clean || clean.length < 10) {
    return ''
  }
  return clean.length > 200 ? clean.substring(0, 197) + '...' : clean
}

function parseSliceTitle(rawTitle?: string) {
  const title = (rawTitle || 'Chapter Overview').trim()
  if (title.includes(':')) {
    const parts = title.split(':')
    const prefix = parts[0].trim()
    const rest = parts.slice(1).join(':').trim()
    if (prefix.length > 0 && prefix.length <= 45) {
      return {
        moduleName: prefix,
        sliceTitle: rest || prefix,
        hasExplicitPrefix: true
      }
    }
  }

  if (title.includes(' - ')) {
    const parts = title.split(' - ')
    const prefix = parts[0].trim()
    const rest = parts.slice(1).join(' - ').trim()
    if (prefix.length > 0 && prefix.length <= 45) {
      return {
        moduleName: prefix,
        sliceTitle: rest || prefix,
        hasExplicitPrefix: true
      }
    }
  }

  // Check dot delimiter (e.g. "Registry.ClassesRoot Field", "RegistryKey.Handle Property")
  if (title.includes('.') && !title.includes(' ')) {
    const parts = title.split('.')
    const prefix = parts[0].trim()
    const rest = parts.slice(1).join('.').trim()
    if (prefix.length > 0 && prefix.length <= 35 && /^[A-Za-z0-9_]+$/.test(prefix)) {
      return {
        moduleName: prefix,
        sliceTitle: rest || prefix,
        hasExplicitPrefix: true
      }
    }
  } else if (title.includes('.')) {
    const firstSpace = title.indexOf(' ')
    const beforeSpace = title.substring(0, firstSpace)
    if (beforeSpace.includes('.')) {
      const dotIndex = beforeSpace.indexOf('.')
      const prefix = beforeSpace.substring(0, dotIndex).trim()
      const rest = (beforeSpace.substring(dotIndex + 1) + title.substring(firstSpace)).trim()
      if (prefix.length > 0 && prefix.length <= 35 && /^[A-Za-z0-9_]+$/.test(prefix)) {
        return {
          moduleName: prefix,
          sliceTitle: rest || prefix,
          hasExplicitPrefix: true
        }
      }
    }
  }

  const baseTitle = title.replace(/\s*\((?:Section|Part)\s+\d+\)/gi, '').trim()
  return {
    moduleName: baseTitle || title,
    sliceTitle: title,
    hasExplicitPrefix: false
  }
}

const chapterMilestones = computed<ChapterMilestone[]>(() => {
  if (!libraryStore.selectedBook?.chunks || !focusStore.data?.pacer) return []
  const pacer = focusStore.data.pacer
  const rawChunks = libraryStore.selectedBook.chunks
  const chunks = [...rawChunks].sort((a, b) => a.chunkOrder - b.chunkOrder)

  const list: ChapterMilestone[] = []
  let currentGroupName = ''
  let currentGroupChunks: typeof chunks = []
  let currentGroupIsStandalone = false

  const shouldClusterStandalone = chunks.length > 8
  const MAX_STANDALONE_CLUSTER_SIZE = 4

  function flushGroup() {
    if (currentGroupChunks.length === 0) return

    const slices: ChapterSlice[] = currentGroupChunks.map((c) => {
      const isCompleted = c.chunkOrder < pacer.currentChunkOrder
      const isActiveToday = c.chunkOrder === pacer.currentChunkOrder
      const isUpcoming = c.chunkOrder > pacer.currentChunkOrder
      const parsed = parseSliceTitle(c.chapterTitle)

      return {
        id: c.id,
        chunkOrder: c.chunkOrder,
        sliceTitle: parsed.sliceTitle,
        chapterTitle: c.chapterTitle || 'Chapter Overview',
        summaryMarkdown: sanitizeSummary(c.summaryMarkdown) || 'Architectural reading slice and trade-off scenario.',
        estimatedReadMinutes: c.estimatedReadMinutes,
        isCompleted,
        isActiveToday,
        isUpcoming
      }
    })

    const completedSlicesCount = slices.filter((s) => s.isCompleted).length
    const totalSlicesCount = slices.length
    const isCompleted = completedSlicesCount === totalSlicesCount
    const isActive = slices.some((s) => s.isActiveToday)

    let displayTitle = currentGroupName
    if (currentGroupIsStandalone && currentGroupChunks.length > 1) {
      const firstTitle = parseSliceTitle(currentGroupChunks[0].chapterTitle).sliceTitle
      displayTitle = `${firstTitle} & Related Topics`
    }

    list.push({
      chapterTitle: displayTitle,
      chapterIndex: list.length + 1,
      slices,
      isCompleted,
      isActive,
      completedSlicesCount,
      totalSlicesCount
    })

    currentGroupChunks = []
    currentGroupIsStandalone = false
  }

  for (const chunk of chunks) {
    const parsed = parseSliceTitle(chunk.chapterTitle)
    if (parsed.hasExplicitPrefix) {
      if (parsed.moduleName === currentGroupName && !currentGroupIsStandalone && currentGroupChunks.length > 0) {
        currentGroupChunks.push(chunk)
      } else {
        flushGroup()
        currentGroupName = parsed.moduleName
        currentGroupIsStandalone = false
        currentGroupChunks = [chunk]
      }
    } else {
      if (
        shouldClusterStandalone &&
        currentGroupIsStandalone &&
        currentGroupChunks.length < MAX_STANDALONE_CLUSTER_SIZE
      ) {
        currentGroupChunks.push(chunk)
      } else {
        flushGroup()
        currentGroupName = parsed.moduleName
        currentGroupIsStandalone = shouldClusterStandalone
        currentGroupChunks = [chunk]
      }
    }
  }

  flushGroup()
  return list
})

const chapterSearch = ref('')
const visibleChaptersCount = ref(30)
const expandedChapters = ref<Set<number>>(new Set())

watch(
  chapterMilestones,
  (milestones) => {
    if (expandedChapters.value.size === 0) {
      const active = milestones.find((m) => m.isActive)
      if (active) {
        expandedChapters.value.add(active.chapterIndex)
      } else if (milestones.length > 0) {
        expandedChapters.value.add(milestones[0].chapterIndex)
      }
    }
  },
  { immediate: true }
)

const filteredChapters = computed(() => {
  if (!chapterSearch.value.trim()) return chapterMilestones.value
  const query = chapterSearch.value.toLowerCase().trim()
  return chapterMilestones.value.filter(
    (c) =>
      c.chapterTitle.toLowerCase().includes(query) ||
      c.slices.some((s) => s.sliceTitle.toLowerCase().includes(query) || s.summaryMarkdown.toLowerCase().includes(query))
  )
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
  const bookId = selectedBookId.value || focusStore.data?.pacer?.bookId
  if (bookId) {
    router.push(`/today?bookId=${bookId}&chunkOrder=${chunkOrder}`)
  }
}

function navigateToActiveSlice() {
  const bookId = selectedBookId.value || focusStore.data?.pacer?.bookId
  const chunkOrder = focusStore.data?.pacer?.currentChunkOrder || 1
  if (bookId) {
    router.push(`/today?bookId=${bookId}&chunkOrder=${chunkOrder}`)
  }
}

function navigateToDay(dayOrder: number) {
  router.push(`/today?day=${dayOrder}`)
}

const categoryIcons = [
  Layers, // 0: FrontendWeb
  Cpu, // 1: BackendRuntime
  Database, // 2: DatabaseStorage
  Network // 3: SystemDesign
]

function getModuleIcon(category: number) {
  return categoryIcons[category] || Layers
}

function getDifficultyLabel(diff: number) {
  switch (diff) {
    case 0:
      return 'Intermediate'
    case 1:
      return 'Senior'
    case 2:
      return 'Lead Architect'
    default:
      return 'Senior'
  }
}

function getDifficultyColor(diff: number) {
  switch (diff) {
    case 0:
      return 'bg-sky-50 dark:bg-sky-950/40 text-sky-700 dark:text-sky-300 border-sky-200 dark:border-sky-800'
    case 1:
      return 'bg-brand-50 dark:bg-brand-950/40 text-brand-700 dark:text-brand-300 border-brand-200 dark:border-brand-800'
    case 2:
      return 'bg-purple-50 dark:bg-purple-950/40 text-purple-700 dark:text-purple-300 border-purple-200 dark:border-purple-800'
    default:
      return 'bg-slate-50 dark:bg-canvas-subtle text-slate-700 dark:text-slate-300 border-slate-200 dark:border-white/[0.08]'
  }
}
</script>

<template>
  <div class="max-w-6xl mx-auto px-3 sm:px-6 py-5 sm:py-8 space-y-6 sm:space-y-8 animate-in fade-in duration-300">
    <!-- ========================================================================= -->
    <!-- UNIFIED HEADER BANNER WITH SYNCHRONIZED TRACK SWITCHER                     -->
    <!-- ========================================================================= -->
    <div
      :class="[
        'p-4 sm:p-8 rounded-3xl bg-gradient-to-br from-indigo-50/80 via-white to-brand-50/50 dark:from-canvas-subtle dark:via-canvas dark:to-brand-950/40 border border-slate-200/90 dark:border-white/[0.08] text-slate-900 dark:text-white shadow-md dark:shadow-xl relative overflow-visible transition-all duration-300',
        isTrackMenuOpen ? 'z-40' : 'z-20'
      ]"
    >
      <!-- Isolated decorative background with overflow containment -->
      <div class="absolute inset-0 rounded-3xl overflow-hidden pointer-events-none">
        <div class="absolute -right-10 -bottom-10 w-64 h-64 bg-brand-500/10 rounded-full blur-3xl"></div>
      </div>

      <div class="relative flex flex-col md:flex-row md:items-center justify-between gap-5 sm:gap-6">
        <div class="space-y-3 max-w-2xl">
          <!-- Track Switcher Dropdown Anchor -->
          <div class="flex items-center gap-2 flex-wrap">
            <div
              class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-brand-50 dark:bg-brand-500/20 border border-brand-200 dark:border-brand-500/30 text-brand-700 dark:text-brand-300 text-xs font-bold tracking-wide uppercase whitespace-nowrap shrink-0"
            >
              <MapIcon class="w-3.5 h-3.5 shrink-0" />
              <span>{{ $t('roadmap.badge') }}</span>
            </div>

            <!-- Track Switcher Dropdown Menu -->
            <div ref="trackMenuRef" class="relative z-30">
              <button
                type="button"
                data-testid="track-switcher-btn"
                @click="isTrackMenuOpen = !isTrackMenuOpen"
                class="inline-flex items-center gap-2 px-3 py-1.5 rounded-xl bg-white/90 dark:bg-canvas-elevated hover:bg-white dark:hover:bg-white/[0.06] border border-slate-200/90 dark:border-white/[0.08] text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200 transition-all shadow-sm active:scale-95 whitespace-nowrap shrink-0"
              >
                <component
                  :is="isCurriculumSelected ? Compass : BookOpen"
                  class="w-4 h-4 text-brand-600 dark:text-brand-400 shrink-0"
                />
                <span class="max-w-[140px] sm:max-w-[220px] md:max-w-[280px] truncate">
                  {{ currentTrackTitle }}
                </span>
                <ChevronDown
                  :class="[
                    'w-3.5 h-3.5 text-slate-400 transition-transform duration-200 shrink-0',
                    isTrackMenuOpen ? 'rotate-180' : ''
                  ]"
                />
              </button>

              <!-- Dropdown Popover Menu -->
              <div
                v-if="isTrackMenuOpen"
                data-testid="track-menu-popover"
                class="absolute left-0 top-full mt-2 w-72 sm:w-84 max-w-[calc(100vw-2rem)] max-h-[calc(100vh-14rem)] overflow-y-auto rounded-2xl bg-white dark:bg-canvas-elevated border border-slate-200 dark:border-white/[0.08] shadow-2xl p-2 z-50 animate-in fade-in zoom-in-95 duration-150 space-y-1"
              >
                <!-- In-Progress Document Tracks -->
                <div
                  v-if="availableBookTracks.length > 0"
                  class="px-3 py-1.5 text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 whitespace-nowrap shrink-0"
                >
                  {{ $t('roadmap.in_progress_tracks') }}
                </div>

                <div v-if="availableBookTracks.length > 0" class="max-h-60 overflow-y-auto space-y-1">
                  <button
                    v-for="b in availableBookTracks"
                    :key="b.id"
                    type="button"
                    :data-testid="`track-book-option-${b.id}`"
                    @click="handleSelectBookTrack(b.id)"
                    :class="[
                      'w-full text-left p-2.5 rounded-xl text-xs sm:text-sm transition-all flex flex-col gap-1.5 group',
                      selectedBookId === b.id && !isCurriculumSelected
                        ? 'bg-brand-50/80 dark:bg-brand-950/40 border border-brand-200 dark:border-brand-800/80 text-brand-950 dark:text-brand-100 font-bold'
                        : 'hover:bg-slate-100 dark:hover:bg-white/[0.06] text-slate-700 dark:text-slate-300'
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
                      <div class="flex-1 h-1.5 bg-slate-200 dark:bg-canvas-subtle rounded-full overflow-hidden">
                        <div
                          class="h-full bg-brand-500 rounded-full transition-all duration-300"
                          :style="{ width: `${b.progressPercentage}%` }"
                        ></div>
                      </div>
                      <span class="font-mono shrink-0 whitespace-nowrap">
                        {{ b.currentChunkOrder }}/{{ b.totalChunks }} ({{ b.progressPercentage }}%)
                      </span>
                    </div>
                  </button>
                </div>

                <!-- 30-Day Senior Curriculum Option -->
                <div class="pt-2 border-t border-slate-100 dark:border-white/[0.08]">
                  <button
                    type="button"
                    data-testid="track-curriculum-option"
                    @click="handleSelectCurriculumTrack"
                    :class="[
                      'w-full text-left p-2.5 rounded-xl text-xs sm:text-sm transition-all flex items-center justify-between group',
                      isCurriculumSelected
                        ? 'bg-brand-50/80 dark:bg-brand-950/40 border border-brand-200 dark:border-brand-800/80 text-brand-950 dark:text-brand-100 font-bold'
                        : 'hover:bg-slate-100 dark:hover:bg-white/[0.06] text-slate-700 dark:text-slate-300'
                    ]"
                  >
                    <div class="flex items-center gap-2.5 min-w-0">
                      <Compass class="w-4 h-4 text-brand-500 shrink-0" />
                      <div class="min-w-0">
                        <div class="font-bold truncate">{{ $t('roadmap.curriculum_track') }}</div>
                        <div class="text-xs text-slate-400 dark:text-slate-500 truncate">
                          {{ $t('roadmap.curriculum_track_desc') }}
                        </div>
                      </div>
                    </div>
                    <span class="text-xs font-mono text-slate-400 shrink-0 whitespace-nowrap">
                      {{ roadmapStore.roadmapData?.completedDaysCount ?? 0 }}/30 ({{
                        roadmapStore.roadmapData?.overallProgressPercentage ?? 0
                      }}%)
                    </span>
                  </button>
                </div>

                <!-- Browse Library Bridge -->
                <div class="pt-2 border-t border-slate-100 dark:border-white/[0.08]">
                  <NuxtLink
                    to="/library"
                    data-testid="track-browse-library-link"
                    @click="isTrackMenuOpen = false"
                    class="flex items-center justify-between px-3 py-2 rounded-xl text-xs sm:text-sm font-bold text-brand-600 dark:text-brand-400 hover:bg-brand-50 dark:hover:bg-white/[0.06] transition-colors whitespace-nowrap shrink-0"
                  >
                    <span>+ {{ $t('roadmap.browse_library') }}</span>
                    <ArrowRight class="w-3.5 h-3.5 shrink-0" />
                  </NuxtLink>
                </div>
              </div>
            </div>
          </div>

          <!-- Header Title & Subtitle -->
          <h1 class="text-xl sm:text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white">
            {{ headerTitle }}
          </h1>
          <p class="text-slate-600 dark:text-slate-300 text-sm md:text-lg leading-relaxed">
            {{ headerSubtitle }}
          </p>
        </div>

        <!-- Metric Counter Card -->
        <div
          class="flex items-center gap-3.5 sm:gap-4 bg-white/90 dark:bg-canvas-subtle/80 backdrop-blur-md p-3.5 sm:p-5 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] shadow-sm shrink-0"
        >
          <div
            class="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-brand-50 dark:bg-brand-500/20 border border-brand-200 dark:border-brand-500/30 flex items-center justify-center text-brand-600 dark:text-brand-400 shrink-0"
          >
            <Award class="w-5 h-5 sm:w-6 sm:h-6 shrink-0" />
          </div>
          <div>
            <div class="text-xs text-slate-500 dark:text-slate-400 uppercase tracking-wider font-semibold">
              {{ $t('roadmap.progress') }}
            </div>
            <div class="text-xl sm:text-2xl font-black text-slate-900 dark:text-white flex items-baseline gap-1.5">
              <span>{{ metricCompletedCount }}</span>
              <span class="text-xs text-slate-500 dark:text-slate-400 font-medium whitespace-nowrap">
                / {{ metricTotalCount }} {{ metricUnitLabel }}
              </span>
            </div>
            <div class="text-xs text-brand-600 dark:text-brand-400 font-bold mt-0.5 flex items-center gap-2">
              <span class="whitespace-nowrap">{{ metricProgressPercentage }}% {{ $t('roadmap.completed') }}</span>
              <template v-if="!isCurriculumSelected && estDaysRemaining > 0">
                <span class="text-slate-400 dark:text-slate-500 font-normal">•</span>
                <span class="text-slate-600 dark:text-slate-300 font-medium whitespace-nowrap">
                  {{ $t('roadmap.est_days', { days: estDaysRemaining }) }}
                </span>
              </template>
            </div>
          </div>
        </div>
      </div>

      <!-- Global Progress Bar -->
      <div class="mt-5 sm:mt-6 space-y-1.5">
        <div
          class="w-full h-2.5 bg-slate-200/80 dark:bg-canvas-subtle rounded-full overflow-hidden p-0.5 border border-slate-200 dark:border-white/[0.08]"
        >
          <div
            class="h-full bg-gradient-to-r from-brand-600 via-brand-500 to-brand-400 rounded-full transition-all duration-500 shadow-sm"
            :style="{ width: `${metricProgressPercentage}%` }"
          ></div>
        </div>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- ROADMAP DUAL-VIEW SWITCHER (TIMELINE VS MINDMAP)                          -->
    <!-- ========================================================================= -->
    <div class="flex items-center justify-between gap-4 flex-wrap">
      <RoadmapViewSwitcher v-model="viewMode" />
    </div>

    <!-- ========================================================================= -->
    <!-- VIEW CONTAINER: MINDMAP VIEW                                              -->
    <!-- ========================================================================= -->
    <div v-if="viewMode === 'mindmap'" key="mindmap-view" class="animate-in fade-in duration-300">
      <div
        v-if="isLoadingBookDetails || (isCurriculumSelected && roadmapStore.isLoading)"
        class="flex flex-col items-center justify-center py-20 space-y-4"
      >
        <div class="w-10 h-10 border-4 border-brand-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-slate-500 dark:text-slate-400 text-sm font-medium">{{ $t('roadmap.loading') }}</p>
      </div>
      <RoadmapMindmapCanvas
        v-else
        :selected-book="libraryStore.selectedBook"
        :chapter-milestones="chapterMilestones"
        :roadmap-data="roadmapStore.roadmapData"
        :is-curriculum-selected="isCurriculumSelected"
        :active-book-id="selectedBookId || focusStore.data?.pacer?.bookId"
        :current-chunk-order="focusStore.data?.pacer?.currentChunkOrder"
        :active-day-order="roadmapStore.roadmapData?.currentActiveDay || 1"
      />
    </div>

    <!-- ========================================================================= -->
    <!-- VIEW CONTAINER: TIMELINE VIEW                                             -->
    <!-- ========================================================================= -->
    <div v-else key="timeline-view" class="space-y-6 sm:space-y-8 animate-in fade-in duration-300">
      <!-- 1. ACTIVE DOCUMENT BOOK TRACK -->
      <template v-if="!isCurriculumSelected">
        <!-- Book Chapter Milestones Loading State -->
        <div v-if="isLoadingBookDetails" class="flex flex-col items-center justify-center py-20 space-y-4">
          <div class="w-10 h-10 border-4 border-brand-500 border-t-transparent rounded-full animate-spin"></div>
          <p class="text-slate-500 dark:text-slate-400 text-sm font-medium">{{ $t('roadmap.loading') }}</p>
        </div>

        <!-- Chapter Milestone Cards List -->
        <div v-else class="space-y-6 sm:space-y-8">
          <!-- Search & Controls Bar -->
          <div
            class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 p-2 bg-slate-100/80 dark:bg-canvas-subtle/80 rounded-2xl border border-slate-200/80 dark:border-white/[0.08]"
          >
            <div class="relative flex-1 max-w-md">
              <Search class="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
              <input
                v-model="chapterSearch"
                type="text"
                :placeholder="$t('roadmap.search_chapters')"
                class="w-full pl-10 pr-4 py-2 bg-white dark:bg-canvas-elevated border border-slate-200/80 dark:border-white/[0.08] rounded-xl text-xs sm:text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 focus:border-brand-500 focus:outline-none transition-colors"
              />
            </div>
            <div class="flex items-center gap-2 self-end sm:self-auto">
              <button
                @click="expandAll"
                type="button"
                class="px-3 py-1.5 rounded-lg border border-slate-200/80 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-white/[0.04] text-xs font-semibold text-slate-600 dark:text-slate-400 transition-colors whitespace-nowrap shrink-0"
              >
                {{ $t('roadmap.expand_all') }}
              </button>
              <button
                @click="collapseAll"
                type="button"
                class="px-3 py-1.5 rounded-lg border border-slate-200/80 dark:border-white/[0.08] hover:bg-slate-100 dark:hover:bg-white/[0.04] text-xs font-semibold text-slate-600 dark:text-slate-400 transition-colors whitespace-nowrap shrink-0"
              >
                {{ $t('roadmap.collapse_all') }}
              </button>
            </div>
          </div>

          <!-- Continuous Vertical Spine Container -->
          <div class="relative pl-6 sm:pl-10 space-y-6 sm:space-y-8">
            <!-- Timeline Spine Line -->
            <div class="absolute left-2.5 sm:left-4.5 top-5 bottom-5 w-0.5 bg-gradient-to-b from-brand-500/50 via-brand-500/20 to-slate-200/60 dark:to-white/[0.08] pointer-events-none"></div>

            <section
              v-for="chapter in displayedChapters"
              :key="chapter.chapterTitle"
              class="relative space-y-4 sm:space-y-5"
            >
              <!-- Timeline Milestone Node Bead on Spine -->
              <div
                :class="[
                  'absolute -left-6 sm:-left-10 top-5 -translate-x-1/2 w-5 h-5 sm:w-6 sm:h-6 rounded-full flex items-center justify-center text-[10px] font-bold transition-all z-10 select-none',
                  chapter.isCompleted
                    ? 'bg-brand-600 text-white ring-4 ring-slate-50 dark:ring-canvas shadow-sm'
                    : chapter.isActive
                      ? 'bg-brand-600 text-white ring-4 ring-slate-50 dark:ring-canvas shadow-md shadow-brand-500/40 animate-pulse'
                      : 'bg-slate-200 dark:bg-canvas-elevated text-slate-500 dark:text-slate-400 border border-slate-300 dark:border-white/[0.12] ring-4 ring-slate-50 dark:ring-canvas'
                ]"
              >
                <CheckCircle2 v-if="chapter.isCompleted" class="w-3 h-3" />
                <Flame v-else-if="chapter.isActive" class="w-3 h-3 text-amber-300" />
                <span v-else>{{ chapter.chapterIndex }}</span>
              </div>

              <!-- Chapter Milestone Header Card (Clickable Accordion) -->
              <div
                @click="toggleChapter(chapter.chapterIndex)"
                class="flex flex-col sm:flex-row sm:items-center justify-between p-4 sm:p-5 rounded-2xl bg-white dark:bg-canvas-subtle border border-slate-200/90 dark:border-white/[0.08] shadow-sm gap-3 sm:gap-4 cursor-pointer hover:border-brand-500/50 dark:hover:border-white/[0.16] transition-all select-none"
              >
              <div class="flex items-center gap-3 sm:gap-3.5">
                <div
                  :class="[
                    'w-9 h-9 sm:w-10 sm:h-10 rounded-xl flex items-center justify-center font-bold text-sm shrink-0 transition-colors',
                    chapter.isCompleted
                      ? 'bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800 text-brand-600 dark:text-brand-400'
                      : chapter.isActive
                        ? 'bg-amber-50 dark:bg-amber-950/60 border border-amber-200 dark:border-amber-800 text-amber-600 dark:text-amber-400'
                        : 'bg-slate-100 dark:bg-canvas-elevated border border-slate-200 dark:border-white/[0.08] text-slate-600 dark:text-slate-300'
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
                      class="text-xs px-2.5 py-0.5 rounded-full bg-amber-100 dark:bg-amber-950/60 border border-amber-300 dark:border-amber-800 text-amber-800 dark:text-amber-300 font-bold flex items-center gap-1 whitespace-nowrap shrink-0"
                    >
                      <Flame class="w-3 h-3 text-amber-500 shrink-0" />
                      <span>{{ $t('roadmap.today') }}</span>
                    </span>
                  </div>
                  <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-0.5">
                    {{ chapter.completedSlicesCount }}/{{ chapter.totalSlicesCount }} {{ $t('roadmap.slices') }}
                    {{ $t('roadmap.completed') }}
                  </p>
                </div>
              </div>

              <!-- Chapter Progress & Jump Action -->
              <div class="flex items-center gap-3 shrink-0 self-end sm:self-auto" @click.stop>
                <div class="w-20 sm:w-28 h-2 bg-slate-100 dark:bg-canvas-elevated rounded-full overflow-hidden">
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
                  <component
                    :is="expandedChapters.has(chapter.chapterIndex) ? ChevronDown : ChevronRight"
                    class="w-5 h-5 transition-transform"
                  />
                </button>
              </div>
            </div>

            <!-- Slices Grid (Collapsible) -->
            <div
              v-show="expandedChapters.has(chapter.chapterIndex)"
              class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 animate-in fade-in duration-200"
            >
              <div
                v-for="slice in chapter.slices"
                :key="slice.id"
                @click="navigateToSlice(slice.chunkOrder)"
                :class="[
                  'p-4 sm:p-5 rounded-2xl border transition-all duration-200 cursor-pointer flex flex-col justify-between group relative overflow-hidden select-none',
                  slice.isActiveToday
                    ? 'bg-brand-500/10 dark:bg-brand-500/15 border-brand-500 dark:border-brand-400 shadow-lg shadow-brand-500/10 ring-2 ring-brand-500/30'
                    : slice.isCompleted
                      ? 'bg-white dark:bg-canvas-subtle border-brand-500/40 dark:border-brand-500/40 hover:border-brand-500 hover:shadow-sm'
                      : 'bg-slate-50/70 dark:bg-canvas-elevated/40 border-slate-200/80 dark:border-white/[0.06] hover:border-brand-500/40 hover:shadow-sm'
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
                            ? 'bg-brand-600 text-white'
                            : 'bg-slate-200 dark:bg-canvas-elevated text-slate-700 dark:text-slate-300'
                      ]"
                    >
                      #{{ slice.chunkOrder }}
                    </span>

                    <span
                      class="text-xs px-2 py-0.5 rounded-md font-bold uppercase tracking-wider border bg-brand-50 dark:bg-brand-950/40 text-brand-700 dark:text-brand-300 border-brand-200 dark:border-brand-800 whitespace-nowrap shrink-0"
                    >
                      {{ $t('roadmap.slice') }} {{ slice.chunkOrder }}
                    </span>
                  </div>

                  <!-- Status Icon Badge -->
                  <div>
                    <span
                      v-if="slice.isActiveToday"
                      class="flex items-center gap-1 text-xs font-bold text-amber-600 dark:text-amber-400 bg-amber-100 dark:bg-amber-950/60 px-2.5 py-0.5 rounded-full border border-amber-300 dark:border-amber-700 animate-pulse whitespace-nowrap shrink-0"
                    >
                      <Flame class="w-3.5 h-3.5 text-amber-500 shrink-0" />
                      <span>{{ $t('roadmap.today') }}</span>
                    </span>
                    <span
                      v-else-if="slice.isCompleted"
                      class="flex items-center gap-1 text-xs font-bold text-brand-600 dark:text-brand-400 bg-brand-50 dark:bg-brand-950/60 px-2.5 py-0.5 rounded-full border border-brand-200 dark:border-brand-800 whitespace-nowrap shrink-0"
                    >
                      <CheckCircle2 class="w-3.5 h-3.5 text-brand-500 shrink-0" />
                      <span>Pass</span>
                    </span>
                    <span
                      v-else
                      class="text-xs font-semibold text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-canvas-elevated border border-transparent dark:border-white/[0.06] px-2.5 py-0.5 rounded-full whitespace-nowrap shrink-0"
                    >
                      <Eye class="w-3.5 h-3.5 inline mr-0.5 shrink-0" />
                      <span>{{ $t('roadmap.ready') }}</span>
                    </span>
                  </div>
                </div>

                <!-- Summary Preview -->
                <div class="space-y-1.5 mb-4">
                  <h4
                    v-if="slice.sliceTitle"
                    class="text-sm font-bold text-slate-900 dark:text-slate-100 line-clamp-1 group-hover:text-brand-600 dark:group-hover:text-brand-400 transition-colors"
                  >
                    {{ slice.sliceTitle }}
                  </h4>
                  <p class="text-sm text-slate-600 dark:text-slate-400 line-clamp-3 leading-relaxed">
                    {{ slice.summaryMarkdown }}
                  </p>
                </div>

                <!-- Action Link -->
                <div
                  class="pt-2.5 border-t border-slate-100 dark:border-white/[0.08] flex items-center justify-between text-xs sm:text-sm font-semibold"
                >
                  <span
                    :class="[
                      slice.isActiveToday
                        ? 'text-amber-600 dark:text-amber-400 font-bold'
                        : slice.isCompleted
                          ? 'text-brand-600 dark:text-brand-400'
                          : 'text-slate-500 dark:text-slate-400 group-hover:text-brand-600 dark:group-hover:text-brand-400'
                    ]"
                  >
                    {{
                      slice.isActiveToday
                        ? $t('roadmap.start_today')
                        : slice.isCompleted
                          ? $t('roadmap.review_day')
                          : $t('roadmap.view_lesson')
                    }}
                  </span>
                  <ArrowRight
                    class="w-3.5 h-3.5 text-slate-400 group-hover:text-brand-500 group-hover:translate-x-0.5 transition-transform shrink-0"
                  />
                </div>
              </div>
            </div>
          </section>
          </div>
          <!-- Load More Chapters Button -->
          <div v-if="filteredChapters.length > visibleChaptersCount" class="flex justify-center pt-4 pb-6">
            <button
              @click="loadMoreChapters"
              type="button"
              class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-bold shadow-md shadow-brand-500/20 transition-all active:scale-95 whitespace-nowrap shrink-0"
            >
              <span>{{
                $t('roadmap.load_more_chapters', {
                  count: Math.min(30, filteredChapters.length - visibleChaptersCount),
                  remaining: filteredChapters.length - visibleChaptersCount
                })
              }}</span>
              <ChevronDown class="w-4 h-4 shrink-0" />
            </button>
          </div>
        </div>
      </template>

      <!-- 2. 30-DAY CORE CURRICULUM TRACK -->
      <template v-else>
        <!-- Loading State -->
        <div v-if="roadmapStore.isLoading" class="flex flex-col items-center justify-center py-20 space-y-4">
          <div class="w-10 h-10 border-4 border-brand-500 border-t-transparent rounded-full animate-spin"></div>
          <p class="text-slate-500 dark:text-slate-400 text-sm font-medium">{{ $t('roadmap.loading') }}</p>
        </div>

        <!-- Modules List -->
        <div v-else-if="roadmapStore.roadmapData" class="space-y-8 sm:space-y-10">
          <!-- Continuous Vertical Spine Container -->
          <div class="relative pl-6 sm:pl-10 space-y-8 sm:space-y-10">
            <!-- Timeline Spine Line -->
            <div class="absolute left-2.5 sm:left-4.5 top-5 bottom-5 w-0.5 bg-gradient-to-b from-brand-500/50 via-brand-500/20 to-slate-200/60 dark:to-white/[0.08] pointer-events-none"></div>

            <section
              v-for="module in roadmapStore.roadmapData.modules"
              :key="module.category"
              class="relative space-y-4 sm:space-y-5"
            >
              <!-- Timeline Milestone Node Bead on Spine -->
              <div
                :class="[
                  'absolute -left-6 sm:-left-10 top-5 -translate-x-1/2 w-5 h-5 sm:w-6 sm:h-6 rounded-full flex items-center justify-center text-[10px] font-bold transition-all z-10 select-none',
                  module.completedCount === module.totalCount
                    ? 'bg-brand-600 text-white ring-4 ring-slate-50 dark:ring-canvas shadow-sm'
                    : module.completedCount > 0
                      ? 'bg-brand-600 text-white ring-4 ring-slate-50 dark:ring-canvas shadow-md shadow-brand-500/40'
                      : 'bg-slate-200 dark:bg-canvas-elevated text-slate-500 dark:text-slate-400 border border-slate-300 dark:border-white/[0.12] ring-4 ring-slate-50 dark:ring-canvas'
                ]"
              >
                <CheckCircle2 v-if="module.completedCount === module.totalCount" class="w-3 h-3" />
                <span v-else>{{ module.category + 1 }}</span>
              </div>

              <!-- Module Header Card -->
              <div
                class="flex flex-col sm:flex-row sm:items-center justify-between p-4 sm:p-5 rounded-2xl bg-white dark:bg-canvas-subtle border border-slate-200/90 dark:border-white/[0.08] shadow-sm gap-3 sm:gap-4"
              >
                <div class="flex items-center gap-3 sm:gap-3.5">
                  <div
                    class="w-9 h-9 sm:w-10 sm:h-10 rounded-xl bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800 flex items-center justify-center text-brand-600 dark:text-brand-400 shrink-0"
                  >
                    <component :is="getModuleIcon(module.category)" class="w-5 h-5 shrink-0" />
                  </div>
                <div>
                  <div class="flex items-center gap-2 flex-wrap">
                    <h2 class="text-sm sm:text-lg font-bold text-slate-900 dark:text-slate-100">
                      {{ module.moduleTitle }}
                    </h2>
                    <span
                      class="text-xs px-2.5 py-0.5 rounded-full bg-slate-100 dark:bg-canvas-elevated border border-transparent dark:border-white/[0.06] text-slate-600 dark:text-slate-300 font-semibold whitespace-nowrap shrink-0"
                    >
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
                <span class="text-xs font-bold text-slate-600 dark:text-slate-400 whitespace-nowrap shrink-0">
                  {{ module.completedCount }}/{{ module.totalCount }} {{ $t('roadmap.completed') }}
                </span>
                <div class="w-16 sm:w-20 h-2 bg-slate-100 dark:bg-canvas-elevated rounded-full overflow-hidden">
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
                    ? 'bg-brand-500/10 dark:bg-brand-500/15 border-brand-500 dark:border-brand-400 shadow-lg shadow-brand-500/10 ring-2 ring-brand-500/30'
                    : day.isCompleted
                      ? 'bg-white dark:bg-canvas-subtle border-brand-500/40 dark:border-brand-500/40 hover:border-brand-500 hover:shadow-sm'
                      : day.isUnlocked
                        ? 'bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08] hover:border-brand-500/40 hover:shadow-sm'
                        : 'bg-slate-50/70 dark:bg-canvas-elevated/40 border-slate-200/80 dark:border-white/[0.06] opacity-75 hover:opacity-100'
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
                            ? 'bg-brand-600 text-white'
                            : 'bg-slate-200 dark:bg-canvas-elevated text-slate-700 dark:text-slate-300'
                      ]"
                    >
                      {{ day.dayOrder }}
                    </span>

                    <span
                      :class="[
                        'text-xs px-2 py-0.5 rounded-md font-bold uppercase tracking-wider border whitespace-nowrap shrink-0',
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
                      class="flex items-center gap-1 text-xs font-bold text-amber-600 dark:text-amber-400 bg-amber-100 dark:bg-amber-950/60 px-2.5 py-0.5 rounded-full border border-amber-300 dark:border-amber-700 animate-pulse whitespace-nowrap shrink-0"
                    >
                      <Flame class="w-3.5 h-3.5 text-amber-500 shrink-0" />
                      <span>{{ $t('roadmap.today') }}</span>
                    </span>
                    <span
                      v-else-if="day.isCompleted"
                      class="flex items-center gap-1 text-xs font-bold text-brand-600 dark:text-brand-400 bg-brand-50 dark:bg-brand-950/60 px-2.5 py-0.5 rounded-full border border-brand-200 dark:border-brand-800 whitespace-nowrap shrink-0"
                    >
                      <CheckCircle2 class="w-3.5 h-3.5 text-brand-500 shrink-0" />
                      <span>{{ day.drillScore !== null ? `+${day.drillScore}` : 'Pass' }}</span>
                    </span>
                    <span
                      v-else-if="day.isUnlocked"
                      class="text-xs font-semibold text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-canvas-elevated border border-transparent dark:border-white/[0.06] px-2.5 py-0.5 rounded-full whitespace-nowrap shrink-0"
                    >
                      <Eye class="w-3.5 h-3.5 inline mr-0.5 shrink-0" />
                      <span>{{ $t('roadmap.ready') }}</span>
                    </span>
                    <span
                      v-else
                      class="text-xs font-semibold text-slate-400 dark:text-slate-500 bg-slate-100/50 dark:bg-canvas-elevated/40 border border-transparent dark:border-white/[0.06] px-2.5 py-0.5 rounded-full whitespace-nowrap shrink-0"
                    >
                      <Lock class="w-3 h-3 inline mr-0.5 shrink-0" />
                      <span>{{ $t('roadmap.locked') }}</span>
                    </span>
                  </div>
                </div>

                <!-- Title & Summary -->
                <div class="space-y-1.5 mb-4">
                  <h3
                    class="font-bold text-sm sm:text-base text-slate-900 dark:text-slate-100 group-hover:text-brand-600 dark:group-hover:text-brand-400 transition-colors line-clamp-1"
                  >
                    {{ day.title }}
                  </h3>
                  <p class="text-sm text-slate-600 dark:text-slate-400 line-clamp-2 leading-relaxed">
                    {{ day.summary }}
                  </p>
                </div>

                <!-- Action Link -->
                <div
                  class="pt-2.5 border-t border-slate-100 dark:border-white/[0.08] flex items-center justify-between text-xs sm:text-sm font-semibold"
                >
                  <span
                    :class="[
                      day.isActiveToday
                        ? 'text-amber-600 dark:text-amber-400 font-bold'
                        : day.isCompleted
                          ? 'text-brand-600 dark:text-brand-400'
                          : 'text-slate-500 dark:text-slate-400 group-hover:text-brand-600 dark:group-hover:text-brand-400'
                    ]"
                  >
                    {{
                      day.isActiveToday
                        ? $t('roadmap.start_today')
                        : day.isCompleted
                          ? $t('roadmap.review_day')
                          : $t('roadmap.view_lesson')
                    }}
                  </span>
                  <ArrowRight
                    class="w-3.5 h-3.5 text-slate-400 group-hover:text-brand-500 group-hover:translate-x-0.5 transition-transform shrink-0"
                  />
                </div>
              </div>
            </div>
          </section>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

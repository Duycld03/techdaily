<script setup lang="ts">
/**
 * HomeBentoDashboard — Canonical Executive Bento Dashboard
 * Implemented cleanly from Design System Showcase (LayoutArchetypesShowcase.vue Demo 4).
 * Asymmetric 2:1 ratio (2 columns Action Stage + 1 column Telemetry Dock).
 * Eliminates legacy circular SVGs and Frankenstein hybrid styling.
 */
import { computed, onMounted } from 'vue'
import {
  ArrowRight,
  Flame,
  Clock,
  CheckCircle2,
  AlertCircle
} from 'lucide-vue-next'
import { useAuthStore } from '~/stores/useAuthStore'
import { useDailyFocusStore, type PacerBookSummary } from '~/stores/useDailyFocusStore'
import { useReviewStore } from '~/stores/useReviewStore'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'
import { useNotesStore } from '~/stores/useNotesStore'
import BentoDashboardLayout from '~/components/layout/BentoDashboardLayout.vue'

const emit = defineEmits<{
  (e: 'startReading'): void
  (e: 'startScenario'): void
  (e: 'startTodayPractice'): void
}>()
const { t } = useI18n()

const authStore = useAuthStore()
const focusStore = useDailyFocusStore()
const reviewStore = useReviewStore()
const graphStore = useKnowledgeGraphStore()
const notesStore = useNotesStore()

function handleStartReading() {
  emit('startReading')
  if (pacer.value?.bookId && pacer.value?.currentChunkOrder) {
    navigateTo({
      path: `/read/${pacer.value.bookId}`,
      query: {
        slice: pacer.value.currentChunkOrder.toString(),
        from: '/'
      }
    })
  } else {
    navigateTo('/today')
  }
}

function handleStartDrill() {
  emit('startTodayPractice')
  emit('startScenario')
  navigateTo({
    path: '/today',
    query: { tab: 'challenge' }
  })
}

function handleStartScenario() {
  emit('startTodayPractice')
  emit('startScenario')
  navigateTo('/quiz')
}

function handlePrimaryBannerCta() {
  if (isDrillSubmitted.value) {
    handleStartDrill()
  } else if (pacer.value?.bookId) {
    handleStartReading()
  } else {
    handleStartDrill()
  }
}

const userName = computed(() => authStore.user?.name?.split(' ')[0] || 'Engineer')
const targetRole = computed(() => {
  const role = (authStore.user as { targetRole?: string } | null)?.targetRole
  return role || 'Senior Software Engineer'
})

const pacer = computed(() => focusStore.data?.pacer)
const scenario = computed(() => focusStore.data?.scenario)
const drill = computed(() => focusStore.data?.drill)
const streak = computed(() => focusStore.data?.currentStreak ?? 0)
const freezeCredits = computed(() => focusStore.data?.freezeCreditsRemaining ?? 2)

const learnedChunksCount = computed(() => {
  const books = pacer.value?.availableBooks
  if (books && books.length > 0) {
    return books.reduce((sum: number, b: PacerBookSummary) => {
      if (b.progressPercentage >= 100) {
        return sum + (b.totalChunks || 0)
      }
      return sum + Math.max(0, (b.currentChunkOrder ?? 1) - 1)
    }, 0)
  }

  if (pacer.value) {
    if (pacer.value.progressPercentage >= 100) {
      return pacer.value.totalChunks || 0
    }
    return Math.max(0, (pacer.value.currentChunkOrder ?? 1) - 1)
  }

  return 0
})

const activeSliceTitle = computed(() => {
  if (focusStore.data?.hasActiveBook === false) {
    return t('roadmap.no_active_books')
  }
  return focusStore.data?.documentChunk?.chapterTitle || pacer.value?.chapterTitle || t('dashboard.loading_dashboard')
})

const activeSliceSummary = computed(() => {
  return focusStore.data?.documentChunk?.summaryMarkdown || t('dashboard.slice_summary_placeholder')
})

const isDrillSubmitted = computed(() => {
  const status = drill.value?.status
  return (
    status === 1 ||
    status === 2 ||
    status === 'Submitted' ||
    status === 'Reviewed' ||
    drill.value?.isCorrect !== undefined ||
    Boolean(drill.value?.submittedAt)
  )
})

const isDrillPassed = computed(() => {
  if (!isDrillSubmitted.value) return false
  return drill.value?.isCorrect === true || (drill.value?.score !== undefined && drill.value?.score > 0)
})

const isDrillFailed = computed(() => {
  if (!isDrillSubmitted.value) return false
  return drill.value?.isCorrect === false || drill.value?.score === 0
})

const drillScore = computed(() => {
  return drill.value?.score ?? (drill.value?.isCorrect ? 10 : 0)
})

const scenarioTitle = computed(() => {
  return scenario.value?.title || focusStore.data?.question?.questionText || t('dashboard.scenario_teaser_title')
})

const scenarioSituation = computed(() => {
  return scenario.value?.situation || focusStore.data?.question?.explanationMarkdown || t('dashboard.scenario_teaser_desc')
})

const curriculumDay = computed(() => {
  const day = (focusStore.data as { topic?: { dayOrder?: number } } | null)?.topic?.dayOrder
  return day || 1
})

const curriculumDayText = computed(() => {
  const custom = t('dashboard.curriculum_day_badge', { day: curriculumDay.value, total: 30 })
  return custom === 'dashboard.curriculum_day_badge'
    ? `${t('today.day')} ${curriculumDay.value}`
    : custom
})

const sliceBadgeText = computed(() => {
  if (pacer.value && pacer.value.totalChunks > 0) {
    const text = t('dashboard.active_slice_badge', {
      current: pacer.value.currentChunkOrder,
      total: pacer.value.totalChunks
    })
    if (text === 'dashboard.active_slice_badge') {
      return `Slice ${pacer.value.currentChunkOrder} / ${pacer.value.totalChunks}`
    }
    return text
  }
  return `Slice ${focusStore.data?.documentChunk?.chunkOrder || 1}`
})

const slicePercentage = computed(() => {
  if (!pacer.value || pacer.value.totalChunks <= 0) return 0
  return Math.round((pacer.value.currentChunkOrder / pacer.value.totalChunks) * 100)
})

const estimatedMinutes = computed(() => {
  return focusStore.data?.estimatedMinutes || 4
})

const actualMinutes = computed(() => {
  return focusStore.data?.todayMinutesSpent || 0
})

const dailyGoalMinutes = computed(() => {
  const goal = (authStore.user as { dailyGoalMinutes?: number } | null)?.dailyGoalMinutes
  return goal || 10
})

// Spaced Repetition stats from reviewStore
const reviewStats = computed(() => {
  const stats = reviewStore.deckStatistics
  const total = stats?.totalCards ?? 0
  const mastered = stats?.masteredCount ?? 0
  const due = reviewStore.totalCardsDue || 0
  return {
    total,
    mastered,
    due
  }
})

// Graph telemetry
const totalNodes = computed(() => {
  return graphStore.rawData?.stats?.totalNodes || graphStore.rawData?.nodes?.length || 0
})

const totalEdges = computed(() => {
  return graphStore.rawData?.stats?.totalEdges || graphStore.rawData?.edges?.length || 0
})

onMounted(() => {
  if (!reviewStore.deckStatistics?.totalCards && typeof reviewStore.fetchDeckCards === 'function') {
    reviewStore.fetchDeckCards({ pageSize: 1, countOnly: true }).catch(() => {})
  }
  if (!graphStore.rawData && typeof graphStore.fetchGraph === 'function') {
    graphStore.fetchGraph().catch(() => {})
  }
  if (!notesStore.totalAllCount && !notesStore.highlights.length && typeof notesStore.fetchHighlights === 'function') {
    notesStore.fetchHighlights({ pageSize: 1 }).catch(() => {})
  }
})
</script>

<template>
  <BentoDashboardLayout>
    <!-- 1. Orientation Banner Slot -->
    <template #header>
      <div class="glass-card p-4 sm:p-5 rounded-2xl flex flex-col sm:flex-row sm:items-center justify-between gap-3 border border-slate-200/80 dark:border-white/[0.06]">
        <div class="space-y-1">
          <div class="flex flex-wrap items-center gap-1.5 sm:gap-2 min-w-0">
            <span class="rounded-full bg-brand-500/15 px-2.5 py-0.5 text-xs font-semibold text-brand-600 dark:text-brand-300 whitespace-nowrap shrink-0">
              Executive Cockpit
            </span>
            <span class="text-xs text-slate-500 dark:text-slate-400">
              {{ targetRole }} • {{ sliceBadgeText }}
            </span>
          </div>
          <h2 class="text-lg sm:text-xl font-bold text-slate-900 dark:text-white tracking-tight">
            {{ $t('dashboard.welcome_subtitle') }}
          </h2>
          <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 max-w-2xl truncate">
            {{ activeSliceTitle }}
          </p>
        </div>

        <button
          @click="handlePrimaryBannerCta"
          type="button"
          class="h-9 px-4 sm:px-5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs sm:text-sm shadow-sm transition-all flex items-center justify-center gap-1.5 cursor-pointer whitespace-nowrap shrink-0 active:scale-95"
        >
          <span>{{ isDrillSubmitted ? $t('dashboard.review_today_practice') : $t('dashboard.start_focus_cta') }}</span>
          <ArrowRight class="w-4 h-4" :stroke-width="2" />
        </button>
      </div>
    </template>

    <!-- 2. Core Practice Action Stage (Left 2 Columns) -->
    <template #action-stage>
      <!-- Tier 1: Primary Action Stage (Hero Reading Card) -->
      <div class="glass-card p-5 rounded-2xl space-y-3 flex flex-col justify-between border border-slate-200/80 dark:border-white/[0.06] group hover:border-brand-500/30 transition-all">
        <div class="space-y-2.5">
          <div class="flex items-center justify-between gap-2">
            <span class="text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400 truncate">
              {{ pacer?.bookTitle || $t('dashboard.active_reading_slice') }}
            </span>
            <div class="flex items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-canvas-subtle px-2.5 py-0.5 rounded-full border border-slate-200/60 dark:border-white/[0.06] shrink-0 font-medium">
              <Clock class="w-3.5 h-3.5" :stroke-width="1.5" />
              <span>{{ estimatedMinutes }} {{ $t('today.estimated_read') }}</span>
            </div>
          </div>

          <h3 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white line-clamp-1 group-hover:text-brand-400 transition-colors">
            {{ activeSliceTitle }}
          </h3>

          <p class="text-xs sm:text-sm leading-relaxed text-slate-600 dark:text-slate-300 line-clamp-2">
            {{ activeSliceSummary }}
          </p>

          <div class="flex flex-wrap items-center gap-2 pt-1">
            <span class="rounded-md bg-slate-100 dark:bg-canvas-subtle px-2.5 py-1 text-xs font-medium text-slate-600 dark:text-slate-300">
              {{ sliceBadgeText }}
            </span>
            <span class="rounded-md bg-brand-500/10 dark:bg-brand-500/20 px-2.5 py-1 text-xs font-medium text-brand-600 dark:text-brand-300 font-mono">
              {{ slicePercentage }}% {{ $t('dashboard.slice_progress') }}
            </span>
          </div>

          <!-- Progress Bar -->
          <div class="h-1.5 w-full bg-slate-100 dark:bg-white/[0.08] rounded-full overflow-hidden">
            <div
              class="h-full bg-gradient-to-r from-brand-600 to-brand-500 rounded-full transition-all duration-500"
              :style="{ width: `${slicePercentage}%` }"
            ></div>
          </div>
        </div>

        <!-- Footer Action -->
        <div class="pt-3 border-t border-slate-200/80 dark:border-white/[0.06] flex items-center justify-between gap-3">
          <span class="text-xs text-slate-500 dark:text-slate-400 hidden sm:inline">
            {{ $t('dashboard.press_enter_to_continue') }}
          </span>
          <button
            @click="handleStartReading"
            type="button"
            class="h-9 px-4 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-semibold text-xs sm:text-sm shadow-sm transition-all flex items-center justify-center gap-1.5 cursor-pointer whitespace-nowrap shrink-0 active:scale-95"
          >
            <span>{{ $t('dashboard.continue_reading') }}</span>
            <ArrowRight class="w-4 h-4" :stroke-width="1.5" />
          </button>
        </div>
      </div>

      <!-- Tier 2: Split Practice Subgrid (Daily Drill + Senior Dilemma) -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5 sm:gap-4 flex-1">
        <!-- Sub-Card 1: Daily Micro-Drill -->
        <div class="glass-card p-4 sm:p-5 rounded-2xl flex flex-col justify-between border border-slate-200/80 dark:border-white/[0.06] group hover:border-brand-500/30 transition-all">
          <div class="space-y-2">
            <div class="flex items-center justify-between gap-2">
              <span class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 truncate">
                {{ $t('dashboard.daily_drill_label') }}
              </span>

              <!-- Status Badge -->
              <span
                v-if="isDrillPassed"
                class="px-2 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 whitespace-nowrap flex items-center gap-1 shrink-0"
              >
                <CheckCircle2 class="w-3 h-3" :stroke-width="1.5" />
                <span>{{ $t('dashboard.status_completed') }}: {{ drillScore }}/10</span>
              </span>
              <span
                v-else-if="isDrillFailed"
                class="px-2 py-0.5 rounded-full text-[11px] font-semibold bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20 whitespace-nowrap flex items-center gap-1 shrink-0"
              >
                <AlertCircle class="w-3 h-3 text-rose-500" :stroke-width="1.5" />
                <span>{{ $t('dashboard.status_needs_review') }}: 0/10</span>
              </span>
              <span
                v-else
                class="px-2 py-0.5 rounded-full text-[11px] font-semibold bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20 whitespace-nowrap shrink-0"
              >
                +10 {{ $t('dashboard.points_reward') }}
              </span>
            </div>

            <h4 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white line-clamp-1 group-hover:text-brand-400 transition-colors">
              {{ curriculumDayText }}
            </h4>
            <p class="text-xs text-slate-600 dark:text-slate-300 line-clamp-2 leading-relaxed">
              {{ focusStore.data?.question?.questionText || $t('today.quiz_title') }}
            </p>
          </div>

          <div class="pt-3 border-t border-slate-200/80 dark:border-white/[0.06] flex items-center justify-between gap-2 mt-3">
            <span class="text-[11px] text-slate-400 truncate">
              1 {{ $t('dashboard.itinerary_scenario') }} • 3 min
            </span>
            <button
              @click="handleStartDrill"
              type="button"
              class="h-8 px-3.5 rounded-lg bg-slate-100 dark:bg-canvas-elevated hover:bg-slate-200 dark:hover:bg-white/[0.08] text-slate-800 dark:text-slate-200 border border-slate-200 dark:border-white/[0.08] font-semibold text-xs transition-all flex items-center justify-center gap-1.5 active:scale-95 shrink-0 whitespace-nowrap cursor-pointer"
            >
              <span>{{ isDrillSubmitted ? $t('dashboard.review_today_practice') : $t('dashboard.start_today_practice') }}</span>
              <ArrowRight class="w-3.5 h-3.5 text-slate-400" :stroke-width="1.5" />
            </button>
          </div>
        </div>
        <!-- Sub-Card 2: Senior Dilemma -->
        <div class="glass-card p-4 sm:p-5 rounded-2xl flex flex-col justify-between border border-slate-200/80 dark:border-white/[0.06] group hover:border-brand-500/30 transition-all">
          <div class="space-y-2">
            <div class="flex items-center justify-between gap-2">
              <span class="text-xs font-bold uppercase tracking-wider text-amber-600 dark:text-amber-400 truncate">
                {{ $t('dashboard.senior_dilemma_label') }}
              </span>
              <span class="px-2 py-0.5 rounded-full text-[11px] font-semibold bg-brand-500/10 text-brand-600 dark:text-brand-300 border border-brand-500/20 whitespace-nowrap shrink-0">
                {{ targetRole }}
              </span>
            </div>

            <h4 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white line-clamp-1 group-hover:text-amber-400 transition-colors">
              {{ scenarioTitle }}
            </h4>
            <p class="text-xs text-slate-600 dark:text-slate-300 line-clamp-2 leading-relaxed">
              {{ scenarioSituation }}
            </p>
          </div>

          <div class="pt-3 border-t border-slate-200/80 dark:border-white/[0.06] flex items-center justify-between gap-2 mt-3">
            <span class="text-[11px] text-slate-400 truncate">
              {{ $t('dashboard.scenario_subtitle') }}
            </span>
            <button
              @click="handleStartScenario"
              type="button"
              class="h-8 px-3.5 rounded-lg bg-amber-500/10 hover:bg-amber-500/20 text-amber-600 dark:text-amber-400 border border-amber-500/20 font-semibold text-xs transition-all flex items-center justify-center gap-1.5 active:scale-95 shrink-0 whitespace-nowrap cursor-pointer"
            >
              <span>{{ $t('dashboard.solve_dilemma_cta') }}</span>
              <ArrowRight class="w-3.5 h-3.5" :stroke-width="1.5" />
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- 3. Telemetry Dock (Right 1 Column) -->
    <template #telemetry-dock>
      <!-- Telemetry Card 1: Practice Streak & Consistency -->
      <div class="glass-card p-4 sm:p-5 rounded-2xl space-y-3.5 border border-slate-200/80 dark:border-white/[0.06] group hover:border-amber-500/30 transition-all">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-400">
            {{ $t('dashboard.metrics_title') }}
          </span>
          <Flame class="w-4 h-4 text-amber-500 fill-amber-500" :stroke-width="1.5" />
        </div>

        <div class="space-y-0.5">
          <div class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tabular-nums tracking-tight">
            {{ streak }} {{ $t('dashboard.days_streak') }}
          </div>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {{ $t('dashboard.metrics_subtitle') }}
          </p>
        </div>

        <!-- 7-Day Consistency Hairline Bar -->
        <div class="space-y-1.5 pt-1">
          <div class="h-2 w-full rounded-full bg-slate-100 dark:bg-white/[0.08] overflow-hidden">
            <div
              class="h-full bg-amber-500 rounded-full transition-all duration-500"
              :style="{ width: `${Math.min(100, Math.max(14, streak * 14))}%` }"
            />
          </div>
          <div class="flex items-center justify-between text-[11px] text-slate-400 font-medium">
            <span>{{ freezeCredits }}/2 {{ $t('dashboard.freezes') }}</span>
            <span class="text-amber-500 font-semibold">{{ actualMinutes }}/{{ dailyGoalMinutes }}m {{ $t('dashboard.daily_goal') }}</span>
          </div>
        </div>
      </div>

      <!-- Telemetry Card 2: Knowledge Constellation -->
      <div class="glass-card p-4 sm:p-5 rounded-2xl space-y-3.5 border border-slate-200/80 dark:border-white/[0.06] group hover:border-brand-500/30 transition-all flex flex-col justify-between flex-1">
        <div class="space-y-2.5">
          <div class="flex items-center justify-between">
            <span class="text-xs font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider">
              {{ $t('dashboard.knowledge_radar') }}
            </span>
            <span class="text-[11px] font-mono text-brand-600 dark:text-brand-400 bg-brand-500/10 px-2 py-0.5 rounded-full">
              {{ reviewStats.mastered }}/{{ reviewStats.total }} SM-2
            </span>
          </div>

          <p class="text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
            {{ totalNodes }} {{ $t('dashboard.connected_nodes') }} • {{ totalEdges }} {{ $t('dashboard.active_relations') }} • {{ learnedChunksCount }} {{ $t('dashboard.stat_learned_chunks') }}
          </p>

          <div class="grid grid-cols-2 gap-2 pt-1 text-center">
            <div class="p-2 rounded-xl bg-slate-100 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
              <span class="block text-base font-black text-slate-900 dark:text-white tabular-nums">{{ totalNodes }}</span>
              <span class="text-[10px] text-slate-500 uppercase tracking-tight block truncate">{{ $t('dashboard.connected_nodes') }}</span>
            </div>
            <div class="p-2 rounded-xl bg-slate-100 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
              <span class="block text-base font-black text-slate-900 dark:text-white tabular-nums">{{ totalEdges }}</span>
              <span class="text-[10px] text-slate-500 uppercase tracking-tight block truncate">{{ $t('dashboard.active_relations') }}</span>
            </div>
            <div class="p-2 rounded-xl bg-slate-100 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
              <span class="block text-base font-black text-slate-900 dark:text-white tabular-nums">{{ reviewStats.due }}</span>
              <span class="text-[10px] text-slate-500 uppercase tracking-tight block truncate">{{ $t('dashboard.stat_cards_due') }}</span>
            </div>
            <div class="p-2 rounded-xl bg-slate-100 dark:bg-canvas-subtle border border-slate-200/60 dark:border-white/[0.06]">
              <span class="block text-base font-black text-slate-900 dark:text-white tabular-nums">{{ reviewStats.mastered }}</span>
              <span class="text-[10px] text-slate-500 uppercase tracking-tight block truncate">{{ $t('dashboard.stat_mastered_cards') }}</span>
            </div>
          </div>
        </div>

        <button
          @click="navigateTo('/graph')"
          type="button"
          class="w-full rounded-xl border border-slate-200/60 dark:border-white/[0.06] bg-slate-50/50 dark:bg-canvas-subtle hover:bg-slate-100 dark:hover:bg-white/[0.06] p-2.5 text-center text-xs text-brand-600 dark:text-brand-400 font-semibold transition-all cursor-pointer flex items-center justify-center gap-1.5"
        >
          <span>{{ $t('dashboard.open_cosmos') }}</span>
          <ArrowRight class="w-3.5 h-3.5" :stroke-width="1.5" />
        </button>
      </div>
    </template>
  </BentoDashboardLayout>
</template>

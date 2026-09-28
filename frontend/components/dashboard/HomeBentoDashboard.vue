<script setup lang="ts">
import { computed, onMounted } from 'vue'
import {
  BookOpen,
  Terminal,
  ArrowRight,
  Flame,
  Shield,
  Clock,
  Target,
  CheckCircle2,
  AlertCircle
} from 'lucide-vue-next'
import ConcentricMetricCard from '~/components/today/ConcentricMetricCard.vue'
import DomainConstellationCard from '~/components/dashboard/DomainConstellationCard.vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useDailyFocusStore } from '~/stores/useDailyFocusStore'
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

function handleStartReading() {
  emit('startReading')
  if (pacer.value?.bookId && pacer.value?.currentChunkOrder) {
    navigateTo({
      path: `/read/${pacer.value.bookId}`,
      query: { slice: pacer.value.currentChunkOrder.toString() }
    })
  } else {
    navigateTo('/today')
  }
}

function handleStartScenario() {
  emit('startTodayPractice')
  emit('startScenario')
  navigateTo('/today')
}

const authStore = useAuthStore()
const focusStore = useDailyFocusStore()
const reviewStore = useReviewStore()
const graphStore = useKnowledgeGraphStore()
const notesStore = useNotesStore()

const userName = computed(() => authStore.user?.name?.split(' ')[0] || 'Engineer')
const targetRole = computed(() => (authStore.user as any)?.targetRole || 'Senior Software Engineer')

const pacer = computed(() => focusStore.data?.pacer)
const scenario = computed(() => (focusStore.data as any)?.scenario)
const drill = computed(() => focusStore.data?.drill)
const streak = computed(() => focusStore.data?.currentStreak ?? 0)
const freezeCredits = computed(() => focusStore.data?.freezeCreditsRemaining ?? 2)

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
    !!drill.value?.submittedAt
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

const isDrillCompleted = computed(() => isDrillSubmitted.value)

const drillScore = computed(() => {
  return drill.value?.score ?? (drill.value?.isCorrect ? 10 : 0)
})

const scenarioTitle = computed(() => {
  return scenario.value?.title || focusStore.data?.question?.questionText || t('dashboard.scenario_teaser_title')
})

const scenarioSituation = computed(() => {
  return scenario.value?.situation || focusStore.data?.question?.explanationMarkdown || t('dashboard.scenario_teaser_desc')
})

const curriculumDay = computed(() => (focusStore.data as any)?.topic?.dayOrder || 1)
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
  return (authStore.user as any)?.dailyGoalMinutes || 10
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

// 7-day consistency calendar calculations
const weekDays = computed(() => {
  const labels = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa']
  const todayIndex = new Date().getDay()

  return labels.map((label, idx) => {
    const isPast = idx < todayIndex
    const isToday = idx === todayIndex
    const isCompleted = isPast && streak.value > 0
    return {
      label,
      isToday,
      isCompleted,
      isFuture: idx > todayIndex
    }
  })
})

onMounted(() => {
  // Defensively load review deck statistics & graph data without crashing if methods are uninitialized
  if (!reviewStore.deckStatistics?.totalCards && typeof reviewStore.fetchDeckCards === 'function') {
    reviewStore.fetchDeckCards({ pageSize: 1 }).catch(() => {})
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
    <!-- 1. Welcome & Orientation Banner -->
    <template #header>
      <div class="glass-card px-4 py-3 sm:px-5 sm:py-3.5 relative overflow-hidden border border-slate-200/80 dark:border-white/[0.06]">
        <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2.5 relative z-10">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-slate-100 dark:bg-white/[0.06] text-slate-700 dark:text-slate-300 border border-slate-200/60 dark:border-white/[0.08]">
                {{ sliceBadgeText }}
              </span>
              <span class="text-xs text-slate-500 dark:text-slate-400">
                • {{ targetRole }}
              </span>
            </div>

            <h1 class="text-lg sm:text-xl lg:text-2xl font-black text-slate-900 dark:text-white tracking-tight">
              {{ $t('dashboard.welcome_back') }}, {{ userName }}! 👋
            </h1>
            <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-300 max-w-2xl">
              {{ $t('dashboard.welcome_subtitle') }}
            </p>
          </div>
        </div>
      </div>
    </template>

    <!-- 2. Core Practice Action Stage (Left 2 Columns) -->
    <template #action-stage>
      <!-- Card A: Active Reading Hero -->
      <div class="glass-card p-4 sm:p-5 flex flex-col justify-between group hover:border-brand-500/30 transition-all border border-slate-200/80 dark:border-white/[0.06] min-h-0">
        <div>
          <!-- Header: Book Info & Read Duration -->
          <div class="flex items-center justify-between gap-3 mb-3">
            <div class="flex items-center gap-2.5 min-w-0">
              <div class="w-8 h-8 rounded-lg bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0">
                <BookOpen class="w-4 h-4" :stroke-width="1.5" />
              </div>
              <div class="min-w-0">
                <span class="text-[11px] font-mono font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider block truncate">
                  {{ pacer?.bookTitle || $t('dashboard.active_reading_slice') }}
                </span>
                <div class="text-xs text-brand-600 dark:text-brand-400 font-semibold truncate">
                  {{ sliceBadgeText }}
                </div>
              </div>
            </div>

            <div class="flex items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-canvas-subtle px-2.5 py-1 rounded-full border border-slate-200/60 dark:border-white/[0.06] shrink-0">
              <Clock class="w-3.5 h-3.5" :stroke-width="1.5" />
              <span>{{ estimatedMinutes }} {{ $t('today.estimated_read') }}</span>
            </div>
          </div>

          <!-- Slice Title & Summary -->
          <h2 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white mb-2 group-hover:text-brand-400 transition-colors line-clamp-1">
            {{ activeSliceTitle }}
          </h2>
          <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-300 line-clamp-2 leading-relaxed mb-3">
            {{ activeSliceSummary }}
          </p>

          <!-- Slice Progress Bar -->
          <div class="space-y-1.5 mb-2">
            <div class="flex items-center justify-between text-xs font-semibold">
              <span class="text-slate-500 dark:text-slate-400">
                {{ $t('dashboard.slice_progress') }} ({{ pacer?.currentChunkOrder || 1 }}/{{ pacer?.totalChunks || 1 }})
              </span>
              <span class="text-slate-700 dark:text-slate-300 font-bold">{{ slicePercentage }}%</span>
            </div>
            <div class="h-2 w-full bg-slate-100 dark:bg-canvas-elevated rounded-full overflow-hidden border border-slate-200/60 dark:border-white/[0.06]">
              <div
                class="h-full bg-gradient-to-r from-brand-600 to-brand-500 rounded-full transition-all duration-500"
                :style="{ width: `${slicePercentage}%` }"
              ></div>
            </div>
          </div>
        </div>

        <!-- Primary Reading CTA -->
        <div class="pt-3 border-t border-slate-200/80 dark:border-white/[0.06] flex items-center justify-between gap-3">
          <span class="text-xs text-slate-500 dark:text-slate-400 hidden sm:inline">
            {{ $t('dashboard.press_enter_to_continue') }}
          </span>

          <button
            @click="handleStartReading"
            type="button"
            class="w-full sm:w-auto px-4 sm:px-5 py-2 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-xs sm:text-sm shadow-md shadow-brand-500/20 transition-all flex items-center justify-center gap-2 active:scale-95 shrink-0 whitespace-nowrap cursor-pointer"
          >
            <span>{{ $t('dashboard.continue_reading') }}</span>
            <ArrowRight class="w-4 h-4" :stroke-width="1.5" />
          </button>
        </div>
      </div>

      <!-- Card B: Today's Practice Session Cockpit -->
      <div class="glass-card p-4 sm:p-5 flex flex-col justify-between group hover:border-brand-500/30 transition-all border border-slate-200/80 dark:border-white/[0.06] min-h-0">
        <div>
          <!-- Header: Practice Badge & Semantic Drill Status -->
          <div class="flex items-center justify-between gap-3 mb-3">
            <div class="flex items-center gap-2.5 min-w-0">
              <div class="w-8 h-8 rounded-lg bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20 flex items-center justify-center shrink-0">
                <Target class="w-4 h-4" :stroke-width="1.5" />
              </div>
              <div class="min-w-0">
                <div class="text-[11px] font-mono font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider whitespace-nowrap">
                  {{ $t('dashboard.today_practice_badge') }}
                </div>
                <div class="text-xs text-slate-500 dark:text-slate-400 truncate">
                  {{ curriculumDayText }}
                </div>
              </div>
            </div>

            <!-- Semantic Drill Status Badge -->
            <div class="shrink-0">
              <span
                v-if="isDrillPassed"
                class="px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 whitespace-nowrap flex items-center gap-1.5"
              >
                <CheckCircle2 class="w-3.5 h-3.5" :stroke-width="1.5" />
                <span>{{ $t('dashboard.status_completed') }}: {{ drillScore }}/10</span>
              </span>
              <span
                v-else-if="isDrillFailed"
                class="px-2.5 py-1 rounded-full text-xs font-semibold bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20 whitespace-nowrap flex items-center gap-1.5"
              >
                <AlertCircle class="w-3.5 h-3.5 text-rose-500" :stroke-width="1.5" />
                <span>{{ $t('dashboard.status_needs_review') }}: 0/10</span>
              </span>
              <span
                v-else
                class="px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20 whitespace-nowrap flex items-center gap-1"
              >
                <Target class="w-3.5 h-3.5 text-amber-500" :stroke-width="1.5" />
                <span>+10 {{ $t('dashboard.points_reward') }}</span>
              </span>
            </div>
          </div>

          <!-- Scenario Dilemma Title & Excerpt -->
          <h3 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white mb-2 group-hover:text-brand-300 transition-colors line-clamp-1">
            {{ scenarioTitle }}
          </h3>
          <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-300 line-clamp-2 leading-relaxed mb-3">
            {{ scenarioSituation }}
          </p>

          <!-- Itinerary Strip -->
          <div class="grid grid-cols-2 gap-2 mb-2">
            <div class="px-2.5 py-1.5 rounded-xl bg-slate-100 dark:bg-white/[0.04] border border-slate-200/80 dark:border-white/[0.06] flex items-center gap-2 text-xs text-slate-700 dark:text-slate-200 min-w-0">
              <BookOpen class="w-3.5 h-3.5 text-brand-500 shrink-0" :stroke-width="1.5" />
              <span class="truncate">{{ $t('dashboard.itinerary_reading') }}</span>
            </div>
            <div class="px-2.5 py-1.5 rounded-xl bg-slate-100 dark:bg-white/[0.04] border border-slate-200/80 dark:border-white/[0.06] flex items-center gap-2 text-xs text-slate-700 dark:text-slate-200 min-w-0">
              <Terminal class="w-3.5 h-3.5 text-emerald-500 shrink-0" :stroke-width="1.5" />
              <span class="truncate">{{ scenario?.title || $t('dashboard.itinerary_scenario') }}</span>
            </div>
          </div>
        </div>

        <!-- Footer Action CTA -->
        <div class="pt-3 border-t border-slate-200/80 dark:border-white/[0.06] flex items-center justify-between gap-3">
          <span class="text-xs text-slate-500 dark:text-slate-400 hidden sm:inline">
            {{ isDrillSubmitted ? $t('dashboard.today_drill_finished_hint') : $t('dashboard.today_drill_ready_hint') }}
          </span>

          <button
            @click="handleStartScenario"
            type="button"
            class="w-full sm:w-auto px-4 py-2 rounded-xl bg-slate-100 dark:bg-canvas-elevated hover:bg-slate-200 dark:hover:bg-white/[0.08] text-slate-800 dark:text-slate-200 border border-slate-200 dark:border-white/[0.08] font-semibold text-xs sm:text-sm transition-all flex items-center justify-center gap-2 active:scale-95 shrink-0 whitespace-nowrap cursor-pointer"
          >
            <span>{{ isDrillSubmitted ? $t('dashboard.review_today_practice') : $t('dashboard.start_today_practice') }}</span>
            <ArrowRight class="w-4 h-4 text-slate-400" :stroke-width="1.5" />
          </button>
        </div>
      </div>
    </template>

    <!-- 3. Telemetry & Constellation Dock (Right 1 Column) -->
    <template #telemetry-dock>
      <!-- Card C: Concentric Rings Metric Card -->
      <ConcentricMetricCard
        :actual-minutes="actualMinutes"
        :goal-minutes="dailyGoalMinutes"
        :mastered-cards="reviewStats.mastered"
        :total-cards="reviewStats.total"
        :due-cards="reviewStats.due"
      />

      <!-- Card D: 7-Day Consistency Matrix -->
      <div class="glass-card p-3 sm:p-3.5 flex flex-col justify-between group hover:border-white/[0.12] transition-all shrink-0">
        <div class="flex items-center justify-between mb-3">
          <div class="flex items-center gap-2">
            <div class="w-7 h-7 rounded-lg bg-amber-500/10 text-amber-500 flex items-center justify-center shrink-0">
              <Flame class="w-4 h-4 fill-amber-500" :stroke-width="1.5" />
            </div>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200">
              {{ streak }} {{ $t('dashboard.days_streak') }}
            </span>
          </div>

          <div class="flex items-center gap-1 text-[11px] text-sky-500 font-semibold" title="Streak Freeze Credits">
            <Shield class="w-3.5 h-3.5 fill-sky-500/20" :stroke-width="1.5" />
            <span>{{ freezeCredits }}/2 {{ $t('dashboard.freezes') }}</span>
          </div>
        </div>

        <!-- Weekly Consistency Circles -->
        <div class="grid grid-cols-7 gap-1.5 text-center my-1">
          <div
            v-for="day in weekDays"
            :key="day.label"
            class="flex flex-col items-center gap-1.5 p-1.5 rounded-lg transition-colors"
          >
            <span class="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase">
              {{ day.label }}
            </span>

            <div
              :class="[
                'w-5 h-5 rounded-full flex items-center justify-center transition-all',
                day.isCompleted
                  ? 'bg-emerald-500 text-white shadow-sm shadow-emerald-500/30'
                  : day.isToday
                    ? 'border-2 border-amber-500 bg-amber-500/20 animate-pulse'
                    : 'border border-slate-300 dark:border-white/10 bg-transparent'
              ]"
            >
              <span v-if="day.isCompleted" class="text-[10px]">✓</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Card E: Domain Knowledge Constellation -->
      <DomainConstellationCard
        :node-count="graphStore.rawData?.stats?.totalNodes ?? graphStore.rawData?.nodes?.length ?? 148"
        :edge-count="graphStore.rawData?.stats?.totalEdges ?? graphStore.rawData?.edges?.length ?? 210"
        :card-count="reviewStats.total"
        :highlight-count="notesStore.totalAllCount || notesStore.totalCount || notesStore.highlights.length"
        :chunk-count="pacer?.totalChunks || 1"
      />
    </template>
  </BentoDashboardLayout>
</template>

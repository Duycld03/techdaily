<script setup lang="ts">
import { computed, onMounted } from 'vue'
import {
  BookOpen,
  Terminal,
  ArrowRight,
  Flame,
  Shield,
  Clock
} from 'lucide-vue-next'
import ConcentricMetricCard from '~/components/today/ConcentricMetricCard.vue'
import DomainConstellationCard from '~/components/dashboard/DomainConstellationCard.vue'
import { useAuthStore } from '~/stores/useAuthStore'
import { useDailyFocusStore } from '~/stores/useDailyFocusStore'
import { useReviewStore } from '~/stores/useReviewStore'
import { useKnowledgeGraphStore } from '~/stores/useKnowledgeGraphStore'
import BentoDashboardLayout from '~/components/layout/BentoDashboardLayout.vue'

const emit = defineEmits<{
  (e: 'startReading'): void
  (e: 'startScenario'): void
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
  emit('startScenario')
  navigateTo('/today')
}

const authStore = useAuthStore()
const focusStore = useDailyFocusStore()
const reviewStore = useReviewStore()
const graphStore = useKnowledgeGraphStore()

const userName = computed(() => authStore.user?.name?.split(' ')[0] || 'Engineer')
const targetRole = computed(() => (authStore.user as any)?.targetRole || 'Senior Software Engineer')

const topic = computed(() => focusStore.data?.topic)
const pacer = computed(() => focusStore.data?.pacer)
const scenario = computed(() => focusStore.data?.scenario)
const streak = computed(() => focusStore.data?.currentStreak ?? 0)
const freezeCredits = computed(() => focusStore.data?.freezeCreditsRemaining ?? 2)

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
  return `${t('dashboard.curriculum_day')} ${topic.value?.dayOrder || 1}`
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
      <!-- Card A: Today's Focus Bento Hero -->
      <div class="glass-card p-4 sm:p-5 flex flex-col justify-between group hover:border-brand-500/30 transition-all border border-slate-200/80 dark:border-white/[0.06] min-h-0">
        <div>
          <div class="flex items-center justify-between gap-3 mb-3">
            <div class="flex items-center gap-2.5">
              <div class="w-8 h-8 rounded-lg bg-slate-100 dark:bg-white/[0.04] text-slate-600 dark:text-slate-400 border border-slate-200/60 dark:border-white/[0.06] flex items-center justify-center shrink-0">
                <BookOpen class="w-4 h-4" :stroke-width="1.5" />
              </div>
              <div>
                <span class="text-[11px] font-mono font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                  {{ $t('dashboard.active_reading_slice') }}
                </span>
                <div class="text-xs text-slate-500 dark:text-slate-400">
                  {{ pacer?.bookTitle || 'Senior Fullstack Architecture' }}
                </div>
              </div>
            </div>

            <div class="flex items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400">
              <Clock class="w-3.5 h-3.5" :stroke-width="1.5" />
              <span>{{ estimatedMinutes }} {{ $t('today.estimated_read') }}</span>
            </div>
          </div>

          <h2 class="text-lg sm:text-xl font-bold text-slate-900 dark:text-white mb-2 group-hover:text-brand-300 transition-colors">
            {{ topic?.title || $t('dashboard.loading_topic') }}
          </h2>

          <p class="text-sm text-slate-600 dark:text-slate-300 line-clamp-2 mb-4">
            {{ topic?.summary || $t('dashboard.slice_summary_placeholder') }}
          </p>

          <!-- Progress Bar -->
          <div class="space-y-1.5 mb-4">
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

        <!-- Primary Action CTA -->
        <div class="pt-3 border-t border-slate-200/80 dark:border-white/[0.06] flex items-center justify-between">
          <span class="text-xs text-slate-500 dark:text-slate-400 hidden sm:inline">
            {{ $t('dashboard.press_enter_to_continue') }}
          </span>

          <button
            @click="handleStartReading"
            type="button"
            class="w-full sm:w-auto px-5 py-2.5 rounded-xl bg-brand-600 hover:bg-brand-500 text-white font-bold text-sm shadow-md shadow-brand-500/20 transition-all flex items-center justify-center gap-2 active:scale-95 shrink-0"
          >
            <span>{{ $t('dashboard.continue_reading') }}</span>
            <ArrowRight class="w-4 h-4" :stroke-width="1.5" />
          </button>
        </div>
      </div>

      <!-- Card B: Senior Scenario Challenge Card -->
      <div class="glass-card p-4 sm:p-5 flex flex-col justify-between group hover:border-emerald-500/30 transition-all border border-slate-200/80 dark:border-white/[0.06] min-h-0">
        <div>
          <div class="flex items-center justify-between gap-3 mb-3">
            <div class="flex items-center gap-2.5">
              <div class="w-8 h-8 rounded-lg bg-slate-100 dark:bg-white/[0.04] text-slate-600 dark:text-slate-400 border border-slate-200/60 dark:border-white/[0.06] flex items-center justify-center shrink-0">
                <Terminal class="w-4 h-4" :stroke-width="1.5" />
              </div>
              <div>
                <span class="text-[11px] font-mono font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                  {{ $t('today.interview_challenge') }}
                </span>
                <div class="text-xs text-slate-500 dark:text-slate-400">
                  {{ $t('dashboard.scenario_subtitle') }}
                </div>
              </div>
            </div>

            <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20">
              +10 {{ $t('dashboard.points_reward') }}
            </span>
          </div>

          <h3 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white mb-2 group-hover:text-emerald-300 transition-colors">
            {{ scenario?.title || $t('dashboard.scenario_teaser_title') }}
          </h3>

          <p class="text-sm text-slate-600 dark:text-slate-300 line-clamp-2 mb-4">
            {{ scenario?.situation || $t('dashboard.scenario_teaser_desc') }}
          </p>
        </div>

        <div class="pt-3 border-t border-slate-200/80 dark:border-white/[0.06] flex items-center justify-end">
          <button
            @click="handleStartScenario"
            type="button"
            class="w-full sm:w-auto px-4 py-2 rounded-xl bg-slate-100 dark:bg-white/[0.04] hover:bg-slate-200 dark:hover:bg-white/[0.08] text-slate-700 dark:text-slate-300 font-semibold text-xs sm:text-sm border border-slate-200/80 dark:border-white/[0.06] transition-all flex items-center justify-center gap-2 shrink-0"
          >
            <span>{{ $t('dashboard.solve_challenge') }}</span>
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
        :node-count="graphStore.rawData?.nodes?.length || 148"
        :edge-count="graphStore.rawData?.edges?.length || 210"
      />
    </template>
  </BentoDashboardLayout>
</template>

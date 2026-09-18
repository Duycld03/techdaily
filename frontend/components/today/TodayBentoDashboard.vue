<script setup lang="ts">
import { computed, onMounted } from 'vue'
import {
  BookOpen,
  Terminal,
  ArrowRight,
  Flame,
  Shield,
  Network,
  Clock,
  Compass
} from 'lucide-vue-next'
import ConcentricMetricCard from '~/components/today/ConcentricMetricCard.vue'
import CyberRadarWidget from '~/components/today/CyberRadarWidget.vue'
const emit = defineEmits<{
  (e: 'startReading'): void
  (e: 'startScenario'): void
}>()

function handleStartReading() {
  emit('startReading')
  navigateTo('/today')
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
const targetRole = computed(() => authStore.user?.targetRole || 'Senior Software Engineer')

const topic = computed(() => focusStore.data?.topic)
const pacer = computed(() => focusStore.data?.pacer)
const scenario = computed(() => focusStore.data?.scenario)
const streak = computed(() => focusStore.data?.currentStreak ?? 0)
const freezeCredits = computed(() => focusStore.data?.freezeCreditsRemaining ?? 2)

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
  return authStore.user?.dailyGoalMinutes || 10
})

// Spaced Repetition stats from reviewStore
const reviewStats = computed(() => {
  const forecast = reviewStore.forecast
  const total = forecast?.totalCards ?? 0
  const mastered = forecast?.cardsByMastery?.Mastered ?? 0
  const due = reviewStore.dueCardsCount || (forecast?.totalDueToday ?? 0)
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
  // Lazily load review forecast & graph stats if not already in store cache
  if (!reviewStore.forecast) {
    reviewStore.fetchForecast().catch(() => {})
  }
  if (!graphStore.graphData) {
    graphStore.fetchGraph().catch(() => {})
  }
})
</script>

<template>
  <div class="max-w-7xl mx-auto px-3.5 sm:px-6 lg:px-8 py-3.5 sm:py-4 lg:h-[calc(100vh-3.5rem)] lg:overflow-hidden flex flex-col justify-start gap-3.5 sm:gap-4">
    <!-- 1. Welcome & Orientation Banner (Image #1 Inspired) -->
    <div class="glass-card px-4 py-3 sm:px-5 sm:py-3.5 relative overflow-hidden border border-slate-200/80 dark:border-white/[0.06] shrink-0">
      <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2.5 relative z-10">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-slate-100 dark:bg-white/[0.06] text-slate-700 dark:text-slate-300 border border-slate-200/60 dark:border-white/[0.08]">
              {{ $t('dashboard.curriculum_day') }} {{ topic?.dayOrder || 1 }} / 30
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

    <!-- 2. Main Bento Grid (Asymmetric Layout) -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-3.5 sm:gap-4 items-start min-h-0">
      <!-- LEFT 2 COLUMNS: Core Practice Cards -->
      <div class="lg:col-span-2 flex flex-col justify-start gap-3.5 sm:gap-4 min-h-0">
        <!-- Card A: Today's Focus Bento Hero (Image #1 Course Progress Style) -->
        <div class="glass-card p-4 sm:p-5 flex flex-col justify-between group hover:border-white/[0.15] transition-all min-h-0">
          <div>
            <div class="flex items-center justify-between gap-3 mb-3">
              <div class="flex items-center gap-2.5">
                <div class="w-8 h-8 rounded-lg bg-slate-100 dark:bg-white/[0.04] text-slate-600 dark:text-slate-400 border border-slate-200/60 dark:border-white/[0.06] flex items-center justify-center shrink-0">
                  <BookOpen class="w-4 h-4" />
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
                <Clock class="w-3.5 h-3.5" />
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
                  {{ $t('dashboard.slice_progress') }} ({{ pacer?.currentChunkOrder || 1 }}/{{ pacer?.totalChunks || 30 }})
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
              <ArrowRight class="w-4 h-4" />
            </button>
          </div>
        </div>

        <!-- Card B: Senior Scenario Challenge Card (Image #2 Inspired) -->
        <div class="glass-card p-4 sm:p-5 flex flex-col justify-between group hover:border-white/[0.12] transition-all min-h-0">
          <div>
            <div class="flex items-center justify-between gap-3 mb-3">
              <div class="flex items-center gap-2.5">
                <div class="w-8 h-8 rounded-lg bg-slate-100 dark:bg-white/[0.04] text-slate-600 dark:text-slate-400 border border-slate-200/60 dark:border-white/[0.06] flex items-center justify-center shrink-0">
                  <Terminal class="w-4 h-4" />
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

            <h3 class="text-base sm:text-lg font-bold text-slate-900 dark:text-white mb-2">
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
              <ArrowRight class="w-4 h-4 text-slate-400" />
            </button>
          </div>
        </div>
      </div>

      <!-- RIGHT 1 COLUMN: Retention, Consistency & Graph Telemetry -->
      <div class="flex flex-col justify-start gap-3.5 sm:gap-4 min-h-0">
        <!-- Card C: Concentric Rings Metric Card (Image #3 Inspired) -->
        <ConcentricMetricCard
          :actual-minutes="actualMinutes"
          :goal-minutes="dailyGoalMinutes"
          :mastered-cards="reviewStats.mastered"
          :total-cards="reviewStats.total"
          :due-cards="reviewStats.due"
        />

        <!-- Card D: 7-Day Consistency Matrix (Image #1 Inspired) -->
        <div class="glass-card p-3 sm:p-3.5 flex flex-col justify-between group hover:border-white/[0.12] transition-all shrink-0">
          <div class="flex items-center justify-between mb-3">
            <div class="flex items-center gap-2">
              <div class="w-7 h-7 rounded-lg bg-amber-500/10 text-amber-500 flex items-center justify-center shrink-0">
                <Flame class="w-4 h-4 fill-amber-500" />
              </div>
              <span class="text-xs font-bold text-slate-800 dark:text-slate-200">
                {{ streak }} {{ $t('dashboard.days_streak') }}
              </span>
            </div>

            <div class="flex items-center gap-1 text-[11px] text-sky-500 font-semibold" title="Streak Freeze Credits">
              <Shield class="w-3.5 h-3.5 fill-sky-500/20" />
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

        <!-- Card E: Knowledge Graph Radar Card (Image #5 Inspired) -->
        <div class="glass-card p-3 sm:p-3.5 flex flex-col justify-between group hover:border-white/[0.12] transition-all shrink-0">
          <div class="flex items-center justify-between mb-2">
            <div class="flex items-center gap-2">
              <div class="w-7 h-7 rounded-lg bg-cyber-500/10 text-cyber-500 flex items-center justify-center shrink-0">
                <Network class="w-4 h-4" />
              </div>
              <span class="text-xs font-bold text-slate-800 dark:text-slate-200">
                {{ $t('dashboard.knowledge_radar') }}
              </span>
            </div>

            <NuxtLink
              to="/graph"
              class="text-xs font-semibold text-cyber-500 hover:text-cyber-400 flex items-center gap-1 transition-colors"
            >
              <span>{{ $t('dashboard.open_cosmos') }}</span>
              <Compass class="w-3.5 h-3.5" />
            </NuxtLink>
          </div>

          <CyberRadarWidget
            :node-count="graphStore.graphData?.nodes?.length || 148"
            :edge-count="graphStore.graphData?.edges?.length || 210"
          />
        </div>
      </div>
    </div>
  </div>
</template>

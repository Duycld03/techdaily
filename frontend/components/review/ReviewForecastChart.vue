<script setup lang="ts">
import { computed } from 'vue'
import { BarChart3 } from 'lucide-vue-next'
import type { ReviewCard } from '~/stores/useReviewStore'

export type Flashcard = ReviewCard

const props = defineProps<{
  cards: Flashcard[]
}>()

const { locale } = useI18n()

interface ForecastDay {
  dateStr: string
  dayLabel: string
  fullDayLabel: string
  dayNum: number
  count: number
  isToday: boolean
}

const viDays = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7']
const enDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']

const viFullDays = ['Chủ Nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7']
const enFullDays = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

const isVi = computed(() => locale.value?.startsWith('vi'))

const forecastDays = computed<ForecastDay[]>(() => {
  const result: ForecastDay[] = []
  const today = new Date()

  for (let i = 0; i < 7; i++) {
    const d = new Date(today.getFullYear(), today.getMonth(), today.getDate() + i)
    const year = d.getFullYear()
    const month = String(d.getMonth() + 1).padStart(2, '0')
    const day = String(d.getDate()).padStart(2, '0')
    const dateStr = `${year}-${month}-${day}`
    const dayOfWeek = d.getDay()
    const dayLabel = isVi.value ? viDays[dayOfWeek] : enDays[dayOfWeek]
    const fullDayLabel = isVi.value ? viFullDays[dayOfWeek] : enFullDays[dayOfWeek]

    let count = 0
    if (i === 0) {
      // Due today or overdue
      count = props.cards.filter((c) => c.nextReviewDate && c.nextReviewDate <= dateStr).length
    } else {
      count = props.cards.filter((c) => c.nextReviewDate === dateStr).length
    }

    result.push({
      dateStr,
      dayLabel,
      fullDayLabel,
      dayNum: d.getDate(),
      count,
      isToday: i === 0
    })
  }

  return result
})

const maxCount = computed(() => {
  const counts = forecastDays.value.map((d) => d.count)
  return Math.max(1, ...counts)
})

const totalUpcoming = computed(() => {
  return forecastDays.value.reduce((acc, d) => acc + d.count, 0)
})

const peakDayInfo = computed(() => {
  if (forecastDays.value.length === 0) {
    return { count: 0, day: isVi.value ? 'Hôm nay' : 'Today' }
  }
  let maxDay = forecastDays.value[0]
  for (const day of forecastDays.value) {
    if (day.count > maxDay.count) {
      maxDay = day
    }
  }
  return {
    count: maxDay.count,
    day: maxDay.fullDayLabel
  }
})

const peakCount = computed(() => peakDayInfo.value.count)
const peakDay = computed(() => peakDayInfo.value.day)

const averagePerDay = computed(() => {
  return Math.round(totalUpcoming.value / 7)
})

function getBarHeightPercent(count: number): number {
  if (count === 0) return 8
  return Math.min(100, Math.max(16, Math.round((count / maxCount.value) * 100)))
}
</script>

<template>
  <div
    class="rounded-2xl glass-card text-slate-900 dark:text-white p-5 shadow-sm flex flex-col justify-between h-full min-h-[190px]"
  >
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-2">
        <div
          class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0"
        >
          <BarChart3 class="w-4 h-4" />
        </div>
        <div>
          <h3 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white">
            {{ $t('review.forecast_title') }}
          </h3>
          <p class="text-[11px] text-slate-500 dark:text-slate-400">
            {{ $t('review.forecast_desc_7d') }}
          </p>
        </div>
      </div>
      <span class="text-xs font-mono font-bold text-brand-500 whitespace-nowrap shrink-0">
        <span class="tabular-nums">{{ totalUpcoming }}</span> {{ $t('review.forecast_cards_count', { count: '' }).trim() || 'cards' }}
      </span>
    </div>

    <!-- 7-Day Mini Bar Chart -->
    <div class="flex items-end justify-between gap-1.5 h-16 pt-2 px-1">
      <div
        v-for="day in forecastDays"
        :key="day.dateStr"
        class="group relative flex-1 flex flex-col items-center justify-end gap-1 h-full cursor-pointer"
      >
        <!-- Floating Tooltip on Hover -->
        <div
          class="absolute -top-8 left-1/2 -translate-x-1/2 px-2 py-0.5 rounded-lg bg-slate-900 dark:bg-slate-100 text-white dark:text-slate-900 text-[10px] font-bold whitespace-nowrap opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity duration-150 shadow-md z-30"
        >
          <span class="tabular-nums">{{ $t('review.forecast_cards_count', { count: day.count }) }}</span>
          <template v-if="day.isToday"> ({{ $t('review.today_badge') }})</template>
        </div>

        <!-- The Bar -->
        <div class="w-full flex-1 flex items-end justify-center">
          <div
            :style="{ height: `${getBarHeightPercent(day.count)}%` }"
            :class="[
              'w-full rounded-t-md transition-all duration-300',
              day.isToday
                ? (day.count > 0 ? 'bg-brand-500 shadow-sm shadow-brand-500/20' : 'bg-brand-500/30 dark:bg-brand-500/20')
                : (day.count > 0 ? 'bg-brand-500/60 dark:bg-brand-500/50 group-hover:bg-brand-500/80 transition-colors' : 'bg-slate-200/60 dark:bg-white/[0.06]')
            ]"
          />
        </div>

        <!-- Day Label -->
        <span
          :class="[
            'text-[10px] font-mono transition-colors',
            day.isToday
              ? 'font-bold text-brand-600 dark:text-brand-400'
              : 'text-slate-400 dark:text-slate-500'
          ]"
        >
          {{ day.dayLabel }}
        </span>
      </div>
    </div>

    <!-- Footer Summary -->
    <div
      class="pt-2 border-t border-slate-100 dark:border-white/[0.06] flex items-center justify-between text-xs text-slate-500 dark:text-slate-400"
    >
      <span>{{ $t('review.forecast_peak_day', { count: peakCount, day: peakDay }) }}</span>
      <span class="font-mono text-brand-500 font-semibold">{{ $t('review.forecast_daily_average', { count: averagePerDay }) }}</span>
    </div>
  </div>
</template>

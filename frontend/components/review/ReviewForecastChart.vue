<script setup lang="ts">
import { computed } from 'vue'
import { Calendar, BarChart3 } from 'lucide-vue-next'
import type { ReviewCard } from '~/stores/useReviewStore'

const props = defineProps<{
  cards: ReviewCard[]
}>()

const { locale, t } = useI18n()

interface ForecastDay {
  dateStr: string
  dayLabel: string
  dayNum: number
  count: number
  isToday: boolean
}

const viDays = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7']
const enDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']

const forecastDays = computed<ForecastDay[]>(() => {
  const result: ForecastDay[] = []
  const today = new Date()
  const isVi = locale.value?.startsWith('vi')

  for (let i = 0; i < 7; i++) {
    const d = new Date(today.getFullYear(), today.getMonth(), today.getDate() + i)
    const year = d.getFullYear()
    const month = String(d.getMonth() + 1).padStart(2, '0')
    const day = String(d.getDate()).padStart(2, '0')
    const dateStr = `${year}-${month}-${day}`
    const dayOfWeek = d.getDay()
    const dayLabel = isVi ? viDays[dayOfWeek] : enDays[dayOfWeek]

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

function getBarHeightPercent(count: number): number {
  if (count === 0) return 6
  return Math.min(100, Math.max(16, Math.round((count / maxCount.value) * 100)))
}
</script>

<template>
  <div
    class="rounded-3xl glass-card text-slate-900 dark:text-white p-5 sm:p-6 shadow-sm flex flex-col justify-between h-full min-h-[200px]"
  >
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-2">
        <div class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center">
          <Calendar class="w-4 h-4" :stroke-width="1.5" />
        </div>
        <div>
          <h3 class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            {{ $t('review.forecast_title') }}
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400 font-medium">
            <span class="tabular-nums">{{ totalUpcoming }}</span> {{ $t('review.forecast_cards_count', { count: '' }).trim() || 'cards' }} (7d)
          </p>
        </div>
      </div>

      <span class="inline-flex items-center gap-1 text-[11px] font-semibold text-slate-400 dark:text-slate-500">
        <BarChart3 class="w-3.5 h-3.5" :stroke-width="1.5" />
        <span>7d</span>
      </span>
    </div>

    <!-- 7-Day Mini Bar Chart -->
    <div class="pt-4 pb-1">
      <div class="grid grid-cols-7 gap-1.5 sm:gap-2 items-end h-24 sm:h-28">
        <div
          v-for="day in forecastDays"
          :key="day.dateStr"
          class="group relative flex flex-col items-center justify-end h-full cursor-pointer"
        >
          <!-- Floating Tooltip on Hover -->
          <div
            class="absolute -top-8 left-1/2 -translate-x-1/2 px-2 py-0.5 rounded-lg bg-slate-900 dark:bg-slate-100 text-white dark:text-slate-900 text-[10px] font-bold whitespace-nowrap opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity duration-150 shadow-md z-30"
          >
            <span class="tabular-nums">{{ day.count }}</span> {{ $t('review.forecast_cards_count', { count: '' }).trim() || 'cards' }}
            <template v-if="day.isToday">({{ $t('review.today_badge') }})</template>
          </div>

          <!-- The Bar -->
          <div class="w-full flex items-end justify-center flex-1">
            <div
              :style="{ height: `${getBarHeightPercent(day.count)}%` }"
              :class="[
                'w-full max-w-[28px] rounded-t-lg transition-all duration-300',
                day.isToday
                  ? 'bg-gradient-to-t from-brand-600 to-indigo-500 shadow-sm shadow-brand-500/20'
                  : day.count > 0
                    ? 'bg-slate-200 dark:bg-white/[0.12] group-hover:bg-brand-400 dark:group-hover:bg-brand-500'
                    : 'bg-slate-100 dark:bg-white/[0.04] border-t border-dashed border-slate-300 dark:border-white/[0.1]'
              ]"
            ></div>
          </div>

          <!-- Day Labels -->
          <div class="mt-2 text-center">
            <div
              :class="[
                'text-[10px] sm:text-xs font-bold leading-none',
                day.isToday ? 'text-brand-600 dark:text-brand-400' : 'text-slate-500 dark:text-slate-400'
              ]"
            >
              {{ day.dayLabel }}
            </div>
            <div
              v-if="day.isToday"
              class="w-1 h-1 rounded-full bg-brand-500 mx-auto mt-1"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Layers } from 'lucide-vue-next'
import type { SourceChannelRetention } from '~/stores/useReviewStore'

const props = defineProps<{
  sources: SourceChannelRetention[]
}>()

// CardSourceType arrives from the API as the enum NAME ('Highlight' | 'QuizMistake'
// | 'DocumentChunk'); numeric codes (1/2/3) are also mapped defensively so each
// source resolves to its own distinct label rather than collapsing to a fallback.
const SOURCE_LABEL_KEYS: Record<string | number, string> = {
  1: 'review.source_highlight',
  2: 'review.source_quiz',
  3: 'review.source_drill',
  Highlight: 'review.source_highlight',
  QuizMistake: 'review.source_quiz',
  DocumentChunk: 'review.source_drill',
}

const rows = computed(() =>
  props.sources.map((s) => ({
    sourceType: s.sourceType,
    labelKey: SOURCE_LABEL_KEYS[s.sourceType] ?? 'review.source_chunk',
    total: s.total,
    mastered: s.mastered,
    averageEaseFactor: s.averageEaseFactor,
    masteryRate: s.total > 0 ? Math.round((s.mastered / s.total) * 100) : 0,
  })),
)
</script>

<template>
  <div
    class="rounded-2xl glass-card text-slate-900 dark:text-white p-5 shadow-sm flex flex-col h-full min-h-[190px]"
  >
    <!-- Header -->
    <div class="flex items-center gap-2 mb-3">
      <div
        class="w-8 h-8 rounded-xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center"
      >
        <Layers class="w-4 h-4" />
      </div>
      <div>
        <h3 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white">
          {{ $t('review.source_title') }}
        </h3>
        <p class="text-[11px] text-slate-500 dark:text-slate-400">
          {{ $t('review.source_desc') }}
        </p>
      </div>
    </div>

    <!-- Rows -->
    <div v-if="rows.length > 0" class="flex flex-col gap-3 flex-1 justify-center">
      <div v-for="row in rows" :key="row.sourceType" class="space-y-1">
        <div class="flex items-center justify-between gap-2 text-xs">
          <span class="font-semibold text-slate-700 dark:text-slate-200 truncate">
            {{ $t(row.labelKey) }}
          </span>
          <span class="flex items-center gap-2 whitespace-nowrap shrink-0 text-slate-500 dark:text-slate-400">
            <span class="tabular-nums">{{ row.mastered }}/{{ row.total }}</span>
            <span
              class="px-1.5 py-0.5 rounded-md text-[10px] font-mono font-bold bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 tabular-nums"
            >
              {{ $t('review.source_ease') }} {{ row.averageEaseFactor.toFixed(2) }}
            </span>
          </span>
        </div>
        <div class="h-1.5 rounded-full bg-slate-100 dark:bg-white/[0.06] overflow-hidden">
          <div
            class="h-full rounded-full bg-brand-500 transition-all duration-700"
            :style="{ width: `${row.masteryRate}%` }"
          ></div>
        </div>
      </div>
    </div>

    <!-- Empty -->
    <div v-else class="flex-1 flex items-center justify-center">
      <p class="text-center text-xs text-slate-500 dark:text-slate-400">
        {{ $t('review.source_empty') }}
      </p>
    </div>
  </div>
</template>

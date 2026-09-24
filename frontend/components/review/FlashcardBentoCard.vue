<script setup lang="ts">
import { computed } from 'vue'
import { Pencil, RotateCcw, Trash2 } from 'lucide-vue-next'
import type { ReviewCard } from '~/stores/useReviewStore'
import { useMarkdownRenderer } from '~/composables/useMarkdownRenderer'

const props = defineProps<{
  card: ReviewCard
}>()

const emit = defineEmits<{
  (e: 'edit', card: ReviewCard): void
  (e: 'reset', card: ReviewCard): void
  (e: 'delete', card: ReviewCard): void
}>()

const { t } = useI18n()
const { render: renderMarkdown, isHighlighterReady } = useMarkdownRenderer()

const isDueToday = computed(() => {
  if (!props.card.nextReviewDate) return false
  const today = new Date().toISOString().slice(0, 10)
  return props.card.nextReviewDate <= today
})

const isMastered = computed(() => {
  return props.card.status === 2
})

const urgencyBadge = computed(() => {
  if (isDueToday.value) {
    return {
      label: t('review.card_urgency_due'),
      classes: 'bg-rose-500/10 text-rose-500 border-rose-500/20'
    }
  }
  if (isMastered.value) {
    return {
      label: t('review.card_urgency_mastered'),
      classes: 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20'
    }
  }
  return {
    label: t('review.card_urgency_learning'),
    classes: 'bg-amber-500/10 text-amber-500 border-amber-500/20'
  }
})

const renderedQuestion = computed(() => {
  const _ = isHighlighterReady.value
  const content = props.card.frontMarkdown || props.card.topicTitle || ''
  return renderMarkdown(content)
})

const plainAnswer = computed(() => {
  const content = props.card.backMarkdown || props.card.topicSummary || ''
  return content.replace(/[#*`_~>[\]()]/g, '').trim()
})

const sourceLabel = computed(() => {
  switch (props.card.sourceType) {
    case 1:
      return t('review.source_highlight')
    case 2:
      return t('review.source_quiz_mistake')
    default:
      return t('review.source_topic')
  }
})
</script>

<template>
  <div
    class="p-4 rounded-xl border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle hover:border-brand-500/40 transition-all flex flex-col justify-between space-y-3 shadow-sm hover:shadow-md"
  >
    <!-- Top Header row -->
    <div class="space-y-2">
      <div class="flex items-center justify-between gap-2">
        <span
          :class="[
            'px-2 py-0.5 rounded text-[10px] font-mono font-bold whitespace-nowrap shrink-0 border',
            urgencyBadge.classes
          ]"
        >
          {{ urgencyBadge.label }}
        </span>

        <span class="text-[11px] font-mono text-slate-400">
          {{ $t('review.card_ef_interval', { ef: (card.easeFactor ?? 2.5).toFixed(2), interval: card.intervalDays ?? 1 }) }}
        </span>

        <div class="flex items-center gap-1 shrink-0">
          <button
            type="button"
            @click="emit('edit', card)"
            class="p-1 rounded text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 cursor-pointer"
            :title="$t('review.edit_card')"
          >
            <Pencil class="w-3.5 h-3.5" :stroke-width="1.5" />
          </button>
          <button
            type="button"
            @click="emit('reset', card)"
            class="p-1 rounded text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 cursor-pointer"
            :title="$t('review.reset_progress')"
          >
            <RotateCcw class="w-3.5 h-3.5" :stroke-width="1.5" />
          </button>
          <button
            type="button"
            @click="emit('delete', card)"
            class="p-1 rounded text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 cursor-pointer"
            :title="$t('review.delete_card')"
          >
            <Trash2 class="w-3.5 h-3.5" :stroke-width="1.5" />
          </button>
        </div>
      </div>

      <!-- Middle Body: Question & Answer preview -->
      <h4
        class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white leading-snug line-clamp-2"
        v-html="renderedQuestion"
      ></h4>
      <p class="text-xs text-slate-500 dark:text-slate-400 line-clamp-2">
        {{ plainAnswer }}
      </p>
    </div>

    <!-- Footer row -->
    <div class="flex items-center justify-between pt-2 border-t border-slate-100 dark:border-white/[0.06] text-xs">
      <span class="text-slate-400 text-[11px] truncate max-w-[180px]">
        {{ $t('review.card_source', { source: sourceLabel }) }}
      </span>
      <button
        type="button"
        @click="emit('edit', card)"
        class="text-brand-500 hover:text-brand-400 font-bold text-xs whitespace-nowrap cursor-pointer"
      >
        {{ $t('review.card_details_btn') }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import {
  HelpCircle,
  FileText,
  ChevronDown,
  Pencil,
  RotateCcw,
  Trash2
} from 'lucide-vue-next'
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

const isExpanded = ref(false)
const { render: renderMarkdown, isHighlighterReady } = useMarkdownRenderer()

const renderedFront = computed(() => {
  const _ = isHighlighterReady.value
  const content = props.card.frontMarkdown || props.card.topicTitle || ''
  return renderMarkdown(content)
})

const renderedBack = computed(() => {
  const _ = isHighlighterReady.value
  const content = props.card.backMarkdown || props.card.topicSummary || ''
  return renderMarkdown(content)
})

const isDueToday = computed(() => {
  if (!props.card.nextReviewDate) return false
  const today = new Date().toISOString().slice(0, 10)
  return props.card.nextReviewDate <= today
})

const sourceBadge = computed(() => {
  switch (props.card.sourceType) {
    case 1:
      return {
        labelKey: 'review.source_highlight',
        classes: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20'
      }
    case 2:
      return {
        labelKey: 'review.source_quiz_mistake',
        classes: 'bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20'
      }
    default:
      return {
        labelKey: 'review.source_topic',
        classes: 'bg-sky-500/10 text-sky-600 dark:text-sky-400 border border-sky-500/20'
      }
  }
})

const statusBadge = computed(() => {
  switch (props.card.status) {
    case 1:
      return {
        labelKey: 'review.status_reviewing',
        classes: 'bg-sky-500/10 text-sky-600 dark:text-sky-400 border border-sky-500/20'
      }
    case 2:
      return {
        labelKey: 'review.status_mastered',
        classes: 'bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20'
      }
    default:
      return {
        labelKey: 'review.status_learning',
        classes: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20'
      }
  }
})
</script>

<template>
  <div
    class="rounded-3xl glass-card hover:border-brand-500/40 dark:hover:border-white/[0.16] text-slate-900 dark:text-white p-5 shadow-sm hover:shadow-md transition-all flex flex-col justify-between h-full space-y-4"
  >
    <!-- Top Header: Source Badge, Status Badge, Due Today, Actions -->
    <div class="flex items-start justify-between gap-2 border-b border-slate-100 dark:border-white/[0.06] pb-3">
      <div class="flex flex-wrap items-center gap-1.5 min-w-0">
        <!-- Source Badge -->
        <span
          :class="[
            'px-2.5 py-0.5 rounded-full text-[11px] font-bold border whitespace-nowrap shrink-0',
            sourceBadge.classes
          ]"
        >
          {{ $t(sourceBadge.labelKey) }}
        </span>

        <!-- Status Badge -->
        <span
          :class="[
            'px-2.5 py-0.5 rounded-full text-[11px] font-bold border whitespace-nowrap shrink-0',
            statusBadge.classes
          ]"
        >
          {{ $t(statusBadge.labelKey) }}
        </span>

        <!-- Due Today Badge -->
        <span
          v-if="isDueToday"
          class="px-2 py-0.5 rounded-full text-[11px] font-bold bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/30 whitespace-nowrap shrink-0"
        >
          {{ $t('review.due_today') }}
        </span>
      </div>

      <!-- Action Buttons -->
      <div class="flex items-center gap-1 shrink-0">
        <button
          type="button"
          @click="emit('edit', card)"
          class="p-1.5 rounded-xl text-slate-400 hover:text-brand-600 dark:hover:text-brand-400 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors cursor-pointer"
          :title="$t('review.edit_card')"
        >
          <Pencil class="w-3.5 h-3.5" />
        </button>

        <button
          type="button"
          @click="emit('reset', card)"
          class="p-1.5 rounded-xl text-slate-400 hover:text-amber-600 dark:hover:text-amber-400 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors cursor-pointer"
          :title="$t('review.reset_progress')"
        >
          <RotateCcw class="w-3.5 h-3.5" />
        </button>

        <button
          type="button"
          @click="emit('delete', card)"
          class="p-1.5 rounded-xl text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors cursor-pointer"
          :title="$t('review.delete_card')"
        >
          <Trash2 class="w-3.5 h-3.5" />
        </button>
      </div>
    </div>

    <!-- Middle Body: Front Prompt -->
    <div class="space-y-2 flex-1">
      <div class="text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 flex items-center gap-1">
        <HelpCircle class="w-3.5 h-3.5 text-brand-500" />
        <span>{{ $t('review.front_label') }}</span>
      </div>
      <div
        class="text-xs sm:text-sm text-slate-900 dark:text-slate-100 leading-relaxed font-sans prose dark:prose-invert max-w-none break-words"
        v-html="renderedFront"
      ></div>
    </div>

    <!-- Accordion Section: Show / Hide Answer -->
    <div class="pt-2 border-t border-slate-100 dark:border-white/[0.06] space-y-2">
      <button
        type="button"
        @click="isExpanded = !isExpanded"
        class="w-full flex items-center justify-between p-2 rounded-xl text-xs font-semibold text-brand-600 dark:text-brand-400 hover:bg-brand-50/50 dark:hover:bg-brand-500/10 transition-all cursor-pointer"
      >
        <span class="flex items-center gap-1.5">
          <FileText class="w-3.5 h-3.5 text-brand-500" />
          <span>{{ isExpanded ? $t('review.hide_answer') : $t('review.show_answer') }}</span>
        </span>
        <ChevronDown
          :class="[
            'w-4 h-4 transition-transform duration-200',
            isExpanded ? 'rotate-180' : 'rotate-0'
          ]"
        />
      </button>

      <!-- Collapsible Back Markdown Container -->
      <div
        v-if="isExpanded"
        class="p-3.5 rounded-2xl bg-brand-500/10 border border-brand-500/20 text-xs sm:text-sm text-slate-900 dark:text-slate-100 leading-relaxed font-sans prose dark:prose-invert max-w-none break-words animate-in fade-in duration-150"
        v-html="renderedBack"
      ></div>
    </div>

    <!-- Footer: SM-2 Badges & Next Review Date -->
    <div class="pt-3 border-t border-slate-100 dark:border-white/[0.06] flex flex-wrap items-center justify-between gap-2 text-[11px] font-mono text-slate-500 dark:text-slate-400">
      <div class="flex flex-wrap items-center gap-1.5">
        <span class="px-2 py-0.5 rounded-lg bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.06] whitespace-nowrap">
          {{ $t('review.repetitions', { count: card.repetitionCount }) }}
        </span>
        <span class="px-2 py-0.5 rounded-lg bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.06] whitespace-nowrap">
          {{ $t('review.interval_days', { days: card.intervalDays }) }}
        </span>
        <span class="px-2 py-0.5 rounded-lg bg-slate-100 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.06] whitespace-nowrap">
          {{ $t('review.ease_factor', { factor: (card.easeFactor ?? 2.5).toFixed(2) }) }}
        </span>
      </div>

      <div class="font-sans font-semibold text-slate-600 dark:text-slate-300 text-[11px]">
        {{ $t('review.next_review', { date: card.nextReviewDate }) }}
      </div>
    </div>
  </div>
</template>

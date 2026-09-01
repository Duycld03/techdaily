<script setup lang="ts">
import { ref, computed } from 'vue'
import { Layers, Eye, Sparkles } from 'lucide-vue-next'
import MarkdownIt from 'markdown-it'
import type { ReviewCard } from '~/stores/useReviewStore'
import Sm2GradingButtons from '~/components/review/Sm2GradingButtons.vue'

const props = defineProps<{
  card: ReviewCard
  remainingCount: number
}>()

const emit = defineEmits<{
  (e: 'grade', grade: number): void
}>()

const isFlipped = ref(false)
const md = new MarkdownIt({ html: true, linkify: true, typographer: true })

const renderedDeepDive = computed(() => {
  return md.render(props.card.topicDeepDiveMarkdown || props.card.topicSummary)
})

function handleGrade(score: number) {
  emit('grade', score)
  isFlipped.value = false
}
</script>

<template>
  <div class="w-full max-w-2xl mx-auto flex flex-col items-center space-y-6">
    <!-- Card Progress Bar -->
    <div class="flex items-center justify-between w-full text-sm text-slate-600 dark:text-slate-400 font-semibold px-2">
      <span class="flex items-center gap-2 text-brand-600 dark:text-brand-400 font-bold">
        <Layers class="w-4 h-4" />
        <span>Card 1 of {{ remainingCount }}</span>
      </span>
      <span class="px-3 py-1 rounded-full bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 font-mono text-xs shadow-sm">
        EF: {{ card.easeFactor.toFixed(2) }} • Interval: {{ card.intervalDays }}d
      </span>
    </div>

    <!-- Active Card Box -->
    <div class="w-full min-h-[320px] sm:min-h-[380px] p-5 sm:p-8 rounded-2xl sm:rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl dark:shadow-2xl flex flex-col justify-between transition-all">
      <!-- Front Content -->
      <div>
        <div class="flex items-center gap-2 mb-3.5 sm:mb-4">
          <span class="px-2.5 sm:px-3 py-0.5 sm:py-1 rounded-xl bg-brand-100 dark:bg-brand-950/80 border border-brand-200 dark:border-brand-800/60 text-brand-800 dark:text-brand-300 text-xs font-bold">
            Senior Core
          </span>
          <span class="text-xs text-slate-500 font-mono">Repetition #{{ card.repetitionCount }}</span>
        </div>

        <h2 class="text-lg sm:text-2xl font-extrabold text-slate-900 dark:text-white leading-tight mb-3.5 sm:mb-4">
          {{ card.topicTitle }}
        </h2>

        <p class="text-sm sm:text-base text-slate-700 dark:text-slate-300 leading-relaxed bg-slate-50 dark:bg-slate-950/60 p-4 sm:p-5 rounded-2xl border border-slate-200 dark:border-slate-800/80">
          {{ card.topicSummary }}
        </p>

        <!-- Flipped Answer Content -->
        <div v-if="isFlipped" class="mt-6 pt-6 border-t border-slate-200 dark:border-slate-800 animate-in fade-in zoom-in-95 duration-200">
          <div class="text-xs sm:text-sm font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400 mb-2.5 flex items-center gap-1.5">
            <Sparkles class="w-4 h-4" />
            <span>Deep Dive Explanation:</span>
          </div>
          <div class="markdown-body text-sm sm:text-base text-slate-800 dark:text-slate-200 leading-relaxed" v-html="renderedDeepDive"></div>
        </div>
      </div>

      <!-- Flip Card Button -->
      <div v-if="!isFlipped" class="mt-8 flex justify-center">
        <button
          @click="isFlipped = true"
          class="flex items-center gap-2 px-7 py-3.5 rounded-2xl bg-slate-800 dark:bg-slate-800 hover:bg-slate-700 text-white font-bold text-sm border border-slate-700 transition-all shadow-lg active:scale-[0.98]"
        >
          <Eye class="w-4 h-4" />
          <span>{{ $t('review.show_answer') }}</span>
        </button>
      </div>
    </div>

    <!-- SM-2 Grading Buttons (Only visible when flipped) -->
    <div v-if="isFlipped" class="w-full animate-in fade-in slide-in-from-bottom-2 duration-200">
      <Sm2GradingButtons @grade="handleGrade" />
    </div>
  </div>
</template>

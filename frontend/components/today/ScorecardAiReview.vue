<script setup lang="ts">
import { ref, computed } from 'vue'
import { CheckCircle2, AlertTriangle, Sparkles, ChevronDown, ChevronUp } from 'lucide-vue-next'
import MarkdownIt from 'markdown-it'
import type { AiReview } from '~/stores/useDailyFocusStore'

const props = defineProps<{
  review: AiReview
}>()

const md = new MarkdownIt({ html: true, linkify: true, typographer: true })
const showModelAnswer = ref(false)

const scoreColor = computed(() => {
  if (props.review.score >= 8) return 'text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-500/30 bg-emerald-100 dark:bg-emerald-500/10'
  if (props.review.score >= 6) return 'text-amber-600 dark:text-amber-400 border-amber-300 dark:border-amber-500/30 bg-amber-100 dark:bg-amber-500/10'
  return 'text-rose-600 dark:text-rose-400 border-rose-300 dark:border-rose-500/30 bg-rose-100 dark:bg-rose-500/10'
})

const renderedImprovedAnswer = computed(() => {
  return md.render(props.review.improvedAnswerMarkdown || '')
})
</script>

<template>
  <div class="p-6 sm:p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl dark:shadow-2xl space-y-6 animate-in fade-in duration-300 transition-colors">
    <!-- Header score row -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-3.5 sm:gap-4">
        <div :class="['w-14 h-14 sm:w-16 sm:h-16 rounded-2xl border-2 flex items-center justify-center font-black text-2xl sm:text-3xl shadow-sm', scoreColor]">
          {{ review.score }}
        </div>
        <div>
          <div class="flex flex-wrap items-center gap-2.5">
            <span class="text-base sm:text-lg font-bold text-slate-900 dark:text-white">{{ $t('today.score') }}: {{ review.score }} / 10</span>
            <span class="text-xs px-3 py-1 rounded-lg bg-brand-100 dark:bg-brand-950 border border-brand-200 dark:border-brand-800 text-brand-800 dark:text-brand-300 font-mono font-bold">
              {{ review.aiModelUsed || 'gemini-3.5-flash' }}
            </span>
          </div>
          <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-1">Evaluated across accuracy, latency, and memory design</p>
        </div>
      </div>
    </div>

    <!-- Summary Feedback -->
    <div class="p-5 sm:p-6 rounded-2xl bg-slate-50 dark:bg-slate-950/80 border border-slate-200 dark:border-slate-800 text-base sm:text-lg text-slate-800 dark:text-slate-200 leading-relaxed font-normal shadow-inner">
      {{ review.summaryFeedback }}
    </div>

    <!-- Strengths -->
    <div v-if="review.strengths?.length" class="space-y-3">
      <h4 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-emerald-600 dark:text-emerald-400 flex items-center gap-2">
        <CheckCircle2 class="w-4 h-4" />
        <span>{{ $t('today.strengths') }}</span>
      </h4>
      <ul class="space-y-2">
        <li
          v-for="(s, i) in review.strengths"
          :key="i"
          class="text-sm sm:text-base text-slate-800 dark:text-slate-200 flex items-start gap-3 bg-emerald-50/80 dark:bg-emerald-950/20 p-4 rounded-xl border border-emerald-200 dark:border-emerald-900/40 leading-relaxed"
        >
          <span class="w-2 h-2 rounded-full bg-emerald-500 mt-2 shrink-0"></span>
          <span>{{ s }}</span>
        </li>
      </ul>
    </div>

    <!-- Missing Points -->
    <div v-if="review.missingPoints?.length" class="space-y-3">
      <h4 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-amber-600 dark:text-amber-400 flex items-center gap-2">
        <AlertTriangle class="w-4 h-4" />
        <span>{{ $t('today.missing_points') }}</span>
      </h4>
      <ul class="space-y-2">
        <li
          v-for="(m, i) in review.missingPoints"
          :key="i"
          class="text-sm sm:text-base text-slate-800 dark:text-slate-200 flex items-start gap-3 bg-amber-50/80 dark:bg-amber-950/20 p-4 rounded-xl border border-amber-200 dark:border-amber-900/40 leading-relaxed"
        >
          <span class="w-2 h-2 rounded-full bg-amber-500 mt-2 shrink-0"></span>
          <span>{{ m }}</span>
        </li>
      </ul>
    </div>

    <!-- Improved / Principal Answer Toggle -->
    <div class="pt-2 border-t border-slate-200 dark:border-slate-800">
      <button
        @click="showModelAnswer = !showModelAnswer"
        class="w-full flex items-center justify-between p-4 rounded-2xl bg-slate-100 dark:bg-slate-950 hover:bg-slate-200 dark:hover:bg-slate-800/80 border border-slate-200 dark:border-slate-800 text-sm sm:text-base font-bold text-slate-900 dark:text-slate-200 transition-all shadow-sm active:scale-[0.99]"
      >
        <span class="flex items-center gap-2.5">
          <Sparkles class="w-5 h-5 text-brand-600 dark:text-brand-400" />
          <span>{{ $t('today.improved_answer') }}</span>
        </span>
        <component :is="showModelAnswer ? ChevronUp : ChevronDown" class="w-5 h-5 text-slate-500 dark:text-slate-400" />
      </button>

      <div v-if="showModelAnswer" class="mt-4 p-6 sm:p-8 rounded-2xl bg-slate-50 dark:bg-slate-950/80 border border-slate-200 dark:border-slate-800 markdown-body text-sm sm:text-base leading-relaxed animate-in fade-in zoom-in-95 duration-200 shadow-inner" v-html="renderedImprovedAnswer"></div>
    </div>
  </div>
</template>

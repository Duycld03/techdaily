<script setup lang="ts">
import { Award, CheckCircle2, AlertTriangle, Sparkles, ChevronDown, ChevronUp } from 'lucide-vue-next'
import MarkdownIt from 'markdown-it'
import type { AiReview } from '~/stores/useDailyFocusStore'

const props = defineProps<{
  review: AiReview
}>()

const md = new MarkdownIt({ html: true, linkify: true, typographer: true })
const showModelAnswer = ref(false)

const scoreColor = computed(() => {
  if (props.review.score >= 8) return 'text-emerald-400 border-emerald-500/30 bg-emerald-500/10'
  if (props.review.score >= 6) return 'text-amber-400 border-amber-500/30 bg-amber-500/10'
  return 'text-rose-400 border-rose-500/30 bg-rose-500/10'
})

const renderedImprovedAnswer = computed(() => {
  return md.render(props.review.improvedAnswerMarkdown || '')
})
</script>

<template>
  <div class="p-6 rounded-2xl bg-slate-900 border border-slate-800 shadow-xl space-y-6 animate-in fade-in duration-300">
    <!-- Header score row -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-3">
        <div :class="['w-12 h-12 rounded-2xl border flex items-center justify-center font-black text-xl shadow-inner', scoreColor]">
          {{ review.score }}
        </div>
        <div>
          <div class="flex items-center gap-2">
            <span class="text-sm font-bold text-white">{{ $t('today.score') }}: {{ review.score }} / 10</span>
            <span class="text-xs px-2 py-0.5 rounded-full bg-brand-950 border border-brand-800 text-brand-300 font-mono">
              {{ review.aiModelUsed || 'Gemini 2.5 Flash' }}
            </span>
          </div>
          <p class="text-xs text-slate-400 mt-0.5">Evaluated across accuracy, latency, and memory design</p>
        </div>
      </div>
    </div>

    <!-- Summary Feedback -->
    <div class="p-4 rounded-xl bg-slate-950/60 border border-slate-800/80 text-sm text-slate-200 leading-relaxed">
      {{ review.summaryFeedback }}
    </div>

    <!-- Strengths -->
    <div v-if="review.strengths?.length" class="space-y-2">
      <h4 class="text-xs font-bold uppercase tracking-wider text-emerald-400 flex items-center gap-1.5">
        <CheckCircle2 class="w-3.5 h-3.5" />
        <span>{{ $t('today.strengths') }}</span>
      </h4>
      <ul class="space-y-1.5">
        <li
          v-for="(s, i) in review.strengths"
          :key="i"
          class="text-xs text-slate-300 flex items-start gap-2 bg-emerald-950/20 p-2.5 rounded-lg border border-emerald-900/40"
        >
          <span class="w-1.5 h-1.5 rounded-full bg-emerald-400 mt-1.5 shrink-0"></span>
          <span>{{ s }}</span>
        </li>
      </ul>
    </div>

    <!-- Missing Points -->
    <div v-if="review.missingPoints?.length" class="space-y-2">
      <h4 class="text-xs font-bold uppercase tracking-wider text-amber-400 flex items-center gap-1.5">
        <AlertTriangle class="w-3.5 h-3.5" />
        <span>{{ $t('today.missing_points') }}</span>
      </h4>
      <ul class="space-y-1.5">
        <li
          v-for="(m, i) in review.missingPoints"
          :key="i"
          class="text-xs text-slate-300 flex items-start gap-2 bg-amber-950/20 p-2.5 rounded-lg border border-amber-900/40"
        >
          <span class="w-1.5 h-1.5 rounded-full bg-amber-400 mt-1.5 shrink-0"></span>
          <span>{{ m }}</span>
        </li>
      </ul>
    </div>

    <!-- Improved / Principal Answer Toggle -->
    <div class="pt-2 border-t border-slate-800">
      <button
        @click="showModelAnswer = !showModelAnswer"
        class="w-full flex items-center justify-between p-3 rounded-xl bg-slate-950 hover:bg-slate-800/80 border border-slate-800 text-xs font-bold text-slate-200 transition-colors"
      >
        <span class="flex items-center gap-2">
          <Sparkles class="w-4 h-4 text-brand-400" />
          <span>{{ $t('today.improved_answer') }}</span>
        </span>
        <component :is="showModelAnswer ? ChevronUp : ChevronDown" class="w-4 h-4 text-slate-400" />
      </button>

      <div v-if="showModelAnswer" class="mt-3 p-4 rounded-xl bg-slate-950/80 border border-slate-800 markdown-body text-xs leading-relaxed" v-html="renderedImprovedAnswer"></div>
    </div>
  </div>
</template>

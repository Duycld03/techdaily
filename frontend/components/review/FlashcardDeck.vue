<script setup lang="ts">
import { ref } from 'vue'
import { Layers, Eye, EyeOff, Sparkles, CheckCircle2 } from 'lucide-vue-next'
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
    <div class="flex items-center justify-between w-full text-xs text-slate-400 font-semibold px-2">
      <span class="flex items-center gap-1.5 text-brand-400">
        <Layers class="w-4 h-4" />
        <span>Card 1 of {{ remainingCount }}</span>
      </span>
      <span class="px-2.5 py-0.5 rounded-full bg-slate-900 border border-slate-800 text-slate-300 font-mono">
        EF: {{ card.easeFactor.toFixed(2) }} • Interval: {{ card.intervalDays }}d
      </span>
    </div>

    <!-- Active Card Box -->
    <div class="w-full min-h-[360px] p-6 md:p-8 rounded-3xl bg-slate-900 border border-slate-800 shadow-2xl flex flex-col justify-between transition-all">
      <!-- Front Content -->
      <div>
        <div class="flex items-center gap-2 mb-4">
          <span class="px-2.5 py-1 rounded-lg bg-brand-950/80 border border-brand-800/60 text-brand-300 text-xs font-semibold">
            Senior Core
          </span>
          <span class="text-xs text-slate-500 font-mono">Repetition #{{ card.repetitionCount }}</span>
        </div>

        <h2 class="text-xl md:text-2xl font-bold text-white leading-tight mb-4">
          {{ card.topicTitle }}
        </h2>

        <p class="text-sm text-slate-300 leading-relaxed bg-slate-950/60 p-4 rounded-2xl border border-slate-800/80">
          {{ card.topicSummary }}
        </p>

        <!-- Flipped Answer Content -->
        <div v-if="isFlipped" class="mt-6 pt-6 border-t border-slate-800 animate-in fade-in zoom-in-95 duration-200">
          <div class="text-xs font-bold uppercase tracking-wider text-brand-400 mb-2 flex items-center gap-1.5">
            <Sparkles class="w-3.5 h-3.5" />
            <span>Deep Dive Explanation:</span>
          </div>
          <div class="markdown-body text-xs text-slate-200 leading-relaxed" v-html="renderedDeepDive"></div>
        </div>
      </div>

      <!-- Flip Card Button -->
      <div v-if="!isFlipped" class="mt-8 flex justify-center">
        <button
          @click="isFlipped = true"
          class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-slate-800 hover:bg-slate-700 text-white font-bold text-sm border border-slate-700 transition-all shadow-md active:scale-[0.98]"
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

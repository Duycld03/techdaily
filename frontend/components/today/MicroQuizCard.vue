<script setup lang="ts">
import { ref } from 'vue'
import { CheckCircle2, XCircle, HelpCircle } from 'lucide-vue-next'
import type { MicroQuiz } from '~/stores/useDailyFocusStore'

const props = defineProps<{
  quiz: MicroQuiz
}>()

const selectedOption = ref<number | null>(null)
const isSubmitted = ref(false)

function submitAnswer() {
  if (selectedOption.value !== null) {
    isSubmitted.value = true
  }
}
</script>

<template>
  <div class="mt-8 p-5 rounded-2xl bg-slate-900/90 border border-slate-800 shadow-sm">
    <div class="flex items-center gap-2 text-brand-400 font-semibold text-sm mb-3">
      <HelpCircle class="w-4 h-4" />
      <span>{{ $t('today.quiz_title') }}</span>
    </div>

    <div class="text-sm font-medium text-slate-100 mb-4 leading-snug">
      {{ quiz.question }}
    </div>

    <div class="space-y-2 mb-4">
      <button
        v-for="(option, index) in quiz.options"
        :key="index"
        @click="!isSubmitted && (selectedOption = index)"
        :disabled="isSubmitted"
        :class="[
          'w-full text-left p-3 rounded-xl text-xs sm:text-sm font-medium border transition-all flex items-center justify-between',
          selectedOption === index && !isSubmitted
            ? 'border-brand-500 bg-brand-500/10 text-brand-300'
            : isSubmitted && index === quiz.answerIndex
            ? 'border-emerald-500 bg-emerald-500/15 text-emerald-300'
            : isSubmitted && selectedOption === index && index !== quiz.answerIndex
            ? 'border-red-500 bg-red-500/15 text-red-300'
            : 'border-slate-800 bg-slate-950/40 text-slate-300 hover:border-slate-700'
        ]"
      >
        <span>{{ option }}</span>
        <span v-if="isSubmitted && index === quiz.answerIndex">
          <CheckCircle2 class="w-4 h-4 text-emerald-400" />
        </span>
        <span v-else-if="isSubmitted && selectedOption === index && index !== quiz.answerIndex">
          <XCircle class="w-4 h-4 text-red-400" />
        </span>
      </button>
    </div>

    <div v-if="!isSubmitted" class="flex justify-end">
      <button
        @click="submitAnswer"
        :disabled="selectedOption === null"
        class="px-4 py-2 rounded-xl text-xs font-semibold bg-brand-600 hover:bg-brand-500 disabled:opacity-40 disabled:cursor-not-allowed text-slate-950 transition-colors shadow-sm"
      >
        {{ $t('today.quiz_submit') }}
      </button>
    </div>

    <!-- Explanation box -->
    <div
      v-else
      :class="[
        'p-3.5 rounded-xl text-xs leading-relaxed border',
        selectedOption === quiz.answerIndex
          ? 'bg-emerald-950/30 border-emerald-800/60 text-emerald-300'
          : 'bg-amber-950/30 border-amber-800/60 text-amber-300'
      ]"
    >
      <div class="font-semibold mb-1">
        {{ selectedOption === quiz.answerIndex ? $t('today.quiz_correct') : $t('today.quiz_incorrect') }}
      </div>
      <p class="text-slate-300">{{ quiz.explanation }}</p>
    </div>
  </div>
</template>

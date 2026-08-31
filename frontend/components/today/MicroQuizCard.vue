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
  <div class="mt-8 p-5 sm:p-6 rounded-3xl bg-slate-50 dark:bg-slate-900/90 border border-slate-200 dark:border-slate-800 shadow-sm transition-colors duration-200">
    <div class="flex items-center gap-2 text-brand-700 dark:text-brand-400 font-bold text-xs sm:text-sm uppercase tracking-wider mb-3">
      <HelpCircle class="w-4 h-4" />
      <span>{{ $t('today.quiz_title') }}</span>
    </div>

    <div class="text-sm sm:text-base font-semibold text-slate-900 dark:text-slate-100 mb-4 leading-snug">
      {{ quiz.question }}
    </div>

    <div class="space-y-2 mb-4">
      <button
        v-for="(option, index) in quiz.options"
        :key="index"
        @click="!isSubmitted && (selectedOption = index)"
        :disabled="isSubmitted"
        :class="[
          'w-full text-left p-3.5 rounded-2xl text-xs sm:text-sm font-semibold border transition-all flex items-center justify-between',
          selectedOption === index && !isSubmitted
            ? 'border-brand-500 bg-brand-50 dark:bg-brand-500/10 text-brand-900 dark:text-brand-300 shadow-sm'
            : isSubmitted && index === quiz.answerIndex
            ? 'border-emerald-500 bg-emerald-50 dark:bg-emerald-500/15 text-emerald-900 dark:text-emerald-300'
            : isSubmitted && selectedOption === index && index !== quiz.answerIndex
            ? 'border-rose-500 bg-rose-50 dark:bg-rose-500/15 text-rose-900 dark:text-rose-300'
            : 'border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950/40 text-slate-700 dark:text-slate-300 hover:border-slate-300 dark:hover:border-slate-700 hover:bg-slate-100/60 dark:hover:bg-slate-800/40'
        ]"
      >
        <span>{{ option }}</span>
        <span v-if="isSubmitted && index === quiz.answerIndex">
          <CheckCircle2 class="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
        </span>
        <span v-else-if="isSubmitted && selectedOption === index && index !== quiz.answerIndex">
          <XCircle class="w-4 h-4 text-rose-600 dark:text-rose-400" />
        </span>
      </button>
    </div>

    <div v-if="!isSubmitted" class="flex justify-end">
      <button
        @click="submitAnswer"
        :disabled="selectedOption === null"
        class="px-5 py-2.5 rounded-xl text-xs font-semibold bg-brand-600 hover:bg-brand-500 disabled:opacity-40 disabled:cursor-not-allowed text-white transition-all shadow-sm active:scale-95"
      >
        {{ $t('today.quiz_submit') }}
      </button>
    </div>

    <!-- Explanation box -->
    <div
      v-else
      :class="[
        'p-4 rounded-2xl text-xs sm:text-sm leading-relaxed border space-y-1',
        selectedOption === quiz.answerIndex
          ? 'bg-emerald-50 dark:bg-emerald-950/30 border-emerald-200 dark:border-emerald-800/60 text-emerald-900 dark:text-emerald-300'
          : 'bg-amber-50 dark:bg-amber-950/30 border-amber-200 dark:border-amber-800/60 text-amber-900 dark:text-amber-300'
      ]"
    >
      <div class="font-bold">
        {{ selectedOption === quiz.answerIndex ? $t('today.quiz_correct') : $t('today.quiz_incorrect') }}
      </div>
      <p class="text-slate-700 dark:text-slate-300">{{ quiz.explanation }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  disabled?: boolean
}>()

const emit = defineEmits<{
  (e: 'grade', score: number): void
}>()
</script>

<template>
  <div class="grid grid-cols-2 sm:grid-cols-4 gap-2.5 sm:gap-3 w-full max-w-xl mx-auto">
    <!-- Again (Score 1) -->
    <button
      @click="emit('grade', 1)"
      :disabled="disabled"
      class="flex flex-col items-center justify-center p-3 sm:p-3.5 rounded-2xl border border-rose-200 dark:border-rose-800/80 bg-rose-50 dark:bg-rose-950/30 hover:bg-rose-100 dark:hover:bg-rose-900/40 text-rose-700 dark:text-rose-300 font-semibold text-xs transition-all active:scale-[0.98] shadow-sm"
    >
      <span class="font-bold text-sm sm:text-base">{{ $t('review.grade_again') }}</span>
      <span class="text-xs text-rose-500 dark:text-rose-400/80 mt-0.5">Reset streak</span>
    </button>

    <!-- Hard (Score 3) -->
    <button
      @click="emit('grade', 3)"
      :disabled="disabled"
      class="flex flex-col items-center justify-center p-3 sm:p-3.5 rounded-2xl border border-amber-200 dark:border-amber-800/80 bg-amber-50 dark:bg-amber-950/30 hover:bg-amber-100 dark:hover:bg-amber-900/40 text-amber-700 dark:text-amber-300 font-semibold text-xs transition-all active:scale-[0.98] shadow-sm"
    >
      <span class="font-bold text-sm sm:text-base">{{ $t('review.grade_hard') }}</span>
      <span class="text-xs text-amber-600 dark:text-amber-400/80 mt-0.5">Interval x1.2</span>
    </button>

    <!-- Good (Score 4) -->
    <button
      @click="emit('grade', 4)"
      :disabled="disabled"
      class="flex flex-col items-center justify-center p-3 sm:p-3.5 rounded-2xl border border-sky-200 dark:border-sky-800/80 bg-sky-50 dark:bg-sky-950/30 hover:bg-sky-100 dark:hover:bg-sky-900/40 text-sky-700 dark:text-sky-300 font-semibold text-xs transition-all active:scale-[0.98] shadow-sm"
    >
      <span class="font-bold text-sm sm:text-base">{{ $t('review.grade_good') }}</span>
      <span class="text-xs text-sky-600 dark:text-sky-400/80 mt-0.5">Interval x2.5</span>
    </button>

    <!-- Easy (Score 5) -->
    <button
      @click="emit('grade', 5)"
      :disabled="disabled"
      class="flex flex-col items-center justify-center p-3 sm:p-3.5 rounded-2xl border border-emerald-200 dark:border-emerald-800/80 bg-emerald-50 dark:bg-emerald-950/30 hover:bg-emerald-100 dark:hover:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300 font-semibold text-xs transition-all active:scale-[0.98] shadow-sm"
    >
      <span class="font-bold text-sm sm:text-base">{{ $t('review.grade_easy') }}</span>
      <span class="text-xs text-emerald-600 dark:text-emerald-400/80 mt-0.5">Bonus interval</span>
    </button>
  </div>
</template>

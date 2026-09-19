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
    <!-- Again (Score 1, Key [1]) -->
    <button
      @click="emit('grade', 1)"
      :disabled="disabled"
      type="button"
      class="group relative flex flex-col items-center justify-center p-3 sm:p-3.5 rounded-2xl border border-rose-200/80 dark:border-rose-500/30 bg-rose-50/80 dark:bg-rose-500/10 hover:bg-rose-100/90 dark:hover:bg-rose-500/20 text-rose-700 dark:text-rose-300 font-semibold transition-all active:scale-[0.98] shadow-sm hover:shadow-md hover:shadow-rose-500/10 cursor-pointer disabled:opacity-50 disabled:pointer-events-none"
    >
      <div class="flex items-center gap-1.5">
        <span class="font-bold text-sm sm:text-base tracking-tight">{{ $t('review.grade_again') }}</span>
        <kbd class="hidden sm:inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-rose-200/60 dark:bg-rose-500/20 text-rose-700 dark:text-rose-300 border border-rose-300/40 dark:border-rose-500/30">1</kbd>
      </div>
      <span class="text-xs text-rose-500 dark:text-rose-400/80 mt-1">{{ $t('review.grade_again_desc') }}</span>
    </button>

    <!-- Hard (Score 3, Key [2]) -->
    <button
      @click="emit('grade', 3)"
      :disabled="disabled"
      type="button"
      class="group relative flex flex-col items-center justify-center p-3 sm:p-3.5 rounded-2xl border border-amber-200/80 dark:border-amber-500/30 bg-amber-50/80 dark:bg-amber-500/10 hover:bg-amber-100/90 dark:hover:bg-amber-500/20 text-amber-700 dark:text-amber-300 font-semibold transition-all active:scale-[0.98] shadow-sm hover:shadow-md hover:shadow-amber-500/10 cursor-pointer disabled:opacity-50 disabled:pointer-events-none"
    >
      <div class="flex items-center gap-1.5">
        <span class="font-bold text-sm sm:text-base tracking-tight">{{ $t('review.grade_hard') }}</span>
        <kbd class="hidden sm:inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-amber-200/60 dark:bg-amber-500/20 text-amber-700 dark:text-amber-300 border border-amber-300/40 dark:border-amber-500/30">2</kbd>
      </div>
      <span class="text-xs text-amber-600 dark:text-amber-400/80 mt-1">{{ $t('review.grade_hard_desc') }}</span>
    </button>

    <!-- Good (Score 4, Key [3]) -->
    <button
      @click="emit('grade', 4)"
      :disabled="disabled"
      type="button"
      class="group relative flex flex-col items-center justify-center p-3 sm:p-3.5 rounded-2xl border border-sky-200/80 dark:border-sky-500/30 bg-sky-50/80 dark:bg-sky-500/10 hover:bg-sky-100/90 dark:hover:bg-sky-500/20 text-sky-700 dark:text-sky-300 font-semibold transition-all active:scale-[0.98] shadow-sm hover:shadow-md hover:shadow-sky-500/10 cursor-pointer disabled:opacity-50 disabled:pointer-events-none"
    >
      <div class="flex items-center gap-1.5">
        <span class="font-bold text-sm sm:text-base tracking-tight">{{ $t('review.grade_good') }}</span>
        <kbd class="hidden sm:inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-sky-200/60 dark:bg-sky-500/20 text-sky-700 dark:text-sky-300 border border-sky-300/40 dark:border-sky-500/30">3</kbd>
      </div>
      <span class="text-xs text-sky-600 dark:text-sky-400/80 mt-1">{{ $t('review.grade_good_desc') }}</span>
    </button>

    <!-- Easy (Score 5, Key [4]) -->
    <button
      @click="emit('grade', 5)"
      :disabled="disabled"
      type="button"
      class="group relative flex flex-col items-center justify-center p-3 sm:p-3.5 rounded-2xl border border-brand-200/80 dark:border-brand-500/30 bg-brand-50/80 dark:bg-brand-500/10 hover:bg-brand-100/90 dark:hover:bg-brand-500/20 text-brand-700 dark:text-brand-300 font-semibold transition-all active:scale-[0.98] shadow-sm hover:shadow-md hover:shadow-brand-500/10 cursor-pointer disabled:opacity-50 disabled:pointer-events-none"
    >
      <div class="flex items-center gap-1.5">
        <span class="font-bold text-sm sm:text-base tracking-tight">{{ $t('review.grade_easy') }}</span>
        <kbd class="hidden sm:inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-brand-200/60 dark:bg-brand-500/20 text-brand-700 dark:text-brand-300 border border-brand-300/40 dark:border-brand-500/30">4</kbd>
      </div>
      <span class="text-xs text-brand-600 dark:text-brand-400/80 mt-1">{{ $t('review.grade_easy_desc') }}</span>
    </button>
  </div>
</template>

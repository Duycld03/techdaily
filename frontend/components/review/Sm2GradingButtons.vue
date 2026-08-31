<script setup lang="ts">
defineProps<{
  disabled?: boolean
}>()

const emit = defineEmits<{
  (e: 'grade', score: number): void
}>()
</script>

<template>
  <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 w-full max-w-xl mx-auto">
    <!-- Again (Score 1) -->
    <button
      @click="emit('grade', 1)"
      :disabled="disabled"
      class="flex flex-col items-center justify-center p-3.5 rounded-xl border border-rose-800/80 bg-rose-950/30 hover:bg-rose-900/40 text-rose-300 font-semibold text-xs transition-all active:scale-[0.98]"
    >
      <span class="font-bold text-sm">{{ $t('review.grade_again') }}</span>
      <span class="text-[10px] text-rose-400/80 mt-0.5">Reset streak</span>
    </button>

    <!-- Hard (Score 3) -->
    <button
      @click="emit('grade', 3)"
      :disabled="disabled"
      class="flex flex-col items-center justify-center p-3.5 rounded-xl border border-amber-800/80 bg-amber-950/30 hover:bg-amber-900/40 text-amber-300 font-semibold text-xs transition-all active:scale-[0.98]"
    >
      <span class="font-bold text-sm">{{ $t('review.grade_hard') }}</span>
      <span class="text-[10px] text-amber-400/80 mt-0.5">Interval x1.2</span>
    </button>

    <!-- Good (Score 4) -->
    <button
      @click="emit('grade', 4)"
      :disabled="disabled"
      class="flex flex-col items-center justify-center p-3.5 rounded-xl border border-sky-800/80 bg-sky-950/30 hover:bg-sky-900/40 text-sky-300 font-semibold text-xs transition-all active:scale-[0.98]"
    >
      <span class="font-bold text-sm">{{ $t('review.grade_good') }}</span>
      <span class="text-[10px] text-sky-400/80 mt-0.5">Interval x2.5</span>
    </button>

    <!-- Easy (Score 5) -->
    <button
      @click="emit('grade', 5)"
      :disabled="disabled"
      class="flex flex-col items-center justify-center p-3.5 rounded-xl border border-emerald-800/80 bg-emerald-950/30 hover:bg-emerald-900/40 text-emerald-300 font-semibold text-xs transition-all active:scale-[0.98]"
    >
      <span class="font-bold text-sm">{{ $t('review.grade_easy') }}</span>
      <span class="text-[10px] text-emerald-400/80 mt-0.5">Bonus interval</span>
    </button>
  </div>
</template>

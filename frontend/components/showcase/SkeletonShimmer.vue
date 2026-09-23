<script setup lang="ts">
withDefaults(
  defineProps<{
    preset?: 'card' | 'quiz' | 'metric'
  }>(),
  {
    preset: 'card'
  }
)

const bar = 'animate-pulse rounded-md bg-slate-200/60 dark:bg-white/[0.06]'
</script>

<template>
  <!-- Card: header + 2 text lines -->
  <div v-if="preset === 'card'" class="glass-card p-4">
    <div class="flex items-center gap-3">
      <div :class="[bar, 'h-9 w-9 rounded-lg']" />
      <div class="flex-1 space-y-2">
        <div :class="[bar, 'h-3.5 w-2/5']" />
        <div :class="[bar, 'h-3 w-1/4']" />
      </div>
    </div>
    <div class="mt-3.5 space-y-2">
      <div :class="[bar, 'h-3 w-full']" />
      <div :class="[bar, 'h-3 w-4/5']" />
    </div>
  </div>

  <!-- Quiz: 4 compact option rows -->
  <div v-else-if="preset === 'quiz'" class="glass-card space-y-2 p-4">
    <div
      v-for="n in 4"
      :key="n"
      class="flex items-center gap-3 rounded-lg border border-slate-200/80 px-3 py-2.5 dark:border-white/[0.06]"
    >
      <div :class="[bar, 'h-7 w-7 shrink-0 rounded-md']" />
      <div :class="[bar, 'h-3 flex-1']" :style="{ maxWidth: [ '70%', '85%', '60%', '78%' ][n - 1] }" />
    </div>
  </div>

  <!-- Metric stat -->
  <div v-else class="glass-card p-4">
    <div class="flex items-center justify-between">
      <div :class="[bar, 'h-8 w-8 rounded-lg']" />
      <div :class="[bar, 'h-4 w-10']" />
    </div>
    <div class="mt-3 space-y-2">
      <div :class="[bar, 'h-6 w-1/2']" />
      <div :class="[bar, 'h-3 w-1/3']" />
    </div>
    <div :class="[bar, 'mt-3.5 h-1.5 w-full']" />
  </div>
</template>

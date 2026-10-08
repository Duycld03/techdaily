<script setup lang="ts">
import { Flame, Shield } from 'lucide-vue-next'

defineProps<{
  streak: number
  freezeCredits: number
}>()
</script>

<template>
  <div class="flex items-center gap-1.5 sm:gap-2 px-2 sm:px-3 py-1 sm:py-1.5 rounded-full bg-slate-100/90 dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-xs sm:text-sm shadow-sm transition-colors shrink-0">
    <!-- Streak Flame -->
    <div class="flex items-center gap-1 sm:gap-1.5 text-amber-600 dark:text-amber-400 font-semibold" title="Active Daily Streak">
      <Flame class="w-3.5 h-3.5 sm:w-4 sm:h-4 fill-amber-500 text-amber-500 drop-shadow-[0_0_8px_rgba(245,158,11,0.5)] animate-pulse" :stroke-width="1.5" />
      <span class="text-slate-800 dark:text-slate-100 font-bold">{{ streak }}<span class="text-xs font-normal text-slate-500 dark:text-slate-400 ml-0.5 sm:ml-1">d</span></span>
    </div>

    <div class="w-px h-3 bg-slate-300 dark:bg-slate-700 hidden xs:block"></div>

    <!-- Freeze Credits -->
    <div class="items-center gap-1 text-sky-600 dark:text-sky-400 text-xs font-semibold hidden xs:flex" title="Monthly Streak Freeze Credits (Max 2/mo)">
      <Shield class="w-3.5 h-3.5 fill-sky-500/20 text-sky-500 dark:text-sky-400" :stroke-width="1.5" />
      <span>{{ freezeCredits }}/2</span>
    </div>
  </div>
</template>

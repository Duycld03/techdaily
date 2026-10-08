<script setup lang="ts">
defineProps<{
  title: string
  subtitle?: string
  index?: string
}>()
</script>

<template>
  <section class="space-y-3.5">
    <div class="flex items-baseline gap-2.5 border-b border-slate-200/80 pb-2 dark:border-white/[0.06]">
      <span
        v-if="index"
        class="font-mono text-xs font-semibold tabular-nums text-brand-500 dark:text-brand-400"
        >{{ index }}</span
      >
      <div class="space-y-0.5">
        <h2 class="text-base font-semibold text-slate-900 dark:text-white">{{ title }}</h2>
        <p v-if="subtitle" class="text-xs text-slate-500 dark:text-slate-400">{{ subtitle }}</p>
      </div>
    </div>
    <slot />
  </section>
</template>

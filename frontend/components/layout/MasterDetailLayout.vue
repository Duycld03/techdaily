<script setup lang="ts">
/**
 * MasterDetailLayout — Settings & Configuration archetype.
 * Left sub-nav rail + wide responsive content panel.
 * Solves the "phone-width single column stretched with dead margins" problem.
 */
defineProps<{
  maxHeight?: string
}>()
</script>

<template>
  <div
    class="glass-card flex min-h-0 flex-col overflow-hidden"
    :class="{ 'flex-1': !maxHeight }"
    :style="maxHeight ? { height: maxHeight } : {}"
  >
    <!-- Header -->
    <header
      v-if="$slots.header"
      class="flex shrink-0 items-center justify-between gap-3 border-b border-slate-200/80 px-4 sm:px-6 py-3.5 dark:border-white/[0.08]"
    >
      <slot name="header" />
    </header>

    <div class="flex min-h-0 flex-1 flex-col md:flex-row">
      <!-- Left sub-navigation rail -->
      <nav
        class="shrink-0 gap-1 overflow-x-auto md:overflow-x-visible md:overflow-y-auto border-b border-slate-200/80 p-2.5 sm:p-3 dark:border-white/[0.08] md:w-72 md:border-b-0 md:border-r"
        aria-label="Settings sections"
      >
        <slot name="nav" />
      </nav>

      <!-- Right content panel -->
      <section
        class="min-h-0 min-w-0 flex-1 overflow-y-auto p-4 sm:p-6"
        aria-label="Settings content"
      >
        <slot name="content" />
      </section>
    </div>
  </div>
</template>

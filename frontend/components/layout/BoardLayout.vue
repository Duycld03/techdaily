<script setup lang="ts">
/**
 * BoardLayout — Content & Knowledge Browsing archetype.
 * Sticky top bar (search + primary action) → filter row → responsive grid → pagination.
 * Fills the full available width instead of a narrow centered list.
 */
defineProps<{
  maxHeight?: string
  flat?: boolean
}>()
</script>

<template>
  <div
    :class="[
      flat
        ? 'flex min-h-0 flex-col space-y-4 w-full'
        : 'glass-card flex min-h-0 flex-col overflow-hidden',
      { 'flex-1': !maxHeight }
    ]"
    :style="maxHeight ? { height: maxHeight } : {}"
  >
    <!-- Top bar: search + primary action -->
    <header
      v-if="$slots.header"
      :class="[
        flat
          ? 'flex shrink-0 flex-wrap items-center justify-between gap-3 w-full'
          : 'flex shrink-0 flex-wrap items-center justify-between gap-3 border-b border-slate-200 px-4 py-3 dark:border-white/[0.08]'
      ]"
    >
      <slot name="header" />
    </header>

    <!-- Filter row -->
    <div
      v-if="$slots.filters"
      :class="[
        flat
          ? 'flex shrink-0 flex-wrap items-center gap-2 w-full py-0.5'
          : 'flex shrink-0 flex-wrap items-center gap-2 border-b border-slate-200 px-4 py-2.5 dark:border-white/[0.08]'
      ]"
    >
      <slot name="filters" />
    </div>

    <!-- Scrollable content surface -->
    <div
      :class="[
        flat
          ? 'min-h-0 flex-1 w-full'
          : 'min-h-0 flex-1 overflow-y-auto p-4'
      ]"
    >
      <slot name="content" />
    </div>

    <!-- Pagination footer -->
    <footer
      v-if="$slots.pagination"
      :class="[
        flat
          ? 'flex shrink-0 items-center justify-between gap-3 w-full pt-2'
          : 'flex shrink-0 items-center justify-between gap-3 border-t border-slate-200 px-4 py-3 dark:border-white/[0.08]'
      ]"
    >
      <slot name="pagination" />
    </footer>
  </div>
</template>

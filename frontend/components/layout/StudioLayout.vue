<script setup lang="ts">
/**
 * StudioLayout — Focused Practice & Learning archetype.
 * Two-column, viewport-bound stage (left) + telemetry dock (right).
 * Solves the "single card in a massive black void" problem on wide desktops.
 */
defineProps<{
  /** Cap the internal height so the stage/dock scroll independently instead of the page. */
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
      class="flex shrink-0 items-center justify-between gap-3 border-b border-slate-200 px-4 py-3 dark:border-white/[0.08]"
    >
      <slot name="header" />
    </header>

    <!-- Body: stage + dock -->
    <div class="flex min-h-0 flex-1 flex-col lg:flex-row">
      <!-- Main action stage -->
      <section
        class="flex min-h-0 min-w-0 flex-1 flex-col overflow-y-auto lg:w-[68%]"
        aria-label="Practice stage"
      >
        <div class="mx-auto flex w-full max-w-2xl flex-1 flex-col justify-center p-5">
          <slot name="main" />
        </div>
      </section>

      <!-- Telemetry & context dock -->
      <aside
        v-if="$slots.dock"
        class="hidden min-h-0 shrink-0 flex-col gap-3.5 overflow-y-auto border-l border-slate-200 bg-slate-50/60 p-4 dark:border-white/[0.08] dark:bg-white/[0.02] lg:flex lg:w-[32%]"
        aria-label="Session telemetry"
      >
        <slot name="dock" />
      </aside>
    </div>

    <!-- Footer -->
    <footer
      v-if="$slots.footer"
      class="flex shrink-0 items-center justify-between gap-3 border-t border-slate-200 px-4 py-3 dark:border-white/[0.08]"
    >
      <slot name="footer" />
    </footer>
  </div>
</template>

<script setup lang="ts">
/**
 * BentoDashboardLayout — Executive Cockpit archetype.
 * Asymmetric 3-column Bento Grid (2 cols action stage + 1 col telemetry/cosmos).
 * Solves unbalanced column heights and wide horizontal card stretching on 1080p desktop displays.
 */
defineProps<{
  maxHeight?: string
}>()
</script>

<template>
  <div
    class="flex min-h-0 flex-col overflow-hidden max-w-7xl mx-auto px-3.5 sm:px-6 lg:px-8 py-3.5 sm:py-4 gap-3.5 sm:gap-4 w-full"
    :class="[maxHeight ? '' : 'flex-1']"
    :style="maxHeight ? { height: maxHeight } : {}"
  >
    <!-- Top Orientation Banner Slot -->
    <header v-if="$slots.header" class="shrink-0">
      <slot name="header" />
    </header>

    <!-- Main Asymmetric Bento Grid (2:1 split) -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-3.5 sm:gap-4 items-stretch min-h-0 flex-1">
      <!-- Left 2 Columns: Action Stage -->
      <section
        class="lg:col-span-2 flex flex-col justify-start gap-3.5 sm:gap-4 min-h-0"
        aria-label="Core action stage"
      >
        <slot name="action-stage" />
      </section>

      <!-- Right 1 Column: Telemetry & Constellation Dock -->
      <aside
        v-if="$slots['telemetry-dock']"
        class="lg:col-span-1 flex flex-col justify-start gap-3.5 sm:gap-4 min-h-0"
        aria-label="Telemetry and constellation dock"
      >
        <slot name="telemetry-dock" />
      </aside>
    </div>

    <!-- Optional Footer -->
    <footer v-if="$slots.footer" class="shrink-0">
      <slot name="footer" />
    </footer>
  </div>
</template>

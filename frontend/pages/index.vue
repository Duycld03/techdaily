<script setup lang="ts">
import { onMounted } from 'vue'
import { useDailyFocusStore } from '~/stores/useDailyFocusStore'
import TodayBentoDashboard from '~/components/today/TodayBentoDashboard.vue'
const focusStore = useDailyFocusStore()
const { locale } = useI18n()

onMounted(async () => {
  if (!focusStore.data) {
    await focusStore.fetchTodayFocus(undefined, undefined, locale.value)
  }
})
</script>

<template>
  <div class="h-full flex flex-col overflow-hidden bg-slate-50 dark:bg-canvas transition-colors duration-200">
    <!-- Loading State -->
    <div
      v-if="focusStore.isLoading && !focusStore.data"
      class="flex-1 flex flex-col items-center justify-center p-6 sm:p-8 text-center my-auto"
    >
      <div
        class="w-12 h-12 rounded-2xl bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800/60 flex items-center justify-center shadow-sm mb-4"
      >
        <div class="w-6 h-6 border-2 border-brand-500 border-t-transparent rounded-full animate-spin"></div>
      </div>
      <p class="text-sm sm:text-base font-semibold text-slate-700 dark:text-slate-300">
        {{ $t('dashboard.loading_dashboard') }}
      </p>
    </div>

    <!-- Main Bento Dashboard -->
    <div v-else class="flex-1 overflow-y-auto lg:overflow-hidden">
      <TodayBentoDashboard />
    </div>
  </div>
</template>

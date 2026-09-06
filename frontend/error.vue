<script setup lang="ts">
import { computed } from 'vue'
import type { NuxtError } from '#app'
import { AlertTriangle, Compass, ArrowLeft, RotateCcw } from 'lucide-vue-next'

const props = defineProps<{
  error: NuxtError
}>()

const { t } = useI18n()

const is404 = computed(() => props.error?.statusCode === 404)
const statusCode = computed(() => props.error?.statusCode || 500)

const title = computed(() => {
  return is404.value ? t('error.not_found_title') : t('error.server_error_title')
})

const description = computed(() => {
  return is404.value ? t('error.not_found_desc') : t('error.server_error_desc')
})

function handleHome() {
  clearError({ redirect: '/today' })
}

function handleRetry() {
  clearError()
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-slate-100 p-4 transition-colors duration-200">
    <div class="max-w-md w-full text-center space-y-6 bg-white dark:bg-slate-900/80 border border-slate-200 dark:border-slate-800 rounded-2xl p-6 sm:p-8 shadow-xl backdrop-blur-sm">
      <!-- Error Status Code Badge -->
      <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full text-xs font-semibold tracking-wide uppercase bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 whitespace-nowrap shrink-0">
        <component :is="is404 ? Compass : AlertTriangle" class="w-4 h-4 shrink-0" />
        <span>{{ t('error.status_code') }}: {{ statusCode }}</span>
      </div>

      <!-- Icon Display -->
      <div class="flex justify-center">
        <div class="w-20 h-20 sm:w-24 sm:h-24 rounded-2xl flex items-center justify-center bg-slate-100 dark:bg-slate-800/80 text-emerald-500 dark:text-emerald-400 border border-slate-200 dark:border-slate-700/60 shadow-inner">
          <component :is="is404 ? Compass : AlertTriangle" class="w-10 h-10 sm:w-12 sm:h-12" />
        </div>
      </div>

      <!-- Text Details -->
      <div class="space-y-2">
        <h1 class="text-xl sm:text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
          {{ title }}
        </h1>
        <p class="text-sm sm:text-base text-slate-600 dark:text-slate-400 leading-relaxed max-w-sm mx-auto">
          {{ description }}
        </p>
      </div>

      <!-- Action Buttons -->
      <div class="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3">
        <button
          type="button"
          class="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white font-medium text-sm sm:text-base shadow-lg shadow-emerald-600/20 transition-all duration-200 whitespace-nowrap shrink-0 cursor-pointer active:scale-95"
          @click="handleHome"
        >
          <ArrowLeft class="w-4 h-4 shrink-0" />
          <span>{{ t('error.btn_home') }}</span>
        </button>

        <button
          v-if="!is404"
          type="button"
          class="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 font-medium text-sm sm:text-base border border-slate-200 dark:border-slate-700 transition-all duration-200 whitespace-nowrap shrink-0 cursor-pointer active:scale-95"
          @click="handleRetry"
        >
          <RotateCcw class="w-4 h-4 shrink-0" />
          <span>{{ t('error.btn_retry') }}</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { Sparkles, RefreshCw, AlertCircle, Cpu } from 'lucide-vue-next'

const props = defineProps<{
  chapterTitle?: string
}>()

const emit = defineEmits<{
  (e: 'retry'): void
}>()

const elapsedSeconds = ref(0)
const isTimedOut = ref(false)
let timer: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  timer = setInterval(() => {
    elapsedSeconds.value++
    if (elapsedSeconds.value >= 6) {
      isTimedOut.value = true
    }
  }, 1000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})

function handleRetry() {
  elapsedSeconds.value = 0
  isTimedOut.value = false
  emit('retry')
}
</script>

<template>
  <div class="h-full flex flex-col justify-between p-4 sm:p-6 md:p-8 bg-white dark:bg-slate-900 border border-slate-200/90 dark:border-slate-800 rounded-3xl shadow-sm relative overflow-hidden">
    <!-- Ambient Background Glow -->
    <div class="absolute -top-16 -right-16 w-48 h-48 bg-brand-500/10 dark:bg-brand-500/20 rounded-full blur-3xl pointer-events-none animate-pulse"></div>

    <!-- Top Header / Status Indicator -->
    <div class="space-y-4">
      <div class="flex items-center justify-between gap-3">
        <div class="inline-flex items-center gap-2 px-3 py-1.5 rounded-xl bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800 text-brand-700 dark:text-brand-300 text-xs sm:text-sm font-bold tracking-wide">
          <Sparkles class="w-4 h-4 animate-spin text-brand-600 dark:text-brand-400" style="animation-duration: 3s;" />
          <span class="whitespace-nowrap shrink-0">{{ $t('pacer.ai_synthesis_badge') }}</span>
        </div>

        <div class="flex items-center gap-1.5 text-xs text-slate-400 dark:text-slate-500 font-mono">
          <span class="inline-block w-2 h-2 rounded-full bg-emerald-500 animate-ping"></span>
          <span>{{ elapsedSeconds }}s</span>
        </div>
      </div>

      <!-- Chapter Context -->
      <div v-if="chapterTitle" class="text-xs sm:text-sm font-semibold text-slate-500 dark:text-slate-400">
        {{ chapterTitle }}
      </div>

      <!-- Explanatory Prompt / Skeleton Header -->
      <div v-if="!isTimedOut" class="space-y-3">
        <p class="text-sm sm:text-base md:text-lg text-slate-700 dark:text-slate-300 font-medium leading-relaxed">
          {{ $t('pacer.ai_synthesis_desc') }}
        </p>

        <!-- Skeleton Question Lines -->
        <div class="space-y-2 pt-2">
          <div class="h-4 sm:h-5 bg-slate-200 dark:bg-slate-800 rounded-lg w-11/12 animate-pulse"></div>
          <div class="h-4 sm:h-5 bg-slate-200 dark:bg-slate-800 rounded-lg w-4/5 animate-pulse" style="animation-delay: 150ms;"></div>
          <div class="h-4 sm:h-5 bg-slate-200 dark:bg-slate-800 rounded-lg w-2/3 animate-pulse" style="animation-delay: 300ms;"></div>
        </div>
      </div>

      <!-- Timeout / Fallback Notice -->
      <div v-else class="p-4 rounded-2xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800 space-y-3 animate-in fade-in duration-200">
        <div class="flex items-start gap-2.5">
          <AlertCircle class="w-5 h-5 text-amber-600 dark:text-amber-400 shrink-0 mt-0.5" />
          <div class="space-y-1">
            <h4 class="text-xs sm:text-sm font-bold text-amber-900 dark:text-amber-200">
              {{ $t('pacer.timeout_title') }}
            </h4>
            <p class="text-xs sm:text-sm text-amber-800 dark:text-amber-300/90 leading-relaxed">
              {{ $t('pacer.timeout_desc') }}
            </p>
          </div>
        </div>

        <button
          @click="handleRetry"
          class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl bg-amber-600 hover:bg-amber-500 text-white text-xs sm:text-sm font-bold transition-all shadow-sm active:scale-95 whitespace-nowrap shrink-0"
        >
          <RefreshCw class="w-3.5 h-3.5 sm:w-4 sm:h-4" />
          <span>{{ $t('pacer.retry_action') }}</span>
        </button>
      </div>
    </div>

    <!-- Skeleton Option Buttons -->
    <div class="space-y-2.5 pt-6">
      <div
        v-for="i in 4"
        :key="i"
        class="h-12 sm:h-14 rounded-2xl bg-slate-100 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-800/80 p-3.5 flex items-center gap-3 animate-pulse"
        :style="{ animationDelay: `${i * 120}ms` }"
      >
        <div class="w-6 h-6 rounded-lg bg-slate-200 dark:bg-slate-700 shrink-0"></div>
        <div class="h-3.5 bg-slate-200 dark:bg-slate-700 rounded-md w-3/4"></div>
      </div>
    </div>

    <!-- Bottom Footer State -->
    <div class="pt-4 border-t border-slate-100 dark:border-slate-800/60 flex items-center justify-between text-xs text-slate-400 dark:text-slate-500">
      <div class="flex items-center gap-1.5 font-medium">
        <Cpu class="w-3.5 h-3.5 text-brand-500" />
        <span>Gemini 3.5 Flash-Lite</span>
      </div>
      <span class="italic">{{ $t('pacer.zero_wait_note') }}</span>
    </div>
  </div>
</template>

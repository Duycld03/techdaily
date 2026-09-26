<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Cpu, Loader2, Pause, Play, Volume2, Zap } from 'lucide-vue-next'
import type { ChunkSummary } from '~/stores/useLibraryStore'
import { useSliceAudio } from '~/composables/useSliceAudio'
import type { NarrationSource } from '~/composables/useSliceAudio'

const props = defineProps<{
  chunk: ChunkSummary | null
}>()

const { t } = useI18n()
const toast = useToast()

const {
  status,
  playing,
  currentTime,
  duration,
  downloadProgress,
  synthIndex,
  synthTotal,
  errorMessage,
  device,
  speed,
  loadAndPlay,
  play,
  pause,
  setSpeed,
  seek,
} = useSliceAudio()

const SPEED_STEPS = [0.75, 1, 1.25, 1.5, 2] as const

const available = computed(() => !!props.chunk?.isAiFormatted)

const source = computed<NarrationSource | null>(() => {
  if (!props.chunk) return null
  return {
    chunkId: props.chunk.id,
    markdown: props.chunk.originalTextMarkdown,
    language: props.chunk.language,
    isAiFormatted: !!props.chunk.isAiFormatted,
  }
})

// Track which slice is loaded so switching slices re-synthesizes rather than
// resuming the previous slice's audio.
const loadedId = ref<string | null>(null)

watch(() => props.chunk?.id, () => {
  pause()
  loadedId.value = null
})

watch(errorMessage, (message) => {
  if (message) toast.error(t('reader.audio_error'))
})

const isLoading = computed(() => status.value === 'loading')

function onToggle(): void {
  if (!source.value || !available.value) return
  if (playing.value) {
    pause()
    return
  }
  if (loadedId.value === source.value.chunkId) {
    void play()
    return
  }
  loadedId.value = source.value.chunkId
  void loadAndPlay(source.value)
}

function cycleSpeed(): void {
  const idx = SPEED_STEPS.indexOf(speed.value as (typeof SPEED_STEPS)[number])
  const next = SPEED_STEPS[(idx + 1) % SPEED_STEPS.length] ?? 1
  setSpeed(next)
}

function onSeek(event: Event): void {
  const value = Number((event.target as HTMLInputElement).value)
  if (Number.isFinite(value)) seek(value)
}

function formatTime(seconds: number): string {
  if (!Number.isFinite(seconds) || seconds < 0) return '0:00'
  const m = Math.floor(seconds / 60)
  const s = Math.floor(seconds % 60)
  return `${m}:${s.toString().padStart(2, '0')}`
}

const statusLabel = computed(() => {
  if (status.value !== 'loading') return ''
  if (synthTotal.value > 0 && synthIndex.value > 0) {
    return t('reader.audio_synthesizing', { current: synthIndex.value, total: synthTotal.value })
  }
  if (downloadProgress.value > 0) {
    const pct = Math.min(100, Math.max(0, Math.round(downloadProgress.value)))
    return t('reader.audio_downloading', { progress: pct })
  }
  return t('reader.audio_preparing')
})

const deviceLabel = computed(() => {
  if (device.value === 'webgpu') return t('reader.audio_device_gpu')
  if (device.value === 'wasm') return t('reader.audio_device_cpu')
  return ''
})

const deviceHint = computed(() => {
  if (device.value === 'webgpu') return t('reader.audio_device_gpu_hint')
  if (device.value === 'wasm') return t('reader.audio_device_cpu_hint')
  return ''
})
</script>

<template>
  <div
    v-if="available"
    class="flex items-center gap-2 sm:gap-3 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/80 dark:bg-canvas-subtle/70 px-3 py-2"
  >
    <!-- Play / Pause -->
    <button
      type="button"
      class="flex items-center gap-1.5 shrink-0 whitespace-nowrap rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-sm font-semibold px-3 py-1.5 transition-all active:scale-95 disabled:opacity-60"
      :disabled="isLoading"
      :aria-label="playing ? t('reader.audio_pause') : t('reader.audio_play')"
      @click="onToggle"
    >
      <Loader2 v-if="isLoading" class="w-4 h-4 animate-spin" :stroke-width="2" />
      <Pause v-else-if="playing" class="w-4 h-4" :stroke-width="2" />
      <Volume2 v-else class="w-4 h-4" :stroke-width="2" />
      <span>{{ t('reader.audio_listen') }}</span>
    </button>

    <!-- Loading status -->
    <span
      v-if="isLoading"
      class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 truncate"
    >
      {{ statusLabel }}
    </span>

    <!-- Scrubber + time (once we have audio) -->
    <template v-else-if="loadedId === chunk?.id && duration > 0">
      <input
        type="range"
        min="0"
        :max="duration"
        step="0.1"
        :value="currentTime"
        class="flex-1 min-w-16 accent-brand-500 cursor-pointer"
        :aria-label="t('reader.audio_listen')"
        @input="onSeek"
      >
      <span class="text-xs text-slate-500 dark:text-slate-400 shrink-0 whitespace-nowrap tabular-nums">
        {{ formatTime(currentTime) }} / {{ formatTime(duration) }}
      </span>
    </template>

    <span v-else class="flex-1" />

    <!-- Active compute device (GPU/CPU) -->
    <span
      v-if="device"
      class="shrink-0 whitespace-nowrap inline-flex items-center gap-1 rounded-lg border border-slate-200/80 dark:border-white/[0.08] text-slate-500 dark:text-slate-400 text-xs font-medium px-2 py-1"
      :title="deviceHint"
    >
      <component :is="device === 'webgpu' ? Zap : Cpu" class="w-3.5 h-3.5" :stroke-width="2" />
      <span class="hidden sm:inline">{{ deviceLabel }}</span>
    </span>

    <!-- Speed -->
    <button
      type="button"
      class="shrink-0 whitespace-nowrap rounded-xl border border-slate-200/80 dark:border-white/[0.08] text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-canvas-elevated text-xs sm:text-sm font-semibold px-2.5 py-1.5 transition-all tabular-nums"
      :aria-label="t('reader.audio_speed')"
      @click="cycleSpeed"
    >
      {{ speed }}x
    </button>
  </div>
</template>

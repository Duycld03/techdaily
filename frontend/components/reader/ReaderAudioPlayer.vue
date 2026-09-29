<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { Cloud, Cpu, Laptop, Loader2, Pause, Volume2 } from 'lucide-vue-next'
import AppSelect from '~/components/common/AppSelect.vue'
import type { ChunkSummary } from '~/stores/useLibraryStore'
import {
  type AudioEngine,
  CLOUD_VOICES,
  resolveCloudVoiceForLanguage,
  useSliceAudio,
} from '~/composables/useSliceAudio'
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
  targetBufferCount,
  device,
  errorMessage,
  errorInfo,
  speed,
  engineMode,
  selectedVoice,
  audioQuota,
  isNearQuota,
  isQuotaExhausted,
  setEngineMode,
  setVoice,
  fetchQuota,
  loadAndPlay,
  play,
  pause,
  setSpeed,
  seek,
} = useSliceAudio({
  onQuotaExhausted: () => {
    toast.error(t('reader.audio_quota_exhausted_toast'))
  },
})

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

const isVi = computed(() => props.chunk?.language?.toLowerCase().startsWith('vi') ?? false)

const availableVoiceOptions = computed(() => {
  const langKey = isVi.value ? 'vi' : 'en'
  const list = CLOUD_VOICES[langKey]
  return list.map(v => ({
    value: v.id,
    label: `${v.gender === 'female' ? t('reader.audio_voice_female') : t('reader.audio_voice_male')} (${v.id.split('-').slice(-2).join('-')})`,
    description: v.id,
  }))
})

const currentVoice = computed({
  get(): string {
    const langKey = isVi.value ? 'vi' : 'en'
    const scoped = typeof localStorage !== 'undefined' ? localStorage.getItem(`techdaily_reader_audio_voice_${langKey}`) : null
    return resolveCloudVoiceForLanguage(props.chunk?.language, selectedVoice.value || scoped)
  },
  set(val: string | number) {
    const voiceId = String(val)
    setVoice(voiceId)
    if (loadedId.value) {
      pause()
      loadedId.value = null
      if (source.value) {
        loadedId.value = source.value.chunkId
        void loadAndPlay(source.value)
      }
    }
  },
})

// Track which slice is loaded so switching slices re-synthesizes rather than
// resuming the previous slice's audio.
const loadedId = ref<string | null>(null)

watch(() => props.chunk?.id, () => {
  pause()
  loadedId.value = null
})

const formattedErrorMessage = computed(() => {
  if (errorInfo?.value?.code === 'DEVICE_OOM') {
    return t('reader.audio_error_oom')
  }
  if (errorInfo?.value?.code === 'DEVICE_INIT_FAILED') {
    return t('reader.audio_error_device')
  }
  if (errorInfo?.value?.code === 'NETWORK_ERROR') {
    return t('reader.audio_error_network')
  }
  if (errorMessage.value && errorMessage.value !== 'QUOTA_EXHAUSTED') {
    return t('reader.audio_error_with_reason', { message: errorMessage.value })
  }
  return t('reader.audio_error')
})

const canFallbackToCloud = computed(() => {
  return (
    status.value === 'error' &&
    engineMode.value === 'device' &&
    !isNearQuota.value &&
    !isQuotaExhausted.value &&
    (errorInfo?.value?.suggestCloudFallback ?? true)
  )
})

function onFallbackToCloud(): void {
  onToggleEngine('cloud', true)
}

watch(errorMessage, (message) => {
  if (message && message !== 'QUOTA_EXHAUSTED') {
    toast.error(formattedErrorMessage.value)
  }
})

const isLoading = computed(() => status.value === 'loading')

const isStreamingIncomplete = computed(() => {
  return engineMode.value === 'device' && synthTotal.value > 0 && synthIndex.value > 0 && synthIndex.value < synthTotal.value
})

const estimatedTotalDuration = computed(() => {
  if (!isStreamingIncomplete.value || duration.value <= 0 || synthIndex.value <= 0) return duration.value
  return Math.round((duration.value / synthIndex.value) * synthTotal.value)
})

function onToggle(): void {
  if (!source.value || !available.value) return
  if (playing.value) {
    pause()
    return
  }
  if (isLoading.value) {
    return
  }
  if (loadedId.value === source.value.chunkId && status.value === 'ready') {
    void play()
    return
  }
  loadedId.value = source.value.chunkId
  void loadAndPlay(source.value)
}

function onToggleEngine(mode: AudioEngine, autoPlay = playing.value): void {
  if (mode === 'cloud' && (isNearQuota.value || isQuotaExhausted.value)) {
    return
  }
  setEngineMode(mode)
  if (loadedId.value) {
    pause()
    loadedId.value = null
    if (source.value) {
      loadedId.value = source.value.chunkId
      void loadAndPlay(source.value, autoPlay)
    }
  }
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
  if (downloadProgress.value > 0 && synthIndex.value === 0) {
    const pct = Math.min(100, Math.max(0, Math.round(downloadProgress.value)))
    return t('reader.audio_downloading', { progress: pct })
  }
  const target = targetBufferCount?.value ?? 0
  if (target > 0 && synthIndex.value < target && synthTotal.value > 0) {
    return t('reader.audio_buffering', { current: synthIndex.value, total: target })
  }
  if (synthTotal.value > 0 && synthIndex.value > 0) {
    return t('reader.audio_synthesizing', { current: synthIndex.value, total: synthTotal.value })
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

onMounted(() => {
  void fetchQuota()
})
</script>

<template>
  <div
    v-if="available"
    class="flex flex-col sm:flex-row sm:flex-wrap sm:items-center min-h-[50px] gap-2 sm:gap-3 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/80 dark:bg-canvas-subtle/70 px-3 py-2"
  >
    <!-- Primary Controls Row -->
    <div class="flex items-center gap-2 sm:gap-3 flex-1 min-w-0 w-full sm:w-auto">
      <!-- Play / Pause -->
      <button
        type="button"
        class="h-8 inline-flex items-center gap-1.5 shrink-0 whitespace-nowrap rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-semibold px-2.5 sm:px-3 transition-all active:scale-95 disabled:opacity-60"
        :aria-label="playing ? t('reader.audio_pause') : t('reader.audio_play')"
        @click="onToggle"
      >
        <Loader2 v-if="isLoading" class="w-4 h-4 animate-spin" :stroke-width="2" />
        <Pause v-else-if="playing" class="w-4 h-4" :stroke-width="2" />
        <Volume2 v-else class="w-4 h-4" :stroke-width="2" />
        <span>{{ t('reader.audio_listen') }}</span>
      </button>

      <!-- Engine Mode Segmented Switch (Cloud vs Device) -->
      <div
        class="inline-flex items-center rounded-xl p-0.5 bg-slate-200/60 dark:bg-white/[0.06] border border-slate-200/80 dark:border-white/[0.08] shrink-0 whitespace-nowrap text-xs font-medium"
        role="group"
        :aria-label="t('reader.audio_engine_cloud_hint')"
      >
        <!-- Cloud Engine Toggle -->
        <button
          type="button"
          class="inline-flex items-center gap-1 px-2 sm:px-2.5 py-1 rounded-lg transition-all shrink-0 whitespace-nowrap disabled:opacity-40 disabled:cursor-not-allowed"
          :class="[
            engineMode === 'cloud'
              ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm font-semibold'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
          ]"
          :disabled="isNearQuota || isQuotaExhausted"
          :title="(isNearQuota || isQuotaExhausted) ? t('reader.audio_quota_near_limit_tooltip') : t('reader.audio_engine_cloud_hint')"
          @click="onToggleEngine('cloud')"
        >
          <Cloud class="w-3.5 h-3.5" :stroke-width="2" />
          <span class="hidden sm:inline">{{ t('reader.audio_engine_cloud') }}</span>
        </button>

        <!-- Device Engine Toggle -->
        <button
          type="button"
          class="inline-flex items-center gap-1 px-2 sm:px-2.5 py-1 rounded-lg transition-all shrink-0 whitespace-nowrap"
          :class="[
            engineMode === 'device'
              ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm font-semibold'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
          ]"
          :title="t('reader.audio_engine_device_hint')"
          @click="onToggleEngine('device')"
        >
          <Laptop class="w-3.5 h-3.5" :stroke-width="2" />
          <span class="hidden sm:inline">{{ t('reader.audio_engine_device') }}</span>
        </button>
      </div>

      <!-- Free-Tier Voice Picker (Only in Cloud mode) -->
      <div
        v-if="engineMode === 'cloud'"
        class="flex-1 min-w-[130px] sm:min-w-0 sm:w-44 sm:flex-none shrink-0"
      >
        <AppSelect
          v-model="currentVoice"
          :options="availableVoiceOptions"
          size="sm"
          :placeholder="t('reader.audio_voice_select_placeholder')"
          :aria-label="t('reader.audio_voice_select_placeholder')"
        />
      </div>

      <!-- Loading status -->
      <span
        v-if="isLoading"
        class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 truncate"
      >
        {{ statusLabel }}
      </span>

      <!-- Error status & Cloud fallback button -->
      <template v-else-if="status === 'error'">
        <span
          class="text-xs text-rose-500 dark:text-rose-400 truncate max-w-[180px] sm:max-w-xs"
          :title="formattedErrorMessage"
        >
          {{ formattedErrorMessage }}
        </span>
        <button
          v-if="canFallbackToCloud"
          type="button"
          class="h-7 inline-flex items-center gap-1.5 shrink-0 whitespace-nowrap rounded-lg bg-amber-500/10 hover:bg-amber-500/20 text-amber-600 dark:text-amber-400 border border-amber-500/30 text-xs font-semibold px-2.5 transition-all active:scale-95"
          @click="onFallbackToCloud"
        >
          <Cloud class="w-3.5 h-3.5" :stroke-width="2" />
          <span>{{ t('reader.audio_fallback_to_cloud') }}</span>
        </button>
      </template>

      <!-- Desktop Scrubber (Shown inline on tablet/desktop >= sm) -->
      <template v-else-if="loadedId === chunk?.id && duration > 0">
        <div class="hidden sm:flex sm:flex-1 sm:items-center sm:gap-2 min-w-0">
          <input
            type="range"
            min="0"
            :max="duration"
            step="0.1"
            :value="currentTime"
            class="flex-1 min-w-[80px] h-1.5 bg-slate-200 dark:bg-white/[0.12] rounded-lg appearance-none cursor-pointer accent-brand-600"
            :aria-label="t('reader.audio_play')"
            @input="onSeek"
          />
          <span class="text-xs text-slate-500 dark:text-slate-400 shrink-0 whitespace-nowrap tabular-nums">
            {{ formatTime(currentTime) }} / {{ formatTime(duration) }}<span v-if="isStreamingIncomplete && estimatedTotalDuration > duration"> (~{{ formatTime(estimatedTotalDuration) }})</span>
          </span>
          <span
            v-if="isStreamingIncomplete"
            class="text-xs text-brand-600 dark:text-brand-400 shrink-0 whitespace-nowrap font-medium"
          >
            ({{ t('reader.audio_synthesizing', { current: synthIndex, total: synthTotal }) }})
          </span>
        </div>
      </template>

      <!-- Desktop Spacer when idle -->
      <span v-else class="hidden sm:block sm:flex-1" />

      <!-- Active compute device (GPU/CPU) when in Device mode -->
      <span
        v-if="engineMode === 'device' && device"
        class="shrink-0 whitespace-nowrap hidden sm:inline-flex items-center gap-1 rounded-lg border border-slate-200/80 dark:border-white/[0.08] text-slate-500 dark:text-slate-400 text-xs font-medium px-2 py-1"
        :title="deviceHint"
      >
        <component :is="device === 'webgpu' ? Laptop : Cpu" class="w-3.5 h-3.5" :stroke-width="2" />
        <span>{{ deviceLabel }}</span>
      </span>

      <!-- Speed -->
      <button
        type="button"
        class="shrink-0 whitespace-nowrap rounded-lg border border-slate-200/80 dark:border-white/[0.08] hover:bg-slate-200/60 dark:hover:bg-white/[0.06] text-slate-600 dark:text-slate-300 text-xs font-semibold px-1.5 sm:px-2 py-1 transition-colors tabular-nums ml-auto sm:ml-0"
        :title="t('reader.audio_speed')"
        @click="cycleSpeed"
      >
        {{ speed }}x
      </button>
    </div>

    <!-- Dedicated Mobile Scrubber Track (Row 2, only when active and on mobile screens < sm) -->
    <div
      v-if="loadedId === chunk?.id && duration > 0"
      class="flex sm:hidden items-center gap-2 w-full pt-1.5 border-t border-slate-200/60 dark:border-white/[0.06]"
    >
      <input
        type="range"
        min="0"
        :max="duration"
        step="0.1"
        :value="currentTime"
        class="flex-1 min-w-[60px] h-1.5 bg-slate-200 dark:bg-white/[0.12] rounded-lg appearance-none cursor-pointer accent-brand-600"
        :aria-label="t('reader.audio_play')"
        @input="onSeek"
      />
      <span class="text-xs text-slate-500 dark:text-slate-400 shrink-0 whitespace-nowrap tabular-nums">
        {{ formatTime(currentTime) }} / {{ formatTime(duration) }}
      </span>
      <span
        v-if="isStreamingIncomplete"
        class="text-xs text-brand-600 dark:text-brand-400 shrink-0 whitespace-nowrap font-medium"
      >
        ({{ synthIndex }}/{{ synthTotal }})
      </span>
    </div>
  </div>
</template>

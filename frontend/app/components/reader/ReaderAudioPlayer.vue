<script setup lang="ts">
import { computed, onUnmounted, ref, shallowRef, watch } from 'vue'
import { onClickOutside } from '@vueuse/core'
import {
  Check,
  Cloud,
  Cpu,
  FastForward,
  Laptop,
  Loader2,
  Moon,
  Pause,
  Route,
  Volume2,
} from 'lucide-vue-next'
import AppSelect from '~/components/common/AppSelect.vue'
import type { ChunkSummary } from '~/stores/useLibraryStore'
import { useAuthStore } from '~/stores/useAuthStore'
import { browserStorage, readStored, type AudioEnginePreference } from '~/utils/clientStorage'
import {
  AUDIO_VOICE_STORAGE_KEY,
  CLOUD_VOICES,
  filterSystemVoicesForLanguage,
  resolveCloudVoiceForLanguage,
  useSliceAudio,
  type NarrationSource
} from '~/composables/useSliceAudio'

const props = withDefaults(
  defineProps<{
    chunk: ChunkSummary | null
    bookTitle?: string
    bookLanguage?: string | null
    disableAutoAdvance?: boolean
  }>(),
  {
    bookTitle: 'DeepPace',
    disableAutoAdvance: false,
  },
)

export interface AudioNavigationRequest {
  isCurrent: () => boolean
  accept: (chunkId: string) => boolean
  cancel: () => void
}

const emit = defineEmits<{
  (e: 'auto-advance', navigation: AudioNavigationRequest): void
  (e: 'seek-slice', direction: 'next' | 'prev', navigation: AudioNavigationRequest): void
}>()

const { t } = useI18n()
const toast = useToast()
const auth = useAuthStore()
let continuationGeneration = 0
let disposed = false

export type SleepTimerOption = 15 | 30 | 45 | 60 | 'end_of_slice' | null
const pendingNavigation = shallowRef<{
  request: AudioNavigationRequest
  targetChunkId: string | null
  autoplay: boolean
  reason: 'auto-advance' | 'source-change'
} | null>(null)
const sleepTimer = ref<SleepTimerOption>(null)
const sleepTimerRemaining = ref<number | null>(null)

function invalidateNavigation(): void {
  continuationGeneration++
  pendingNavigation.value = null
}

function createNavigation(isCurrent: () => boolean, reason: 'auto-advance' | 'source-change'): AudioNavigationRequest {
  invalidateNavigation()
  const generation = continuationGeneration
  const accountId = auth.user?.id
  const sessionGeneration = auth.sessionGeneration
  const request: AudioNavigationRequest = {
    isCurrent: () => !disposed && generation === continuationGeneration && isCurrent()
      && auth.user?.id === accountId && auth.sessionGeneration === sessionGeneration,
    accept(chunkId) {
      const pending = pendingNavigation.value
      if (pending?.request !== request || !request.isCurrent()) return false
      pendingNavigation.value = { ...pending, targetChunkId: chunkId }
      return true
    },
    cancel() {
      if (pendingNavigation.value?.request === request) invalidateNavigation()
    },
  }
  pendingNavigation.value = {
    request, targetChunkId: null, autoplay: reason === 'auto-advance' || playing.value, reason,
  }
  return request
}
const isSleepTimerOpen = ref(false)
const sleepTimerMenuRef = ref<HTMLElement | null>(null)
let sleepTimerInterval: ReturnType<typeof setInterval> | null = null

onClickOutside(sleepTimerMenuRef, () => {
  isSleepTimerOpen.value = false
})

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
  enginePreference,
  persistenceAvailable,
  restoredProgress,
  selectedVoice,
  selectedSystemVoice,
  systemVoices,
  autoAdvance,
  isNearQuota,
  isQuotaExhausted,
  setEnginePreference,
  resetSource,
  setVoice,
  setSystemVoice,
  setVolume,
  loadAndPlay,
  play,
  pause,
  setSpeed,
  seek,
  playSliceTransitionChime,
  updateMediaSessionMetadata,
} = useSliceAudio({
  getAccount: () => ({ accountId: auth.user?.id ?? null, sessionGeneration: auth.sessionGeneration }),
  onQuotaExhausted: () => {
    toast.error(t('reader.audio_quota_exhausted_toast'))
  },
  onFallback: ({ to, reason }) => {
    toast.info(t('reader.audio_fallback_restart', {
      engine: t(`reader.audio_engine_${to}`),
      reason: t(`reader.audio_fallback_reason_${reason}`),
    }))
  },
  onPersistenceUnavailable: () => {
    toast.info(t('reader.audio_persistence_unavailable'))
  },
  onSliceEnded: (isCurrent) => {
    if (sleepTimer.value === 'end_of_slice') {
      pausePlayback()
      sleepTimer.value = null
      setVolume(1)
      toast.info(t('reader.audio_sleep_timer_ended'))
      return
    }
    if (props.disableAutoAdvance || !autoAdvance.value) return
    const navigation = createNavigation(() => isCurrent()
      && !props.disableAutoAdvance && autoAdvance.value, 'auto-advance')
    void playSliceTransitionChime().then(() => {
      if (navigation.isCurrent()) emit('auto-advance', navigation)
    })
  },
  onPause: invalidateNavigation,
  onNextTrack: (isCurrent) => {
    if (!props.disableAutoAdvance) emit('seek-slice', 'next', createNavigation(isCurrent, 'source-change'))
  },
  onPreviousTrack: (isCurrent) => {
    if (!props.disableAutoAdvance) emit('seek-slice', 'prev', createNavigation(isCurrent, 'source-change'))
  },
})

watch([playing, () => auth.user?.id, () => auth.sessionGeneration], invalidateNavigation, { flush: 'sync' })
watch(autoAdvance, enabled => {
  if (!enabled && pendingNavigation.value?.reason === 'auto-advance') invalidateNavigation()
}, { flush: 'sync' })

function setSleepTimer(option: SleepTimerOption): void {
  sleepTimer.value = option
  isSleepTimerOpen.value = false
  if (typeof option === 'number') {
    sleepTimerRemaining.value = option * 60
    startSleepTimerCountdown()
  } else {
    stopSleepTimerCountdown()
    sleepTimerRemaining.value = null
    setVolume(1)
  }
}

function startSleepTimerCountdown(): void {
  stopSleepTimerCountdown()
  sleepTimerInterval = setInterval(() => {
    if (!playing.value) return
    if (sleepTimerRemaining.value === null) return
    sleepTimerRemaining.value -= 1

    if (sleepTimerRemaining.value <= 15 && sleepTimerRemaining.value > 0) {
      const ratio = sleepTimerRemaining.value / 15
      setVolume(Math.pow(ratio, 2))
    } else if (sleepTimerRemaining.value <= 0) {
      stopSleepTimerCountdown()
      pausePlayback()
      sleepTimer.value = null
      sleepTimerRemaining.value = null
      setVolume(1)
      toast.info(t('reader.audio_sleep_timer_ended'))
    }
  }, 1000)
}

function stopSleepTimerCountdown(): void {
  if (sleepTimerInterval) {
    clearInterval(sleepTimerInterval)
    sleepTimerInterval = null
  }
}

watch([currentTime, duration, sleepTimer], ([time, dur, timer]) => {
  if (timer === 'end_of_slice' && dur > 0 && playing.value) {
    const remaining = dur - time
    if (remaining <= 15 && remaining > 0) {
      const ratio = remaining / 15
      setVolume(Math.pow(ratio, 2))
    } else if (remaining > 15) {
      setVolume(1)
    }
  }
})

onUnmounted(() => {
  disposed = true
  invalidateNavigation()
  stopSleepTimerCountdown()
})

const sleepTimerLabel = computed(() => {
  if (sleepTimer.value === null) return null
  if (sleepTimer.value === 'end_of_slice') return t('reader.audio_sleep_timer_end_of_slice')
  if (sleepTimerRemaining.value !== null) {
    const m = Math.floor(sleepTimerRemaining.value / 60)
    const s = Math.floor(sleepTimerRemaining.value % 60)
    return `${m}:${s.toString().padStart(2, '0')}`
  }
  return `${sleepTimer.value}m`
})

const SPEED_STEPS = [0.75, 1, 1.25, 1.5, 2] as const
const ENGINE_OPTIONS = [
  { value: 'auto', icon: Route },
  { value: 'system', icon: Volume2 },
  { value: 'cloud', icon: Cloud },
  { value: 'device', icon: Laptop },
] as const

const available = computed(() => !!props.chunk?.isAiFormatted)
const narrationLanguage = computed(() => props.chunk?.language || props.bookLanguage || 'en')

const source = computed<NarrationSource | null>(() => {
  if (!props.chunk) return null
  return {
    chunkId: props.chunk.id,
    markdown: props.chunk.originalTextMarkdown,
    language: narrationLanguage.value,
    isAiFormatted: !!props.chunk.isAiFormatted,
  }
})

watch([() => props.chunk, playing], ([chunk, isPlaying]) => {
  if (isPlaying && chunk) {
    updateMediaSessionMetadata({
      title: chunk.chapterTitle || `Slice #${chunk.chunkOrder ?? 1}`,
      artist: 'DeepPace Reader',
      album: props.bookTitle || 'Technical Documentation',
    })
  }
})

const isVi = computed(() => narrationLanguage.value.toLowerCase().startsWith('vi'))

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
    const scoped = readStored(browserStorage('localStorage'), `${AUDIO_VOICE_STORAGE_KEY}_${langKey}`)
    return resolveCloudVoiceForLanguage(narrationLanguage.value, selectedVoice.value || scoped)
  },
  set(val: string | number) {
    const voiceId = String(val)
    if (voiceId === currentVoice.value) return
    invalidateNavigation()
    const shouldContinue = playing.value || isLoading.value
    setVoice(voiceId)
    loadedId.value = null
    if (shouldContinue && source.value) {
      loadedId.value = source.value.chunkId
      void loadAndPlay(source.value, { reason: 'voice-change' })
    }
  },
})

const availableSystemVoiceOptions = computed(() => {
  const matching = filterSystemVoicesForLanguage(systemVoices.value, narrationLanguage.value)
  if (matching.length === 0) {
    return [{
      value: '',
      label: t('reader.audio_system_voice_default'),
      description: t('reader.audio_system_voice_auto_desc'),
    }]
  }
  return matching.map(v => ({
    value: v.id,
    label: `${v.name} (${v.lang})`,
    description: v.localService ? t('reader.audio_device_local') : undefined,
  }))
})

const currentSystemVoice = computed({
  get(): string {
    const matching = filterSystemVoicesForLanguage(systemVoices.value, narrationLanguage.value)
    if (matching.length === 0) return ''
    const found = matching.find(v => v.id === selectedSystemVoice.value)
    return found ? found.id : (matching[0]?.id ?? '')
  },
  set(val: string | number) {
    const voiceId = String(val)
    if (voiceId === currentSystemVoice.value) return
    invalidateNavigation()
    const shouldContinue = playing.value || isLoading.value
    setSystemVoice(voiceId)
    loadedId.value = null
    if (shouldContinue && source.value) {
      loadedId.value = source.value.chunkId
      void loadAndPlay(source.value, { reason: 'voice-change' })
    }
  },
})
const currentVoiceName = computed(() => {
  if (engineMode.value === 'system') {
    const matching = filterSystemVoicesForLanguage(systemVoices.value, narrationLanguage.value)
    const found = matching.find(v => v.id === currentSystemVoice.value)
    return found?.name || t('reader.audio_system_voice_default')
  }
  const langKey = isVi.value ? 'vi' : 'en'
  const list = CLOUD_VOICES[langKey]
  const found = list.find(v => v.id === currentVoice.value)
  return found?.id || currentVoice.value
})

const loadedId = ref<string | null>(null)

watch([
  () => props.chunk?.id,
  () => props.chunk?.originalTextMarkdown,
  narrationLanguage,
  () => props.chunk?.isAiFormatted,
], () => {
  const pending = pendingNavigation.value
  const continuation = pending?.targetChunkId === props.chunk?.id && pending?.request.isCurrent() ? pending : null
  invalidateNavigation()
  resetSource()
  loadedId.value = null
  if (available.value && source.value) {
    const autoplay = continuation?.autoplay ?? false
    if (autoplay) loadedId.value = source.value.chunkId
    void loadAndPlay(source.value, { autoplay, reason: continuation?.reason ?? 'play' })
  }
}, { flush: 'sync', immediate: true })

function startPlayback(): void {
  invalidateNavigation()
  if (source.value && available.value) {
    loadedId.value = source.value.chunkId
    void loadAndPlay(source.value, { reason: 'play' })
  }
}

function pausePlayback(): void {
  invalidateNavigation()
  pause()
}

defineExpose({
  startPlayback,
  onToggle,
  play,
  pause: pausePlayback,
})

const formattedErrorMessage = computed(() => {
  if (errorInfo?.value?.code === 'DEVICE_OOM') {
    return t('reader.audio_error_oom')
  }
  if (errorInfo?.value?.code === 'DEVICE_INIT_FAILED') {
    return t('reader.audio_error_device')
  }
  if (errorInfo?.value?.code === 'SYSTEM_TTS_FAILED') {
    return t('reader.audio_error_system')
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
    (engineMode.value === 'device' || engineMode.value === 'system') &&
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
const isPreparingPlayback = computed(() => isLoading.value || pendingNavigation.value !== null)

const isStreamingIncomplete = computed(() => {
  return engineMode.value === 'device' && synthTotal.value > 0 && synthIndex.value > 0 && synthIndex.value < synthTotal.value
})

const estimatedTotalDuration = computed(() => {
  if (!isStreamingIncomplete.value || duration.value <= 0 || synthIndex.value <= 0) return duration.value
  return Math.round((duration.value / synthIndex.value) * synthTotal.value)
})

function onToggle(): void {
  if (!source.value || !available.value) return
  if (playing.value || isPreparingPlayback.value) {
    pausePlayback()
    return
  }
  invalidateNavigation()
  if (loadedId.value === source.value.chunkId && status.value === 'ready') {
    void play()
    return
  }
  loadedId.value = source.value.chunkId
  void loadAndPlay(source.value, { reason: status.value === 'error' ? 'recovery' : 'play' })
}

function onToggleEngine(mode: AudioEnginePreference, recovery = false): void {
  if (mode === 'cloud' && (isNearQuota.value || isQuotaExhausted.value)) return
  if (mode === enginePreference.value && !recovery) return
  const shouldContinue = playing.value || isLoading.value || recovery
  const unchangedEngine = mode === engineMode.value && !recovery
  invalidateNavigation()
  setEnginePreference(mode)
  if (unchangedEngine || (mode === 'auto' && status.value === 'ready')) return
  loadedId.value = null
  if (shouldContinue && source.value) {
    loadedId.value = source.value.chunkId
    void loadAndPlay(source.value, { reason: recovery ? 'recovery' : 'engine-change' })
  }
}

function cycleSpeed(): void {
  const idx = SPEED_STEPS.indexOf(speed.value as (typeof SPEED_STEPS)[number])
  const next = SPEED_STEPS[(idx + 1) % SPEED_STEPS.length] ?? 1
  setSpeed(next)
}

function onSeek(event: Event): void {
  const value = Number((event.target as HTMLInputElement).value)
  if (!Number.isFinite(value)) return
  invalidateNavigation()
  seek(value)
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

const effectiveEngineLabel = computed(() => t('reader.audio_effective_engine', {
  engine: t(`reader.audio_engine_${engineMode.value}`),
}))
</script>

<template>
  <div
    v-if="available"
    class="audio-player relative flex flex-col gap-2.5 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/80 dark:bg-canvas-subtle/70 p-2.5 sm:p-3"
  >
    <!-- Row 1: Primary playback controls, engine mode, right utilities -->
    <div class="flex items-center justify-between gap-1.5 sm:gap-2 w-full min-w-0">
      <!-- Left cluster: Play/Pause button + Engine Mode switch -->
      <div class="flex items-center gap-1.5 sm:gap-2 min-w-0 shrink-0">
        <!-- Play / Pause -->
        <button
          type="button"
          class="h-8 inline-flex items-center gap-1.5 shrink-0 whitespace-nowrap rounded-xl bg-brand-600 hover:bg-brand-500 text-white text-xs sm:text-sm font-semibold px-2.5 sm:px-3 transition-all active:scale-95 disabled:opacity-60"
          :aria-label="(playing || isPreparingPlayback) ? t('reader.audio_pause_desc') : t('reader.audio_play')"
          :title="(playing || isPreparingPlayback) ? t('reader.audio_pause_desc') : t('reader.audio_play')"
          @click="onToggle"
        >
          <Loader2 v-if="isPreparingPlayback" class="w-4 h-4 animate-spin" :stroke-width="2" />
          <Pause v-else-if="playing" class="w-4 h-4" :stroke-width="2" />
          <Volume2 v-else class="w-4 h-4" :stroke-width="2" />
          <span class="audio-toolbar-label">{{ (playing || isPreparingPlayback) ? t('reader.audio_pause') : t('reader.audio_listen') }}</span>
        </button>

        <!-- Persisted preference is independent of the engine currently speaking. -->
        <div
          class="h-8 inline-flex items-center rounded-xl p-0.5 bg-slate-200/60 dark:bg-white/[0.06] border border-slate-200/80 dark:border-white/[0.08] shrink-0 whitespace-nowrap text-xs font-medium"
          role="group"
          :aria-label="t('reader.audio_engine_preference')"
        >
          <button
            v-for="option in ENGINE_OPTIONS"
            :key="option.value"
            type="button"
            class="inline-flex items-center gap-1 px-1.5 sm:px-2.5 py-1 rounded-lg transition-all shrink-0 whitespace-nowrap disabled:opacity-40 disabled:cursor-not-allowed"
            :class="enginePreference === option.value
              ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm font-semibold'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'"
            :aria-label="t(`reader.audio_engine_${option.value}`)"
            :aria-pressed="enginePreference === option.value"
            :disabled="option.value === 'cloud' && (isNearQuota || isQuotaExhausted)"
            :title="option.value === 'cloud' && (isNearQuota || isQuotaExhausted)
              ? t('reader.audio_quota_near_limit_tooltip')
              : t(`reader.audio_engine_${option.value}_hint`)"
            @click="onToggleEngine(option.value)"
          >
            <component :is="option.icon" class="w-3.5 h-3.5" :stroke-width="2" aria-hidden="true" />
            <span class="audio-toolbar-label hidden sm:inline">{{ t(`reader.audio_engine_${option.value}`) }}</span>
          </button>
        </div>

        <!-- Active compute device (GPU/CPU) when in Device mode -->
        <span
          v-if="engineMode === 'device' && device"
          class="shrink-0 whitespace-nowrap hidden sm:inline-flex items-center gap-1 rounded-lg border border-slate-200/80 dark:border-white/[0.08] text-slate-500 dark:text-slate-400 text-xs font-medium px-2 py-1"
          :title="deviceHint"
        >
          <component :is="device === 'webgpu' ? Laptop : Cpu" class="w-3.5 h-3.5" :stroke-width="2" />
          <span>{{ deviceLabel }}</span>
        </span>
      </div>

      <!-- Right cluster: Auto-advance, Sleep timer, Speed -->
      <div class="flex items-center gap-1 sm:gap-1.5 shrink-0">
        <!-- Auto Next Toggle Button (rendered only when !disableAutoAdvance) -->
        <button
          v-if="!disableAutoAdvance"
          type="button"
          class="h-7 inline-flex items-center gap-1.5 px-2 py-1 rounded-lg text-xs font-semibold transition-all shrink-0 whitespace-nowrap"
          :class="[
            autoAdvance
              ? 'bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/30 font-medium'
              : 'text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 border border-slate-200/80 dark:border-white/[0.08]'
          ]"
          :title="t('reader.audio_auto_advance_hint')"
          :aria-label="t('reader.audio_auto_advance')"
          :aria-pressed="autoAdvance"
          @click="autoAdvance = !autoAdvance"
        >
          <FastForward class="w-3.5 h-3.5" :stroke-width="2" />
          <span class="audio-toolbar-label hidden md:inline">{{ t('reader.audio_auto_advance') }}</span>
        </button>

        <!-- Sleep Timer Menu Container -->
        <div ref="sleepTimerMenuRef" class="relative shrink-0 whitespace-nowrap">
          <button
            type="button"
            class="h-7 inline-flex items-center gap-1.5 px-2 py-1 rounded-lg text-xs font-semibold transition-all shrink-0 whitespace-nowrap"
            :class="[
              sleepTimer !== null
                ? 'bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/30 font-medium'
                : 'text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 border border-slate-200/80 dark:border-white/[0.08]'
            ]"
            :title="t('reader.audio_sleep_timer')"
            :aria-label="sleepTimerLabel ? `${t('reader.audio_sleep_timer')}: ${sleepTimerLabel}` : t('reader.audio_sleep_timer')"
            :aria-expanded="isSleepTimerOpen"
            @click="isSleepTimerOpen = !isSleepTimerOpen"
          >
            <Moon class="w-3.5 h-3.5" :stroke-width="2" />
            <span v-if="sleepTimerLabel" class="tabular-nums font-medium text-[11px]" :class="{ 'audio-toolbar-label': sleepTimer === 'end_of_slice' }">{{ sleepTimerLabel }}</span>
            <span v-else class="audio-toolbar-label hidden md:inline">{{ t('reader.audio_sleep_timer') }}</span>
          </button>

          <!-- Dropdown Menu -->
          <div
            v-if="isSleepTimerOpen"
            class="absolute top-full mt-1.5 right-0 z-40 w-44 rounded-xl border border-slate-200 dark:border-white/[0.12] bg-white dark:bg-canvas-elevated shadow-lg p-1 text-xs space-y-0.5"
          >
            <div class="px-2.5 py-1.5 text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
              {{ t('reader.audio_sleep_timer') }}
            </div>
            <button
              type="button"
              class="w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg text-left transition-colors"
              :class="sleepTimer === null ? 'bg-brand-50 dark:bg-brand-950/40 text-brand-600 dark:text-brand-400 font-semibold' : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06]'"
              @click="setSleepTimer(null)"
            >
              <span>{{ t('reader.audio_sleep_timer_off') }}</span>
              <Check v-if="sleepTimer === null" class="w-3.5 h-3.5 text-brand-600 dark:text-brand-400" />
            </button>
            <button
              type="button"
              class="w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg text-left transition-colors"
              :class="sleepTimer === 15 ? 'bg-brand-50 dark:bg-brand-950/40 text-brand-600 dark:text-brand-400 font-semibold' : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06]'"
              @click="setSleepTimer(15)"
            >
              <span>{{ t('reader.audio_sleep_timer_15m') }}</span>
              <Check v-if="sleepTimer === 15" class="w-3.5 h-3.5 text-brand-600 dark:text-brand-400" />
            </button>
            <button
              type="button"
              class="w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg text-left transition-colors"
              :class="sleepTimer === 30 ? 'bg-brand-50 dark:bg-brand-950/40 text-brand-600 dark:text-brand-400 font-semibold' : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06]'"
              @click="setSleepTimer(30)"
            >
              <span>{{ t('reader.audio_sleep_timer_30m') }}</span>
              <Check v-if="sleepTimer === 30" class="w-3.5 h-3.5 text-brand-600 dark:text-brand-400" />
            </button>
            <button
              type="button"
              class="w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg text-left transition-colors"
              :class="sleepTimer === 45 ? 'bg-brand-50 dark:bg-brand-950/40 text-brand-600 dark:text-brand-400 font-semibold' : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06]'"
              @click="setSleepTimer(45)"
            >
              <span>{{ t('reader.audio_sleep_timer_45m') }}</span>
              <Check v-if="sleepTimer === 45" class="w-3.5 h-3.5 text-brand-600 dark:text-brand-400" />
            </button>
            <button
              type="button"
              class="w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg text-left transition-colors"
              :class="sleepTimer === 60 ? 'bg-brand-50 dark:bg-brand-950/40 text-brand-600 dark:text-brand-400 font-semibold' : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06]'"
              @click="setSleepTimer(60)"
            >
              <span>{{ t('reader.audio_sleep_timer_60m') }}</span>
              <Check v-if="sleepTimer === 60" class="w-3.5 h-3.5 text-brand-600 dark:text-brand-400" />
            </button>
            <button
              type="button"
              class="w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg text-left transition-colors border-t border-slate-200/60 dark:border-white/[0.06] mt-1 pt-1"
              :class="sleepTimer === 'end_of_slice' ? 'bg-brand-50 dark:bg-brand-950/40 text-brand-600 dark:text-brand-400 font-semibold' : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-white/[0.06]'"
              @click="setSleepTimer('end_of_slice')"
            >
              <span>{{ t('reader.audio_sleep_timer_end_of_slice') }}</span>
              <Check v-if="sleepTimer === 'end_of_slice'" class="w-3.5 h-3.5 text-brand-600 dark:text-brand-400" />
            </button>
          </div>
        </div>

        <!-- Speed -->
        <button
          type="button"
          class="shrink-0 whitespace-nowrap rounded-lg border border-slate-200/80 dark:border-white/[0.08] hover:bg-slate-200/60 dark:hover:bg-white/[0.06] text-slate-600 dark:text-slate-300 text-xs font-semibold px-1.5 sm:px-2 py-1 transition-colors tabular-nums"
          :title="t('reader.audio_speed')"
          @click="cycleSpeed"
        >
          {{ speed }}x
        </button>
      </div>
    </div>

    <!-- Row 2: Voice Picker, Status/Scrubber/Banner -->
    <div class="flex items-center gap-2 sm:gap-3 w-full min-w-0 pt-1.5 border-t border-slate-200/60 dark:border-white/[0.06]">
      <!-- Voice Selection Dropdown -->
      <div
        v-if="engineMode === 'system'"
        class="w-36 sm:w-44 shrink-0"
      >
        <AppSelect
          v-model="currentSystemVoice"
          :options="availableSystemVoiceOptions"
          size="sm"
          :placeholder="t('reader.audio_voice_select_placeholder')"
          :aria-label="t('reader.audio_voice_select_placeholder')"
        />
      </div>
      <div
        v-else-if="engineMode === 'cloud'"
        class="w-36 sm:w-44 shrink-0"
      >
        <AppSelect
          v-model="currentVoice"
          :options="availableVoiceOptions"
          size="sm"
          :placeholder="t('reader.audio_voice_select_placeholder')"
          :aria-label="t('reader.audio_voice_select_placeholder')"
        />
      </div>
      <div
        v-else
        class="w-36 sm:w-44 shrink-0 text-sm sm:text-base text-slate-500 dark:text-slate-400 font-medium truncate px-1"
      >
        {{ t('reader.audio_engine_device') }}
      </div>

      <!-- Right portion: Error / Loading / Scrubber / Web Speech Banner / Ready Status -->
      <div class="flex flex-col gap-1 flex-1 min-w-0">
        <div class="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs text-slate-500 dark:text-slate-400" role="status" aria-live="polite">
          <span class="whitespace-nowrap shrink-0">{{ effectiveEngineLabel }}</span>
          <span v-if="restoredProgress" class="whitespace-nowrap shrink-0" :title="t('reader.audio_resumed_hint')">{{ t('reader.audio_resumed') }}</span>
          <span v-if="!persistenceAvailable" class="whitespace-nowrap shrink-0" :title="t('reader.audio_persistence_unavailable')">{{ t('reader.audio_session_only') }}</span>
        </div>
        <div class="flex items-center gap-2 w-full min-w-0">
        <!-- Error status & Cloud fallback button -->
        <template v-if="status === 'error'">
          <span
            class="text-sm sm:text-base text-rose-500 dark:text-rose-400 flex-1 min-w-0"
            :title="formattedErrorMessage"
            role="alert"
          >
            {{ formattedErrorMessage }}
          </span>
          <button
            v-if="canFallbackToCloud"
            type="button"
            class="h-7 inline-flex items-center gap-1.5 shrink-0 whitespace-nowrap rounded-lg bg-amber-500/10 hover:bg-amber-500/20 text-amber-600 dark:text-amber-400 border border-amber-500/30 text-xs font-semibold px-2.5 transition-all active:scale-95"
            @click="onFallbackToCloud"
            :aria-label="t('reader.audio_fallback_to_cloud')"
            :title="t('reader.audio_fallback_to_cloud')"
          >
            <Cloud class="w-3.5 h-3.5" :stroke-width="2" />
            <span class="hidden lg:inline">{{ t('reader.audio_fallback_to_cloud') }}</span>
          </button>
        </template>

        <!-- Loading status -->
        <span
          v-else-if="isLoading"
          class="text-sm sm:text-base text-slate-500 dark:text-slate-400 flex-1 min-w-0"
        >
          {{ statusLabel }}
        </span>

        <!-- Seekable Scrubber Slider Track (duration > 0) -->
        <template v-else-if="loadedId === chunk?.id && duration > 0">
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
            {{ formatTime(currentTime) }} / {{ formatTime(duration) }}<span v-if="isStreamingIncomplete && estimatedTotalDuration > duration" class="hidden sm:inline"> (~{{ formatTime(estimatedTotalDuration) }})</span>
          </span>
          <span
            v-if="isStreamingIncomplete"
            class="text-xs text-brand-600 dark:text-brand-400 shrink-0 whitespace-nowrap font-medium hidden sm:inline"
          >
            ({{ t('reader.audio_synthesizing', { current: synthIndex, total: synthTotal }) }})
          </span>
        </template>

        <!-- Active Web Speech banner (duration === 0 && playing) -->
        <div
          v-else-if="duration === 0 && playing"
          class="flex items-center gap-1.5 text-sm sm:text-base text-brand-600 dark:text-brand-400 font-medium flex-1 min-w-0"
        >
          <Volume2 class="w-3.5 h-3.5 shrink-0 animate-pulse" :stroke-width="2" />
          <span>{{ t('reader.audio_system_playing_label', { voice: currentVoiceName }) }}</span>
        </div>

        <!-- Ready / Idle indicator when duration === 0 && !playing -->
        <div
          v-else-if="engineMode === 'system'"
          class="text-sm sm:text-base text-slate-400 dark:text-slate-500 flex-1 min-w-0"
        >
          {{ t('reader.audio_system_ready_label') }}
        </div>

        <div v-else class="flex-1 min-w-0" />
      </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.audio-player {
  container: reader-audio / inline-size;
}

@container reader-audio (max-width: 36rem) {
  .audio-toolbar-label {
    display: none;
  }
}
</style>

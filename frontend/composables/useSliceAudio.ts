// Orchestrates on-device narration for a reader slice: derive the script,
// resolve the language voice, reuse cached audio, otherwise stream synthesis
// from the worker, assemble a complete seekable WAV, cache it, and drive an
// <audio> element (play/pause/seek/speed). All heavy work is client-only.

import { computed, getCurrentScope, onScopeDispose, ref, shallowRef, watch } from 'vue'
import { useStorage } from '@vueuse/core'
import { computeContentHash, extractNarrationScript, splitSentences } from '~/utils/narrationScript'
import { resolveVoiceForLanguage } from '~/utils/ttsVoices'
import { concatFloat32, encodeWav } from '~/utils/audioWav'
import { buildAudioKey, createIdbBackend, createSliceAudioCache } from '~/utils/sliceAudioCache'
import type { SliceAudioCache } from '~/utils/sliceAudioCache'
import type { AudioQuotaInfo } from '~/types/audio'

export type AudioEngine = 'cloud' | 'device' | 'system'

export interface NarrationSource {
  chunkId: string
  markdown: string
  language?: string | null
  isAiFormatted: boolean
}

export type AudioStatus = 'idle' | 'loading' | 'ready' | 'error'

export interface SynthProgress {
  stage: 'download' | 'synth'
  progress?: number
  index?: number
  count?: number
}

// Resolved compute backend the worker ran synthesis on: GPU (WebGPU) or CPU (WASM).
export type ComputeDevice = 'webgpu' | 'wasm'

export interface SynthHandlers {
  onProgress: (progress: SynthProgress) => void
  onChunk: (samples: Float32Array, sampleRate: number) => void
  onDevice?: (device: ComputeDevice) => void
}

export interface TtsEngine {
  synthesize: (model: string, sentences: string[], handlers: SynthHandlers, offsetIndex?: number) => Promise<void>
  cancel?: (reqId?: number) => void
  dispose: () => void
}

export interface SliceAudioDeps {
  engine?: TtsEngine
  cache?: SliceAudioCache
  createAudio?: () => HTMLAudioElement
  fetchClient?: (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>
  defaultEngine?: AudioEngine
  onQuotaExhausted?: () => void
  onSliceEnded?: () => void
  onNextTrack?: () => void
  onPreviousTrack?: () => void
  speechSynthesis?: SpeechSynthesis
  onFallbackToCloud?: () => void
}

export const AUDIO_SPEED_STORAGE_KEY = 'techdaily_reader_audio_speed'
export const AUDIO_ENGINE_STORAGE_KEY = 'techdaily_reader_audio_engine'
export const AUDIO_VOICE_STORAGE_KEY = 'techdaily_reader_audio_voice'
export const AUDIO_PITCH_STORAGE_KEY = 'techdaily_reader_audio_pitch'
export const AUDIO_SYSTEM_VOICE_STORAGE_KEY = 'techdaily_reader_audio_system_voice'
export const AUDIO_AUTO_ADVANCE_STORAGE_KEY = 'techdaily_reader_audio_auto_advance'
export const AUDIO_SLEEP_TIMER_STORAGE_KEY = 'techdaily_reader_audio_sleep_timer'

export interface SystemVoiceOption {
  id: string
  name: string
  lang: string
  default: boolean
  localService?: boolean
}

export interface MediaSessionMetadataPayload {
  title: string
  artist?: string
  album?: string
  artwork?: { src: string; sizes?: string; type?: string }[]
}

export function getAvailableSystemVoices(speechSynth?: SpeechSynthesis | null): SystemVoiceOption[] {
  if (typeof window === 'undefined') return []
  const synth = speechSynth ?? (typeof window.speechSynthesis !== 'undefined' ? window.speechSynthesis : null)
  if (!synth) return []
  return synth.getVoices().map(v => ({
    id: v.voiceURI || v.name,
    name: v.name,
    lang: v.lang,
    default: v.default,
    localService: v.localService,
  }))
}

export function filterSystemVoicesForLanguage(voices: SystemVoiceOption[], lang?: string | null): SystemVoiceOption[] {
  if (!lang) return voices
  const prefix = lang.toLowerCase().split(/[-_]/)[0]
  if (!prefix) return voices
  return voices.filter(v => v.lang.toLowerCase().replace('_', '-').startsWith(prefix))
}

export function resolveCascadedEngine(options: {
  manualOverride?: AudioEngine | null
  sliceLanguage?: string | null
  availableSystemVoices?: SystemVoiceOption[]
  isOnline?: boolean
  isQuotaExhausted?: boolean
  hasDeviceEngineOnly?: boolean
}): AudioEngine {
  if (options.manualOverride) {
    return options.manualOverride
  }
  if (options.hasDeviceEngineOnly) {
    return 'device'
  }
  const matchingVoices = filterSystemVoicesForLanguage(options.availableSystemVoices ?? [], options.sliceLanguage)
  if (matchingVoices.length > 0) {
    return 'system'
  }
  const isOnline = options.isOnline ?? (typeof navigator !== 'undefined' ? navigator.onLine : true)
  if (isOnline && !options.isQuotaExhausted) {
    return 'cloud'
  }
  return 'device'
}

export function playSliceTransitionChime(): Promise<void> {
  if (typeof window === 'undefined') return Promise.resolve()
  const { promise, resolve } = typeof Promise.withResolvers === 'function'
    ? Promise.withResolvers<void>()
    : (() => {
        let res!: () => void
        const p = new Promise<void>((r) => { res = r })
        return { promise: p, resolve: res }
      })()

  try {
    let AudioCtx: typeof AudioContext | null = window.AudioContext ?? null
    if (!AudioCtx && 'webkitAudioContext' in window) {
      AudioCtx = window.webkitAudioContext as typeof AudioContext
    }
    if (!AudioCtx) {
      resolve()
      return promise
    }
    const ctx = new AudioCtx()
    const osc = ctx.createOscillator()
    const gain = ctx.createGain()

    osc.type = 'sine'
    const now = ctx.currentTime
    osc.frequency.setValueAtTime(440, now)
    osc.frequency.exponentialRampToValueAtTime(880, now + 0.25)

    gain.gain.setValueAtTime(0.15, now)
    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.25)

    osc.connect(gain)
    gain.connect(ctx.destination)

    osc.start(now)
    osc.stop(now + 0.25)

    osc.onended = () => {
      void ctx.close()
      resolve()
    }
  } catch {
    resolve()
  }
  return promise
}

export function updateMediaSessionMetadata(payload: MediaSessionMetadataPayload): void {
  if (typeof navigator === 'undefined' || !('mediaSession' in navigator)) return
  try {
    navigator.mediaSession.metadata = new MediaMetadata({
      title: payload.title,
      artist: payload.artist || 'TechDaily',
      album: payload.album || 'TechDaily Reader',
      artwork: payload.artwork || [],
    })
  } catch {
    // Ignored if MediaMetadata fails
  }
}

export interface CloudVoiceOption {
  id: string
  label: string
  gender: 'female' | 'male'
  language: 'vi' | 'en'
}

export const CLOUD_VOICES: Record<'vi' | 'en', CloudVoiceOption[]> = {
  vi: [
    { id: 'vi-VN-Neural2-A', label: 'vi-VN-Neural2-A', gender: 'female', language: 'vi' },
    { id: 'vi-VN-Neural2-D', label: 'vi-VN-Neural2-D', gender: 'male', language: 'vi' },
  ],
  en: [
    { id: 'en-US-Neural2-F', label: 'en-US-Neural2-F', gender: 'female', language: 'en' },
    { id: 'en-US-Neural2-D', label: 'en-US-Neural2-D', gender: 'male', language: 'en' },
  ],
}

export function resolveCloudVoiceForLanguage(lang?: string | null, customVoiceId?: string | null): string {
  const isVi = (lang || '').toLowerCase().startsWith('vi')
  const defaultVoice = isVi ? 'vi-VN-Neural2-A' : 'en-US-Neural2-F'
  if (!customVoiceId) return defaultVoice
  const matches = isVi ? customVoiceId.startsWith('vi-') : customVoiceId.startsWith('en-')
  return matches ? customVoiceId : defaultVoice
}

export interface AudioErrorInfo {
  code: 'QUOTA_EXHAUSTED' | 'DEVICE_OOM' | 'DEVICE_INIT_FAILED' | 'NETWORK_ERROR' | 'UNKNOWN'
  rawMessage: string
  suggestCloudFallback?: boolean
}

export function categorizeAudioError(err: unknown, engine: AudioEngine): AudioErrorInfo {
  const rawMessage = err instanceof Error ? err.message : String(err)
  const lower = rawMessage.toLowerCase()

  if (rawMessage === 'QUOTA_EXHAUSTED') {
    return { code: 'QUOTA_EXHAUSTED', rawMessage, suggestCloudFallback: false }
  }

  if (lower.includes('out of memory') || lower.includes('allocation failed') || lower.includes('oom') || lower.includes('memory')) {
    return { code: 'DEVICE_OOM', rawMessage, suggestCloudFallback: engine === 'device' }
  }

  if (
    lower.includes('network')
    || lower.includes('failed to fetch')
    || lower === 'load failed'
    || lower.includes('typeerror: load failed')
    || lower.includes('failed to load resource')
    || lower.includes('timeout')
    || lower.includes('econnrefused')
    || lower.includes('enotfound')
    || lower.includes('etimedout')
  ) {
    return { code: 'NETWORK_ERROR', rawMessage, suggestCloudFallback: engine === 'device' }
  }

  if (engine === 'device') {
    return { code: 'DEVICE_INIT_FAILED', rawMessage, suggestCloudFallback: true }
  }

  if (engine === 'system') {
    return { code: 'SYSTEM_TTS_FAILED', rawMessage, suggestCloudFallback: true }
  }
  return { code: 'UNKNOWN', rawMessage, suggestCloudFallback: false }
}

interface WorkerMessage {
  type: 'progress' | 'chunk' | 'done' | 'error' | 'device'
  reqId: number
  stage?: 'download' | 'synth'
  progress?: number
  index?: number
  count?: number
  sampleRate?: number
  samples?: Float32Array
  message?: string
  device?: ComputeDevice
}

// Production engine: a module Web Worker running Transformers.js.
function createWorkerEngine(): TtsEngine {
  let worker: Worker | null = null
  let reqCounter = 0
  const pending = new Map<number, { handlers: SynthHandlers, resolve: () => void, reject: (e: Error) => void }>()

  function ensureWorker(): Worker {
    if (!worker) {
      worker = new Worker(new URL('../workers/ttsSynth.worker.ts', import.meta.url), { type: 'module' })
      worker.onerror = (event: ErrorEvent) => {
        const message = event.message || 'Worker initialization failed'
        console.error('[useSliceAudio] Device TTS Error (worker.onerror):', message, event)
        const err = new Error(message)
        for (const p of pending.values()) {
          p.reject(err)
        }
        pending.clear()
        worker = null
      }
      worker.onmessage = (event: MessageEvent<WorkerMessage>) => {
        const m = event.data
        const p = pending.get(m.reqId)
        if (!p) return
        if (m.type === 'progress') {
          p.handlers.onProgress({ stage: m.stage ?? 'download', progress: m.progress, index: m.index, count: m.count })
        }
        else if (m.type === 'chunk' && m.samples && m.sampleRate) {
          p.handlers.onChunk(m.samples, m.sampleRate)
        }
        else if (m.type === 'device' && m.device) {
          p.handlers.onDevice?.(m.device)
        }
        else if (m.type === 'done') {
          pending.delete(m.reqId)
          p.resolve()
        }
        else if (m.type === 'error') {
          console.error('[useSliceAudio] Device TTS Error (worker message):', m.message)
          pending.delete(m.reqId)
          p.reject(new Error(m.message ?? 'Synthesis failed'))
        }
      }
    }
    return worker
  }

  return {
    synthesize(model, sentences, handlers, offsetIndex = 0) {
      return new Promise<void>((resolve, reject) => {
        const w = ensureWorker()
        const reqId = ++reqCounter
        pending.set(reqId, { handlers, resolve, reject })
        w.postMessage({ type: 'synth', reqId, model, sentences, offsetIndex })
      })
    },
    cancel(reqId) {
      if (worker) {
        worker.postMessage({ type: 'cancel', reqId })
      }
      const cancelError = new Error('Synthesis cancelled')
      if (reqId != null) {
        const p = pending.get(reqId)
        if (p) {
          pending.delete(reqId)
          p.reject(cancelError)
        }
      } else {
        for (const p of pending.values()) {
          p.reject(cancelError)
        }
        pending.clear()
      }
    },
    dispose() {
      worker?.terminate()
      worker = null
      // Reject in-flight synths so an awaiting loadAndPlay unwinds instead of
      // leaking a suspended promise when the reader is left mid-synthesis.
      for (const p of pending.values()) p.reject(new Error('Synthesis cancelled'))
      pending.clear()
    },
  }
}

export function useSliceAudio(deps: SliceAudioDeps = {}) {
  const isClient = typeof window !== 'undefined'

  const speed = useStorage(AUDIO_SPEED_STORAGE_KEY, 1)
  const status = ref<AudioStatus>('idle')
  const playing = ref(false)
  const currentTime = ref(0)
  const duration = ref(0)
  const downloadProgress = ref(0)
  const synthIndex = ref(0)
  const synthTotal = ref(0)
  const targetBufferCount = ref(0)
  const errorMessage = ref<string | null>(null)
  const errorInfo = ref<AudioErrorInfo | null>(null)
  const device = ref<ComputeDevice | null>(null)
  const initialEngine: AudioEngine =
    deps.defaultEngine
    ?? (isClient ? (localStorage.getItem(AUDIO_ENGINE_STORAGE_KEY) as AudioEngine) : null)
    ?? (deps.engine && !deps.fetchClient ? 'device' : 'cloud')

  const engineMode = ref<AudioEngine>(initialEngine)
  const activeCascadeTier = ref<AudioEngine>(initialEngine)
  const selectedVoice = ref<string>(
    (isClient ? localStorage.getItem(AUDIO_VOICE_STORAGE_KEY) : null) ?? ''
  )
  const pitch = ref<number>(
    isClient && localStorage.getItem(AUDIO_PITCH_STORAGE_KEY)
      ? Number(localStorage.getItem(AUDIO_PITCH_STORAGE_KEY))
        : 1
  )
  const volume = ref<number>(1)
  const selectedSystemVoice = ref<string>(
    (isClient ? localStorage.getItem(AUDIO_SYSTEM_VOICE_STORAGE_KEY) : null) ?? ''
  )
  const autoAdvance = useStorage(AUDIO_AUTO_ADVANCE_STORAGE_KEY, true)
  const systemVoices = ref<SystemVoiceOption[]>([])

  let activeUtterance: SpeechSynthesisUtterance | null = null
  let isSilentCarrierActive = false
  let systemSentences: string[] = []
  let systemSentenceDurations: number[] = []
  let systemSentenceIndex = 0
  let isSystemStopped = false
  let systemTimeTicker: number | null = null
  let resumeSystemSpeech: (() => void) | null = null
  const SILENT_AUDIO_URI = 'data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEARKwAAIhYAQACABAAZGF0YQAAAAA='

  function computeSystemSentenceDurations(sentences: string[], speechSpeed: number): number[] {
    const CHARS_PER_SECOND = 16
    const s = Math.max(0.5, Math.min(2.0, speechSpeed))
    return sentences.map(text => Math.max(1.0, (text.trim().length / CHARS_PER_SECOND) / s))
  }

  function stopSystemTimeTicker(): void {
    if (systemTimeTicker) {
      clearInterval(systemTimeTicker)
      systemTimeTicker = null
    }
  }

  function startSystemTimeTicker(sentenceIdx: number): void {
    stopSystemTimeTicker()
    if (!isClient) return
    const startSentenceTime = performance.now()
    const baseElapsed = systemSentenceDurations.slice(0, sentenceIdx).reduce((a, b) => a + b, 0)
    const currentSentenceDuration = systemSentenceDurations[sentenceIdx] || 1
    systemTimeTicker = window.setInterval(() => {
      if (!playing.value || isSystemStopped) {
        stopSystemTimeTicker()
        return
      }
      const elapsedInSentence = (performance.now() - startSentenceTime) / 1000
      const clampedInSentence = Math.min(currentSentenceDuration, elapsedInSentence)
      currentTime.value = Math.min(duration.value, baseElapsed + clampedInSentence)
    }, 200)
  }

  function getSpeechSynth(): SpeechSynthesis | null {
    if (deps.speechSynthesis) return deps.speechSynthesis
    if (isClient && 'speechSynthesis' in window) {
      return window.speechSynthesis
    }
    return null
  }

  function loadSystemVoices(): void {
    const synth = getSpeechSynth()
    if (!synth) return
    const raw = synth.getVoices()
    if (raw && raw.length > 0) {
      systemVoices.value = raw.map(v => ({
        id: v.voiceURI || v.name,
        name: v.name,
        lang: v.lang,
        default: v.default,
        localService: v.localService,
      }))
    }
  }

  if (isClient) {
    loadSystemVoices()
    const synth = getSpeechSynth()
    if (synth && 'onvoiceschanged' in synth) {
      synth.onvoiceschanged = () => loadSystemVoices()
    }
    if ('mediaSession' in navigator) {
      try {
        navigator.mediaSession.setActionHandler('play', () => { void play() })
        navigator.mediaSession.setActionHandler('pause', () => { pause() })
        if (deps.onNextTrack) {
          navigator.mediaSession.setActionHandler('nexttrack', () => { deps.onNextTrack?.() })
        }
        if (deps.onPreviousTrack) {
          navigator.mediaSession.setActionHandler('previoustrack', () => { deps.onPreviousTrack?.() })
        }
      } catch {
        // Non-fatal
      }
    }
  }

  function startSilentCarrier(): void {
    if (!isClient) return
    const el = ensureAudio()
    if (!el || isSilentCarrierActive) return
    try {
      if (el.src !== SILENT_AUDIO_URI) {
        el.src = SILENT_AUDIO_URI
        el.loop = true
      }
      el.volume = 0
      void el.play()
      isSilentCarrierActive = true
    } catch {
      // Non-fatal
    }
  }

  function stopSilentCarrier(): void {
    if (!isSilentCarrierActive) return
    isSilentCarrierActive = false
    const el = audio.value
    if (el) {
      el.pause()
      el.loop = false
      if (el.src === SILENT_AUDIO_URI) {
        try {
          el.removeAttribute('src')
          el.load()
        } catch {
          // Non-fatal
        }
      }
      el.volume = volume.value
    }
  }


  const audioQuota = ref<AudioQuotaInfo | null>(null)
  const isNearQuota = computed(() => (audioQuota.value?.usedCharacters ?? 0) >= 900_000 || !!audioQuota.value?.isNearLimit)
  const isQuotaExhausted = computed(() => (audioQuota.value?.usedCharacters ?? 0) >= 950_000 || !!audioQuota.value?.isExhausted)

  const audio = shallowRef<HTMLAudioElement | null>(null)
  let objectUrl: string | null = null
  let activeKey: string | null = null
  let onChunkEndedCallback: (() => void) | null = null
  let isUserPaused = false
  let cancelWaitingForChunk: (() => void) | null = null
  // Engine and cache are created lazily on first playback so merely rendering
  // the reader never opens IndexedDB or instantiates the worker.
  let engineInstance: TtsEngine | null = null
  let cacheInstance: SliceAudioCache | null = null

  function getEngine(): TtsEngine | null {
    if (deps.engine) return deps.engine
    if (!isClient) return null
    engineInstance ??= createWorkerEngine()
    return engineInstance
  }
  function getFetchClient(): (input: string | URL | Request, init?: RequestInit) => Promise<Response> {
    if (deps.fetchClient) return deps.fetchClient
    return async (input, init) => {
      let url = typeof input === 'string' ? input : input.toString()
      let baseUrl = ''
      try {
        if (process.env.API_INTERNAL_URL) {
          baseUrl = process.env.API_INTERNAL_URL
        } else {
          const config = useRuntimeConfig()
          baseUrl = (config?.public?.apiBaseUrl as string | undefined)?.trim() || ''
        }
      } catch {
        baseUrl = ''
      }
      if (url.startsWith('/')) {
        url = `${baseUrl}${url}`
      }

      const headers = new Headers(init?.headers || {})
      try {
        const authStore = useAuthStore()
        const token = authStore?.token
        if (token && !headers.has('Authorization')) {
          headers.set('Authorization', `Bearer ${token}`)
        }
      } catch {
        // Outside store or unauthenticated
      }

      return globalThis.fetch(url, {
        ...init,
        headers,
      })
    }
  }

  function getCache(): SliceAudioCache | null {
    if (deps.cache) return deps.cache
    if (!isClient) return null
    cacheInstance ??= createSliceAudioCache(createIdbBackend())
    return cacheInstance
  }

  function ensureAudio(): HTMLAudioElement | null {
    if (!audio.value) {
      if (deps.createAudio) audio.value = deps.createAudio()
      else if (isClient) audio.value = new Audio()
      else return null
      const el = audio.value
      el.addEventListener('timeupdate', () => {
        if (!isSilentCarrierActive && engineMode.value !== 'system') {
          currentTime.value = el.currentTime
        }
      })
      el.addEventListener('durationchange', () => {
        if (!isSilentCarrierActive && engineMode.value !== 'system') {
          duration.value = Number.isFinite(el.duration) ? el.duration : 0
        }
      })
      el.addEventListener('play', () => { playing.value = true })
      el.addEventListener('pause', () => { playing.value = false })
      el.addEventListener('ended', () => {
        if (isSilentCarrierActive || isUserPaused || engineMode.value === 'system') {
          return
        }
        if (onChunkEndedCallback) {
          onChunkEndedCallback()
        } else {
          playing.value = false
          deps.onSliceEnded?.()
        }
      })
      el.playbackRate = speed.value
      el.volume = volume.value
    }
    return audio.value
  }

  function setSource(blob: Blob, initialOffset = 0): void {
    const el = ensureAudio()
    if (!el) return
    if (objectUrl) URL.revokeObjectURL(objectUrl)
    objectUrl = URL.createObjectURL(blob)
    el.src = objectUrl
    el.playbackRate = speed.value

    if (initialOffset > 0) {
      const applyOffset = () => {
        try {
          el.currentTime = initialOffset
        } catch {
          // Ignored if media element prevents seeking
        }
      }
      if (el.readyState >= 1) {
        applyOffset()
      } else {
        el.addEventListener('loadedmetadata', applyOffset, { once: true })
      }
    }
  }

  async function play(): Promise<void> {
    isUserPaused = false
    if (engineMode.value === 'system') {
      isSystemStopped = false
      const synth = getSpeechSynth()
      if (synth) {
        if (resumeSystemSpeech) {
          startSilentCarrier()
          resumeSystemSpeech()
          playing.value = true
          return
        }
        if (activeUtterance) {
          startSilentCarrier()
          synth.speak(activeUtterance)
          playing.value = true
          return
        }
      }
    }
    const el = ensureAudio()
    if (!el) return
    try {
      if (el.ended || (el.duration > 0 && el.currentTime >= el.duration)) {
        el.currentTime = 0
      }
      await el.play()
    }
    catch {
      // Autoplay rejections are non-fatal; the user can press play again.
    }
  }

  function pause(): void {
    isUserPaused = true
    cancelWaitingForChunk?.()
    if (engineMode.value === 'system') {
      stopSystemTimeTicker()
      isSystemStopped = true
      const synth = getSpeechSynth()
      if (synth) {
        synth.cancel()
      }
      stopSilentCarrier()
    }
    audio.value?.pause()
    playing.value = false
    if (status.value === 'loading') {
      status.value = 'ready'
    }
  }

  function cancelWorkerSynthesis(): void {
    if (engineInstance?.cancel) {
      engineInstance.cancel()
    }
    if (deps.engine?.cancel) {
      deps.engine.cancel()
    }
    synthIndex.value = 0
    synthTotal.value = 0
    targetBufferCount.value = 0
  }

  function setEngineMode(mode: AudioEngine): void {
    engineMode.value = mode
    activeCascadeTier.value = mode
    errorMessage.value = null
    errorInfo.value = null
    if (mode === 'cloud' || mode === 'system') {
      cancelWorkerSynthesis()
    }
    if (mode !== 'system') {
      stopSystemTimeTicker()
      isSystemStopped = true
      const synth = getSpeechSynth()
      if (synth) {
        synth.cancel()
        stopSilentCarrier()
      }
    }
    if (isClient) {
      localStorage.setItem(AUDIO_ENGINE_STORAGE_KEY, mode)
    }
  }

  function setVoice(voiceId: string): void {
    selectedVoice.value = voiceId
    if (isClient) {
      const isVi = voiceId.startsWith('vi-')
      localStorage.setItem(`${AUDIO_VOICE_STORAGE_KEY}_${isVi ? 'vi' : 'en'}`, voiceId)
      localStorage.setItem(AUDIO_VOICE_STORAGE_KEY, voiceId)
    }
  }

  function setSystemVoice(voiceId: string): void {
    selectedSystemVoice.value = voiceId
    if (isClient) {
      localStorage.setItem(AUDIO_SYSTEM_VOICE_STORAGE_KEY, voiceId)
    }
  }

  function setPitch(value: number): void {
    pitch.value = value
    if (isClient) {
      localStorage.setItem(AUDIO_PITCH_STORAGE_KEY, String(value))
    }
    if (engineMode.value === 'system' && activeUtterance) {
      activeUtterance.pitch = value
    }
  }

  function setAutoAdvance(value: boolean): void {
    autoAdvance.value = value
  }
  async function fetchQuota(): Promise<AudioQuotaInfo | null> {
    try {
      const client = getFetchClient()
      const res = await client('/api/v1/library/audio/quota')
      if (res.ok) {
        const data = await res.json() as AudioQuotaInfo
        audioQuota.value = data
        if (data.isNearLimit || data.isExhausted) {
          if (engineMode.value === 'cloud') {
            setEngineMode('device')
          }
        }
        return data
      }
    } catch {
      // Non-fatal
    }
    return null
  }

  async function synthesizeOnSystem(
    source: NarrationSource,
    script: string,
    contentHash: string,
    cache: SliceAudioCache,
    autoPlay = true,
    initialOffset = 0,
  ): Promise<void> {
    const synth = getSpeechSynth()
    if (!synth) {
      engineMode.value = 'cloud'
      activeCascadeTier.value = 'cloud'
      if (isClient) localStorage.setItem(AUDIO_ENGINE_STORAGE_KEY, 'cloud')
      deps.onFallbackToCloud?.()
      await synthesizeOnCloud(source, script, contentHash, cache, autoPlay, initialOffset)
      return
    }

    synth.cancel()
    loadSystemVoices()
    const matching = filterSystemVoicesForLanguage(systemVoices.value, source.language)
    if (matching.length === 0) {
      engineMode.value = 'cloud'
      activeCascadeTier.value = 'cloud'
      if (isClient) localStorage.setItem(AUDIO_ENGINE_STORAGE_KEY, 'cloud')
      deps.onFallbackToCloud?.()
      await synthesizeOnCloud(source, script, contentHash, cache, autoPlay, initialOffset)
      return
    }

    const matched = matching.find(v => v.id === selectedSystemVoice.value) || matching[0]
    selectedSystemVoice.value = matched.id
    activeCascadeTier.value = 'system'

    const UtteranceCtor = typeof SpeechSynthesisUtterance !== 'undefined'
      ? SpeechSynthesisUtterance
      : (typeof globalThis !== 'undefined' && 'SpeechSynthesisUtterance' in globalThis
          ? (globalThis.SpeechSynthesisUtterance as typeof SpeechSynthesisUtterance)
          : null)
    if (!UtteranceCtor) {
      engineMode.value = 'cloud'
      activeCascadeTier.value = 'cloud'
      if (isClient) localStorage.setItem(AUDIO_ENGINE_STORAGE_KEY, 'cloud')
      deps.onFallbackToCloud?.()
      await synthesizeOnCloud(source, script, contentHash, cache, autoPlay, initialOffset)
      return
    }

    const split = splitSentences(script)
    systemSentences = split.length > 0 ? split : [script.trim()].filter(Boolean)
    if (systemSentences.length === 0) {
      status.value = 'ready'
      currentTime.value = 0
      duration.value = 0
      playing.value = false
      return
    }

    systemSentenceDurations = computeSystemSentenceDurations(systemSentences, speed.value)
    duration.value = systemSentenceDurations.reduce((a, b) => a + b, 0)

    let startSentenceIndex = 0
    if (initialOffset > 0 && duration.value > 0) {
      let acc = 0
      for (let i = 0; i < systemSentenceDurations.length; i++) {
        if (acc + systemSentenceDurations[i] > initialOffset || i === systemSentenceDurations.length - 1) {
          startSentenceIndex = i
          break
        }
        acc += systemSentenceDurations[i]
      }
    }

    systemSentenceIndex = startSentenceIndex
    isSystemStopped = false
    activeKey = `system:${source.chunkId}:${matched.id}:${contentHash}`
    status.value = 'ready'
    currentTime.value = initialOffset

    function speakSentence(idx: number): void {
      if (isSystemStopped || idx >= systemSentences.length) {
        if (idx >= systemSentences.length) {
          playing.value = false
          stopSystemTimeTicker()
          stopSilentCarrier()
          deps.onSliceEnded?.()
        }
        return
      }

      const text = systemSentences[idx]
      const utterance = new UtteranceCtor(text)
      const allRaw = synth.getVoices()
      const actualVoice = allRaw.find(v => (v.voiceURI || v.name) === matched.id)
      if (actualVoice) {
        utterance.voice = actualVoice
      }
      utterance.lang = matched.lang
      utterance.rate = Math.max(0.5, Math.min(2.0, speed.value))
      utterance.pitch = Math.max(0.5, Math.min(1.5, pitch.value))
      utterance.volume = Math.max(0, Math.min(1.0, volume.value))
      activeUtterance = utterance

      utterance.onstart = () => {
        playing.value = true
        status.value = 'ready'
        startSystemTimeTicker(idx)
      }
      utterance.onpause = () => {
        playing.value = false
        stopSystemTimeTicker()
      }
      utterance.onresume = () => {
        playing.value = true
        startSystemTimeTicker(idx)
      }
      utterance.onend = () => {
        stopSystemTimeTicker()
        if (isSystemStopped || isUserPaused) return
        systemSentenceIndex = idx + 1
        if (systemSentenceIndex < systemSentences.length) {
          speakSentence(systemSentenceIndex)
        } else {
          playing.value = false
          currentTime.value = duration.value
          stopSilentCarrier()
          deps.onSliceEnded?.()
        }
      }
      utterance.onerror = async (e) => {
        stopSystemTimeTicker()
        if (e.error === 'canceled' || e.error === 'interrupted') return
        playing.value = false
        stopSilentCarrier()
        isSystemStopped = true

        const isOnline = typeof navigator !== 'undefined' ? navigator.onLine : true
        if (isOnline && !isQuotaExhausted.value) {
          engineMode.value = 'cloud'
          activeCascadeTier.value = 'cloud'
          if (isClient) localStorage.setItem(AUDIO_ENGINE_STORAGE_KEY, 'cloud')
          deps.onFallbackToCloud?.()
          await synthesizeOnCloud(source, script, contentHash, cache, autoPlay, currentTime.value)
          return
        }

        status.value = 'error'
        const info = categorizeAudioError(new Error(`System TTS Error: ${e.error}`), 'system')
        errorMessage.value = info.rawMessage
        errorInfo.value = info
      }

      startSilentCarrier()
      synth.speak(utterance)
      playing.value = true
    }

    resumeSystemSpeech = () => {
      speakSentence(systemSentenceIndex)
    }

    if (autoPlay) {
      speakSentence(startSentenceIndex)
    }
  }

  async function synthesizeOnCloud(
    source: NarrationSource,
    script: string,
    contentHash: string,
    cache: SliceAudioCache,
    autoPlay = true,
    initialOffset = 0,
  ): Promise<void> {
    activeCascadeTier.value = 'cloud'
    const langKey = (source.language || '').toLowerCase().startsWith('vi') ? 'vi' : 'en'
    const scopedVoice = isClient ? localStorage.getItem(`${AUDIO_VOICE_STORAGE_KEY}_${langKey}`) : null
    const requestedVoice = selectedVoice.value || scopedVoice
    const voiceId = resolveCloudVoiceForLanguage(source.language, requestedVoice)
    selectedVoice.value = voiceId
    const key = buildAudioKey(source.chunkId, voiceId, contentHash)
    activeKey = key

    // Cache hit in browser IndexedDB
    const cached = await cache.get(key)
    if (cached) {
      setSource(cached, initialOffset)
      status.value = 'ready'
      if (autoPlay) {
        await play()
      }
      return
    }

    // Cache miss -> Fetch from backend proxy
    status.value = 'loading'
    downloadProgress.value = 0

    try {
      const client = getFetchClient()
      const response = await client(`/api/v1/library/chunks/${source.chunkId}/audio`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          voiceId,
          contentHash,
          narrationScript: script,
        }),
      })

      if (response.status === 429) {
        deps.onQuotaExhausted?.()
        setEngineMode('device')
        if (audioQuota.value) {
          audioQuota.value.isExhausted = true
        }
        const info = categorizeAudioError(new Error('QUOTA_EXHAUSTED'), 'cloud')
        errorMessage.value = 'QUOTA_EXHAUSTED'
        errorInfo.value = info
        // Automatic fallback to On-Device Web Worker
        await synthesizeOnDevice(source, script, contentHash, cache, autoPlay, initialOffset)
        return
      }

      if (!response.ok) {
        status.value = 'error'
        const info = categorizeAudioError(new Error(`Synthesis failed (${response.status})`), 'cloud')
        errorMessage.value = info.rawMessage
        errorInfo.value = info
        return
      }
      const blob = await response.blob()
      if (activeKey !== key) return

      await cache.set(key, blob)
      setSource(blob, initialOffset)
      status.value = 'ready'
      if (autoPlay) {
        await play()
      }
    } catch (err) {
      status.value = 'error'
      const info = categorizeAudioError(err, 'cloud')
      errorMessage.value = info.rawMessage
      errorInfo.value = info
    }
  }

  async function loadAndPlay(source: NarrationSource, autoPlay = true, initialOffset = 0): Promise<void> {
    errorMessage.value = null
    errorInfo.value = null
    device.value = null
    isUserPaused = !autoPlay
    const cache = getCache()
    if (!source.isAiFormatted || !source.markdown || !cache) return
    const script = extractNarrationScript(source.markdown)
    if (!script) return

    const contentHash = await computeContentHash(script)
    cancelWorkerSynthesis()

    const manualStored = isClient ? (localStorage.getItem(AUDIO_ENGINE_STORAGE_KEY) as AudioEngine | null) : null
    const effectiveEngine = resolveCascadedEngine({
      manualOverride: deps.defaultEngine || manualStored,
      sliceLanguage: source.language,
      availableSystemVoices: systemVoices.value,
      isQuotaExhausted: isQuotaExhausted.value,
      hasDeviceEngineOnly: !!(deps.engine && !deps.fetchClient),
    })
    activeCascadeTier.value = effectiveEngine
    engineMode.value = effectiveEngine

    if (effectiveEngine === 'system') {
      await synthesizeOnSystem(source, script, contentHash, cache, autoPlay, initialOffset)
    } else if (effectiveEngine === 'cloud') {
      await synthesizeOnCloud(source, script, contentHash, cache, autoPlay, initialOffset)
    } else {
      await synthesizeOnDevice(source, script, contentHash, cache, autoPlay, initialOffset)
    }
  }

  async function synthesizeOnDevice(
    source: NarrationSource,
    script: string,
    contentHash: string,
    cache: SliceAudioCache,
    autoPlay = true,
    initialOffset = 0,
  ): Promise<void> {
    const engine = getEngine()
    if (!engine) return

    const voice = resolveVoiceForLanguage(source.language)
    const key = buildAudioKey(source.chunkId, voice.id, contentHash)
    activeKey = key

    // Cache hit
    const cached = await cache.get(key)
    if (cached) {
      setSource(cached, initialOffset)
      status.value = 'ready'
      if (autoPlay) {
        await play()
      }
      return
    }

    // Cache miss -> synthesize via worker (check partial cache first)
    status.value = 'loading'
    const sentences = splitSentences(script)
    let target = Math.min(2, sentences.length)

    let buffers: Float32Array[] = []
    let sampleRate = 16000

    const cachedPartial = cache.getPartial ? await cache.getPartial(key) : undefined
    if (
      cachedPartial
      && cachedPartial.total === sentences.length
      && cachedPartial.chunks.length > 0
      && cachedPartial.chunks.length < sentences.length
    ) {
      buffers = [...cachedPartial.chunks]
      sampleRate = cachedPartial.sampleRate
      synthIndex.value = buffers.length
      target = Math.max(target, buffers.length)
    } else {
      synthIndex.value = 0
    }
    const startIndex = buffers.length
    targetBufferCount.value = target
    synthTotal.value = sentences.length
    let currentPlayingIndex = 0
    let isPlayingPreRoll = false
    let isStreaming = true
    let isWaitingForNextChunk = false
    cancelWaitingForChunk = () => {
      isWaitingForNextChunk = false
    }
    const playPreRoll = (count = target) => {
      if (activeKey !== key || buffers.length < count) return
      isPlayingPreRoll = true
      currentPlayingIndex = count - 1
      const preRollWav = encodeWav(concatFloat32(buffers.slice(0, count)), sampleRate)
      setSource(preRollWav, initialOffset)
      status.value = 'ready'
      if (autoPlay && !isUserPaused) {
        void play()
      }
    }

    const playChunk = (index: number, shouldPlay = playing.value) => {
      if (activeKey !== key || index >= buffers.length) return
      const chunk = buffers[index]
      if (!chunk) return
      isPlayingPreRoll = false
      currentPlayingIndex = index
      const chunkWav = encodeWav(chunk, sampleRate)
      setSource(chunkWav)
      status.value = 'ready'
      if (shouldPlay && !isUserPaused) {
        void play()
      }
    }

    if (buffers.length >= target && !isPlayingPreRoll) {
      playPreRoll(buffers.length)
    }

    onChunkEndedCallback = () => {
      if (!isStreaming || isUserPaused) {
        playing.value = false
        return
      }
      if (currentPlayingIndex + 1 < buffers.length) {
        isWaitingForNextChunk = false
        isPlayingPreRoll = false
        playChunk(currentPlayingIndex + 1, true)
      } else {
        isWaitingForNextChunk = true
        status.value = 'loading'
      }
    }
    try {
      const remainingSentences = sentences.slice(startIndex)
      if (remainingSentences.length > 0) {
        await engine.synthesize(voice.model, remainingSentences, {
          onProgress: (progress) => {
            if (progress.stage === 'download' && progress.progress != null) {
              downloadProgress.value = progress.progress
            }
          },
          onChunk: (samples, sr) => {
            sampleRate = sr
            buffers.push(samples)
            synthIndex.value = buffers.length
            if (cache.savePartial) {
              void cache.savePartial(key, {
                chunks: buffers,
                sampleRate,
                total: sentences.length,
              })
            }

            if (buffers.length === target && activeKey === key && !isPlayingPreRoll) {
              playPreRoll()
            } else if (isStreaming && isWaitingForNextChunk && !isUserPaused && currentPlayingIndex + 1 < buffers.length) {
              isWaitingForNextChunk = false
              playChunk(currentPlayingIndex + 1, true)
            }
          },
          onDevice: (d) => {
            device.value = d
          },
        }, startIndex)
      }
    } catch (error) {
      const isCancelled = error instanceof Error && error.message === 'Synthesis cancelled'
      if (isCancelled) {
        if (cache.savePartial && buffers.length > 0 && buffers.length < sentences.length) {
          await cache.savePartial(key, {
            chunks: buffers,
            sampleRate,
            total: sentences.length,
          })
        }
        return
      }
      console.error('[useSliceAudio] Device TTS Error (synthesizeOnDevice):', error)
      onChunkEndedCallback = null
      status.value = 'error'
      const info = categorizeAudioError(error, 'device')
      errorMessage.value = info.rawMessage
      errorInfo.value = info
      return
    }
    if (activeKey !== key || buffers.length < sentences.length) {
      onChunkEndedCallback = null
      return
    }

    if (buffers.length === 0) {
      onChunkEndedCallback = null
      status.value = 'error'
      const info = categorizeAudioError(new Error('Synthesis returned empty audio buffer'), 'device')
      errorMessage.value = info.rawMessage
      errorInfo.value = info
      return
    }
    isStreaming = false
    onChunkEndedCallback = null
    cancelWaitingForChunk = null
    if (buffers.length === sentences.length) {
      const complete = encodeWav(concatFloat32(buffers), sampleRate)
      await cache.set(key, complete)
      if (cache.deletePartial) {
        await cache.deletePartial(key)
      }

      if (buffers.length > 1) {
        const el = ensureAudio()
        if (el) {
          let offsetSec = 0
          if (isPlayingPreRoll) {
            offsetSec = el.currentTime
          } else {
            for (let i = 0; i < currentPlayingIndex; i++) {
              const buf = buffers[i]
              if (buf) {
                offsetSec += buf.length / sampleRate
              }
            }
            offsetSec += el.currentTime
          }

          const wasPlaying = playing.value
          setSource(complete, offsetSec)
          status.value = 'ready'
          if (wasPlaying) {
            void play()
          }
        }
      } else {
        status.value = 'ready'
      }
    }
  }
  function toggle(source: NarrationSource): void {
    if (playing.value) {
      pause()
      return
    }
    if (audio.value?.src) {
      void play()
      return
    }
    void loadAndPlay(source)
  }

  function setSpeed(value: number): void {
    speed.value = value
    const el = ensureAudio()
    if (el) el.playbackRate = value
    if (engineMode.value === 'system' && activeUtterance) {
      activeUtterance.rate = value
    }
  }

  function setVolume(value: number): void {
    const bounded = Math.max(0, Math.min(1.0, value))
    volume.value = bounded
    if (audio.value) audio.value.volume = bounded
    if (engineMode.value === 'system' && activeUtterance) {
      activeUtterance.volume = bounded
    }
  }

  function seek(time: number): void {
    if (engineMode.value === 'system') {
      if (systemSentences.length === 0) return

      const targetTime = Math.max(0, Math.min(duration.value, time))
      currentTime.value = targetTime

      let accumulated = 0
      let targetIndex = 0
      for (let i = 0; i < systemSentenceDurations.length; i++) {
        const dur = systemSentenceDurations[i]
        if (accumulated + dur > targetTime || i === systemSentenceDurations.length - 1) {
          targetIndex = i
          break
        }
        accumulated += dur
      }

      systemSentenceIndex = targetIndex
      if (playing.value && !isUserPaused) {
        stopSystemTimeTicker()
        const synth = getSpeechSynth()
        if (synth) {
          isSystemStopped = true
          synth.cancel()
          isSystemStopped = false
        }
        if (resumeSystemSpeech) {
          resumeSystemSpeech()
        }
      }
      return
    }

    if (audio.value) audio.value.currentTime = time
  }

  watch(speed, (value) => {
    const el = ensureAudio()
    if (el) el.playbackRate = value
    if (engineMode.value === 'system' && systemSentences.length > 0) {
      systemSentenceDurations = computeSystemSentenceDurations(systemSentences, value)
      duration.value = systemSentenceDurations.reduce((a, b) => a + b, 0)
    }
  })

  function dispose(): void {
    stopSystemTimeTicker()
    const engineToDispose = deps.engine ?? engineInstance
    engineToDispose?.dispose()
    if (objectUrl) {
      URL.revokeObjectURL(objectUrl)
      objectUrl = null
    }
    audio.value?.pause()
    stopSilentCarrier()
    const synth = getSpeechSynth()
    if (synth) {
      synth.cancel()
    }
    isSystemStopped = true
    resumeSystemSpeech = null
    systemSentences = []
    systemSentenceDurations = []
    systemSentenceIndex = 0
    activeKey = null
    activeUtterance = null
    status.value = 'idle'
    device.value = null
    errorMessage.value = null
    errorInfo.value = null
  }
  if (getCurrentScope()) onScopeDispose(() => dispose())

  return {
    status,
    playing,
    currentTime,
    duration,
    downloadProgress,
    synthIndex,
    synthTotal,
    targetBufferCount,
    errorMessage,
    errorInfo,
    device,
    speed,
    engineMode,
    activeCascadeTier,
    selectedVoice,
    selectedSystemVoice,
    systemVoices,
    pitch,
    autoAdvance,
    audioQuota,
    isNearQuota,
    isQuotaExhausted,
    setEngineMode,
    setVoice,
    setSystemVoice,
    setPitch,
    setAutoAdvance,
    fetchQuota,
    loadAndPlay,
    play,
    pause,
    toggle,
    setSpeed,
    volume,
    setVolume,
    seek,
    dispose,
    playSliceTransitionChime,
    updateMediaSessionMetadata,
  }
}

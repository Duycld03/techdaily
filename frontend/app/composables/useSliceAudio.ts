// System-first narration orchestration. Engine policy, media operation ownership,
// worker transport, Local PCM playback, and listening records have separate owners.
// Persisted cursors describe only the resolved engine's actual playback surface.

import { computed, getCurrentScope, onScopeDispose, ref, watch } from 'vue'
import { useStorage } from '@vueuse/core'
import { computeContentHash, extractNarrationScript, splitSentences } from '~/utils/narrationScript'
import { resolveVoiceForLanguage } from '~/utils/ttsVoices'
import { playLocalAudio } from '~/utils/audioLocalPlayback'
import { createAudioPlaybackMedia } from '~/utils/audioPlaybackMedia'
import { createWorkerEngine } from '~/utils/audioSynthesisWorker'
import { discoverSystemVoices, downstreamFailureReason, engineCandidates, isDownstreamFailure } from '~/utils/audioEnginePolicy'
import type { AudioFallbackReason } from '~/utils/audioEnginePolicy'
import { AUDIO_PROGRESS_INTERVAL_MS, createAudioPlaybackProgress } from '~/utils/audioPlaybackProgress'
import type { AudioPlaybackCheckpoint, AudioPlaybackCursor, AudioPlaybackIdentity } from '~/utils/audioPlaybackProgress'
import { browserStorage, readAudioPreference, readStored, writeAudioPreference } from '~/utils/clientStorage'
import type { AudioEnginePreference } from '~/utils/clientStorage'
import { buildAudioKey, createIdbBackend, createSliceAudioCache } from '~/utils/sliceAudioCache'
import type { SliceAudioCache } from '~/utils/sliceAudioCache'
import type { AudioQuotaInfo } from '~/types/audio'

export type AudioEngine = 'cloud' | 'device' | 'system'
export type AudioStartReason = 'play' | 'engine-change' | 'voice-change' | 'source-change' | 'auto-advance' | 'recovery' | 'fallback'

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
  onSliceEnded?: (isCurrent: () => boolean) => void
  onPause?: () => void
  onNextTrack?: (isCurrent: () => boolean) => void
  onPreviousTrack?: (isCurrent: () => boolean) => void
  speechSynthesis?: SpeechSynthesis
  getAccount?: () => { accountId: string | null; sessionGeneration: number }
  onFallback?: (transition: { from: AudioEngine; to: AudioEngine; reason: AudioFallbackReason }) => void
  onPersistenceUnavailable?: () => void
}

export const AUDIO_SPEED_STORAGE_KEY = 'deeppace_reader_audio_speed'
export const AUDIO_VOICE_STORAGE_KEY = 'deeppace_reader_audio_voice'
export const AUDIO_SYSTEM_VOICE_STORAGE_KEY = 'deeppace_reader_audio_system_voice'
export const AUDIO_AUTO_ADVANCE_STORAGE_KEY = 'deeppace_reader_audio_auto_advance'

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
  const synth = speechSynth ?? (typeof window !== 'undefined' && 'speechSynthesis' in window && window.speechSynthesis ? window.speechSynthesis : null)
  if (!synth) return []
  return (synth.getVoices() || []).map(v => ({
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
      artist: payload.artist || 'DeepPace',
      album: payload.album || 'DeepPace Reader',
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

export type AudioErrorType =
  | 'QUOTA_EXHAUSTED'
  | 'DEVICE_OOM'
  | 'DEVICE_INIT_FAILED'
  | 'NETWORK_ERROR'
  | 'SYSTEM_TTS_FAILED'
  | 'UNKNOWN'

export interface AudioErrorInfo {
  code: AudioErrorType
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


export function useSliceAudio(deps: SliceAudioDeps = {}) {
  const isClient = typeof window !== 'undefined'
  const storage = browserStorage('localStorage')
  const progress = createAudioPlaybackProgress(storage)
  const speed = useStorage(AUDIO_SPEED_STORAGE_KEY, 1)
  const autoAdvance = useStorage(AUDIO_AUTO_ADVANCE_STORAGE_KEY, true)
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
  const enginePreference = ref<AudioEnginePreference>(deps.defaultEngine ?? readAudioPreference(storage))
  const engineMode = ref<AudioEngine>(enginePreference.value === 'auto' ? 'system' : enginePreference.value)
  const activeCascadeTier = computed(() => engineMode.value)
  const selectedVoice = ref(readStored(storage, AUDIO_VOICE_STORAGE_KEY) ?? '')
  const selectedSystemVoice = ref(readStored(storage, AUDIO_SYSTEM_VOICE_STORAGE_KEY) ?? '')
  const systemVoices = ref<SystemVoiceOption[]>([])
  const volume = ref(1)
  const persistenceAvailable = ref(true)
  const restoredProgress = ref(false)
  const audioQuota = ref<AudioQuotaInfo | null>(null)
  const isNearQuota = computed(() => (audioQuota.value?.usedCharacters ?? 0) >= 900_000 || !!audioQuota.value?.isNearLimit)
  const isQuotaExhausted = computed(() => (audioQuota.value?.usedCharacters ?? 0) >= 950_000 || !!audioQuota.value?.isExhausted)
  let engineInstance: TtsEngine | null = null
  let cacheInstance: SliceAudioCache | null = null
  let disposed = false
  let noticeSent = false
  let lastSource: NarrationSource | null = null
  let pendingReason: AudioStartReason | null = null
  let transitionFrom: AudioEngine | null = null
  let systemTicker: ReturnType<typeof setInterval> | null = null
  let utterance: SpeechSynthesisUtterance | null = null

  interface Session {
    source: NarrationSource
    contentHash: string
    account: { accountId: string | null; sessionGeneration: number }
    controller: AbortController
    intent: boolean
    preparing: boolean
    localStarted: boolean
    attempted: AudioEngine[]
    reason: AudioStartReason
    restartPending: boolean
    identity: AudioPlaybackIdentity | null
    cursor: (() => AudioPlaybackCursor) | null
    completed: boolean
    operation: number
    sentenceIndex: number
    sentences: string[]
    sentenceDurations: number[]
    speak: (() => void) | null
    resumeLocal: (() => boolean) | null
    seekLocal: ((seconds: number) => boolean) | null
  }
  let session: Session | null = null
  const account = () => deps.getAccount?.() ?? { accountId: null, sessionGeneration: 0 }
  const speechSynth = () => deps.speechSynthesis ?? (isClient ? window.speechSynthesis : null) ?? null
  const current = (value: Session) => !disposed && session === value
    && value.account.accountId === account().accountId && value.account.sessionGeneration === account().sessionGeneration
  const intendsPlay = () => !!session && current(session) && session.intent
  const media = createAudioPlaybackMedia({
    createAudio: () => deps.createAudio?.() ?? (isClient ? new Audio() : null),
    speed: () => speed.value, volume: () => volume.value, intendsPlay,
    onTime: (time, total) => { currentTime.value = time; duration.value = total },
    onBlocked: () => { if (session) session.intent = false; playing.value = false },
  })

  // Resolve Nuxt-bound dependencies during setup, never for an injected transport.
  let baseUrl = ''
  let auth: { token: string | null } | null = null
  if (!deps.fetchClient) {
    try { baseUrl = process.env.API_INTERNAL_URL || String(useRuntimeConfig().public.apiBaseUrl || '').trim() } catch { /* Relative backend routes are the default. */ }
    try { auth = useAuthStore() } catch { /* Pure composable consumers inject their transport. */ }
  }
  const fetchClient = deps.fetchClient ?? ((input: RequestInfo | URL, init?: RequestInit) => {
    const headers = new Headers(init?.headers)
    if (auth?.token && !headers.has('Authorization')) headers.set('Authorization', `Bearer ${auth.token}`)
    const url = typeof input === 'string' && input.startsWith('/') ? `${baseUrl}${input}` : input
    return globalThis.fetch(url, { ...init, headers })
  })
  const getCache = () => deps.cache ?? (cacheInstance ??= createSliceAudioCache(createIdbBackend()))
  const getEngine = () => deps.engine ?? (engineInstance ??= createWorkerEngine())

  function unavailable(): void {
    persistenceAvailable.value = false
    if (!noticeSent) { noticeSent = true; deps.onPersistenceUnavailable?.() }
  }

  function save(value = session, completed = false): void {
    if (!value || !current(value) || !value.account.accountId || !value.identity || !value.cursor || !persistenceAvailable.value) return
    if (!progress.save(value.identity, value.cursor(), completed || value.completed)) unavailable()
  }

  function checkpoint(value: Session): AudioPlaybackCheckpoint | null {
    if (!value.account.accountId || !value.identity || value.reason !== 'play' || !persistenceAvailable.value) return null
    const result = progress.read(value.identity)
    if (!result.available) unavailable()
    return result.checkpoint?.completed ? null : result.checkpoint
  }

  function stopTicker(): void {
    clearInterval(systemTicker ?? undefined)
    systemTicker = null
  }

  function retire(): void {
    const previous = session
    session = null
    previous?.controller.abort()
    if (previous) previous.operation++
    stopTicker()
    speechSynth()?.cancel()
    utterance = null
    media.retire()
    if (previous) (deps.engine ?? engineInstance)?.cancel?.()
    playing.value = false
    synthIndex.value = 0
    synthTotal.value = 0
    targetBufferCount.value = 0
    downloadProgress.value = 0
    device.value = null
  }

  function resetSource(): void {
    save()
    retire()
    lastSource = null
    status.value = 'idle'
    currentTime.value = 0
    duration.value = 0
    restoredProgress.value = false
    errorMessage.value = null
    errorInfo.value = null
  }

  function setEnginePreference(preference: AudioEnginePreference): void {
    if (!writeAudioPreference(preference, storage)) unavailable()
    enginePreference.value = preference
    errorMessage.value = null
    errorInfo.value = null
    if (preference === engineMode.value) return
    save()
    if (preference === 'auto' && session && engineMode.value === 'system'
      && filterSystemVoicesForLanguage(systemVoices.value, session.source.language).length) return
    transitionFrom = engineMode.value
    if (preference !== 'auto') pendingReason = 'engine-change'
    retire()
    if (preference !== 'auto') engineMode.value = preference
    status.value = 'idle'
    currentTime.value = 0
    duration.value = 0
    restoredProgress.value = false
  }

  function storeVoice(key: string, voiceId: string): void {
    try { storage?.setItem(key, voiceId) } catch { unavailable() }
  }

  function changeVoice(): void {
    save()
    pendingReason = 'voice-change'
    retire()
    status.value = 'idle'
    restoredProgress.value = false
  }

  function setVoice(voiceId: string): void {
    if (voiceId === selectedVoice.value) return
    changeVoice()
    selectedVoice.value = voiceId
    storeVoice(`${AUDIO_VOICE_STORAGE_KEY}_${voiceId.startsWith('vi-') ? 'vi' : 'en'}`, voiceId)
    storeVoice(AUDIO_VOICE_STORAGE_KEY, voiceId)
  }

  function setSystemVoice(voiceId: string): void {
    if (voiceId === selectedSystemVoice.value) return
    changeVoice()
    selectedSystemVoice.value = voiceId
    storeVoice(AUDIO_SYSTEM_VOICE_STORAGE_KEY, voiceId)
  }

  async function fetchQuota(value?: Session): Promise<AudioQuotaInfo | null> {
    const admission = { ...account() }
    try {
      const response = await fetchClient('/api/v1/library/audio/quota', { signal: value?.controller.signal })
      if (!response.ok) {
        if (response.status >= 400 && response.status < 500) {
          throw Object.assign(new Error(`Cloud audio quota failed (${response.status})`), { status: response.status })
        }
        return null
      }
      const quota: unknown = await response.json()
      if (!quota || typeof quota !== 'object' || !('monthlyLimit' in quota) || !('usedCharacters' in quota)
        || !('remainingCharacters' in quota) || !('isNearLimit' in quota) || !('isExhausted' in quota)
        || typeof quota.monthlyLimit !== 'number' || !Number.isFinite(quota.monthlyLimit) || quota.monthlyLimit < 0
        || typeof quota.usedCharacters !== 'number' || !Number.isFinite(quota.usedCharacters) || quota.usedCharacters < 0
        || typeof quota.remainingCharacters !== 'number' || !Number.isFinite(quota.remainingCharacters) || quota.remainingCharacters < 0
        || typeof quota.isNearLimit !== 'boolean' || typeof quota.isExhausted !== 'boolean') throw new Error('Invalid Cloud audio quota response')
      if (disposed || (value && !current(value)) || admission.accountId !== account().accountId
        || admission.sessionGeneration !== account().sessionGeneration) return null
      const validated: AudioQuotaInfo = {
        monthlyLimit: quota.monthlyLimit, usedCharacters: quota.usedCharacters, remainingCharacters: quota.remainingCharacters,
        isNearLimit: quota.isNearLimit, isExhausted: quota.isExhausted,
      }
      audioQuota.value = validated
      return validated
    } catch (error) {
      if (error && typeof error === 'object' && 'status' in error
        && typeof error.status === 'number' && error.status >= 400 && error.status < 500) throw error
      if (!(error instanceof DOMException && error.name === 'AbortError')) console.warn('[useSliceAudio] Could not read Cloud quota', error)
      return null
    }
  }

  function finish(value: Session): void {
    if (!current(value) || !value.intent || value.completed) return
    value.completed = true
    const completedCursor = value.cursor?.()
    if (completedCursor) value.cursor = () => completedCursor
    save(value, true)
    value.intent = false
    value.operation++
    stopTicker()
    media.retire()
    playing.value = false
    currentTime.value = duration.value
    const operation = value.operation
    deps.onSliceEnded?.(() => current(value) && value.operation === operation)
  }

  function showError(error: unknown, value: Session): void {
    if (!current(value)) return
    value.preparing = false
    value.intent = false
    playing.value = false
    status.value = 'error'
    const info = categorizeAudioError(error, engineMode.value)
    errorMessage.value = info.rawMessage
    errorInfo.value = info
    media.pause()
    stopTicker()
  }

  function ready(value: Session, restored: boolean): void {
    if (!current(value)) return
    status.value = 'ready'
    value.restartPending = false
    restoredProgress.value = restored
    // This also converts a paused streaming cursor at complete-file handoff.
    save(value)
  }

  function selectIdentity(value: Session, engine: AudioEngine, voiceId: string, hash: string): void {
    value.identity = { accountId: value.account.accountId ?? '', chunkId: value.source.chunkId, engine, voiceId, contentHash: hash }
    value.cursor = null
  }

  function startSystem(value: Session, voice: SystemVoiceOption, hash: string): void {
    const synth = speechSynth()
    if (!synth || typeof SpeechSynthesisUtterance === 'undefined') throw new Error('System speech is unavailable')
    selectedSystemVoice.value = voice.id
    selectIdentity(value, 'system', voice.id, hash)
    const saved = checkpoint(value)?.cursor
    value.sentenceIndex = saved?.kind === 'system' && saved.sentenceIndex < value.sentences.length ? saved.sentenceIndex : 0
    restoredProgress.value = value.sentenceIndex > 0
    value.sentenceDurations = value.sentences.map(text => Math.max(1, text.trim().length / 16 / Math.max(0.5, Math.min(2, speed.value))))
    duration.value = value.sentenceDurations.reduce((sum, seconds) => sum + seconds, 0)
    const elapsed = () => value.sentenceDurations.slice(0, value.sentenceIndex).reduce((sum, seconds) => sum + seconds, 0)
    currentTime.value = elapsed()
    value.cursor = () => ({ kind: 'system', sentenceIndex: Math.min(value.sentenceIndex, value.sentences.length - 1) })
    value.preparing = false
    status.value = 'ready'
    value.speak = () => {
      if (!current(value) || !value.intent) return
      stopTicker()
      const operation = ++value.operation
      synth.cancel()
      const valid = () => current(value) && value.operation === operation && value.intent
      const index = value.sentenceIndex
      const text = value.sentences[index]
      if (!text) { finish(value); return }
      const speech = new SpeechSynthesisUtterance(text)
      utterance = speech
      speech.voice = synth.getVoices().find(item => (item.voiceURI || item.name) === voice.id) ?? null
      speech.lang = voice.lang
      speech.rate = Math.max(0.5, Math.min(2, speed.value))
      speech.volume = volume.value
      speech.onstart = () => {
        if (!valid()) return
        playing.value = true
        const startedAt = performance.now()
        systemTicker = setInterval(() => {
          if (!valid()) return
          currentTime.value = elapsed() + Math.min(value.sentenceDurations[index] ?? 0, (performance.now() - startedAt) / 1000)
        }, 200)
      }
      speech.onpause = () => { if (valid()) stopTicker() }
      speech.onresume = () => { if (valid()) playing.value = true }
      speech.onend = () => {
        if (!valid()) return
        stopTicker()
        value.sentenceIndex++
        if (value.sentenceIndex >= value.sentences.length) finish(value)
        else { currentTime.value = elapsed(); save(value); value.speak?.() }
      }
      speech.onerror = event => {
        if (!valid() || event.error === 'canceled' || event.error === 'interrupted') return
        stopTicker()
        value.operation++
        media.retire()
        void advance(value, new Error(event.error), 'system-failed')
      }
      media.mount('data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEARKwAAIhYAQACABAAZGF0YQAAAAA=', { valid, carrier: true })
      try {
        synth.speak(speech)
        if (valid()) playing.value = true
      } catch (error) {
        if (valid()) void advance(value, error, 'system-failed')
      }
    }
    value.speak()
  }

  async function startCloud(value: Session, hash: string): Promise<void> {
    const language = value.source.language?.toLowerCase().startsWith('vi') ? 'vi' : 'en'
    const scoped = readStored(storage, `${AUDIO_VOICE_STORAGE_KEY}_${language}`)
    const voiceId = resolveCloudVoiceForLanguage(value.source.language, selectedVoice.value || scoped)
    selectedVoice.value = voiceId
    selectIdentity(value, 'cloud', voiceId, hash)
    const saved = checkpoint(value)?.cursor
    const key = buildAudioKey(value.source.chunkId, voiceId, hash)
    const cache = getCache()
    let blob = await cache.get(key)
    if (!current(value) || !value.intent) return
    const cached = !!blob?.size
    if (!cached) {
      if (!audioQuota.value) await fetchQuota(value)
      if (!current(value) || !value.intent) return
      if (isNearQuota.value || isQuotaExhausted.value) {
        throw Object.assign(new Error('QUOTA_EXHAUSTED'), { status: 429, quotaGuard: !isQuotaExhausted.value })
      }
      const response = await fetchClient(`/api/v1/library/chunks/${value.source.chunkId}/audio`, {
        method: 'POST', signal: value.controller.signal, headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ voiceId, contentHash: hash, narrationScript: extractNarrationScript(value.source.markdown) }),
      })
      if (!current(value) || !value.intent) return
      if (!response.ok) throw Object.assign(new Error(`Synthesis failed (${response.status})`), { status: response.status })
      blob = await response.blob()
      if (!current(value) || !value.intent) return
      if (!blob.size) throw Object.assign(new Error('Cloud speech returned empty audio'), { status: 503 })
      await cache.set(key, blob)
      if (!current(value) || !value.intent) return
    }
    value.cursor = () => ({ kind: 'file', seconds: media.time() })
    value.preparing = false
    media.mount(blob!, {
      valid: () => current(value), offset: cached && saved?.kind === 'file' ? saved.seconds : 0,
      onReady: (_offset, restored) => ready(value, restored), onEnded: () => finish(value),
      onError: error => { if (current(value) && value.intent) void advance(value, Object.assign(error, { status: 503 })) },
    })
    status.value = 'ready'
  }

  async function startDevice(value: Session, hash: string): Promise<void> {
    const voice = resolveVoiceForLanguage(value.source.language)
    selectIdentity(value, 'device', voice.id, hash)
    value.localStarted = true
    await playLocalAudio({
      engine: getEngine, cache: getCache(), key: buildAudioKey(value.source.chunkId, voice.id, hash),
      model: voice.model, sentences: value.sentences, checkpoint: checkpoint(value)?.cursor ?? null,
      valid: () => current(value), intendsPlay: () => current(value) && value.intent,
      mount: (blob, source) => { if (current(value)) media.mount(blob, source) }, time: media.time,
      setCursor: read => { value.cursor = read },
      setResume: resume => { value.resumeLocal = resume },
      setSeek: seek => { value.seekLocal = seek },
      onProgress: (generated, total, buffered) => { synthIndex.value = generated; synthTotal.value = total; targetBufferCount.value = buffered },
      onDownload: percent => { downloadProgress.value = percent },
      onDevice: resolved => { device.value = resolved },
      onReady: restored => { value.preparing = false; ready(value, restored) },
      onBuffering: () => { if (current(value)) status.value = 'loading' },
      onCompleted: () => finish(value), onError: error => showError(error, value),
    })
  }

  async function advance(value: Session, failure?: unknown, reason?: AudioFallbackReason): Promise<void> {
    if (!current(value) || !value.intent) return
    const from = engineMode.value
    if (failure && !isDownstreamFailure(failure, from)) { showError(failure, value); return }
    if (failure) {
      save(value)
      value.reason = 'fallback'
      value.operation++
      speechSynth()?.cancel()
      media.retire()
      playing.value = true
      currentTime.value = 0
      duration.value = 0
      restoredProgress.value = false
    }
    const hash = value.contentHash
    if (!current(value) || !value.intent) return
    const candidates = engineCandidates(enginePreference.value)
    let skippedReason = reason
    for (const candidate of candidates) {
      if (value.attempted.includes(candidate)) continue
      value.attempted = [...value.attempted, candidate]
      let voices: SystemVoiceOption[] = []
      if (candidate === 'system') {
        voices = await discoverSystemVoices(speechSynth(), value.source.language, value.controller.signal)
        if (!current(value) || !value.intent) return
        systemVoices.value = getAvailableSystemVoices(speechSynth())
        if (!voices.length || typeof SpeechSynthesisUtterance === 'undefined') { skippedReason = 'voice-unavailable'; continue }
      }
      if (candidate === 'cloud' && (!navigator.onLine || isNearQuota.value || isQuotaExhausted.value)) {
        skippedReason = navigator.onLine ? 'quota-exhausted' : 'offline'
        if (isQuotaExhausted.value) deps.onQuotaExhausted?.()
        continue
      }
      if (transitionFrom !== null) value.reason = transitionFrom !== candidate || value.restartPending ? 'engine-change' : 'play'
      transitionFrom = null
      engineMode.value = candidate
      if (failure || skippedReason) deps.onFallback?.({ from, to: candidate, reason: skippedReason ?? downstreamFailureReason(failure, from) })
      value.preparing = true
      try {
        if (candidate === 'system') {
          const voice = voices.find(item => item.id === selectedSystemVoice.value) ?? voices[0]!
          startSystem(value, voice, hash)
        } else if (candidate === 'cloud') await startCloud(value, hash)
        else await startDevice(value, hash)
        return
      } catch (error) {
        if (!current(value) || !value.intent) return
        if (!isDownstreamFailure(error, candidate)) { showError(error, value); return }
        if (error && typeof error === 'object' && 'status' in error && error.status === 429
          && !('quotaGuard' in error && error.quotaGuard)) {
          deps.onQuotaExhausted?.()
          if (audioQuota.value) audioQuota.value = { ...audioQuota.value, isExhausted: true }
        }
        await advance(value, error, downstreamFailureReason(error, candidate))
        return
      }
    }
    showError(failure ?? new Error('No narration engine is available'), value)
  }

  async function loadAndPlay(source: NarrationSource, options: { autoplay?: boolean; reason?: AudioStartReason } = {}): Promise<void> {
    save()
    retire()
    lastSource = { ...source }
    errorMessage.value = null
    errorInfo.value = null
    restoredProgress.value = false
    currentTime.value = 0
    duration.value = 0
    status.value = 'idle'
    if (disposed || !isClient || !source.isAiFormatted || !source.markdown) return
    if (options.autoplay === false) {
      if (options.reason && options.reason !== 'play') pendingReason ??= options.reason
      return
    }
    const script = extractNarrationScript(source.markdown)
    if (!script) return
    const value: Session = {
      source: { ...source }, contentHash: '', account: { ...account() }, controller: new AbortController(), intent: true, preparing: true, localStarted: false,
      attempted: [], reason: pendingReason ?? options.reason ?? 'play', restartPending: pendingReason !== null, identity: null, cursor: null, completed: false,
      operation: 0, sentenceIndex: 0, sentences: splitSentences(script), sentenceDurations: [], speak: null, resumeLocal: null, seekLocal: null,
    }
    pendingReason = null
    session = value
    status.value = 'loading'
    playing.value = true
    try {
      value.contentHash = await computeContentHash(script)
      if (current(value) && value.intent) await advance(value)
    } catch (error) {
      if (current(value) && value.intent && !(error instanceof DOMException && error.name === 'AbortError')) showError(error, value)
    }
  }

  async function play(): Promise<void> {
    const value = session
    if (!value || !current(value) || value.completed || status.value === 'error') {
      if (lastSource) await loadAndPlay(lastSource, { reason: value?.completed || status.value === 'error' ? 'recovery' : 'play' })
      return
    }
    value.operation++
    value.intent = true
    playing.value = true
    if (engineMode.value === 'system') value.speak?.()
    else {
      if (value.resumeLocal?.()) return
      const element = media.element()
      if (element?.ended || (element && element.duration > 0 && element.currentTime >= element.duration)) media.seek(0)
      await media.play()
    }
  }

  function pause(): void {
    deps.onPause?.()
    const value = session
    save(value)
    if (value) {
      value.intent = false
      value.operation++
      if (value.preparing && !value.localStarted) {
        if (value.reason !== 'play') pendingReason = value.reason
        retire()
        status.value = 'ready'
      }
    }
    stopTicker()
    if (engineMode.value === 'system') { speechSynth()?.cancel(); media.retire() }
    else media.pause()
    playing.value = false
    if (status.value === 'loading') status.value = 'ready'
  }

  function seek(time: number): void {
    const value = session
    if (!value || !current(value) || !Number.isFinite(time)) return
    value.operation++
    if (engineMode.value !== 'system') {
      if (value.seekLocal ? value.seekLocal(time) : media.seek(time)) save(value)
      return
    }
    const target = Math.max(0, Math.min(duration.value, time))
    let elapsed = 0
    value.sentenceIndex = value.sentences.length - 1
    for (let index = 0; index < value.sentenceDurations.length; index++) {
      const seconds = value.sentenceDurations[index]!
      if (elapsed + seconds > target) { value.sentenceIndex = index; break }
      elapsed += seconds
    }
    currentTime.value = target
    stopTicker()
    speechSynth()?.cancel()
    if (value.intent) value.speak?.()
    save(value)
  }

  function setSpeed(value: number): void { speed.value = value; media.setSpeed(value); if (utterance) utterance.rate = value }
  function setVolume(value: number): void { volume.value = Math.max(0, Math.min(1, value)); media.setVolume(volume.value); if (utterance) utterance.volume = volume.value }
  const stopSpeedWatch = watch(speed, value => {
    media.setSpeed(value)
    if (session?.sentenceDurations.length) {
      session.sentenceDurations = session.sentences.map(text => Math.max(1, text.trim().length / 16 / Math.max(0.5, Math.min(2, value))))
      duration.value = session.sentenceDurations.reduce((sum, seconds) => sum + seconds, 0)
    }
  })
  const stopAccountWatch = watch([() => account().accountId, () => account().sessionGeneration], () => {
    // Admission already changed: never flush the outgoing session as the new account.
    retire()
    audioQuota.value = null
    lastSource = null
    pendingReason = null
    transitionFrom = null
    restoredProgress.value = false
    persistenceAvailable.value = true
    noticeSent = false
    status.value = 'idle'
    currentTime.value = 0
    duration.value = 0
  }, { flush: 'sync' })
  const loadVoices = () => { systemVoices.value = getAvailableSystemVoices(speechSynth()) }
  const hidden = () => { if (document.visibilityState === 'hidden') save() }
  const pagehide = () => save()
  const checkpointTimer = isClient ? setInterval(() => { if (intendsPlay()) save() }, AUDIO_PROGRESS_INTERVAL_MS) : null
  function navigateTrack(callback: ((isCurrent: () => boolean) => void) | undefined): void {
    const value = session
    const operation = value?.operation
    const { accountId, sessionGeneration } = account()
    callback?.(() => {
      const admission = account()
      return !disposed && session === value && value?.operation === operation
        && admission.accountId === accountId && admission.sessionGeneration === sessionGeneration
    })
  }

  if (isClient) {
    loadVoices()
    speechSynth()?.addEventListener?.('voiceschanged', loadVoices)
    document.addEventListener('visibilitychange', hidden)
    window.addEventListener('pagehide', pagehide)
    try {
      navigator.mediaSession?.setActionHandler('play', () => { void play() })
      navigator.mediaSession?.setActionHandler('pause', pause)
      if (deps.onNextTrack) navigator.mediaSession?.setActionHandler('nexttrack', () => navigateTrack(deps.onNextTrack))
      if (deps.onPreviousTrack) navigator.mediaSession?.setActionHandler('previoustrack', () => navigateTrack(deps.onPreviousTrack))
    } catch { /* Browser support varies by action. */ }
  }

  function dispose(): void {
    if (disposed) return
    save()
    retire()
    disposed = true
    stopSpeedWatch()
    stopAccountWatch()
    clearInterval(checkpointTimer ?? undefined)
    speechSynth()?.removeEventListener?.('voiceschanged', loadVoices)
    if (isClient) {
      document.removeEventListener('visibilitychange', hidden)
      window.removeEventListener('pagehide', pagehide)
      try {
        for (const action of ['play', 'pause', 'nexttrack', 'previoustrack'] as const) navigator.mediaSession?.setActionHandler(action, null)
      } catch { /* Browser support varies by action. */ }
    }
    ;(deps.engine ?? engineInstance)?.dispose()
    lastSource = null
    status.value = 'idle'
  }
  if (getCurrentScope()) onScopeDispose(dispose)

  return {
    status, playing, currentTime, duration, downloadProgress, synthIndex, synthTotal, targetBufferCount,
    errorMessage, errorInfo, device, speed, enginePreference, engineMode, activeCascadeTier,
    selectedVoice, selectedSystemVoice, systemVoices, autoAdvance, audioQuota, isNearQuota, isQuotaExhausted,
    persistenceAvailable, restoredProgress, setEnginePreference, resetSource, setVoice, setSystemVoice,
    fetchQuota, loadAndPlay, play, pause, setSpeed, volume, setVolume, seek, dispose,
    playSliceTransitionChime, updateMediaSessionMetadata,
  }
}

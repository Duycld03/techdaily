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

export type AudioEngine = 'cloud' | 'device'

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
  synthesize: (model: string, sentences: string[], handlers: SynthHandlers) => Promise<void>
  dispose: () => void
}

export interface SliceAudioDeps {
  engine?: TtsEngine
  cache?: SliceAudioCache
  createAudio?: () => HTMLAudioElement
  fetchClient?: (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>
  defaultEngine?: AudioEngine
  onQuotaExhausted?: () => void
}

export const AUDIO_SPEED_STORAGE_KEY = 'techdaily_reader_audio_speed'
export const AUDIO_ENGINE_STORAGE_KEY = 'techdaily_reader_audio_engine'
export const AUDIO_VOICE_STORAGE_KEY = 'techdaily_reader_audio_voice'

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

export function resolveCloudVoiceForLanguage(lang?: string | null): string {
  const isVi = lang?.toLowerCase().startsWith('vi')
  return isVi ? 'vi-VN-Neural2-A' : 'en-US-Neural2-F'
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
          pending.delete(m.reqId)
          p.reject(new Error(m.message ?? 'Synthesis failed'))
        }
      }
    }
    return worker
  }

  return {
    synthesize(model, sentences, handlers) {
      return new Promise<void>((resolve, reject) => {
        const w = ensureWorker()
        const reqId = ++reqCounter
        pending.set(reqId, { handlers, resolve, reject })
        w.postMessage({ type: 'synth', reqId, model, sentences })
      })
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
  const errorMessage = ref<string | null>(null)
  const device = ref<ComputeDevice | null>(null)

  const initialEngine: AudioEngine =
    deps.defaultEngine
    ?? (isClient ? (localStorage.getItem(AUDIO_ENGINE_STORAGE_KEY) as AudioEngine) : null)
    ?? (deps.engine && !deps.fetchClient ? 'device' : 'cloud')

  const engineMode = ref<AudioEngine>(initialEngine)
  const selectedVoice = ref<string>(
    (isClient ? localStorage.getItem(AUDIO_VOICE_STORAGE_KEY) : null) ?? ''
  )

  const audioQuota = ref<AudioQuotaInfo | null>(null)
  const isNearQuota = computed(() => (audioQuota.value?.usedCharacters ?? 0) >= 900_000 || !!audioQuota.value?.isNearLimit)
  const isQuotaExhausted = computed(() => (audioQuota.value?.usedCharacters ?? 0) >= 950_000 || !!audioQuota.value?.isExhausted)

  const audio = shallowRef<HTMLAudioElement | null>(null)
  let objectUrl: string | null = null
  let activeKey: string | null = null

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
        const config = useRuntimeConfig()
        baseUrl = (config?.public?.apiBaseUrl as string | undefined)?.trim() || 'http://localhost:5000'
      } catch {
        baseUrl = 'http://localhost:5000'
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
      el.addEventListener('timeupdate', () => { currentTime.value = el.currentTime })
      el.addEventListener('durationchange', () => { duration.value = Number.isFinite(el.duration) ? el.duration : 0 })
      el.addEventListener('play', () => { playing.value = true })
      el.addEventListener('pause', () => { playing.value = false })
      el.addEventListener('ended', () => { playing.value = false })
      el.playbackRate = speed.value
    }
    return audio.value
  }

  function setSource(blob: Blob): void {
    const el = ensureAudio()
    if (!el) return
    if (objectUrl) URL.revokeObjectURL(objectUrl)
    objectUrl = URL.createObjectURL(blob)
    el.src = objectUrl
    el.playbackRate = speed.value
  }

  async function play(): Promise<void> {
    const el = ensureAudio()
    if (!el) return
    try {
      await el.play()
    }
    catch {
      // Autoplay rejections are non-fatal; the user can press play again.
    }
  }

  function pause(): void {
    audio.value?.pause()
  }

  function setEngineMode(mode: AudioEngine): void {
    engineMode.value = mode
    if (isClient) {
      localStorage.setItem(AUDIO_ENGINE_STORAGE_KEY, mode)
    }
  }

  function setVoice(voiceId: string): void {
    selectedVoice.value = voiceId
    if (isClient) {
      localStorage.setItem(AUDIO_VOICE_STORAGE_KEY, voiceId)
    }
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

  async function loadAndPlay(source: NarrationSource): Promise<void> {
    errorMessage.value = null
    device.value = null
    const cache = getCache()
    if (!source.isAiFormatted || !source.markdown || !cache) return

    const script = extractNarrationScript(source.markdown)
    if (!script) return

    const contentHash = await computeContentHash(script)

    if (engineMode.value === 'cloud') {
      const voiceId = selectedVoice.value || resolveCloudVoiceForLanguage(source.language)
      const key = buildAudioKey(source.chunkId, voiceId, contentHash)
      activeKey = key

      // Cache hit in browser IndexedDB
      const cached = await cache.get(key)
      if (cached) {
        setSource(cached)
        status.value = 'ready'
        await play()
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
          errorMessage.value = 'QUOTA_EXHAUSTED'
          // Automatic fallback to On-Device Web Worker
          await synthesizeOnDevice(source, script, contentHash, cache)
          return
        }

        if (!response.ok) {
          status.value = 'error'
          errorMessage.value = `Synthesis failed (${response.status})`
          return
        }

        const blob = await response.blob()
        if (activeKey !== key) return

        await cache.set(key, blob)
        setSource(blob)
        status.value = 'ready'
        await play()
      } catch (err) {
        status.value = 'error'
        errorMessage.value = err instanceof Error ? err.message : String(err)
      }
    } else {
      await synthesizeOnDevice(source, script, contentHash, cache)
    }
  }

  async function synthesizeOnDevice(
    source: NarrationSource,
    script: string,
    contentHash: string,
    cache: SliceAudioCache,
  ): Promise<void> {
    const engine = getEngine()
    if (!engine) return

    const voice = resolveVoiceForLanguage(source.language)
    const key = buildAudioKey(source.chunkId, voice.id, contentHash)
    activeKey = key

    // Cache hit
    const cached = await cache.get(key)
    if (cached) {
      setSource(cached)
      status.value = 'ready'
      await play()
      return
    }

    // Cache miss -> synthesize via worker
    status.value = 'loading'
    downloadProgress.value = 0
    const sentences = splitSentences(script)
    synthTotal.value = sentences.length
    synthIndex.value = 0
    const buffers: Float32Array[] = []
    let sampleRate = 16000

    try {
      await engine.synthesize(voice.model, sentences, {
        onProgress: (progress) => {
          if (progress.stage === 'download' && progress.progress != null) {
            downloadProgress.value = progress.progress
          }
        },
        onChunk: (samples, sr) => {
          sampleRate = sr
          buffers.push(samples)
          synthIndex.value += 1
        },
        onDevice: (d) => {
          device.value = d
        },
      })
    } catch (error) {
      status.value = 'error'
      errorMessage.value = error instanceof Error ? error.message : String(error)
      return
    }

    if (activeKey !== key) return

    if (buffers.length === 0) {
      status.value = 'error'
      return
    }

    const complete = encodeWav(concatFloat32(buffers), sampleRate)
    await cache.set(key, complete)
    setSource(complete)
    status.value = 'ready'
    await play()
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
    if (audio.value) audio.value.playbackRate = value
  }

  function seek(time: number): void {
    if (audio.value) audio.value.currentTime = time
  }

  watch(speed, (value) => {
    if (audio.value) audio.value.playbackRate = value
  })

  function dispose(): void {
    (deps.engine ?? engineInstance)?.dispose()
    if (objectUrl) {
      URL.revokeObjectURL(objectUrl)
      objectUrl = null
    }
    audio.value?.pause()
    activeKey = null
    status.value = 'idle'
    device.value = null
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
    errorMessage,
    device,
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
    toggle,
    setSpeed,
    seek,
    dispose,
  }
}

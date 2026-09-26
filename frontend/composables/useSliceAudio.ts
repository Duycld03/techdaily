// Orchestrates on-device narration for a reader slice: derive the script,
// resolve the language voice, reuse cached audio, otherwise stream synthesis
// from the worker, assemble a complete seekable WAV, cache it, and drive an
// <audio> element (play/pause/seek/speed). All heavy work is client-only.

import { getCurrentScope, onScopeDispose, ref, shallowRef, watch } from 'vue'
import { useStorage } from '@vueuse/core'
import { computeContentHash, extractNarrationScript, splitSentences } from '~/utils/narrationScript'
import { resolveVoiceForLanguage } from '~/utils/ttsVoices'
import { concatFloat32, encodeWav } from '~/utils/audioWav'
import { buildAudioKey, createIdbBackend, createSliceAudioCache } from '~/utils/sliceAudioCache'
import type { SliceAudioCache } from '~/utils/sliceAudioCache'

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
}

export const AUDIO_SPEED_STORAGE_KEY = 'techdaily_reader_audio_speed'

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

  async function loadAndPlay(source: NarrationSource): Promise<void> {
    errorMessage.value = null
    device.value = null
    const engine = getEngine()
    const cache = getCache()
    if (!source.isAiFormatted || !source.markdown || !engine || !cache) return

    const voice = resolveVoiceForLanguage(source.language)
    const script = extractNarrationScript(source.markdown)
    if (!script) return

    const contentHash = await computeContentHash(script)
    const key = buildAudioKey(source.chunkId, voice.id, contentHash)
    activeKey = key

    // Cache hit → play the complete file, no synthesis.
    const cached = await cache.get(key)
    if (cached) {
      setSource(cached)
      status.value = 'ready'
      await play()
      return
    }

    // Cache miss → synthesize every sentence (surfacing synth progress),
    // assemble one complete seekable WAV, cache it, then play that single file.
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
    }
    catch (error) {
      status.value = 'error'
      errorMessage.value = error instanceof Error ? error.message : String(error)
      return
    }

    // A newer slice was requested while this one synthesized; drop the result.
    if (activeKey !== key) return

    if (buffers.length === 0) {
      status.value = 'error'
      return
    }

    // Assemble the whole slice into one seekable file and use it as the sole
    // playback source, so playback is continuous and the duration is accurate.
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
    // Drop the active request and reset status so a synthesis resolving after
    // teardown cannot start playback (see the activeKey guard in loadAndPlay).
    activeKey = null
    status.value = 'idle'
    device.value = null
  }

  // Release the worker, model, and playback when the owning scope (the reader
  // component) unmounts. Guarded so direct use outside a component scope does
  // not emit a Vue "no active scope" warning in tests.
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
    loadAndPlay,
    play,
    pause,
    toggle,
    setSpeed,
    seek,
    dispose,
  }
}

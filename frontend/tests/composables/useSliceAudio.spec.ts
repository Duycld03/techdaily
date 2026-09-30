import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { nextTick } from 'vue'
import { buildAudioKey } from '~/utils/sliceAudioCache'
import { computeContentHash, extractNarrationScript } from '~/utils/narrationScript'
import { resolveVoiceForLanguage } from '~/utils/ttsVoices'
import {
  AUDIO_SPEED_STORAGE_KEY,
  AUDIO_ENGINE_STORAGE_KEY,
  useSliceAudio,
  categorizeAudioError,
  type SynthHandlers,
  type TtsEngine,
  type NarrationSource,
} from '~/composables/useSliceAudio'
import type { PartialSliceAudio, SliceAudioCache } from '~/utils/sliceAudioCache'
import type { AudioQuotaInfo } from '~/types/audio'
interface MockAudio {
  src: string
  currentTime: number
  duration: number
  paused: boolean
  ended: boolean
  readyState: number
  playbackRate: number
  addEventListener: (type: string, cb: () => void, options?: any) => void
  play: () => Promise<void>
  pause: () => void
  _fire: (type: string) => void
}

function createFakeAudio(): MockAudio {
  const listeners: Record<string, Array<() => void>> = {}
  const fire = (type: string) => (listeners[type] || []).forEach(cb => cb())
  const state = {
    src: '',
    currentTime: 0,
    duration: 0,
    paused: true,
    ended: false,
    readyState: 1,
    playbackRate: 1,
    addEventListener(type: string, cb: () => void) {
      (listeners[type] ||= []).push(cb)
    },
    play: vi.fn(async () => {
      state.paused = false
      state.ended = false
      fire('play')
    }),
    pause: vi.fn(() => {
      state.paused = true
      fire('pause')
    }),
    _fire: fire,
  }
  return state as unknown as MockAudio
}

// A streaming engine whose `synthesize` mock is exposed so tests can assert the
// chosen voice model and call count. It emits a download-progress event and one
// audio chunk per sentence.
function streamingEngine() {
  const synthesize = vi.fn(async (_model: string, sentences: string[], handlers: SynthHandlers) => {
    handlers.onProgress({ stage: 'download', progress: 42 })
    for (let i = 0; i < sentences.length; i++) {
      handlers.onChunk(new Float32Array([0.2, -0.2, 0.1]), 16000)
    }
  })
  const engine = { synthesize, cancel: vi.fn(), dispose: vi.fn() }
  return engine
}

function memoryCache() {
  const store = new Map<string, Blob>()
  const partialStore = new Map<string, PartialSliceAudio>()
  const cache: SliceAudioCache = {
    get: vi.fn((key: string) => Promise.resolve(store.get(key))),
    set: vi.fn((key: string, blob: Blob) => {
      store.set(key, blob)
      return Promise.resolve()
    }),
    getPartial: vi.fn((key: string) => Promise.resolve(partialStore.get(key))),
    savePartial: vi.fn((key: string, data: PartialSliceAudio) => {
      partialStore.set(key, data)
      return Promise.resolve()
    }),
    deletePartial: vi.fn((key: string) => {
      partialStore.delete(key)
      return Promise.resolve()
    }),
  }
  return { store, partialStore, cache }
}

const MARKDOWN = '# Heading\n\nFirst sentence. Second sentence.\n\n```ts\nconst x = 1\n```'

function source(overrides: Partial<NarrationSource> = {}): NarrationSource {
  return {
    chunkId: 'chunk-1',
    markdown: MARKDOWN,
    language: 'en',
    isAiFormatted: true,
    ...overrides
  }
}

async function expectedKey(src: NarrationSource): Promise<string> {
  const hash = await computeContentHash(extractNarrationScript(src.markdown))
  return buildAudioKey(src.chunkId, resolveVoiceForLanguage(src.language).id, hash)
}

const createObjectUrl = vi.fn((_blob: Blob) => 'blob:mock')

describe('useSliceAudio', () => {
  beforeEach(() => {
    localStorage.clear()
    createObjectUrl.mockClear()
    URL.createObjectURL = createObjectUrl
    URL.revokeObjectURL = () => {}
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('synthesizes the whole slice into one cached, seekable file and plays it', async () => {
    const engine = streamingEngine()
    const { cache, store } = memoryCache()
    const audio = createFakeAudio()
    const src = source()

    const player = useSliceAudio({ engine, cache, createAudio: () => audio })
    await player.loadAndPlay(src)

    expect(engine.synthesize).toHaveBeenCalledTimes(1)
    expect(engine.synthesize.mock.calls[0]![1]).toEqual(
      expect.arrayContaining(['First sentence.', 'Second sentence.'])
    )
    // Progressive streaming: chunk 0 is loaded and played immediately, then
    // cutover to the complete assembled file occurs once all sentences resolve.
    expect(createObjectUrl).toHaveBeenCalledTimes(2)
    expect(audio.play).toHaveBeenCalledTimes(2)
    expect(player.status.value).toBe('ready')

    const key = await expectedKey(src)
    expect(cache.set).toHaveBeenCalledTimes(1)
    expect(cache.set.mock.calls[0]![0]).toBe(key)
    expect(store.get(key)?.type).toBe('audio/wav')
  })

  it('plays cached audio without re-synthesizing on a cache hit', async () => {
    const engine = streamingEngine()
    const { cache, store } = memoryCache()
    const audio = createFakeAudio()
    const src = source()
    store.set(await expectedKey(src), new Blob(['pre'], { type: 'audio/wav' }))

    const player = useSliceAudio({ engine, cache, createAudio: () => audio })
    await player.loadAndPlay(src)

    expect(engine.synthesize).not.toHaveBeenCalled()
    expect(cache.set).not.toHaveBeenCalled()
    expect(audio.play).toHaveBeenCalled()
    expect(player.status.value).toBe('ready')
  })

  it('re-synthesizes under a fresh key when the slice content changes', async () => {
    const engine = streamingEngine()
    const { cache, store } = memoryCache()
    const player = useSliceAudio({ engine, cache, createAudio: createFakeAudio })

    const first = source({ markdown: 'Original sentence one. Original sentence two.' })
    const second = source({ markdown: 'Rewritten sentence one. Rewritten sentence two.' })

    await player.loadAndPlay(first)
    await player.loadAndPlay(second)

    expect(engine.cancel).toHaveBeenCalled()
    expect(engine.synthesize).toHaveBeenCalledTimes(2)
    expect(store.has(await expectedKey(first))).toBe(true)
    expect(store.has(await expectedKey(second))).toBe(true)
  })

  it('unconditionally cancels any in-flight worker synthesis when loadAndPlay is called', async () => {
    const engine = streamingEngine()
    const { cache } = memoryCache()
    const player = useSliceAudio({ engine, cache, createAudio: createFakeAudio })

    const chunkA = source({ markdown: 'Sentence A. Sentence B.' })
    await player.loadAndPlay(chunkA)
    expect(engine.cancel).toHaveBeenCalled()

    engine.cancel.mockClear()
    const chunkB = source({ markdown: 'Sentence C. Sentence D.' })
    await player.loadAndPlay(chunkB)
    expect(engine.cancel).toHaveBeenCalled()
  })

  it('tracks per-sentence synth progress and stores the raw download percentage', async () => {
    const engine = streamingEngine()
    const { cache } = memoryCache()
    const player = useSliceAudio({ engine, cache, createAudio: createFakeAudio })

    await player.loadAndPlay(source({ markdown: 'First sentence. Second sentence.' }))

    // Two prose sentences → two synth chunks.
    expect(player.synthTotal.value).toBe(2)
    expect(player.synthIndex.value).toBe(2)
    // Download percentage is stored on its native 0-100 scale, not re-scaled.
    expect(player.downloadProgress.value).toBe(42)
  })

  it('selects the on-device voice model from the slice language', async () => {
    const viEngine = streamingEngine()
    const viCache = memoryCache()
    const viPlayer = useSliceAudio({ engine: viEngine, cache: viCache.cache, createAudio: createFakeAudio })
    await viPlayer.loadAndPlay(source({ language: 'vi' }))
    expect(viEngine.synthesize.mock.calls[0]![0]).toBe('Xenova/mms-tts-vie')

    const enEngine = streamingEngine()
    const enCache = memoryCache()
    const enPlayer = useSliceAudio({ engine: enEngine, cache: enCache.cache, createAudio: createFakeAudio })
    await enPlayer.loadAndPlay(source({ language: undefined }))
    expect(enEngine.synthesize.mock.calls[0]![0]).toBe('Xenova/mms-tts-eng')
  })

  it('exposes the compute device the engine resolved and clears it on cache-hit playback', async () => {
    const reportingEngine = (device: 'webgpu' | 'wasm'): TtsEngine => ({
      synthesize: vi.fn(async (_model: string, sentences: string[], handlers: SynthHandlers) => {
        handlers.onDevice?.(device)
        for (let i = 0; i < sentences.length; i++) handlers.onChunk(new Float32Array([0.1]), 16000)
      }),
      dispose: vi.fn()
    })

    // A GPU-backed run surfaces the WebGPU backend to the reader.
    const gpu = useSliceAudio({ engine: reportingEngine('webgpu'), cache: memoryCache().cache, createAudio: createFakeAudio })
    await gpu.loadAndPlay(source())
    expect(gpu.device.value).toBe('webgpu')

    // A CPU-only run surfaces the WASM backend (the many-core CPU fallback case).
    const shared = memoryCache()
    const cpu = useSliceAudio({ engine: reportingEngine('wasm'), cache: shared.cache, createAudio: createFakeAudio })
    await cpu.loadAndPlay(source())
    expect(cpu.device.value).toBe('wasm')

    // Cache-hit playback runs no synthesis, so the indicator must not report a
    // stale backend carried over from a prior run.
    const cached = useSliceAudio({ engine: reportingEngine('webgpu'), cache: shared.cache, createAudio: createFakeAudio })
    await cached.loadAndPlay(source())
    expect(cached.device.value).toBeNull()
  })

  it('does nothing for slices that are not AI-formatted', async () => {
    const engine = streamingEngine()
    const { cache } = memoryCache()
    const player = useSliceAudio({ engine, cache, createAudio: createFakeAudio })

    await player.loadAndPlay(source({ isAiFormatted: false }))

    expect(engine.synthesize).not.toHaveBeenCalled()
    expect(player.status.value).toBe('idle')
  })

  it('surfaces synthesis failures as an error status and message', async () => {
    const engine: TtsEngine = {
      synthesize: vi.fn(() => Promise.reject(new Error('model load failed'))),
      dispose: vi.fn()
    }
    const { cache } = memoryCache()
    const player = useSliceAudio({ engine, cache, createAudio: createFakeAudio })

    await player.loadAndPlay(source())

    expect(player.status.value).toBe('error')
    expect(player.errorMessage.value).toBe('model load failed')
    expect(player.errorInfo.value).toEqual({
      code: 'DEVICE_INIT_FAILED',
      rawMessage: 'model load failed',
      suggestCloudFallback: true,
    })
    expect(cache.set).not.toHaveBeenCalled()
  })

  it('categorizes out of memory errors and suggests cloud fallback on device', async () => {
    const engine: TtsEngine = {
      synthesize: vi.fn(() => Promise.reject(new Error('RangeError: WebAssembly.Memory(): could not allocate memory'))),
      dispose: vi.fn(),
    }
    const { cache } = memoryCache()
    const player = useSliceAudio({ engine, cache, createAudio: createFakeAudio })

    await player.loadAndPlay(source())

    expect(player.status.value).toBe('error')
    expect(player.errorInfo.value?.code).toBe('DEVICE_OOM')
    expect(player.errorInfo.value?.suggestCloudFallback).toBe(true)

    // Switching engine mode clears error state
    player.setEngineMode('cloud')
    expect(player.errorMessage.value).toBeNull()
    expect(player.errorInfo.value).toBeNull()
  })

  it('persists playback speed and applies it to the audio element', async () => {
    const engine = streamingEngine()
    const { cache } = memoryCache()
    const audio = createFakeAudio()
    const player = useSliceAudio({ engine, cache, createAudio: () => audio })

    await player.loadAndPlay(source())
    player.setSpeed(1.5)
    await nextTick()

    expect(player.speed.value).toBe(1.5)
    expect(audio.playbackRate).toBe(1.5)
    expect(localStorage.getItem(AUDIO_SPEED_STORAGE_KEY)).toContain('1.5')
  })

  it('cancels playback on dispose so a late synthesis completion does not play', async () => {
    // Engine whose synthesize stays pending until released, so we can leave the
    // reader mid-synthesis and then let the synthesis resolve afterwards.
    let release!: () => void
    const gate = new Promise<void>((resolve) => { release = resolve })
    const synthesize = vi.fn(async (_model: string, sentences: string[], handlers: SynthHandlers) => {
      await gate
      for (let i = 0; i < sentences.length; i++) {
        handlers.onChunk(new Float32Array([0.1, -0.1]), 16000)
      }
    })
    const engine: TtsEngine = { synthesize, dispose: vi.fn() }
    const { cache } = memoryCache()
    const audio = createFakeAudio()
    const player = useSliceAudio({ engine, cache, createAudio: () => audio })

    const playback = player.loadAndPlay(source())
    await vi.waitFor(() => expect(synthesize).toHaveBeenCalledTimes(1))

    // Leaving the reader disposes the composable mid-synthesis.
    player.dispose()
    expect(engine.dispose).toHaveBeenCalledTimes(1)
    expect(player.status.value).toBe('idle')

    // The in-flight synthesis now completes; it must not start playback.
    release()
    await playback

    expect(audio.play).not.toHaveBeenCalled()
    expect(createObjectUrl).not.toHaveBeenCalled()
  })

  it('dispatches to Cloud mode via POST endpoint and caches in IndexedDB', async () => {
    const { cache, store } = memoryCache()
    const audio = createFakeAudio()
    const mockBlob = new Blob(['mock-mp3-bytes'], { type: 'audio/mpeg' })

    const fetchClient = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      return new Response(mockBlob, {
        status: 200,
        headers: { 'Content-Type': 'audio/mpeg' }
      })
    })

    const player = useSliceAudio({
      defaultEngine: 'cloud',
      cache,
      createAudio: () => audio,
      fetchClient
    })

    const src = source({ chunkId: 'chunk-cloud-1' })
    await player.loadAndPlay(src)

    expect(fetchClient).toHaveBeenCalledTimes(1)
    expect(fetchClient.mock.calls[0]![0]).toBe('/api/v1/library/chunks/chunk-cloud-1/audio')
    expect(fetchClient.mock.calls[0]![1]?.method).toBe('POST')

    expect(cache.set).toHaveBeenCalledTimes(1)
    expect(cache.set.mock.calls[0]![0]).toContain('chunk-cloud-1')
    expect(player.status.value).toBe('ready')
    expect(audio.play).toHaveBeenCalledTimes(1)
  })

  it('enforces that English slices never use Vietnamese voices even if previously saved in localStorage', async () => {
    localStorage.setItem('techdaily_reader_audio_voice', 'vi-VN-Neural2-A')
    localStorage.setItem('techdaily_reader_audio_voice_vi', 'vi-VN-Neural2-A')

    const { cache } = memoryCache()
    const audio = createFakeAudio()
    const mockBlob = new Blob(['mock-mp3-bytes'], { type: 'audio/mpeg' })

    const fetchClient = vi.fn(async (_input: RequestInfo | URL, _init?: RequestInit) => {
      return new Response(mockBlob, {
        status: 200,
        headers: { 'Content-Type': 'audio/mpeg' }
      })
    })

    const player = useSliceAudio({
      defaultEngine: 'cloud',
      cache,
      createAudio: () => audio,
      fetchClient
    })

    const src = source({ chunkId: 'chunk-en-1', language: 'en' })
    await player.loadAndPlay(src)

    expect(fetchClient).toHaveBeenCalledTimes(1)
    const requestBody = JSON.parse(fetchClient.mock.calls[0]![1]?.body as string)
    expect(requestBody.voiceId).toBe('en-US-Neural2-F')
    expect(requestBody.voiceId).not.toContain('vi-VN')
  })

  it('selects Vietnamese voice when source language is Vietnamese', async () => {
    const { cache } = memoryCache()
    const audio = createFakeAudio()
    const mockBlob = new Blob(['mock-mp3-bytes'], { type: 'audio/mpeg' })

    const fetchClient = vi.fn(async (_input: RequestInfo | URL, _init?: RequestInit) => {
      return new Response(mockBlob, {
        status: 200,
        headers: { 'Content-Type': 'audio/mpeg' }
      })
    })

    const player = useSliceAudio({
      defaultEngine: 'cloud',
      cache,
      createAudio: () => audio,
      fetchClient
    })

    const src = source({ chunkId: 'chunk-vi-1', language: 'vi' })
    await player.loadAndPlay(src)

    expect(fetchClient).toHaveBeenCalledTimes(1)
    const requestBody = JSON.parse(fetchClient.mock.calls[0]![1]?.body as string)
    expect(requestBody.voiceId).toBe('vi-VN-Neural2-A')
  })

  it('dispatches to Device mode via Web Worker and caches in IndexedDB', async () => {
    const engine = streamingEngine()
    const { cache } = memoryCache()
    const audio = createFakeAudio()

    const player = useSliceAudio({
      defaultEngine: 'device',
      engine,
      cache,
      createAudio: () => audio
    })

    const src = source({ chunkId: 'chunk-device-1' })
    await player.loadAndPlay(src)

    expect(engine.synthesize).toHaveBeenCalledTimes(1)
    expect(cache.set).toHaveBeenCalledTimes(1)
    expect(cache.set.mock.calls[0]![0]).toContain('chunk-device-1')
    expect(player.status.value).toBe('ready')
    expect(audio.play).toHaveBeenCalled()
  })

  it('handles 429 AudioQuotaExhausted in Cloud mode by falling back to Device mode and synthesizing on-device', async () => {
    const engine = streamingEngine()
    const { cache } = memoryCache()
    const audio = createFakeAudio()
    const onQuotaExhausted = vi.fn()

    const fetchClient = vi.fn(async () => {
      return new Response(JSON.stringify({ code: 'AudioQuotaExhausted' }), {
        status: 429,
        headers: { 'Content-Type': 'application/problem+json' }
      })
    })

    const player = useSliceAudio({
      defaultEngine: 'cloud',
      engine,
      cache,
      createAudio: () => audio,
      fetchClient,
      onQuotaExhausted
    })

    const src = source({ chunkId: 'chunk-quota-fallback-1' })
    await player.loadAndPlay(src)

    expect(fetchClient).toHaveBeenCalledTimes(1)
    expect(onQuotaExhausted).toHaveBeenCalledTimes(1)
    expect(player.engineMode.value).toBe('device')
    expect(engine.synthesize).toHaveBeenCalledTimes(1)
    expect(player.status.value).toBe('ready')
    expect(audio.play).toHaveBeenCalled()
  })

  it('fetches quota and flags near-limit and exhausted states', async () => {
    const fetchClient = vi.fn(async () => {
      return new Response(JSON.stringify({
        monthlyLimit: 950000,
        usedCharacters: 920000,
        remainingCharacters: 30000,
        isNearLimit: true,
        isExhausted: false
      }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    })

    const player = useSliceAudio({
      defaultEngine: 'cloud',
      fetchClient
    })

    const quota = await player.fetchQuota()
    expect(quota).not.toBeNull()
    expect(player.isNearQuota.value).toBe(true)
    expect(player.isQuotaExhausted.value).toBe(false)
    expect(player.engineMode.value).toBe('device')
  })

  it('resolves relative URLs in production environments when no custom fetchClient is provided', async () => {
    const originalFetch = globalThis.fetch
    const fetchSpy = vi.fn(async () => {
      return new Response(JSON.stringify({
        monthlyLimit: 950000,
        usedCharacters: 100000,
        remainingCharacters: 850000,
        isExhausted: false,
        isNearLimit: false
      }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    })
    globalThis.fetch = fetchSpy
    const originalRuntimeConfig = (globalThis as any).useRuntimeConfig
    ;(globalThis as any).useRuntimeConfig = () => ({
      public: {
        apiBaseUrl: '',
        googleClientId: 'mock-google-client-id'
      }
    })

    const originalLocation = window.location
    try {
      Object.defineProperty(window, 'location', {
        value: {
          protocol: 'https:',
          hostname: 'techdaily.duckdns.org',
          port: ''
        },
        writable: true,
        configurable: true
      })

      const player = useSliceAudio({
        defaultEngine: 'cloud'
      })

      await player.fetchQuota()
      expect(fetchSpy).toHaveBeenCalledTimes(1)
      const calledUrl = fetchSpy.mock.calls[0]![0] as string
      expect(calledUrl).toBe('/api/v1/library/audio/quota')
      expect(calledUrl).not.toContain('localhost:5000')
    } finally {
      ;(globalThis as any).useRuntimeConfig = originalRuntimeConfig
      globalThis.fetch = originalFetch
      Object.defineProperty(window, 'location', {
        value: originalLocation,
        writable: true,
        configurable: true
      })
    }
  })

  it('buffers all sentences before playback begins for slices with 1 or 2 sentences', async () => {
    const { promise: p1, resolve: resolveChunk1 } = Promise.withResolvers<void>()
    const delayedEngine: TtsEngine = {
      synthesize: vi.fn(async (_model: string, _sentences: string[], handlers: SynthHandlers) => {
        handlers.onChunk(new Float32Array([0.1, -0.1]), 16000)
        await p1
        handlers.onChunk(new Float32Array([0.2, -0.2]), 16000)
      }),
      dispose: vi.fn()
    }

    const { cache } = memoryCache()
    const audio = createFakeAudio()
    const player = useSliceAudio({
      defaultEngine: 'device',
      engine: delayedEngine,
      cache,
      createAudio: () => audio
    })

    const loadPromise = player.loadAndPlay(source({ markdown: 'Sentence 1. Sentence 2.' }))

    await vi.waitFor(() => {
      expect(player.targetBufferCount.value).toBe(2)
    })

    // Only 1 of 2 chunks arrived -> still buffering!
    expect(player.status.value).toBe('loading')
    expect(audio.play).not.toHaveBeenCalled()
    expect(player.synthIndex.value).toBe(1)
    resolveChunk1()
    await loadPromise

    expect(player.status.value).toBe('ready')
    expect(audio.play).toHaveBeenCalled()
    expect(player.synthIndex.value).toBe(2)
    expect(cache.set).toHaveBeenCalledTimes(1)
  })

  it('buffers 1 sentence for single-sentence slices', async () => {
    const delayedEngine: TtsEngine = {
      synthesize: vi.fn(async (_model: string, _sentences: string[], handlers: SynthHandlers) => {
        handlers.onChunk(new Float32Array([0.1, -0.1]), 16000)
      }),
      dispose: vi.fn()
    }

    const { cache } = memoryCache()
    const audio = createFakeAudio()
    const player = useSliceAudio({
      defaultEngine: 'device',
      engine: delayedEngine,
      cache,
      createAudio: () => audio
    })

    await player.loadAndPlay(source({ markdown: 'Single sentence only.' }))
    expect(player.targetBufferCount.value).toBe(1)
    expect(player.status.value).toBe('ready')
    expect(audio.play).toHaveBeenCalled()
  })

  it('caps buffer threshold at 2 sentences for longer slices and chains sequential chunks', async () => {
    const listeners: Record<string, Array<() => void>> = {}
    const fire = (type: string) => (listeners[type] || []).forEach(cb => cb())
    const audioState = {
      src: '',
      currentTime: 0,
      duration: 0,
      paused: true,
      playbackRate: 1,
      addEventListener(type: string, cb: () => void) {
        (listeners[type] ||= []).push(cb)
      },
      play: vi.fn(async () => {
        audioState.paused = false
        fire('play')
      }),
      pause: vi.fn(() => {
        audioState.paused = true
        fire('pause')
      })
    }

    const { promise, resolve: resolveChunk3 } = Promise.withResolvers<void>()
    const delayedEngine: TtsEngine = {
      synthesize: vi.fn(async (_model: string, _sentences: string[], handlers: SynthHandlers) => {
        // Emit chunk 0 and chunk 1 (2 sentences = ceil(6 / 3) = 2)
        handlers.onChunk(new Float32Array([0.1, -0.1]), 16000)
        handlers.onChunk(new Float32Array([0.2, -0.2]), 16000)
        await promise
        for (let i = 2; i < 6; i++) {
          handlers.onChunk(new Float32Array([0.1 * (i + 1), -0.1 * (i + 1)]), 16000)
        }
      }),
      dispose: vi.fn()
    }

    const { cache } = memoryCache()
    const player = useSliceAudio({
      defaultEngine: 'device',
      engine: delayedEngine,
      cache,
      createAudio: () => audioState as unknown as MockAudio
    })

    // 6-sentence slice -> targetBufferCount = ceil(6 / 3) = 2
    const longMarkdown = 'Sentence 1. Sentence 2. Sentence 3. Sentence 4. Sentence 5. Sentence 6.'
    const loadPromise = player.loadAndPlay(source({ markdown: longMarkdown }))

    await vi.waitFor(() => {
      expect(player.targetBufferCount.value).toBe(2)
      expect(player.status.value).toBe('ready')
    })
    expect(audioState.play).toHaveBeenCalledTimes(1)

    // Simulate pre-roll audio ending while chunk 2 arrives
    resolveChunk3()
    await nextTick()
    await new Promise(r => setTimeout(r, 20))
    fire('ended')
    await nextTick()

    // Advances to next chunk
    expect(audioState.play).toHaveBeenCalledTimes(2)

    await loadPromise
    expect(cache.set).toHaveBeenCalledTimes(1)
  })

  it('resumes on-device synthesis from partial cache and cleans up partial cache on completion', async () => {
    const { cache } = memoryCache()
    const audio = createFakeAudio()

    // Pre-populate partial cache with 2 sentences out of 4
    const initialMarkdown = 'First sentence. Second sentence. Third sentence. Fourth sentence.'
    const src = source({ chunkId: 'chunk-resumable-1', markdown: initialMarkdown })
    const script = extractNarrationScript(initialMarkdown)
    const key = await expectedKey(src)
    await cache.savePartial?.(key, {
      chunks: [new Float32Array([0.1, 0.1]), new Float32Array([0.2, 0.2])],
      sampleRate: 16000,
      total: 4,
    })

    const engine: TtsEngine = {
      synthesize: vi.fn(async (_model: string, sentences: string[], handlers: SynthHandlers) => {
        // Expect only remaining 2 sentences passed
        expect(sentences).toEqual(['Third sentence.', 'Fourth sentence.'])
        handlers.onChunk(new Float32Array([0.3, 0.3]), 16000)
        handlers.onChunk(new Float32Array([0.4, 0.4]), 16000)
      }),
      cancel: vi.fn(),
      dispose: vi.fn(),
    }

    const player = useSliceAudio({
      defaultEngine: 'device',
      engine,
      cache,
      createAudio: () => audio,
    })

    await player.loadAndPlay(src)

    expect(engine.synthesize).toHaveBeenCalledTimes(1)
    expect(cache.set).toHaveBeenCalledTimes(1)
    expect(cache.deletePartial).toHaveBeenCalledWith(key)
  })

  it('invalidates partial cache and synthesizes from sentence 0 when slice content changes', async () => {
    const { cache } = memoryCache()
    const audio = createFakeAudio()

    // Pre-populate partial cache under old content
    const oldMarkdown = 'Old content sentence one. Old content sentence two.'
    const oldScript = extractNarrationScript(oldMarkdown)
    const oldContentHash = await computeContentHash(oldScript)
    const oldKey = buildAudioKey('chunk-hash-test', 'mms-eng', oldContentHash)
    await cache.savePartial?.(oldKey, {
      chunks: [new Float32Array([0.1, 0.1])],
      sampleRate: 16000,
      total: 2,
    })

    const engine: TtsEngine = {
      synthesize: vi.fn(async (_model: string, sentences: string[], handlers: SynthHandlers) => {
        // New content -> all 3 sentences synthesized from start
        expect(sentences.length).toBe(3)
        for (let i = 0; i < sentences.length; i++) {
          handlers.onChunk(new Float32Array([0.1 * (i + 1)]), 16000)
        }
      }),
      cancel: vi.fn(),
      dispose: vi.fn(),
    }

    const player = useSliceAudio({
      defaultEngine: 'device',
      engine,
      cache,
      createAudio: () => audio,
    })

    const newMarkdown = 'New content sentence one. New content sentence two. New content sentence three.'
    await player.loadAndPlay(source({ chunkId: 'chunk-hash-test', markdown: newMarkdown }))

    expect(engine.synthesize).toHaveBeenCalledTimes(1)
    expect(cache.set).toHaveBeenCalledTimes(1)
  })

  it('cancels active on-device synthesis and resets progress counters when switching to Cloud engine', () => {
    const cancelMock = vi.fn()
    const engine: TtsEngine = {
      synthesize: vi.fn(),
      cancel: cancelMock,
      dispose: vi.fn(),
    }
    const { cache } = memoryCache()
    const audio = createFakeAudio()

    const player = useSliceAudio({
      defaultEngine: 'device',
      engine,
      cache,
      createAudio: () => audio,
    })

    player.synthIndex.value = 5
    player.synthTotal.value = 15
    player.targetBufferCount.value = 5

    player.setEngineMode('cloud')

    expect(cancelMock).toHaveBeenCalledTimes(1)
    expect(player.synthIndex.value).toBe(0)
    expect(player.synthTotal.value).toBe(0)
    expect(player.targetBufferCount.value).toBe(0)
    expect(player.engineMode.value).toBe('cloud')
  })

  it('restarts playback from 0:00 when play is invoked on ended audio', async () => {
    const audio = createFakeAudio()
    const engine = streamingEngine()
    const { cache } = memoryCache()

    const player = useSliceAudio({
      defaultEngine: 'device',
      engine,
      cache,
      createAudio: () => audio,
    })

    await player.loadAndPlay(source())
    expect(player.status.value).toBe('ready')

    // Simulate audio ended
    audio.ended = true
    audio.currentTime = 42
    audio.duration = 42

    // Calling play again should reset currentTime to 0
    await player.play()
    expect(audio.currentTime).toBe(0)
    expect(audio.play).toHaveBeenCalled()
  })

  it('restores 5 of 15 sentences from partial cache, plays all 5 sentences immediately, and synthesizes remaining 10 sentences', async () => {
    const { cache } = memoryCache()
    const audio = createFakeAudio()

    // 15 sentences markdown
    const fifteenSentences = Array.from({ length: 15 }, (_, i) => `Sentence ${i + 1}.`).join(' ')
    const src = source({ chunkId: 'chunk-15-sentences', markdown: fifteenSentences })
    const script = extractNarrationScript(fifteenSentences)
    const contentHash = await computeContentHash(script)
    const key = await expectedKey(src)
    // Pre-populate partial cache with 5 chunks out of 15
    const partialChunks = Array.from({ length: 5 }, (_, i) => new Float32Array([0.1 * (i + 1), 0.1 * (i + 1)]))
    await cache.savePartial?.(key, {
      chunks: partialChunks,
      sampleRate: 16000,
      total: 15,
    })

    const synthesizedSentences: string[][] = []
    const engine: TtsEngine = {
      synthesize: vi.fn(async (_model: string, sentences: string[], handlers: SynthHandlers) => {
        synthesizedSentences.push(sentences)
        // Emit the remaining 10 sentences
        for (let i = 0; i < sentences.length; i++) {
          handlers.onChunk(new Float32Array([0.5 + 0.05 * i]), 16000)
        }
      }),
      cancel: vi.fn(),
      dispose: vi.fn(),
    }

    const player = useSliceAudio({
      defaultEngine: 'device',
      engine,
      cache,
      createAudio: () => audio,
    })

    await player.loadAndPlay(src)

    // Verify only the remaining 10 sentences were passed to engine.synthesize
    expect(synthesizedSentences).toHaveLength(1)
    expect(synthesizedSentences[0]).toHaveLength(10)
    expect(synthesizedSentences[0]![0]).toBe('Sentence 6.')
    expect(synthesizedSentences[0]![9]).toBe('Sentence 15.')

    // Verify pre-roll played all 5 restored chunks
    expect(player.targetBufferCount.value).toBe(5)
    expect(player.synthTotal.value).toBe(15)
    expect(player.synthIndex.value).toBe(15)
    expect(cache.set).toHaveBeenCalledTimes(1)
    expect(cache.deletePartial).toHaveBeenCalledWith(key)
  })

  it('preserves playback offset via loadedmetadata when transitioning to complete assembled file', async () => {
    const audio = createFakeAudio()
    // Start with readyState 0 (metadata not yet loaded)
    audio.readyState = 0

    const engine = streamingEngine()
    const { cache } = memoryCache()

    const player = useSliceAudio({
      defaultEngine: 'device',
      engine,
      cache,
      createAudio: () => audio,
    })

    // Simulate playback reaching 12.5 seconds
    audio.currentTime = 12.5
    await player.loadAndPlay(source())

    // Trigger loadedmetadata event
    audio._fire('loadedmetadata')

    // Offset should be preserved on the audio element
    expect(audio.currentTime).toBe(12.5)
  })

  it('handles stream underrun by entering buffering status and resumes playback when next chunk arrives', async () => {
    const audio = createFakeAudio()
    const { cache } = memoryCache()

    const { promise, resolve: resolveSynthesis } = Promise.withResolvers<void>()
    let emitChunk3: () => void = () => {}
    const engine: TtsEngine = {
      synthesize: vi.fn(async (_model: string, _sentences: string[], handlers: SynthHandlers) => {
        // Emit chunks 0 and 1 (pre-roll)
        handlers.onChunk(new Float32Array([0.1, 0.1]), 16000)
        handlers.onChunk(new Float32Array([0.2, 0.2]), 16000)
        // Store callback to emit chunk 2 later
        emitChunk3 = () => {
          handlers.onChunk(new Float32Array([0.3, 0.3]), 16000)
          resolveSynthesis()
        }
        await promise
      }),
      cancel: vi.fn(),
      dispose: vi.fn(),
    }

    const player = useSliceAudio({
      defaultEngine: 'device',
      engine,
      cache,
      createAudio: () => audio,
    })

    const markdown = 'Sentence one. Sentence two. Sentence three.'
    const loadPromise = player.loadAndPlay(source({ markdown }))
    await vi.waitFor(() => {
      expect(player.status.value).toBe('ready')
    })
    expect(audio.play).toHaveBeenCalledTimes(1)
    // Simulate pre-roll audio ending before chunk 3 has arrived (stream underrun)
    audio._fire('ended')
    expect(player.status.value).toBe('loading') // buffering state
    expect(player.playing.value).toBe(true) // still in playing state

    // Now chunk 3 arrives from the worker
    emitChunk3()
    await nextTick()

    // Player should automatically resume playing chunk 3
    expect(player.status.value).toBe('ready')
    expect(audio.play).toHaveBeenCalledTimes(2)

    await loadPromise
  })

  it('prepares audio source and metadata without playing when autoPlay is false', async () => {
    const audio = createFakeAudio()
    const engine = streamingEngine()
    const { cache } = memoryCache()

    const player = useSliceAudio({
      defaultEngine: 'device',
      engine,
      cache,
      createAudio: () => audio,
    })

    await player.loadAndPlay(source(), false)

    expect(player.status.value).toBe('ready')
    expect(audio.play).not.toHaveBeenCalled()
  })

  it('does not save to full cache when synthesis is cancelled and preserves partial cache', async () => {
    const audio = createFakeAudio()
    const { cache } = memoryCache()

    const { promise, reject } = Promise.withResolvers<void>()
    promise.catch(() => {})
    const cancellingEngine: TtsEngine = {
      synthesize: vi.fn(async (_model: string, _sentences: string[], handlers: SynthHandlers) => {
        handlers.onChunk(new Float32Array([0.1, 0.1]), 16000)
        await promise
      }),
      cancel: vi.fn(() => {
        reject(new Error('Synthesis cancelled'))
      }),
      dispose: vi.fn()
    }

    const player = useSliceAudio({
      defaultEngine: 'device',
      engine: cancellingEngine,
      cache,
      createAudio: () => audio,
    })

    const src = source({ markdown: 'Sentence 1. Sentence 2. Sentence 3.' })
    const loadPromise = player.loadAndPlay(src)
    await nextTick()

    cancellingEngine.cancel?.()
    await loadPromise

    expect(cache.set).not.toHaveBeenCalled()
    expect(cache.savePartial).toHaveBeenCalled()
    expect(cache.deletePartial).not.toHaveBeenCalled()
  })
  it('does not auto-play when a new chunk arrives if the user has paused', async () => {
    const audio = createFakeAudio()
    let emitChunk3: (() => void) | null = null
    const asyncEngine: TtsEngine = {
      synthesize: vi.fn(async (_model: string, _sentences: string[], handlers: SynthHandlers) => {
        handlers.onChunk(new Float32Array([0.1, 0.1]), 16000)
        handlers.onChunk(new Float32Array([0.2, 0.2]), 16000)
        await new Promise<void>((resolve) => {
          emitChunk3 = () => {
            handlers.onChunk(new Float32Array([0.3, 0.3]), 16000)
            resolve()
          }
        })
      }),
      cancel: vi.fn(),
      dispose: vi.fn()
    }

    const { cache } = memoryCache()
    const player = useSliceAudio({
      defaultEngine: 'device',
      engine: asyncEngine,
      cache,
      createAudio: () => audio
    })
    const src = source({ markdown: 'Sentence 1. Sentence 2. Sentence 3.' })
    const loadPromise = player.loadAndPlay(src)
    await vi.waitFor(() => {
      expect(audio.play).toHaveBeenCalledTimes(1)
      expect(player.playing.value).toBe(true)
    })
    // User explicitly clicks pause
    player.pause()
    expect(audio.pause).toHaveBeenCalled()
    expect(player.playing.value).toBe(false)

    // Worker finishes chunk 3 while user is paused
    emitChunk3?.()
    await nextTick()
    await loadPromise

    // Audio MUST NOT auto-play chunk 3! Play call count remains 1!
    expect(audio.play).toHaveBeenCalledTimes(1)
    expect(player.playing.value).toBe(false)
  })
  describe('categorizeAudioError and device error diagnostics', () => {
    it('categorizes WebKit/Safari "Load failed" as NETWORK_ERROR with cloud fallback', () => {
      const error = new Error('TypeError: Load failed')
      const info = categorizeAudioError(error, 'device')
      expect(info.code).toBe('NETWORK_ERROR')
      expect(info.suggestCloudFallback).toBe(true)
      expect(info.rawMessage).toContain('Load failed')
    })

    it('categorizes connection timeouts and fetch errors as NETWORK_ERROR', () => {
      const fetchError = new Error('Failed to fetch model weights')
      const timeoutError = new Error('Connection timeout while downloading ort-wasm')
      expect(categorizeAudioError(fetchError, 'device').code).toBe('NETWORK_ERROR')
      expect(categorizeAudioError(timeoutError, 'device').code).toBe('NETWORK_ERROR')
    })

    it('categorizes memory exhaustion as DEVICE_OOM', () => {
      const oomError = new Error('Out of memory: WebAssembly allocation failed')
      const info = categorizeAudioError(oomError, 'device')
      expect(info.code).toBe('DEVICE_OOM')
      expect(info.suggestCloudFallback).toBe(true)
    })

    it('categorizes unknown worker errors as DEVICE_INIT_FAILED with cloud fallback', () => {
      const genericError = new Error('Worker initialization failed')
      const info = categorizeAudioError(genericError, 'device')
      expect(info.code).toBe('DEVICE_INIT_FAILED')
      expect(info.suggestCloudFallback).toBe(true)
    })

    it('sets player error state and preserves raw message when device synthesis fails with network error', async () => {
      const audio = createFakeAudio()
      const { cache } = memoryCache()
      const failingEngine: TtsEngine = {
        synthesize: vi.fn(async () => {
          throw new Error('TypeError: Load failed')
        }),
        cancel: vi.fn(),
        dispose: vi.fn()
      }

      const player = useSliceAudio({
        defaultEngine: 'device',
        engine: failingEngine,
        cache,
        createAudio: () => audio
      })

      await player.loadAndPlay(source())
      expect(player.status.value).toBe('error')
      expect(player.errorInfo.value?.code).toBe('NETWORK_ERROR')
      expect(player.errorInfo.value?.suggestCloudFallback).toBe(true)
      expect(player.errorMessage.value).toBe('TypeError: Load failed')
    })
  })
})

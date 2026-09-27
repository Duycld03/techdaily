import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { nextTick } from 'vue'
import { buildAudioKey } from '~/utils/sliceAudioCache'
import { computeContentHash, extractNarrationScript } from '~/utils/narrationScript'
import { resolveVoiceForLanguage } from '~/utils/ttsVoices'
import {
  AUDIO_SPEED_STORAGE_KEY,
  AUDIO_ENGINE_STORAGE_KEY,
  useSliceAudio,
  type NarrationSource,
  type SynthHandlers,
  type TtsEngine
} from '~/composables/useSliceAudio'
type MockAudio = HTMLAudioElement & { play: Mock, pause: Mock }

function createFakeAudio(): MockAudio {
  const listeners: Record<string, Array<() => void>> = {}
  const fire = (type: string) => (listeners[type] || []).forEach(cb => cb())
  const state = {
    src: '',
    currentTime: 0,
    duration: 0,
    paused: true,
    playbackRate: 1,
    addEventListener(type: string, cb: () => void) {
      (listeners[type] ||= []).push(cb)
    },
    play: vi.fn(async () => {
      state.paused = false
      fire('play')
    }),
    pause: vi.fn(() => {
      state.paused = true
      fire('pause')
    })
  }
  // Boundary cast: the composable touches only this subset of HTMLAudioElement.
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
  const engine = { synthesize, dispose: vi.fn() }
  return engine
}

function memoryCache() {
  const store = new Map<string, Blob>()
  const cache = {
    get: vi.fn((key: string) => Promise.resolve(store.get(key))),
    set: vi.fn((key: string, blob: Blob) => {
      store.set(key, blob)
      return Promise.resolve()
    })
  }
  return { store, cache }
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

    expect(engine.synthesize).toHaveBeenCalledTimes(2)
    expect(store.has(await expectedKey(first))).toBe(true)
    expect(store.has(await expectedKey(second))).toBe(true)
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
    expect(cache.set).not.toHaveBeenCalled()
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
      globalThis.fetch = originalFetch
      Object.defineProperty(window, 'location', {
        value: originalLocation,
        writable: true,
        configurable: true
      })
    }
  })

  it('buffers 100% of sentences before playback begins for short slices (<= 3 sentences)', async () => {
    const { promise, resolve: resolveChunk2 } = Promise.withResolvers<void>()
    const delayedEngine: TtsEngine = {
      synthesize: vi.fn(async (_model: string, _sentences: string[], handlers: SynthHandlers) => {
        handlers.onChunk(new Float32Array([0.1, -0.1]), 16000)
        handlers.onChunk(new Float32Array([0.2, -0.2]), 16000)
        await promise
        handlers.onChunk(new Float32Array([0.3, -0.3]), 16000)
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

    // source() has 3 sentences -> targetBufferCount = 3
    const loadPromise = player.loadAndPlay(source())

    await nextTick()
    await new Promise(r => setTimeout(r, 10))

    // Only 2 of 3 chunks arrived -> still buffering!
    expect(player.status.value).toBe('loading')
    expect(audio.play).not.toHaveBeenCalled()
    expect(player.synthIndex.value).toBe(2)
    expect(player.targetBufferCount.value).toBe(3)

    // Deliver chunk 2 (third sentence) -> completes buffer threshold
    resolveChunk2()
    await loadPromise

    expect(player.status.value).toBe('ready')
    expect(audio.play).toHaveBeenCalled()
    expect(player.synthIndex.value).toBe(3)
    expect(cache.set).toHaveBeenCalledTimes(1)
  })

  it('buffers at least 33% of sentences before playback begins for longer slices and chains sequential chunks', async () => {
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
        handlers.onChunk(new Float32Array([0.3, -0.3]), 16000)
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

    await nextTick()
    await new Promise(r => setTimeout(r, 10))

    // Target buffer count is 2 (33% of 6)
    expect(player.targetBufferCount.value).toBe(2)
    expect(player.status.value).toBe('ready')
    expect(audioState.play).toHaveBeenCalledTimes(1)

    // Simulate pre-roll audio ending while chunk 2 arrives
    resolveChunk3()
    await new Promise(r => setTimeout(r, 10))

    fire('ended')
    await nextTick()

    // Advances to next chunk
    expect(audioState.play).toHaveBeenCalledTimes(2)

    await loadPromise
    expect(cache.set).toHaveBeenCalledTimes(1)
  })
})

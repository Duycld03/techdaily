import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { nextTick } from 'vue'
import { buildAudioKey } from '~/utils/sliceAudioCache'
import { computeContentHash, extractNarrationScript } from '~/utils/narrationScript'
import { resolveVoiceForLanguage } from '~/utils/ttsVoices'
import {
  AUDIO_SPEED_STORAGE_KEY,
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
    // Single complete-file source: exactly one object URL is created (no
    // per-sentence clip swap).
    expect(createObjectUrl).toHaveBeenCalledTimes(1)
    expect(audio.play).toHaveBeenCalledTimes(1)
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
})

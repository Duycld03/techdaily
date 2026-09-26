import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'

// Control the audio composable so we can drive loading/progress state and assert
// what the control renders and forwards - without running real synthesis.
vi.mock('~/composables/useSliceAudio', async () => {
  const { ref } = await import('vue')
  const state = {
    status: ref('idle'),
    playing: ref(false),
    currentTime: ref(0),
    duration: ref(0),
    downloadProgress: ref(0),
    synthIndex: ref(0),
    synthTotal: ref(0),
    errorMessage: ref(null),
    speed: ref(1),
    loadAndPlay: vi.fn(),
    play: vi.fn(),
    pause: vi.fn(),
    setSpeed: vi.fn(),
    seek: vi.fn()
  }
  return { useSliceAudio: () => state }
})

import ReaderAudioPlayer from '~/components/reader/ReaderAudioPlayer.vue'
import { useSliceAudio } from '~/composables/useSliceAudio'
import type { ChunkSummary } from '~/stores/useLibraryStore'

const MESSAGES: Record<string, string> = {
  'reader.audio_listen': 'Listen',
  'reader.audio_play': 'Play narration',
  'reader.audio_pause': 'Pause narration',
  'reader.audio_downloading': 'Downloading voice… {progress}%',
  'reader.audio_preparing': 'Preparing audio…',
  'reader.audio_synthesizing': 'Generating audio… {current}/{total}',
  'reader.audio_speed': 'Speed',
  'reader.audio_error': 'Could not generate audio.'
}

function interpolate(key: string, params?: Record<string, unknown>): string {
  let out = MESSAGES[key] ?? key
  if (params) {
    for (const [k, v] of Object.entries(params)) out = out.replace(`{${k}}`, String(v))
  }
  return out
}

const iconStubs = { Loader2: true, Pause: true, Play: true, Volume2: true }

function chunk(overrides: Partial<ChunkSummary> = {}): ChunkSummary {
  return {
    id: 'chunk-1',
    chunkOrder: 1,
    chapterTitle: 'Intro',
    summaryMarkdown: '',
    originalTextMarkdown: 'First sentence. Second sentence.',
    keyTakeaways: [],
    estimatedReadMinutes: 3,
    isAiFormatted: true,
    language: 'vi',
    ...overrides
  }
}

// Shared mocked composable state (same singleton the component consumes).
const audio = useSliceAudio()
let originalUseI18n: unknown

function mountPlayer(props: { chunk: ChunkSummary | null }) {
  return mount(ReaderAudioPlayer, { props, global: { stubs: iconStubs } })
}

describe('ReaderAudioPlayer.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    audio.status.value = 'idle'
    audio.playing.value = false
    audio.downloadProgress.value = 0
    audio.synthIndex.value = 0
    audio.synthTotal.value = 0
    originalUseI18n = Reflect.get(globalThis, 'useI18n')
    Reflect.set(globalThis, 'useI18n', () => ({ t: interpolate, locale: { value: 'en' } }))
  })

  afterEach(() => {
    Reflect.set(globalThis, 'useI18n', originalUseI18n)
  })

  it('hides the control for a slice that is not AI-formatted', () => {
    const wrapper = mountPlayer({ chunk: chunk({ isAiFormatted: false }) })
    expect(wrapper.find('button').exists()).toBe(false)
  })

  it('shows the play control for an AI-formatted slice', () => {
    const wrapper = mountPlayer({ chunk: chunk() })
    expect(wrapper.find('button').exists()).toBe(true)
    expect(wrapper.text()).toContain('Listen')
  })

  it('renders the download percentage on a 0-100 scale, never 10000%', async () => {
    const wrapper = mountPlayer({ chunk: chunk() })
    audio.status.value = 'loading'
    audio.downloadProgress.value = 100
    await nextTick()
    expect(wrapper.text()).toContain('100%')
    expect(wrapper.text()).not.toContain('10000%')
  })

  it('renders sentence synthesis progress while generating', async () => {
    const wrapper = mountPlayer({ chunk: chunk() })
    audio.status.value = 'loading'
    audio.synthTotal.value = 2
    audio.synthIndex.value = 1
    await nextTick()
    expect(wrapper.text()).toContain('1/2')
  })

  it('forwards the slice language into the narration source on play', async () => {
    const wrapper = mountPlayer({ chunk: chunk({ language: 'vi' }) })
    await wrapper.find('button').trigger('click')
    expect(audio.loadAndPlay).toHaveBeenCalledWith(
      expect.objectContaining({ chunkId: 'chunk-1', language: 'vi', isAiFormatted: true })
    )
  })
})

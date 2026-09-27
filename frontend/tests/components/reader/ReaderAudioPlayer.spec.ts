import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'

// Control the audio composable so we can drive loading/progress state and assert
// what the control renders and forwards - without running real synthesis.
vi.mock('~/composables/useSliceAudio', async () => {
  const { ref, computed } = await import('vue')
  const engineMode = ref('cloud')
  const selectedVoice = ref('')
  const audioQuota = ref(null)
  const isNearQuota = ref(false)
  const isQuotaExhausted = ref(false)
  const state = {
    status: ref('idle'),
    playing: ref(false),
    currentTime: ref(0),
    duration: ref(0),
    downloadProgress: ref(0),
    synthIndex: ref(0),
    synthTotal: ref(0),
    targetBufferCount: ref(0),
    errorMessage: ref<string | null>(null),
    errorInfo: ref<any>(null),
    device: ref<string | null>(null),
    speed: ref(1),
    engineMode,
    selectedVoice,
    audioQuota,
    isNearQuota,
    isQuotaExhausted,
    setEngineMode: vi.fn((m) => { engineMode.value = m }),
    setVoice: vi.fn((v) => { selectedVoice.value = v }),
    fetchQuota: vi.fn(async () => null),
    loadAndPlay: vi.fn(),
    play: vi.fn(),
    pause: vi.fn(),
    setSpeed: vi.fn(),
    seek: vi.fn()
  }
  return {
    useSliceAudio: () => state,
    CLOUD_VOICES: {
      vi: [
        { id: 'vi-VN-Neural2-A', label: 'vi-VN-Neural2-A', gender: 'female', language: 'vi' },
        { id: 'vi-VN-Neural2-D', label: 'vi-VN-Neural2-D', gender: 'male', language: 'vi' },
      ],
      en: [
        { id: 'en-US-Neural2-F', label: 'en-US-Neural2-F', gender: 'female', language: 'en' },
        { id: 'en-US-Neural2-D', label: 'en-US-Neural2-D', gender: 'male', language: 'en' },
      ],
    },
    resolveCloudVoiceForLanguage: (lang?: string | null) => (lang === 'vi' ? 'vi-VN-Neural2-A' : 'en-US-Neural2-F'),
  }
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
  'reader.audio_buffering': 'Buffering audio… {current}/{total}',
  'reader.audio_speed': 'Speed',
  'reader.audio_error': 'Could not generate audio.',
  'reader.audio_error_oom': 'Device memory limit reached. Try Cloud engine.',
  'reader.audio_error_device': 'On-device narration unavailable on this device. Try Cloud engine.',
  'reader.audio_error_network': 'Network error while loading audio. Please try again.',
  'reader.audio_error_with_reason': 'Could not generate audio: {message}',
  'reader.audio_fallback_to_cloud': 'Switch to Google Cloud',
  'reader.audio_engine_cloud': 'Cloud',
  'reader.audio_engine_device': 'Device',
  'reader.audio_engine_cloud_hint': 'Google Cloud high-speed narration',
  'reader.audio_engine_device_hint': 'On-device Web Worker synthesis',
  'reader.audio_quota_exhausted_toast': 'Monthly cloud audio quota reached. Switched to on-device narration.',
  'reader.audio_quota_near_limit_tooltip': 'Monthly cloud quota reached, using on-device narration',
  'reader.audio_voice_select_placeholder': 'Select voice',
  'reader.audio_voice_female': 'Female',
  'reader.audio_voice_male': 'Male'
}

function interpolate(key: string, params?: Record<string, unknown>): string {
  let out = MESSAGES[key] ?? key
  if (params) {
    for (const [k, v] of Object.entries(params)) out = out.replace(`{${k}}`, String(v))
  }
  return out
}

const iconStubs = {
  Loader2: true,
  Pause: true,
  Play: true,
  Volume2: true,
  Cloud: true,
  Laptop: true,
  Cpu: true,
  AppSelect: true
}

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
    audio.targetBufferCount.value = 0
    audio.errorMessage.value = null
    audio.errorInfo.value = null
    audio.isNearQuota.value = false
    audio.isQuotaExhausted.value = false
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

  it('renders buffering progress while synthIndex < targetBufferCount', async () => {
    const wrapper = mountPlayer({ chunk: chunk() })
    audio.status.value = 'loading'
    audio.synthTotal.value = 6
    audio.targetBufferCount.value = 2
    audio.synthIndex.value = 1
    await nextTick()
    expect(wrapper.text()).toContain('Buffering audio… 1/2')
  })

  it('hides synthesis progress badge when engineMode is cloud and shows it when device', async () => {
    audio.engineMode.value = 'cloud'
    audio.duration.value = 100
    audio.synthTotal.value = 45
    audio.synthIndex.value = 2
    audio.status.value = 'ready'

    const testChunk = chunk({ id: 'chunk-badge-test' })
    const wrapper = mountPlayer({ chunk: testChunk })

    // Trigger listen to set loadedId
    await wrapper.find('button').trigger('click')
    await nextTick()

    // In Cloud mode, (2/45) should NOT be rendered
    expect(wrapper.text()).not.toContain('2/45')

    // Switch to Device mode
    audio.engineMode.value = 'device'
    await nextTick()
    expect(wrapper.text()).toContain('2/45')
  })

  it('forwards the slice language into the narration source on play', async () => {
    const wrapper = mountPlayer({ chunk: chunk({ language: 'vi' }) })
    await wrapper.find('button').trigger('click')
    expect(audio.loadAndPlay).toHaveBeenCalledWith(
      expect.objectContaining({ chunkId: 'chunk-1', language: 'vi', isAiFormatted: true })
    )
  })

  it('renders engine switch and toggles between Cloud and Device mode', async () => {
    audio.engineMode.value = 'cloud'
    const wrapper = mountPlayer({ chunk: chunk() })
    expect(wrapper.text()).toContain('Cloud')
    expect(wrapper.text()).toContain('Device')

    // Find device button and click
    const buttons = wrapper.findAll('button')
    const deviceBtn = buttons.find(b => b.text().includes('Device'))
    expect(deviceBtn).toBeDefined()
    await deviceBtn!.trigger('click')
    expect(audio.setEngineMode).toHaveBeenCalledWith('device')
  })

  it('disables Cloud toggle with tooltip when quota is near limit', async () => {
    audio.isNearQuota.value = true
    const wrapper = mountPlayer({ chunk: chunk() })
    const buttons = wrapper.findAll('button')
    const cloudBtn = buttons.find(b => b.text().includes('Cloud'))
    expect(cloudBtn).toBeDefined()
    expect(cloudBtn!.attributes('disabled')).toBeDefined()
    expect(cloudBtn!.attributes('title')).toBe('Monthly cloud quota reached, using on-device narration')
  })

  it('renders voice selector when Cloud mode is active', async () => {
    audio.engineMode.value = 'cloud'
    audio.isNearQuota.value = false
    const wrapper = mountPlayer({ chunk: chunk({ language: 'vi' }) })
    expect(wrapper.findComponent({ name: 'AppSelect' }).exists()).toBe(true)
  })

  it('does not render "AI" buzzword labels or Zap icons', () => {
    const wrapper = mountPlayer({ chunk: chunk() })
    const text = wrapper.text()
    expect(text).not.toContain('AI')
    expect(text).not.toContain('Google AI')
    expect(wrapper.html()).not.toContain('lucide-zap')
  })

  it('renders diagnostic error and 1-tap Cloud fallback button on device failure', async () => {
    audio.engineMode.value = 'device'
    audio.status.value = 'error'
    audio.errorMessage.value = 'model load failed'
    audio.errorInfo.value = {
      code: 'DEVICE_INIT_FAILED',
      rawMessage: 'model load failed',
      suggestCloudFallback: true,
    }
    const wrapper = mountPlayer({ chunk: chunk() })
    await nextTick()

    expect(wrapper.text()).toContain('On-device narration unavailable on this device')
    expect(wrapper.text()).toContain('Switch to Google Cloud')

    const fallbackBtn = wrapper.findAll('button').find(b => b.text().includes('Switch to Google Cloud'))
    expect(fallbackBtn).toBeDefined()
    await fallbackBtn!.trigger('click')
    expect(audio.setEngineMode).toHaveBeenCalledWith('cloud')
  })

  it('hides the 1-tap Cloud fallback button when cloud quota is exhausted', async () => {
    audio.engineMode.value = 'device'
    audio.status.value = 'error'
    audio.isQuotaExhausted.value = true
    audio.errorInfo.value = {
      code: 'DEVICE_OOM',
      rawMessage: 'OOM',
      suggestCloudFallback: true,
    }
    const wrapper = mountPlayer({ chunk: chunk() })
    await nextTick()

    expect(wrapper.text()).not.toContain('Switch to Google Cloud')
  })
})

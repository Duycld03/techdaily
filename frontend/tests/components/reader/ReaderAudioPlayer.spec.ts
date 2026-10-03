import { describe, it, expect, beforeEach, afterEach, vi, type Mock } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick, type Ref } from 'vue'

// Control the audio composable so we can drive loading/progress state and assert
// what the control renders and forwards - without running real synthesis.
vi.mock('~/composables/useSliceAudio', async () => {
  const { ref } = await import('vue')
  const engineMode = ref('cloud')
  const activeCascadeTier = ref('cloud')
  const selectedVoice = ref('')
  const selectedSystemVoice = ref('')
  const systemVoices = ref([
    { id: 'vi-vn-x-vic-local', name: 'Google Tiếng Việt', lang: 'vi-VN', localService: true, default: true },
    { id: 'en-us-x-sfg-local', name: 'Google US English', lang: 'en-US', localService: true, default: false },
  ])
  const pitch = ref(1)
  const autoAdvance = ref(true)
  const volume = ref(1)
  const audioQuota = ref(null)
  const isNearQuota = ref(false)
  const isQuotaExhausted = ref(false)
  let onSliceEndedCallback: (() => void) | null = null
  let onNextTrackCallback: (() => void) | null = null
  let onPrevTrackCallback: (() => void) | null = null
  let onFallbackToCloudCallback: (() => void) | null = null
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
    activeCascadeTier,
    selectedVoice,
    selectedSystemVoice,
    systemVoices,
    pitch,
    autoAdvance,
    volume,
    audioQuota,
    isNearQuota,
    isQuotaExhausted,
    setEngineMode: vi.fn((m) => { engineMode.value = m; activeCascadeTier.value = m }),
    setVoice: vi.fn((v) => { selectedVoice.value = v }),
    setSystemVoice: vi.fn((v) => { selectedSystemVoice.value = v }),
    setPitch: vi.fn((p) => { pitch.value = p }),
    setAutoAdvance: vi.fn((a) => { autoAdvance.value = a }),
    setVolume: vi.fn((v) => { volume.value = v }),
    fetchQuota: vi.fn(async () => null),
    loadAndPlay: vi.fn(),
    play: vi.fn(),
    pause: vi.fn(),
    setSpeed: vi.fn(),
    seek: vi.fn(),
    playSliceTransitionChime: vi.fn(async () => {}),
    updateMediaSessionMetadata: vi.fn(),
    _triggerSliceEnded: () => onSliceEndedCallback?.(),
    _triggerNextTrack: () => onNextTrackCallback?.(),
    _triggerPrevTrack: () => onPrevTrackCallback?.(),
    _triggerFallbackToCloud: () => onFallbackToCloudCallback?.(),
  }
  return {
    useSliceAudio: (deps?: any) => {
      if (deps?.onSliceEnded) onSliceEndedCallback = deps.onSliceEnded
      if (deps?.onNextTrack) onNextTrackCallback = deps.onNextTrack
      if (deps?.onPreviousTrack) onPrevTrackCallback = deps.onPreviousTrack
      if (deps?.onFallbackToCloud) onFallbackToCloudCallback = deps.onFallbackToCloud
      return state
    },
    filterSystemVoicesForLanguage: (voices: any[], lang?: string | null) => {
      if (!lang) return voices
      const prefix = lang.toLowerCase().slice(0, 2)
      return voices.filter(v => v.lang.toLowerCase().startsWith(prefix))
    },
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
import { useSliceAudio, type AudioStatus, type AudioEngine } from '~/composables/useSliceAudio'
import type { ChunkSummary } from '~/stores/useLibraryStore'

const MESSAGES: Record<string, string> = {
  'reader.audio_listen': 'Listen',
  'reader.audio_play': 'Play narration',
  'reader.audio_pause': 'Pause',
  'reader.audio_pause_desc': 'Pause narration',
  'reader.audio_downloading': 'Downloading voice… {progress}%',
  'reader.audio_preparing': 'Preparing audio…',
  'reader.audio_synthesizing': 'Generating audio… {current}/{total}',
  'reader.audio_buffering': 'Buffering audio… {current}/{total}',
  'reader.audio_speed': 'Speed',
  'reader.audio_error': 'Could not generate audio.',
  'reader.audio_error_oom': 'Device memory limit reached. Try Cloud engine.',
  'reader.audio_error_device': 'On-device narration unavailable on this device. Try Cloud engine.',
  'reader.audio_error_system': 'Browser speech synthesis failed. Try Cloud engine.',
  'reader.audio_error_network': 'Network error while loading audio. Please try again.',
  'reader.audio_error_with_reason': 'Could not generate audio: {message}',
  'reader.audio_fallback_to_cloud': 'Switch to Google Cloud',
  'reader.audio_engine_system': 'System',
  'reader.audio_engine_cloud': 'Cloud',
  'reader.audio_engine_device': 'Device',
  'reader.audio_engine_system_hint': 'Local device system speech synthesis',
  'reader.audio_engine_cloud_hint': 'Google Cloud high-speed narration',
  'reader.audio_engine_device_hint': 'On-device Web Worker synthesis',
  'reader.audio_cascade_tier': 'Tier: {tier}',
  'reader.audio_cascade_active_hint': 'Active synthesis cascade tier',
  'reader.audio_system_voice_default': 'System Default Voice',
  'reader.audio_system_voice_auto_desc': 'Automatic voice assignment',
  'reader.audio_auto_advance': 'Auto Next',
  'reader.audio_auto_advance_hint': 'Automatically advance to next slice and continue narration',
  'reader.audio_sleep_timer': 'Sleep Timer',
  'reader.audio_sleep_timer_off': 'Off',
  'reader.audio_sleep_timer_15m': '15 min',
  'reader.audio_sleep_timer_30m': '30 min',
  'reader.audio_sleep_timer_45m': '45 min',
  'reader.audio_sleep_timer_60m': '60 min',
  'reader.audio_sleep_timer_end_of_slice': 'End of slice',
  'reader.audio_sleep_timer_ended': 'Sleep timer expired. Audio paused.',
  'reader.audio_pitch': 'Pitch',
  'reader.audio_quota_exhausted_toast': 'Monthly cloud audio quota reached. Switched to on-device narration.',
  'reader.audio_quota_near_limit_tooltip': 'Monthly cloud quota reached, using on-device narration',
  'reader.audio_voice_select_placeholder': 'Select voice',
  'reader.audio_voice_female': 'Female',
  'reader.audio_voice_male': 'Male',
  'reader.audio_device_gpu': 'GPU',
  'reader.audio_device_cpu': 'CPU',
  'reader.audio_device_gpu_hint': 'Narration running on GPU',
  'reader.audio_device_cpu_hint': 'Narration running on CPU',
  'reader.audio_fallback_to_cloud_toast': 'No matching browser voice found; automatically switched to Cloud TTS.',
  'reader.audio_system_playing_label': 'Reading via browser voice: {voice}',
  'reader.audio_system_ready_label': 'Ready to read via browser voice',
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
interface MockedSliceAudio {
  status: Ref<AudioStatus>
  playing: Ref<boolean>
  currentTime: Ref<number>
  duration: Ref<number>
  downloadProgress: Ref<number>
  synthIndex: Ref<number>
  synthTotal: Ref<number>
  targetBufferCount: Ref<number>
  errorMessage: Ref<string | null>
  errorInfo: Ref<unknown>
  device: Ref<string | null>
  speed: Ref<number>
  engineMode: Ref<AudioEngine>
  selectedVoice: Ref<string>
  audioQuota: Ref<unknown>
  isNearQuota: Ref<boolean>
  isQuotaExhausted: Ref<boolean>
  setEngineMode: Mock
  setVoice: Mock
  fetchQuota: Mock
  loadAndPlay: Mock
  play: Mock
  pause: Mock
  setSpeed: Mock
  seek: Mock
  _triggerSliceEnded: () => void
  _triggerNextTrack: () => void
  _triggerPrevTrack: () => void
  _triggerFallbackToCloud: () => void
}
const audio = useSliceAudio() as unknown as MockedSliceAudio
let originalUseI18n: unknown

function mountPlayer(props: { chunk: ChunkSummary | null; disableAutoAdvance?: boolean; bookTitle?: string }) {
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
    useToast().clear()
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

  it('prioritizes model download progress over buffering status', async () => {
    const wrapper = mountPlayer({ chunk: chunk() })
    audio.status.value = 'loading'
    audio.synthTotal.value = 12
    audio.targetBufferCount.value = 2
    audio.synthIndex.value = 0
    audio.downloadProgress.value = 45
    await nextTick()
    expect(wrapper.text()).toContain('45%')
    expect(wrapper.text()).not.toContain('0/2')
  })

  it('renders compute device badge for webgpu and wasm without warnings', async () => {
    audio.engineMode.value = 'device'
    const wrapper = mountPlayer({ chunk: chunk() })
    audio.device.value = 'webgpu'
    await nextTick()
    expect(wrapper.text()).toContain('GPU')

    audio.device.value = 'wasm'
    await nextTick()
    expect(wrapper.text()).toContain('CPU')
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
    expect(wrapper.text()).toContain('~37:30')
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

  it('prepares audio with autoPlay=false when toggling engine while paused', async () => {
    audio.engineMode.value = 'cloud'
    audio.playing.value = false
    const wrapper = mountPlayer({ chunk: chunk() })

    // Simulate loaded slice
    await wrapper.find('button').trigger('click')
    audio.loadAndPlay.mockClear()

    // Find device toggle button
    const deviceBtn = wrapper.findAll('button').find(b => b.text().includes('Device'))
    await deviceBtn!.trigger('click')

    expect(audio.setEngineMode).toHaveBeenCalledWith('device')
    expect(audio.loadAndPlay).toHaveBeenCalledWith(
      expect.objectContaining({ chunkId: 'chunk-1' }),
      false,
      0
    )
  })

  it('invokes play directly without restarting loadAndPlay when audio is already loaded and ready', async () => {
    audio.engineMode.value = 'device'
    audio.playing.value = false
    audio.synthIndex.value = 5
    audio.synthTotal.value = 15
    const wrapper = mountPlayer({ chunk: chunk() })

    // First click to load
    await wrapper.find('button').trigger('click')
    expect(audio.loadAndPlay).toHaveBeenCalledTimes(1)
    audio.loadAndPlay.mockClear()

    // Audio is loaded and ready
    audio.status.value = 'ready'
    await wrapper.find('button').trigger('click')

    expect(audio.play).toHaveBeenCalled()
    expect(audio.loadAndPlay).not.toHaveBeenCalled()
  })

  it('does not trigger duplicate loadAndPlay when user clicks play button while audio is loading or buffering', async () => {
    audio.engineMode.value = 'device'
    audio.playing.value = false
    audio.status.value = 'loading'
    audio.synthIndex.value = 1
    audio.synthTotal.value = 15
    const wrapper = mountPlayer({ chunk: chunk() })

    // Click play button while in loading state
    await wrapper.find('button').trigger('click')

    expect(audio.loadAndPlay).not.toHaveBeenCalled()
    expect(audio.play).not.toHaveBeenCalled()
  })

  it('automatically continues playback when toggling engine while actively playing', async () => {
    audio.engineMode.value = 'cloud'
    audio.playing.value = false
    const wrapper = mountPlayer({ chunk: chunk() })

    // Simulate loaded slice
    await wrapper.find('button').trigger('click')
    audio.loadAndPlay.mockClear()

    // Now actively playing
    audio.playing.value = true

    // Find device toggle button
    const deviceBtn = wrapper.findAll('button').find(b => b.text().includes('Device'))
    await deviceBtn!.trigger('click')

    expect(audio.setEngineMode).toHaveBeenCalledWith('device')
    expect(audio.loadAndPlay).toHaveBeenCalledTimes(1)
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

  it('renders diagnostic error and 1-tap Cloud fallback button on system TTS failure', async () => {
    audio.engineMode.value = 'system'
    audio.status.value = 'error'
    audio.errorMessage.value = 'System TTS Error: synthesis-failed'
    audio.errorInfo.value = {
      code: 'SYSTEM_TTS_FAILED',
      rawMessage: 'System TTS Error: synthesis-failed',
      suggestCloudFallback: true,
    }
    const wrapper = mountPlayer({ chunk: chunk() })
    await nextTick()

    expect(wrapper.text()).toContain('Browser speech synthesis failed. Try Cloud engine.')
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

  describe('Auto Next, Sleep Timer, and Advanced Controls', () => {
    it('toggles Auto Next when the auto-advance button is clicked', async () => {
      const wrapper = mountPlayer({ chunk: chunk() })
      const autoBtn = wrapper.findAll('button').find(b => b.text().includes('Auto Next'))
      expect(autoBtn).toBeDefined()
      expect(audio.autoAdvance.value).toBe(true)

      await autoBtn!.trigger('click')
      expect(audio.autoAdvance.value).toBe(false)

      await autoBtn!.trigger('click')
      expect(audio.autoAdvance.value).toBe(true)
    })

    it('opens sleep timer dropdown and selects 15m preset', async () => {
      vi.useFakeTimers()
      try {
        const wrapper = mountPlayer({ chunk: chunk() })
        const timerBtn = wrapper.findAll('button').find(b => b.text().includes('Sleep Timer'))
        expect(timerBtn).toBeDefined()

        // Open dropdown
        await timerBtn!.trigger('click')
        await nextTick()

        // Find the 15 min option
        const opt15m = wrapper.findAll('button').find(b => b.text().includes('15 min'))
        expect(opt15m).toBeDefined()

        // Select 15 min
        await opt15m!.trigger('click')
        await nextTick()

        // Badge should display 15:00
        expect(wrapper.text()).toContain('15:00')
      } finally {
        vi.useRealTimers()
      }
    })

    it('applies exponential fade-out during the final 15 seconds of sleep timer countdown and pauses', async () => {
      vi.useFakeTimers()
      try {
        audio.playing.value = true
        const wrapper = mountPlayer({ chunk: chunk() })
        const timerBtn = wrapper.findAll('button').find(b => b.text().includes('Sleep Timer'))
        await timerBtn!.trigger('click')
        await nextTick()

        const opt15m = wrapper.findAll('button').find(b => b.text().includes('15 min'))
        await opt15m!.trigger('click')
        await nextTick()

        // Advance timer 14 minutes and 46 seconds (886 seconds) -> 14 seconds remaining
        vi.advanceTimersByTime(886 * 1000)
        await nextTick()

        // Expect volume to be faded: (14 / 15)^2 ≈ 0.871
        expect(audio.setVolume).toHaveBeenCalled()
        const lastCallVolume = audio.setVolume.mock.calls.at(-1)?.[0]
        expect(lastCallVolume).toBeLessThan(1.0)
        expect(lastCallVolume).toBeGreaterThan(0.8)

        // Advance 14 more seconds to reach zero
        vi.advanceTimersByTime(14 * 1000)
        await nextTick()

        expect(audio.pause).toHaveBeenCalled()
        expect(audio.setVolume).toHaveBeenCalledWith(1)
      } finally {
        vi.useRealTimers()
      }
    })

    it('emits auto-advance when slice finishes and autoAdvance is active', async () => {
      const wrapper = mountPlayer({ chunk: chunk() })
      audio.autoAdvance.value = true

      // Simulate composable onSliceEnded callback
      audio._triggerSliceEnded()
      await nextTick()
      // Allow microtask resolution for playSliceTransitionChime
      await Promise.resolve()
      await nextTick()

      expect(wrapper.emitted('auto-advance')).toHaveLength(1)
    })

    it('emits seek-slice when nexttrack and previoustrack trigger', async () => {
      const wrapper = mountPlayer({ chunk: chunk() })

      audio._triggerNextTrack()
      await nextTick()
      expect(wrapper.emitted('seek-slice')?.[0]).toEqual(['next'])

      audio._triggerPrevTrack()
      await nextTick()
      expect(wrapper.emitted('seek-slice')?.[1]).toEqual(['prev'])
    })

    it('renders system engine toggle and switches to System mode', async () => {
      const wrapper = mountPlayer({ chunk: chunk() })
      const sysBtn = wrapper.findAll('button').find(b => b.text().includes('System'))
      expect(sysBtn).toBeDefined()

      await sysBtn!.trigger('click')
      expect(audio.setEngineMode).toHaveBeenCalledWith('system')
    })
    it('toggles play/pause button label between Listen and Pause based on playing state', async () => {
      audio.playing.value = false
      const wrapper = mountPlayer({ chunk: chunk() })
      const playBtn = wrapper.findAll('button')[0]
      expect(playBtn.text()).toContain('Listen')
      expect(playBtn.attributes('aria-label')).toBe('Play narration')

      audio.playing.value = true
      await nextTick()
      expect(playBtn.text()).toContain('Pause')
      expect(playBtn.attributes('aria-label')).toBe('Pause narration')
    })

    it('hides the auto-advance button when disableAutoAdvance prop is true', () => {
      const wrapper = mountPlayer({ chunk: chunk(), disableAutoAdvance: true })
      const autoBtn = wrapper.findAll('button').find(b => b.text().includes('Auto Next'))
      expect(autoBtn).toBeUndefined()
    })

    it('never renders active cascade tier badge', async () => {
      audio.activeCascadeTier.value = 'cloud'
      audio.engineMode.value = 'device'
      const wrapper = mountPlayer({ chunk: chunk() })
      await nextTick()
      expect(wrapper.text()).not.toContain('Tier:')
    })

    it('triggers info toast on fallback to cloud callback', async () => {
      const toast = useToast()
      mountPlayer({ chunk: chunk() })
      audio._triggerFallbackToCloud()
      await nextTick()
      const lastToast = toast.toasts.value.at(-1)
      expect(lastToast?.type).toBe('info')
      expect(lastToast?.message).toBe('No matching browser voice found; automatically switched to Cloud TTS.')
    })

    it('renders Web Speech active playing banner when duration is 0 and playing is true', async () => {
      audio.engineMode.value = 'system'
      audio.duration.value = 0
      audio.playing.value = true
      const wrapper = mountPlayer({ chunk: chunk() })
      await nextTick()
      expect(wrapper.text()).toContain('Reading via browser voice:')
    })
  })

  it('renders seekable scrubber and formatTime when System mode is loaded with duration > 0', async () => {
    audio.engineMode.value = 'system'
    audio.playing.value = false
    audio.duration.value = 65
    audio.currentTime.value = 15
    const wrapper = mountPlayer({ chunk: chunk() })

    // Simulate loaded slice
    await wrapper.find('button').trigger('click')
    audio.playing.value = true
    await nextTick()

    const slider = wrapper.find('input[type="range"]')
    expect(slider.exists()).toBe(true)
    expect(wrapper.text()).toContain('0:15 / 1:05')
  })

  it('preserves currentTime as offset when toggling engines', async () => {
    audio.engineMode.value = 'cloud'
    audio.playing.value = false
    const wrapper = mountPlayer({ chunk: chunk() })

    // Simulate loaded slice
    await wrapper.find('button').trigger('click')
    audio.loadAndPlay.mockClear()

    audio.currentTime.value = 42
    audio.playing.value = true

    const deviceBtn = wrapper.findAll('button').find(b => b.text().includes('Device'))
    await deviceBtn!.trigger('click')

    expect(audio.loadAndPlay).toHaveBeenCalledWith(
      expect.objectContaining({ chunkId: 'chunk-1' }),
      true,
      42
    )
  })

  it('does not trigger end_of_slice toast when user clicks pause', async () => {
    const toast = useToast()
    toast.clear()
    const wrapper = mountPlayer({ chunk: chunk() })
    const timerBtn = wrapper.findAll('button').find(b => b.text().includes('Sleep Timer'))
    expect(timerBtn).toBeDefined()
    await timerBtn!.trigger('click')
    await nextTick()

    const endOfSliceBtn = wrapper.findAll('button').find(b => b.text().includes('End of slice'))
    expect(endOfSliceBtn).toBeDefined()
    await endOfSliceBtn!.trigger('click')
    await nextTick()

    // Click pause button
    const playPauseBtn = wrapper.find('button')
    await playPauseBtn.trigger('click')

    const sleepToast = toast.toasts.value.find(t => t.message.includes('Sleep timer expired'))
    expect(sleepToast).toBeUndefined()
  })
})

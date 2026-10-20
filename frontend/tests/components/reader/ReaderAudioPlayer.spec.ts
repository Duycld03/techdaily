import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import type * as VueModule from 'vue'
import type * as NarrationModule from '~/composables/useSliceAudio'
import { useToast } from '~/composables/useToast'
import english from '~/i18n/locales/en.json'
import vietnamese from '~/i18n/locales/vi.json'
import { useAuthStore } from '~/stores/useAuthStore'
import type { AudioEngine } from '~/composables/useSliceAudio'
import type { ChunkSummary } from '~/stores/useLibraryStore'
import type { AudioNavigationRequest } from '~/components/reader/ReaderAudioPlayer.vue'

enableAutoUnmount(afterEach)

const events = vi.hoisted(() => ({
  ended: null as ((isCurrent: () => boolean) => void) | null,
  paused: null as (() => void) | null,
  next: null as ((isCurrent: () => boolean) => void) | null,
  previous: null as ((isCurrent: () => boolean) => void) | null,
  fallback: null as ((transition: { from: AudioEngine; to: AudioEngine; reason: string }) => void) | null,
  persistence: null as (() => void) | null,
}))

vi.mock('~/stores/useAuthStore', async () => {
  const { reactive } = await vi.importActual<typeof VueModule>('vue')
  const auth = reactive({ user: { id: 'reader-account' }, sessionGeneration: 0 })
  return { useAuthStore: () => auth }
})

// Engine execution is covered by composable regressions and real-browser playback.
// This fixture drives the toolbar's user-visible state and navigation boundaries.
vi.mock('~/composables/useSliceAudio', async (importOriginal) => {
  // Vitest hoists this factory before normal imports; its helpers load the unmocked modules.
  const original = await importOriginal<typeof NarrationModule>()
  const { ref } = await vi.importActual<typeof VueModule>('vue')
  let retainedSource = false
  const state = {
    status: ref('idle'), playing: ref(false), currentTime: ref(0), duration: ref(0),
    downloadProgress: ref(0), synthIndex: ref(0), synthTotal: ref(0), targetBufferCount: ref(0),
    errorMessage: ref<string | null>(null), errorInfo: ref<{ code: string; suggestCloudFallback: boolean } | null>(null),
    device: ref<string | null>(null), speed: ref(1), volume: ref(1),
    enginePreference: ref('auto'), engineMode: ref('cloud'),
    persistenceAvailable: ref(true), restoredProgress: ref(false),
    selectedVoice: ref(''), selectedSystemVoice: ref(''),
    systemVoices: ref([
      { id: 'vi-system', name: 'Vietnamese browser voice', lang: 'vi-VN', localService: true, default: true },
      { id: 'en-system', name: 'English browser voice', lang: 'en-US', localService: true, default: false },
    ]),
    autoAdvance: ref(true), isNearQuota: ref(false), isQuotaExhausted: ref(false),
    setEnginePreference: vi.fn((mode: string) => {
      state.enginePreference.value = mode
      if (mode !== 'auto' && mode !== state.engineMode.value) {
        state.engineMode.value = mode
        state.resetSource()
      }
    }),
    resetSource: vi.fn(() => {
      retainedSource = false
      state.playing.value = false
      state.status.value = 'idle'
      state.currentTime.value = 0
      state.duration.value = 0
    }),
    setVoice: vi.fn((voice: string) => {
      state.selectedVoice.value = voice
      state.playing.value = false
      state.status.value = 'idle'
      state.currentTime.value = 0
    }),
    setSystemVoice: vi.fn((voice: string) => {
      state.selectedSystemVoice.value = voice
      state.playing.value = false
      state.status.value = 'idle'
      state.currentTime.value = 0
    }),
    setVolume: vi.fn((volume: number) => { state.volume.value = volume }),
    loadAndPlay: vi.fn(async (_source: unknown, options?: { autoplay?: boolean }) => {
      retainedSource = true
      state.currentTime.value = 0
      state.status.value = options?.autoplay === false ? 'idle' : 'ready'
      state.playing.value = options?.autoplay !== false
    }),
    play: vi.fn(async () => { if (retainedSource) state.playing.value = true }),
    pause: vi.fn(() => {
      events.paused?.()
      state.playing.value = false
      if (state.status.value === 'loading') state.status.value = 'idle'
    }),
    setSpeed: vi.fn((speed: number) => { state.speed.value = speed }),
    seek: vi.fn((seconds: number) => { state.currentTime.value = seconds }),
    playSliceTransitionChime: vi.fn(async () => {}),
    updateMediaSessionMetadata: vi.fn(),
  }
  return {
    ...original,
    useSliceAudio: (deps?: Parameters<typeof original.useSliceAudio>[0]) => {
      events.ended = deps?.onSliceEnded ?? null
      events.paused = deps?.onPause ?? null
      events.next = deps?.onNextTrack ?? null
      events.previous = deps?.onPreviousTrack ?? null
      events.fallback = deps?.onFallback ?? null
      events.persistence = deps?.onPersistenceUnavailable ?? null
      return state
    },
  }
})

import ReaderAudioPlayer from '~/components/reader/ReaderAudioPlayer.vue'
import AppSelect from '~/components/common/AppSelect.vue'
import { useSliceAudio } from '~/composables/useSliceAudio'

const audio = useSliceAudio()
let reader = english.reader
let originalUseI18n: unknown

function translate(key: string, values: Record<string, unknown> = {}): string {
  const name = key.replace(/^reader\./, '') as keyof typeof reader
  let message = reader[name] ?? key
  for (const [name, value] of Object.entries(values)) message = message.replace(`{${name}}`, String(value))
  return message
}

function chunk(overrides: Partial<ChunkSummary> = {}): ChunkSummary {
  return {
    id: 'chunk-1', chunkOrder: 1, chapterTitle: 'Intro', summaryMarkdown: '',
    originalTextMarkdown: 'First sentence. Second sentence.', keyTakeaways: [],
    estimatedReadMinutes: 3, isAiFormatted: true, language: 'vi', ...overrides,
  }
}

function mountPlayer(props: { chunk: ChunkSummary | null; bookLanguage?: string | null; disableAutoAdvance?: boolean } = { chunk: chunk() }) {
  return mount(ReaderAudioPlayer, { props, global: { stubs: { AppSelect: true } } })
}

async function completeSlice() {
  events.ended?.(() => true)
  await Promise.resolve()
  await nextTick()
}

beforeEach(() => {
  reader = english.reader
  const auth = useAuthStore()
  auth.user = { ...auth.user!, id: 'reader-account' }
  auth.sessionGeneration = 0
  audio.resetSource()
  vi.clearAllMocks()
  audio.status.value = 'idle'
  audio.playing.value = false
  audio.currentTime.value = 0
  audio.duration.value = 0
  audio.speed.value = 1
  audio.volume.value = 1
  audio.enginePreference.value = 'auto'
  audio.engineMode.value = 'cloud'
  audio.downloadProgress.value = 0
  audio.synthIndex.value = 0
  audio.synthTotal.value = 0
  audio.targetBufferCount.value = 0
  audio.errorMessage.value = null
  audio.errorInfo.value = null
  audio.device.value = null
  audio.isNearQuota.value = false
  audio.isQuotaExhausted.value = false
  audio.autoAdvance.value = true
  audio.persistenceAvailable.value = true
  audio.restoredProgress.value = false
  audio.selectedVoice.value = ''
  audio.selectedSystemVoice.value = ''
  useToast().clear()
  originalUseI18n = Reflect.get(globalThis, 'useI18n')
  Reflect.set(globalThis, 'useI18n', () => ({ t: translate, locale: { value: 'en' } }))
})

afterEach(() => {
  Reflect.set(globalThis, 'useI18n', originalUseI18n)
  vi.useRealTimers()
})

describe('ReaderAudioPlayer', () => {
  it('does not offer narration for an unformatted slice', () => {
    const wrapper = mountPlayer({ chunk: chunk({ isAiFormatted: false }) })
    expect(wrapper.find('button').exists()).toBe(false)
  })

  it('exposes Auto preference independently of the effective fallback engine', () => {
    const wrapper = mountPlayer()
    expect(wrapper.get('button[aria-label="Auto"]').attributes('aria-pressed')).toBe('true')
    expect(wrapper.get('button[aria-label="Cloud"]').attributes('aria-pressed')).toBe('false')
    expect(wrapper.get('[role="status"]').text()).toContain(reader.audio_engine_cloud)
  })

  it('keeps Auto selected when the effective engine changes to Local', async () => {
    const wrapper = mountPlayer()
    audio.engineMode.value = 'device'
    await nextTick()
    expect(wrapper.get('button[aria-label="Auto"]').attributes('aria-pressed')).toBe('true')
    expect(wrapper.get('[role="status"]').text()).toContain(reader.audio_engine_device)
  })

  it('keeps a paused selection paused and exposes its explicit preference', async () => {
    const wrapper = mountPlayer()
    await wrapper.get(`button[aria-label="${reader.audio_engine_device}"]`).trigger('click')
    expect(audio.playing.value).toBe(false)
    expect(wrapper.get(`button[aria-label="${reader.audio_engine_device}"]`).attributes('aria-pressed')).toBe('true')
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it('allows canceling pending preparation through the playback control', async () => {
    const wrapper = mountPlayer()
    audio.status.value = 'loading'
    await nextTick()
    await wrapper.get(`button[aria-label="${reader.audio_pause_desc}"]`).trigger('click')
    expect(wrapper.find(`button[aria-label="${reader.audio_play}"]`).exists()).toBe(true)
    expect(audio.playing.value).toBe(false)
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it('offers only compatible Cloud voices for English and Vietnamese slices', async () => {
    const wrapper = mountPlayer()
    expect(wrapper.getComponent(AppSelect).props('options').map((option: { value: string }) => option.value)).toEqual(['vi-VN-Neural2-A', 'vi-VN-Neural2-D'])
    await wrapper.setProps({ chunk: chunk({ language: 'en' }) })
    expect(wrapper.getComponent(AppSelect).props('options').map((option: { value: string }) => option.value)).toEqual(['en-US-Neural2-F', 'en-US-Neural2-D'])
  })

  it('uses known book language for compatible voices when slice language is unknown', async () => {
    const wrapper = mountPlayer({ chunk: chunk({ language: undefined }), bookLanguage: 'vi' })
    expect(wrapper.getComponent(AppSelect).props('options').map((option: { value: string }) => option.value)).toEqual(['vi-VN-Neural2-A', 'vi-VN-Neural2-D'])
    await wrapper.setProps({ bookLanguage: 'en' })
    expect(wrapper.getComponent(AppSelect).props('options').map((option: { value: string }) => option.value)).toEqual(['en-US-Neural2-F', 'en-US-Neural2-D'])
  })

  it('does not autoplay after changing a paused voice', async () => {
    const wrapper = mountPlayer()
    await wrapper.get(`button[aria-label="${reader.audio_play}"]`).trigger('click')
    await wrapper.get(`button[aria-label="${reader.audio_pause_desc}"]`).trigger('click')
    wrapper.getComponent(AppSelect).vm.$emit('update:modelValue', 'vi-VN-Neural2-D')
    await nextTick()
    expect(audio.playing.value).toBe(false)
    expect(wrapper.find(`button[aria-label="${reader.audio_play}"]`).exists()).toBe(true)
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it.each(['cloud', 'system'] as const)('allows remote Play after a paused %s voice selection without autoplay', async (engine) => {
    audio.engineMode.value = engine
    const wrapper = mountPlayer()
    await wrapper.get(`button[aria-label="${reader.audio_play}"]`).trigger('click')
    await wrapper.get(`button[aria-label="${reader.audio_pause_desc}"]`).trigger('click')
    wrapper.getComponent(AppSelect).vm.$emit('update:modelValue', engine === 'system' ? 'vi-system-other' : 'vi-VN-Neural2-D')
    await nextTick()
    expect(audio.playing.value).toBe(false)
    await wrapper.vm.play()
    expect(audio.playing.value).toBe(true)
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it('preserves paused System position when switching the preference to Auto', async () => {
    audio.engineMode.value = 'system'
    audio.enginePreference.value = 'system'
    const wrapper = mountPlayer()
    await wrapper.get(`button[aria-label="${reader.audio_play}"]`).trigger('click')
    await wrapper.get(`button[aria-label="${reader.audio_pause_desc}"]`).trigger('click')
    audio.currentTime.value = 25
    await wrapper.get('button[aria-label="Auto"]').trigger('click')
    await wrapper.get(`button[aria-label="${reader.audio_play}"]`).trigger('click')
    expect(audio.currentTime.value).toBe(25)
    expect(audio.playing.value).toBe(true)
  })

  it('stops obsolete playback when the same slice is reformatted', async () => {
    const wrapper = mountPlayer()
    await wrapper.get(`button[aria-label="${reader.audio_play}"]`).trigger('click')
    audio.currentTime.value = 25
    await wrapper.setProps({ chunk: chunk({ originalTextMarkdown: 'Revised narration.' }) })
    expect(audio.playing.value).toBe(false)
    expect(audio.currentTime.value).toBe(0)
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it('does not interrupt narration for an unrelated chapter-title update', async () => {
    const wrapper = mountPlayer()
    await wrapper.get(`button[aria-label="${reader.audio_play}"]`).trigger('click')
    audio.currentTime.value = 25
    await wrapper.setProps({ chunk: chunk({ chapterTitle: 'Renamed chapter' }) })
    expect(audio.playing.value).toBe(true)
    expect(audio.currentTime.value).toBe(25)
  })

  it('renders percentage and buffering values without multiplying progress twice', async () => {
    const wrapper = mountPlayer()
    audio.status.value = 'loading'
    audio.downloadProgress.value = 100
    await nextTick()
    expect(wrapper.text()).toContain('100%')
    expect(wrapper.text()).not.toContain('10000%')
    audio.downloadProgress.value = 0
    audio.synthTotal.value = 6
    audio.targetBufferCount.value = 2
    audio.synthIndex.value = 1
    await nextTick()
    expect(wrapper.text()).toContain('1/2')
  })

  it('disables Cloud at the quota guard without disabling Auto or System', () => {
    audio.isNearQuota.value = true
    const wrapper = mountPlayer()
    expect(wrapper.get('button[aria-label="Cloud"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('button[aria-label="Auto"]').attributes('disabled')).toBeUndefined()
    expect(wrapper.get('button[aria-label="System"]').attributes('disabled')).toBeUndefined()
  })

  it('shows a diagnostic error and explicit recovery without reporting completion', async () => {
    const wrapper = mountPlayer()
    audio.status.value = 'error'
    audio.engineMode.value = 'device'
    audio.errorInfo.value = { code: 'DEVICE_INIT_FAILED', rawMessage: 'Model unavailable', suggestCloudFallback: true }
    await nextTick()
    expect(wrapper.find('[role="alert"]').exists()).toBe(true)
    expect(wrapper.find(`button[aria-label="${reader.audio_fallback_to_cloud}"]`).exists()).toBe(true)
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it('reports the actual fallback target, reason, and restart rather than claiming Cloud playback', async () => {
    mountPlayer()
    events.fallback?.({ from: 'system', to: 'device', reason: 'voice-unavailable' })
    await nextTick()
    const notice = useToast().toasts.value.at(-1)
    expect(notice?.type).toBe('info')
    expect(notice?.message).toContain(reader.audio_engine_device)
    expect(notice?.message).toContain(reader['audio_fallback_reason_voice-unavailable'])
    expect(notice?.message).toContain('restarts')
    expect(notice?.message).not.toContain('Cloud')
  })

  it('distinguishes restored progress from unavailable cross-reload persistence', async () => {
    const wrapper = mountPlayer()
    audio.restoredProgress.value = true
    await nextTick()
    expect(wrapper.get('[role="status"]').text()).toContain(reader.audio_resumed)
    audio.persistenceAvailable.value = false
    events.persistence?.()
    await nextTick()
    expect(wrapper.get('[role="status"]').text()).toContain(reader.audio_session_only)
    expect(useToast().toasts.value.at(-1)?.message).toContain('cannot be saved')
  })

  it('allows Auto Next to be toggled without advancing the slice', async () => {
    const wrapper = mountPlayer()
    const button = wrapper.get(`button[aria-label="${reader.audio_auto_advance}"]`)
    await button.trigger('click')
    expect(button.attributes('aria-pressed')).toBe('false')
    await completeSlice()
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it('advances the standalone reader only after whole-slice completion', async () => {
    const wrapper = mountPlayer()
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
    await completeSlice()
    const [navigation] = wrapper.emitted<[AudioNavigationRequest]>('auto-advance')?.[0] ?? []
    expect(navigation?.isCurrent()).toBe(true)
  })

  it('revokes emitted continuation permission if paused while the next source is loading', async () => {
    const wrapper = mountPlayer()
    await completeSlice()
    const [navigation] = wrapper.emitted<[AudioNavigationRequest]>('auto-advance')?.[0] ?? []
    expect(navigation?.isCurrent()).toBe(true)
    wrapper.vm.pause()
    expect(navigation?.isCurrent()).toBe(false)
  })

  it('revokes emitted track navigation permission after a media-session pause', async () => {
    const wrapper = mountPlayer()
    events.next?.(() => true)
    const [, navigation] = wrapper.emitted<['next' | 'prev', AudioNavigationRequest]>('seek-slice')?.[0] ?? []
    expect(navigation?.isCurrent()).toBe(true)
    audio.pause()
    await nextTick()
    expect(navigation?.isCurrent()).toBe(false)
    expect(wrapper.get('button').attributes('aria-label')).toBe(reader.audio_play)
  })

  it('does not turn a canceled continuation into autoplay of a manually selected source', async () => {
    const wrapper = mountPlayer()
    await completeSlice()
    await wrapper.get(`button[aria-label="${reader.audio_auto_advance}"]`).trigger('click')
    await wrapper.setProps({ chunk: chunk({ id: 'manually-selected-slice' }) })
    expect(audio.playing.value).toBe(false)
  })

  it('cancels pending continuation from the playback button without replaying the completed slice', async () => {
    const wrapper = mountPlayer()
    await completeSlice()
    await wrapper.get(`button[aria-label="${reader.audio_pause_desc}"]`).trigger('click')
    expect(audio.playing.value).toBe(false)
  })

  it('keeps Today remote navigation unavailable without arming a pending transition', async () => {
    const wrapper = mountPlayer({ chunk: chunk(), disableAutoAdvance: true })
    events.next?.(() => true)
    events.previous?.(() => true)
    await nextTick()
    expect(wrapper.get('button').attributes('aria-label')).toBe(reader.audio_play)
    expect(wrapper.emitted('seek-slice')).toBeUndefined()
  })

  it('autoplays only the accepted destination after a genuine completion', async () => {
    const wrapper = mountPlayer()
    await completeSlice()
    const [navigation] = wrapper.emitted<[AudioNavigationRequest]>('auto-advance')?.[0] ?? []
    expect(navigation?.accept('accepted-slice')).toBe(true)
    await wrapper.setProps({ chunk: chunk({ id: 'accepted-slice' }) })
    expect(audio.playing.value).toBe(true)
  })

  it('does not apply accepted playback intent to another destination', async () => {
    const wrapper = mountPlayer()
    await completeSlice()
    const [navigation] = wrapper.emitted<[AudioNavigationRequest]>('auto-advance')?.[0] ?? []
    expect(navigation?.accept('accepted-slice')).toBe(true)
    await wrapper.setProps({ chunk: chunk({ id: 'another-slice' }) })
    expect(audio.playing.value).toBe(false)
  })

  it('allows explicit remote Play after a paused track change without autoplaying the destination', async () => {
    const wrapper = mountPlayer()
    events.next?.(() => true)
    const [, navigation] = wrapper.emitted<['next' | 'prev', AudioNavigationRequest]>('seek-slice')?.[0] ?? []
    expect(navigation?.accept('paused-destination')).toBe(true)
    await wrapper.setProps({ chunk: chunk({ id: 'paused-destination' }) })
    expect(audio.playing.value).toBe(false)
    await wrapper.vm.play()
    expect(audio.playing.value).toBe(true)
  })

  it('never advances Today from audio completion even when the saved Auto Next preference is enabled', async () => {
    const wrapper = mountPlayer({ chunk: chunk(), disableAutoAdvance: true })
    await completeSlice()
    expect(wrapper.find(`button[aria-label="${reader.audio_auto_advance}"]`).exists()).toBe(false)
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it('does not advance a replaced source after a pending transition chime', async () => {
    const { promise, resolve: finish } = Promise.withResolvers<void>()
    vi.mocked(audio.playSliceTransitionChime).mockReturnValueOnce(promise)
    const wrapper = mountPlayer()
    events.ended?.(() => true)
    await wrapper.setProps({ chunk: chunk({ id: 'chunk-2' }) })
    finish()
    await nextTick()
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
    expect(audio.playing.value).toBe(false)
  })

  it('cancels pending natural continuation when the user pauses', async () => {
    const { promise, resolve: finish } = Promise.withResolvers<void>()
    vi.mocked(audio.playSliceTransitionChime).mockReturnValueOnce(promise)
    const wrapper = mountPlayer()
    events.ended?.(() => true)
    wrapper.vm.pause()
    finish()
    await nextTick()
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it('allows an unchanged narration identity to advance despite a chapter-title update', async () => {
    const { promise, resolve: finish } = Promise.withResolvers<void>()
    vi.mocked(audio.playSliceTransitionChime).mockReturnValueOnce(promise)
    const wrapper = mountPlayer()
    events.ended?.(() => true)
    await wrapper.setProps({ chunk: chunk({ chapterTitle: 'Renamed chapter' }) })
    finish()
    await nextTick()
    const [navigation] = wrapper.emitted<[AudioNavigationRequest]>('auto-advance')?.[0] ?? []
    expect(navigation?.isCurrent()).toBe(true)
  })

  it('rejects completion when its playback operation is no longer current', async () => {
    const { promise, resolve: finish } = Promise.withResolvers<void>()
    vi.mocked(audio.playSliceTransitionChime).mockReturnValueOnce(promise)
    const wrapper = mountPlayer()
    let current = true
    events.ended?.(() => current)
    current = false
    finish()
    await nextTick()
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it('rejects completion after account admission changes during the chime', async () => {
    const { promise, resolve: finish } = Promise.withResolvers<void>()
    vi.mocked(audio.playSliceTransitionChime).mockReturnValueOnce(promise)
    const wrapper = mountPlayer()
    events.ended?.(() => true)
    useAuthStore().sessionGeneration++
    finish()
    await nextTick()
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it.each([['en', english.reader], ['vi', vietnamese.reader]] as const)('includes the active sleep setting in its %s accessible name', async (_locale, messages) => {
    reader = messages
    const wrapper = mountPlayer()
    await wrapper.get(`button[aria-label="${messages.audio_sleep_timer}"]`).trigger('click')
    await wrapper.findAll('button').find(button => button.text() === messages.audio_sleep_timer_15m)!.trigger('click')
    expect(wrapper.find(`button[aria-label="${messages.audio_sleep_timer}: 15:00"]`).exists()).toBe(true)
    await wrapper.find(`button[aria-label="${messages.audio_sleep_timer}: 15:00"]`).trigger('click')
    await wrapper.findAll('button').find(button => button.text() === messages.audio_sleep_timer_end_of_slice)!.trigger('click')
    expect(wrapper.find(`button[aria-label="${messages.audio_sleep_timer}: ${messages.audio_sleep_timer_end_of_slice}"]`).exists()).toBe(true)
  })

  it('allows a newer track request to supersede an older pending navigation', () => {
    const wrapper = mountPlayer()
    events.next?.(() => true)
    const [, first] = wrapper.emitted<['next' | 'prev', AudioNavigationRequest]>('seek-slice')?.[0] ?? []
    expect(first?.isCurrent()).toBe(true)
    events.previous?.(() => true)
    const [, second] = wrapper.emitted<['next' | 'prev', AudioNavigationRequest]>('seek-slice')?.[1] ?? []
    expect(first?.isCurrent()).toBe(false)
    expect(second?.isCurrent()).toBe(true)
  })

  it('pauses and restores volume when the chosen sleep timer expires', async () => {
    vi.useFakeTimers()
    const wrapper = mountPlayer()
    audio.playing.value = true
    await wrapper.get(`button[aria-label="${reader.audio_sleep_timer}"]`).trigger('click')
    const option = wrapper.findAll('button').find(button => button.text() === reader.audio_sleep_timer_15m)
    await option!.trigger('click')
    await vi.advanceTimersByTimeAsync(886_000)
    expect(audio.volume.value).toBeGreaterThan(0)
    expect(audio.volume.value).toBeLessThan(1)
    await vi.advanceTimersByTimeAsync(14_000)
    expect(audio.playing.value).toBe(false)
    expect(audio.volume.value).toBe(1)
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })

  it('stops at the slice boundary when that sleep timer is selected', async () => {
    const wrapper = mountPlayer()
    await wrapper.get(`button[aria-label="${reader.audio_sleep_timer}"]`).trigger('click')
    const option = wrapper.findAll('button').find(button => button.text() === reader.audio_sleep_timer_end_of_slice)
    await option!.trigger('click')
    await completeSlice()
    expect(audio.playing.value).toBe(false)
    expect(wrapper.emitted('auto-advance')).toBeUndefined()
  })
})

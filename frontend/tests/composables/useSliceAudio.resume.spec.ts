import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { reactive, ref } from 'vue'
import { useSliceAudio, type NarrationSource, type SliceAudioDeps, type SynthHandlers, type TtsEngine } from '~/composables/useSliceAudio'
import { buildAudioKey, type PartialSliceAudio, type SliceAudioCache } from '~/utils/sliceAudioCache'
import { computeContentHash, extractNarrationScript } from '~/utils/narrationScript'
import { resolveVoiceForLanguage } from '~/utils/ttsVoices'

const PREFERENCE_KEY = 'deeppace_reader_audio_preference'
const PROGRESS_KEY = 'deeppace_reader_audio_progress'
const CLOUD_VOICE = 'en-US-Neural2-F'
const ACCOUNT = { accountId: 'account-a', sessionGeneration: 1 }
const src: NarrationSource = { chunkId: 'slice-a', markdown: 'First sentence. Second sentence. Third sentence.', language: 'en', isAiFormatted: true }

class FakeUtterance {
  voice: SpeechSynthesisVoice | null = null
  lang = ''
  rate = 1
  pitch = 1
  volume = 1
  onstart: SpeechSynthesisUtterance['onstart'] = null
  onend: SpeechSynthesisUtterance['onend'] = null
  onerror: SpeechSynthesisUtterance['onerror'] = null
  onpause: SpeechSynthesisUtterance['onpause'] = null
  onresume: SpeechSynthesisUtterance['onresume'] = null
  constructor(public text: string) {}
}

function speechFixture(initialLanguage?: string) {
  const events = new EventTarget()
  let voices = initialLanguage ? [voice(initialLanguage)] : []
  const utterances: SpeechSynthesisUtterance[] = []
  const synth = {
    getVoices: vi.fn(() => voices),
    addEventListener: vi.fn(events.addEventListener.bind(events)),
    removeEventListener: vi.fn(events.removeEventListener.bind(events)),
    speak: vi.fn((utterance: SpeechSynthesisUtterance) => {
      utterances.push(utterance)
      utterance.onstart?.call(utterance, new Event('start') as SpeechSynthesisEvent)
    }),
    cancel: vi.fn(), pause: vi.fn(), resume: vi.fn(),
  } as unknown as SpeechSynthesis
  return {
    synth, utterances,
    reveal(language: string) {
      voices = [voice(language)]
      events.dispatchEvent(new Event('voiceschanged'))
    },
  }
}

function voice(language: string) {
  return { voiceURI: `system-${language}`, name: language, lang: language, default: true } as SpeechSynthesisVoice
}

function mediaFixture() {
  const listeners = new Map<string, Set<EventListener>>()
  let mediaSrc = ''
  const fire = (type: string) => {
    for (const listener of [...(listeners.get(type) ?? [])]) listener(new Event(type))
  }
  const audio = {
    get src() { return mediaSrc },
    set src(value: string) { mediaSrc = value; this.currentTime = 0; this.duration = NaN; this.readyState = 0 },
    currentTime: 0, duration: NaN, readyState: 0, paused: true, ended: false,
    playbackRate: 1, volume: 1, loop: false,
    addEventListener(type: string, listener: EventListener) {
      if (!listeners.has(type)) listeners.set(type, new Set())
      listeners.get(type)!.add(listener)
    },
    removeEventListener(type: string, listener: EventListener) { listeners.get(type)?.delete(listener) },
    removeAttribute: vi.fn(), load: vi.fn(),
    play: vi.fn(async () => { audio.paused = false; audio.ended = false; fire('play') }),
    pause: vi.fn(() => { audio.paused = true; fire('pause') }),
  }
  return {
    audio: audio as unknown as HTMLAudioElement,
    fire,
    callbacks(type: string) { return [...(listeners.get(type) ?? [])] },
    metadata(duration = 120) { audio.duration = duration; audio.readyState = 1; fire('loadedmetadata'); fire('durationchange') },
    end() { audio.paused = true; audio.ended = true; fire('pause'); fire('ended') },
  }
}

function cacheFixture() {
  const complete = new Map<string, Blob>()
  const partial = new Map<string, PartialSliceAudio>()
  const cache: SliceAudioCache = {
    get: vi.fn(async key => complete.get(key)),
    set: vi.fn(async (key, blob) => { complete.set(key, blob) }),
    getPartial: vi.fn(async key => partial.get(key)),
    savePartial: vi.fn(async (key, value) => { partial.set(key, value) }),
    deletePartial: vi.fn(async key => { partial.delete(key) }),
  }
  return { cache, complete, partial }
}

function pendingEngine() {
  const done = Promise.withResolvers<void>()
  void done.promise.catch(() => {}) // A deliberately delayed worker may be rejected before its request is admitted.
  let handlers: SynthHandlers | undefined
  const engine: TtsEngine = {
    synthesize: vi.fn(async (_model, _sentences, callbacks) => { handlers = callbacks; await done.promise }),
    cancel: vi.fn(), dispose: vi.fn(),
  }
  return { engine, done, chunk(seconds = 1) { handlers!.onChunk(new Float32Array(16000 * seconds), 16000) } }
}

async function flush() {
  for (let i = 0; i < 15; i++) await Promise.resolve()
}

async function identity(engine: 'system' | 'cloud' | 'device', voiceId: string, source = src) {
  return { accountId: ACCOUNT.accountId, chunkId: source.chunkId, engine, voiceId, contentHash: await computeContentHash(extractNarrationScript(source.markdown)) }
}

async function seed(engine: 'system' | 'cloud' | 'device', voiceId: string, cursor: Record<string, unknown>, source = src, completed = false) {
  const record = { version: 1, identity: await identity(engine, voiceId, source), cursor, completed, updatedAt: 1 }
  localStorage.setItem(PROGRESS_KEY, JSON.stringify({ version: 1, checkpoints: [record] }))
  return record.identity
}

function stored() {
  return JSON.parse(localStorage.getItem(PROGRESS_KEY) ?? '{"checkpoints":[]}').checkpoints as Array<{ identity: { accountId: string; engine: string }; cursor: Record<string, unknown>; completed: boolean }>
}

const players: ReturnType<typeof useSliceAudio>[] = []
function player(deps: SliceAudioDeps = {}) {
  const value = useSliceAudio({ getAccount: () => ACCOUNT, ...deps })
  value.audioQuota.value = { monthlyLimit: 950000, usedCharacters: 0, remainingCharacters: 950000, isNearLimit: false, isExhausted: false }
  players.push(value)
  return value
}

beforeEach(() => {
  localStorage.clear()
  vi.stubGlobal('SpeechSynthesisUtterance', FakeUtterance)
  URL.createObjectURL = vi.fn(() => `blob:${Math.random()}`)
  URL.revokeObjectURL = vi.fn()
})
afterEach(() => {
  for (const value of players.splice(0)) value.dispose()
  vi.useRealTimers()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('System-first narration policy and request ownership', () => {
  it('honors a canonical explicit Cloud choice without attempting matching System speech', async () => {
    localStorage.setItem(PREFERENCE_KEY, JSON.stringify({ version: 1, mode: 'cloud' }))
    const speech = speechFixture('en-US')
    const fetchClient = vi.fn(async () => new Response(new Blob(['cloud'])))
    const value = player({ speechSynthesis: speech.synth, fetchClient, cache: cacheFixture().cache, createAudio: () => mediaFixture().audio })
    await value.loadAndPlay(src)
    expect(value.enginePreference.value).toBe('cloud')
    expect(value.engineMode.value).toBe('cloud')
    expect(speech.synth.speak).not.toHaveBeenCalled()
    expect(fetchClient).toHaveBeenCalledWith(expect.stringContaining('/chunks/slice-a/audio'), expect.objectContaining({ method: 'POST' }))
  })

  it('queries quota only when uncached Cloud becomes a candidate', async () => {
    const fetchClient = vi.fn(async (input: RequestInfo | URL) => String(input).endsWith('/quota')
      ? Response.json({ monthlyLimit: 950000, usedCharacters: 0, remainingCharacters: 950000, isNearLimit: false, isExhausted: false })
      : new Response(new Blob(['cloud'])))
    const value = player({ defaultEngine: 'cloud', fetchClient, cache: cacheFixture().cache, createAudio: () => mediaFixture().audio })
    value.audioQuota.value = null
    await value.loadAndPlay(src)
    expect(fetchClient.mock.calls.map(call => call[0])).toEqual(['/api/v1/library/audio/quota', '/api/v1/library/chunks/slice-a/audio'])
  })

  it.each([400, 401, 403, 422])('surfaces quota HTTP %s without synthesis or downstream fallback', async (status) => {
    const local = pendingEngine()
    const fetchClient = vi.fn(async () => new Response(null, { status }))
    const onFallback = vi.fn()
    const value = player({ defaultEngine: 'cloud', fetchClient, onFallback, engine: local.engine, cache: cacheFixture().cache })
    value.audioQuota.value = null
    await value.loadAndPlay(src)
    expect(value.status.value).toBe('error')
    expect(fetchClient.mock.calls.map(call => call[0])).toEqual(['/api/v1/library/audio/quota'])
    expect(local.engine.synthesize).not.toHaveBeenCalled()
    expect(onFallback).not.toHaveBeenCalled()
  })

  it('retries failed Cloud preparation when remote Play explicitly requests recovery', async () => {
    const media = mediaFixture()
    const fetchClient = vi.fn().mockResolvedValueOnce(new Response(null, { status: 403 })).mockResolvedValueOnce(new Response(new Blob(['recovered audio'])))
    const value = player({ defaultEngine: 'cloud', fetchClient, cache: cacheFixture().cache, createAudio: () => media.audio })
    await value.loadAndPlay(src)
    expect(value.status.value).toBe('error')
    await value.play()
    media.metadata()
    await flush()
    expect(fetchClient).toHaveBeenCalledTimes(2)
    expect(value.status.value).toBe('ready')
    expect(media.audio.play).toHaveBeenCalledTimes(1)
  })

  it('does not reinterpret the next slice as restore after redundant System to Auto selection', async () => {
    const nextSource = { ...src, chunkId: 'next-slice' }
    await seed('system', 'system-en-US', { kind: 'system', sentenceIndex: 1 }, nextSource)
    const speech = speechFixture('en-US')
    const value = player({ defaultEngine: 'system', speechSynthesis: speech.synth, cache: cacheFixture().cache, createAudio: () => mediaFixture().audio })
    await value.loadAndPlay(src)
    value.setEnginePreference('auto')
    await value.loadAndPlay(nextSource, { reason: 'auto-advance' })
    expect(speech.utterances.at(-1)?.text).toBe('First sentence.')
    expect(value.restoredProgress.value).toBe(false)
  })

  it.each(['en-US', 'vi-VN'])('waits for %s voices without Cloud quota or model work', async (language) => {
    vi.useFakeTimers()
    const speech = speechFixture()
    const media = mediaFixture()
    const fetchClient = vi.fn()
    const local = pendingEngine()
    const value = player({ speechSynthesis: speech.synth, fetchClient, engine: local.engine, cache: cacheFixture().cache, createAudio: () => media.audio })
    value.audioQuota.value = null
    const loading = value.loadAndPlay({ ...src, language: language.slice(0, 2) })
    await vi.waitFor(() => expect(speech.synth.addEventListener).toHaveBeenCalledTimes(2))
    await vi.advanceTimersByTimeAsync(700)
    expect(fetchClient).not.toHaveBeenCalled()
    expect(local.engine.synthesize).not.toHaveBeenCalled()
    speech.reveal(language)
    await loading
    expect(value.enginePreference.value).toBe('auto')
    expect(value.engineMode.value).toBe('system')
    expect(speech.utterances).toHaveLength(1)
    expect(fetchClient).not.toHaveBeenCalled()
    expect(local.engine.synthesize).not.toHaveBeenCalled()
  })

  it('keeps Auto after runtime fallback and retries System in a later playback session', async () => {
    const speech = speechFixture('en-US')
    const fetchClient = vi.fn(async () => new Response(new Blob(['cloud']), { status: 200 }))
    const media = mediaFixture()
    const onFallback = vi.fn()
    const deps = { speechSynthesis: speech.synth, fetchClient, cache: cacheFixture().cache, createAudio: () => media.audio, onFallback }
    const value = player(deps)
    await value.loadAndPlay(src)
    const utterance = speech.utterances[0]!
    utterance.onerror?.call(utterance, { error: 'synthesis-failed' } as SpeechSynthesisErrorEvent)
    await flush()
    expect(value.engineMode.value).toBe('cloud')
    expect(value.enginePreference.value).toBe('auto')
    expect(localStorage.getItem(PREFERENCE_KEY)).toBeNull()
    expect(onFallback).toHaveBeenCalledWith({ from: 'system', to: 'cloud', reason: 'system-failed' })
    value.dispose()
    const reopened = player(deps)
    await reopened.loadAndPlay(src)
    expect(reopened.engineMode.value).toBe('system')
  })

  it('cancels voice discovery when the source is reset without starting a downstream engine', async () => {
    vi.useFakeTimers()
    const speech = speechFixture()
    const fetchClient = vi.fn()
    const value = player({ speechSynthesis: speech.synth, fetchClient, cache: cacheFixture().cache })
    const loading = value.loadAndPlay(src)
    await vi.waitFor(() => expect(speech.synth.addEventListener).toHaveBeenCalledTimes(2))
    value.resetSource()
    speech.reveal('en-US')
    await vi.advanceTimersByTimeAsync(1600)
    await loading
    expect(speech.synth.speak).not.toHaveBeenCalled()
    expect(fetchClient).not.toHaveBeenCalled()
    expect(speech.synth.removeEventListener).toHaveBeenCalledWith('voiceschanged', expect.any(Function))
  })

  it.each([400, 401, 403, 422])('keeps Cloud %s visible without Local fallback', async (status) => {
    const local = pendingEngine()
    const value = player({ defaultEngine: 'cloud', engine: local.engine, cache: cacheFixture().cache, fetchClient: async () => new Response(null, { status }) })
    await value.loadAndPlay(src)
    expect(value.status.value).toBe('error')
    expect(local.engine.synthesize).not.toHaveBeenCalled()
  })

  it('falls downstream only once after Cloud connectivity failure, preserving explicit preference', async () => {
    const local = pendingEngine()
    const fetchClient = vi.fn(async () => { throw new TypeError('Failed to fetch') })
    const value = player({ defaultEngine: 'cloud', engine: local.engine, fetchClient, cache: cacheFixture().cache })
    const loading = value.loadAndPlay(src)
    await vi.waitFor(() => expect(local.engine.synthesize).toHaveBeenCalledTimes(1))
    local.done.reject(new Error('model unavailable offline'))
    await loading
    expect(value.status.value).toBe('error')
    expect(fetchClient).toHaveBeenCalledTimes(1)
    expect(value.enginePreference.value).toBe('cloud')
  })

  it.each(['success', 'failure'])('fences late Cloud %s for a superseded request with the same cache key', async (outcome) => {
    const pending = Promise.withResolvers<Response>()
    void pending.promise.catch(() => {}) // Keep deliberate backend rejection observed even if the obsolete request is not admitted.
    const media = mediaFixture()
    const fetchClient = vi.fn().mockImplementationOnce(() => pending.promise).mockResolvedValue(new Response(new Blob(['new'])))
    const onFallback = vi.fn()
    const value = player({ defaultEngine: 'cloud', fetchClient, onFallback, cache: cacheFixture().cache, createAudio: () => media.audio })
    const old = value.loadAndPlay(src)
    await vi.waitFor(() => expect(fetchClient).toHaveBeenCalledTimes(1))
    const current = value.loadAndPlay(src)
    await current
    media.metadata()
    await flush()
    const playCount = vi.mocked(media.audio.play).mock.calls.length
    expect(fetchClient.mock.calls[0]?.[1]?.signal?.aborted).toBe(true)
    if (outcome === 'success') pending.resolve(new Response(new Blob(['old'])))
    else pending.reject(new TypeError('Failed to fetch'))
    await old
    expect(vi.mocked(media.audio.play).mock.calls).toHaveLength(playCount)
    expect(onFallback).not.toHaveBeenCalled()
    expect(value.status.value).toBe('ready')
  })

  it('rejects old utterance start/end/error after seeking within the current session', async () => {
    const speech = speechFixture('en-US')
    const onSliceEnded = vi.fn()
    const fetchClient = vi.fn()
    const value = player({ defaultEngine: 'system', speechSynthesis: speech.synth, fetchClient, cache: cacheFixture().cache, createAudio: () => mediaFixture().audio, onSliceEnded })
    await value.loadAndPlay(src)
    const obsolete = speech.utterances[0]!
    value.seek(value.duration.value * 0.9)
    const spoken = speech.utterances.length
    const cursor = value.currentTime.value
    obsolete.onstart?.call(obsolete, new Event('start') as SpeechSynthesisEvent)
    obsolete.onend?.call(obsolete, new Event('end') as SpeechSynthesisEvent)
    obsolete.onerror?.call(obsolete, { error: 'synthesis-failed' } as SpeechSynthesisErrorEvent)
    await flush()
    expect(speech.utterances).toHaveLength(spoken)
    expect(value.currentTime.value).toBe(cursor)
    expect(fetchClient).not.toHaveBeenCalled()
    expect(onSliceEnded).not.toHaveBeenCalled()
  })

  it('keeps native fragment pause-before-ended as buffering intent, not user pause or completion', async () => {
    const local = pendingEngine()
    const media = mediaFixture()
    const onSliceEnded = vi.fn()
    const value = player({ defaultEngine: 'device', engine: local.engine, cache: cacheFixture().cache, createAudio: () => media.audio, onSliceEnded })
    const loading = value.loadAndPlay(src)
    await vi.waitFor(() => expect(local.engine.synthesize).toHaveBeenCalledTimes(1))
    local.chunk(2); local.chunk(3)
    media.metadata(5)
    await flush()
    media.end()
    expect(value.status.value).toBe('loading')
    expect(value.playing.value).toBe(true)
    expect(onSliceEnded).not.toHaveBeenCalled()
    local.chunk(4)
    media.metadata(4)
    await flush()
    expect(value.playing.value).toBe(true)
    expect(onSliceEnded).not.toHaveBeenCalled()
    local.done.resolve()
    await loading
  })

  it.each(['success', 'network failure'])('does not autoplay or fall back after pause during pending Cloud %s', async (outcome) => {
    const response = Promise.withResolvers<Response>()
    void response.promise.catch(() => {}) // The pause branch can abort before the transport observes this response.
    const local = pendingEngine()
    const media = mediaFixture()
    const onFallback = vi.fn()
    const fetchClient = vi.fn(() => response.promise)
    const value = player({ defaultEngine: 'cloud', engine: local.engine, onFallback, fetchClient, cache: cacheFixture().cache, createAudio: () => media.audio })
    const loading = value.loadAndPlay(src)
    await vi.waitFor(() => expect(fetchClient).toHaveBeenCalledTimes(1))
    value.pause()
    if (outcome === 'success') response.resolve(new Response(new Blob(['cloud'])))
    else response.reject(new TypeError('Failed to fetch'))
    await loading
    media.metadata()
    await flush()
    expect(media.audio.play).not.toHaveBeenCalled()
    expect(local.engine.synthesize).not.toHaveBeenCalled()
    expect(onFallback).not.toHaveBeenCalled()
    expect(value.playing.value).toBe(false)
  })

  it('can resume after pausing Local preparation before a pipeline owns it', async () => {
    const local = pendingEngine()
    const media = mediaFixture()
    const value = player({ defaultEngine: 'device', engine: local.engine, cache: cacheFixture().cache, createAudio: () => media.audio })
    const preparing = value.loadAndPlay(src)
    value.pause()
    await preparing
    const resumed = value.play()
    await vi.waitFor(() => expect(local.engine.synthesize).toHaveBeenCalledTimes(1))
    local.chunk(1); local.chunk(1); local.chunk(1)
    local.done.resolve()
    await resumed
    media.metadata(3)
    await flush()
    expect(value.playing.value).toBe(true)
    expect(media.audio.play).toHaveBeenCalled()
  })

  it('keeps usable System speech advancing when the optional silent carrier cannot play', async () => {
    const speech = speechFixture('en-US')
    const media = mediaFixture()
    vi.mocked(media.audio.play).mockRejectedValue(new DOMException('No supported carrier source', 'NotSupportedError'))
    const onSliceEnded = vi.fn()
    const fetchClient = vi.fn()
    const value = player({ defaultEngine: 'system', speechSynthesis: speech.synth, fetchClient, cache: cacheFixture().cache, createAudio: () => media.audio, onSliceEnded })
    await value.loadAndPlay({ ...src, markdown: 'First sentence. Final sentence.' })
    await flush()
    const first = speech.utterances[0]!
    first.onend?.call(first, new Event('end') as SpeechSynthesisEvent)
    await flush()
    expect(speech.utterances.at(-1)?.text).toBe('Final sentence.')
    expect(value.playing.value).toBe(true)
    const last = speech.utterances.at(-1)!
    last.onend?.call(last, new Event('end') as SpeechSynthesisEvent)
    expect(onSliceEnded).toHaveBeenCalledTimes(1)
    expect(fetchClient).not.toHaveBeenCalled()
  })

  it('fences pending Cloud playback and writes on account/session replacement', async () => {
    const account = reactive({ ...ACCOUNT })
    const response = Promise.withResolvers<Response>()
    const media = mediaFixture()
    const fetchClient = vi.fn(() => response.promise)
    const value = player({ defaultEngine: 'cloud', getAccount: () => account, fetchClient, cache: cacheFixture().cache, createAudio: () => media.audio })
    const loading = value.loadAndPlay(src)
    await vi.waitFor(() => expect(fetchClient).toHaveBeenCalledTimes(1))
    account.accountId = 'account-b'
    account.sessionGeneration++
    response.resolve(new Response(new Blob(['old account'])))
    await loading
    media.metadata()
    media.fire('timeupdate')
    window.dispatchEvent(new Event('pagehide'))
    expect(media.audio.play).not.toHaveBeenCalled()
    expect(stored()).toEqual([])
    expect(value.status.value).toBe('idle')
  })

  it('does not turn a System permission rejection into paid or Local fallback', async () => {
    const speech = speechFixture('en-US')
    const fetchClient = vi.fn()
    const onFallback = vi.fn()
    const value = player({ defaultEngine: 'system', speechSynthesis: speech.synth, fetchClient, onFallback, cache: cacheFixture().cache, createAudio: () => mediaFixture().audio })
    await value.loadAndPlay(src)
    const utterance = speech.utterances[0]!
    utterance.onerror?.call(utterance, { error: 'not-allowed' } as SpeechSynthesisErrorEvent)
    await flush()
    expect(fetchClient).not.toHaveBeenCalled()
    expect(onFallback).not.toHaveBeenCalled()
    expect(value.playing.value).toBe(false)
  })

  it('keeps a same-account refresh playing but invalidates quota for a replacement admission', async () => {
    const admission = ref({ ...ACCOUNT })
    const speech = speechFixture('en-US')
    const value = player({ defaultEngine: 'system', getAccount: () => admission.value, speechSynthesis: speech.synth, cache: cacheFixture().cache, createAudio: () => mediaFixture().audio })
    await value.loadAndPlay(src)
    const cancellations = vi.mocked(speech.synth.cancel).mock.calls.length
    admission.value = { ...ACCOUNT }
    expect(value.playing.value).toBe(true)
    expect(speech.synth.cancel).toHaveBeenCalledTimes(cancellations)
    value.audioQuota.value = { monthlyLimit: 950000, usedCharacters: 950000, remainingCharacters: 0, isNearLimit: true, isExhausted: true }
    admission.value = { accountId: 'account-b', sessionGeneration: 2 }
    expect(value.playing.value).toBe(false)
    expect(value.audioQuota.value).toBeNull()
  })

  it.each(['play', 'pause', 'seek', 'source', 'account'])('revokes a completed continuation after %s supersedes it', async (action) => {
    const speech = speechFixture('en-US')
    const account = reactive({ ...ACCOUNT })
    const onSliceEnded = vi.fn()
    const value = player({ defaultEngine: 'system', getAccount: () => account, speechSynthesis: speech.synth, cache: cacheFixture().cache, createAudio: () => mediaFixture().audio, onSliceEnded })
    await value.loadAndPlay({ ...src, markdown: 'The entire slice.' })
    const utterance = speech.utterances[0]!
    utterance.onend?.call(utterance, new Event('end') as SpeechSynthesisEvent)
    const stillCurrent = onSliceEnded.mock.calls[0]?.[0]
    expect(stillCurrent?.()).toBe(true)
    if (action === 'play') await value.play()
    else if (action === 'pause') value.pause()
    else if (action === 'seek') value.seek(0)
    else if (action === 'source') value.resetSource()
    else account.sessionGeneration++
    expect(stillCurrent?.()).toBe(false)
    expect(onSliceEnded).toHaveBeenCalledTimes(1)
  })

  it.each(['system', 'cloud'] as const)('retains the source for remote Play after a paused %s voice change', async (engine) => {
    const speech = speechFixture('en-US')
    const media = mediaFixture()
    const value = player({ defaultEngine: engine, speechSynthesis: speech.synth, fetchClient: async () => new Response(new Blob(['cloud'])), cache: cacheFixture().cache, createAudio: () => media.audio })
    await value.loadAndPlay(src)
    media.metadata()
    value.pause()
    if (engine === 'system') {
      speech.reveal('en-GB')
      value.setSystemVoice('system-en-GB')
    } else value.setVoice('en-US-Neural2-D')
    expect(value.playing.value).toBe(false)
    await value.play()
    media.metadata()
    await flush()
    expect(value.playing.value).toBe(true)
    expect(value.currentTime.value).toBe(0)
    if (engine === 'system') expect(speech.utterances.at(-1)?.voice?.voiceURI).toBe('system-en-GB')
    else expect(value.selectedVoice.value).toBe('en-US-Neural2-D')
  })
})

describe('engine-local listening checkpoints', () => {
  it('restores the System sentence boundary only after an explicit playback request', async () => {
    const speech = speechFixture('en-US')
    await seed('system', 'system-en-US', { kind: 'system', sentenceIndex: 1 })
    const value = player({ defaultEngine: 'system', speechSynthesis: speech.synth, cache: cacheFixture().cache, createAudio: () => mediaFixture().audio })
    expect(speech.synth.speak).not.toHaveBeenCalled()
    await value.loadAndPlay(src)
    expect(speech.utterances[0]?.text).toBe('Second sentence.')
    expect(value.restoredProgress.value).toBe(true)
    speech.utterances[0]?.onend?.call(speech.utterances[0], new Event('end') as SpeechSynthesisEvent)
    expect(speech.utterances[1]?.text).toBe('Third sentence.')
  })

  it.each(['cloud', 'device'] as const)('restores cached %s seconds after metadata and before playback', async (engine) => {
    const voiceId = engine === 'cloud' ? CLOUD_VOICE : resolveVoiceForLanguage('en').id
    const checkpoint = await seed(engine, voiceId, { kind: 'file', seconds: 45 })
    const cache = cacheFixture()
    cache.complete.set(buildAudioKey(src.chunkId, voiceId, checkpoint.contentHash), new Blob(['cached']))
    const media = mediaFixture()
    const value = player({ defaultEngine: engine, cache: cache.cache, createAudio: () => media.audio })
    await value.loadAndPlay(src)
    expect(media.audio.currentTime).toBe(0)
    expect(media.audio.play).not.toHaveBeenCalled()
    media.metadata(100)
    await flush()
    expect(media.audio.currentTime).toBe(45)
    expect(media.audio.play).toHaveBeenCalledTimes(1)
    expect(value.restoredProgress.value).toBe(true)
  })

  it('restores initial Auto-to-Cloud after voice eligibility fails without treating discovery as engine transition', async () => {
    vi.useFakeTimers()
    const checkpoint = await seed('cloud', CLOUD_VOICE, { kind: 'file', seconds: 45 })
    const cache = cacheFixture()
    cache.complete.set(buildAudioKey(src.chunkId, CLOUD_VOICE, checkpoint.contentHash), new Blob(['cached']))
    const media = mediaFixture()
    const speech = speechFixture()
    const value = player({ speechSynthesis: speech.synth, cache: cache.cache, createAudio: () => media.audio })
    const loading = value.loadAndPlay(src)
    await vi.waitFor(() => expect(speech.synth.addEventListener).toHaveBeenCalledTimes(2))
    await vi.advanceTimersByTimeAsync(1500)
    await loading
    media.metadata()
    await flush()
    expect(value.engineMode.value).toBe('cloud')
    expect(media.audio.currentTime).toBe(45)
  })

  it('restores sentence two inside five generated Local sentences and hands off global time while paused', async () => {
    const source = { ...src, markdown: Array.from({ length: 15 }, (_, i) => `Sentence ${i + 1}.`).join(' ') }
    const voiceId = resolveVoiceForLanguage('en').id
    const checkpoint = await seed('device', voiceId, { kind: 'local', sentenceIndex: 1, sentenceSeconds: 1, globalSeconds: 3 }, source)
    const cache = cacheFixture()
    cache.partial.set(buildAudioKey(source.chunkId, voiceId, checkpoint.contentHash), { chunks: Array.from({ length: 5 }, () => new Float32Array(32000)), sampleRate: 16000, total: 15 })
    const media = mediaFixture()
    const local = pendingEngine()
    const value = player({ defaultEngine: 'device', engine: local.engine, cache: cache.cache, createAudio: () => media.audio })
    const loading = value.loadAndPlay(source)
    await vi.waitFor(() => expect(local.engine.synthesize).toHaveBeenCalledTimes(1))
    media.metadata(2)
    await flush()
    expect(media.audio.currentTime).toBe(1)
    expect(value.currentTime.value).toBe(3)
    expect(vi.mocked(local.engine.synthesize).mock.calls[0]?.[1][0]).toBe('Sentence 6.')
    media.audio.currentTime = 1.5
    media.fire('timeupdate')
    value.pause()
    expect(stored()[0]?.cursor).toEqual({ kind: 'local', sentenceIndex: 1, sentenceSeconds: 1.5, globalSeconds: 3.5 })
    const obsoleteEnd = media.callbacks('ended')
    const obsoleteMetadata = media.callbacks('loadedmetadata')
    for (let i = 0; i < 10; i++) local.chunk(2)
    local.done.resolve()
    await loading
    media.metadata(30)
    await flush()
    expect(media.audio.currentTime).toBe(3.5)
    expect(stored()[0]?.cursor).toEqual({ kind: 'file', seconds: 3.5 })
    for (const callback of [...obsoleteEnd, ...obsoleteMetadata]) callback(new Event('ended'))
    expect(media.audio.currentTime).toBe(3.5)
    expect(value.playing.value).toBe(false)
  })

  it('uses a streaming Local checkpoint global offset when the matching complete cache now exists', async () => {
    const voiceId = resolveVoiceForLanguage('en').id
    const checkpoint = await seed('device', voiceId, { kind: 'local', sentenceIndex: 1, sentenceSeconds: 1.5, globalSeconds: 3.5 })
    const cache = cacheFixture()
    cache.complete.set(buildAudioKey(src.chunkId, voiceId, checkpoint.contentHash), new Blob(['complete']))
    const media = mediaFixture()
    const value = player({ defaultEngine: 'device', cache: cache.cache, createAudio: () => media.audio })
    await value.loadAndPlay(src)
    media.metadata(10)
    await flush()
    expect(media.audio.currentTime).toBe(3.5)
  })

  it.each(['missing', 'out-of-range', 'completed', 'changed-voice', 'changed-content'])('starts Cloud from zero for a %s referenced checkpoint', async (condition) => {
    const checkpoint = await seed('cloud', condition === 'changed-voice' ? 'en-US-Neural2-D' : CLOUD_VOICE, { kind: 'file', seconds: condition === 'out-of-range' ? 200 : 45 }, condition === 'changed-content' ? { ...src, markdown: 'Old content.' } : src, condition === 'completed')
    const cache = cacheFixture()
    if (condition !== 'missing') cache.complete.set(buildAudioKey(src.chunkId, CLOUD_VOICE, checkpoint.contentHash), new Blob(['cached']))
    const media = mediaFixture()
    const value = player({ defaultEngine: 'cloud', cache: cache.cache, fetchClient: async () => new Response(new Blob(['fresh'])), createAudio: () => media.audio })
    await value.loadAndPlay(src)
    media.metadata(100)
    await flush()
    expect(media.audio.currentTime).toBe(0)
    expect(value.restoredProgress.value).toBe(false)
  })

  it('keeps pending paused engine-change restart ahead of an older target-engine checkpoint', async () => {
    const voiceId = resolveVoiceForLanguage('en').id
    const checkpoint = await seed('device', voiceId, { kind: 'file', seconds: 45 })
    const cache = cacheFixture()
    cache.complete.set(buildAudioKey(src.chunkId, voiceId, checkpoint.contentHash), new Blob(['local']))
    const media = mediaFixture()
    const value = player({ defaultEngine: 'cloud', cache: cache.cache, fetchClient: async () => new Response(new Blob(['cloud'])), createAudio: () => media.audio })
    await value.loadAndPlay(src)
    media.metadata()
    await flush()
    value.pause()
    value.setEnginePreference('device')
    expect(vi.mocked(media.audio.play)).toHaveBeenCalledTimes(1)
    await value.loadAndPlay(src)
    media.metadata()
    await flush()
    expect(media.audio.currentTime).toBe(0)
    expect(value.restoredProgress.value).toBe(false)
  })

  it('retains engine restart intent if replacement cache preparation is paused before it resolves', async () => {
    const checkpoint = await seed('cloud', CLOUD_VOICE, { kind: 'file', seconds: 45 })
    const cache = cacheFixture()
    const key = buildAudioKey(src.chunkId, CLOUD_VOICE, checkpoint.contentHash)
    cache.complete.set(key, new Blob(['cloud']))
    const speech = speechFixture('en-US')
    const media = mediaFixture()
    const pending = Promise.withResolvers<Blob | undefined>()
    const originalGet = cache.cache.get
    const get = vi.fn((requested: string) => originalGet(requested))
    cache.cache.get = get
    const value = player({ defaultEngine: 'system', speechSynthesis: speech.synth, cache: cache.cache, createAudio: () => media.audio })
    await value.loadAndPlay(src)
    value.pause()
    value.setEnginePreference('cloud')
    get.mockImplementationOnce(() => pending.promise)
    const preparing = value.loadAndPlay(src)
    await vi.waitFor(() => expect(get).toHaveBeenCalledWith(key))
    value.pause()
    pending.resolve(cache.complete.get(key))
    await preparing
    await value.play()
    media.metadata(120)
    await flush()
    expect(media.audio.currentTime).toBe(0)
    expect(value.restoredProgress.value).toBe(false)
  })

  it('does not inherit an outgoing file clock while the new Local first segment is pending', async () => {
    const media = mediaFixture()
    const local = pendingEngine()
    const value = player({ defaultEngine: 'cloud', engine: local.engine, cache: cacheFixture().cache, fetchClient: async () => new Response(new Blob(['cloud'])), createAudio: () => media.audio })
    await value.loadAndPlay(src)
    media.metadata()
    await flush()
    value.seek(30)
    value.setEnginePreference('device')
    const loading = value.loadAndPlay(src)
    await vi.waitFor(() => expect(local.engine.synthesize).toHaveBeenCalledTimes(1))
    local.chunk(40)
    value.pause()
    const localRecord = stored().find(record => record.identity.engine === 'device')
    expect(localRecord?.cursor).toEqual({ kind: 'local', sentenceIndex: 0, sentenceSeconds: 0, globalSeconds: 0 })
    local.done.reject(new Error('Synthesis cancelled'))
    await loading
  })

  it('emits genuine completion once when the last Local fragment ends before synthesis finalization', async () => {
    const local = pendingEngine()
    const media = mediaFixture()
    const onSliceEnded = vi.fn()
    const value = player({ defaultEngine: 'device', engine: local.engine, cache: cacheFixture().cache, createAudio: () => media.audio, onSliceEnded })
    const loading = value.loadAndPlay(src)
    await vi.waitFor(() => expect(local.engine.synthesize).toHaveBeenCalledTimes(1))
    local.chunk(1); local.chunk(1)
    media.metadata(2)
    await flush()
    local.chunk(1)
    media.end()
    media.metadata(1)
    await flush()
    media.audio.currentTime = 1
    media.end()
    local.done.resolve()
    await loading
    expect(onSliceEnded).toHaveBeenCalledTimes(1)
    expect(media.audio.play).toHaveBeenCalledTimes(2)
    expect(stored()[0]?.completed).toBe(true)
  })

  it('does not complete Local after seeking backward while finalization is pending', async () => {
    const local = pendingEngine()
    const media = mediaFixture()
    const onSliceEnded = vi.fn()
    const value = player({ defaultEngine: 'device', engine: local.engine, cache: cacheFixture().cache, createAudio: () => media.audio, onSliceEnded })
    const loading = value.loadAndPlay(src)
    await vi.waitFor(() => expect(local.engine.synthesize).toHaveBeenCalledTimes(1))
    local.chunk(1); local.chunk(1)
    media.metadata(2)
    await flush()
    local.chunk(1)
    media.end()
    media.metadata(1)
    await flush()
    media.audio.currentTime = 1
    media.end()
    value.seek(0.5)
    media.metadata(1)
    await flush()
    local.done.resolve()
    await loading
    media.metadata(3)
    await flush()
    expect(onSliceEnded).not.toHaveBeenCalled()
    expect(value.playing.value).toBe(true)
    expect(value.currentTime.value).toBe(0.5)
    expect(stored()[0]?.completed).toBe(false)
  })

  it.each(['malformed', 'fully-generated', 'missing'] as const)('handles a %s Local partial without inventing listening progress', async (condition) => {
    const voiceId = resolveVoiceForLanguage('en').id
    const checkpoint = await seed('device', voiceId, { kind: 'local', sentenceIndex: 1, sentenceSeconds: 0.5, globalSeconds: 1.5 })
    const cache = cacheFixture()
    const key = buildAudioKey(src.chunkId, voiceId, checkpoint.contentHash)
    if (condition === 'fully-generated') cache.partial.set(key, { chunks: Array.from({ length: 3 }, () => new Float32Array(16000)), sampleRate: 16000, total: 3 })
    if (condition === 'malformed') {
      // Deliberately model an edited IndexedDB record at the runtime boundary.
      const malformed = { chunks: null, sampleRate: 16000, total: 3 } as unknown as PartialSliceAudio
      cache.partial.set(key, malformed)
    }
    const engine: TtsEngine = {
      synthesize: vi.fn(async (_model, sentences, handlers) => {
        for (const _sentence of sentences) handlers.onChunk(new Float32Array(16000), 16000)
      }),
      dispose: vi.fn(),
    }
    const media = mediaFixture()
    const value = player({ defaultEngine: 'device', engine, cache: cache.cache, createAudio: () => media.audio })
    await value.loadAndPlay(src)
    media.metadata(3)
    await flush()
    expect(media.audio.currentTime).toBe(condition === 'fully-generated' ? 1.5 : 0)
    if (condition === 'fully-generated') expect(engine.synthesize).not.toHaveBeenCalled()
    else expect(engine.synthesize).toHaveBeenCalledTimes(1)
  })

  it('preserves position on a redundant effective-engine choice and performs no idle synthesis', async () => {
    const cache = cacheFixture()
    const hash = await computeContentHash(extractNarrationScript(src.markdown))
    cache.complete.set(buildAudioKey(src.chunkId, CLOUD_VOICE, hash), new Blob(['cached']))
    const media = mediaFixture()
    const value = player({ defaultEngine: 'cloud', cache: cache.cache, createAudio: () => media.audio })
    await value.loadAndPlay(src)
    media.metadata()
    await flush()
    media.audio.currentTime = 20
    media.fire('timeupdate')
    value.pause()
    value.setEnginePreference('cloud')
    expect(media.audio.currentTime).toBe(20)
    const local = pendingEngine()
    const quiet = player({ engine: local.engine, cache: cacheFixture().cache })
    quiet.setEnginePreference('device')
    await quiet.loadAndPlay(src, { autoplay: false })
    expect(local.engine.synthesize).not.toHaveBeenCalled()
  })

  it('flushes latest backward seek, pagehide, visibility loss and periodic progress without stale account writes', async () => {
    vi.useFakeTimers()
    const cache = cacheFixture()
    const checkpoint = await seed('cloud', CLOUD_VOICE, { kind: 'file', seconds: 10 })
    cache.complete.set(buildAudioKey(src.chunkId, CLOUD_VOICE, checkpoint.contentHash), new Blob(['cached']))
    const media = mediaFixture()
    const account = reactive({ ...ACCOUNT })
    const value = player({ defaultEngine: 'cloud', getAccount: () => account, cache: cache.cache, createAudio: () => media.audio })
    await value.loadAndPlay(src)
    media.metadata()
    await flush()
    media.audio.currentTime = 30
    media.fire('timeupdate')
    await vi.advanceTimersByTimeAsync(5000)
    expect(stored()[0]?.cursor).toEqual({ kind: 'file', seconds: 30 })
    value.seek(7)
    expect(stored()[0]?.cursor).toEqual({ kind: 'file', seconds: 7 })
    media.audio.currentTime = 8
    window.dispatchEvent(new Event('pagehide'))
    expect(stored()[0]?.cursor).toEqual({ kind: 'file', seconds: 8 })
    media.audio.currentTime = 9
    vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden')
    document.dispatchEvent(new Event('visibilitychange'))
    expect(stored()[0]?.cursor).toEqual({ kind: 'file', seconds: 9 })
    account.accountId = 'account-b'; account.sessionGeneration++
    media.audio.currentTime = 50
    media.fire('timeupdate')
    await vi.advanceTimersByTimeAsync(10000)
    expect(stored().some(record => record.identity.accountId === 'account-b')).toBe(false)
    expect(stored()[0]?.cursor).toEqual({ kind: 'file', seconds: 9 })
  })

  it('continues listening and reports actual unavailable storage only once, not for an absent account', async () => {
    const onPersistenceUnavailable = vi.fn()
    const media = mediaFixture()
    const cache = cacheFixture()
    const hash = await computeContentHash(extractNarrationScript(src.markdown))
    cache.complete.set(buildAudioKey(src.chunkId, CLOUD_VOICE, hash), new Blob(['cached']))
    vi.spyOn(window.localStorage, 'getItem').mockImplementation(key => {
      if (key === PROGRESS_KEY) throw new DOMException('denied', 'SecurityError')
      return null
    })
    const value = player({ defaultEngine: 'cloud', cache: cache.cache, createAudio: () => media.audio, onPersistenceUnavailable })
    await value.loadAndPlay(src)
    media.metadata()
    await flush()
    value.pause()
    window.dispatchEvent(new Event('pagehide'))
    expect(onPersistenceUnavailable).toHaveBeenCalledTimes(1)
    expect(value.persistenceAvailable.value).toBe(false)
    expect(media.audio.play).toHaveBeenCalled()
    const anonymousNotice = vi.fn()
    const anonymous = player({ defaultEngine: 'cloud', getAccount: () => ({ accountId: null, sessionGeneration: 0 }), cache: cache.cache, createAudio: () => mediaFixture().audio, onPersistenceUnavailable: anonymousNotice })
    await anonymous.loadAndPlay(src)
    anonymous.pause()
    expect(anonymousNotice).not.toHaveBeenCalled()
  })

  it('revokes a pending track permission when native Pause leaves playback already paused', async () => {
    const actions = new Map<MediaSessionAction, MediaSessionActionHandler | null>()
    vi.stubGlobal('navigator', {
      mediaSession: {
        setActionHandler: (action: MediaSessionAction, handler: MediaSessionActionHandler | null) => actions.set(action, handler),
      },
    })
    let canNavigate: (() => boolean) | undefined
    const speech = speechFixture('en-US')
    const value = player({
      defaultEngine: 'system', speechSynthesis: speech.synth,
      onNextTrack: isCurrent => { canNavigate = isCurrent },
    })
    await value.loadAndPlay(src)
    value.pause()
    actions.get('nexttrack')?.({ action: 'nexttrack' })
    expect(canNavigate?.()).toBe(true)
    actions.get('pause')?.({ action: 'pause' })
    expect(value.playing.value).toBe(false)
    expect(canNavigate?.()).toBe(false)
  })

  it('registers a paused destination without synthesis and defers its restart until remote Play', async () => {
    await seed('system', 'system-en-US', { kind: 'system', sentenceIndex: 2 })
    const speech = speechFixture('en-US')
    const value = player({ defaultEngine: 'system', speechSynthesis: speech.synth })
    await value.loadAndPlay({ ...src, chunkId: 'previous-slice' })
    value.pause()
    await value.loadAndPlay(src, { autoplay: false, reason: 'source-change' })
    expect(value.playing.value).toBe(false)
    expect(speech.synth.speak).toHaveBeenCalledTimes(1)
    await value.play()
    expect(speech.utterances.at(-1)?.text).toBe('First sentence.')
  })
})

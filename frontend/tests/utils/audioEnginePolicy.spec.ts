import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  SYSTEM_VOICE_DISCOVERY_TIMEOUT_MS,
  discoverSystemVoices,
  engineCandidates,
  isDownstreamFailure,
} from '~/utils/audioEnginePolicy'

function speechFixture() {
  const events = new EventTarget()
  let voices: SpeechSynthesisVoice[] = []
  const synth = {
    getVoices: () => voices,
    addEventListener: vi.fn(events.addEventListener.bind(events)),
    removeEventListener: vi.fn(events.removeEventListener.bind(events)),
    onvoiceschanged: vi.fn(),
  } as unknown as SpeechSynthesis
  return {
    synth,
    reveal(language: string) {
      voices = [{ voiceURI: `voice-${language}`, name: language, lang: language, default: true }] as SpeechSynthesisVoice[]
      events.dispatchEvent(new Event('voiceschanged'))
    },
  }
}

afterEach(() => vi.useRealTimers())

describe('audio engine policy', () => {
  it('uses a finite downstream-only hierarchy for Auto and explicit preferences', () => {
    expect(engineCandidates('auto')).toEqual(['system', 'cloud', 'device'])
    expect(engineCandidates('system')).toEqual(['system', 'cloud', 'device'])
    expect(engineCandidates('cloud')).toEqual(['cloud', 'device'])
    expect(engineCandidates('device')).toEqual(['device'])
  })

  it.each(['en-US', 'vi-VN'])('waits for a delayed %s voice without replacing the shared handler', async (language) => {
    vi.useFakeTimers()
    const { synth, reveal } = speechFixture()
    const sharedHandler = synth.onvoiceschanged
    const controller = new AbortController()
    const discovery = discoverSystemVoices(synth, language.slice(0, 2), controller.signal)
    await vi.advanceTimersByTimeAsync(800)
    reveal(language)

    expect((await discovery)[0]?.lang).toBe(language)
    expect(synth.onvoiceschanged).toBe(sharedHandler)
    expect(synth.removeEventListener).toHaveBeenCalledWith('voiceschanged', expect.any(Function))
    expect(vi.getTimerCount()).toBe(0)
  })

  it('expires after the named 1500ms window and releases the listener', async () => {
    vi.useFakeTimers()
    const { synth } = speechFixture()
    expect(SYSTEM_VOICE_DISCOVERY_TIMEOUT_MS).toBe(1500)
    const discovery = discoverSystemVoices(synth, 'vi', new AbortController().signal)
    await vi.advanceTimersByTimeAsync(1500)
    expect(await discovery).toEqual([])
    expect(synth.removeEventListener).toHaveBeenCalledWith('voiceschanged', expect.any(Function))
    expect(vi.getTimerCount()).toBe(0)
  })

  it('cancels discovery and releases its timer without returning late voices', async () => {
    vi.useFakeTimers()
    const { synth, reveal } = speechFixture()
    const controller = new AbortController()
    const discovery = discoverSystemVoices(synth, 'vi', controller.signal)
    const rejection = expect(discovery).rejects.toMatchObject({ name: 'AbortError' })
    controller.abort()
    reveal('vi-VN')
    await rejection
    expect(synth.removeEventListener).toHaveBeenCalledWith('voiceschanged', expect.any(Function))
    expect(vi.getTimerCount()).toBe(0)
  })

  it.each([400, 401, 403, 404, 422])('does not disguise Cloud HTTP %s as a provider outage', (status) => {
    expect(isDownstreamFailure(Object.assign(new Error('request rejected'), { status }), 'cloud')).toBe(false)
  })

  it('distinguishes hard failures from cancellation and autoplay permission', () => {
    expect(isDownstreamFailure(new TypeError('Failed to fetch'), 'cloud')).toBe(true)
    expect(isDownstreamFailure(Object.assign(new Error('provider unavailable'), { status: 503 }), 'cloud')).toBe(true)
    expect(isDownstreamFailure(Object.assign(new Error('quota'), { status: 429 }), 'cloud')).toBe(true)
    expect(isDownstreamFailure(new Error('synthesis-failed'), 'system')).toBe(true)
    expect(isDownstreamFailure(new DOMException('aborted', 'AbortError'), 'cloud')).toBe(false)
    expect(isDownstreamFailure(new DOMException('gesture required', 'NotAllowedError'), 'cloud')).toBe(false)
    expect(isDownstreamFailure(new Error('Synthesis cancelled'), 'device')).toBe(false)
    expect(isDownstreamFailure(new Error('model unavailable'), 'device')).toBe(false)
  })
})

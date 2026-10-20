import type { AudioEnginePreference } from './clientStorage'
import type { AudioEngine, SystemVoiceOption } from '~/composables/useSliceAudio'

export const SYSTEM_VOICE_DISCOVERY_TIMEOUT_MS = 1500
export type AudioFallbackReason = 'voice-unavailable' | 'system-failed' | 'network-error' | 'provider-unavailable' | 'quota-exhausted' | 'offline'

export function engineCandidates(preference: AudioEnginePreference): AudioEngine[] {
  if (preference === 'device') return ['device']
  if (preference === 'cloud') return ['cloud', 'device']
  return ['system', 'cloud', 'device']
}

/** Discovery never owns the browser's shared onvoiceschanged property. */
export function discoverSystemVoices(synth: SpeechSynthesis | null, language: string | null | undefined, signal: AbortSignal): Promise<SystemVoiceOption[]> {
  if (signal.aborted) return Promise.reject(new DOMException('Aborted', 'AbortError'))
  if (!synth) return Promise.resolve([])
  const matching = () => {
    const prefix = (language || 'en').toLowerCase().split(/[-_]/)[0]
    return synth.getVoices().filter(voice => voice.lang.toLowerCase().split(/[-_]/)[0] === prefix)
      .map(voice => ({ id: voice.voiceURI || voice.name, name: voice.name, lang: voice.lang, default: voice.default, localService: voice.localService }))
  }
  const existing = matching()
  if (existing.length) return Promise.resolve(existing)
  const { promise, resolve, reject } = Promise.withResolvers<SystemVoiceOption[]>()
  const cleanup = () => {
    clearTimeout(timer)
    synth.removeEventListener?.('voiceschanged', changed)
    signal.removeEventListener('abort', aborted)
  }
  const finish = (voices: SystemVoiceOption[]) => { cleanup(); resolve(voices) }
  const changed = () => { const voices = matching(); if (voices.length) finish(voices) }
  const aborted = () => { cleanup(); reject(new DOMException('Aborted', 'AbortError')) }
  const timer = setTimeout(() => finish(matching()), SYSTEM_VOICE_DISCOVERY_TIMEOUT_MS)
  synth.addEventListener?.('voiceschanged', changed)
  signal.addEventListener('abort', aborted, { once: true })
  // Close the gap between the first enumeration and listener registration.
  changed()
  if (signal.aborted) aborted()
  return promise
}

export function isDownstreamFailure(error: unknown, engine: AudioEngine): boolean {
  const name = error instanceof Error ? error.name : ''
  const message = error instanceof Error ? error.message.toLowerCase() : String(error).toLowerCase()
  if (name === 'AbortError' || name === 'NotAllowedError'
    || message === 'not-allowed' || message === 'synthesis cancelled' || message === 'canceled' || message === 'interrupted') return false
  if (engine === 'device') return false
  if (engine === 'system') return true
  if (error && typeof error === 'object' && 'status' in error && typeof error.status === 'number') return error.status === 429 || error.status >= 500
  return /network|failed to fetch|load failed|timeout|offline|econnrefused|enotfound|etimedout/.test(message)
}

export function downstreamFailureReason(error: unknown, engine: AudioEngine): AudioFallbackReason {
  if (engine === 'system') return 'system-failed'
  if (error && typeof error === 'object' && 'status' in error) {
    if (error.status === 429) return 'quota-exhausted'
    if (typeof error.status === 'number' && error.status >= 500) return 'provider-unavailable'
  }
  return 'network-error'
}

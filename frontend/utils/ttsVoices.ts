// Curated on-device narration voices. The in-browser model (MMS-TTS) is
// single-speaker per language, so the voice is resolved automatically from the
// slice content language — there is no user-facing voice picker.

export interface OnDeviceVoice {
  /** Stable id used as part of the audio cache key. */
  id: string
  /** Content language this voice narrates. */
  lang: 'vi' | 'en'
  /** Transformers.js model id loaded in the worker. */
  model: string
}

export const ON_DEVICE_VOICES: Record<'vi' | 'en', OnDeviceVoice> = {
  vi: { id: 'mms-vie', lang: 'vi', model: 'Xenova/mms-tts-vie' },
  en: { id: 'mms-eng', lang: 'en', model: 'Xenova/mms-tts-eng' },
}

const DEFAULT_LANG: 'vi' | 'en' = 'en'

/**
 * Resolve the on-device voice for a slice language. Unsupported/unknown
 * languages fall back to the default (English) voice.
 */
export function resolveVoiceForLanguage(language?: string | null): OnDeviceVoice {
  const normalized = (language || '').trim().toLowerCase()
  if (normalized.startsWith('vi')) return ON_DEVICE_VOICES.vi
  if (normalized.startsWith('en')) return ON_DEVICE_VOICES.en
  return ON_DEVICE_VOICES[DEFAULT_LANG]
}

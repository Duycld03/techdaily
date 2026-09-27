# Design

## Context

When users switch between documents of different languages (e.g. Vietnamese and English) or toggle between Local TTS and Cloud TTS, `useSliceAudio.ts` reads `selectedVoice` from a single global `localStorage` entry (`techdaily_audio_voice`).

If a user previously listened to a Vietnamese document or selected a Vietnamese voice (`vi-VN-Neural2-A`), `selectedVoice.value` remains set to that Vietnamese voice. When subsequently opening an English document (`source.language === 'en'`), `useSliceAudio.ts` passed `selectedVoice.value` directly to the backend synthesis API without validating language compatibility:
```typescript
const voiceId = selectedVoice.value || resolveCloudVoiceForLanguage(source.language)
```
Consequently, English prose was synthesized using a Vietnamese neural voice model.

See `proposal.md` for motivation and scope boundaries.

## Goals / Non-Goals

**Goals:**
- **Strict Voice-Language Guard**: Enforce that a voice ID can only be used if it matches the slice's content language.
- **Language-Scoped Voice Storage**: Store voice preferences independently per language (`techdaily_audio_voice_en` and `techdaily_audio_voice_vi`).
- **Backend Defense-in-Depth**: Validate language compatibility in `GetOrSynthesizeChunkAudioHandler.cs` and reject mismatched requests with `VOICE_LANGUAGE_MISMATCH`.
- **Seamless UI Dropdown**: Ensure the voice selector in `ReaderAudioPlayer.vue` always displays the active language-compatible voice.

**Non-Goals:**
- Modifying on-device MMS-TTS model resolution (which already uses language-bound models `mms-eng` and `mms-vie`).
- Adding new voices beyond the curated Google Cloud free-tier Neural2 voices.

## Decisions

### 1. Language-Scoped Voice Preference Storage

**Decision:** Replace single global key `techdaily_audio_voice` with language-scoped keys:
- `techdaily_audio_voice_en` for English voices
- `techdaily_audio_voice_vi` for Vietnamese voices

*Rationale:*
A developer studying both English technical docs (e.g. ASP.NET Core) and Vietnamese design system guides should be able to pick male/female preferences for each language independently without switching one clobbering the other.

### 2. Strict Voice Resolution Function

**Decision:** Refactor `resolveCloudVoiceForLanguage` in `useSliceAudio.ts` to accept an optional requested voice and validate its language prefix:
```typescript
export function resolveCloudVoiceForLanguage(lang?: string | null, customVoiceId?: string | null): string {
  const isVi = (lang || '').toLowerCase().startsWith('vi')
  const defaultVoice = isVi ? 'vi-VN-Neural2-A' : 'en-US-Neural2-F'
  if (!customVoiceId) return defaultVoice
  const matches = isVi ? customVoiceId.startsWith('vi-') : customVoiceId.startsWith('en-')
  return matches ? customVoiceId : defaultVoice
}
```

*Rationale:*
Ensures that even if an old stale value exists in memory or storage, it is automatically sanitized to a language-appropriate voice before generating cache keys or calling the backend.

### 3. Backend Validation in `GetOrSynthesizeChunkAudioHandler.cs`

**Decision:** Add a language compatibility check in the application use-case handler:
```csharp
var isViChunk = (chunk.Language ?? "").StartsWith("vi", StringComparison.OrdinalIgnoreCase);
var isViVoice = request.VoiceId.StartsWith("vi-", StringComparison.OrdinalIgnoreCase);
if (isViChunk != isViVoice)
{
    return Error.Custom("VOICE_LANGUAGE_MISMATCH",
        $"Requested voice '{request.VoiceId}' does not match document chunk language '{chunk.Language}'.");
}
```

*Rationale:*
Prevents corrupted cache entries in PostgreSQL (`DocumentChunkAudios`) if an invalid client request is received.

## Risks / Trade-offs

- **Existing `techdaily_audio_voice` localStorage migration**: Users with the legacy key might retain an old voice.
  *Mitigation:* The resolution function checks prefix match. If the legacy key matches the active language, it is accepted; if not, it gracefully defaults without errors.

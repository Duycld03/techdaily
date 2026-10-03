# Proposal

## Why

When reading Vietnamese technical and engineering craft books (such as *Thói Quen Nguyên Tử* / *Atomic Habits*) on mobile browsers (Android Chrome, iOS Safari), clicking "Listen" ("Nghe") in the reader player immediately fails with `Không tạo được audio: System TTS Error: synthesis-failed`. 

This failure occurs because the Cascading Audio Engine resolves to System TTS (Priority 1) when the browser reports a Vietnamese system voice (e.g. `Google tiếng Việt`), but:
1. Long verbatim book prose (4,000–10,000+ characters) is passed to a single `SpeechSynthesisUtterance`, instantly violating mobile TTS character limits (such as Android's 4,000-character `getMaxSpeechInputLength()` ceiling).
2. Remote network-based mobile TTS voices (such as Google Speech Services network voices) fail or time out on large payloads, or abort when local voice packs are not pre-installed on the mobile device.
3. The reader audio composable does not gracefully cascade to Priority 2 (Google Cloud TTS) upon runtime `utterance.onerror` events, and `ReaderAudioPlayer.vue` restricts the manual 1-tap "Chuyển sang Google Cloud" fallback button strictly to `engineMode === 'device'`, trapping mobile readers in a permanent error state.

Enhancing System TTS with sentence-level streaming chunking, automatic runtime cascade to Cloud TTS, and 1-tap UI recovery ensures zero-friction audio narration across all mobile devices.

## What Changes

- **Automatic Cloud Cascade on System TTS Runtime Faults**: When `SpeechSynthesisUtterance.onerror` fires with fatal synthesis errors (`synthesis-failed`, `synthesis-unavailable`, `language-unavailable`, `voice-unavailable`, or `audio-busy`), automatically cascade to Google Cloud TTS (`synthesizeOnCloud`), switch the active engine mode to `cloud`, and notify the user via localized toast.
- **Sentence-Level Utterance Chunking for System TTS**: Chunk the narration script into sentence boundaries using `splitSentences(script)` instead of dispatching monolithic multi-thousand-character blocks, eliminating Android `getMaxSpeechInputLength` overflows and Chromium 15-second speech cutoff limits.
- **Sequential Utterance Playback Queue**: Play chunked sentences sequentially via `utterance.onend`, updating current sentence index and auto-advancing cleanly through the slice prose.
- **Unified 1-Tap Cloud Fallback in Audio Player**: Update `categorizeAudioError` to set `suggestCloudFallback: true` for `system` engine failures, and update `canFallbackToCloud` in `ReaderAudioPlayer.vue` to allow immediate 1-tap Cloud switching from both `system` and `device` failure states.
- **Audio Focus & Carrier Safeguard**: Guard the silent carrier loop to ensure it does not preempt media focus or disrupt native mobile TTS engine initialization.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update the cascading engine hierarchy and error recovery specifications to require automatic runtime cascade to Cloud TTS upon System TTS synthesis failure (`synthesis-failed`), sentence chunking for Web Speech API execution, and 1-tap Cloud fallback button availability in System mode.

## Impact

- **Frontend Core**: `frontend/composables/useSliceAudio.ts` and `frontend/components/reader/ReaderAudioPlayer.vue`.
- **Localization**: Localized toast messages and error diagnosis copy in `frontend/i18n/locales/en.json` and `vi.json`.
- **Backend / Database**: Zero backend or database changes. Client-side audio generation remains zero-cost and compliant with the database storage invariant.
- **Breaking Changes**: None. All changes are backward compatible and degrade gracefully across desktop and mobile browsers.

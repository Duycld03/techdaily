# Design

## Context

TechDaily's reader audio narration (`/read/[bookId]`) implements an automated Cascading Audio Engine Resolution Hierarchy:
1. **Priority 1**: System TTS via browser Web Speech API (`window.speechSynthesis`).
2. **Priority 2**: Google Cloud Text-to-Speech API (`/api/v1/library/chunks/{chunkId}/audio`).
3. **Priority 3**: Client-side On-Device Neural Worker (Transformers.js MMS-TTS).

On mobile browsers (Android Chrome, iOS Safari), opening Vietnamese literature and engineering craft books (such as *Thói Quen Nguyên Tử* / *Atomic Habits*) automatically resolves to Priority 1 (System TTS) because the browser returns voices matching `vi` (such as `Google tiếng Việt`). 

However, because verbatim book prose preservation (`Category.EngineeringCraft`) produces reading slices of 4,000–10,000+ characters:
1. Passing the entire slice script to a single `SpeechSynthesisUtterance` exceeds mobile platform input limits (specifically Android's 4,000-character `TextToSpeech.getMaxSpeechInputLength()` ceiling).
2. Remote network synthesis in mobile Google Speech Services fails or times out on large payloads, or crashes when offline voice packs are missing.
3. When `utterance.onerror` fires with `synthesis-failed`, `synthesizeOnSystem` halts in an error state without cascading to Priority 2 (Google Cloud TTS).
4. `ReaderAudioPlayer.vue` limits the manual 1-tap Cloud fallback button (`canFallbackToCloud`) strictly to `engineMode === 'device'`, completely trapping the user in an unplayable error state.

See `proposal.md` for motivation and background.

## Goals / Non-Goals

**Goals:**
- **Automatic Cloud Cascade on System TTS Runtime Faults**: Automatically transition to Google Cloud TTS (`synthesizeOnCloud`) whenever System TTS encounters fatal runtime errors (`synthesis-failed`, `synthesis-unavailable`, `language-unavailable`, `voice-unavailable`, `audio-busy`), ensuring uninterrupted listening.
- **Sentence-Level Streaming Chunking for System TTS**: Segment long narration scripts into individual sentences via `splitSentences(script)` and speak them sequentially, preventing Android `getMaxSpeechInputLength` 4,000-character overflow and Chromium 15-second speech cutoff limits.
- **Universal 1-Tap Cloud Fallback in UI**: Enable the "Chuyển sang Google Cloud" action button in `ReaderAudioPlayer.vue` from both `system` and `device` error states, and configure `categorizeAudioError` to set `suggestCloudFallback: true` for `system` failures.
- **Robust Playback Lifecycle & Focus Control**: Ensure sequential sentence queuing properly handles play, pause, seek, speed changes, and slice auto-advance without leaking pending utterances or contending with mobile audio focus.
- **Complete Test Coverage**: Verify that unit and component test suites in `useSliceAudio.spec.ts` and `ReaderAudioPlayer.spec.ts` thoroughly test automatic fallback on `synthesis-failed`, sentence progression, and UI button visibility.

**Non-Goals:**
- Modifying backend endpoints, DTOs, or database schemas (`DocumentChunkAudios`).
- Adding third-party audio libraries or native Android/iOS bridge plugins.
- Altering the priority order of the cascading engine hierarchy (Priority 1 remains System TTS, Priority 2 Cloud TTS, Priority 3 On-Device Web Worker).

## Decisions

### Decision 1: Sentence-Level Sequential Queue for System TTS
- **Approach**: In `synthesizeOnSystem`, split `script` using `splitSentences(script)`. Maintain an internal sentence pointer `systemSentenceIndex` (from `0` to `sentences.length - 1`).
- **Execution Flow**:
  1. Instantiate `SpeechSynthesisUtterance(sentences[index])`.
  2. Bind voice, rate, pitch, and volume.
  3. On `utterance.onend`: increment `systemSentenceIndex`. If more sentences remain, speak the next sentence; otherwise, stop the carrier, mark `playing = false`, and invoke `deps.onSliceEnded?.()`.
  4. On pause/cancel/dispose: cancel `synth.cancel()` and reset or preserve sentence index for resume.
- **Rationale**: Android `TextToSpeech` strictly caps input text to 4,000 characters. Slices in *Thói Quen Nguyên Tử* easily exceed 5,000–10,000 characters. Chunking by sentence guarantees that every individual utterance remains within 50–300 characters, completely eliminating `getMaxSpeechInputLength` buffer overflows and avoiding the 15-second speech synthesis cutoff bug in Chromium.

### Decision 2: Automatic Runtime Cascade to Cloud TTS on Synthesis Failure
- **Approach**: In `utterance.onerror`:
  ```ts
  utterance.onerror = async (e) => {
    if (e.error === 'canceled' || e.error === 'interrupted') return
    playing.value = false
    stopSilentCarrier()
    
    // Check if eligible for automatic cascade to Cloud
    const isOnline = typeof navigator !== 'undefined' ? navigator.onLine : true
    if (isOnline && !isQuotaExhausted.value) {
      engineMode.value = 'cloud'
      activeCascadeTier.value = 'cloud'
      if (isClient) localStorage.setItem(AUDIO_ENGINE_STORAGE_KEY, 'cloud')
      deps.onFallbackToCloud?.()
      await synthesizeOnCloud(source, script, contentHash, cache, autoPlay)
      return
    }

    // Otherwise surface error with Cloud fallback option
    status.value = 'error'
    const info = categorizeAudioError(new Error(`System TTS Error: ${e.error}`), 'system')
    errorMessage.value = info.rawMessage
    errorInfo.value = info
  }
  ```
- **Rationale**: If a mobile device's Vietnamese voice is not pre-installed in the OS or if the mobile network voice server fails, the user should not be halted with a fatal error. Automatically cascading to Cloud TTS fulfills the core promise of the Cascading Engine Hierarchy.

### Decision 3: Universal Cloud Fallback Recovery in Audio Player Component
- **Approach**:
  1. In `categorizeAudioError`:
     ```ts
     if (engine === 'system' || engine === 'device') {
       return { 
         code: engine === 'device' ? 'DEVICE_INIT_FAILED' : 'SYSTEM_TTS_FAILED', 
         rawMessage, 
         suggestCloudFallback: true 
       }
     }
     ```
  2. In `ReaderAudioPlayer.vue`:
     ```ts
     const canFallbackToCloud = computed(() => {
       return (
         status.value === 'error' &&
         (engineMode.value === 'device' || engineMode.value === 'system') &&
         !isNearQuota.value &&
         !isQuotaExhausted.value &&
         (errorInfo?.value?.suggestCloudFallback ?? true)
       )
     })
     ```
- **Rationale**: If automatic cascade is bypassed (e.g., user manually selected System TTS after initial load and it encounters an error), the player toolbar must display the 1-tap "Chuyển sang Google Cloud" action button instead of disabling it.

### Decision 4: Audio Focus & Silent Carrier Lifecycle Isolation
- **Approach**:
  - In `startSilentCarrier`, ensure audio is only played when audio element is active and muted or carrier volume is negligible.
  - Cancel any active silent carrier immediately when speech synthesis errors or completes.
- **Rationale**: Prevents media focus contention between the HTML5 audio element and the native Android/iOS speech synthesis engine.

## Risks / Trade-offs

- **Risk: Increased Cloud API Usage Upon System Voice Failure**
  - *Impact*: Slices that fail on System TTS will consume monthly Google Cloud character quota.
  - *Mitigation*: Google Cloud TTS provides 1,000,000 free characters per month. Successful System TTS continues to run free on desktop and capable devices. The quota tracking system (`AudioQuotaResponse`) and `isQuotaExhausted` safeguards already prevent quota overruns.
- **Risk: Sentence Boundary Cadence Stutter**
  - *Impact*: Chaining sentences sequentially via `onend` might introduce a microscopic pause between sentences.
  - *Mitigation*: In human speech and audiobook narration, natural pauses between sentences (~100–200ms) significantly improve comprehension compared to robotic uninterrupted run-on sentences.

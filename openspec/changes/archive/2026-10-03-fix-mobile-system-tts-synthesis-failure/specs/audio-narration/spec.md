# audio-narration Spec Delta

## MODIFIED Requirements

### Requirement: Cascading Audio Engine Resolution Hierarchy
The reader audio system SHALL implement an automatic **Cascading Engine Resolution Hierarchy** that selects the most efficient, natural, and cost-effective text-to-speech engine available for each slice:
1. **Priority 1 (Universal System TTS via Web Speech API across Mobile & Laptop/PC)**: On all client environments (Laptop/PC on Windows, macOS, Linux, and Mobile on Android, iOS), the system SHALL first attempt to use `window.speechSynthesis`. If the browser supports the Web Speech API and exposes at least one voice matching the slice's language (e.g. Chrome's built-in Google voices, Edge's Natural voices, macOS Siri/Linh, or Android Google Speech Services), the system SHALL resolve to `'system'`.
2. **Priority 2 (Cloud TTS via Google Cloud API)**: If no matching system voice is installed on the host operating system, OR if System TTS encounters a runtime synthesis failure (`SpeechSynthesisErrorEvent.error` of `synthesis-failed`, `synthesis-unavailable`, `language-unavailable`, `voice-unavailable`, or `audio-busy`), the system SHALL automatically cascade to `'cloud'`, utilizing Google Cloud Text-to-Speech via the backend API.
3. **Priority 3 (Device TTS as Last Resort)**: If Cloud TTS is unavailable (due to client being offline `navigator.onLine === false`, backend network error, or cloud quota exhaustion `QUOTA_EXHAUSTED` / HTTP 429), the system SHALL degrade gracefully to the on-device Transformers.js Web Worker (`'device'`), ensuring narration is never blocked.
4. **Manual User Override**: If the user explicitly selects an engine mode via the UI dropdown, the system SHALL honor their selection unless that engine experiences a hard error, in which case it SHALL offer or initiate graceful fallback.

#### Scenario: Host OS has matching Vietnamese voice installed
- **WHEN** the user initiates narration on a Vietnamese slice and the host OS exposes a Vietnamese system voice (e.g. `vi-vn-x-vic-local` or Google tiếng Việt)
- **THEN** the reader resolves to Priority 1 (System TTS), initiating playback instantly without calling the backend cloud API and without downloading neural weights.

#### Scenario: Host OS lacks Vietnamese voice pack
- **WHEN** the user initiates narration on a Vietnamese slice on a device without any Vietnamese system voice installed
- **THEN** the reader automatically bypasses System TTS and cascades to Priority 2 (Cloud TTS), synthesizing natural speech via Google Cloud.

#### Scenario: Cloud TTS quota exhausted or device offline
- **WHEN** Cloud TTS encounters a 429 quota exhaustion error or the device is offline
- **THEN** the reader automatically degrades to Priority 3 (Device TTS), utilizing the local Web Worker to synthesize narration locally.

#### Scenario: User manually selects Cloud engine
- **WHEN** the user explicitly selects "Google Cloud" from the engine dropdown
- **THEN** the reader sets `engineMode = 'cloud'` and uses Google Cloud narration, bypassing automatic System TTS selection.

#### Scenario: System TTS runtime synthesis failure cascades automatically to Cloud TTS
- **WHEN** a user plays a Vietnamese slice with System TTS active and the browser voice emits `synthesis-failed` (such as on Android Chrome when network synthesis fails or voice packs are missing)
- **THEN** the reader automatically switches the active engine mode to `'cloud'`, displays a localized toast informing the user of the transition, and seamlessly synthesizes and plays the slice via Google Cloud TTS without remaining in an error state.

### Requirement: Graceful Speech Engine Fallback & Notification
When a user initiates playback using the `System` engine (Web Speech API) and the client environment lacks compatible voices for the slice language OR speech synthesis fails during playback:
1. The reader SHALL automatically switch the active engine mode to `Cloud`.
2. The UI Segmented Switch SHALL immediately update its active state to highlight `Cloud`.
3. The Voice Picker SHALL switch to the corresponding Cloud voice selection for the slice language.
4. The system SHALL display a toast notification informing the user that the engine was transitioned to Cloud TTS due to unavailable browser voices.
5. **Storage Invariant**: Client-side synthesis (Web Speech API and on-device Web Worker) SHALL NEVER send synthesized audio payloads, audio chunks, or character tracking records to the backend PostgreSQL database (`DocumentChunkAudios`). Only Cloud TTS synthesis requests handled by the backend server SHALL persist records to `DocumentChunkAudios`.

#### Scenario: Web Speech API has no compatible voice for slice language
- **WHEN** a user with `engineMode = 'system'` plays a slice whose language has no matching voices in `window.speechSynthesis.getVoices()`
- **THEN** the engine mode automatically switches to `'cloud'`, the UI toggle updates to "Cloud", a toast notification is displayed, and audio synthesizes via the Cloud API.

#### Scenario: No backend database persistence for client-side audio
- **WHEN** audio is played via `system` (Web Speech API) or `device` (On-Device Neural TTS)
- **THEN** no POST request is issued to `/api/v1/library/chunks/{chunkId}/audio` and zero rows are inserted into `DocumentChunkAudios`.

#### Scenario: Web Speech API synthesis failure transitions to Cloud engine
- **WHEN** audio playback fails with `synthesis-failed` while running on `engineMode = 'system'`
- **THEN** the engine mode automatically switches to `'cloud'`, the UI toggle updates to "Cloud", a localized toast notification is displayed, and narration continues via Google Cloud.

### Requirement: Actionable Error Diagnostics & Cloud Fallback Recovery
The reader audio composable and player component SHALL capture, diagnose, and present specific diagnostic feedback whenever audio synthesis or playback encounters an error, rather than masking failures behind opaque generic error messages:
1. **Specific Error Diagnostics**: The audio composable and player SHALL capture and display specific diagnostic context (such as memory exhaustion, worker initialization error, network failure, or HTTP error status) instead of an unexplained generic toast.
2. **Console Diagnostics**: The composable layer SHALL log full raw error details and stack traces to `console.error('[useSliceAudio] Device TTS Error:', err)` for all worker errors, lifecycle faults, and unhandled rejections.
3. **Network Error Classification**: Resource loading failures, including WebKit/Safari's `TypeError: Load failed`, CDN connection timeouts, and offline fetch rejections, SHALL be classified as `NETWORK_ERROR` rather than `DEVICE_INIT_FAILED`.
4. **Mobile Hardware Failure Cloud Recommendation**: When on-device synthesis fails on a mobile client due to hardware constraints, memory limits, or worker initialization errors, the player SHALL present an informative localized message explaining that on-device narration is constrained on this device and provide a direct 1-tap action to switch to the Google Cloud engine.
5. **Graceful Engine Switch on Error**: Activating the Cloud fallback action from the error state SHALL immediately switch the active engine mode to Google Cloud, clear the error state, and initiate Cloud narration for the slice without requiring a manual page refresh.
6. **Unified Cloud Fallback Availability**: When audio synthesis encounters a recoverable error under either `device` mode (hardware constraints, memory limits) OR `system` mode (synthesis-failed, voice-unavailable), the player toolbar SHALL render the 1-tap Cloud fallback action button (`Chuyển sang Google Cloud` / `Switch to Cloud`), provided the user's monthly cloud quota is not exhausted.

#### Scenario: On-device synthesis failure surfaces diagnostic reason
- **WHEN** on-device synthesis fails due to a worker or memory error
- **THEN** the reader UI displays an error notice containing actionable context explaining the failure rather than a blanket "unknown error".

#### Scenario: Mobile on-device failure offers 1-tap switch to Google Cloud TTS
- **WHEN** on-device synthesis encounters an error on a mobile device and the user's monthly cloud quota is not exhausted
- **THEN** the player displays a localized notification recommending Google Cloud narration with an action button that immediately switches to Cloud mode and plays the audio.

#### Scenario: Worker errors output detailed console diagnostics
- **WHEN** an on-device synthesis worker encounters an error during model download or sentence inference
- **THEN** `useSliceAudio` logs the raw error message to the browser console under `[useSliceAudio] Device TTS Error:` before updating player state.

#### Scenario: WebKit "Load failed" categorized as network error
- **WHEN** a mobile Safari or WebKit browser fails to download model assets and throws `TypeError: Load failed`
- **THEN** the composable categorizes the error as `NETWORK_ERROR` and displays the localized network error message rather than a device hardware failure message.

#### Scenario: Hardware error provides 1-tap Cloud fallback
- **WHEN** on-device synthesis fails due to hardware constraints and the user's monthly cloud quota is not exhausted
- **THEN** the player displays an error notice recommending Google Cloud narration with an action button that immediately switches to Cloud mode and plays the audio.

#### Scenario: System TTS error offers 1-tap switch to Google Cloud TTS
- **WHEN** System TTS encounters an error and the player enters error state
- **THEN** the player toolbar displays an actionable "Chuyển sang Google Cloud" button allowing the user to immediately trigger Cloud playback.

## ADDED Requirements

### Requirement: Web Speech API Sentence-Level Streaming Chunking
When synthesizing narration via System TTS (`engineMode === 'system'`), the reader SHALL segment the plain-text narration script into sentence-level chunks using punctuation-aware sentence boundaries (`splitSentences`):
1. **Input Size Bounds**: The reader SHALL NOT pass multi-thousand-character prose blocks to a single `SpeechSynthesisUtterance`. Each utterance text SHALL be bounded to individual sentence boundaries, strictly adhering to mobile platform input constraints (including Android `TextToSpeech.getMaxSpeechInputLength()` of 4,000 characters).
2. **Sequential Sentence Progression**: The player SHALL queue and play sentence utterances sequentially via `utterance.onend`.
3. **Playback Lifecycle Synchronization**: Pausing, stopping, or navigating away SHALL cancel the active utterance and reset the sentence playback pointer.
4. **Media Focus Isolation**: The silent carrier loop SHALL NOT contend with or interrupt the mobile platform's audio focus during sentence utterance transitions.

#### Scenario: Long book slice is chunked for System TTS
- **WHEN** a user plays a 6,000-character reading slice using System TTS
- **THEN** the player splits the script into sentence-level utterances and plays them sequentially, preventing `getMaxSpeechInputLength` overflows and browser synthesis cutoff.

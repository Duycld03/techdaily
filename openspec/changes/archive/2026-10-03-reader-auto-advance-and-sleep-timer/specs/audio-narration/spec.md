# Spec Delta

## ADDED Requirements

### Requirement: Cascading Audio Engine Resolution Hierarchy
The reader audio system SHALL implement an automatic **Cascading Engine Resolution Hierarchy** that selects the most efficient, natural, and cost-effective text-to-speech engine available for each slice:
1. **Priority 1 (Universal System TTS via Web Speech API across Mobile & Laptop/PC)**: On all client environments (Laptop/PC on Windows, macOS, Linux, and Mobile on Android, iOS), the system SHALL first attempt to use `window.speechSynthesis`. If the browser supports the Web Speech API and exposes at least one voice matching the slice's language (e.g. Chrome's built-in Google voices, Edge's Natural voices, macOS Siri/Linh, or Android Google Speech Services), the system SHALL resolve to `'system'`.
2. **Priority 2 (Cloud TTS via Google Cloud API)**: If no matching system voice is installed on the host operating system, the system SHALL automatically cascade to `'cloud'`, utilizing Google Cloud Text-to-Speech via the backend API.
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

### Requirement: System TTS Engine via Web Speech API
The reader audio narration system SHALL support a **System TTS** engine mode (`engineMode === 'system'`) powered by the browser's native Web Speech API (`window.speechSynthesis`), alongside existing Cloud and Device engines.
1. **Engine Selection**: The reader audio player SHALL provide a selectable engine option for "System" (or "Hệ thống" / "Thiết bị"), persisting the user's engine choice in `localStorage`.
2. **Local Voice Discovery**: When System engine is active, the player SHALL enumerate all available voices via `speechSynthesis.getVoices()`, filtering or presenting voices matching the slice's language, including native OS engines (e.g. Android `com.google.android.tts` with local voices such as `vi-vn-x-vic-local` or Google Tiếng Việt).
3. **Voice Customization & Persistence**: The user SHALL be able to select their preferred local voice, with the selection persisted in `localStorage`.
4. **Playback Rate & Pitch**: The System engine SHALL honor user-configured playback speed (up to 2.0x) and pitch, applying them directly to the `SpeechSynthesisUtterance`.
5. **Zero-Latency Execution**: System TTS SHALL start playback immediately without downloading neural network model weights or executing server-side API requests, and SHALL operate without consuming cloud quota.

#### Scenario: User selects System TTS engine on Android
- **WHEN** the user selects the "System" audio engine on an Android device with Google Speech Services installed
- **THEN** the audio player displays available local system voices (including `vi-vn-x-vic-local`), starts narration immediately upon play without model download latency, and does not increment backend cloud character usage.

#### Scenario: System TTS honors 2.0x playback speed
- **WHEN** the user sets playback speed to 2.0x and initiates System TTS narration
- **THEN** the `SpeechSynthesisUtterance.rate` is set to 2.0 and speech is synthesized at double speed.

### Requirement: Continuous Cross-Slice Auto-Advance
The reader audio player SHALL provide an automatic slice progression mechanism that continues narration to subsequent document slices without manual user intervention.
1. **Auto-Advance Toggle**: The player SHALL render an "Auto Next" (`autoAdvance`) toggle control, defaulted to enabled (`true`) and persisted in `localStorage` (`techdaily_reader_audio_auto_advance`).
2. **End-of-Slice Trigger**: When narration of the current slice completes (via `<audio>` `ended` event for Cloud/Device or `utterance.onend` for System), if `autoAdvance` is enabled and a subsequent slice exists (`activeChunkIndex < chunks.length - 1`), the system SHALL trigger progression to the next slice (`activeChunkIndex + 1`).
3. **Continuous Playback Hand-off**: Upon advancing to the next slice, the audio player SHALL automatically begin playback of the new slice's narration.

#### Scenario: Current slice audio finishes with auto-advance enabled
- **WHEN** audio narration for slice $N$ reaches completion and `autoAdvance` is true and slice $N+1$ exists
- **THEN** the reader automatically advances to slice $N+1$ and begins narration of slice $N+1$ without requiring the user to press Play or click Next.

#### Scenario: Auto-advance disabled at end of slice
- **WHEN** audio narration for slice $N$ finishes and `autoAdvance` is false
- **THEN** playback transitions to paused (`playing === false`), remaining on slice $N$.

### Requirement: Soft Chime Transition on Slice Boundary
The reader audio system SHALL emit a subtle, non-intrusive auditory chime whenever auto-advancing across slice boundaries.
1. **Web Audio Synthesis**: The chime SHALL be synthesized dynamically via the Web Audio API (`AudioContext`), requiring zero external network asset downloads.
2. **Acoustic Characteristics**: The chime SHALL produce a gentle dual-frequency harmonic sweep (440Hz transitioning to 880Hz, duration 200–300ms, low-gain envelope) signaling progression without startling the listener.
3. **Execution Timing**: The chime SHALL play immediately upon slice completion, directly before starting narration of the subsequent slice.

#### Scenario: Auto-advancing emits soft chime
- **WHEN** slice $N$ completes narration and auto-advance initiates transition to slice $N+1$
- **THEN** a gentle synthesized Web Audio chime is sounded, followed immediately by narration of slice $N+1$.

### Requirement: Lockscreen and Headset Media Session Control
The reader audio system SHALL integrate with the Web Media Session API (`navigator.mediaSession`) to provide background playback metadata and remote hardware controls.
1. **Metadata Synchronization**: When narration begins or advances, the system SHALL update `navigator.mediaSession.metadata` with:
   - `title`: Current slice title or slice section heading.
   - `artist`: Document or book title.
   - `album`: "TechDaily Reader".
   - `artwork`: Document cover thumbnail image.
2. **Remote Action Handlers**: The system SHALL register action handlers for:
   - `play`: Resumes audio playback or speech synthesis.
   - `pause`: Pauses active playback.
   - `nexttrack`: Advances to the next slice and initiates narration.
   - `previoustrack`: Rewinds to the previous slice or restarts the current slice.
3. **Background Persistence**: The media session SHALL remain active and responsive when the mobile screen is locked or when interacting via Bluetooth headphones/car audio controls.

#### Scenario: User clicks Next Track on Bluetooth headset
- **WHEN** narration is playing and the user presses the "Next Track" hardware button on their Bluetooth headphones
- **THEN** the media session `nexttrack` handler executes, advancing the reader to the next slice and beginning its narration.

#### Scenario: User pauses from lockscreen notification
- **WHEN** audio is playing and the user taps "Pause" on the Android lockscreen media notification
- **THEN** playback is paused and the lockscreen state updates to paused.

### Requirement: Fixed-Preset Sleep Timer and 15-Second Audio Fade-Out
The reader audio player SHALL provide a Sleep Timer control with fixed duration presets and a smooth audio fade-out mechanism.
1. **Fixed Presets**: The player SHALL offer selectable presets: 15 minutes (`15m`), 30 minutes (`30m`), 45 minutes (`45m`), 60 minutes (`60m`), and "End of Current Slice" (`end_of_slice`), plus an option to cancel/turn off.
2. **Countdown Display**: While active, the reader audio player toolbar SHALL display a real-time countdown badge indicating remaining time (e.g. `29:45` or slice icon).
3. **15-Second Audio Fade-Out**: When a timed sleep timer reaches its final 15 seconds, the system SHALL smoothly attenuate audio volume exponentially from 1.0 down to 0.0 over the 15-second window prior to pausing.
4. **End-of-Slice Stop**: When set to "End of Current Slice", the player SHALL disable auto-advance for the current slice, pausing playback upon slice completion with volume intact.
5. **State Preservation**: When the timer expires, playback SHALL pause and volume SHALL be restored to 1.0 for the next manual playback session, leaving the reader bookmark at the exact position reached.

#### Scenario: Sleep timer reaches final 15 seconds
- **WHEN** an active 30-minute sleep timer counts down to 0:15 remaining
- **THEN** audio volume smoothly fades down from 1.0 to 0.0 over 15 seconds, and at 0:00 playback pauses without sudden auditory clipping.

#### Scenario: User selects End of Current Slice preset
- **WHEN** the user selects "End of Current Slice" as the sleep timer mode
- **THEN** the reader plays the remainder of the active slice, and upon reaching slice completion, halts playback without advancing to the next slice.

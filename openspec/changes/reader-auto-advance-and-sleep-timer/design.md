# Design

## Context

TechDaily's reader currently implements dual-engine audio narration (`useSliceAudio.ts`):
- **Cloud Engine**: Proxied Google Cloud Text-to-Speech API (`/api/audio/synthesize`) subject to a monthly 1,000,000 character limit and user quota exhaustion.
- **Device Engine**: On-device Transformers.js Web Worker synthesizing chunks via WebGPU/WASM, which requires downloading heavy model weights and exhibits high initial latency.

Both existing engines isolate playback to the currently active slice. When playback finishes, `<audio>` emits `ended`, which halts playback and sets `playing.value = false`. Furthermore, mobile and Bluetooth users lack lockscreen controls (`navigator.mediaSession`), and listeners have no automated mechanism to halt playback when falling asleep.

## Goals / Non-Goals

**Goals:**
- **Cascading Tri-Engine Architecture**: Prioritize **System TTS (`window.speechSynthesis`)** as Priority 1 (0ms latency, zero quota, local OS voices like `vi-vn-x-vic-local` at 2.0x speed). Cascade automatically to **Cloud TTS** as Priority 2 when host OS lacks matching voices. Degrade gracefully to **Device TTS (Web Worker)** as Priority 3 (last resort) when offline or when cloud quota is exhausted.
- **Continuous Cross-Slice Auto-Advance**: Seamlessly advance to `activeChunkIndex + 1` upon slice narration completion when `autoAdvance` is enabled, updating reader bookmarks and syncing with backend `UserBookPacer`.
- **Soft Auditory Chime**: Synthesize an instantaneous 250ms harmonic chime (440Hz to 880Hz) via the Web Audio API (`AudioContext`) on slice transition without downloading external audio files.
- **Lockscreen & Bluetooth Controls (`navigator.mediaSession`)**: Wire play, pause, nexttrack, and previoustrack to reader slice progression, keeping Android lockscreen media notifications synchronized.
- **Fixed-Preset Sleep Timer & 15-Second Audio Fade-Out**: Support presets (`15m`, `30m`, `45m`, `60m`, and `end_of_slice`) with active countdown badge and a smooth exponential 15-second volume fade before stopping.

**Non-Goals:**
- Modifying backend PostgreSQL schemas or database entities (existing slice and pacer APIs are utilized as-is).
- Third-party paid cloud TTS integrations beyond Google Cloud.
- Cross-device audio synchronization (reading bookmarks and pacer sync handle position state).

## Decisions

### 1. Cascading Engine Resolution Hierarchy (`useSliceAudio.ts`)
We introduce an automated resolution cascade that picks the best available engine mode:

```
               CASCADING ENGINE RESOLUTION (FALLBACK CASCADE)
               =============================================

                       [Initiate Slice Playback]
                                  |
                                  v
                 +----------------------------------+
                 |  PRIORITY 1: SYSTEM (Web Speech) |
                 +----------------------------------+
                 | * window.speechSynthesis ready?  |
                 | * Has voice matching slice lang? |
                 +----------------------------------+
                       /                      \
                    (YES)                     (NO)
                     /                          \
                    v                            v
            [Play via System TTS]       +----------------------------------+
            - Zero latency              |    PRIORITY 2: CLOUD (Google)    |
            - Zero quota used           +----------------------------------+
            - Default 1.0x (up to 2.0x), Pitch 1.0
            - Native OS voice           | * Cloud quota not exhausted?     |
                                        +----------------------------------+
                                              /                      \
                                           (YES)                     (NO / ERROR)
                                            /                          \
                                           v                            v
                                   [Play via Cloud TTS]        +----------------------------------+
                                   - High-quality Neural       |  PRIORITY 3: DEVICE (Web Worker) |
                                   - Google Cloud WAV blob     +----------------------------------+
                                                               | Last resort: Offline / Quota 429 |
                                                               | - Local WebGPU / WASM synthesis  |
                                                               | - 100% offline guaranteed        |
                                                               +----------------------------------+
```

#### Decision Table: Engine Selection & Degradation

| Condition | Selected Engine | Behavior |
|---|---|---|
| User has manual override stored in `localStorage` | User-selected engine (`'system'`, `'cloud'`, or `'device'`) | Directly activates selected engine. If it fails, prompts or initiates fallback. |
| Auto mode + Host OS/browser has matching voice (e.g. Chrome Google voices on PC, Edge Natural voices on Windows, macOS Siri/Linh, Android `com.google.android.tts` on Mobile) | `'system'` (Priority 1 across Laptop/PC & Mobile) | Instant synthesis via `SpeechSynthesisUtterance`. No network traffic. |
| Auto mode + Host OS lacks matching voice (e.g. Linux desktop with only English voices) | `'cloud'` (Priority 2) | Calls backend `/api/audio/synthesize` to produce Google Cloud Neural WAV. |
| Auto mode + Offline or Cloud Quota Exhausted (`QUOTA_EXHAUSTED` / HTTP 429) | `'device'` (Priority 3 - Last Resort) | Delegates to Transformers.js Web Worker. Displays offline/local badge. |

### 2. Event-Driven Slice Auto-Advance Pipeline
We decouple slice navigation from the reader page by having `ReaderAudioPlayer.vue` coordinate with `read/[bookId].vue`:

```
  [Slice N Narration Active]
              |
              v (Audio ended event / Utterance onend)
  +--------------------------------------------------------------+
  | Check Auto-Advance Conditions:                               |
  | - autoAdvance === true                                       |
  | - sleepTimerMode !== 'end_of_slice'                          |
  | - activeChunkIndex < chunks.length - 1                       |
  +--------------------------------------------------------------+
              | (Conditions met)
              |
              v
  [1. Emit / Trigger Soft Chime via AudioContext (440Hz -> 880Hz)]
              |
              v
  [2. Advance activeChunkIndex = N + 1]
              |
              v
  [3. Update Bookmark: localStorage.setItem(techdaily_bookmark_{bookId}, N+1)]
              |
              v
  [4. Sync Pacer: notesStore / libraryStore UserBookPacer API]
              |
              v
  [5. Initiate Narration on Slice N+1 (Zero-lag transition)]
```

### 3. Procedural Web Audio Chime Synthesis
Rather than bundling or fetching an external MP3/WAV chime file (which risks 404s, latency, and extra bundle weight), we synthesize a gentle chime procedurally using the Web Audio API:
- Create `AudioContext` lazily on user gesture.
- Chain `OscillatorNode` (sine wave sweeping 440Hz to 880Hz) to a `GainNode` with exponential decay envelope (gain 0.15 ramping down to 0.001 over 250ms).
- Self-cleaning: closes or suspends the audio node immediately upon chime completion.

### 4. Lockscreen & Headset Media Session Integration
We register actions with `navigator.mediaSession`:
- `setActionHandler('play')`: Resume playback.
- `setActionHandler('pause')`: Pause active playback.
- `setActionHandler('nexttrack')`: Advance to next slice (`activeChunkIndex + 1`) and start audio.
- `setActionHandler('previoustrack')`: Return to previous slice (`activeChunkIndex - 1`) or rewind.
- Update `metadata` on each slice change with `title`, `artist`, `album`, and `artwork`.

### 5. Sleep Timer with 15-Second Exponential Audio Fade-Out
- Sleep timer state is managed reactively: `timerMode` (`off`, `15m`, `30m`, `45m`, `60m`, `end_of_slice`), `remainingSeconds`, and `timerActive`.
- A 1-second interval (`useIntervalFn` or `setInterval`) decrements `remainingSeconds`.
- When `remainingSeconds <= 15` and `remainingSeconds > 0`:
  - Audio volume attenuates smoothly: `volume = Math.max(0, remainingSeconds / 15)`.
- At `remainingSeconds === 0`:
  - Call `pause()`.
  - Reset volume back to `1.0` so subsequent manual plays are not silent.
  - Reset timer state to `'off'`.
- If `timerMode === 'end_of_slice'`:
  - No countdown badge needed; sets a flag that suppresses the auto-advance hook upon slice completion, pausing audio at slice end.

## Risks / Trade-offs

| Risk / Constraint | Mitigation Strategy |
|---|---|
| **Host OS missing language voice for System TTS** | Validate `speechSynthesis.getVoices().some(v => v.lang.startsWith(lang))` before picking `'system'`. If missing, cascade immediately to `'cloud'` without surfacing an error to the user. |
| **Android Chrome speech synthesis background throttling** | When using System TTS, maintain a silent HTML `<audio>` element playing in parallel. This signals the OS media manager that the tab holds active media playback, keeping the audio session alive in background. |
| **`speechSynthesis.getVoices()` returns empty on first call** | In Chromium, voices are populated asynchronously. We listen to `window.speechSynthesis.onvoiceschanged` and initialize reactive `voices` ref when fired. |
| **Autoplay policy blocking next slice audio** | Because the user initiates playback on Slice 1 via an explicit click ("Listen" / Play button), the browser grants user media engagement. Continuing playback in the same audio context or calling `play()` on the subsequent slice preserves the user gesture token. |
| **Volume fade-out with System TTS** | `SpeechSynthesisUtterance` does not support real-time volume modulation during an utterance on all mobile engines. For System TTS, volume is adjusted on the carrier audio or the speech is paused cleanly at 0:00. |

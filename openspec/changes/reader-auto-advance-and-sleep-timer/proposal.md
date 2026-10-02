# Proposal

## Why

TechDaily's dedicated book reader (`/read/[bookId]`) currently supports two audio narration approaches: Google Cloud TTS (subject to restrictive monthly character quotas and API limits) and on-device neural TTS via Transformers.js/WASM (which suffers from heavy model downloads, noticeable initialization latency, and robotic cadence). Furthermore, playback is currently restricted to a single isolated slice, abruptly halting when audio finishes and forcing manual interaction to load the next slice.

Additionally, users listening on mobile devices or Bluetooth headsets lack lockscreen controls, and night-time learners listening before sleep have no automated mechanism to halt playback without abrupt cuts or battery drain.

Introducing a **Cascading Tri-Engine Architecture** prioritizes the zero-latency, quota-free **System TTS Engine (`window.speechSynthesis`)** leveraging native OS synthesizers (such as Android's `com.google.android.tts` with local voices like `vi-vn-x-vic-local` at a natural 1.0x speed and 1.0 pitch, adjustable up to 2.0x). When a matching native voice is missing, the system gracefully falls back to **Cloud TTS (Google)**, and only as a last resort (when offline or cloud quota is exhausted) degrades to **Device TTS (Web Worker)**. Combined with **Continuous Auto-Advance Playback**, a **Subtle Transition Chime**, **Lockscreen/Bluetooth Controls (`navigator.mediaSession`)**, and a **Preset Sleep Timer with 15-Second Audio Fade-Out**, this turns TechDaily into a hands-free, audiobook-quality documentation reader.

## What Changes

- **Cascading Tri-Engine Architecture & System TTS**:
  - Add `'system'` alongside existing `'cloud'` and `'device'` engine modes in `useSliceAudio.ts`.
  - Implement an intelligent **Engine Fallback Cascade**:
    1. **Priority 1 (System TTS via Web Speech API - Universal across Mobile & Laptop/PC)**: On all platforms (whether Laptop/PC on Chrome/Edge/macOS or Mobile on Android/iOS), the system always prioritizes `window.speechSynthesis`. If the browser or host OS has any voice matching the slice language (e.g. Chrome's built-in Google voices, Edge's Natural voices, macOS Siri/Linh, or Android Google Speech Services), it plays immediately with zero latency, zero quota usage, and zero network overhead.
    2. **Priority 2 (Cloud TTS)**: If no compatible voice matching the slice language exists on the system/browser, it automatically routes to Google Cloud TTS for high-quality speech.
    3. **Priority 3 (Device TTS - Last Resort)**: If Cloud TTS fails (offline mode, network failure, or `QUOTA_EXHAUSTED` / HTTP 429), it degrades seamlessly to the on-device Transformers.js Web Worker so reading is never blocked.
  - Enumerate and expose local system voices via `speechSynthesis.getVoices()`, allowing users to select their favorite OS-level voices (e.g. `vi-vn-x-vic-local`, Google tiếng Việt).
  - Default playback rate to 1.0x and pitch to 1.0, while allowing full user customizability (speed steps up to 2.0x and pitch modulation), persisted in `localStorage`.
- **Continuous Auto-Advance & Soft Transition Chime**:
  - Add an "Auto Next" (`autoAdvance`) toggle in `ReaderAudioPlayer.vue` (persisted in `localStorage`, default enabled).
  - When the current slice narration reaches completion (`ended` event), if `autoAdvance` is enabled and a subsequent slice exists, the reader automatically advances to `chunkOrder + 1`, synchronizes reading bookmarks, and immediately begins narration.
  - Synthesize a subtle, warm notification chime (~250ms, 440Hz to 880Hz) via Web `AudioContext` upon slice transition to notify the listener without intrusive speech interruptions.
- **Lockscreen & Bluetooth Headset Controls (`navigator.mediaSession`)**:
  - Bind playback metadata (`title`: slice title, `album`: book title, `artwork`: cover image) to `navigator.mediaSession`.
  - Implement handlers for `play`, `pause`, `previoustrack`, and `nexttrack` to enable hands-free navigation directly from the Android lockscreen, notification shade, and Bluetooth earphone buttons.
  - Keep media session persistent across slice transitions and during background playback.
- **Fixed-Preset Sleep Timer & 15-Second Audio Fade-Out**:
  - Add a Sleep Timer selector to `ReaderAudioPlayer.vue` with fixed presets (15 min, 30 min, 45 min, 60 min, and "End of Current Slice" `end_of_slice`).
  - Render an active countdown badge (e.g. `29:45`) directly on the reader audio toolbar.
  - Implement a 15-second smooth exponential audio fade-out (volume ramp from 1.0 to 0.0) prior to pausing playback to prevent jarring stops.
  - When "End of Current Slice" is selected, audio simply stops at the slice boundary without advancing.
- **Reading Progress & Pacer Sync**:
  - Advancing to the next slice via audio completion automatically persists the bookmark to `localStorage` (`techdaily_bookmark_{bookId}`) and syncs progress with the backend `UserBookPacer`.

## Capabilities

### New Capabilities
- *None* (this enhancement extends existing reader and audio capabilities).

### Modified Capabilities
- `audio-narration`: Add specifications for Cascading Engine Resolution (System -> Cloud -> Device), System TTS engine (`window.speechSynthesis`), cross-slice continuous playback queueing, soft chime transition, `navigator.mediaSession` integration, and sleep timer with audio fade-out mechanics.
- `reader`: Add specifications for automated slice progression driven by audio completion events, preserving reader focus and bookmark persistence.

## Impact

- **Frontend Code**: Modifies `frontend/components/reader/ReaderAudioPlayer.vue`, `frontend/composables/useSliceAudio.ts`, and `frontend/pages/read/[bookId].vue`.
- **Backend API**: No breaking changes; existing slice and progress endpoints are utilized as-is.
- **Dependencies**: No external npm packages required; implemented using native Web APIs (`window.speechSynthesis`, `navigator.mediaSession`, Web Audio `AudioContext`) and Vue 3 / VueUse composables.
- **Breaking Changes**: None.

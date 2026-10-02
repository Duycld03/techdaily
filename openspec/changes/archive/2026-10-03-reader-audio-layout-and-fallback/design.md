# Technical Design: Reader Audio Layout, Cloud Fallback & Pacer Gating

## Context

The reader audio narration interface (`ReaderAudioPlayer.vue`) is shared between two core views:
1. **Daily Studio (`/today`)**: In `DocReaderPane.vue`, which is placed side-by-side with `InterviewChallengePane.vue` (`md:w-1/2`). The reader panel is constrained to ~550px–650px wide even on 1080p monitors.
2. **Standalone Book Reader (`/read/[bookId]`)**: Features a collapsible table of contents rail (`w-72`), leaving ~650px–750px for the reading card.

Currently, all 8 controls (Play button, Engine segmented switch, Tier badge, Voice dropdown, Scrubber slider with timestamp, Auto-advance toggle, Sleep timer, and Speed cycle button) are forced onto a single flex row without wrapping. Breakpoints (`sm:`, `md:`) evaluate against the browser window instead of the container, leading to severe visual collisions, clipped elements, and broken layouts.

Furthermore, on environments lacking local speech synthesis voices (e.g., Linux/Chromium without `speech-dispatcher`), the system displays an obscure debug badge (`Bậc: cloud`) instead of automatically updating the engine mode switch to Cloud with a clear user toast. When playing via Web Speech API (`duration === 0`), the scrubber track disappears completely and the button stays labeled "Nghe", making playback appear broken or invisible.

## Goals / Non-Goals

**Goals:**
- Guarantee zero element collisions, zero text overlaps, and zero button clipping in `ReaderAudioPlayer.vue` across all screen resolutions (mobile 390px, workbench 550px, desktop 1440px+).
- Completely eliminate the `Bậc: cloud` debug badge.
- Automatically update `engineMode` state to `'cloud'` when browser voices are missing, and display an informative toast notification.
- Maintain the strict storage invariant: client synthesis (Web Speech API and on-device Web Worker) remains 100% client-side (using IndexedDB for caching) and never writes to PostgreSQL. Only Google Cloud TTS interacts with the backend database.
- Provide vivid visual feedback for Web Speech API playback (dynamic button label "Tạm dừng" / "Pause" and an active reading banner).
- Gate auto-advancing in `/today` so completing audio narration does not skip the mandatory Senior Challenge drill.

**Non-Goals:**
- Changing backend Google Cloud TTS synthesis logic, monthly character quotas, or PostgreSQL schema.
- Refactoring the ONNX Web Worker neural synthesis engine.

## Decisions

### 1. Two-Row Responsive Layout Architecture

Instead of an unstable single row that depends on viewport media queries, `ReaderAudioPlayer.vue` will use a structured two-row flex layout:

```
+-----------------------------------------------------------------------------------+
| Row 1: [> Nghe / Tạm dừng]  [ Hệ thống | Cloud | Thiết bị ]   [>>]  [Moon]  [2x]   |
| --------------------------------------------------------------------------------- |
| Row 2: [ Giọng đọc v ]  ═════════════════════O════════════    0:45 / 2:41         |
+-----------------------------------------------------------------------------------+
```

- **Outer Wrapper**: `relative flex flex-col gap-2.5 rounded-2xl border border-slate-200/80 dark:border-white/[0.08] bg-slate-50/80 dark:bg-canvas-subtle/70 p-2.5 sm:p-3`
- **Row 1 (Primary & Utilities)**:
  - `flex items-center justify-between gap-2 w-full min-w-0`
  - **Left Cluster**: Play/Pause button (`h-8 px-3 rounded-xl bg-brand-600 ...`) + Engine Mode switch (`h-8 inline-flex rounded-xl p-0.5 ...`).
    - The Play button dynamically displays `playing ? t('reader.audio_pause') : t('reader.audio_listen')`.
  - **Right Cluster**: Utility tools grouped in a `flex items-center gap-1.5 shrink-0`:
    - Auto-advance button (`[>>]`, compact icon on mobile/tablet, full label on wide screens if space permits).
    - Sleep timer dropdown (`[Moon]`, showing minutes badge when active).
    - Speed cycle button (`[1x]`, `[1.25x]`, etc.).
- **Row 2 (Voice Selection & Timeline / State)**:
  - `flex items-center gap-2 sm:gap-3 w-full min-w-0 pt-1 border-t border-slate-200/60 dark:border-white/[0.06]`
  - **Left**: Contextual Voice Dropdown (`AppSelect`) with flexible width (`w-36 sm:w-44 shrink-0`).
  - **Right**:
    - When `duration > 0` (Cloud / Device TTS): Seekable slider track (`input[type=range]`) with duration label (`0:45 / 2:41`).
    - When `duration === 0 && playing` (Web Speech API): An animated active speech banner with volume icon and voice name (`t('reader.audio_system_playing_label', { voice: currentVoiceName })`).
    - When idle and no duration: Subtle placeholder indicator (`t('reader.audio_system_ready_label')`).

### 2. Automatic Engine Fallback with Toast Notification

In `useSliceAudio.ts` and `ReaderAudioPlayer.vue`:
1. When `loadAndPlay` or `synthesizeOnSystem` is invoked with `engineMode === 'system'`:
   - Inspect available voices using `filterSystemVoicesForLanguage(systemVoices.value, source.language)`.
   - If `matching.length === 0` or `speechSynthesis` is unavailable:
     - Set `engineMode.value = 'cloud'`.
     - Update `localStorage.setItem(AUDIO_ENGINE_STORAGE_KEY, 'cloud')`.
     - Call `useToast().info(t('reader.audio_fallback_to_cloud_toast'))`.
     - Immediately invoke `synthesizeOnCloud(source, script, contentHash, cache, autoPlay)`.
2. The UI segmented switch updates reactively to highlight "Cloud", and the voice picker updates to Cloud voices.
3. The obsolete `activeCascadeTier` badge (`Bậc: {tier}`) is deleted from `ReaderAudioPlayer.vue`.

### 3. Client-Side vs Backend Storage Boundary

| Engine Mode | Synthesis Location | Caching Mechanism | Backend DB (`DocumentChunkAudios`) |
|---|---|---|---|
| **System** (Web Speech API) | Browser Native Audio | None (in-memory speech stream) | **NEVER** |
| **Device** (Neural TTS) | Web Worker (Transformers.js) | Client **IndexedDB** (`idb-keyval`) | **NEVER** |
| **Cloud** (Google Cloud) | Server Proxy | Backend PostgreSQL + Client IndexedDB | **YES** (`DocumentChunkAudio` MP3) |

### 4. Pacer Auto-Advance Gating in `/today`

In `DocReaderPane.vue`:
- Add prop `disableAutoAdvance` to `ReaderAudioPlayer` (defaulting to `false`).
- In `DocReaderPane.vue`, pass `:disable-auto-advance="true"`.
- When `disableAutoAdvance` is `true`:
  - The Auto-Advance toggle button in Row 1 is hidden or disabled with a tooltip (`t('today.audio_auto_advance_disabled_hint')`).
  - Narration completion (`onSliceEnded`) does not trigger slice jumping in Today mode, ensuring the user completes the Senior Challenge drill before progressing.

## Risks / Trade-offs

- **Component Height Increase**: The two-row layout increases the player height from ~50px to ~80px.
  - *Mitigation*: 80px remains very compact on both desktop and mobile, while eliminating all horizontal squishing and clipping permanently.
- **Cloud Quota Consumption on Fallback**: Users on Linux without local voices will automatically use Cloud TTS.
  - *Mitigation*: The app already enforces a 950,000 monthly character limit and cascades to On-Device Web Worker if quota is exhausted. The toast notification clearly explains to the user that Cloud TTS is being utilized.

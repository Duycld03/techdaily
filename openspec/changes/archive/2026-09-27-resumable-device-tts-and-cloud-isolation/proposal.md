# Proposal

## Why

In the reader audio player (`ReaderAudioPlayer.vue`), two critical defects and optimization opportunities exist in audio engine management:
1. **Engine Cross-Contamination & Ghost Progress:** When playing Google Cloud TTS, the player UI erroneously displays on-device synthesis progress (e.g. `(Đang tạo audio... 2/45)`) alongside actively playing Cloud audio. This occurs because background Web Worker synthesis jobs are not cancelled when switching to Cloud mode, and synthesis progress indicators (`synthIndex`, `synthTotal`) are neither reset nor isolated to Device mode.
2. **Loss of Partial On-Device Synthesis Progress:** On-device neural synthesis is compute- and battery-intensive. If synthesis is paused or interrupted (for instance, after synthesizing 5 of 15 sentences) when switching engines or navigating, all 5 generated sentence chunks are currently discarded from volatile memory. Resuming requires re-synthesizing from sentence 0. By caching partial chunks in IndexedDB keyed by `(chunkId, voice, contentHash)`, the player can validate content integrity: if the content hash matches, it seamlessly resumes synthesis from sentence 5; if the slice text was modified (producing a different content hash), it invalidates stale partial chunks and restarts from sentence 0.

## What Changes

- **Worker Task Cancellation & Isolation:**
  - Add `{ type: 'cancel', reqId }` message handling to `ttsSynth.worker.ts` so the worker stops inference immediately upon request.
  - In `useSliceAudio.ts`, cancel active on-device worker tasks when switching engine modes or starting Cloud TTS playback.
  - In `ReaderAudioPlayer.vue`, ensure the `(Đang tạo audio... current/total)` progress indicator renders **strictly** when `engineMode === 'device'`.
- **Partial Synthesis Chunk Caching & Content-Hash Validation:**
  - Extend the IndexedDB client audio cache schema/store to persist partial sentence chunks under a composite key `partial:${chunkId}:${voice}:${contentHash}`.
  - When initiating on-device synthesis:
    - Check if the full assembled audio exists in cache (instant playback).
    - If not, check for existing partial chunks under `(chunkId, voice, contentHash)`.
    - If valid cached chunks exist ($K$ of $N$ sentences) and the content hash matches, restore chunks $0 \dots K-1$, set `synthIndex` to $K$, immediately trigger pre-roll if $K \ge \text{targetBufferCount}$, and dispatch synthesis to the Web Worker for only the remaining sentences ($K \dots N-1$).
    - If slice content has changed (differing `contentHash`), discard any stale partial entries and synthesize from sentence 0.
  - As each new chunk arrives from the worker, append it to the partial cache in IndexedDB. Upon completing all sentences, write the assembled full-slice audio to the permanent cache and clean up the temporary partial cache.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update the on-device synthesis and dual-engine specifications to require Web Worker task cancellation on engine switch, strict engine state isolation for synthesis UI indicators, and content-hash-validated partial chunk caching and resumable synthesis.

## Impact

- Frontend: `frontend/workers/ttsSynth.worker.ts`, `frontend/composables/useSliceAudio.ts`, `frontend/components/reader/ReaderAudioPlayer.vue`, `frontend/tests/composables/useSliceAudio.spec.ts`, `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts`.
- User Experience: Completely eliminates ghost synthesis badges when playing Cloud TTS, stops wasted background CPU/GPU cycles, and enables fast resumption of on-device narration without re-generating previously completed sentences.

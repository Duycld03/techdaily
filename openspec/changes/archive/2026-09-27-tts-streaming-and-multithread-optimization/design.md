# Design

## Context

On-device narration in `useSliceAudio.ts` buffers all sentences from `ttsSynth.worker.ts` into memory before building a single complete WAV file. For multi-sentence slices (e.g. 19 sentences in Slice 2), synthesis on a single CPU core takes ~489 seconds, leaving users with an 8-minute silent wait before playback starts.

Furthermore, duplicate COOP/COEP headers emitted simultaneously by Nginx (`nginx.conf`) and Nuxt Nitro (`nuxt.config.ts`), combined with SPA client-side route transitions, prevent the browser from granting `window.crossOriginIsolated`. Without isolation, `SharedArrayBuffer` is blocked and ONNX Runtime Web is restricted to single-threaded CPU execution.

See `proposal.md` for motivation and scope boundaries.

## Goals / Non-Goals

**Goals:**
- **Time-to-Speech under 10 seconds**: Begin audible narration on-device as soon as the first sentence chunk is synthesized (`onChunk`).
- **Seamless Streaming Queue**: Play subsequent sentence chunks in sequence without audible gaps while background synthesis continues.
- **Cache & Seek Invariant**: Upon completion of all sentences in the slice, assemble the complete unified WAV, store it in IndexedDB (`techdaily-audio`), and switch the player to the unified seekable audio without re-synthesis.
- **Clean Cross-Origin Isolation**: Eliminate duplicate COOP/COEP headers via Nginx proxy header hygiene and ensure reader routes establish `window.crossOriginIsolated === true` for multi-threaded WASM parallelism.

**Non-Goals:**
- Modifying the underlying MMS-TTS model weights or replacing Transformers.js.
- Changing Google Cloud TTS server-side narration pipelines.
- Forcing WebGPU on browsers or environments that do not support it (CPU WASM remains the reliable universal fallback).

## Decisions

### 1. Progressive Audio Chaining vs Web Audio API

**Decision:** Use a sequential WAV chunk queue leveraging `HTMLAudioElement` with automatic transition to the full assembled WAV file once synthesis completes.

*Rationale:* 
1. `ReaderAudioPlayer.vue` already interfaces with an `HTMLAudioElement` (`currentTime`, `duration`, `playbackRate`, `play()`, `pause()`).
2. Encoding an individual sentence of 16kHz audio to a WAV Blob via `encodeWav` takes <1ms on the main thread.
3. On first chunk received (`index === 0`):
   - Immediately encode chunk 0 into a temporary WAV Blob and start playback (`status.value = 'ready'`).
   - As subsequent chunks arrive, push them into a playback queue.
   - When the current audio chunk fires `'ended'`, the player advances to the next available chunk in the queue immediately.
4. When the worker emits `done`:
   - Concatenate all raw float32 buffers, encode the complete unified WAV, save to IndexedDB (`techdaily-audio`), and smoothly update the audio source to the complete file preserving playback position (`currentTime`). Full timeline scrubbing is then unlocked.

*Alternative Considered:* Web Audio API `AudioContext` with `AudioBufferSourceNode`. While Web Audio API provides sample-accurate scheduling, it complicates synchronization with media session controls, playback rate adjustments, and existing player UI refs. Sequential chunk playback with full WAV cutover provides zero UX disruption and preserves existing audio contracts.

### 2. Header De-duplication Strategy

**Decision:** Enforce Nginx as the single authoritative source of COOP/COEP headers, and add `proxy_hide_header Cross-Origin-Opener-Policy` and `proxy_hide_header Cross-Origin-Embedder-Policy` in Nginx when proxying to `frontend_upstream`.

*Rationale:*
Nuxt Nitro's `routeRules` also emits COOP and COEP. By instructing Nginx to hide upstream headers before appending its own canonical headers, we guarantee that HTTP responses contain exactly one set of headers, preventing browser header parsing rejections.

```
[Browser Request]
       │
       ▼
 [Nginx Reverse Proxy]
       │ (Hides duplicate upstream COOP/COEP)
       │ (Injects canonical COOP: same-origin & COEP: credentialless)
       ▼
[Nuxt Nitro Upstream]
```

### 3. Cross-Origin Isolated Entry Navigation

**Decision:** Add a client-side navigation check in the reader page / route middleware. If entering `/read/**` and `window.crossOriginIsolated` is `false`, trigger a hard document reload via `window.location.assign(to.fullPath)` instead of an in-memory SPA route swap.

*Rationale:*
`window.crossOriginIsolated` is fundamentally bound to the browsing context created during top-level document navigation. Client-side SPA navigation from `/login` or `/library` cannot elevate an unisolated document to isolated status. A hard reload on reader entry (taking ~100ms) establishes the isolated document context, enabling `SharedArrayBuffer` and multi-threaded WASM.

## Risks / Trade-offs

- **Audio Playback Cadence**: If a single complex sentence takes longer to synthesize than the previous sentence takes to speak, playback could briefly pause waiting for the next chunk.
  *Mitigation:* The player displays progressive synthesis status (`Generating audio... X/Y`), and with multi-threaded WASM active, per-sentence synthesis latency is reduced to ~1.5s–3s, well within spoken sentence durations (4s–8s).
- **Hard Reload on Reader Entry**: Users navigating from the library to a book will experience a standard page reload rather than an instantaneous SPA transition.
  *Mitigation:* Only occurs when `crossOriginIsolated` is not yet active. Once isolated, navigation between slices within the reader remains instant SPA transitions.

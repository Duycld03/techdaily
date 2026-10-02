# Design: Integrate Audio Narration into Daily Focus (/today)

## Context
See `proposal.md` for background and motivation.

`ReaderAudioPlayer.vue` is a production-hardened component encapsulating `useSliceAudio.ts` with both On-Device Neural TTS (WASM/WebGPU via Web Worker and IndexedDB chunk cache) and Google Cloud TTS (backend API with male/female voice selection and quota monitoring).

Currently:
1. `ReaderAudioPlayer.vue` is used exclusively in `frontend/pages/read/[bookId].vue`.
2. `DocReaderPane.vue` (the reading pane on `/today`) receives `props: { chunk: DocumentChunkDto }` from `pages/today.vue`.
3. Backend `GetTodayFocusHandler.cs` already executes JIT AI formatting and serializes `IsAiFormatted` in `DocumentChunkDto`.
4. In `frontend/stores/useDailyFocusStore.ts`, the TypeScript interface `DocumentChunk` currently lacks the `isAiFormatted?: boolean` property declaration, although the field is present in the runtime JSON response.
5. The `/today` route operates without cross-origin isolation headers (`COOP: same-origin` / `COEP: credentialless`) to protect OAuth login popups (`window.opener`). In non-isolated contexts, `ttsSynth.worker.ts` automatically runs in single-threaded WASM mode (`numThreads = 1`), which synthesizes 2-sentence pre-rolls within ~1.5s on desktop and uses lightweight quantized weights (`q8`) on mobile.

## Goals / Non-Goals

**Goals:**
- **Zero Component Duplication**: Directly reuse `ReaderAudioPlayer.vue` inside `DocReaderPane.vue` with no fork or duplicated playback logic.
- **Immediate Visual Access**: Anchor the audio player container directly below the chapter title heading (`h1`) and above the executive summary callout, ensuring immediate zero-scroll access on both desktop and mobile viewports.
- **Type Contract Alignment**: Add `isAiFormatted?: boolean` to `DocumentChunk` in `frontend/stores/useDailyFocusStore.ts` to guarantee TypeScript compile-time safety when binding to `ReaderAudioPlayer`'s `chunk: ChunkSummary` prop.
- **Clean Lifecycle & Reset**: Ensure audio pauses and resets automatically when switching slices or books, with complete worker termination and audio element pause on route unmount.
- **Comprehensive Unit Testing**: Update `DocReaderPane.spec.ts` to test player rendering, prop forwarding, and gating on `isAiFormatted`.

**Non-Goals:**
- **Backend / Database Changes**: Backend already curates and delivers `IsAiFormatted`; no backend endpoints or migrations are needed.
- **Altering Route Isolation on /today**: Do NOT add COOP/COEP headers to `/today`. Keeping standard headers ensures Google OAuth and top-level navigation remain completely stable without process reload loops.
- **Altering Audio Narration Engines**: Do NOT alter `useSliceAudio.ts` or `ttsSynth.worker.ts`; the existing engine already handles single-threaded fallback and cloud API seamlessly.

## Decisions

### 1. Direct Component Reuse of `ReaderAudioPlayer.vue`
**Decision:** Import and render `ReaderAudioPlayer.vue` directly within `DocReaderPane.vue`:
```vue
<div v-if="chunk.isAiFormatted" class="mb-4">
  <ReaderAudioPlayer :chunk="chunk" />
</div>
```
**Rationale:** `ReaderAudioPlayer.vue` is completely decoupled from page routing and accepts a `chunk` prop. It already handles engine switching (Cloud vs Device), speed control, volume/seeking, and error toasts. Reusing it guarantees feature parity between `/today` and `/read/[bookId]` with zero code duplication.

### 2. Header Placement Below Chapter Title (`h1`)
**Decision:** Position the player container directly below `h1 {{ chunk.chapterTitle }}` and above the summary callout paragraph (`cleanSummary`).
**Rationale:** On desktop dual-pane and mobile stacked layouts, placing the player right after the title gives the user immediate 1-click access to play audio while following along with the summary and deep-dive text. Placing it lower (e.g. below the summary or takeaways) would force users to scroll on mobile before discovering audio playback.

### 3. Graceful Non-Isolated WASM Execution on `/today`
**Decision:** Maintain `/today` as a standard browsing context without injecting `COOP: same-origin` or `COEP: credentialless`.
**Rationale:**
- In `ttsSynth.worker.ts`, the engine checks `self.crossOriginIsolated`. When false, it sets `onnxWasm.numThreads = 1`.
- Because on-device synthesis buffers only 2 sentences for pre-roll playback, single-threaded WASM starts audible speech within ~1.5 seconds on desktop.
- On mobile devices, inference is already single-threaded and quantized (`q8`).
- Google Cloud TTS runs over standard HTTPS API endpoints and is completely unaffected by origin isolation.
- Avoiding COOP/COEP on `/today` prevents cross-origin navigation process boundaries and eliminates reload loops.

## Risks / Trade-offs

- **Risk: Audio continues playing when switching slices or books.**
  - *Mitigation:* `ReaderAudioPlayer.vue` already contains an active watcher:
    ```ts
    watch(() => props.chunk?.id, () => {
      pause()
      loadedId.value = null
    })
    ```
    This immediately halts audio playback and resets the buffer whenever the active chunk ID changes.
- **Risk: Memory leaks or lingering Web Workers when navigating away from `/today`.**
  - *Mitigation:* `useSliceAudio.ts` implements `onScopeDispose(() => dispose())`, ensuring worker termination, blob URL revocation, and `<audio>` element pausing upon component destruction.

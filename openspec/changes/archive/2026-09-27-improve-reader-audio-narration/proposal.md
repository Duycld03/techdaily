# Proposal

## Why

After `fix-reader-audio-narration-defects`, on-device narration is correct but has three quality/usability problems observed on real documents, plus one root cause of wrong-voice selection:

1. **Unusably slow synthesis.** On a real slice the label sat at *"Đang tạo audio… 2/22"* for minutes. Synthesis runs single-threaded on CPU WASM.
2. **Orphaned audio + worker leak on navigation.** Leaving the reader (e.g. to the knowledge graph) does not stop playback or release the TTS worker.
3. **Wrong voice from mis-tagged language.** English documents are narrated with the Vietnamese voice because the document's language is taken from the UI locale at upload time, not chosen for the document.

## What Changes

### 1. Faster synthesis: GPU acceleration (frontend worker)
- `workers/ttsSynth.worker.ts:27` calls `pipeline('text-to-speech', model, { progress_callback })` with **no `device`**, so Transformers.js defaults to CPU WASM.
- Change: attempt `device: 'webgpu'` when `navigator.gpu` is available in the worker, and fall back to `wasm` on any pipeline-creation failure. The cached pipeline records which device won so failures are not retried per sentence.

### 2. Faster synthesis: multi-threaded WASM via cross-origin isolation (frontend headers)
- onnxruntime-web downloads `ort-wasm-simd-threaded.*` but cannot spawn threads because the page is **not** cross-origin isolated (`self.crossOriginIsolated === false`); `nuxt.config.ts` sets no COOP/COEP headers. Threads require `SharedArrayBuffer`, which requires isolation.
- Change: send `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` **scoped to `/read/**` only** (Nuxt route rules). Reader pages become isolated (threads enabled) while `/login` Google OAuth (which relies on `window.opener`/`postMessage` and would break under COOP `same-origin`) and the rest of the app are unaffected.

### 3. Stop narration and release resources on reader unmount (frontend composable)
- `composables/useSliceAudio.ts:280` defines `dispose()` (terminate worker, revoke object URL, pause audio) but nothing calls it: neither the composable nor `components/reader/ReaderAudioPlayer.vue` registers `onScopeDispose`/`onUnmounted`. Navigating away leaves the `<audio>` playing with no visible controls and leaks a Web Worker + loaded model per reader visit.
- Change: register `onScopeDispose(dispose)` inside `useSliceAudio` (guarded so direct test usage outside a component scope does not warn), cancel the in-flight synthesis (clear `activeKey` so a late-resolving synth cannot start playback), and terminate the worker.

### 4. Explicit document language at upload/import (frontend modals)
- `pages/library.vue:465` and `:540` send `language: locale.value` (the current **UI** locale). The upload/import modals have **no** language field, so an English document uploaded while the UI is Vietnamese is tagged `vi`, and the Vietnamese voice later reads English text.
- Change: add an accessible language selector (`AppSelect`, `en`/`vi`) to the upload and import-remote-pdf modals, defaulting to the current UI locale but user-overridable, and send the chosen value as `language`. Backend contract is unchanged (`UploadPdf`/`ImportRemotePdf`/`ImportDocument` already accept `Language`).

## Capabilities

### New Capabilities
- none

### Modified Capabilities
- `audio-narration`: (a) on-device synthesis MUST use GPU acceleration when available and fall back to CPU WASM otherwise; (b) synthesis MUST be able to use multi-threaded WASM on reader routes via cross-origin isolation, without affecting authentication or other routes; (c) narration MUST stop playback and release the synthesis worker when the reader view is left.
- `library`: document upload and remote-import MUST let the user choose the document's language explicitly rather than silently inheriting the interface locale.

## Impact

- **Frontend**:
  - `workers/ttsSynth.worker.ts` — WebGPU-with-WASM-fallback device selection.
  - `nuxt.config.ts` — COOP/COEP route rules scoped to `/read/**`.
  - `composables/useSliceAudio.ts` — `onScopeDispose` cleanup + in-flight cancel.
  - `pages/library.vue` — language selector in upload + import modals (bound refs, sent as `language`).
  - i18n: add upload-language label/keys to `en.json`/`vi.json`.
- **Backend**: none (language already plumbed end-to-end).
- **Tests**: extend `tests/composables/useSliceAudio.spec.ts` (dispose terminates the engine and prevents post-dispose playback); library upload payload test asserts the chosen `language` is sent. CSS/layout assertions excluded per repo testing boundaries.

## Verification (acceptance)

- On a reader route, `self.crossOriginIsolated === true`; on `/login` it is `false` and Google OAuth still works.
- Where WebGPU is available, synthesis uses it; where absent (or on failure), it falls back to WASM and still completes; overall synthesis is faster than the single-threaded WASM baseline.
- Navigating away from the reader stops audio and terminates the worker (no orphaned playback; no leaked worker).
- Uploading a document with the language selector set to English tags its chunks `en`, and narration uses the English voice regardless of UI locale.
- Gate 1 (`npm test`) green; Gate 2 desktop+mobile screenshots of the reader and the upload modal in `en` and `vi`.

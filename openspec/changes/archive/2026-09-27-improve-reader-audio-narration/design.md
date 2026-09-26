# Design

## Context

See `proposal.md - Why`. Current state relevant to the approach:

- Synthesis worker `frontend/workers/ttsSynth.worker.ts` builds a Transformers.js `pipeline('text-to-speech', model, { progress_callback })` with **no `device`** → CPU WASM by default. onnxruntime-web fetches `ort-wasm-simd-threaded.*` but cannot use threads because the page is not cross-origin isolated.
- `frontend/nuxt.config.ts` sets no COOP/COEP headers; `/login` uses Google OAuth whose popup relies on `window.opener`/`postMessage`.
- `frontend/composables/useSliceAudio.ts` exposes `dispose()` (terminate worker, revoke object URL, pause audio) and guards late synthesis with `if (activeKey !== key) return`, but nothing calls `dispose()` on unmount; `ReaderAudioPlayer.vue` registers no lifecycle hook.
- `frontend/pages/library.vue` sends `language: locale.value` for both upload (`upload-pdf`) and remote import; the modal (`3-Tab Import Modal`) has no language field. Backend `UploadPdf`/`ImportRemotePdf`/`ImportDocument` already accept a `Language` argument that flows to `DocumentChunk.Language`.
- `@huggingface/transformers@4.3.0`; `AppSelect.vue` is the mandated accessible select (native `<select>` prohibited).

## Goals / Non-Goals

**Goals:**
- Cut synthesis wall-time via GPU when available and multi-threaded WASM otherwise, without changing produced audio or controls.
- Guarantee narration stops and frees its worker/model when the reader is left.
- Let users set the document language at import so narration voice matches content.

**Non-Goals:**
- Automatic content-based language detection (explicit selector only).
- New languages/voices beyond `en`/`vi`.
- Backend changes (language is already plumbed end-to-end).
- Changing the audio format, caching keys, or the sentence-by-sentence progress model.

## Decisions

### D1. WebGPU with a hard CPU fallback, decided per model
In `getPipeline(model)`, probe `('gpu' in navigator)` inside the worker. Attempt `pipeline('text-to-speech', model, { device: 'webgpu', progress_callback })`. Because a VITS op can succeed at pipeline creation but throw at first inference on WebGPU, wrap the **first** `synth(sentence)` call: on error while the active device is `webgpu`, rebuild the pipeline with `{ device: 'wasm' }`, replace the cached entry, and retry that sentence once. When `navigator.gpu` is absent, build `wasm` directly. The cache stores the resolved backend so later sentences never re-probe or repeat a failed init. Progress forwarding is unchanged (already filtered to the `.onnx` weights).

Rationale: WebGPU is the largest speedup where present; the inference-time fallback makes it safe on browsers that advertise but cannot run the model (e.g. Brave with partial WebGPU).

### D2. Cross-origin isolation scoped to `/read/**` via Nitro route rules
Add to `nuxt.config.ts`:
```ts
routeRules: {
  '/read/**': {
    headers: {
      'Cross-Origin-Opener-Policy': 'same-origin',
      'Cross-Origin-Embedder-Policy': 'credentialless',
    },
  },
}
```
`COOP: same-origin` + `COEP: credentialless` makes reader pages `crossOriginIsolated`, enabling `SharedArrayBuffer` and onnxruntime threads. Scoping to `/read/**` keeps `/login` non-isolated so the OAuth popup's `window.opener` channel keeps working. `credentialless` (not `require-corp`) lets cross-origin **no-credential** subresources — Google Fonts, the HF model CDN fetched by the worker, and public document images — load without each origin sending a CORP header.

Rationale: threads are the reliable speedup that does not depend on a GPU; the narrow scope avoids the app-wide OAuth breakage that full-site COOP `same-origin` would cause.

### D3. Auto-dispose on reader scope teardown
Inside `useSliceAudio`, register cleanup guarded so direct (non-component) test usage does not warn:
```ts
if (getCurrentScope()) onScopeDispose(() => dispose())
```
Extend `dispose()` to also set `activeKey = null` and `status.value = 'idle'` so an in-flight synthesis that resolves after teardown hits the existing `activeKey !== key` guard and cannot start playback. The worker engine's `dispose()` terminates the worker and SHALL reject any pending synth handlers so the awaiting `loadAndPlay` unwinds instead of leaking a suspended promise.

Rationale: `onScopeDispose` fires exactly on the owning component's unmount (route change), which is the precise "left the reader" signal; no new watcher on route is needed.

### D4. Explicit language selector in the import modal
Add `uploadLanguage` and `importLanguage` refs in `library.vue`, each defaulting to `locale.value`, rendered with `AppSelect` (options `en`/`vi`, localized labels). Send the ref value (not `locale.value`) as `language` in the upload `FormData` and the import payload. Add i18n keys (`library.document_language` + option labels) to `en.json`/`vi.json`.

Rationale: reuses the mandated accessible select; smallest change that makes document language a deliberate choice; backend contract unchanged.

## Risks / Trade-offs

- **COEP breaks a cross-origin reader asset.** `credentialless` covers public no-credential fetches; an asset that requires credentials or lacks CORS could fail to load on reader pages only. Mitigation: keep isolation scoped to `/read/**`; if a required asset breaks, add CORP/`crossorigin` on that asset. Verify fonts, the model CDN fetch, and a document with external images during Gate 2.
- **WebGPU unavailable/disabled** (e.g. Brave default): D1 falls back to WASM; the D2 threads still deliver a speedup, so the feature is not GPU-dependent.
- **Time-to-first-audio unchanged in shape.** These changes shorten total synthesis time but do not alter the "synthesize whole slice before playing" model from the prior change; sentence progress remains the legibility mechanism.
- **`credentialless` browser support.** Where unsupported, the reader is simply not isolated and synthesis stays single-threaded WASM (current behavior) — no regression.

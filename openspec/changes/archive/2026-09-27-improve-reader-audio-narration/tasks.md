# Tasks

## 1. Frontend: GPU-accelerated synthesis with CPU fallback

- [x] 1.1 In `workers/ttsSynth.worker.ts`, make `getPipeline(model)` probe `('gpu' in navigator)` and build the pipeline with `{ device: 'webgpu', progress_callback }` when present, else `{ device: 'wasm', progress_callback }`. Keep the existing `.onnx`-only progress filter. Cache the resolved backend alongside the pipeline so it is decided once per model.
- [x] 1.2 Wrap the first `synth(sentence)` call so that, when the active backend is `webgpu` and inference throws, the worker rebuilds the pipeline with `{ device: 'wasm' }`, replaces the cached entry, and retries that sentence once; subsequent sentences reuse the fallback without re-probing. Verify the produced audio and the streamed `chunk` messages are unchanged versus the WASM path.
- [x] 1.3 Smoke-verify in-browser: on a WebGPU-capable context synthesis runs and completes; with WebGPU forced unavailable it falls back to WASM and still completes and plays. Record the observed backend for each run.

## 2. Frontend: multi-threaded WASM via cross-origin isolation on reader routes

- [x] 2.1 Add `routeRules` in `nuxt.config.ts` setting `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` for `/read/**` only. Do not apply these headers to any other route.
- [x] 2.2 Verify in-browser that a reader route reports `self.crossOriginIsolated === true` while `/login` reports `false`, and that Google OAuth sign-in on `/login` still completes (popup `window.opener` channel intact).
- [x] 2.3 Verify reader cross-origin assets still load under the policy: web fonts render, the on-device model CDN fetch in the worker succeeds, and a slice containing an external image renders. Note any asset that fails so a CORP/`crossorigin` follow-up can be scoped.

## 3. Frontend: narration lifecycle cleanup on reader exit

- [x] 3.1 In `composables/useSliceAudio.ts`, register `if (getCurrentScope()) onScopeDispose(() => dispose())`, and extend `dispose()` to set `activeKey = null` and `status.value = 'idle'` so an in-flight synthesis resolving after teardown cannot start playback (existing `activeKey !== key` guard).
- [x] 3.2 In the worker engine (`createWorkerEngine`), make `dispose()` reject any pending synth handlers (in addition to `worker.terminate()`), so an awaiting `loadAndPlay` unwinds instead of leaking a suspended promise.
- [x] 3.3 Update `tests/composables/useSliceAudio.spec.ts`: assert that calling `dispose()` while a synthesis is in-flight terminates the engine (engine `dispose` called / worker terminated) and that a synthesis completion arriving after `dispose()` does NOT set a playback source or call `play()`.

## 4. Frontend: explicit document language selection at import

- [x] 4.1 In `pages/library.vue`, add `uploadLanguage` and `importLanguage` refs (each defaulting to `locale.value`) and render an `AppSelect` (options `en`/`vi`, localized labels) in the PDF Upload and URL/remote-import forms of the import modal.
- [x] 4.2 Send the selected ref value as `language` in the `upload-pdf` `FormData` (`pages/library.vue:465`) and the `importRemotePdf` payload (`:540`), replacing `locale.value`.
- [x] 4.3 Add i18n keys for the document-language label and option labels to `i18n/locales/en.json` and `vi.json`. Verify `npm test` runs with no missing-i18n-key warnings.
- [x] 4.4 Add/extend a Vitest test asserting the upload path sends the chosen `language` (e.g. selecting English under a `vi` interface serializes `language: "en"` in the submitted payload), following the data-contract testing boundary (no CSS/layout assertions).

## 5. Verification (integration + visual)

- [x] 5.1 Run `npm test` (frontend); verify the suite passes 100% (Gate 1). Backend unchanged — no `dotnet test` required for this change.
- [x] 5.2 Gate 2 - against the running app: (a) reader route is cross-origin isolated and synthesis is faster than the single-threaded WASM baseline (report the observed backend and rough timing); (b) navigating from the reader to another view stops audio and terminates the worker (no orphaned playback); (c) uploading an English document with the language selector set to English under a `vi` interface tags it `en` and narrates with the English voice. Capture Desktop (1920x1080) and Mobile (390x844) screenshots of the reader and the import modal in `en` and `vi` and present them.
- [ ] 5.3 Precondition for archive only: confirm `add-reader-audio-narration` and `fix-reader-audio-narration-defects` are applied and archived before archiving this change, since the `audio-narration` delta here appends to requirements those changes establish.

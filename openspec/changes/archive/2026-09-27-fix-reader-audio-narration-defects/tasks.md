# Tasks

## 1. Backend: expose per-slice language on the library contract

- [x] 1.1 Add a `Language` string property (default `"en"`) to the library `ChunkSummaryDto` and verify `dotnet build` succeeds.
- [x] 1.2 Populate `Language` from `DocumentChunk.Language` everywhere the Library feature builds a `ChunkSummaryDto` — the `GetBookById` book-detail chunk list, the `GetBookSlice` single-slice response, and the `CurateSlice` response. Verify with a handler test (or an authenticated request against a `vi`-ingested book) that book-detail chunks and the single-slice response carry `language == "vi"`.

## 2. Frontend: regenerate contract and resolve voice from slice language

- [x] 2.1 With the backend running, regenerate types via `npm run gen:api` and verify the diff is limited to adding `language` on `ChunkSummaryDto`.
- [x] 2.2 Confirm `ChunkSummary.language` flows through `stores/useLibraryStore.ts` (slice/curate spreads) and that `ReaderAudioPlayer.vue` forwards `chunk.language` into the narration source, defaulting to `en` only when absent. Verify with a Vitest test that a `vi` slice resolves the vi voice (`resolveVoiceForLanguage` receives `"vi"`) and a slice with no language resolves `en`.

## 3. Frontend: accurate download progress percentage

- [x] 3.1 Render the model-download percentage in `ReaderAudioPlayer.vue` as `Math.round(downloadProgress.value)` clamped to `[0, 100]` (drop the extra `* 100`), keeping the worker/composable value on the native `0–100` scale. Verify with a Vitest test that an injected engine emitting `onProgress({ stage: 'download', progress: 42 })` leaves `downloadProgress.value === 42` (no re-scaling), and that a `100` progress renders `100%`, never `10000%`.
- [x] 3.2 Fix the download bar flashing `100%` then restarting `0→100%`: Transformers.js fires `progress` per file, so the tiny config/tokenizer JSONs each hit `100%` before the large `.onnx` weights start. In `workers/ttsSynth.worker.ts`, surface `stage:'download'` progress only for the `.onnx` model-weights file. Verified live in-browser: label now goes `Preparing… → Downloading 0% → … → 100%` monotonically (no reset).

## 4. Frontend: continuous full-slice playback

- [x] 4.1 Rework `useSliceAudio.loadAndPlay` to stop playing the throwaway first-sentence clip and stop swapping sources: synthesize all sentences (emitting `synth` progress `index/count`), assemble a single complete WAV, cache it, set it as the sole `<audio>` source, then play. Verify with a Vitest test that, for a streaming engine emitting N sentence chunks, `engine.synthesize` is called once, the source is set exactly once (to the assembled complete blob), exactly one blob is cached, and playback starts only after assembly.
- [x] 4.2 Surface the synthesis progress state (`index`/`count`) from the composable and render a localized "generating i/N" message in the control; add the `reader.audio_synthesizing` key (with `{current}`/`{total}`) to `i18n/locales/en.json` and `vi.json`. Verify with a Vitest test that the composable exposes the synth index/count from `onProgress`, and that `npm test` runs with no missing-i18n-key warnings.
- [x] 4.3 Update `tests/composables/useSliceAudio.spec.ts` to the new flow — remove assertions tied to per-sentence source swapping and assert continuous playback of the single complete file with a duration derived from the full assembled audio. Verify the updated spec passes.

## 5. Verification (integration + visual)

- [x] 5.1 Run `dotnet test` (backend) and `npm test` (frontend); verify both suites pass 100% (Gate 1). Result: backend 315/315 passed, frontend 71 files / 537 tests passed.
- [x] 5.2 Gate 2 - against the running app on a Vietnamese (`vi`) AI-formatted slice: confirm the download label shows a `0–100%` value, the vi voice is used, playback plays the entire slice continuously with a full-length seekable scrubber and correct total duration, and replaying the slice loads from IndexedDB without re-synthesis. Capture Desktop (1920x1080) and Mobile (390x844) screenshots in `en` and `vi` and present them; note any on-device model-execution limitation of the environment. Result: verified on book `6ed17379…` slice 1 (vi). Download label rendered `Downloading voice… 100%` (0–100 scale, never `10000%`); the vi voice model `Xenova/mms-tts-vie` was fetched for the vi slice; synth-progress label incremented live (`Đang tạo audio… 1/58 → 4/58`). Desktop+Mobile screenshots in `en`/`vi` captured and presented. Limitation: on-device WASM TTS synthesis of all 58 sentences is too slow on this headless CPU to finish within the session, so completed continuous playback, the full-length scrubber/duration, and IndexedDB replay could not be observed live in-browser; those are covered by the `useSliceAudio` unit tests (single assembled source set once, one cached blob, cache-hit replay without re-synthesis).
- [x] 5.3 Precondition for archive only: confirm `add-reader-audio-narration` is applied and archived before archiving this change, since the `audio-narration` delta MODIFIES its requirements (the `library` delta has no such dependency). Status: `add-reader-audio-narration` is complete (13/13 tasks, applied) but NOT yet archived. It MUST be archived before this change is archived.

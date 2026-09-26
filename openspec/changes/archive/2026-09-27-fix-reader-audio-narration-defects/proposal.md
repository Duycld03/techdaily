# Proposal

## Why

On-device narration (shipped in `add-reader-audio-narration`) has three defects observed on a real Vietnamese document at `/read/<id>?slice=1`:

1. Download indicator renders **"Đang tải giọng đọc... 10000%"** — a nonsense percentage.
2. Playback stops after ~1s and the scrubber shows **0:01 / 0:01** instead of the full slice.
3. On a **Vietnamese** slice the **English** voice is used ("nó đọc tiếng anh").

Each has a distinct root cause: a frontend scaling bug, a streaming-playback design flaw, and a backend contract gap. The feature is unusable for its primary bilingual case until fixed.

## What Changes

### 1. Progress percentage (frontend only)
- Transformers.js `progress_callback` reports `progress` as a **0–100 percentage** already. The worker forwards it verbatim (`workers/ttsSynth.worker.ts:26` → `progress: info.progress ?? 0`), and the component multiplies by 100 again (`components/reader/ReaderAudioPlayer.vue:94` → `Math.round(downloadProgress.value * 100)`), yielding `~100 * 100 = 10000%`.
- Fix: render `Math.round(downloadProgress.value)` (drop the extra `* 100`). Clamp to `[0, 100]` defensively.

### 2. Continuous playback + accurate duration (frontend design change)
- Current flow (`composables/useSliceAudio.ts:204–243`): synthesize sentence-by-sentence; on the **first** chunk, encode a one-sentence WAV, flip `status` to `ready`, and start playback; only **after all** sentences finish, assemble the complete WAV and swap the `<audio>` source. The first clip ends (~1s), fires `ended` (`playing=false`), and the end-of-run swap sets `currentTime` to the old position without resuming — so audio halts at ~1s and `duration` reflects the throwaway first clip.
- Fix (recommended, boring/correct): **do not** play per-sentence clips or swap sources. Synthesize all sentences while showing a `synth` progress state ("Đang tạo audio... i/N"), assemble a single complete seekable WAV, cache it, then set it as the only source and play. Guarantees continuous playback, correct total duration, and free seeking.
- Tradeoff: time-to-first-audio equals full-slice synthesis time (vs. ~1 sentence today). Accepted: correctness over a broken fast-start; results are cached in IndexedDB so replays are instant. Sentence-level progress keeps the wait legible.

### 3. Narration language from the document (**BREAKING** API contract: backend + frontend)
- The library slice contract omits language: `ChunkSummaryDto` has no `Language` field (`frontend/types/api.generated.ts:1147–1159`), and neither does `BookDetailDto` (`:1082–1098`). The store maps responses straight through (`stores/useLibraryStore.ts:268–271, 310–313`), so `chunk.language` is always `undefined` and `resolveVoiceForLanguage(undefined)` falls back to English (`utils/ttsVoices.ts:29`).
- The data exists server-side: `DocumentChunk.Language` (entity default `"en"`, `Persistence/Configurations/EntityConfigurations.cs:199`) and is already exposed on the DailyFocus `DocumentChunkDto` (`Application/.../DailyFocus/DTOs/DailyFocusDtos.cs` + handler `:561`) — the **Library** endpoints simply do not project it.
- Fix:
  - Backend: add `Language` to `ChunkSummaryDto` and populate it in the GetBookSlice, GetBookById (book-detail chunks), and CurateSlice mappings.
  - Regenerate `frontend/types/api.generated.ts`.
  - Frontend: resolve the narration voice from the slice `Language`, falling back to the book language, then `en`. (`ChunkSummary.language` already added; `ReaderAudioPlayer` already forwards `chunk.language`.)
- **BREAKING** only in the additive sense that the slice DTO shape changes; no DB migration (column already exists).

## Capabilities

### New Capabilities
- none

### Modified Capabilities
- `library`: slice, book-detail, and curate responses MUST expose the per-slice `Language` so clients can act on document language (behavioral contract change, not just implementation).
- `audio-narration` (introduced by `add-reader-audio-narration`; corrected here): (a) the narration voice MUST be derived from the slice/document `Language` (default `en` only when unknown); (b) the model-download indicator MUST display a `0–100%` value; (c) playback MUST present the entire slice as one continuous, seekable track with a correct total duration, rather than a truncated first-sentence clip.

## Impact

- **Backend**: `ChunkSummaryDto` + three mapping sites (GetBookSlice, GetBookById, CurateSlice). No schema/migration change.
- **Frontend**:
  - `components/reader/ReaderAudioPlayer.vue` — progress label scaling.
  - `composables/useSliceAudio.ts` — remove first-sentence play/swap; add `synth` progress; single complete-file source; slice→book language fallback.
  - `types/api.generated.ts` — regenerated (adds `ChunkSummaryDto.language`).
  - `stores/useLibraryStore.ts` — `ChunkSummary.language` already present; verify pass-through.
- **Tests**: update `tests/composables/useSliceAudio.spec.ts` (no per-sentence swap; progress is 0–100; voice resolves from slice/book language); keep util specs. Re-run Gate 1 + Gate 2 (verify "Downloading voice... N%", full-length scrubber, Vietnamese voice on a `vi` slice).

## Verification (acceptance)

- On a `vi` slice: the download label shows a sane `0–100%`; the Vietnamese voice (`Xenova/mms-tts-vie`) is used.
- Playback plays the whole slice continuously; the scrubber shows the true total duration and is seekable start→end.
- Replaying a synthesized slice loads from IndexedDB with no re-synthesis.
- Gate 1 (`npm test`) green; Gate 2 desktop+mobile screenshots in `en` and `vi`.

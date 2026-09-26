# Design

## Context

See `proposal.md` - Why. Three defects were observed on a live Vietnamese slice: a `10000%` download label, playback that stalls at `0:01`, and the English voice on Vietnamese content. The narration feature (`add-reader-audio-narration`) is implemented but not yet archived; its behavior contract lives in that change's `audio-narration` delta. Relevant current state:

- Transformers.js `progress_callback` emits `progress` as a `0–100` percentage; the worker forwards it and the component multiplies by 100 again.
- `useSliceAudio` plays a one-sentence WAV first, flips to `ready`, then swaps to the complete WAV only after full synthesis - which halts playback on the first clip and reports its short duration.
- The library slice contract (`ChunkSummaryDto`, `BookDetailDto`) omits `Language`, though `DocumentChunk.Language` exists and DailyFocus's `DocumentChunkDto` already exposes it.

## Goals / Non-Goals

**Goals:**
- Display model-download progress as an accurate `0–100%`.
- Play the entire slice continuously with a correct, seekable total duration.
- Use the on-device voice matching the document's language.
- Keep all synthesis client-only; no DB migration.

**Non-Goals:**
- Gapless *incremental* playback while synthesis is still running (fast-start is dropped in favor of correctness).
- New voices/languages beyond the existing `en`/`vi`.
- Changes to how ingestion determines a document's language (only its exposure over the API).

## Decisions

### D1 - Treat Transformers.js `progress` as an authoritative 0–100 percentage end-to-end
The worker forwards `progress` (0–100), the composable stores it unchanged, and the component renders `Math.round(value)` clamped to `[0, 100]`. Remove the `* 100` at the display site.
- Alternative: normalize to a 0–1 fraction in the worker and scale once in the UI. Rejected - it adds a conversion the library already did and invites the same double-scaling bug elsewhere.

### D2 - Synthesize the whole slice, then play a single complete file
Drop the "play the first sentence immediately" path. Synthesize sentence-by-sentence while showing a synthesis progress state ("Đang tạo audio... i/N"), assemble one complete WAV (existing `audioWav.encodeWav` + `concatFloat32`), cache it, then set it as the **sole** `<audio>` source and play. This yields continuous playback, a correct total duration, and free seeking, and removes the fragile source-swap.
- Alternative: MediaSource Extensions appending WAV as it streams for a growing seekable buffer. Rejected - WAV is not a fragmented/streamable MSE container without custom muxing; high complexity and browser fragility for a bounded on-device slice.
- Alternative: keep fast-start and just fix the resume/duration bookkeeping on swap. Rejected - two live sources and a moving duration remain error-prone; the boring single-file model is simpler and correct.
- Trade-off: time-to-first-audio grows to full-slice synthesis time. Mitigated by visible per-sentence progress and the existing IndexedDB cache (replays are instant).

### D3 - Expose per-slice `Language` on the library contract
Add `Language` to `ChunkSummaryDto` and populate it from `DocumentChunk.Language` in the three Library mappings (`GetBookById` book-detail chunks, `GetBookSlice`, `CurateSlice`). Regenerate `frontend/types/api.generated.ts`. The frontend resolves the narration voice from the slice's `language`, defaulting to `en` only when absent.
- Alternative: expose only a book-level `language` and have the client apply it to every slice. Rejected - the single-slice endpoint returns a slice with no book context on the client, per-slice `Language` is the entity's own field, and DailyFocus already projects per-chunk language (consistency). Per-slice is authoritative; a book-level value would be redundant.

### D4 - Stack on top of `add-reader-audio-narration`
This change's `audio-narration` delta MODIFIES requirements that the sibling change introduces, so it MUST be applied and archived **after** `add-reader-audio-narration`. The `library` delta is independent and can archive at any time.

## Risks / Trade-offs

- Slower first-audio after D2 → per-sentence synthesis progress + IndexedDB cache keep the wait legible and one-time.
- Archive/apply ordering (D4) → documented as an explicit task precondition; the `audio-narration` base must exist before this change archives.
- Regenerated API types could drift beyond the intended field → regenerate with the project's `openapi-typescript` script and review that the diff is limited to `ChunkSummaryDto.language`.
- Books ingested before language capture default to `en` at the DB level → out of scope; re-ingesting corrects them. The mapping only projects the stored value.
- `<audio>.duration` accuracy → the assembled WAV writes correct RIFF/`data` sizes (existing `audioWav.encodeWav`), so duration is deterministic once the complete file is the source.

## Migration Plan

- No database migration (`DocumentChunk.Language` and its `en` default already exist).
- Ship the backend DTO change and the regenerated frontend types together (single additive contract change). Rollback: revert both; the added field is additive and low-risk.
- Apply order: implement/verify after `add-reader-audio-narration`; archive this change only once that sibling is archived (D4).

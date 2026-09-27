# Design: Sub-Chunk TTS Narration & Bilingual AI Curation

## Context

TechDaily provides dual-engine slice narration and AI-assisted reading curation. See `proposal.md` for background motivation.
- Google Cloud Text-to-Speech enforces a 5,000-byte UTF-8 payload ceiling per single `synthesize` call. Slices with detailed prose or multi-byte characters (such as Vietnamese diacritics where each character is 2-3 bytes) frequently exceed this threshold and fail with HTTP 400 `INVALID_ARGUMENT`.
- `GeminiAiService.FormatSliceAsync` uses a static English system instruction and English JSON response schema template for all documents, causing Gemini 3.5 Flash Lite to produce English executive callouts (`> [!NOTE]`, `> [!TIP]`, `> [!WARNING]`) even for Vietnamese source books.

## Goals / Non-Goals

**Goals:**
- Enable seamless synthesis of arbitrary-length slices through automatic, sentence-bounded sub-batching under 4,500 bytes and byte-level MP3 stream concatenation in `GoogleCloudTtsService`.
- Strictly enforce document language fidelity in `GeminiAiService.FormatSliceAsync` so all synthesized callouts, takeaways, and drills match the document's original language (`vi` vs `en`).
- Maintain zero latency regressions on cache hits ($<50\text{ms}$ from PostgreSQL or IndexedDB).

**Non-Goals:**
- Migrating to Google Cloud Long Audio API (which requires Google Cloud Storage buckets and asynchronous job polling, adding unnecessary operational overhead and latency for 1-5 minute reader slices).
- Re-encoding or transcoding audio through external binaries (FFmpeg). MP3 frame concatenation is native and lossless.

## Decisions

### Decision 1: Sentence-Bounded Sub-Batching with MP3 Binary Concatenation

In `GoogleCloudTtsService.cs`:
1. Check UTF-8 byte length: `Encoding.UTF8.GetByteCount(text)`.
2. If $\le 4,500$ bytes: execute single API call as today.
3. If $> 4,500$ bytes:
   - Split `text` into sentences using regex boundary delimiters (`(?<=[.!?\n])\s+`).
   - Group sentences into chunks such that each chunk does not exceed 4,000 bytes UTF-8.
   - Synthesize each chunk in sequence via Google Cloud TTS.
   - Combine all returned audio byte arrays into a single `MemoryStream` / byte array.
   - MP3 (MPEG-1 Audio Layer III) streams encoded with identical parameters (same voice, bitrate, sample rate) concatenate losslessly without header corruption or audible glitches.
   - Calculate cumulative character count and duration.

*Alternatives Considered:*
- *Google Cloud Long Audio API:* Requires creating a GCS bucket, service account IAM permissions, and asynchronous webhook/polling. Too heavy for 2-5KB text slices.
- *Truncating narration text:* Violates content fidelity and leaves readers with incomplete audio.

### Decision 2: Language-Aware Gemini Prompt Synthesis

In `GeminiAiService.cs`:
1. Determine `isVietnamese = language.Trim().ToLowerInvariant().StartsWith("vi")`.
2. Provide explicit language instructions in `systemInstruction`:
   - If `isVietnamese`:
     - Executive Note (`> [!NOTE]`): Must explain the core principles in 2-3 idiomatic Vietnamese sentences.
     - Alerts (`> [!TIP]`, `> [!WARNING]`, `> [!IMPORTANT]`): Must be written entirely in Vietnamese.
     - Section subheadings and run-in headings: Retain Vietnamese text.
     - Key Takeaways: `### Ý chính cốt lõi` with 3 Vietnamese bullet points.
     - Scenario Drill: Question, 4 options, explanation, and trade-offs in Vietnamese.
   - If English:
     - Retain English prompts and headings (`### Key Takeaways`).
3. Tailor the JSON schema few-shot example to the resolved language to prevent LLM language drift.

*Alternatives Considered:*
- *Post-processing English callouts with translation:* Adds an extra LLM call (latency + cost) and risks awkward machine translation. Generating directly in the source language produces superior idiomatic quality.

## Risks / Trade-offs

- **Multiple HTTP Calls for Oversized Slices:** Slices $>4,500$ bytes will require 2-3 sub-requests to Google Cloud TTS during initial synthesis.
  - *Mitigation:* Slices are typically 800-1,500 words, requiring at most 2-3 sub-calls taking $\approx 1.0 - 1.5\text{s}$. Once synthesized, the combined MP3 is stored in PostgreSQL and browser IndexedDB; all future reads are instant cache hits.
- **Quota Tracking Integrity:** Sub-batching must sum the character count of each sub-request accurately so the monthly quota guard in `GetOrSynthesizeChunkAudioHandler` reflects exact consumption.

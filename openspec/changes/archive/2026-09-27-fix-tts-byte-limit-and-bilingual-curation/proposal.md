# Proposal: Fix Google Cloud TTS Byte Limit & Bilingual Slice Curation

## Why

1. **Google Cloud TTS 5000-Byte Hard Limit:** When readers request narration for slices whose text exceeds 5000 UTF-8 bytes (especially common in multi-byte Vietnamese text and long documentation sections), Google Cloud TTS rejects the API call with HTTP 400 (`input.text is longer than the limit of 5000 bytes`). Audio narration fails and triggers error toast notifications in the reader.
2. **Source Language Preservation Invariant Violation:** During AI slice formatting (`CurateSliceHandler` / `GeminiAiService`), the model generates executive context callouts (`NOTE`, `TIP`, `WARNING`, `IMPORTANT`) in English even when formatting Vietnamese technical books, violating the core platform invariant of strictly preserving the document's original language.

## What Changes

- **Google Cloud TTS Sub-Chunking & MP3 Stream Concatenation:** Update `GoogleCloudTtsService` to detect when narration text exceeds 4,500 bytes UTF-8. The service splits oversized text into sentence-bounded sub-chunks under 4,500 bytes, synthesizes each sub-chunk via Google Cloud TTS, and concatenates the resulting MP3 audio byte streams into a single seamless audio file before caching in PostgreSQL (`DocumentChunkAudios`).
- **Strict Bilingual AI Curation Prompts:** Update `GeminiAiService.FormatSliceAsync` prompt templates and system instructions to explicitly enforce the document's target language for all synthesized elements. When curating a Vietnamese slice (`Language == "vi"`), all GitHub alert callouts (`> [!NOTE]`, `> [!TIP]`, `> [!WARNING]`), key takeaways, and scenario drills MUST be generated in Vietnamese.
- **Language Normalization Guarantee:** Ensure document chunks and remote PDF ingestion jobs correctly propagate and persist the normalized language tag (`en` or `vi`) across curation and narration pipelines.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Add transparent sentence-bounded sub-chunking and MP3 byte concatenation for narration text exceeding Google Cloud TTS single-request byte limits.
- `library`: Enforce document language fidelity across AI-generated structural elements (callout boxes, executive summaries, scenario challenges).

## Impact

- **Reliability:** Readers can listen to arbitrary-length slices in any language without encountering 400 Bad Request errors from Google Cloud TTS.
- **User Experience & Invariant Compliance:** Vietnamese readers will see coherent, native Vietnamese callout boxes and takeaways matching the book's narrative content, eliminating jarring English/Vietnamese language collisions.
- **Backward Compatibility:** Cached audio records and existing markdown formatting remain unaffected.

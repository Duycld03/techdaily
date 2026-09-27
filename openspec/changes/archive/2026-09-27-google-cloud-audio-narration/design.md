# Design

## Context

See proposal.md — Why. Currently, audio narration is client-only: `useSliceAudio.ts` derives narration text, hashes it, and streams synthesis from a Web Worker running Transformers.js MMS-TTS, caching the resulting WAV in browser IndexedDB (`(chunkId, voice, contentHash)`). While multi-threading and fp32 weights have optimized CPU execution, CPU synthesis still takes 30–60 seconds for a full slice.

The backend ASP.NET Core application uses Pure Dependency Injection, EF Core with PostgreSQL 17 (pgvector), and standard `HttpClient` integrations (such as `TermExplanationService` and `GeminiAiService`). Google Cloud Text-to-Speech provides a permanent 1M character/month free tier for Neural2/WaveNet voices with sub-2-second response latency.

## Goals / Non-Goals

**Goals:**
- Provide sub-2-second narration synthesis via Google Cloud TTS (Neural2/WaveNet) through a secure backend proxy.
- Ensure global cross-user deduplication: a slice synthesized once by Google Cloud TTS is persisted in PostgreSQL, consuming zero additional Google quota on repeat plays across all users.
- Track monthly quota usage with dual protection: proactive UI toggle disable at 950,000 characters and strict backend RFC 7807 rejection if bypassed.
- Provide a clean, in-player toggle switch between Google Cloud (default) and On-Device (fallback) engines.
- Allow selecting among free-tier female/male voices when Google Cloud mode is active.
- Polish copy and UI: strip buzzwordy "AI" tags and remove inappropriate icons (e.g. lightning bolts / `Zap`).

**Non-Goals:**
- Bring-Your-Own-Key (BYOK) per-user settings: single server-side key in `.env` is specified.
- Sentence-by-sentence streaming from Google Cloud: Google Cloud TTS REST API synthesizes an entire slice in a single call (~1–2s); sentence chunking is unnecessary and multiplies network roundtrips.
- Changing the on-device Web Worker implementation: MMS-TTS remains intact as the offline/fallback engine.

## Decisions

### 1. Direct REST API via `HttpClient` over Google Cloud SDK
The backend will call `https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}` using standard `HttpClient` and JSON payloads.
- **Rationale**: Keeps the backend lean with zero extra NuGet packages, avoids heavy gRPC dependencies and reflection overhead, and complies with Pillar 1/2 of `AGENTS.md` (Pure DI).
- **Alternative considered**: `Google.Cloud.TextToSpeech.V1` NuGet package — rejected due to heavy binary footprint, gRPC assembly dependencies, and reflection-based auth wrappers.

### 2. Audio Storage via PostgreSQL `bytea` with Unique Index
Audio files synthesized by Google Cloud TTS will be stored as MP3 bytes in a new table `DocumentChunkAudios`:
- Columns: `Id`, `DocumentChunkId`, `ContentHash`, `VoiceId`, `MimeType` (`audio/mpeg`), `AudioData` (`byte[]`), `CharacterCount`, `DurationSeconds`, `CreatedAt`, `IsDeleted`.
- Unique Partial Index: `CREATE UNIQUE INDEX IX_DocumentChunkAudios_Lookup ON "DocumentChunkAudios" ("DocumentChunkId", "ContentHash", "VoiceId") WHERE "IsDeleted" = false;`
- **Rationale**: A slice's MP3 at standard voice bitrates (64kbps) is approximately 500KB–1MB. PostgreSQL automatically handles large bytea columns out-of-line in TOAST tables with LZ4/pglz compression. Normal table queries that omit `AudioData` incur zero I/O penalty. Lookup is $O(1)$ and takes <10ms.
- **Alternative considered**: Local filesystem / disk storage — rejected because Docker container recreation requires volume mapping and lacks transactional consistency with the database.
- **Alternative considered**: External S3 bucket — rejected as unnecessary external infrastructure when PostgreSQL easily accommodates thousands of slice audio files.

### 3. Monthly Quota Tracking via Database Aggregation
Monthly character consumption is calculated dynamically via SQL aggregation:
```sql
SELECT COALESCE(SUM("CharacterCount"), 0)
FROM "DocumentChunkAudios"
WHERE "CreatedAt" >= DATE_TRUNC('month', NOW() AT TIME ZONE 'UTC')
  AND NOT "IsDeleted";
```
- **Rationale**: Self-healing, zero external state (no Redis counter to drift), automatically rolls over on the 1st of every calendar month, and perfectly reflects actual Google API consumption.
- **Quota threshold**: Monthly limit = 1,000,000 characters. Proactive UI disable threshold = 950,000 characters (95%).

### 4. Two-Tier Caching Architecture
1. **Tier 1 (Browser IndexedDB)**: When client receives MP3 audio from the server, it caches it locally under `(chunkId, voiceId, contentHash)` using the existing `sliceAudioCache.ts`. Repeat listens and scrubber seeks in that browser require zero network requests.
2. **Tier 2 (Server PostgreSQL)**: When client requests audio for a slice not in its local IndexedDB, the backend checks `DocumentChunkAudios`. If present, it streams the MP3 from PostgreSQL (<20ms).
3. **Tier 3 (Google Cloud API)**: Only on a complete cache miss across both tiers does the server call Google Cloud TTS, save the MP3 into PostgreSQL, and return it.

### 5. In-Player Engine Toggle and Voice Selection UI
- **Toggle switch**: In `ReaderAudioPlayer.vue`, a clean segmented button or toggle (`Cloud` vs `Thiết bị` / `Device`) is placed adjacent to the play button and scrubber.
  - Active Cloud: subtle cloud icon or clean toggle switch.
  - Active Device: device icon (e.g. `Laptop` or existing CPU badge).
  - Buzzword removal: all occurrences of "AI", "Google AI", "AI formatted" in player labels are replaced with clean terms: "Google Cloud" / "Cloud" and "Thiết bị" / "Device".
  - Removal of `Zap` icon: replaces the lightning bolt icon with subtle, context-appropriate iconography.
- **Voice selection**: When Cloud mode is active, an inline voice selector (`AppSelect.vue`) allows choosing between:
  - Vietnamese: `vi-VN-Neural2-A` (Nữ / Female), `vi-VN-Neural2-D` (Nam / Male).
  - English: `en-US-Neural2-F` (Female), `en-US-Neural2-D` (Male).
- **Quota disable**: When monthly quota is $\ge$ 950k characters, the Cloud option is disabled with tooltip *"Đã đạt giới hạn tháng, tự động dùng giọng thiết bị"* / *"Monthly cloud quota reached, using on-device narration"*.
- **Client fallback on 429/403**: If a request fails with `AudioQuotaExhausted`, the client displays a toast notification and sets the engine preference to `device`.

## Risks / Trade-offs

- **[Risk] Google API Key missing or invalid** → Mitigation: If `Google__TtsApiKey` is empty or invalid, the backend returns an error or quota exhaustion code; the frontend falls back to On-Device Web Worker without breaking the user experience.
- **[Risk] User attempts to bypass client UI to call Google TTS** → Mitigation: Backend endpoint validates monthly character count before every synthesis call. If exceeded, returns RFC 7807 `AudioQuotaExhausted` (HTTP 429).
- **[Risk] Database storage growth for audio** → Mitigation: 500 cached slices $\approx$ 350MB of TOAST-compressed MP3s, which is modest for PostgreSQL. Stale content hashes can be purged via standard cleanup jobs.
- **[Risk] Offline playback when default is Cloud** → Mitigation: If network request to backend fails, the player automatically prompts or falls back to synthesizing via the local on-device worker.

## Migration Plan

1. **Database Migration**: Add EF Core migration for `DocumentChunkAudios` table with partial unique index.
2. **Configuration**: Add `Google__TtsApiKey` placeholder in `appsettings.json` and inject secret in `.env`.
3. **Backend Services**: Register `IGoogleCloudTtsService` in DI, wire up `HttpClient`, implement synthesis and quota checking.
4. **Frontend Updates**: Add Cloud TTS client integration in `useSliceAudio.ts`, update `ReaderAudioPlayer.vue` with engine toggle and voice selector, add i18n keys.
5. **Rollback**: Set engine toggle default to `device` or remove `Google__TtsApiKey`; the existing On-Device TTS remains fully operational.

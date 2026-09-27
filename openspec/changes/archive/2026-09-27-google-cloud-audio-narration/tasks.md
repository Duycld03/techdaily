# Tasks

## 1. Backend Entity, Migration, Google Cloud TTS Service & API Endpoints

- [x] 1.1 Create `DocumentChunkAudio` entity in `TechDaily.Domain/Entities/DocumentChunkAudio.cs`, configure the unique partial index `(DocumentChunkId, ContentHash, VoiceId) WHERE IsDeleted = false` in `EntityConfigurations.cs`, and create an EF Core migration. Verify by running `dotnet build backend/src/TechDaily.Infrastructure` and confirming the migration compiles without error.
- [x] 1.2 Implement `IGoogleCloudTtsService` in `backend/src/TechDaily.Infrastructure/Services/GoogleCloudTtsService.cs` using `HttpClient` to call `https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}` with JSON payload, converting base64 MP3 to bytes and calculating character count. Verify with unit tests in `backend/tests/TechDaily.Infrastructure.Tests` covering successful synthesis and API error handling.
- [x] 1.3 Implement `GetOrSynthesizeChunkAudioHandler` in `backend/src/TechDaily.Application/Features/Library/SynthesizeAudio/` with database cache lookup, monthly character quota aggregation, quota guard check (rejecting with RFC 7807 `AudioQuotaExhausted` when exceeding limit), and saving new audio to PostgreSQL. Verify with unit tests in `backend/tests/TechDaily.Application.Tests` covering cache hit, synthesis on cache miss, and quota-exhaustion rejection.
- [x] 1.4 Expose `POST /api/v1/library/chunks/{chunkId}/audio` and `GET /api/v1/library/audio/quota` in `backend/src/TechDaily.Api/Endpoints/LibraryEndpoints.cs`, wired with authorization and RFC 7807 problem details. Verify with `dotnet test backend/tests/TechDaily.Api.Tests` confirming endpoint routing and status codes.

## 2. Frontend Composable, Dual-Engine Orchestration & Quota Guard

- [x] 2.1 Extend `frontend/composables/useSliceAudio.ts` to support dual engines (`'cloud'` | `'device'`), routing Cloud mode synthesis to `POST /api/v1/library/chunks/{chunkId}/audio` while preserving On-Device Web Worker synthesis for Device mode, and caching both into browser IndexedDB under `(chunkId, voiceId, contentHash)`. Verify with a Vitest test in `frontend/tests/composables/useSliceAudio.spec.ts` asserting engine dispatch and IndexedDB storage for both modes.
- [x] 2.2 Wire monthly quota awareness in `useSliceAudio.ts` / `useLibraryStore.ts`: query `GET /api/v1/library/audio/quota` on reader load, flag near-quota state ($\ge$ 950,000 characters), and catch 429 `AudioQuotaExhausted` errors with a localized toast notification and automatic fallback to On-Device mode. Verify with a Vitest test simulating a 429 response and asserting fallback to `'device'`.

## 3. Audio Player UI Toggle, Voice Picker & Presentation Polish

- [x] 3.1 Add an in-player engine switch in `frontend/components/reader/ReaderAudioPlayer.vue` allowing the reader to toggle between Cloud and Device mode, defaulting to Cloud, persisting preference in `localStorage`, and disabling the Cloud toggle with an explanatory tooltip when the quota is near limit. Verify by toggling engines and confirming reactive state changes and disabled-state tooltip behavior.
- [x] 3.2 Add a compact Free-Tier Voice selector (`AppSelect.vue`) in `ReaderAudioPlayer.vue` when Cloud mode is active, providing curated Neural2 female and male options for Vietnamese (`vi-VN`) and English (`en-US`). Verify that changing the selected voice updates the voice parameter and re-synthesizes audio under the new voice key.
- [x] 3.3 Polish player UI copy and iconography: remove buzzwordy "AI" labels across player buttons, tooltips, and status text; replace the lightning bolt icon (`Zap`) with clean, subtle icons (`Cloud`, `Laptop` / `Cpu`); add all new localized strings to `frontend/i18n/locales/en.json` and `vi.json`. Verify that no "AI" or `Zap` icons are rendered in the audio player across English and Vietnamese locales.

## 4. Verification Gates

- [x] 4.1 Gate 1 — run `dotnet test` for the backend and `cd frontend && npx vitest run` for the frontend; verify 100% of backend and frontend test suites pass.
- [x] 4.2 Gate 2 - drive headless Chromium via `browser` in `eval` to capture reader screenshots at Desktop (1920x1080) and Mobile (390x844) viewports in both `en` and `vi` locales, visually verifying the engine toggle, free-tier voice selector, and absence of buzzwords or mismatched icons.

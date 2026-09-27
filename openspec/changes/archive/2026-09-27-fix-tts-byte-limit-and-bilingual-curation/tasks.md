# Tasks

- [x] 1.1 Implement sentence-bounded text partitioning utility in `GoogleCloudTtsService` capping sub-batches at 4,000 UTF-8 bytes.
- [x] 1.2 Implement multi-chunk synthesis with binary MP3 frame concatenation and cumulative character count tracking in `GoogleCloudTtsService`.
- [x] 1.3 Add unit tests in `GoogleCloudTtsServiceTests.cs` verifying single-chunk pass-through, multi-chunk partitioning, and concatenated MP3 output.

## 2. Infrastructure - Bilingual AI Slice Formatting

- [x] 2.1 Refactor system instructions in `GeminiAiService.FormatSliceAsync` to dynamically adapt for Vietnamese (`vi`) and English (`en`) documents, enforcing localized callouts (`> [!NOTE]`, `> [!TIP]`, `> [!WARNING]`).
- [x] 2.2 Localize JSON few-shot response schemas in `GeminiAiService.FormatSliceAsync` to prevent English language drift on Vietnamese documents.
- [x] 2.3 Add unit tests in `GeminiAiServiceTests.cs` verifying language enforcement for Vietnamese and English slice formatting.

## 3. Application & Endpoints Verification

- [x] 3.1 Verify `GetOrSynthesizeChunkAudioHandler` correctly records total character count and duration from concatenated audio segments.
- [x] 3.2 Update and execute test cases in `GetOrSynthesizeChunkAudioHandlerTests.cs` covering long narration scripts (>5,000 bytes).

## 4. Verification Gates

- [x] 4.1 Gate 1 — run `dotnet test` and `cd frontend && npx vitest run` to verify 100% pass across backend and frontend test suites.
- [x] 4.2 Gate 2 — drive headless Chromium via `browser` in `eval` to verify live playback of the oversized Vietnamese slice (`827-thoi-quen-nguyen-tu-thuviensach.vn` Slice 5) without 5000-byte errors, and visually confirm Vietnamese callout box presentation.

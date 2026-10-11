# Tasks

## 1. Configuration & Baseline

- [x] 1.1 Ensure `backend/src/DeepPace.Api/appsettings.json` defines `Gemini:Model` as an empty placeholder (`""`), forcing runtime resolution to depend strictly on `.env` (`Gemini__Model`).

## 2. Infrastructure Services

- [x] 2.1 Remove right-hand fallback default strings from `GeminiAiService.cs` constructor, and add fail-loud guard checks in `GenerateInsightAsync`, `GenerateQuestionsAsync`, `AnswerBookQueryAsync`, and `SynthesizeActiveRecallCardAsync` returning `Result.Failure` when `_model` is empty.
- [x] 2.2 Remove right-hand fallback default strings from `LookAheadBufferService.cs` constructor and guard against empty `_model`.
- [x] 2.3 Remove right-hand fallback default strings from `TermExplanationService.cs` constructor and guard against empty `_model`, returning `Result.Failure`.

## 3. Api Endpoints

- [x] 3.1 Update `GET /api/v1/system/ai-health` in `SystemEndpoints.cs` to return `HTTP 503 Service Unavailable` with `status: "unhealthy"` and `error: "Gemini model is not configured."` when `Gemini:Model` is missing or whitespace.

## 4. Testing & Verification

- [x] 4.1 Add unit test in `SystemEndpointsTests.cs` asserting `GET /api/v1/system/ai-health` returns HTTP 503 and `error: "Gemini model is not configured."` when `Gemini:Model` is missing.
- [x] 4.2 Add unit test in `GeminiAiServiceTests.cs` asserting `GenerateInsightAsync` returns failure with code `AiService.Unavailable` when `Gemini:Model` is missing.
- [x] 4.3 Run `dotnet test backend/tests/DeepPace.Tests/DeepPace.Tests.csproj` and verify 100% test pass rate.

## 5. Documentation & Invariants

- [x] 5.1 Update `AGENTS.md` (Pillar 4) documenting that missing `Gemini__Model` in `.env` must fail loud without silent in-code defaults.
- [x] 5.2 Validate OpenSpec change compliance via `openspec validate fail-loud-gemini-model-config`.

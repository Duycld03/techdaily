# Tasks

## 1. Configuration & Environment

- [x] 1.1 Update `.env.example` to specify `Gemini__Model=gemini-3.6-flash` as the runtime environment template.
- [x] 1.2 Update `appsettings.json` non-secret default `Gemini:Model` to `"gemini-3.6-flash"`.

## 2. Infrastructure Services

- [x] 2.1 Update `GeminiAiService.cs` C# fallback string from `"gemini-3.5-flash-lite"` to `"gemini-3.6-flash"`.
- [x] 2.2 Update `LookAheadBufferService.cs` C# fallback string from `"gemini-3.5-flash-lite"` to `"gemini-3.6-flash"`.
- [x] 2.3 Update `TermExplanationService.cs` C# fallback string from `"gemini-3.5-flash-lite"` to `"gemini-3.6-flash"`.

## 3. Api Endpoints

- [x] 3.1 Refactor `GET /api/v1/system/ai-health` in `SystemEndpoints.cs` to dynamically populate `AiHealthResponse.TextModel` with the resolved configuration value (`textModel`) instead of hardcoding `"gemini-3.5-flash-lite"`.

## 4. Testing & Verification

- [x] 4.1 Update `SystemEndpointsTests.cs` test configurations and assertions to expect `"gemini-3.6-flash"`.
- [x] 4.2 Update test configuration dictionaries in `GeminiAiServiceTests.cs` to `"gemini-3.6-flash"`.
- [x] 4.3 Update test configuration dictionaries in `TermExplanationServiceTests.cs` to `"gemini-3.6-flash"`.
- [x] 4.4 Update test configuration dictionaries in `TechInsightsTests.cs` and `InterviewQuizTests.cs` to `"gemini-3.6-flash"`.
- [x] 4.5 Run `dotnet test backend/tests/DeepPace.Tests/DeepPace.Tests.csproj` and verify all tests pass.

## 5. Architectural Standards & Documentation

- [x] 5.1 Update `AGENTS.md` (Pillar 4 and Section 1) to document `gemini-3.6-flash` as the standardized low-latency model.
- [x] 5.2 Validate OpenSpec change compliance via `openspec validate upgrade-to-gemini-3-6-flash`.

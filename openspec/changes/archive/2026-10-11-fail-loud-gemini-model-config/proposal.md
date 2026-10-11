# Proposal

## Why

In production and VPS deployments, silent in-code fallbacks (such as `configuration["Gemini:Model"] ?? "gemini-3.6-flash"`) and pre-filled defaults in `appsettings.json` swallow environment configuration omissions. If an engineer deploys to a server and forgets to declare `Gemini__Model` in `.env`, the system quietly masks the omission rather than alerting the operator. To guarantee that misconfigured deployments fail loud and immediately ("lỗi luôn, không bị nuốt lỗi"), all right-hand string fallbacks must be eliminated and missing model configurations must fail explicitly at runtime and in operational health probes.

## What Changes

- **Eliminate Silent In-Code String Fallbacks**:
  - Remove right-hand fallback strings (`?? "gemini-3.6-flash"`) from `GeminiAiService.cs`, `LookAheadBufferService.cs`, `TermExplanationService.cs`, and `SystemEndpoints.cs`.
  - Set `Gemini:Model` in `appsettings.json` to an empty placeholder (`""`), mirroring `Gemini:ApiKey`.
- **Fail-Loud Runtime Validation**:
  - In `GeminiAiService.cs`: Explicitly validate `string.IsNullOrWhiteSpace(_model)`. If unconfigured, immediately return `Result.Failure(Error.Custom("AiService.Unavailable", "Gemini AI model or API key is not configured."))` and log an error without making malformed HTTP requests.
  - In `TermExplanationService.cs`: Explicitly check `!string.IsNullOrWhiteSpace(_model)`. If absent, short-circuit and return `Result.Failure(Error.Custom("AiService.Unavailable", ...))`.
  - In `LookAheadBufferService.cs`: Guard against empty `_model` alongside `_apiKey`.
- **Health Check Operational Failure (`GET /api/v1/system/ai-health`)**:
  - Return `HTTP 503 Service Unavailable` with `status: "unhealthy"` and `error: "Gemini model is not configured."` whenever `Gemini:Model` is missing or whitespace in the runtime configuration.
- **Verification & Test Coverage**:
  - Add test scenarios verifying that when `Gemini:Model` is missing, `GET /api/v1/system/ai-health` returns 503 and AI generation returns `Result.Failure`.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `core-platform`: Mandate explicit fail-loud enforcement when `Gemini:Model` is unconfigured, prohibiting silent default string fallbacks in text generation services and operational health probes.

## Impact

- **Backend Configuration**: `backend/src/DeepPace.Api/appsettings.json`.
- **Backend Services**: `GeminiAiService.cs`, `LookAheadBufferService.cs`, `TermExplanationService.cs`.
- **API Endpoints**: `SystemEndpoints.cs` (`GET /api/v1/system/ai-health`).
- **Tests**: `DeepPace.Tests` (`SystemEndpointsTests.cs`, `GeminiAiServiceTests.cs`).

# Proposal

## Why

Google AI Studio and the Gemini API have officially deprecated `gemini-3.5-flash` and will permanently sunset standalone traffic, currently redirecting requests to `gemini-3.6-flash`. DeepPace currently references `gemini-3.5-flash-lite` across its AI generation services, health check probes, test assertions, and architecture specifications. Upgrading explicitly to `gemini-3.6-flash` eliminates reliance on automatic gateway redirects, secures continued Free Tier / low-cost quota operations, preserves DeepPace's strict `<5s` interactive latency invariant, and decouples the AI health check endpoint from hardcoded model strings.

## What Changes

- **AI Model Upgrade**: Standardize text generation across `GeminiAiService`, `LookAheadBufferService`, and `TermExplanationService` on `gemini-3.6-flash`.
- **Environment & Non-Secret Defaults**:
  - Update `.env.example` to document `Gemini__Model=gemini-3.6-flash` as the single environment source of truth.
  - Update C# in-code fallback defaults from `gemini-3.5-flash-lite` to `gemini-3.6-flash` so test runners and environments without explicit `.env` overrides never default to an obsolete model.
  - Keep `appsettings.json` aligned with non-secret defaults (`"Model": "gemini-3.6-flash"`) while `.env` remains the single runtime authority.
- **Dynamic AI Health Endpoint**: Refactor `GET /api/v1/system/ai-health` in `SystemEndpoints.cs` to return the dynamically resolved `textModel` from configuration instead of hardcoding `"gemini-3.5-flash-lite"`.
- **Test Suite Updates**: Update unit and integration tests (`SystemEndpointsTests.cs`, `GeminiAiServiceTests.cs`, `TermExplanationServiceTests.cs`, `TechInsightsTests.cs`) to validate against `gemini-3.6-flash`.
- **Documentation & Invariants**: Update `AGENTS.md` (Pillar 4 and Section 1) and OpenSpec specs to reference `gemini-3.6-flash`.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `core-platform`: Standardize text generation requirements and the `GET /api/v1/system/ai-health` operational probe on `gemini-3.6-flash`.
- `reader`: Update the interactive AI Ask Book citation/grounding scenario to reference `gemini-3.6-flash`.

## Impact

- **Backend APIs & Services**: `DeepPace.Infrastructure` (`GeminiAiService`, `LookAheadBufferService`, `TermExplanationService`), `DeepPace.Api` (`SystemEndpoints`).
- **Tests**: `DeepPace.Tests` (`SystemEndpointsTests`, `GeminiAiServiceTests`, `TermExplanationServiceTests`, `TechInsightsTests`).
- **Configuration**: `.env.example`, `appsettings.json`.
- **Zero Frontend Breaking Changes**: The frontend consumes RFC 7807 problem details and existing REST DTOs without coupling to model identifiers.

# Design

## Context

DeepPace utilizes Google Gemini for AI-driven technical insight generation, active recall flashcard synthesis, term explanations, and in-context book Q&A. Following Google's deprecation and auto-redirection of `gemini-3.5-flash` to `gemini-3.6-flash`, DeepPace needs an explicit model cutover across configuration, service defaults, operational probes, tests, and architectural standards.

## Goals / Non-Goals

**Goals:**
- Formally upgrade the default text synthesis engine to `gemini-3.6-flash`.
- Clarify and unify configuration precedence: preserve `.env` as the single runtime source of truth while ensuring in-code fallback defaults point to `gemini-3.6-flash`.
- Decouple `GET /api/v1/system/ai-health` so that `textModel` dynamically reports the runtime configuration rather than an unchangeable string constant.
- Align all unit and integration test fixtures with `gemini-3.6-flash`.
- Maintain strict compliance with architectural pillars in `AGENTS.md` (Pillar 4: `<5s` latency invariant and balanced bracket depth JSON parsing).

**Non-Goals:**
- Changing the embedding model (`gemini-embedding-001` with 768 dimensions remains unchanged).
- Modifying prompt schemas or JSON output structures, as `gemini-3.6-flash` is backward-compatible with DeepPace's `responseMimeType: "application/json"` payload format.
- Adding client-side model indicators in the Vue frontend UI (the frontend remains cleanly abstracted from backend LLM model identifiers).

## Decisions

### 1. Configuration Hierarchy: Single Runtime Authority (.env) with Clean Fallbacks

```
+-----------------------------------------------------------------------+
|                        CONFIGURATION HIERARCHY                        |
+-----------------------------------------------------------------------+
|                                                                       |
|  [Priority 1 - Runtime Authority]                                     |
|  .env: Gemini__Model=gemini-3.6-flash                                 |
|  (Injected by run-dev.sh / docker-compose env_file)                   |
|                               |                                       |
|                               v                                       |
|  [Priority 2 - Non-Secret Baseline]                                   |
|  appsettings.json: "Gemini:Model": "gemini-3.6-flash"                 |
|  (Committed baseline for environments without explicit .env)          |
|                               |                                       |
|                               v                                       |
|  [Priority 3 - Resilient In-Code Fallback]                            |
|  C# Services: ?? "gemini-3.6-flash"                                   |
|  (Guarantees unit tests and minimal DI containers never fall back     |
|   to deprecated gemini-3.5-flash-lite)                                |
|                                                                       |
+-----------------------------------------------------------------------+
```

*Rationale*:
Per `AGENTS.md` (Secrets Management), `.env` is the single environment source of truth. However, `appsettings.json` is committed to git to provide non-secret defaults. If `appsettings.json` or C# string fallbacks retain `"gemini-3.5-flash-lite"`, any headless test runner, container, or developer running without a local `.env` file would silently send traffic to the deprecated model endpoint. Updating all three tiers ensures complete consistency without compromising `.env` as the runtime authority.

### 2. Decouple Operational Health Check Endpoint (`SystemEndpoints.cs`)

*Current state*:
```csharp
// Current: hardcoded string ignores configuration
return Results.Ok(new AiHealthResponse(
    Status: "healthy",
    TextModel: "gemini-3.5-flash-lite", // <-- Hardcoded!
    ...
```

*Refactored state*:
```csharp
var textModel = configuration["Gemini:Model"] ?? "gemini-3.6-flash";
...
return Results.Ok(new AiHealthResponse(
    Status: "healthy",
    TextModel: textModel, // <-- Dynamically resolved
    ...
```

*Rationale*:
Health check endpoints must reflect actual operational runtime state. Hardcoded model names in DTO responses create false diagnostics when operators customize models via environment variables.

### 3. Preserving Depth Parsing and Latency Standards

- **Depth Parsing**: `GeminiAiService.cs` already utilizes bracket depth balance parsing (`FindBalancedJsonSubstring`). This remains active and requires no modifications.
- **Latency**: `gemini-3.6-flash` preserves low-latency response times within DeepPace's `<5s` interactive loop budget.

## Risks / Trade-offs

- **Rate Limits (Free Tier vs Paid)**: Free tier users retain 15 RPM / 1,500 RPD quotas. If usage spikes, requests return `429 Too Many Requests`, which DeepPace maps to `AiService.Unavailable` without database corruption.
- **Test Suite Fixture Updates**: InMemory configuration dictionaries across `DeepPace.Tests` must be systematically updated from `gemini-3.5-flash-lite` / `gemini-3.1-flash-lite` to `gemini-3.6-flash` to prevent regression test failures.

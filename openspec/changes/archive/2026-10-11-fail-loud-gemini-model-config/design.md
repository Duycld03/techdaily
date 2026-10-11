# Design

## Context

DeepPace enforces a strict "Fail-Fast / Fail-Loud" invariant across its AI and data ingestion subsystems: systems must never silently mask missing credentials or configuration via unannounced defaults or synthetic fallbacks. While `.env` is the single source of truth for runtime secrets and configuration, right-hand fallback expressions (`?? "gemini-3.6-flash"`) in constructors and pre-filled placeholders in `appsettings.json` quietly masked missing environment variables. If a deployment to VPS omitted `Gemini__Model`, the service would silently proceed. To ensure operational integrity, missing model configuration must immediately fail loud ("lỗi luôn, không bị nuốt lỗi").

## Goals / Non-Goals

**Goals:**
- Eliminate all right-hand fallback default strings (`?? "gemini-3.6-flash"`) from C# constructors and endpoints.
- Establish `appsettings.json` `"Model": ""` as an empty placeholder so that runtime relies strictly on `.env` (`Gemini__Model`).
- Implement short-circuit guard clauses in `GeminiAiService`, `LookAheadBufferService`, and `TermExplanationService` that immediately fail and log when `_model` is null or whitespace.
- Configure `GET /api/v1/system/ai-health` to return `503 Service Unavailable` with `status: "unhealthy"` and `error: "Gemini model is not configured."` when `Gemini:Model` is unconfigured.
- Add regression unit tests validating fail-loud behavior when `Gemini:Model` is missing.

**Non-Goals:**
- Changing embedding model contracts (`Gemini:EmbeddingModel` remains `gemini-embedding-001`).
- Altering the SM-2 algorithm or database entities.

## Decisions

### 1. Empty Placeholder in Committed `appsettings.json`

```json
"Gemini": {
  "ApiKey": "",
  "Model": "",
  "EmbeddingModel": "gemini-embedding-001",
  "UseOfflineMock": false
}
```

*Rationale*:
If `appsettings.json` contains `"gemini-3.6-flash"`, .NET's configuration system treats it as a non-empty value even when `.env` is missing `Gemini__Model`. Setting `"Model": ""` forces `.NET` configuration to evaluate `configuration["Gemini:Model"]` as empty unless overridden by `.env`.

### 2. Constructor & Method Guard Clauses

```csharp
// Constructor: no right-hand fallback string
_model = configuration["Gemini:Model"]
      ?? configuration["GEMINI_MODEL"]
      ?? Environment.GetEnvironmentVariable("GEMINI_MODEL")
      ?? string.Empty;

// Method execution: short-circuit before dispatching HTTP calls
if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_model))
{
    _logger.LogError("Gemini API key or model is not configured.");
    return Result<...>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI model or API key is not configured."));
}
```

*Rationale*:
Short-circuiting in the method prevents creating invalid request URLs such as `https://generativelanguage.googleapis.com/v1beta/models/:generateContent` and provides clean error reporting without swallowing errors.

### 3. Fail-Loud Health Check Endpoint Diagnostics

```csharp
var textModel = configuration["Gemini:Model"] ?? string.Empty;
if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(textModel))
{
    var errorMsg = string.IsNullOrWhiteSpace(apiKey)
        ? "Gemini API key is not configured."
        : "Gemini model is not configured.";

    return Results.Json(new AiHealthResponse(
        Status: "unhealthy",
        TextModel: "unhealthy",
        ...,
        Error: errorMsg), statusCode: StatusCodes.Status503ServiceUnavailable);
}
```

*Rationale*:
Operational probes (`/api/v1/system/ai-health`) called by monitoring tools or Docker health checks will immediately report `unhealthy` if an administrator forgets `Gemini__Model` during server setup.

## Risks / Trade-offs

- **Strictness for New Developers**: A developer starting the project who creates `.env` without copying `Gemini__Model=gemini-3.6-flash` from `.env.example` will see AI features fail.
  - *Mitigation*: The log clearly states `"Gemini model is not configured"`, and `.env.example` explicitly provides the template key and value.

# Spec Delta

## MODIFIED Requirements

### Requirement: AI Content Generation Handlers & Persistence
AI content generation services SHALL require explicit `Gemini:Model` and `Gemini:ApiKey` configuration and SHALL NOT provide silent in-code string fallbacks when either setting is missing or empty. When configuration is unconfigured or empty, the service SHALL return `Result.Failure` without dispatching HTTP calls or returning canned fallback entities.

Application database tables (`TermExplanationCaches`, `TechInsights`, `QuizQuestions`, `SpacedRepetitionCards`) SHALL NOT contain synthetic mock records, canned boilerplate explanations, or un-synthesized flashcard templates. Any such records identified by database hygiene maintenance runners SHALL be purged.

#### Scenario: Gemini model is unconfigured in environment
- **WHEN** an AI text generation service is invoked and `Gemini:Model` is missing or empty in configuration
- **THEN** the service returns `Result.Failure` with code `AiService.Unavailable`
- **AND** logs an error indicating that Gemini model is not configured
- **AND** does NOT dispatch any HTTP request with a malformed URL.

#### Scenario: AI insight generation fails
- **WHEN** `GenerateInsightHandler` invokes `ITechInsightGenerator.GenerateInsightAsync` and Gemini API fails
- **THEN** the generator returns `Result<TechInsight>.Failure`
- **AND** the handler returns `Result<TechInsightDto>.Failure` without adding any record to the `TechInsights` table.

#### Scenario: AI quiz question generation fails
- **WHEN** `GenerateQuizHandler` requests new questions from `IQuizGeneratorService` and Gemini API fails
- **THEN** the service returns `Result<List<QuizQuestion>>.Failure`
- **AND** the handler does NOT persist canned mock questions to the `QuizQuestions` table.

#### Scenario: Active recall flashcard synthesis fails
- **WHEN** `CreateCardFromHighlightHandler` requests flashcard synthesis from `IGeminiAiService.SynthesizeActiveRecallCardAsync` and Gemini API fails
- **THEN** the service returns `Result.Failure`
- **AND** the handler returns `Result<CreateCardFromHighlightResponse>.Failure` without inserting a fallback card into `SpacedRepetitionCards`.

#### Scenario: Detection of legacy boilerplate flashcards
- **WHEN** a database maintenance scan evaluates `SpacedRepetitionCards`
- **AND** a card has `SourceType = 'Highlight'` with front matching `"What is the core architectural principle behind: %"` or `"Nguyên lý kiến trúc cốt lõi đằng sau trích dẫn trong %"`
- **THEN** the card is flagged as tainted fallback data and deleted
- **AND** the associated `UserHighlights` record is preserved intact.

#### Scenario: Detection of legacy mock insights
- **WHEN** a database maintenance scan evaluates `TechInsights`
- **AND** an insight record has a slug ending in a randomized 6-character hex suffix (`-[0-9a-f]{6}`) and matches mock pool title signatures
- **THEN** the insight is flagged as tainted mock data and deleted
- **AND** any associated `UserInsightBookmarks` are removed via cascading foreign key deletion.

### Requirement: System AI Health Check Endpoint
The platform SHALL provide an operational diagnostics endpoint `GET /api/v1/system/ai-health` that concurrently probes Google Gemini text generation and Google Gemini embedding generation (`gemini-embedding-001`) via `Task.WhenAll`.

The health check SHALL dynamically resolve the configured text model name and verify:
1. Text model connectivity and latency.
2. Embedding model (`gemini-embedding-001`) connectivity, latency, and vector dimensionality (asserting length == 768).

The endpoint SHALL respond with `HTTP 200 OK` containing `{ textModel: "<configured-model>", embeddingModel: "gemini-embedding-001", dimension: 768 }` when both probes succeed, or `HTTP 503 Service Unavailable` with diagnostic error details when either probe fails or when either model/key configuration is unconfigured.

#### Scenario: AI health check probe succeeds
- **WHEN** a client or monitoring agent invokes `GET /api/v1/system/ai-health`
- **AND** Gemini text generation and Gemini embedding generation (`gemini-embedding-001`) both respond successfully with a 768-dimensional vector
- **THEN** the server returns `HTTP 200 OK`
- **AND** the JSON response contains `status = "healthy"`, `textModel = "gemini-3.6-flash"`, `embeddingModel = "gemini-embedding-001"`, and `dimension = 768`.

#### Scenario: AI health check detects unconfigured text model
- **WHEN** a client invokes `GET /api/v1/system/ai-health` and `Gemini:Model` is missing or whitespace in configuration
- **THEN** the server returns `HTTP 503 Service Unavailable`
- **AND** the JSON response contains `status = "unhealthy"` with `error` containing `"Gemini model is not configured."`.

#### Scenario: AI health check probe detects dimension mismatch or model failure
- **WHEN** a client invokes `GET /api/v1/system/ai-health` and either Gemini text generation fails or `gemini-embedding-001` returns a vector with dimension != 768 or returns an HTTP error
- **THEN** the server returns `HTTP 503 Service Unavailable`
- **AND** the JSON response contains `status = "unhealthy"` with diagnostic details identifying the failing model and error reason.

# Core Platform Specification

## Purpose
Provides the foundational user authentication, user profile management, daily curriculum doc reading slices, scenario challenges, SM-2 spaced repetition engine, document library, and background notification dispatches for the TechDaily platform.

## Requirements

### Requirement: Standard Email & Password Authentication
The system SHALL allow users to register an account with email, password (min 6 characters), full name, and preferred locale (`POST /api/v1/auth/register`), securely hash passwords using PBKDF2 with SHA-256 (16-byte random salt, 100,000 iterations), and authenticate users via email and password (`POST /api/v1/auth/login`), issuing a 256-bit JWT bearer token upon successful verification.

#### Scenario: User registers with valid email and password
- **WHEN** visitor sends `POST /api/v1/auth/register` with valid email, name, and password >= 6 characters
- **THEN** system provisions user entity with PBKDF2 password hash, creates user learning stats, and returns `201 Created` with JWT token.

#### Scenario: User authenticates with registered credentials
- **WHEN** user sends `POST /api/v1/auth/login` with registered email and correct password
- **THEN** system verifies hash and returns `200 OK` with JWT bearer token and user profile.

---

### Requirement: Google OAuth 2.0 Authentication
The system SHALL support signing in with Google via Google Identity Services (GIS) on the frontend (`POST /api/v1/auth/google`), verify Google ID token cryptographic signatures using `GoogleJsonWebSignature`, automatically provision new user accounts, and issue application JWT tokens.

#### Scenario: User logs in via Google Identity Services
- **WHEN** client sends `POST /api/v1/auth/google` with valid Google ID token credential
- **THEN** system verifies token signature with Google, provisions new user if not exists or links existing account, and returns application JWT.

---

### Requirement: User Profile Management, Route Guards & Security
The user profile endpoints (`GET /api/v1/user/profile`, `PUT /api/v1/user/profile`, `PUT /api/v1/user/change-password`) SHALL support managing user study schedules, streak preservation alert preferences, IANA timezones, and browser push status alongside existing profile properties, protected with strict JWT Bearer authentication, reject unauthenticated requests with `HTTP 401 Unauthorized`, and enforce route middleware guards on protected frontend pages.

The User Profile interface (`frontend/pages/profile.vue`) and update action SHALL be strictly dedicated to personal identity (`name`), career role targets (`targetRole`), daily study pace (`dailyGoalMinutes`), and account credentials/password security. 

The User Profile interface SHALL NOT display notification scheduling controls (`preferredStudyTime`, `streakAlertTime`) or timezone displays. The User Profile interface SHALL NOT include redirection links or navigational bridges to the system settings page, keeping the user experience clean and decluttered.

When submitting profile updates from the profile page, the client application SHALL dispatch only personal identity and pace fields (`name`, `targetRole`, `dailyGoalMinutes`) to `PUT /api/v1/user/profile`. The backend API SHALL support partial updates, preserving existing notification schedule and timezone database records when those fields are omitted.

#### Scenario: Unauthenticated request to user profile
- **WHEN** unauthenticated client calls `GET /api/v1/user/profile`
- **THEN** system returns `401 Unauthorized`.

#### Scenario: User updates profile settings
- **WHEN** authenticated user sends `PUT /api/v1/user/profile` with target level and learning goals
- **THEN** system updates user profile and returns updated profile DTO.

#### Scenario: User configures personal study schedule and timezone
- **WHEN** an authenticated user submits `PUT /api/v1/user/profile` with `preferredStudyTime: "07:30"`, `streakAlertTime: "21:00"`, `timeZone: "Asia/Ho_Chi_Minh"`, and `isPushEnabled: true`
- **THEN** the system validates the IANA timezone string, persists the preferences on the `User` entity, and returns the updated profile DTO.

#### Scenario: Validation of invalid timezone identifier
- **WHEN** a client submits an invalid or unrecognized timezone string (e.g., `"Invalid/Zone"`)
- **THEN** the system falls back safely to `"UTC"` or responds with `HTTP 400 Bad Request` with code `VALIDATION_FAILED`.

#### Scenario: User updates personal profile information from `/profile`
- **WHEN** an authenticated user modifies their name, target role, or daily goal minutes in `/profile` and submits the form
- **THEN** the client sends a `PUT /api/v1/user/profile` request containing only `{ name, targetRole, dailyGoalMinutes }`
- **AND** the payload does NOT contain `preferredStudyTime`, `streakAlertTime`, or `timeZone`
- **AND** the backend updates the user's name, target role, and daily goal minutes in PostgreSQL while preserving existing notification schedule and timezone values
- **AND** the user receives a localized success toast notification.

#### Scenario: User inspects `/profile` interface for decluttering
- **WHEN** a user navigates to the `/profile` page
- **THEN** the "Study Schedule & Timezone" card is completely absent from the DOM
- **AND** no time input controls or timezone badges are rendered on the page
- **AND** no navigation links or buttons pointing to `/settings` are rendered in the profile view.

#### Scenario: Server processes partial profile update from `/profile`
- **WHEN** the backend `PUT /api/v1/user/profile` endpoint receives a request with `Name`, `TargetRole`, and `DailyGoalMinutes` populated, but with `PreferredStudyTime`, `StreakAlertTime`, and `TimeZone` null/omitted
- **THEN** the server updates only the non-null properties, updates `UpdatedAt = DateTime.UtcNow`, and returns `200 OK` with the updated `UserProfileDto`.

---

### Requirement: Daily Doc Reading Slice
The system SHALL serve one curated 3–5 minute reading slice per active document series per day (`GET /api/v1/daily/today`) preserving source documentation excerpt language, structured summary, key takeaways, and quick-check questions.

#### Scenario: Authenticated user retrieves today's reading slice
- **WHEN** user calls `GET /api/v1/daily/today`
- **THEN** system returns today's document chunk with excerpt, summary, takeaways, and quiz status.

---

### Requirement: Senior Scenario Interview Challenge
The system SHALL present a daily scenario interview challenge aligned with the day's curriculum topic with instant grading, architectural feedback, and score evaluation.

#### Scenario: User completes daily interview scenario
- **WHEN** user submits answer to `POST /api/v1/daily/drill/submit`
- **THEN** system records drill submission, evaluates answer, and returns score with architectural explanation.

---

### Requirement: Spaced Repetition (SM-2) Engine
The system SHALL track user performance on technical concepts and schedule review dates using the SuperMemo SM-2 spaced repetition algorithm ($EF \in [1.30, 2.50]$).

#### Scenario: User reviews flashcard deck
- **WHEN** user calls `GET /api/v1/review/deck`
- **THEN** system returns cards where `NextReviewDate <= DateTime.UtcNow`.

#### Scenario: User grades card review
- **WHEN** user sends `POST /api/v1/review/cards/{id}/grade` with grade between 0 and 5
- **THEN** system recalculates interval and ease factor according to SM-2 formula and updates card schedule.

---

### Requirement: Document Library & AI Slicing
The system SHALL allow browsing library books and importing technical articles (`GET /api/v1/library/books`, `POST /api/v1/library/import`), parsing documents into sequential daily reading slices.

#### Scenario: User imports markdown document
- **WHEN** user sends `POST /api/v1/library/import` with markdown text
- **THEN** system slices document into sequential chunks and creates a new book record.

---

### Requirement: Highlight Notes System
The system SHALL allow users to create, view, and delete highlighted quotes with optional tags and book references (`GET /api/v1/notes/highlights`, `POST /api/v1/notes/highlights`, `DELETE /api/v1/notes/highlights/{id}`).

#### Scenario: User saves a highlight note
- **WHEN** user calls `POST /api/v1/notes/highlights` with selected text excerpt and chunk ID
- **THEN** system persists the highlight note and returns `201 Created`.

---

### Requirement: Streak Tracking & Freeze Protection
The system SHALL increment active streak upon daily completion and provide monthly Streak Freezes to prevent streak reset on missed days.

#### Scenario: User completes activity on consecutive day
- **WHEN** user completes daily drill or slice on next calendar day
- **THEN** system increments `CurrentStreak` by 1 and updates `LongestStreak` if current exceeds longest.

---

### Requirement: Telegram Push Notifications
The system SHALL provide notification dispatches for morning curriculum reminders (08:00 AM) and evening streak preservation alerts (20:00 PM).

#### Scenario: Morning dispatch worker runs
- **WHEN** background scheduler triggers 08:00 AM dispatch
- **THEN** worker sends telegram message with today's reading title and link to users with configured `TelegramChatId`.

---

### Requirement: Internationalization (i18n) & Dark Mode
The web frontend SHALL support seamless switching between English (`en-US`) and Vietnamese (`vi-VN`) via `@nuxtjs/i18n` and provide persistent dark/light theme switching without visual flashing.

#### Scenario: User toggles language to Vietnamese
- **WHEN** user selects Vietnamese language option
- **THEN** interface updates labels and messages to Vietnamese locale with full layout parity.

---

### Requirement: English-Only Development Standards
All backend C# code, frontend Vue/TypeScript code, variable names, database entities/columns, unit tests, and code comments SHALL be written in 100% English.

#### Scenario: Developer inspects codebase
- **WHEN** examining classes, database migrations, and unit tests
- **THEN** all code symbols, class names, variable identifiers, and comments are strictly in English.

### Requirement: Standardized Machine-Readable Error Response Envelope
All API endpoints returning error responses (HTTP 4xx and 5xx) SHALL include a structured JSON envelope containing:
- `code` (string): Unique uppercase snake_case machine-readable identifier (e.g. `RESOURCE_NOT_FOUND`, `AUTH_INVALID_CREDENTIALS`).
- `error` (string): English developer-facing descriptive error message.
- `details` (object or null): Optional structured metadata or validation failure details.

#### Scenario: Endpoint returns bad request error
- **WHEN** client sends an invalid request
- **THEN** server returns HTTP 400 with body `{ "code": "VALIDATION_FAILED", "error": "...", "details": null }`.

#### Scenario: Resource not found
- **WHEN** client queries a non-existent entity
- **THEN** server returns HTTP 404 with body `{ "code": "RESOURCE_NOT_FOUND", "error": "The requested resource was not found.", "details": null }`.

---

### Requirement: Client-Side Dynamic Error Code Localization
The web client (`useApiError` composable) SHALL resolve API error codes against the active i18n locale (`api_errors.<CODE>`). When an error code is present in the locale dictionary, the translated text SHALL be displayed. If no match is found, the system SHALL display the provided localized fallback message or generic localized error message.

#### Scenario: API returns AUTH_INVALID_CREDENTIALS with Vietnamese locale
- **WHEN** API responds with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }` and client locale is `vi`
- **THEN** client renders toast: "Email hoặc mật khẩu không chính xác."

#### Scenario: API returns AUTH_INVALID_CREDENTIALS with English locale
- **WHEN** API responds with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }` and client locale is `en`
- **THEN** client renders toast: "Invalid email or password."

---

### Requirement: 100% i18n Coverage for UI Notifications and Banners
All toast notifications, confirmation dialog texts, action button loading states, and contextual alert banners across the frontend application SHALL use `@nuxtjs/i18n` translation keys (`$t` or `t()`). No user-facing notification strings SHALL be hardcoded in component scripts or templates.

#### Scenario: User performs action triggering notification
- **WHEN** user copies text, saves a highlight, creates or deletes an item, or encounters an action error
- **THEN** the emitted toast or inline message reflects the active locale using defined i18n dictionary keys.

### Requirement: Notification Dispatch Multi-Channel Support
The system SHALL support multi-channel notifications, giving precedence to native browser Web Push while maintaining optional Telegram integration for users who explicitly configure a `TelegramChatId`. Notification dispatches SHALL strictly respect the user's localized timezone and preferred time slots rather than firing at hardcoded server hours.

#### Scenario: User receives reminder via Web Push
- **WHEN** the background scheduler triggers a study reminder for a user with active web push subscriptions
- **THEN** the system sends a VAPID-encrypted Web Push notification to all active devices registered by that user, delivering the message directly to the operating system notification center.

---

### Requirement: Web Push Subscription & VAPID Infrastructure
The system SHALL implement modern browser Web Push using Voluntary Application Server Identification (VAPID) across standard browser push endpoints (FCM, Apple Web Push, Mozilla autopush). The `POST /api/v1/notifications/push/subscribe` endpoint SHALL accept an optional `timeZone` string parameter alongside the endpoint, subscription keys, and user agent. When provided, the backend SHALL validate and persist this timezone to `User.TimeZone`, updating `User.UpdatedAt` and `User.IsPushEnabled = true` within the same transaction. The frontend Web Push composable (`useWebPush`) SHALL automatically detect the client browser's IANA timezone and include it in the subscription payload.

#### Scenario: Client retrieves VAPID public key
- **WHEN** an authenticated client calls `GET /api/v1/notifications/push/vapid-public-key`
- **THEN** the server returns the base64url-encoded VAPID public key configured in `WebPush:VapidPublicKey`.

#### Scenario: Client subscribes browser device to push notifications
- **WHEN** an authenticated user enables push and sends `POST /api/v1/notifications/push/subscribe` with `endpoint`, `keys.p256dh`, `keys.auth`, and optional `userAgent`
- **THEN** the system upserts the record into `UserPushSubscriptions`, sets `User.IsPushEnabled = true`, and returns `HTTP 200 OK`.

#### Scenario: Client unsubscribes browser device
- **WHEN** a client sends `POST /api/v1/notifications/push/unsubscribe` with `endpoint`
- **THEN** the system removes the subscription record, and sets `User.IsPushEnabled = false` if no remaining active subscriptions exist for that user.

#### Scenario: Client subscribes to push notifications with detected timezone
- **WHEN** an authenticated user calls `POST /api/v1/notifications/push/subscribe` with valid push keys and `timeZone: "Asia/Ho_Chi_Minh"`
- **THEN** the system upserts the `UserPushSubscription` record, updates `User.TimeZone = "Asia/Ho_Chi_Minh"`, sets `User.IsPushEnabled = true`, and returns `HTTP 200 OK`.

#### Scenario: Client subscribes without timezone or with empty timezone
- **WHEN** a client calls `POST /api/v1/notifications/push/subscribe` with null or whitespace `timeZone`
- **THEN** the system registers the subscription, sets `User.IsPushEnabled = true`, and preserves the existing `User.TimeZone` value without modification.

#### Scenario: Client subscribes with unrecognized timezone string
- **WHEN** a client calls `POST /api/v1/notifications/push/subscribe` with an unrecognized or malformed timezone identifier (e.g. `"Invalid/Timezone"`)
- **THEN** the system safely falls back to `"UTC"`, persisting `"UTC"` to `User.TimeZone` without failing the subscription request.

---

### Requirement: Timezone-Aware Background Push Dispatch Worker
The system SHALL run a background service (`DailyPushNotificationWorker`) executing every 15 minutes to evaluate study reminder and streak preservation alert windows against each user's local timezone.

#### Scenario: Morning study reminder dispatch within user's local window
- **WHEN** the worker evaluates a user whose local time in their configured `TimeZone` matches their `PreferredStudyTime` (within the current 15-minute slot), and the user has not completed today's reading or quiz
- **THEN** the worker dispatches a push notification with title `"TechDaily Study Time 📚"` and deep link to `/today`, recording the dispatch to prevent duplicate messages on subsequent ticks.

#### Scenario: Evening streak preservation alert
- **WHEN** the worker evaluates a user whose local time matches their `StreakAlertTime`, and the user has an active streak (`CurrentStreak > 0`) but has not yet studied today
- **THEN** the worker dispatches an urgent push alert `"Keep your {streak}-day streak alive! 🔥"` warning that the streak will expire at local midnight.

#### Scenario: Skip notification when daily activity is already completed
- **WHEN** the evaluation window matches but the user has already completed today's reading slice and challenge
- **THEN** the worker suppresses the notification, preventing unnecessary notification spam.

---

### Requirement: Stale Push Subscription Auto-Pruning
The Web Push dispatch pipeline SHALL automatically detect and delete revoked or expired push subscription endpoints.

#### Scenario: Push service returns HTTP 404 or 410 Gone
- **WHEN** sending a push notification to an endpoint and the push service responds with HTTP 404 (Not Found) or HTTP 410 (Gone) indicating the user unsubscribed or reset browser state
- **THEN** the worker catches the response and immediately deletes that `UserPushSubscription` row from PostgreSQL, preserving database hygiene.

### Requirement: Brave Browser Push Service Restriction Handling & Actionable Guidance
The client-side Web Push composable (`useWebPush`) and Settings view (`settings.vue`) SHALL detect when push subscription fails due to browser-level push service restrictions (such as Brave browser disabling Google services for push messaging by default), classify the restriction, and present clear, localized, actionable instructions directing the user to `brave://settings/privacy`.

#### Scenario: Push subscription on Brave browser with Google push services disabled
- **WHEN** a user on Brave browser attempts to enable Web Push notifications while "Use Google services for push messaging" is disabled in the browser settings
- **THEN** `useWebPush` catches the resulting `DOMException` (`"Registration failed - push service error"`), identifies the environment or error pattern, and classifies it as a Brave push service restriction
- **AND** the settings view displays an actionable localized alert/toast explaining that Brave requires enabling Google push services at `brave://settings/privacy` to receive notifications, suppressing confusing raw browser error strings.

#### Scenario: Localization parity for Brave push service instructions
- **WHEN** the Brave push service blocker is encountered under English (`en`) or Vietnamese (`vi`) locale
- **THEN** the actionable guidance is presented in the user's active locale using defined i18n translation keys without untranslated raw strings.

### Requirement: Cloud Embedding Service Contract & Dimensions
The system SHALL provide an application-layer interface `IEmbeddingService` for vectorizing text with support for single-text (`GenerateEmbeddingAsync`) and batch-text (`GenerateBatchEmbeddingsAsync`) operations returning 768-dimensional `Pgvector.Vector` structures.

The embedding service SHALL utilize Google Gemini model `gemini-embedding-001` and SHALL include `"outputDimensionality": 768` in all single and batch Google Generative Language API requests to guarantee exact alignment with the PostgreSQL `vector(768)` database schema.

The embedding service SHALL NOT silently fall back to mock or pseudo-random vector generation when Google API calls fail, when the API key is unconfigured, or when network timeouts occur, unless an explicit configuration flag `Gemini:UseOfflineMock` is set to `true`. When `Gemini:UseOfflineMock` is `false` (the default), the service SHALL return `Result<Vector>.Failure` or `Result<List<Vector>>.Failure` containing structured error details.

#### Scenario: Generate embedding using gemini-embedding-001 with 768 dimensions
- **WHEN** client invokes `IEmbeddingService.GenerateEmbeddingAsync(text)` with valid non-empty text and a valid Gemini API key
- **THEN** service invokes Google Generative Language API endpoint for `gemini-embedding-001:embedContent` specifying `"outputDimensionality": 768`
- **AND** returns a `Result<Vector>` with `IsSuccess = true` containing exactly 768 float elements matching PostgreSQL `vector(768)`.

#### Scenario: Gemini Embedding API returns error status code
- **WHEN** client invokes `IEmbeddingService.GenerateEmbeddingAsync(text)` and Google API responds with HTTP 404, 429, or 500
- **AND** configuration setting `Gemini:UseOfflineMock` is `false`
- **THEN** service logs an error with `LogLevel.Error`
- **AND** returns `Result<Vector>.Failure(Error.Custom("Embedding.ApiError", ...))`
- **AND** does NOT return a deterministic mock vector.

#### Scenario: Gemini API key is missing and offline mock is disabled
- **WHEN** `IEmbeddingService` executes while `Gemini:ApiKey` is empty or whitespace
- **AND** `Gemini:UseOfflineMock` is `false`
- **THEN** service returns `Result<Vector>.Failure(Error.Custom("Embedding.MissingApiKey", ...))`
- **AND** does NOT return a deterministic mock vector.

---

### Requirement: Curriculum Vector Backfill Pipeline
The system SHALL provide a batched backfill mechanism (`CurriculumSeeder.BackfillEmbeddingsAsync` and `DatabaseMaintenanceRunner.BackfillEmbeddingsAsync`) that iteratively scans and vectorizes all `DocumentChunks` where `Embedding IS NULL` in configurable batches (default 25 chunks per batch) until zero unvectorized chunks remain.

The backfill mechanism SHALL vectorize chunks using Google Gemini model `gemini-embedding-001` specifying `"outputDimensionality": 768`, assert that returned vectors have a length of exactly 768 floats, pace requests with an inter-batch delay to respect API rate limits, and persist changes transactionally.

#### Scenario: Backfill encounters embedding service failure
- **WHEN** `CurriculumSeeder.BackfillEmbeddingsAsync` executes during startup and `IEmbeddingService.GenerateBatchEmbeddingsAsync` returns a failure result
- **THEN** the seeder logs an error message detailing the embedding failure
- **AND** does NOT save changes to `DocumentChunks`
- **AND** leaves unvectorized chunks with `Embedding = null` in the database.

#### Scenario: Backfill processes all unvectorized chunks across multiple batches
- **WHEN** the maintenance backfill runner executes against a database with 465 unvectorized document chunks
- **THEN** the runner processes chunks in sequential batches of 25
- **AND** generates 768-dimensional vectors for each batch using `gemini-embedding-001`
- **AND** updates `DocumentChunk.Embedding` in PostgreSQL
- **AND** continues until 0 chunks remain with `Embedding IS NULL`.

#### Scenario: Backfill handles transient Google API rate limiting
- **WHEN** the embedding service encounters an HTTP 429 rate limit or transient network timeout during a batch backfill
- **THEN** the runner applies exponential backoff and retries the batch up to 3 times
- **AND** logs warning details without terminating the entire maintenance process prematurely.

---

### Requirement: Document Chunk Vectorization on Ingestion
When new documents are ingested via `PdfIngestionWorker`, the worker SHALL attempt to vectorize initial slices using `IEmbeddingService`. If chunking or embedding fails due to unhandled exceptions or API errors, the worker SHALL mark the book status as `ProcessingStatus.Failed`, record the error detail in `ErrorMessage`, and SHALL NOT mark incomplete books as `ProcessingStatus.Ready`.

#### Scenario: PDF ingestion worker encounters unhandled embedding failure
- **WHEN** `PdfIngestionWorker` processes an uploaded PDF and `IEmbeddingService.GenerateBatchEmbeddingsAsync` fails or throws an exception
- **THEN** worker marks `book.Status = ProcessingStatus.Failed`
- **AND** sets `book.ErrorMessage` to the specific failure reason
- **AND** saves changes to PostgreSQL without marking the book as `Ready`.
---

### Requirement: AI Content Generation Handlers & Persistence
AI content generation services SHALL standardize text generation on Google Gemini model `gemini-3.5-flash-lite` and return `Result.Failure` on API errors without returning canned fallback entities wrapped in successful results. 

Application database tables (`TermExplanationCaches`, `TechInsights`, `QuizQuestions`, `SpacedRepetitionCards`) SHALL NOT contain synthetic mock records, canned boilerplate explanations, or un-synthesized flashcard templates. Any such records identified by database hygiene maintenance runners SHALL be purged.

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

---

### Requirement: AI Term Explanation & Cache Protocol
`ITermExplanationService.ExplainTermAsync` SHALL check exact database cache and semantic HNSW vector cache before invoking Gemini text generation (`gemini-3.5-flash-lite`). If the term is not cached and the Gemini API call fails, the service SHALL return `Result<TermExplanationResult>.Failure` with code `AiService.Unavailable`. The service SHALL NOT return generic boilerplate sentences wrapped in `HTTP 200 OK`.

The endpoint `POST /api/v1/daily/explain-term` SHALL return an error HTTP status (such as `400 Bad Request` or `503 Service Unavailable`) when term explanation fails, enabling the frontend reader modal to display a clear error state and retry prompt.

#### Scenario: Term explanation fails during cache miss
- **WHEN** client sends `POST /api/v1/daily/explain-term` for an uncached term and Gemini text generation fails
- **THEN** the service returns `Result.Failure(Error.Custom("AiService.Unavailable", ...))`
- **AND** the API responds with a non-200 HTTP status code and error payload
- **AND** does NOT cache or return generic boilerplate text.

---

### Requirement: Frontend Client Error Resolution & Problem Details
The client error resolution composable `frontend/composables/useApiError.ts` SHALL prioritize RFC 7807 problem details (`detail`) and backend custom error messages (`error`) for client-actionable 4xx errors, while mapping HTTP 500 Internal Server Error responses to localized platform error translations (`api_errors.SERVER_ERROR` or caller-provided `fallbackKey`) to prevent leaking unlocalized English server fault strings.

#### Scenario: Backend returns RFC 7807 ProblemDetails with detail
- **WHEN** an API request fails and the backend returns `{ "detail": "Google Gemini rate limit exceeded", "status": 429 }`
- **AND** the caller invokes `formatError(err, "quiz.generate_error")`
- **THEN** `formatError` returns `"Google Gemini rate limit exceeded"` instead of the generic translation for `"quiz.generate_error"`.

#### Scenario: Backend returns custom error payload
- **WHEN** an API request fails and the backend returns `{ "code": "Embedding.ApiError", "error": "Gemini Embedding API returned status 503" }`
- **AND** the caller invokes `formatError(err, "common.error")`
- **THEN** `formatError` returns `"Gemini Embedding API returned status 503"`.

#### Scenario: Backend returns HTTP 500 ProblemDetails
- **WHEN** an API request encounters an unhandled server error and returns HTTP 500 with `{ "title": "Server Error", "detail": "An unexpected error occurred.", "status": 500 }`
- **AND** the active locale is Vietnamese (`vi`)
- **THEN** `formatError` resolves to the localized server error message `"Đã xảy ra lỗi máy chủ. Vui lòng thử lại sau."` (`api_errors.SERVER_ERROR`) instead of the raw English string.

#### Scenario: Backend returns HTTP 500 with caller fallback key
- **WHEN** an API request fails with HTTP 500 and the caller provides a specific fallback key (e.g., `"today.explain_error"`)
- **THEN** `formatError` resolves to the localized translation of the fallback key or `api_errors.SERVER_ERROR` rather than unlocalized server details.

---

### Requirement: System AI Health Check Endpoint
The platform SHALL provide an operational diagnostics endpoint `GET /api/v1/system/ai-health` that concurrently probes Google Gemini text generation (`gemini-3.5-flash-lite`) and Google Gemini embedding generation (`gemini-embedding-001`) via `Task.WhenAll`.

The health check SHALL verify:
1. Text model (`gemini-3.5-flash-lite`) connectivity and latency.
2. Embedding model (`gemini-embedding-001`) connectivity, latency, and vector dimensionality (asserting length == 768).

The endpoint SHALL respond with `HTTP 200 OK` containing `{ textModel: "healthy", embeddingModel: "healthy", dimension: 768 }` when both probes succeed, or `HTTP 503 Service Unavailable` with diagnostic error details when either probe fails.

#### Scenario: AI health check probe succeeds
- **WHEN** a client or monitoring agent invokes `GET /api/v1/system/ai-health`
- **AND** Gemini text generation (`gemini-3.5-flash-lite`) and Gemini embedding generation (`gemini-embedding-001`) both respond successfully with a 768-dimensional vector
- **THEN** the server returns `HTTP 200 OK`
- **AND** the JSON response contains `status = "healthy"`, `textModel = "healthy"`, `embeddingModel = "healthy"`, and `dimension = 768`.

#### Scenario: AI health check probe detects dimension mismatch or model failure
- **WHEN** a client invokes `GET /api/v1/system/ai-health` and either `gemini-3.5-flash-lite` fails or `gemini-embedding-001` returns a vector with dimension != 768 or returns an HTTP error
- **THEN** the server returns `HTTP 503 Service Unavailable`
- **AND** the JSON response contains `status = "unhealthy"` with diagnostic details identifying the failing model and error reason.

### Requirement: Controlled Offline Mock Vector Configuration
The platform SHALL support an explicit configuration flag `Gemini:UseOfflineMock` in `appsettings*.json`. The system SHALL strictly disallow mock vector generation in production and default environments, permitting mock generation only when `Gemini:UseOfflineMock` is explicitly configured as `true`.

#### Scenario: Offline developer enables mock vectors
- **WHEN** `Gemini:UseOfflineMock` is configured to `true` in `appsettings.Development.json`
- **AND** `IEmbeddingService` is invoked without a valid Gemini API key or network connection
- **THEN** the service logs a warning that offline mock mode is active
- **AND** returns a deterministic 768-dimensional unit vector wrapped in `Result.Success`.

#### Scenario: Production environment with missing key and offline mock disabled
- **WHEN** `Gemini:UseOfflineMock` is `false` (default)
- **AND** `IEmbeddingService` is invoked without a valid Gemini API key
- **THEN** the service returns `Result.Failure(Error.Custom("Embedding.MissingApiKey", ...))`
- **AND** refuses to generate or persist mock vectors.

### Requirement: Database Hygiene & Tainted Data Elimination
The platform SHALL provide an operational data hygiene capability to identify, analyze, and purge synthetic, boilerplate, or corrupted fallback data across `TermExplanationCaches`, `TechInsights`, `QuizQuestions`, and `SpacedRepetitionCards`.

The data hygiene capability SHALL enforce the following invariants:
1. **Term Explanation Hygiene:** No cache entry shall contain boilerplate phrases (`"represents a core runtime or architectural mechanism"` or `"Khái niệm kỹ thuật quan trọng mô tả cơ chế hoạt động nội tại"`).
2. **Tech Insights Hygiene:** No insight shall contain canned mock titles or randomized slug suffixes not defined in `tech-insights.json`.
3. **Quiz Questions Hygiene:** No quiz question shall contain deterministic mock question templates (`"When addressing \"%\", which architectural strategy is optimal?"`) or mock deep dive explanation templates.
4. **Flashcard Hygiene:** No spaced repetition card shall contain boilerplate prompt templates generated during Gemini API outages.

#### Scenario: Dry-run analysis previews affected records without data mutation
- **WHEN** an operator runs the data hygiene tooling with `--dry-run`
- **THEN** the tooling counts all records matching tainted fallback signatures across `TermExplanationCaches`, `TechInsights`, `QuizQuestions`, `SpacedRepetitionCards`, and `DocumentChunks`
- **AND** emits a structured diagnostic count report
- **AND** rolls back the database transaction, guaranteeing zero data mutations.

#### Scenario: Transactional purge removes tainted data atomically
- **WHEN** an operator runs the data hygiene tooling with `--execute`
- **THEN** the tooling executes atomic `DELETE` statements inside a `BEGIN ... COMMIT` transaction
- **AND** removes all tainted records matching the hygiene criteria
- **AND** preserves all genuine user highlights, user profiles, and curated seed data.

---

### Requirement: Document Chunk Vector Completeness Invariant
Every document chunk in `DocumentChunks` associated with active curriculum books and imported technical publications SHALL possess a valid, non-null 768-dimensional float vector (`Embedding IS NOT NULL`) before being included in semantic vector similarity search or RAG retrieval pipelines.

#### Scenario: Verification of vector completeness post-maintenance
- **WHEN** post-maintenance integrity verification is executed
- **THEN** a query for `SELECT COUNT(*) FROM "DocumentChunks" WHERE "Embedding" IS NULL` returns exactly `0`
- **AND** an approximate nearest neighbor cosine similarity query using the `hnsw` index executes successfully without error.

---

### Requirement: Database Maintenance CLI & Tooling Contract
The platform application `TechDaily.Api` SHALL provide command-line arguments for headless operational maintenance:
- `--cleanup-data`: Activates maintenance mode.
- `--dry-run`: Analyzes and reports tainted records within an aborted transaction.
- `--execute`: Executes atomic data purge within a committed transaction.
- `--backfill-embeddings`: Iteratively backfills unvectorized document chunks using `IEmbeddingService`.
- `--batch-size=<N>`: Controls the chunk batch size for embedding requests (bounds: 5 to 50, default: 25).
- `--reseed-catalog`: Restores canonical catalog entries from `tech-insights.json` and `curriculum-30-days.json`.

#### Scenario: Headless execution in container environment
- **WHEN** the maintenance CLI is executed inside a container via `dotnet TechDaily.Api.dll --cleanup-data --execute --backfill-embeddings --reseed-catalog`
- **THEN** the application executes the purge, re-seeds curated items, completes the vector backfill, and exits with code `0`.

#### Scenario: Maintenance CLI dry-run reports non-zero tainted records
- **WHEN** the maintenance CLI is executed with `--cleanup-data --dry-run` against a database containing legacy fallback records
- **THEN** the CLI outputs the exact count of tainted records per table and exits with code `0` without altering database state.

### Requirement: Resilient Secondary Operation Fault Isolation
Application services performing secondary or auxiliary caching operations (such as embedding generation and cache persistence in `TermExplanationService`) SHALL isolate secondary database interactions within non-blocking exception handlers. Secondary caching failures SHALL NOT fail primary user-facing requests or discard valid LLM generation outputs.

#### Scenario: Auxiliary caching failure does not fail primary user operation
- **WHEN** a primary business operation succeeds (e.g. Gemini generates a term explanation) but secondary caching to PostgreSQL encounters a database exception
- **THEN** the application service catches and logs the exception without propagating an unhandled HTTP 500 error to the client
- **AND** returns the generated output to the user.

#### Scenario: Diagnostic logging on auxiliary caching failure
- **WHEN** a secondary caching failure occurs
- **THEN** the service logs a structured warning containing the entity identifier, error message, and context for operational observability.

### Requirement: System Settings, Notification Scheduling & Timezone Configuration
The Settings interface (`frontend/pages/settings.vue`) SHALL serve as the exclusive single source of truth for notification schedule configuration (`preferredStudyTime`, `streakAlertTime`) and timezone preferences (`timeZone`). 

All user modifications to notification reminder timing and timezone detection SHALL occur within the Settings domain and be persisted via `PUT /api/v1/user/profile` or the Web Push subscription flow (`POST /api/v1/notifications/push/subscribe`).

#### Scenario: User configures study schedule and timezone in `/settings`
- **WHEN** an authenticated user adjusts their preferred study time, streak alert time, or timezone in `/settings` and clicks "Save Schedule"
- **THEN** the client dispatches `PUT /api/v1/user/profile` containing `{ preferredStudyTime, streakAlertTime, timeZone }`
- **AND** the server updates these preferences in PostgreSQL and returns `200 OK`.

#### Scenario: User updates Web Push subscription with timezone synchronization
- **WHEN** a user enables Web Push notifications in `/settings`
- **THEN** the client automatically includes the detected or selected IANA timezone identifier in the subscription request
- **AND** the backend updates `User.TimeZone` and `User.IsPushEnabled = true` simultaneously.

---

### Requirement: Zero-Repo-Footprint Live Production E2E Verification
The platform verification harness SHALL support running live end-to-end headless browser test suites against the production deployment (`https://techdaily.duckdns.org`) using an ephemeral runner outside the git repository (`/tmp/techdaily-live-e2e.mjs`) leveraging host pre-cached Chromium binaries without committing test scripts, configuration files, or temporary artifacts to the source repository.

The test suite SHALL validate critical production user journeys including:
1. **S1:** Authentication & Navigation Shell
2. **S2:** Reader & AI Term Explainer Modal (bilingual layout, geometry bounds, zero overflow)
3. **S3:** Notes & SM-2 Flashcard Creation (zero `[vue-i18n]` injection crash, success toast, review queue check)
4. **S4:** Settings & Web Push (schedule controls, timezone dropdown, Brave push service guidance)
5. **S5:** Decluttered Profile Form Save (absence of schedule inputs, absence of settings link, pure identity payload)

#### Scenario: Live execution of end-to-end critical journey test suite
- **WHEN** the ephemeral test runner is invoked from `/tmp/techdaily-live-e2e.mjs` against `https://techdaily.duckdns.org`
- **THEN** it executes scenarios S1 through S5 in headless Chromium resolved from host caches
- **AND** captures timestamped logs and failure screenshots in `/tmp/` upon any assertion failure
- **AND** generates an execution summary report in `/tmp/techdaily-live-e2e-report.json`.

#### Scenario: Repository cleanliness after live verification execution
- **WHEN** the live E2E test execution finishes (either passing or failing)
- **THEN** `git status` in the TechDaily repository demonstrates zero untracked test scripts, zero modified application files, and zero temporary test artifacts.

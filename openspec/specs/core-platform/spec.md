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

The User Profile interface (`frontend/pages/profile.vue`) SHALL present an Asymmetric 2-Column Engineer Portfolio Dashboard (Desktop 2/3 - 1/3, stacking on mobile):
1. Left Column (Settings & Goals): Personal info tab (Name, Target Role selector, interactive Daily Goal Pace chips 5m/10m/15m/30m) and Security tab (Current password, New password with dynamic strength bar, Confirm password).
2. Right Column (Identity & Milestones Widget): Large avatar with status badge, display name, target role badge, account type (Google linked / Standard email). Stacked milestone card: Active Streak with fire icon, longest streak, freeze credits remaining; Total Drills completed; Quiz Accuracy rate.
3. Domain Mastery Progress Bar (Goal Tracker): Progress bars tracking curriculum domain coverage across four universal, framework-agnostic core engineering pillars:
   - **Pillar 1: Backend Runtime & Concurrency** (`profile.domain_backend_runtime`: "Nền Tảng Backend & Runtime" / "Backend Runtime & Concurrency") - tracking runtime mechanisms, memory allocation, async I/O, threads, and concurrency across .NET/CLR, Node.js/NestJS/Express, Go/Goroutines, Java/Spring/JVM, and Python.
   - **Pillar 2: Data Storage & Persistence** (`profile.domain_data_storage`: "Hệ Lưu Trữ & Cơ Sở Dữ Liệu" / "Data Storage & Persistence") - tracking storage engines, query execution, and persistence across PostgreSQL, MongoDB, Redis, MySQL, SQLite, Cassandra, ACID transactions, B-Trees, LSM-Trees, replication, WAL, and indexing.
   - **Pillar 3: Distributed Systems & Architecture** (`profile.domain_system_design`: "Hệ Thống Phân Tán & Thiết Kế" / "Distributed Systems & Architecture") - tracking distributed architecture patterns, microservices, message queues (Kafka, RabbitMQ), CAP theorem, transactional outbox, consensus, idempotency, rate limiting, and observability.
   - **Pillar 4: Frontend & Browser Engineering** (`profile.domain_frontend`: "Hiệu Năng Frontend & Trình Duyệt" / "Frontend & Browser Engineering") - tracking browser execution, rendering pipelines, critical rendering path, DOM, Vue, React, TypeScript, JavaScript, Web Vitals, and SSR/hydration.

The Domain Mastery Goal Tracker component (`frontend/components/profile/DomainGoalTracker.vue`) SHALL evaluate and aggregate topic mastery dynamically across multi-stack keywords via `matchCategory(keyOrTopic: string)`, ensuring book chapters, drills, and quiz attempts in any modern stack map seamlessly into the appropriate universal pillar.

The Engineer Portfolio Dashboard SHALL adapt responsively across Desktop (≥ 1280px, asymmetric 2-column 2/3 - 1/3 layout) and Mobile (~375px - 390px, single-column vertically stacked layout) viewports without horizontal scrolling or layout overlap.

The Identity widget, milestone statistics, and domain goal tracker SHALL support bilingual rendering in both English and Vietnamese, ensuring that universal engineering pillar titles render cleanly without text truncation, badge clipping, or broken progress bar labels.

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

#### Scenario: Desktop 2-column vs mobile single-column stacked responsive layout
- **WHEN** authenticated user accesses `/profile` on a desktop viewport ($\ge 1280\text{px}$)
- **THEN** the layout renders an asymmetric 2-column structure with settings and domain tracker on the left (2/3 width) and identity/milestone cards on the right (1/3 width)
- **WHEN** user accesses `/profile` on a mobile viewport ($375\text{px} - 390\text{px}$)
- **THEN** the layout stacks into a single vertical column with the Identity widget at the top followed by the settings tabs and domain goal tracker, maintaining touch-friendly targets and zero horizontal overflow.

#### Scenario: User inspects identity and learning milestones widget
- **WHEN** user views the right-column identity card
- **THEN** UI renders the user avatar, account connection badge (Google / Email), active streak with fire icon and freeze credits, total drills completed, and quiz accuracy percentage.

#### Scenario: User monitors curriculum domain mastery goal progress
- **WHEN** user views the Domain Mastery Goal Tracker section on `/profile`
- **THEN** UI displays categorized visual progress bars for each of the 4 universal engineering pillars:
  - Backend Runtime & Concurrency (aggregating .NET, Node.js, Go, Java, Python runtime topics)
  - Data Storage & Persistence (aggregating PostgreSQL, MongoDB, Redis, MySQL, ACID, indexing topics)
  - Distributed Systems & Architecture (aggregating microservices, Kafka, outbox, system design topics)
  - Frontend & Browser Engineering (aggregating browser performance, Vue, React, TypeScript topics)
- **AND** topic stats from multi-stack curricula accurately increment completed and total counts in the corresponding universal pillar.

#### Scenario: Bilingual visual verification for identity widget, milestone stats, and domain goal tracker
- **WHEN** user toggles between English (`en`) and Vietnamese (`vi`) on the `/profile` page
- **THEN** all copy across the Identity card, milestone badges (active streak, drills completed, quiz accuracy), and domain goal tracker updates dynamically
- **AND** universal pillar titles render with exact localized strings:
  - In English: "Backend Runtime & Concurrency", "Data Storage & Persistence", "Distributed Systems & Architecture", "Frontend & Browser Engineering"
  - In Vietnamese: "Nền Tảng Backend & Runtime", "Hệ Lưu Trữ & Cơ Sở Dữ Liệu", "Hệ Thống Phân Tán & Thiết Kế", "Hiệu Năng Frontend & Trình Duyệt"
- **AND** longer Vietnamese domain titles render without badge clipping, text truncation, or misalignment of progress bar percentages.

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
The web client (`useApiError` composable) SHALL resolve API error codes against the active i18n locale (`api_errors.<CODE>`). When an error code is present in the locale dictionary, the translated text SHALL take precedence over raw developer-facing English error messages (`responseData.error`). If no match is found, the system SHALL display the provided localized fallback message or generic localized error message.

#### Scenario: API returns AUTH_INVALID_CREDENTIALS with Vietnamese locale
- **WHEN** API responds with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }` and client locale is `vi`
- **THEN** client renders toast: "Email hoặc mật khẩu không chính xác."

#### Scenario: API returns AUTH_INVALID_CREDENTIALS with English locale
- **WHEN** API responds with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }` and client locale is `en`
- **THEN** client renders toast: "Invalid email or password."

#### Scenario: API returns error code with matching translation in locale dictionary
- **WHEN** API responds with `{ "code": "PUSH_SUBSCRIPTION_EXPIRED", "error": "Push subscription has expired." }`
- **AND** the active locale is Vietnamese (`vi`)
- **THEN** `formatError` returns the localized message `"Đăng ký thông báo đẩy đã hết hạn. Vui lòng tắt và bật lại thông báo để làm mới."` (`api_errors.PUSH_SUBSCRIPTION_EXPIRED`) instead of the English `error` string.

#### Scenario: API returns error code without translation in locale dictionary
- **WHEN** API responds with `{ "code": "UNKNOWN_ERROR_CODE", "error": "Something went wrong." }`
- **AND** no matching entry exists in `api_errors`
- **THEN** `formatError` falls back to `responseData.error` or the caller-provided `fallbackKey`.

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
The Web Push dispatch pipeline SHALL automatically detect and delete revoked or expired push subscription endpoints during both background scheduled dispatches (`DailyPushNotificationWorker`) and user-initiated test dispatches (`POST /api/v1/notifications/push/test`). When all registered push subscriptions for a user are purged as stale, the system SHALL update `User.IsPushEnabled = false` and refresh `User.UpdatedAt`.

#### Scenario: Push service returns HTTP 404 or 410 Gone
- **WHEN** sending a push notification to an endpoint and the push service responds with HTTP 404 (Not Found) or HTTP 410 (Gone) indicating the user unsubscribed or reset browser state
- **THEN** the worker catches the response and immediately deletes that `UserPushSubscription` row from PostgreSQL, preserving database hygiene.

#### Scenario: Push service returns HTTP 404 or 410 Gone during test push dispatch
- **WHEN** an authenticated user invokes `POST /api/v1/notifications/push/test`
- **AND** the upstream push service returns HTTP 410 (Gone) or HTTP 404 (Not Found) throwing `WebPushSubscriptionExpiredException`
- **THEN** the endpoint catches the exception per subscription without propagating an unhandled HTTP 500 error
- **AND** removes the expired subscription rows from `UserPushSubscriptions`
- **AND** sets `User.IsPushEnabled = false` if no active subscriptions remain for that user
- **AND** returns `HTTP 400 Bad Request` with code `PUSH_SUBSCRIPTION_EXPIRED`.

---

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

### Requirement: Resilient Test Push Dispatch & Standardized Error Contracts
The endpoint `POST /api/v1/notifications/push/test` SHALL isolate push delivery errors per subscription, automatically purge expired endpoints, update `User.IsPushEnabled = false` when all endpoints are pruned, and return standardized machine-readable envelopes (`code`, `error`, `details`) with `sent`, `total`, and `stalePurged` metrics.

The endpoint SHALL adhere to the following response contract:
1. **No Registered Subscriptions:** If the user has zero registered push subscriptions, the endpoint SHALL return `HTTP 400 Bad Request` with body:
   ```json
   {
     "code": "PUSH_NO_SUBSCRIPTIONS",
     "error": "No active push subscriptions found for this device.",
     "details": null
   }
   ```
2. **All Subscriptions Expired:** If all registered push subscriptions throw `WebPushSubscriptionExpiredException`, the endpoint SHALL purge the expired subscriptions, update `User.IsPushEnabled = false`, and return `HTTP 400 Bad Request` with body:
   ```json
   {
     "code": "PUSH_SUBSCRIPTION_EXPIRED",
     "error": "All push notification subscriptions for this device have expired. Please toggle notifications off and on to renew your browser subscription.",
     "details": {
       "sent": 0,
       "total": 1,
       "stalePurged": 1
     }
   }
   ```
3. **Successful Dispatch:** When at least one push notification is successfully dispatched, the endpoint SHALL return `HTTP 200 OK` with body:
   ```json
   {
     "success": true,
     "sent": 1,
     "total": 1,
     "stalePurged": 0
   }
   ```
4. **Mixed Dispatch (Partial Expiry):** When some subscriptions succeed and some throw `WebPushSubscriptionExpiredException`, the endpoint SHALL purge the expired subscriptions and return `HTTP 200 OK` with body:
   ```json
   {
     "success": true,
     "sent": 1,
     "total": 2,
     "stalePurged": 1
   }
   ```

#### Scenario: User sends test push with valid active subscription
- **WHEN** an authenticated user calls `POST /api/v1/notifications/push/test` with 1 active, valid subscription
- **THEN** the system dispatches the push payload via `IWebPushService`
- **AND** returns `HTTP 200 OK` with `{ "success": true, "sent": 1, "total": 1, "stalePurged": 0 }`.

#### Scenario: User sends test push with expired browser subscription
- **WHEN** an authenticated user calls `POST /api/v1/notifications/push/test` with 1 subscription that returns HTTP 410/404 from the push service
- **THEN** the endpoint catches `WebPushSubscriptionExpiredException`
- **AND** deletes the stale subscription from `UserPushSubscriptions`
- **AND** sets `user.IsPushEnabled = false`
- **AND** returns `HTTP 400 Bad Request` with code `PUSH_SUBSCRIPTION_EXPIRED`.

#### Scenario: User sends test push with multiple subscriptions where one is expired
- **WHEN** an authenticated user has 2 subscriptions (e.g. mobile and desktop), where mobile is expired (HTTP 410) and desktop is active (HTTP 200)
- **THEN** the endpoint purges the mobile subscription from `UserPushSubscriptions`
- **AND** successfully delivers the push notification to the desktop device
- **AND** preserves `user.IsPushEnabled = true` because 1 subscription remains
- **AND** returns `HTTP 200 OK` with `{ "success": true, "sent": 1, "total": 2, "stalePurged": 1 }`.

#### Scenario: User sends test push without any active subscriptions
- **WHEN** an authenticated user calls `POST /api/v1/notifications/push/test` but has zero subscriptions in `UserPushSubscriptions`
- **THEN** the endpoint returns `HTTP 400 Bad Request` with code `PUSH_NO_SUBSCRIPTIONS`.

---

### Requirement: Localized Settings Error Resolution & Token Expiration Guidance
The Settings interface (`frontend/pages/settings.vue`) SHALL use `useApiError.formatError` to resolve API error codes and server failures against the active locale, suppressing raw unlocalized RFC 7807 ProblemDetails strings.

When a push subscription is detected as expired (`PUSH_SUBSCRIPTION_EXPIRED`) or when the backend returns zero delivered notifications (`res.sent === 0`), the UI SHALL display clear, localized guidance prompting the user to toggle notifications off and on to renew their browser push token.

#### Scenario: User clicks test notification and receives successful delivery
- **WHEN** user clicks "Gửi Thông Báo Thử" and `sendTestPush` returns `{ success: true, sent: 1, ... }`
- **THEN** UI displays a success toast using `settings.web_push_test_success`.

#### Scenario: User clicks test notification with expired browser token
- **WHEN** user clicks "Gửi Thông Báo Thử" and the backend returns `PUSH_SUBSCRIPTION_EXPIRED`
- **THEN** UI displays an error or warning toast using `settings.web_push_test_expired`
- **AND** the toast text instructs the user to toggle the notification switch off and on to renew their browser push token.

#### Scenario: User clicks test notification during unhandled server failure
- **WHEN** user clicks "Gửi Thông Báo Thử" and the backend returns HTTP 500
- **THEN** `useApiError.formatError` resolves the error to `settings.web_push_test_error` or `api_errors.SERVER_ERROR`
- **AND** does NOT display the raw English title `"An unexpected error occurred while processing your request."` to a Vietnamese user.

#### Scenario: Test push response indicates zero sent notifications
- **WHEN** `sendTestPush` completes successfully without throwing but returns `sent: 0`
- **THEN** UI displays a warning toast using `settings.web_push_test_zero_sent`
- **AND** does NOT display a misleading success notification.

### Requirement: Pure OpenSpec Single Source of Truth Architecture
The platform SHALL maintain specifications, business constraints, and acceptance criteria exclusively within modular domain capability files in `openspec/specs/<capability>/spec.md` and active lifecycle changes in `openspec/changes/`. Monolithic legacy documentation directories (`docs/`) SHALL be completely retired, preventing desynchronization, dual sources of truth, and outdated developer context.

#### Scenario: Developer or AI agent inspects platform specifications
- **WHEN** a developer or AI agent queries platform capabilities, business rules, or acceptance criteria
- **THEN** specifications are resolved exclusively from modular domain files in `openspec/specs/` and active changes in `openspec/changes/`
- **AND** static legacy documentation directories such as `docs/` are absent from the repository.

#### Scenario: Developer or AI agent consults architecture invariants
- **WHEN** an AI agent or developer inspects non-negotiable architectural rules and anti-patterns
- **THEN** invariants are resolved from `AGENTS.md` (Sections 2 and 3) without requiring or loading monolithic markdown files (`docs/features.md`, `docs/api-design.md`, `docs/database-design.md`).

#### Scenario: Executable contracts serve as database and API truth
- **WHEN** an engineer or agent inspects endpoint signatures or data models
- **THEN** strongly-typed code in `TechDaily.Api/Endpoints/`, OpenAPI/Swagger specifications, EF Core Entity models, and database migrations serve as the definitive, executable implementation contracts.

### Requirement: Dev-Learning Studio Design System & Navigation Shell
The web frontend SHALL implement the **Dev-Learning Studio** visual language, replacing default Nuxt/VitePress documentation styles with an OLED dark-mode-first aesthetic, Electric Violet branding, translucent hairline borders, glassmorphic surface elevations, and a keyboard-driven command navigation shell.

1. **Design Tokens & Color Palette:**
   - The application theme in `tailwind.config.js` SHALL define semantic layers:
     - `canvas`: `#09080e` (main deep dark background), `subtle: '#12101b'` (primary card surface), `elevated: '#1a1726'` (modals, dropdowns, popovers), `border: 'rgba(255, 255, 255, 0.08)'` (translucent hairline divider).
     - `brand`: Electric Violet spectrum (`brand-500: '#8b5cf6'`, `brand-400: '#a78bfa'`, `brand-600: '#7c3aed'`, `brand-glow: 'rgba(139, 92, 246, 0.35)'`).
     - `streak`: Ember Orange (`amber: '#f59e0b'`, `glow: 'rgba(245, 158, 11, 0.4)'`).
     - `cyber`: Cyber Cyan (`cyber-400: '#22d3ee'`, `cyber-500: '#06b6d4'`).
   - The typography SHALL standardize on crisp font tracking (`tracking-tight`), modern monospace accents, and responsive body scales following project typography invariants (body $\ge 14\text{px}$ on mobile, $\ge 16\text{px}$ on desktop/tablet).

2. **Hairline Borders & Elevation Utilities:**
   - Card and panel components SHALL utilize hairline borders (`border border-white/[0.08]` in dark mode, `border-slate-200/80` in light mode) and glassmorphism backdrop blur (`backdrop-blur-md` / `backdrop-blur-lg`) to provide visual separation and depth without muddy opaque backgrounds.

3. **Global Command Palette Navigation (`AppCommandPalette.vue`):**
   - The application shell SHALL include a global Command Palette modal accessible via keyboard shortcut (`Cmd+K` on macOS, `Ctrl+K` on Windows/Linux) or by clicking the topbar search input.
   - The palette SHALL support real-time fuzzy filtering of navigation destinations across core platform capabilities: Today's Reading Slice (`/`), Spaced Repetition Review (`/review`), Interview Quiz (`/quiz`), Architecture Knowledge Graph (`/graph`), Architecture Roadmap (`/roadmap`), Document Library (`/library`), Highlight Notes (`/notes`), and Settings (`/settings`).
   - The palette SHALL support keyboard navigation (`ArrowDown`, `ArrowUp`, `Enter` to navigate, `Escape` to dismiss) and touch tap on mobile devices.
   - When opened, the palette SHALL focus the search input automatically and prevent background page scrolling.

4. **Modernized Application Shell & Navigation:**
   - The topbar (`AppNavbar.vue` or header in `default.vue`) SHALL feature:
     - Prominent TechDaily monogram/logo with subtle glowing dot indicator.
     - Centered `⌘K Quick Jump` pill trigger button displaying localized placeholder and `⌘K` keyboard badge on desktop viewports.
     - Interactive streak pill displaying the user's active streak count with an amber glow flame icon.
     - Locale switcher (EN / VI) and user profile ring.
   - The navigation sidebar SHALL display sleek icon rail geometry, smooth collapsible state transitions, and an Electric Violet glow accent (`bg-brand-500/10 text-brand-400 border-l-2 border-brand-500`) for the active route.

#### Scenario: User opens application in dark mode with new design tokens
- **WHEN** a user visits any page in dark mode
- **THEN** the body background is rendered with deep OLED `#09080e`
- **AND** primary buttons and active indicators display Electric Violet `#8b5cf6`
- **AND** card borders display translucent hairline styling (`border-white/[0.08]`) rather than solid gray borders.

#### Scenario: User triggers Command Palette via keyboard shortcut
- **WHEN** a user presses `Cmd+K` (on macOS) or `Ctrl+K` (on Windows/Linux) while on any page
- **THEN** the global Command Palette modal smoothly teleports into view
- **AND** the search input is focused immediately with the cursor ready
- **AND** background page scrolling is temporarily disabled.

#### Scenario: User searches and navigates via Command Palette
- **WHEN** the Command Palette is open and the user types `"graph"`
- **THEN** the results list filters instantaneously to show the Knowledge Graph destination
- **WHEN** the user presses `Enter` or clicks the result
- **THEN** the Command Palette closes
- **AND** the router navigates immediately to `/graph`.

#### Scenario: User closes Command Palette via Escape or backdrop click
- **WHEN** the Command Palette is open and the user presses `Escape` or clicks outside the modal
- **THEN** the modal closes cleanly and restores page focus without errors.

#### Scenario: Topbar search trigger on mobile viewport
- **WHEN** a user views the application on a mobile screen ($< 640\text{px}$)
- **THEN** the topbar renders a compact search icon trigger button
- **WHEN** tapped, it opens the full-screen or centered Command Palette.

### Requirement: Home Command Center Dashboard & Zero-Scroll Desktop Layout
The root route `/` SHALL host the primary **Home Command Center Dashboard** (`frontend/pages/index.vue`), presenting an executive overview of daily momentum, active reading slice, scenario drill, retention metrics, 7-day consistency, and knowledge cosmos connectivity.

1. **Information Architecture & Contextual AI Restraint:**
   - **Welcome Banner:** Displays personalized greeting (`"Welcome Back, {Name}!"`) and curriculum progress pill (`"Curriculum Day {N} / 30"`). The banner SHALL NOT include an "Ask AI Explainer" button; AI explanation is strictly reserved for in-context reading text selection and scenario problem solving.
   - **Active Reading Hero:** Displays active curriculum slice title, summary, reading time estimate, progress percentage, and a prominent `"Continue Reading →"` CTA button navigating to `/today`.
   - **Scenario Challenge Card:** Displays the architectural interview scenario teaser, score reward (`+10 Points`), and a `"Solve Challenge →"` CTA button navigating to `/today`.
   - **Active Recall & Concentric Metrics:** Renders dual concentric SVG rings for daily study pace and SM-2 retention health, constrained in height to prevent stretching.
   - **7-Day Consistency Matrix:** Renders weekly completion dots and active streak flames with freeze credit indicators.
   - **Knowledge Graph Radar:** Displays connected concept counts and active relation counts with a direct link to the 3D Cosmos (`/graph`).

2. **Zero-Scroll Single-Screen Desktop Layout Invariant:**
   - On desktop screens ($\ge 1024\text{px}$), the dashboard container and column flexboxes SHALL be top-aligned (`justify-start`) with consistent, snug vertical gaps (`gap-3.5 sm:gap-4`), preventing cards from scattering or dispersing to the vertical extremes on tall displays while fitting entirely within the viewport (`h-[calc(100vh-3.5rem)]`) with zero required scrolling.
   - On mobile ($< 640\text{px}$) and tablet ($640\text{px} - 1023\text{px}$) viewports, the layout SHALL transition to a natural vertically scrollable stack.

3. **Global Navigation Alignment:**
   - The desktop sidebar (`AppSidebar.vue`) and mobile navigation drawer SHALL represent `/` as the primary `"Dashboard"` / `"Home"` entry and `/today` as `"Today's Focus"` / `"Focus Studio"`.
   - The command palette (`AppCommandPalette.vue`) SHALL register `/` as the primary Dashboard route.

#### Scenario: Authenticated user visits root route /
- **WHEN** an authenticated user navigates to `/`
- **THEN** the system renders the Home Command Center Dashboard
- **AND** the Welcome Banner displays the user's greeting without an AI explainer button
- **AND** all metrics and active curriculum cards populate.

#### Scenario: Single-screen desktop presentation
- **WHEN** the dashboard is viewed on a desktop viewport ($\ge 1024\text{px}$)
- **THEN** the entire dashboard container fits within the viewport height without vertical scrolling
- **AND** the Concentric Metric Card, 7-Day Consistency Matrix, and Knowledge Graph Radar are simultaneously visible above the fold.

#### Scenario: User clicks Continue Reading on Home Dashboard
- **WHEN** the user clicks "Continue Reading" on the active reading slice card
- **THEN** the router navigates to `/today`
- **AND** the Focus Studio reading pane renders the active slice.

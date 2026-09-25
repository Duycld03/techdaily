# Core Platform Specification

## Purpose
Provides the foundational user authentication, user profile management, daily curriculum doc reading slices, scenario challenges, SM-2 spaced repetition engine, document library, and background notification dispatches for the TechDaily platform.

## Requirements

### Requirement: Standard Email & Password Authentication
The system SHALL allow users to register an account with email, password (min 8 characters), full name, and preferred locale (`POST /api/v1/auth/register`), securely hash passwords using PBKDF2 with SHA-256 (16-byte random salt, 600,000 iterations), and authenticate users via email and password (`POST /api/v1/auth/login`), issuing a 256-bit JWT bearer token upon successful verification. On login, if the stored password hash uses fewer than 600,000 iterations, the system SHALL transparently rehash the password with 600,000 iterations after successful verification.

#### Scenario: User registers with valid email and password
- **WHEN** visitor sends `POST /api/v1/auth/register` with valid email, name, and password >= 8 characters
- **THEN** system provisions user entity with PBKDF2 password hash (600,000 iterations), creates user learning stats, and returns `201 Created` with JWT token and refresh token.

#### Scenario: User authenticates with registered credentials
- **WHEN** user sends `POST /api/v1/auth/login` with registered email and correct password
- **THEN** system verifies hash and returns `200 OK` with JWT bearer token, refresh token, and user profile.

#### Scenario: Existing user with legacy iteration count logs in
- **WHEN** user with a password hashed at 100,000 iterations sends `POST /api/v1/auth/login` with correct password
- **THEN** system verifies the password against the stored hash, rehashes with 600,000 iterations, persists the updated hash, and returns the normal login response

#### Scenario: User attempts to register with password shorter than 8 characters
- **WHEN** visitor sends `POST /api/v1/auth/register` with a password of 7 characters or fewer
- **THEN** system returns `HTTP 400` with error code `AUTH_PASSWORD_TOO_SHORT`
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

The User Profile interface (`frontend/pages/profile.vue`) SHALL present an executive **3-Tier Bento Dashboard** adhering to the **Dev-Learning Studio** visual standard:
1. **Root Container**: Renders on deep dark canvas (`dark:bg-canvas`, `dark:bg-canvas-subtle`) with `scrollbar-gutter: stable` and responsive spacing, utilizing a dedicated, non-duplicated page subtitle (`profile.subtitle`).
2. **Tier 1 (Top Full-Width Engineer Identity Passport)**: A prominent `.glass-card` banner spanning full width across the top featuring:
   - Large avatar with status indicator badge.
   - Engineer display name, email address, and membership tenure (`Member since: <date>`).
   - Badges for Target Role (e.g. `Senior Engineer`), Account Connection (Google Linked / Standard Email), and Personal Best Record (`Longest Streak: <days>` trophy badge).
3. **Tier 2 (Full-Width Milestones Telemetry Strip)**: A prominent horizontal 4-column bento telemetry strip (`grid-cols-2 lg:grid-cols-4 gap-4`) sitting immediately beneath the identity passport:
   - *Cell 1 - Architecture Drills*: Total senior scenario drills completed and average score (`{totalDrillsCompleted}` completed, `{averageScore}/10`).
   - *Cell 2 - Interview Quiz Accuracy*: Accuracy percentage (`{accuracyRate}%`) and mastered concepts ratio (`{masteredCount}/{totalAnswered}`), utilizing concise, non-truncated copy across all locales (e.g. "Độ chính xác Quiz" in Vietnamese).
   - *Cell 3 - Memory Vault (Spaced Repetition)*: Total technical concepts active in the SM-2 review deck (`{totalCardsInDeck}`).
   - *Cell 4 - Architecture Source Highlights*: Total highlighted code and architectural quotes saved (`{totalHighlightsSaved}`).
4. **Tier 3 (Balanced 2-Column Split)**: A balanced 50/50 grid on desktop (`lg:grid-cols-2 gap-6 items-start`):
   - *Left Column (Account & Security Hub)*: Houses the settings and credentials forms inside a dedicated `.glass-card` (~360px height) with clean tab switching:
     - *Personal Info Tab*: Full Name input, Target Role selector, interactive Daily Goal Pace chips (`5m`, `10m`, `15m`, `30m`).
     - *Security Tab*: Current password verification, new password with dynamic strength bar, confirm password matching, and Google account hint banner.
   - *Right Column (Domain Mastery Goal Tracker)*: Houses `DomainGoalTracker.vue` (~380px height), presenting curriculum domain coverage across four universal, framework-agnostic core engineering pillars with compact padding and high-contrast gradient tracks:
     - **Pillar 1: Backend Runtime & Concurrency** (`profile.domain_backend_runtime`: "Nền Tảng Backend & Runtime" / "Backend Runtime & Concurrency").
     - **Pillar 2: Data Storage & Persistence** (`profile.domain_data_storage`: "Hệ Lưu Trữ & Cơ Sở Dữ Liệu" / "Data Storage & Persistence").
     - **Pillar 3: Distributed Systems & Architecture** (`profile.domain_system_design`: "Hệ Thống Phân Tán & Thiết Kế" / "Distributed Systems & Architecture").
     - **Pillar 4: Frontend & Browser Engineering** (`profile.domain_frontend`: "Hiệu Năng Frontend & Trình Duyệt" / "Frontend & Browser Engineering").

The Domain Mastery Goal Tracker component SHALL evaluate and aggregate topic mastery dynamically across multi-stack keywords via `matchCategory(keyOrTopic: string)`, ensuring book chapters, drills, and quiz attempts in any modern stack map seamlessly into the appropriate universal pillar.

The Engineer Portfolio Dashboard SHALL adapt responsively across Desktop (≥ 1024px, 3-tier layout with balanced 50/50 lower columns) and Mobile (< 1024px, vertically stacked layout with Identity Passport -> 2x2 Milestones Strip -> Domain Mastery -> Account Settings) without horizontal scrolling or layout overlap.

The Identity passport, milestone statistics, and domain goal tracker SHALL support bilingual rendering in both English and Vietnamese, ensuring that milestone metric titles, domain pillar names, and subtitles render cleanly without text truncation, badge clipping, or broken progress bar labels.

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
- **WHEN** authenticated user accesses `/profile` on a desktop viewport ($\ge 1024\text{px}$)
- **THEN** the layout renders a 3-tier executive structure: Tier 1 full-width Identity Passport, Tier 2 full-width 4-column Milestones Telemetry Strip, and Tier 3 balanced 50/50 2-column grid with Account & Security Hub on the left (~360px) and Domain Mastery Goal Tracker on the right (~380px), eliminating empty vertical void space
- **WHEN** user accesses `/profile` on a mobile viewport ($< 1024\text{px}$)
- **THEN** the layout stacks into a single vertical stream with the Identity Passport at the top, followed by the 2x2 Milestones Strip, Domain Mastery tracker, and Account settings form, maintaining touch-friendly targets and zero horizontal overflow.

#### Scenario: User inspects identity and learning milestones widget
- **WHEN** user views the profile page
- **THEN** UI renders the full-width 4-column milestones telemetry strip displaying:
  - Architecture Drills completed with average score
  - Interview Quiz accuracy percentage and mastered topics count with non-truncated concise title ("Độ chính xác Quiz" / "Quiz Accuracy")
  - Spaced Repetition Memory Vault active card count
  - Architecture Highlights saved count
- **AND** all 4 cells render in individual `.glass-card` surfaces with hairline borders and distinct semantic accent badges.

#### Scenario: User monitors curriculum domain mastery goal progress
- **WHEN** user views the Domain Mastery Goal Tracker section on `/profile`
- **THEN** UI displays categorized visual progress bars for each of the 4 universal engineering pillars with compact vertical padding:
  - Backend Runtime & Concurrency (aggregating .NET, Node.js, Go, Java, Python runtime topics)
  - Data Storage & Persistence (aggregating PostgreSQL, MongoDB, Redis, MySQL, ACID, indexing topics)
  - Distributed Systems & Architecture (aggregating microservices, Kafka, outbox, system design topics)
  - Frontend & Browser Engineering (aggregating browser performance, Vue, React, TypeScript topics)
- **AND** topic stats from multi-stack curricula accurately increment completed and total counts in the corresponding universal pillar.

#### Scenario: Bilingual visual verification for identity widget, milestone stats, and domain goal tracker
- **WHEN** user toggles between English (`en`) and Vietnamese (`vi`) on the `/profile` page
- **THEN** all copy across the Identity passport, milestones telemetry strip (drills completed, quiz accuracy, memory vault, highlights saved), and domain goal tracker updates dynamically
- **AND** the quiz accuracy card label in Vietnamese renders as "Độ chính xác Quiz" without ellipsis truncation or badge clipping
- **AND** the page header subtitle renders "Quản lý hồ sơ kỹ sư, năng lực chuyên môn và bảo mật tài khoản" in Vietnamese and "Manage your senior engineer profile, career telemetry, and security credentials" in English, without duplicating the domain mastery subtitle.

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
The Settings interface (`frontend/pages/settings.vue`) SHALL serve as the exclusive single source of truth for notification schedule configuration (`preferredStudyTime`, `streakAlertTime`) and timezone preferences (`timeZone`), adhering to the **Dev-Learning Studio** visual standard.

The Settings interface SHALL render on an Obsidian Canvas (`dark:bg-canvas`, `dark:bg-canvas-subtle`) and organize controls into `.glass-card` surfaces with hairline borders (`dark:border-white/[0.08]`):
1. **Appearance & Language**: Interface language switcher (`LocaleSelector.vue`) and color theme toggle (`ThemeToggle.vue`) with Studio hairline elevation.
2. **Web Push Notifications**: Push activation toggle with Electric Violet active state, push active status banner with subtle violet glow, study schedule time inputs, and IANA timezone selector.

The timezone options list SHALL be reactive and deduplicated, automatically incorporating the user's detected local timezone or saved profile timezone.

When the user selects a new timezone in `/settings`, the client SHALL automatically persist the selection to the backend (`PUT /api/v1/user/profile`), guaranteeing immediate server-side persistence matching the auto-save behavior of interface language and theme settings.

All user modifications to notification reminder timing and timezone detection SHALL occur within the Settings domain and be persisted via `PUT /api/v1/user/profile` or the Web Push subscription flow (`POST /api/v1/notifications/push/subscribe`).

#### Scenario: User configures study schedule and timezone in `/settings`
- **WHEN** an authenticated user adjusts their preferred study time, streak alert time, or timezone in `/settings` and clicks "Save Schedule"
- **THEN** the client dispatches `PUT /api/v1/user/profile` containing `{ preferredStudyTime, streakAlertTime, timeZone }`
- **AND** the server updates these preferences in PostgreSQL and returns `200 OK`.

#### Scenario: User updates Web Push subscription with timezone synchronization
- **WHEN** a user enables Web Push notifications in `/settings`
- **THEN** the client automatically includes the detected or selected IANA timezone identifier in the subscription request
- **AND** the backend updates `User.TimeZone` and `User.IsPushEnabled = true` simultaneously.

#### Scenario: User selects a new timezone from the dropdown
- **WHEN** a user changes the timezone in `frontend/pages/settings.vue` via `AppSelect`
- **THEN** the client immediately updates `timeZone.value` and automatically persists `{ timeZone }` to `PUT /api/v1/user/profile`
- **AND** displays a success toast confirmation to the user.

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
The web frontend SHALL implement the **Dev-Learning Studio** visual language, replacing default Nuxt/VitePress documentation styles with a neutral obsidian dark-mode-first aesthetic, Deep Iris Violet branding, translucent hairline borders, decluttered glassmorphic surface elevations, and a keyboard-driven command navigation shell following the 60-30-10 color hierarchy:

1. **Design Tokens & Color Palette:**
   - The application theme in `tailwind.config.js` SHALL define semantic layers:
     - `canvas`: `#09090b` (main neutral dark obsidian background), `subtle: '#121215'` (primary card surface), `elevated: '#18181b'` (modals, dropdowns, popovers), `border: 'rgba(255, 255, 255, 0.08)'` (translucent hairline divider).
     - `brand`: Deep Iris Violet spectrum (`brand-500: '#7c3aed'`, `brand-400: '#a78bfa'`, `brand-600: '#6d28d9'`, `brand-glow: 'rgba(124, 58, 237, 0.35)'`).
     - `streak`: Ember Orange (`amber: '#f59e0b'`, `glow: 'rgba(245, 158, 11, 0.4)'`).
     - `cyber`: Cyber Cyan (`cyber-400: '#22d3ee'`, `cyber-500: '#06b6d4'`).
   - The typography SHALL standardize on crisp font tracking (`tracking-tight`), modern monospace accents, and responsive body scales following project typography invariants (body $\ge 14\text{px}$ on mobile, $\ge 16\text{px}$ on desktop/tablet).

2. **Hairline Borders & Elevation Utilities:**
   - Card and panel components SHALL utilize hairline borders (`border border-white/[0.08]` in dark mode, `border-slate-200/80` in light mode) and glassmorphism backdrop blur (`backdrop-blur-md` / `backdrop-blur-lg`) to provide visual separation and depth without muddy opaque backgrounds or ambient radial blur glows.

3. **Global Command Palette Navigation (`AppCommandPalette.vue`):**
   - The application shell SHALL include a global Command Palette modal accessible via keyboard shortcut (`Cmd+K` on macOS, `Ctrl+K` on Windows/Linux) or by clicking the topbar search input.
   - The palette SHALL support real-time fuzzy filtering of navigation destinations across core platform capabilities.

4. **Modernized Application Shell & Navigation (Zero-Shift Transitions):**
   - The topbar (`AppHeader.vue`) SHALL feature:
     - Prominent TechDaily monogram/logo with subtle glowing dot indicator.
     - Centered `⌘K Quick Jump` pill trigger button displaying localized placeholder and `⌘K` keyboard badge on desktop viewports.
     - Interactive streak pill displaying the user's active streak count with an amber glow flame icon.
     - Locale switcher (`LocaleSelector.vue`) with smooth `transition-colors` and user profile ring.
   - The navigation sidebar (`AppSidebar.vue`) links SHALL maintain a constant 2px left border geometry across both active and inactive states (`border-l-2 border-transparent` when inactive; `border-l-2 border-brand-500` when active) and constrain animations to `transition-colors`, eliminating horizontal layout shifting and border collapse flicker when navigating between routes.

5. **Universal Zero-Shift Segmented Controls & Tab Switchers:**
   - All segmented view switchers, tab bars, and mode toggles (`RoadmapViewSwitcher.vue`, `review.vue`, `quiz.vue`, `library.vue`, `profile.vue`) SHALL maintain a constant 1px border geometry across both active and inactive states (`border border-transparent` when inactive; `border border-slate-200/80 dark:border-white/[0.12]` or `dark:border-white/[0.06]` when active).
   - Inactive buttons in segmented controls SHALL pre-allocate `border border-transparent`, and transitions SHALL be strictly restricted to `transition-colors` (duration 150ms).
   - The system SHALL NEVER apply `transition-all` to segmented controls or tab switchers where border appearance or padding could be animated, completely eliminating the 1px twitch, flicker, and layout jump when switching views (e.g., between "Dạng Dòng Thời Gian" and "Dạng Sơ Đồ Tư Duy" in the roadmap).

6. **Floating Dropdown Isolation & Auto-Flip Collision Prevention:**
   - Floating dropdown popovers (`AppSelect.vue`) SHALL render via `<Teleport to="body">` with fixed positioning calculated from trigger bounding rect coordinates, rendering outside parent scroll containers (`overflow-y-auto`, `overflow: hidden`, `max-h-[90vh]`).
   - The dropdown popover SHALL NOT expand the scrollable height (`scrollHeight`) of its parent container or modal, preventing modal dialogs (`library.vue`) and page views (`settings.vue`) from spawning sudden vertical scrollbars or causing horizontal content jumps.
   - The dropdown popover SHALL automatically detect vertical viewport clearance: when the available space between the trigger bottom and the viewport bottom is insufficient (< 260px) and there is more space above, the popover SHALL flip upwards above the trigger, eliminating bottom clipping and viewport overflow.
   - When open, the floating popover SHALL update coordinates on window `scroll` (capture mode) and dismiss seamlessly on click-outside and `Escape`.

7. **Viewport & Modal Scrollbar Track Stability**:
   - The root `html` container SHALL declare `scrollbar-gutter: stable`, reserving space for the vertical scrollbar track at all times.
   - Scrollable modal dialog bodies and drawers (`overflow-y-auto`) SHALL include `scrollbar-gutter: stable`, ensuring that internal content additions or tab transitions do not produce horizontal layout shifts or jarring content reflows.

8. **Keyboard Accessibility & Focus Ring Standards (WCAG 2.1 AA)**:
   - Interactive elements (`button`, `a`, `input`, `textarea`, `select`, `[tabindex]`) SHALL provide prominent, high-contrast visual focus rings when navigated via keyboard (`:focus-visible`).
   - The keyboard focus ring SHALL utilize Electric Violet (`outline: 2px solid #8b5cf6; outline-offset: 2px;`) across both light and dark themes.
   - Pointer or touch click interactions SHALL NOT produce persistent sticky focus outlines, enforced via `:focus:not(:focus-visible) { outline: none; }`.

9. **Mobile Dynamic Viewport Height Standards**:
   - Full-height reading views, studio workspaces, and viewports SHALL employ dynamic viewport height units (`h-dvh` or `min-h-[100dvh]`) rather than static `h-screen` (`100vh`), preventing viewport clipping and overflow underneath mobile browser dynamic chrome.

10. **Semantic Overlay Z-Index Stacking Hierarchy**:
   - Overlay and floating layers SHALL adhere to a deterministic, semantic z-index scale:
     - Global Toast Notifications: `z-[9999]`
     - Global Command Palette (`⌘K`): `z-60`
     - Floating Dropdown Popovers (`AppSelect.vue`, `AppTimePicker.vue`): `z-[60]`
     - Full-screen Modals and Teleported Drawers: `z-50`
     - Contextual Tooltips and In-Page Menus: `z-40`
     - Sticky Header and Top Navigation Bars: `z-30`
     - In-Page Floating Action Bars and Canvas Controls: `z-10`

11. **Clean Engineering Iconography Standard**:
    - The application iconography SHALL standardize on `lucide-vue-next` following clean engineering aesthetics:
      - **Stroke Width**: Icons on navigation bars, Bento cards, action buttons, and input controls SHALL enforce a sleek 1.5px stroke weight (`:stroke-width="1.5"`), replacing clunky default 2px lines.
      - **3-Tier Sizing**: Micro metadata and inline tags SHALL use `w-3.5 h-3.5` (14px); interactive controls, inputs, and tabs SHALL use `w-4 h-4` (16px); feature tiles and studio section headers SHALL use `w-5 h-5` (20px).
      - **Standardized Icon Tile Container**: Feature cards and section banners SHALL house prominent icons within a standard glass tile container (`p-2.5 rounded-2xl bg-brand-500/10 border border-brand-500/20 text-brand-600 dark:text-brand-400 shrink-0`).
      - **Semantic Purpose**: The `Sparkles` icon SHALL NOT be used as a generic loading spinner or catch-all decoration. Loading states SHALL use dedicated `Loader2 class="animate-spin"`. Navigation and insights features SHALL use purposeful domain icons (e.g. `Compass` for Insights).
      - **Unified Palette**: Secondary metadata icons SHALL use neutral slate tones (`text-slate-400 dark:text-slate-500`), avoiding disparate rainbow icon fills across single cards.

12. **Native Form Controls & Color-Scheme Alignment**:
    - The application root (`html.dark`, `.dark`) and native input controls (`input[type="time"]`, `input[type="date"]`, `input[type="datetime-local"]`) SHALL declare `color-scheme: dark;` when operating in dark mode, ensuring that native browser popup dialogs and User-Agent Shadow DOM pickers render with dark background surfaces and high-contrast text.
    - Light mode (`html:not(.dark)`) SHALL declare `color-scheme: light;`.
    - Native calendar and clock picker indicators (`::-webkit-calendar-picker-indicator`) SHALL render with high contrast, brand accent styling, and `cursor: pointer` in dark mode.
    - All workspace loading states SHALL utilize dedicated `Loader2` spinners (`class="animate-spin"`) with 1.5px stroke weight, eliminating decorative sparkle animations for loading processes.

13. **Studio Native Time Input Architecture (`AppTimePicker.vue`)**:
    - The application SHALL provide a dedicated `AppTimePicker.vue` component rendering as an in-place native time input (`<input type="time">`) styled according to TechDaily form control standards, eliminating dropdown popovers, floating overlays, and duplicate time displays.
    - The input container SHALL adhere to standard input dimensions (`h-11`, `rounded-xl`, `border-slate-200/90 dark:border-white/[0.08]`, `bg-white dark:bg-canvas-subtle`) with an inset 1.5px stroke `Clock` icon on the left.
    - The component SHALL accept `modelValue: string | null | undefined`, sanitize input strings (`HH:mm`), and emit `update:modelValue` and `change` with 24-hour formatted time upon selection.
    - The native calendar/picker indicator icon SHALL remain clickable and adapt cleanly to Dark Mode via invert filter.
#### Scenario: User opens application in dark mode with new design tokens
- **WHEN** a user visits any page in dark mode
- **THEN** the body background is rendered with neutral dark obsidian `#09090b`
- **AND** primary buttons and active indicators display Deep Iris Violet `#7c3aed`
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

#### Scenario: Modal locks body scroll without layout shift
- **WHEN** a user opens a modal, drawer, or the Command Palette (`Cmd+K`) on a desktop screen with a visible scrollbar
- **AND** the application sets `document.body.style.overflow = 'hidden'`
- **THEN** the root `html` retains its stable scrollbar gutter
- **AND** centered page containers (e.g., `max-w-6xl mx-auto`) experience zero horizontal layout shift ($0\text{px}$ shift).

#### Scenario: Keyboard user tabs through interactive elements
- **WHEN** a user navigates interactive buttons or links using the `Tab` key
- **THEN** each active element displays an Electric Violet 2px focus ring with 2px offset (`:focus-visible`)
- **WHEN** the user clicks an element with a mouse or tap pointer
- **THEN** no persistent outline or box-shadow ring remains visible.

#### Scenario: Mobile reader renders on dynamic viewport
- **WHEN** a user opens the reader view (`/read/[bookId]`) on a mobile browser with dynamic address bars (e.g. iOS Safari)
- **THEN** the reader container scales to dynamic viewport height (`h-dvh`)
- **AND** the top navigation bar and bottom pagination footer remain fully visible within the active screen area without being concealed by the browser UI.

#### Scenario: Layer stacking order across simultaneous overlays
- **WHEN** a toast notification fires while the Command Palette and a contextual popover are visible
- **THEN** the Toast (`z-[9999]`) renders above the Command Palette (`z-60`), which renders above any standard modal or drawer (`z-50`), preventing visual collision or z-index clipping.

#### Scenario: User opens custom dropdown inside an overflow-y-auto modal
- **WHEN** user clicks an `AppSelect` dropdown inside the document import modal (`library.vue`)
- **THEN** the options listbox is teleported to `document.body` with fixed viewport coordinates directly aligned with the trigger button
- **AND** the modal's `scrollHeight` does not expand and no new vertical scrollbar is spawned
- **AND** the options listbox is completely visible above the modal overlay without being clipped by the modal's bottom border.

#### Scenario: User opens dropdown near the bottom of the viewport
- **WHEN** user clicks the timezone `AppSelect` near the bottom of `settings.vue` where bottom clearance is less than 260px
- **THEN** the popover automatically flips upwards above the trigger button
- **AND** the page does not expand downwards or trigger a browser scrollbar jump.

#### Scenario: User switches views in Roadmap view switcher
- **WHEN** user clicks between "Dạng Dòng Thời Gian" (Timeline) and "Dạng Sơ Đồ Tư Duy" (Mindmap)
- **THEN** both buttons maintain identical 1px border geometry (`border border-transparent` when inactive, `border dark:border-white/[0.06]` when active)
- **AND** color transitions occur via `transition-colors` without any 1px layout twitch, geometry shift, or visual flicker.

#### Scenario: User opens custom AppSelect dropdown in dark mode
- **WHEN** user clicks or taps the custom `AppSelect` trigger on `/profile`, `/settings`, `/quiz`, or `/library`
- **THEN** a floating listbox popover smoothly opens anchored below the trigger
- **AND** the popover renders with studio glass-panel elevation (`dark:bg-canvas-elevated`, `dark:border-white/[0.08]`, `shadow-2xl`)
- **AND** options render as styled studio cards without delegating rendering to the host operating system window manager.

#### Scenario: User navigates and selects option via keyboard
- **WHEN** the `AppSelect` dropdown is focused and user presses `ArrowDown` or `ArrowUp`
- **THEN** visual highlight moves sequentially between options with `:focus-visible` studio tokens
- **WHEN** user presses `Enter` or `Space` on an option
- **THEN** the value is updated via `v-model`, the active selection shows a checkmark indicator, and the dropdown closes.

#### Scenario: User dismisses AppSelect dropdown via Escape or outside click
- **WHEN** the `AppSelect` dropdown is open and the user presses `Escape` or clicks anywhere outside the component
- **THEN** the dropdown closes immediately and returns focus cleanly to the trigger button.

#### Scenario: Universal code block terminal surface consistency across routes
- **WHEN** a user views a code block in the GitBook reader (`/read/[bookId]`), daily focus reader (`/today`), or interview scenario challenge
- **THEN** the code block renders inside a `dark:bg-canvas-subtle` container with `dark:border-white/[0.08]` hairline border
- **AND** displays the glassmorphic terminal header with traffic-light window dots, Deep Iris Violet language telemetry, and glassmorphic copy button
- **AND** syntax highlighting renders on a transparent background matching the container obsidian canvas.

#### Scenario: Universal form select styling across themes
- **WHEN** a user views or interacts with any `<select>` input control across the application (e.g., target role in `/profile`, book selector in `/quiz`, timezone in `/settings`, or category in `/library`)
- **THEN** default OS/browser appearance is suppressed (`appearance: none`)
- **AND** a custom theme-calibrated SVG dropdown chevron renders on the right side without colliding with option text
- **AND** dropdown `<option>` items render with crisp contrast in both Light Mode (`#ffffff` background) and Dark Mode (`#18181b` canvas-elevated background).

#### Scenario: Icons render with refined 1.5px stroke weight and semantic loading spinners
- **WHEN** a user navigates between studio workspaces (Today, Quiz, Insights, Library, Reader)
- **THEN** interactive icons render with a sleek 1.5px stroke weight
- **AND** loading states display dedicated `Loader2` spinners without using spinning sparkle icons.

#### Scenario: User opens native time picker in dark mode
- **WHEN** a user clicks on a native time input control (such as preferred study time in `/settings`) while the application is in dark mode
- **THEN** the native browser dropdown picker renders in dark theme with dark canvas background and high-contrast text
- **AND** the picker does not flash a stark white (`#ffffff`) background.

#### Scenario: Daily focus workspace renders clean engineering loading state
- **WHEN** a user visits the daily focus workspace (`/today`) while daily topics are loading
- **THEN** the loading container displays a `Loader2` spinner with 1.5px stroke weight rather than a spinning sparkle icon.

#### Scenario: User changes time via native AppTimePicker control
- **WHEN** user selects or types a new time `20:00` into the native time input on `/settings`
- **THEN** the component emits `update:modelValue` with `20:00`
- **AND** the input value reflects the update immediately in-place.

#### Scenario: User navigates native AppTimePicker on mobile device
- **WHEN** user taps the time input on a touch screen
- **THEN** the device's native time picker interface appears
- **AND** confirming the selection updates the model value with zero layout shifting.
---

### Requirement: Home Command Center Dashboard & Zero-Scroll Desktop Layout
The root route `/` SHALL host the primary **Home Command Center Dashboard** (`frontend/pages/index.vue`), presenting an executive overview of daily momentum, active reading slice, scenario drill, retention metrics, 7-day consistency, and knowledge cosmos connectivity with clean visual decluttering:

1. **Information Architecture & Contextual AI Restraint:**
   - **Component Organization & Decoupling:** The Home Dashboard component SHALL reside in `frontend/components/dashboard/HomeBentoDashboard.vue`, cleanly decoupled from the reading focus studio components in `frontend/components/today/`.
   - **Welcome Banner:** Displays personalized greeting (`"Welcome Back, {Name}!"`) and dynamic document progress pill (`"Slice {currentChunkOrder} / {totalChunks}"` / `"Lát cắt {currentChunkOrder} / {totalChunks}"`). The banner SHALL NOT include an "Ask AI Explainer" button and SHALL NOT display ambient radial blur glows.
   - **Active Reading Hero:** Displays active curriculum slice title, summary, reading time estimate, progress percentage, and a prominent `"Continue Reading →"` CTA button that navigates directly to the dedicated GitBook reader at that slice (`/read/${bookId}?slice=${currentChunkOrder}`).
   - **Scenario Challenge Card:** Displays the architectural interview scenario teaser, score reward (`+10 Points`), and a `"Solve Challenge →"` CTA button navigating directly to the Focus Studio scenario challenge (`/today?tab=challenge`).
   - **Active Recall & Concentric Metrics:** Renders dual concentric SVG rings for daily study pace and SM-2 retention health, constrained in height to prevent stretching.
   - **7-Day Consistency Matrix:** Renders weekly completion dots and active streak flames with freeze credit indicators.
   - **Domain Knowledge Constellation:** Renders an engineering-grade constellation widget (`frontend/components/dashboard/DomainConstellationCard.vue`) displaying 5 core engineering pillars, connected vector paths, live node and relation counts, and a direct link to the 3D Cosmos (`/graph`). The SVG vertices SHALL render as clean, static circular nodes without animated pulsing rings, flashing aura effects, or scaling transforms (`animate-pulse`, `animate-ping`), providing a stable, distraction-free visual presentation. The card header link SHALL NOT shift horizontally on card body hover.

2. **Crash-Free Lifecycle & Store Hardening:**
   - When querying Spaced Repetition deck stats, the component SHALL safely invoke valid `useReviewStore` methods (`fetchDeckCards({ pageSize: 1 })` or `fetchReviewDeck()`) and read counts from `deckStatistics` and `totalCardsDue` with defensive fallbacks, NEVER calling non-existent methods or throwing unhandled synchronous exceptions.
   - When querying Knowledge Graph data, the component SHALL read from `graphStore.rawData` with defensive fallbacks.
   - Full page refreshes (F5) and direct URL navigation to `/` SHALL render the dashboard reliably with zero uncaught runtime errors and zero redirection to `error.vue` (500 Internal Server Error).

3. **Zero-Scroll Single-Screen Desktop Layout Invariant:**
   - On desktop screens ($\ge 1024\text{px}$), the dashboard container and column flexboxes SHALL be top-aligned (`justify-start`) with consistent, snug vertical gaps (`gap-3.5 sm:gap-4`), preventing cards from scattering or dispersing to the vertical extremes on tall displays while fitting entirely within the viewport (`h-[calc(100vh-3.5rem)]`) with zero required scrolling.
   - On mobile ($< 640\text{px}$) and tablet ($640\text{px} - 1023\text{px}$) viewports, the layout SHALL transition to a natural vertically scrollable stack.

4. **Global Navigation Alignment:**
   - The desktop sidebar (`AppSidebar.vue`) and mobile navigation drawer SHALL represent `/` as the primary `"Dashboard"` / `"Home"` entry and `/today` as `"Today's Focus"` / `"Focus Studio"`.
   - The command palette (`AppCommandPalette.vue`) SHALL register `/` as the primary Dashboard route.
   - The global top header (`AppHeader.vue`) SHALL render dynamic slice progress without hardcoded `/ 30` boundaries.

#### Scenario: Authenticated user visits root route /
- **WHEN** an authenticated user navigates to `/`
- **THEN** the system renders the Home Command Center Dashboard
- **AND** the Welcome Banner displays the user's greeting without an AI explainer button
- **AND** all metrics and active curriculum cards populate.

#### Scenario: Single-screen desktop presentation
- **WHEN** the dashboard is viewed on a desktop viewport ($\ge 1024\text{px}$)
- **THEN** the entire dashboard container fits within the viewport height without vertical scrolling
- **AND** the Concentric Metric Card, 7-Day Consistency Matrix, and Knowledge Graph Constellation are simultaneously visible above the fold.

#### Scenario: User clicks Continue Reading on Home Dashboard
- **WHEN** the user clicks "Continue Reading" on the active reading slice card
- **THEN** the router navigates to `/read/${bookId}?slice=${currentChunkOrder}`
- **AND** the GitBook reader renders the document positioned at that active slice.

#### Scenario: User clicks Solve Challenge on Home Dashboard
- **WHEN** the user clicks "Solve Challenge" on the scenario drill card
- **THEN** the router navigates to `/today?tab=challenge`
- **AND** the Focus Studio opens with the architectural challenge pane active.

#### Scenario: Authenticated user loads or refreshes the Home Dashboard
- **WHEN** an authenticated user navigates directly to `/` or performs a browser refresh (F5)
- **THEN** the server and client render the Home Bento Dashboard without triggering unhandled JavaScript lifecycle exceptions or navigating to the 500 error boundary page.

#### Scenario: User inspects the Domain Knowledge Constellation card
- **WHEN** user views Card E on the Home Dashboard
- **THEN** the card renders a clean SVG constellation displaying 5 engineering pillar vertices as static nodes without animated pulsing halos or flashing effects
- **AND** the "Open 3D Cosmos" header link remains stable in position when hovering anywhere on the card.

#### Scenario: Store data is initially empty or loading
- **WHEN** store data is in a loading or empty state during dashboard initialization
- **THEN** the dashboard renders graceful fallbacks for metrics, cards, and constellation telemetry without throwing `TypeError` or breaking layout geometry.

### Requirement: Global Error Experience & Authentication Studio Layout
The global error boundary page (`frontend/error.vue`) and authentication views (`frontend/pages/login.vue`) SHALL adhere to the **Dev-Learning Studio** visual theme:

1. **Global Error Page Refinement (`error.vue`):**
   - The error page SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) instead of legacy slate.
   - The central error card SHALL utilize `.glass-panel` elevation with translucent hairline borders (`border-white/[0.08]`).
   - Legacy Emerald styling (`bg-emerald-600`, `text-emerald-400`) SHALL be replaced with Deep Iris Violet (`brand-600` / `brand-500`) for primary action buttons (`Back to Daily Practice`) and status code badges.

2. **Authentication Studio Modernization (`login.vue`):**
   - The login/register form card SHALL render using `.glass-panel` over `dark:bg-canvas`.
   - The authentication mode switcher (`Sign In` / `Register`) SHALL feature clean glass tab styling.
   - Text input fields SHALL render with subtle dark glass backgrounds (`bg-white/[0.04] dark:bg-canvas-subtle`) and hairline borders (`border-white/[0.08]`).

#### Scenario: User encounters system error or 404
- **WHEN** an unhandled error or missing page occurs
- **THEN** the system displays the error boundary page over a neutral obsidian canvas
- **AND** the primary action button displays Deep Iris Violet without legacy emerald colors.

#### Scenario: User visits login page
- **WHEN** a visitor navigates to `/login`
- **THEN** the authentication card displays glass panel styling with refined brand accents.

---

### Requirement: Interactive API Documentation & OpenAPI Explorer
The backend system SHALL generate OpenAPI 3.1 specification metadata and serve an interactive developer API explorer via `Scalar.AspNetCore` at route `/scalar/v1` during development environment runs. The API documentation SHALL support JWT Bearer authentication input, accurately reflect all Minimal API route groupings and status codes, and feature dark-theme styling consistent with TechDaily's Dev-Learning Studio theme. Legacy `/swagger` route requests SHALL be gracefully redirected to `/scalar/v1`.

#### Scenario: Developer accesses interactive API documentation in development
- **WHEN** a developer navigates to `/scalar/v1` in the development environment
- **THEN** the system serves the Scalar API explorer rendered with dark theme
- **AND** all registered Minimal API endpoints, parameter contracts, and RFC 7807 problem detail schemas are listed.

#### Scenario: Developer authorizes API requests via JWT Bearer in Scalar
- **WHEN** a developer provides a valid JWT token in Scalar's security definition dialog
- **THEN** subsequent test requests executed from the Scalar UI include the `Authorization: Bearer <token>` header.

#### Scenario: Legacy Swagger URL is requested
- **WHEN** a user or client requests `/swagger` or `/swagger/index.html`
- **THEN** the server responds with a redirect to `/scalar/v1`.

---

### Requirement: Frontend Composable Utilities & DOM Lifecycle Hygiene
The web frontend SHALL integrate `@vueuse/nuxt` to standardize declarative DOM event listeners, outside-click detection, debounce utilities, asynchronous interval timers, delayed prefetch lookahead timers, and clipboard interactions across Vue components. Components requiring document-level event listeners, outside-click triggers, window resize tracking, global keyboard shortcuts, repeating interval timers, debounced search queries, or delayed lookahead prefetching MUST use VueUse composables (`onClickOutside`, `useEventListener`, `useDebounceFn`, `useIntervalFn`, `useTimeoutFn`, `useClipboard`) rather than manual `document.addEventListener`, `window.addEventListener`, `setInterval`, or `setTimeout` bindings to eliminate memory leaks, timer pollution, and ensure SSR hydration safety. Furthermore, heavy third-party canvas or rendering instances (such as Cytoscape `Core` or Three.js objects) SHALL be wrapped in `shallowRef` rather than deep `ref` to prevent recursive reactive proxying.

#### Scenario: User clicks outside a floating dropdown or modal
- **WHEN** a component utilizing `onClickOutside` (such as `AppSelect.vue` or `QuickHelpModal.vue`) is open and user clicks outside its target boundary
- **THEN** the component state closes smoothly without throwing SSR mismatch warnings or leaving unmanaged event listeners on unmount.

#### Scenario: Keyboard shortcuts and window event listeners detach cleanly
- **WHEN** a component registering window shortcuts via `useEventListener` is unmounted
- **THEN** all associated event listeners are automatically detached by VueUse without manual `onBeforeUnmount` boilerplate.

#### Scenario: Heavy graph canvas instance uses shallowRef
- **WHEN** `pages/graph.vue` initializes and stores the Cytoscape `Core` instance
- **THEN** the reference is stored using `shallowRef` to avoid recursive proxy overhead
- **AND** graph canvas interactions and layout calculations execute without Vue reactivity lag.

#### Scenario: Mindmap drag-to-pan listeners manage lifecycle safely
- **WHEN** user initiates dragging or touch panning in `RoadmapMindmapCanvas.vue`
- **THEN** mouse and touch movement listeners are managed via `useEventListener`
- **AND** all event listeners are cleanly detached upon pan release or component unmount.

#### Scenario: Page dropdown menus utilize onClickOutside for outside-click dismissal
- **WHEN** the slice selection menu in `pages/today.vue` or track menu in `pages/roadmap.vue` is open and user clicks outside
- **THEN** the menu closes cleanly via `onClickOutside` without manual document event listeners.

#### Scenario: Global command palette and graph drawers manage keyboard shortcuts via useEventListener
- **WHEN** `AppCommandPalette.vue` or `GraphDetailDrawer.vue` binds keyboard triggers (such as `Cmd+K` or `Escape`)
- **THEN** the shortcuts are attached using `useEventListener`
- **AND** all event listeners are automatically released upon unmount without manual removal boilerplate.

#### Scenario: Custom select dropdown manages window scroll and resize via useEventListener
- **WHEN** `AppSelect.vue` opens its floating options menu
- **THEN** window `scroll` and `resize` listeners tracking the floating trigger position are managed via declarative `useEventListener`
- **AND** all listeners are automatically released upon dropdown closure or component unmount without manual `removeEventListener` calls.

#### Scenario: 2D graph canvas manages resize responsiveness via useEventListener
- **WHEN** `GraphCanvas.vue` is mounted
- **THEN** window `resize` events triggering canvas relayout are managed via `useEventListener`
- **AND** unmounting the canvas cleanly releases the window resize listener alongside Cytoscape instance destruction.

#### Scenario: Reader pane typography popover and document clicks manage lifecycle cleanly
- **WHEN** `DocReaderPane.vue` opens its typography settings or selection menu
- **THEN** typography popover dismissal uses `onClickOutside` and keydown shortcuts use `useEventListener`
- **AND** all document click and keydown bindings detach automatically upon component unmount.

#### Scenario: Insights feed and review tabs handle navigation shortcuts via useEventListener
- **WHEN** user navigates cards in `pages/insights.vue` or switches tabs in `pages/review.vue` using keyboard navigation
- **THEN** window `keydown` listeners are registered via `useEventListener`
- **AND** all listeners detach automatically upon leaving the respective page routes.

#### Scenario: AI synthesis elapsed progress timer operates via useIntervalFn with automatic cleanup
- **WHEN** `AISynthesisCard.vue` tracks generation progress and timeout status
- **THEN** the 1-second timer interval is managed via `useIntervalFn`
- **AND** the interval automatically pauses or tears down on component unmount without unmanaged `setInterval` loops.

#### Scenario: Search input debouncing across views operates via useDebounceFn
- **WHEN** user types queries into search inputs in `pages/review.vue`, `pages/notes.vue`, or `components/roadmap/RoadmapMindmapCanvas.vue`
- **THEN** debounce delays are managed with `useDebounceFn`
- **AND** pending calls cancel cleanly without manual mutable timeout ID management.

#### Scenario: Code and term clipboard copying provides reactive feedback via useClipboard
- **WHEN** user clicks copy on `ShikiCodeBlock.vue` or `TermExplainerModal.vue`
- **THEN** clipboard writing and temporary copied status are managed via `useClipboard`
- **AND** copied visual state reverts automatically after the configured duration without manual `setTimeout` references.

#### Scenario: Library in-progress books and upload status poll via useIntervalFn with auto-teardown
- **WHEN** `pages/library.vue` initiates polling for PDF processing status or background processing books
- **THEN** repeating polling calls are managed via `useIntervalFn`
- **AND** all intervals automatically terminate upon component destruction without dangling background requests.

#### Scenario: Next-day pacer slice prefetching executes via useTimeoutFn
- **WHEN** `pages/today.vue` schedules prefetching for the next day's reading slice after initial focus load
- **THEN** the 2500ms delay is governed by `useTimeoutFn`
- **AND** pending prefetch timers cancel cleanly upon route changes or locale switching.

#### Scenario: Immersive reader lookahead slice prefetching executes via useTimeoutFn
- **WHEN** `pages/read/[bookId].vue` finishes rendering the active slice and schedules background lookahead prefetching
- **THEN** the 2500ms lookahead delay is governed by `useTimeoutFn`
- **AND** slice navigation immediately cancels pending prefetch calls to prevent redundant network fetches.

#### Scenario: External authentication SDK readiness polling executes via useIntervalFn
- **WHEN** `pages/login.vue` polls for third-party OAuth script readiness
- **THEN** the readiness polling is managed via `useIntervalFn` with bounded retry counts and automatic timer cessation.

### Requirement: Application Shell Navigation & Brand Logo Routing
The application shell topbar (`AppHeader.vue`) SHALL provide intuitive, direct navigation back to the user's primary dashboard overview. Clicking the primary TechDaily brand logo and monogram in the top header SHALL navigate to the root Home Bento Dashboard (`/`) rather than an internal practice subroute.

1. **Brand Identity & Emblem**:
   - The topbar (`AppHeader.vue`) and mobile navigation drawer SHALL render the **Stitch Developer Emblem**:
     - Squircle boundary with dark obsidian gradient (`#151419` to `#08080a`) and subtle violet border stroke.
     - Symmetrical regular hexagon ring with electric violet gradient (`#c4b5fd` to `#5b21b6`).
     - Inner concentric hairline hexagon for architectural precision.
     - Symmetrical left and right code brackets (`< >`) flanking the core.
     - Central glowing memory nucleus node with soft ambient radial halo.
   - The brand title SHALL display "TechDaily" with high-contrast gradient text.

#### Scenario: Brand logo navigation returns user to home dashboard
- **WHEN** a user clicks the TechDaily brand logo in the top application header (`AppHeader.vue`)
- **THEN** the application navigates to the root Home Bento Dashboard (`/`) instead of `/today`.
- **AND** the emblem renders the Stitch Developer Emblem with crisp vector lines.
### Requirement: Modernized Mobile Navigation Drawer & Cross-Device Parity
The mobile slide-out navigation drawer (`AppHeader.vue`) SHALL provide 100% visual and functional parity with the desktop sidebar (`AppSidebar.vue`), adhering to the **Dev-Learning Studio** design tokens, zero-shift active link geometry, unified route structure, and complete localization across all supported locales:

1. **Navigation Structure & Route Completeness**:
   - The mobile drawer SHALL render the identical structured navigation groups as the desktop sidebar:
     - **Practice (`nav.group_practice`)**: Dashboard (`/`, `LayoutGrid`), Today's Focus (`/today`, `Target`), Roadmap (`/roadmap`, `Map`), Quiz (`/quiz`, `HelpCircle`), Spaced Review (`/review`, `Layers`).
     - **Knowledge (`nav.group_knowledge`)**: Insights (`/insights`, `Compass`), Document Library (`/library`, `BookOpen`), Study Notes (`/notes`, `Highlighter`), Knowledge Graph (`/graph`, `Network`).
     - **Account (`nav.group_account`)**: Profile (`/profile`, `User`), Settings (`/settings`, `Settings`).
   - The mobile drawer SHALL NEVER omit the root Dashboard (`/`) route.

2. **Accurate Active Route Matching**:
   - The route active check (`isLinkActive`) SHALL distinguish between `/` and `/today`:
     - When route path is `/`, only the Dashboard link SHALL be marked active.
     - The Today link SHALL NOT be marked active when the user is on the root path `/`.
     - Subroutes for `/today`, `/library` (including `/read`), and `/graph` SHALL remain active when viewing child content.

3. **Design Tokens & Surface Elevation**:
   - The mobile drawer container SHALL use unified theme tokens:
     - Drawer container: `bg-white/95 dark:bg-canvas-subtle/95 backdrop-blur-md` with `border-r border-slate-200/80 dark:border-white/[0.08]`.
     - Drawer overlay: `bg-slate-950/75 backdrop-blur-sm touch-none`.
     - Backdrop and content transitions SHALL use smooth hardware-accelerated animations (`animate-in slide-in-from-left duration-200`).

4. **Brand Header Realignment**:
   - The drawer header SHALL display the canonical TechDaily brand mark with the Deep Iris Violet gradient: `bg-gradient-to-tr from-brand-600 via-brand-500 to-indigo-400` with white `BookOpen` icon (`w-4 h-4 :stroke-width="1.5"`).
   - The drawer brand title SHALL render `TechDaily` with the gradient text styling matching the desktop header, and SHALL NOT use hardcoded `"TechDaily Menu"` English text.
   - The close button (`X`) SHALL provide accessible hover states (`hover:bg-slate-100 dark:hover:bg-canvas-elevated`).

5. **Zero-Shift Active Link Geometry**:
   - Navigation links in the mobile drawer SHALL maintain a constant 2px left border geometry across both active and inactive states:
     - Inactive: `border-l-2 border-transparent text-slate-600 dark:text-slate-400 hover:bg-slate-100/70 dark:hover:bg-white/[0.04] hover:text-slate-900 dark:hover:text-slate-100 font-medium`.
     - Active: `border-l-2 border-brand-500 bg-brand-500/10 dark:bg-white/[0.06] text-brand-600 dark:text-white font-semibold shadow-sm`.
   - Active link icons SHALL use `text-brand-600 dark:text-brand-400`; inactive icons SHALL use `text-slate-400 dark:text-slate-500`.
   - All navigation icons SHALL enforce sleek `:stroke-width="1.5"`.

6. **Localized User Profile Footer Card**:
   - When authenticated, the pinned bottom card SHALL use `bg-slate-100/90 dark:bg-canvas-elevated/80 border border-slate-200/80 dark:border-white/[0.06] rounded-2xl p-3`:
     - User initial avatar in a circular brand container.
     - User display name with truncate and high-contrast text (`text-slate-900 dark:text-white`).
     - Log Out action button localized via `$t('nav.logout')` (e.g. "Đăng xuất" in Vietnamese), with a clean icon or styled action button instead of raw hardcoded English text.
   - When unauthenticated, the bottom card SHALL render a full-width login button localized via `$t('nav.login')`.

7. **Lifecycle & Viewport Hygiene**:
   - Clicking any navigation item or close button SHALL immediately dismiss the mobile drawer and restore body scroll without horizontal shift.
   - Drawer container SHALL respect mobile dynamic viewport and safe area insets: `min-h-[100dvh] pb-[max(1rem,env(safe-area-inset-bottom))]`.

#### Scenario: Mobile drawer displays Dashboard as first item and highlights correctly
- **WHEN** a user on a mobile viewport (< 768px) navigates to `/` and opens the mobile navigation drawer
- **THEN** the first item under `nav.group_practice` is Dashboard (`nav.dashboard`)
- **AND** the Dashboard link is active with `border-l-2 border-brand-500`
- **AND** the Today link is inactive.

#### Scenario: Mobile drawer displays localized brand header and logout button in Vietnamese
- **WHEN** a user with locale set to `vi-VN` opens the mobile drawer
- **THEN** navigation groups render in Vietnamese ("LUYỆN TẬP", "TRI THỨC & GHI NHỚ", "HỆ THỐNG")
- **AND** the brand header renders "TechDaily" with the primary gradient
- **AND** the logout button renders localized text ("Đăng xuất") instead of hardcoded English "Log Out".

### Requirement: Automated VPS Deployment Pipeline & Image Pull Resilience
The automated continuous deployment workflow targeting Google Cloud VPS via SSH SHALL execute container image pulls with bounded retry logic and exponential backoff to ensure resilience against transient network resets, TCP connection drops, and registry CDN rate limits. The deployment script SHALL enforce that all deployed application containers run strictly from pre-built registry images (`--no-build`) and prohibit local compilation or image building on the production server. Following container recreation, the pipeline SHALL restart the Nginx reverse proxy service to clear cached upstream IP resolutions and prevent stale DNS 502 Bad Gateway responses.

#### Scenario: Image pull encounters transient TCP connection reset or CDN network blip
- **WHEN** the deployment workflow executes `docker compose pull` on the VPS and encounters a network reset or registry connection drop
- **THEN** the script logs the failure attempt counter and pauses for a designated backoff interval (5 seconds)
- **AND** retries pulling the container images up to 5 attempts before failing the deployment
- **AND** if any retry attempt succeeds within the limit, the pipeline advances to container recreation.

#### Scenario: Application containers launch strictly from pre-built GHCR images
- **WHEN** the deployment workflow executes `docker compose up -d` on the VPS
- **THEN** the command includes `--no-build`, `--force-recreate`, and `--remove-orphans`
- **AND** Docker Compose launches exclusively from the pre-pulled images without attempting source builds or consuming host CPU/memory for compilation.

#### Scenario: Nginx upstream container IP resolution is refreshed upon deployment
- **WHEN** production application containers are successfully recreated and launched
- **THEN** the deployment workflow explicitly executes `docker compose restart nginx`
- **AND** Nginx re-resolves internal bridge network DNS mappings for the backend and frontend services without routing requests to stale container IP addresses.

### Requirement: Shared UI Primitives Responsive Geometry and Event Hygiene
All shared foundation UI primitives (`frontend/components/common/`, `frontend/components/app/`, `app.vue`, and `error.vue`) SHALL conform to strict mobile viewport responsiveness down to $320\text{px}$, zero horizontal overflow, VueUse declarative event lifecycle management, and clean engineering iconography.

#### Scenario: Dropdown and Time Picker Popovers on Narrow Mobile Viewports
- **WHEN** user activates `AppSelect` or `AppTimePicker` on a narrow mobile viewport ($320\text{px}$ to $375\text{px}$)
- **THEN** the popover menu or dropdown modal SHALL clamp within the visible viewport bounds without horizontal scrolling or clipping
- **AND** all clickable items SHALL provide a touch-target size of at least $44\text{px} \times 44\text{px}$.

#### Scenario: Mobile Dynamic Viewport Shell Adaptation
- **WHEN** user navigates any application route on a mobile device with dynamic address bars
- **THEN** the root layout shell in `app.vue` and `error.vue` SHALL utilize dynamic viewport units (`min-h-dvh`) to prevent layout jumpiness upon browser chrome collapse.

#### Scenario: Keyboard and Outside-Click Hygiene in Primitives
- **WHEN** floating primitives (`AppSelect`, `AppTimePicker`, `AppCommandPalette`) are opened or closed
- **THEN** document listeners for keyboard navigation (`Escape`, `ArrowUp`, `ArrowDown`, `Enter`) and backdrop dismissal SHALL be handled via VueUse composables (`useEventListener`, `onClickOutside`) with automatic teardown upon component unmount.

### Requirement: Executive Bento Profile and Settings Mobile Responsive Standards
The Engineer Portfolio Profile (`pages/profile.vue`), System Settings (`pages/settings.vue`), and Authentication (`pages/login.vue`) surfaces SHALL render with responsive Bento geometry down to $320\text{px}$, responsive 2x2 daily study pace chips, and touch-accessible notification scheduling controls.

#### Scenario: Executive Bento Profile on 320px Viewports
- **WHEN** user views `pages/profile.vue` on a narrow mobile viewport ($320\text{px}$ to $375\text{px}$)
- **THEN** Tier 1 Passport badges (Role, Google Linked, Streak Trophy) SHALL wrap cleanly without clipping
- **AND** Tier 2 Milestones telemetry cells SHALL render in a balanced 2-column grid (`grid-cols-2 lg:grid-cols-4`) without label truncation.

#### Scenario: Responsive Daily Goal Pace Selector
- **WHEN** user selects daily study pace ("5m", "10m", "15m", "30m") in Account Settings
- **THEN** the selection chips SHALL wrap into `grid-cols-2 sm:grid-cols-4 gap-2` on mobile screens to ensure touch targets remain $\ge 44\text{px}$ without horizontal text squishing.

#### Scenario: Mobile Settings Web Push and Timezone Controls
- **WHEN** user configures notification schedules or timezone preferences in `pages/settings.vue` on a mobile device
- **THEN** time picker popovers and timezone dropdowns SHALL clamp within viewport boundaries
- **AND** the Brave push setup guidance card SHALL adapt responsively without table or code block clipping.

### Requirement: Dev-Learning Studio Visual Language & Responsive Density
The web frontend SHALL implement the Dev-Learning Studio visual language with platform-independent visual density and uniform typography metrics:
1. **Font Metrics Parity**: The application SHALL load the `Inter` webfont across all supported browsers and platforms, preventing font metric discrepancies between operating systems (such as wider `Segoe UI` tracking on Windows versus condensed `Ubuntu` on Linux).
2. **Decoupled Typography Hierarchy**:
   - **Reading Prose Scale**: Long-form curriculum document text and article paragraphs SHALL use `text-base md:text-lg` (16px–18px) with `leading-relaxed` for reading ergonomics.
   - **Interactive Control Scale**: Interactive elements, scenario options, form inputs, buttons, card headers, and UI widgets SHALL use `text-sm md:text-base` (14px–16px), strictly preventing `text-lg` from bloating interactive controls.
3. **Card & Surface Spacing Tokens**: Standard card surfaces (`.glass-card`, `.glass-panel`) SHALL use balanced padding (`p-4 sm:p-5`) with standard radius (`rounded-2xl`), eliminating disproportionate padding (`p-7`, `p-8`, `md:p-10`).
4. **Natural Viewport Flow**: Primary dashboard and studio views SHALL utilize natural vertical scrolling (`min-h-[calc(100dvh-3.5rem)]`) and SHALL NOT lock container height with `overflow-hidden` on desktop displays, ensuring all widgets remain accessible when viewports are constrained by browser chrome and taskbars.

#### Scenario: Inter font loaded uniformly across operating systems
- **WHEN** a user accesses TechDaily from any desktop operating system (Windows 11, Linux, macOS)
- **THEN** the browser loads and renders the `Inter` webfont family with consistent letter spacing, character width, and x-height metrics.

#### Scenario: Interactive controls adhere to UI typography scale
- **WHEN** viewing interactive controls, option choices, buttons, and form inputs on desktop viewports
- **THEN** text is styled between `text-sm` (14px) and `text-base` (16px) rather than scaling up to `text-lg` (18px).

#### Scenario: Dashboard widgets remain accessible on constrained desktop viewports
- **WHEN** the dashboard is viewed in a browser with bookmarks bar and OS taskbar visible (available height $\le 860\text{px}$)
- **THEN** all Bento cards (including knowledge constellation) are reachable via smooth vertical scrolling without overflow clipping.

### Requirement: Engineering Cockpit Visual Density Standards
The web frontend SHALL standardize on an Engineering Cockpit visual density baseline (Density 8/10) across all application pages, ensuring compact, high-efficiency information layout on desktop viewports:
1. **Interactive Control Heights**: Buttons, inputs, search boxes, and dropdown selects SHALL standardize to `h-9` (36px height) with `px-3.5 text-sm`, eliminating oversized touch targets (`py-3.5`, `rounded-2xl`) on desktop screens.
2. **Concentric Border Radii**: Cards and containers SHALL maintain concentric radius geometry ($R_{\text{outer}} = R_{\text{inner}} + \text{padding}$). Outer cards SHALL use `rounded-xl` (12px), while nested controls and option buttons SHALL use `rounded-lg` (8px) or `rounded-md` (6px). Large `rounded-3xl` radii that force excessive internal padding are prohibited on data cards.
3. **Card & Container Spacing Scale**: Base card padding (`.glass-card`) SHALL standardize to `p-3.5` (14px) to `p-4.5` (18px). Page outer containers SHALL standardize to `py-4 sm:py-5 px-4 sm:px-6`, eliminating `p-8` to `p-10` dead margins.
4. **Desktop Fold Fit Constraint**: Standard desktop pages and cards SHALL fit key data and actions within an available vertical viewport height of $700\text{px}–850\text{px}$ (simulating 1080p desktop with 125% DPI scaling, taskbar, and browser chrome) without unintentional vertical clipping.

#### Scenario: User navigates pages on 1080p desktop with 125% DPI
- **WHEN** user views `/settings`, `/profile`, or `/insights` on a 1080p display with 125% DPI display scaling
- **THEN** page containers render with compact padding (`py-4 sm:py-5 px-4 sm:px-6`)
- **AND** cards render with compact padding (`p-3.5` to `p-4.5`)
- **AND** interactive buttons and inputs render at `h-9` (36px) height.

### Requirement: 3-Tier Production Modal Shell
All modal dialogs across the platform SHALL adhere to a 3-tier layout architecture capped at `max-h-[85vh]`:
1. **Fixed Header**: Pinned at the top (`shrink-0`) containing the modal title and close button.
2. **Scrollable Body**: Constrained to `max-h-[60vh]` with `overflow-y-auto` and `scrollbar-gutter: stable`, allowing long forms to scroll independently.
3. **Sticky Footer**: Pinned at the bottom (`shrink-0`) containing submission and cancellation action buttons. Action buttons SHALL remain permanently visible above the screen fold at all times regardless of content height.

#### Scenario: User opens modal with lengthy form content
- **WHEN** user opens a modal dialog containing multiple inputs, file upload zones, or explanatory guidelines
- **THEN** the modal header and modal footer remain fixed in place
- **AND** the submit and cancel buttons in the footer are immediately visible without requiring internal scrolling
- **AND** only the central form body scrolls when content exceeds `60vh`.

### Requirement: Interactive Design System Showcase
The platform SHALL maintain an interactive living design system showcase at `/showcase` exclusively in development environments (`NODE_ENV !== 'production'`) displaying:
1. Base UI Primitives (Buttons, inputs with ⌘K badge, tags, segmented switcher)
2. Interactive Multiple-Choice Option Cards with active/correct/incorrect states
3. Production Modal Shell with sticky actions
4. Bento Metric Cards with tabular numerals
5. Code Snippet Block with clipboard copy feedback
6. Skeleton Loading Shimmers
7. Empty and Error State Cards
8. Reader Floating Selection Toolbar

The frontend build pipeline (`nuxt.config.ts`) and navigation shell (`useNavigationMenu.ts`) SHALL enforce that `/showcase` and `/playground` routes are strictly isolated to development environments:
1. **Build-Time Route Pruning**:
   - In production builds (`process.env.NODE_ENV === 'production'`), `/showcase` and `/playground` routes SHALL be stripped during build time via the Nuxt `pages:extend` hook, ensuring zero JavaScript chunks or client-side route manifest entries are emitted into production artifacts.
2. **Environment-Gated Navigation Menu**:
   - The application navigation composable (`useNavigationMenu.ts`) SHALL only include `{ name: 'nav.showcase', path: '/showcase', icon: Palette }` when running in local development mode (`import.meta.dev`), omitting it from the sidebar in production builds.
3. **Route Defense & Zero Metadata Leakage**:
   - Direct URL requests to `/showcase` or `/playground` on production deployments SHALL return standard 404 Not Found status without leaking internal design system components, source maps, or prototype state.

#### Scenario: Developer or Agent inspects design system showcase
- **WHEN** user navigates to `/showcase` in a local development environment
- **THEN** the page renders all 8 component showcases in both light and dark modes
- **AND** interactive demo states (selection, modal open, copy feedback) function seamlessly.

#### Scenario: User attempts to access design system showcase in production
- **WHEN** a user or crawler accesses `/showcase` or `/playground` on a production deployment
- **THEN** the system returns a standard 404 Not Found error
- **AND** client-side DevTools, route tables, and source maps contain zero references to showcase or playground components.

---

### Requirement: Accessible Custom Select Dropdown Invariant
All dropdown selection controls across the frontend application and component showcases SHALL use custom accessible dropdown components (`AppSelect.vue`) rather than unstyled native HTML `<select><option>` elements.
1. **Styling & Theme Integrity**:
   - The dropdown trigger button and floating options listbox SHALL adhere to the Dev-Learning Studio theme (`dark:bg-canvas-elevated`, `dark:border-white/[0.08]`, `dark:text-slate-200`).
   - The dropdown listbox SHALL NOT display native operating system selection highlights (such as default blue Windows highlight `#0078d7` or unstyled browser option boxes).
2. **Keyboard Accessibility**:
   - The custom select component SHALL support standard WAI-ARIA combobox/listbox navigation: `Enter` or `Space` to toggle, `Up` / `Down` arrow keys to highlight options, `Escape` to dismiss, and `Enter` to commit selection.

#### Scenario: Interacting with Select Dropdowns on Windows
- **WHEN** an engineer opens a select dropdown on Windows 11
- **THEN** the options menu displays as a themed dark obsidian floating panel with brand-tinted active/hover states, with zero native OS unstyled option rendering.

---

### Requirement: Living Design System Layout Archetypes Showcase
The interactive design system showcase at `/showcase` (`frontend/pages/showcase.vue`) SHALL include Section 09: **"System Layout Archetypes"** (`LayoutArchetypesShowcase.vue`).
1. **Interactive Layout Demos**:
   - The showcase section SHALL provide interactive tabs to demonstrate each of the three layout archetypes:
     - **Tab 1: Flashcards Studio Demo**: Demonstrates `StudioLayout` with sample flashcard, telemetry dock, and hotkey cheatsheet.
     - **Tab 2: Settings Master-Detail Demo**: Demonstrates `MasterDetailLayout` with left sub-nav and right configuration panels using custom `AppSelect` controls.
     - **Tab 3: Notes Board Demo**: Demonstrates `BoardLayout` with sticky search/filter toolbar and responsive card grid.
2. **Full Primitive Parity**:
   - The showcase Primitives section (`PrimitivesShowcase.vue`) SHALL use `AppSelect.vue` for all dropdown controls, confirming elimination of raw `<select>` elements.

#### Scenario: Viewing Layout Archetypes in Showcase
- **WHEN** a developer navigates to `/showcase` and selects the "09. System Layout Archetypes" section
- **THEN** interactive previews of `StudioLayout`, `MasterDetailLayout`, and `BoardLayout` are rendered with realistic mock data and responsive layout controls.

---

### Requirement: Unified Account & Settings Master-Detail Architecture
The Settings interface (`frontend/pages/settings.vue`) SHALL serve as the unified Master-Detail Hub for all account identity, credentials, learning telemetry, interface preferences, notifications, and scheduling, consolidating previously separate `/profile` and `/settings` surfaces into a single comprehensive layout using `MasterDetailLayout.vue`.
1. **Consolidated Category Navigation Rail (`#nav`)**:
   - Pinned on the left (`w-full md:w-64 shrink-0`) featuring category icons, titles, and active pills for 3 consolidated sections:
     - `general` (**General & Profile**): Developer identity card (avatar initials, name, email, level badge), 2-column form grid for difficulty track, daily goal, timezone, theme, full name, and interface language, followed by auto-advance toggle card and contextual "Save changes" submit action.
     - `notifications` (**Web Push Notifications**): Browser push toggle switch, active endpoint status banner, test push trigger, and Study & Alert schedule time pickers with dedicated "Save Schedule Preferences" action.
     - `security` (**Security & Password**): Account password change form with real-time strength bar, Google OAuth connection status, and dedicated "Update Password" action.
   - Navigation rail buttons SHALL maintain clean typography without persistent numeric badge counters.
2. **Contextual Per-Tab Action Invariant**:
   - The `#header` template of `MasterDetailLayout` SHALL display only the clean section icon and title, without global save buttons.
   - Each tab SHALL own its dedicated save action button positioned at the bottom right of its respective form content panel.
3. **Comprehensive Bilingual i18n Invariant**:
   - All form controls, select dropdowns, options, placeholders, status callouts, and action buttons SHALL be 100% localized in English and Vietnamese.

#### Scenario: Accessing Unified Settings on Desktop
- **WHEN** an authenticated user opens `/settings` on a desktop browser
- **THEN** the left rail displays the 3 consolidated configuration categories without unread badge noise and the active tab displays its controls and dedicated save button.

---

### Requirement: Zero-Flicker Tab Navigation Invariant
All navigation rail tab buttons in `MasterDetailLayout` and interactive tab switchers SHALL enforce a constant 1px border baseline (`border border-transparent` in inactive state, `border-brand-500/20` in active state) and scoped `transition-colors` rather than `transition-all`.
1. **Layout Shift Elimination**:
   - Tab switching SHALL NOT cause 1px box-sizing height/width jumps or border flashing.
2. **Visual Consistency**:
   - Inactive buttons maintain consistent padding and alignment with active pill buttons.

#### Scenario: Switching Tabs in Master-Detail Settings
- **WHEN** a user clicks between navigation rail tabs in `/settings`
- **THEN** the active tab updates smoothly with zero border flashing, zero layout jumping, and immediate visual feedback.

---

### Requirement: Deep-Linked Tab Synchronization & Profile Route Redirection
The Settings interface SHALL support deep-linking and state preservation via URL search parameters, and existing `/profile` routes SHALL seamlessly redirect to the unified settings view.
1. **URL Query Synchronization**:
   - The active tab SHALL synchronize with `route.query.tab` (e.g. `/settings?tab=security`).
   - Clicking a rail tab updates the URL query without triggering full page reloads.
2. **Profile Route Redirection**:
   - Navigating to `/profile` SHALL immediately redirect to `/settings?tab=profile`, preserving backward compatibility for bookmarks and cached links.
3. **Application Shell Integration**:
   - The topbar user avatar chip (`AppHeader.vue`) SHALL link directly to `/settings?tab=profile`.
   - The sidebar navigation menu (`useNavigationMenu.ts`) under `nav.group_account` SHALL consolidate the separate Profile link into a unified "Settings & Profile" destination.

#### Scenario: Navigating from Legacy Profile Link
- **WHEN** a user navigates to `/profile` or clicks their user avatar in `AppHeader.vue`
- **THEN** the browser lands on `/settings?tab=profile` with the Profile & Identity tab actively selected.

### Requirement: Executive Cockpit Bento Dashboard Layout Integration
The primary root route `/` (`HomeBentoDashboard.vue`) SHALL implement the `BentoDashboardLayout` archetype (`BentoDashboardLayout.vue`), decoupling layout shell geometry from individual card content and providing distinct, uncluttered action pathways for continuous reading and daily practice routines.

1. **Header Slot (`#header`)**:
   - Houses the Welcome & Orientation Banner, displaying the personalized engineer greeting, role target, active reading slice badge, and streak status.

2. **Action Stage Slot (`#action-stage`)**:
   - **Card A (Active Reading Slice Hero)**:
     - Displays the user's ongoing book reading progress: book title, active slice title, summary, estimated read time, and progress bar with percentage.
     - Provides a prominent "Continue Reading →" ("Đọc Tiếp →") primary CTA button that navigates directly to the dedicated Library Reader at that slice (`/read/${bookId}?slice=${currentChunkOrder}`).
   - **Card B (Today's Practice Session Cockpit)**:
     - Serves as the primary entry point for the user's scheduled daily curriculum session (`/today`).
     - **Header & Badges**: Displays the session emblem (`Target` / `CalendarDays`), uppercase category pill "TODAY'S PRACTICE" ("LUYỆN TẬP HÔM NAY"), and curriculum progress badge ("Day {dayOrder} / 30" / "Lộ trình Ngày {dayOrder} / 30").
     - **Status Indicator**: Displays real-time drill status:
       - *Pending*: Subtle pending indicator ("Ready to practice" / "Sẵn sàng luyện tập" or "+10 Points Available").
       - *Submitted / Completed*: Completed badge with score ("Completed: {score}/10" or "Đã hoàn thành").
     - **Session Focus & Itinerary**: Renders the daily curriculum topic title (`topic?.title`) and a structured itinerary breakdown indicating the dual daily components (1 In-Depth Concept Reading + 1 Architectural Scenario Drill).
     - **Action CTA**: Displays a high-contrast action button "Start Today's Practice →" ("Vào Luyện Tập →" / "Bắt Đầu Bài Hôm Nay") that navigates directly to the Daily Focus session (`/today`).

3. **Telemetry Dock Slot (`#telemetry-dock`)**:
   - Houses Card C (7-day Consistency Heatmap and SM-2 Due Count) and Card D (Domain Knowledge Constellation Card), equalizing total vertical height with the action stage.

#### Scenario: Navigating Home Dashboard on 1080p Desktop
- **WHEN** an engineer loads the root page `/` on a 1920x1080 desktop browser
- **THEN** the entire Bento Grid renders with cohesive spacing and equalized column heights, eliminating empty internal margins within Card A and Card B.

#### Scenario: User navigates to Today's Practice from Home Dashboard
- **WHEN** an authenticated user clicks the "Start Today's Practice" ("Vào Luyện Tập") CTA button on Card B of the Home Bento Dashboard
- **THEN** the browser navigates directly to `/today`
- **AND** the Daily Focus Cockpit opens with today's reading slice and interview challenge ready for practice.

#### Scenario: User distinguishes between Continue Reading and Today's Practice
- **WHEN** an authenticated user views the Home Bento Dashboard
- **THEN** Card A clearly identifies the active document slice with action "Continue Reading" ("Đọc Tiếp") targeting `/read/${bookId}`
- **AND** Card B clearly identifies the scheduled daily curriculum session with action "Start Today's Practice" ("Vào Luyện Tập") targeting `/today`
- **AND** neither card presents ambiguous or duplicate routing destinations.

#### Scenario: Today's Practice Card reflects daily drill completion state
- **GIVEN** an authenticated user who has already submitted today's architectural drill (`drill.status === 'Submitted'`)
- **WHEN** the user views Card B on the Home Bento Dashboard
- **THEN** Card B displays the completed status badge with earned score
- **AND** the action button indicates "Review Practice" ("Xem Lại Buổi Học") while still routing to `/today`.
---

### Requirement: Settings Master-Detail Desktop Layout Standard
The Settings interface (`frontend/pages/settings.vue`) SHALL implement the `MasterDetailLayout` archetype (`MasterDetailLayout.vue`), replacing the narrow single-column layout with a standard desktop master-detail architecture.
1. **Category Navigation Rail (`#nav`)**:
   - Pinned on the left (`w-full md:w-64 shrink-0`) featuring section icons, category titles (General & Preferences, Push Notifications, Security), and active state pills.
2. **Settings Content Panel (`#content`)**:
   - Expands to fill available width (`flex-1 min-w-0`), organizing form controls into responsive 2-column grids (`grid sm:grid-cols-2 gap-4`) using `AppSelect.vue` for all selection inputs.
   - Eliminates $> 800\text{px}$ dead margins on desktop displays.

#### Scenario: Configuring Settings on Desktop
- **WHEN** an engineer accesses `/settings` on a desktop viewport ($\ge 1280\text{px}$)
- **THEN** the left rail displays configuration categories, the right panel displays the active settings form in a 2-column grid, and no empty side voids surround the interface.

---

### Requirement: Developer & Agent UI Design Governance Protocol
The project repository SHALL mandate strict engineering governance rules codified in `AGENTS.md`, organized into five foundational pillars:
1. **Pillar 1: Production Security & Auth Boundaries**:
   - Zero fake or default fallback users. Endpoints requiring auth must enforce `.RequireAuthorization()` and return `401 Unauthorized` when no valid JWT is present.
   - Zero development 1-click bypasses or local-dev banners in production code.
2. **Pillar 2: UI Design System & Component Governance**:
   - Mandatory System Layout Archetypes (`StudioLayout`, `BentoDashboardLayout`, `MasterDetailLayout`, `BoardLayout`). Unconstrained ad-hoc wrapper divs causing empty black voids on 1080p desktop viewports are strictly prohibited.
   - Strict prohibition of raw native HTML `<select>` (MUST use `AppSelect.vue`) and native browser dialogs (`alert`, `confirm`, `prompt`).
   - Responsive typography standards ($\ge 14\text{px}$ on mobile, $\ge 16\text{px}$ on desktop/tablet) and bilingual layout protection (`whitespace-nowrap shrink-0` across English and Vietnamese).
   - Mandatory Vue playground protocol (`frontend/pages/playground/<feature>.vue`) for UI previews, completely prohibiting isolated static HTML files.
3. **Pillar 3: Frontend Testing Boundaries & Visual Inspection**:
   - Automated Vitest unit tests MUST strictly defend data contracts, form serialization payloads, validation barriers, auth state, and route guards.
   - Vitest tests MUST NOT assert CSS/Tailwind classes to evaluate layout geometry.
   - Visual layout, responsiveness, and spacing MUST be verified through direct screenshot inspection.
4. **Pillar 4: AI Engine & External Ingestion**:
   - High-speed model selection (`gemini-3.5-flash-lite`, <5s latency, $\ge 120\text{s}$ proxy timeout) with user-triggered generation.
   - Balanced bracket depth scanning for LLM JSON outputs.
   - Canonical URL resolution and clean markdown extraction for web crawlers.
5. **Pillar 5: Verification, DevOps & Skills Protocol**:
   - Local-first verification before git push (`dotnet test`, `npm test`).
   - Nginx container upstream cache restart and modern ED25519 SSH keys.
   - Mandatory pre-flight reading of canonical skill documentation in `.agents/skills/`.

#### Scenario: Agent Implements a New View
- **WHEN** an AI agent or developer is instructed to create or refactor a frontend view
- **THEN** the agent selects an established layout archetype, verifies dropdowns use `AppSelect.vue`, and prototypes in `frontend/pages/playground/` with visual screenshot proof before touching production routes.

#### Scenario: Agent implements a new feature or refactor
- **WHEN** an AI agent or developer is instructed to create or refactor frontend or backend code
- **THEN** the agent adheres to the 5 Core Engineering Pillars in `AGENTS.md`
- **AND** the agent executes local verification before committing or pushing changes.
---

### Requirement: Frontend Dev Playground UI Previews
The system SHALL support integrated frontend UI prototyping and previewing through dedicated development playground pages located under `frontend/pages/playground/*.vue`. Playground pages MUST utilize local reactive mock data, share the project's canonical Tailwind CSS and Vite asset pipeline, and require no authentication during local development.

#### Scenario: Reviewing prospective UI in local development
- **WHEN** a developer or reviewer navigates to `http://localhost:3000/playground/<feature>`
- **THEN** the playground page MUST render with full design system typography (including `JetBrains Mono` and `Inter`), authentic layout archetypes (`BoardLayout`, `StudioLayout`), and reactive mock data without requiring a user login or backend API connectivity.

#### Scenario: Exclusion from production builds
- **WHEN** the frontend application is compiled for production deployment (`NODE_ENV === 'production'`)
- **THEN** all routes matching `/playground` and `/showcase` MUST be automatically stripped by Nuxt page generation hooks and excluded from the production distribution.

---

### Requirement: Prohibition of Disconnected Static Preview HTML Files
Developers and AI agents MUST NOT create disconnected, standalone `.html` files utilizing third-party CDN stylesheets (such as Tailwind CDN or external unbundled fonts) for UI previews. All UI evaluations and user design reviews MUST be performed directly within the Vue playground environment to guarantee 100% visual parity with production components.

#### Scenario: Prototyping a prospective UI phase
- **WHEN** preparing a visual preview for user design review
- **THEN** the preview MUST be created as a Vue Single File Component under `frontend/pages/playground/` consuming project component primitives rather than an isolated HTML file.

---

### Requirement: Native Time Input Conforming to Studio Design System
The custom timepicker component (`AppTimePicker.vue`) SHALL render as an in-place native time input (`<input type="time">`) styled according to TechDaily form control standards, eliminating dropdown popovers and duplicate time displays.

1. **In-Place Input Presentation**:
   - The component SHALL render directly in the form layout without triggering floating popovers, dropdowns, or modal dialogs.
   - The input SHALL display the time in 24-hour (`HH:mm`) format internally, allowing the user's browser/OS locale to format the presentation (e.g. 12h AM/PM on US/VN systems).
   - An inset `Clock` icon SHALL be rendered inside the left edge of the input container.

2. **Form Interaction & Events**:
   - The component SHALL accept `modelValue: string | null | undefined` and safely sanitize input strings (e.g. `08:00:00` or `08:00`).
   - Editing the time SHALL emit `update:modelValue` and `change` with the updated 24-hour string format (`HH:mm`).
   - The component SHALL support a `disabled` property that renders the input in an inactive, non-interactive state.

3. **Styling & Theme Uniformity**:
   - The input container SHALL adhere to the standard TechDaily input dimensions: `h-11`, `rounded-xl`, `border-slate-200/90 dark:border-white/[0.08]`, `bg-white dark:bg-canvas-subtle`.
   - The native calendar/picker indicator icon SHALL remain clickable and adapt cleanly to Dark Mode via invert filter.

#### Scenario: User changes time via native control
- **WHEN** user selects or types a new time `20:00` into the native time input
- **THEN** the component emits `update:modelValue` with `20:00`
- **AND** the input value reflects the update immediately in-place.

#### Scenario: User navigates on mobile device
- **WHEN** user taps the time input on a touch screen
- **THEN** the device's native time picker interface appears
- **AND** confirming the selection updates the model value with zero layout shifting.

---

### Requirement: Frontend Testing Boundaries & Visual Inspection Standard
The web frontend automated test suite (`Vitest` + `Happy-DOM`) and verification workflow SHALL enforce a strict separation between behavioral data contracts and visual layout verification, actively purging test theater assertions and mandating direct headless browser screenshot inspection:

1. **Prohibition of CSS Class Assertions in Vitest**:
   - Unit tests SHALL NOT assert the presence, absence, or modification of Tailwind CSS utility classes (e.g. `classes().toContain('max-w-5xl')`, `classes().toContain('hidden')`, `classes().toContain('lg:grid-cols-12')`, `classes().toContain('flex')`, `classes().toContain('shrink-0')`, `classes().toContain('whitespace-nowrap')`) under the premise of verifying visual presentation or layout integrity.
   - All tests in the frontend test suite asserting styling classes under the pretense of UI layout verification SHALL be pruned or replaced with behavioral state assertions.

2. **Permitted Scope for Automated Unit Testing (Vitest)**:
   - **Form Serialization & Data Contracts**: Verify that user input (names, emails, passwords, numerical values) is correctly parsed and dispatched in the exact required payload schema to backend endpoints or stores.
   - **Form Validation & Constraint Enforcement**: Verify that invalid inputs (missing required fields, passwords < 8 characters, mismatched passwords) produce validation errors and prevent submission.
   - **Authentication State & Route Middleware**: Verify that unauthenticated requests redirect to `/login?redirect=...`, and that token expiration triggers proper cleanup.
   - **Asynchronous Lifecycle & Error Feedback**: Verify loading indicators (`isSubmitting`), disabled button states during async requests, and RFC 7807 error problem details.

3. **Mandatory Automated Browser Visual Verification Protocol**:
   - Visual correctness (alignment, padding, typography, element collisions, and text wrapping across English and Vietnamese) SHALL be verified exclusively through direct headless browser rendering and screenshot previews.
   - For all frontend UI modifications (pages, layouts, components), the agent or developer SHALL execute automated browser inspection before marking tasks complete:
     - Open headless Chromium via the `browser` device.
     - Inject authentic user session cookies/tokens (`techdaily_token`, `techdaily_user`) when navigating authenticated views.
     - Capture visual screenshots at both **Desktop (1440x900 or 1920x1080)** and **Mobile (390x844)** viewports.
     - Present the captured screenshots directly in the verification output.

4. **Dual-Gate Verification Invariant**:
   - Marking a frontend UI task complete (`- [x]`) in planning tasks or declaring "Implementation Complete" SHALL strictly require passing both verification gates:
     - **Gate 1 (Behavioral Data Contract):** Vitest test suite (`npm test`) passes with zero regression.
     - **Gate 2 (Visual Integrity Gate):** Headless browser screenshots captured and verified across both desktop and mobile viewports.
   - Declaring a UI task complete based solely on Gate 1 without Gate 2 is strictly prohibited.

#### Scenario: Dual-Gate verification requirement for frontend UI changes
- **WHEN** an agent completes coding changes to a Vue page, component, or layout
- **THEN** the agent executes `npm test` in `frontend/` to satisfy Gate 1 (Behavioral Data Contract)
- **AND** the agent executes automated browser screenshot capture for Desktop and Mobile viewports to satisfy Gate 2 (Visual Integrity)
- **AND** the task is only marked complete (`- [x]`) after both gates have succeeded.

#### Scenario: Agent captures automated browser screenshots across Desktop and Mobile viewports
- **WHEN** an agent performs visual verification on an updated route (e.g. `/notes`)
- **THEN** the agent launches headless Chromium, navigates to the route (injecting auth cookies if required), and takes screenshots at 1440x900 (Desktop) and 390x844 (Mobile)
- **AND** inspects the resulting images for text wrapping, layout shift, and element collision across locales before yielding to the user.

#### Scenario: Rejection of premature UI task sign-off without visual inspection
- **WHEN** an agent passes all Vitest unit tests but has not captured browser screenshots for modified UI surfaces
- **THEN** the agent SHALL NOT mark the visual verification task complete
- **AND** SHALL NOT yield "Implementation Complete" until the automated visual inspection is executed and presented.
---

### Requirement: Universal Scalable Vector Favicon
The application SHALL serve a high-fidelity scalable vector favicon (`/favicon.svg`) derived from the Stitch Developer Emblem to provide immediate brand recognition across browser tabs, bookmarks, and mobile PWA home screens.

1. **Vector Geometry & Legibility**:
   - The favicon SHALL use SVG vector definitions optimized for multi-resolution rendering (16x16, 32x32, 48x48, and 512x512).
   - The graphic elements (brackets, hexagon, glowing node) SHALL maintain high contrast against both dark and light browser chrome backgrounds.

#### Scenario: Browser loads site favicon
- **WHEN** any page of TechDaily is loaded in a web browser
- **THEN** the browser tab displays the Stitch Developer Emblem favicon (`/favicon.svg`).
- **AND** the icon is crisp and clearly identifiable on both dark and light browser tab bars.

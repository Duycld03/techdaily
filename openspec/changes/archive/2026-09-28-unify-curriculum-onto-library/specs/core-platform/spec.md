# Core Platform Specification

## MODIFIED Requirements

### Requirement: Senior Scenario Interview Challenge
The system SHALL present a daily scenario interview challenge aligned with the user's active reading slice (`DocumentChunk`) with instant grading, architectural feedback, and score evaluation. The challenge SHALL derive from the active book slice the user is currently pacing through, never from a fixed curriculum topic.

#### Scenario: User completes daily interview scenario
- **WHEN** user submits answer to `POST /api/v1/daily/drill/submit`
- **THEN** system records drill submission, evaluates answer, and returns score with architectural explanation.

#### Scenario: Challenge sourced from active reading slice
- **WHEN** the system presents the daily scenario challenge for a user with an active `DocumentBook`
- **THEN** the challenge is grounded in the user's active `DocumentChunk` slice
- **AND** no fixed-curriculum topic supplies the challenge content.

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
   - *Right Column (Domain Mastery Goal Tracker)*: Houses `DomainGoalTracker.vue` (~380px height), presenting the user's library reading and recall coverage across four universal, framework-agnostic core engineering pillars with compact padding and high-contrast gradient tracks:
     - **Pillar 1: Backend Runtime & Concurrency** (`profile.domain_backend_runtime`: "Nền Tảng Backend & Runtime" / "Backend Runtime & Concurrency").
     - **Pillar 2: Data Storage & Persistence** (`profile.domain_data_storage`: "Hệ Lưu Trữ & Cơ Sở Dữ Liệu" / "Data Storage & Persistence").
     - **Pillar 3: Distributed Systems & Architecture** (`profile.domain_system_design`: "Hệ Thống Phân Tán & Thiết Kế" / "Distributed Systems & Architecture").
     - **Pillar 4: Frontend & Browser Engineering** (`profile.domain_frontend`: "Hiệu Năng Frontend & Trình Duyệt" / "Frontend & Browser Engineering").

The Domain Mastery Goal Tracker component SHALL evaluate and aggregate mastery dynamically from the user's library reading and recall activity — the categories of the `DocumentBook`s the user reads, drills, and reviews — via `matchCategory(category: string)`, ensuring book chapters, drills, and quiz attempts in any modern stack map seamlessly into the appropriate universal pillar.

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
  - Interview Quiz accuracy percentage and mastered concepts count with non-truncated concise title ("Độ chính xác Quiz" / "Quiz Accuracy")
  - Spaced Repetition Memory Vault active card count
  - Architecture Highlights saved count
- **AND** all 4 cells render in individual `.glass-card` surfaces with hairline borders and distinct semantic accent badges.

#### Scenario: User monitors curriculum domain mastery goal progress
- **WHEN** user views the Domain Mastery Goal Tracker section on `/profile`
- **THEN** UI displays categorized visual progress bars for each of the 4 universal engineering pillars with compact vertical padding:
  - Backend Runtime & Concurrency (aggregating .NET, Node.js, Go, Java, Python runtime documents)
  - Data Storage & Persistence (aggregating PostgreSQL, MongoDB, Redis, MySQL, ACID, indexing documents)
  - Distributed Systems & Architecture (aggregating microservices, Kafka, outbox, system design documents)
  - Frontend & Browser Engineering (aggregating browser performance, Vue, React, TypeScript documents)
- **AND** reading and recall stats from the user's library `DocumentBook` categories accurately increment completed and total counts in the corresponding universal pillar.

#### Scenario: Bilingual visual verification for identity widget, milestone stats, and domain goal tracker
- **WHEN** user toggles between English (`en`) and Vietnamese (`vi`) on the `/profile` page
- **THEN** all copy across the Identity passport, milestones telemetry strip (drills completed, quiz accuracy, memory vault, highlights saved), and domain goal tracker updates dynamically
- **AND** the quiz accuracy card label in Vietnamese renders as "Độ chính xác Quiz" without ellipsis truncation or badge clipping
- **AND** the page header subtitle renders "Quản lý hồ sơ kỹ sư, năng lực chuyên môn và bảo mật tài khoản" in Vietnamese and "Manage your senior engineer profile, career telemetry, and security credentials" in English, without duplicating the domain mastery subtitle.

### Requirement: Executive Cockpit Bento Dashboard Layout Integration
The primary root route `/` (`HomeBentoDashboard.vue`) SHALL implement the `BentoDashboardLayout` archetype (`BentoDashboardLayout.vue`), decoupling layout shell geometry from individual card content and providing a single, uncluttered unified focus pathway for the user's active reading slice and its scenario challenge.

1. **Header Slot (`#header`)**:
   - Houses the Welcome & Orientation Banner, displaying the personalized engineer greeting, role target, active reading slice badge, and streak status.

2. **Action Stage Slot (`#action-stage`)**:
   - **Unified Focus Card (Active Slice & Scenario Challenge)**:
     - Displays the user's active `DocumentBook` reading slice: book title, active slice title (`chunk.title`), summary, estimated read time, and progress bar with percentage.
     - Surfaces today's scenario challenge drill status for the active slice inline within the same card:
       - *Pending*: Subtle pending indicator ("Ready to practice" / "Sẵn sàng luyện tập" or "+10 Points Available").
       - *Submitted / Completed*: Completed badge with score ("Completed: {score}/10" or "Đã hoàn thành").
     - Provides a prominent "Continue Reading →" ("Đọc Tiếp →") primary CTA button that navigates directly to the dedicated Library Reader at that slice (`/read/${bookId}?slice=${currentChunkOrder}`), and a "Start Today's Practice →" ("Vào Luyện Tập →") action that navigates to the Daily Focus session (`/today`).
     - The card SHALL NOT render the dual Card A / Card B split, the `"Day {dayOrder} / 30"` curriculum progress badge, or `topic?.title` bindings.

3. **Telemetry Dock Slot (`#telemetry-dock`)**:
   - Houses Card C (7-day Consistency Heatmap and SM-2 Due Count) and Card D (Domain Knowledge Constellation Card), equalizing total vertical height with the action stage.

#### Scenario: Navigating Home Dashboard on 1080p Desktop
- **WHEN** an engineer loads the root page `/` on a 1920x1080 desktop browser
- **THEN** the entire Bento Grid renders with cohesive spacing and equalized column heights, eliminating empty internal margins within the unified focus card.

#### Scenario: User navigates to Today's Practice from Home Dashboard
- **WHEN** an authenticated user clicks the "Start Today's Practice" ("Vào Luyện Tập") CTA button on the unified focus card of the Home Bento Dashboard
- **THEN** the browser navigates directly to `/today`
- **AND** the Daily Focus Cockpit opens with the active reading slice and interview challenge ready for practice.

#### Scenario: User distinguishes between Continue Reading and Today's Practice
- **WHEN** an authenticated user with an active `DocumentBook` views the Home Bento Dashboard
- **THEN** the unified focus card identifies the active document slice with "Continue Reading" ("Đọc Tiếp") targeting `/read/${bookId}` and "Start Today's Practice" ("Vào Luyện Tập") targeting `/today`
- **AND** no `"Day {dayOrder} / 30"` badge and no `topic?.title` binding is rendered.

#### Scenario: Today's Practice Card reflects daily drill completion state
- **GIVEN** an authenticated user who has already submitted the active slice's architectural drill (`drill.status === 'Submitted'`)
- **WHEN** the user views the unified focus card on the Home Bento Dashboard
- **THEN** the card displays the completed status badge with earned score
- **AND** the practice action button indicates "Review Practice" ("Xem Lại Buổi Học") while still routing to `/today`.

### Requirement: Interactive API Documentation & OpenAPI Explorer
The backend system SHALL generate OpenAPI 3.1 specification metadata and serve an interactive developer API reference via `Scalar.AspNetCore` at route `/scalar/v1` during development environment runs. The API documentation SHALL support JWT Bearer authorization input, provide clean navigation, enforce concise operation summaries, expose complete and accurate per-operation response contracts (typed bodies and every producible status code), and support automated TypeScript client generation:

1. **OpenAPI Security Requirement & Interactive Authorization**:
   - The OpenAPI document at `/openapi/v1.json` SHALL declare the HTTP Bearer JWT security scheme in `components.securitySchemes.Bearer`.
   - The OpenAPI document SHALL declare a document-level `security` requirement (`[{ "Bearer": [] }]`), enabling Scalar's interactive authorization client, auth state indicator, and automatic token header injection (`Authorization: Bearer <token>`).

2. **Concise Operation Summaries & Granular Descriptions Standard**:
   - All Minimal API endpoints across all route groups SHALL strictly define `.WithSummary(...)` using concise, human-readable 2–5 word titles (e.g. `Get Today Focus`, `Upload PDF Book`, `Generate AI Insight`).
   - Deep architectural breakdowns, invariants, fallback behaviors, and rate-limiting policies SHALL be defined in `.WithDescription(...)`, ensuring Scalar's navigation sidebar displays clean endpoint titles without unreadable multi-sentence paragraphs.

3. **100% Minimal API Endpoint Tagging & Metadata Coverage**:
   - Every Minimal API endpoint (including all Library, Notes, and HealthCheck routes) SHALL explicitly define `.WithTags(...)`, `.WithSummary(...)`, and `.WithDescription(...)`.
   - The `/health` probe SHALL be categorized under `System Diagnostics & Health` with summary `System Health & Database Liveness`.

4. **Clean Legacy Swagger Redirection**:
   - Requests to `/swagger` and `/swagger/index.html` SHALL redirect to `/scalar/v1` (HTTP 302) and SHALL be excluded from the OpenAPI specification description (`.ExcludeFromDescription()`).
   - Project documentation (`AGENTS.md`, `README.md`) SHALL reference the Scalar API explorer at `/scalar/v1` and OpenAPI spec at `/openapi/v1.json`.

5. **Automated Frontend Client Contract Generation**:
   - The project SHALL provide an automated command (`npm run gen:api` in `frontend/package.json`) utilizing `openapi-typescript` that queries `/openapi/v1.json` and outputs strongly typed TypeScript interfaces to `frontend/types/api.generated.ts`.

6. **Typed Success Response Body Schemas**:
   - Every Minimal API endpoint that returns a payload SHALL declare its success response body schema in the OpenAPI document, so `/openapi/v1.json` and Scalar render the concrete response shape instead of an empty "No Body".
   - Each endpoint's declared success schema SHALL reference a concrete named response type (a DTO), not an untyped/anonymous object. Endpoints that currently return inline anonymous objects (authentication, notifications, user profile, system AI health, and `/health`) SHALL expose named response DTOs.
   - Endpoints producing a non-JSON payload (e.g. `text/markdown` file export) SHALL declare the produced content type and status rather than an inferred empty `200`.

7. **Complete Producible Status-Code Documentation with RFC 7807 Errors**:
   - Every endpoint SHALL document all status codes it can produce, not solely `200`. Success codes SHALL be documented with their body schema; error codes (`400`, `401`, `404`, `409` as applicable) SHALL be documented with the RFC 7807 `application/problem+json` problem-details schema.
   - Authenticated endpoints (`.RequireAuthorization()`) SHALL document `401 Unauthorized`. Endpoints performing input validation SHALL document `400 Bad Request` (validation problem). Endpoints resolving a resource by identifier SHALL document `404 Not Found`.
   - Domain and validation error responses returned by endpoints SHALL be emitted as RFC 7807 problem details (carrying the domain error `code`), replacing ad-hoc anonymous `{ code, error }` JSON bodies, so the documented error schema matches the body actually returned.

8. **REST-Correct Runtime Status Codes & Documentation Parity**:
   - Endpoints that persist a new domain resource and return its representation SHALL respond with `201 Created` (including a `Location` header where a canonical resource URL exists); mutations with no response body SHALL respond with `204 No Content`; reads and commands that return a payload SHALL respond with `200 OK`.
   - Authentication/session endpoints (register, login, Google sign-in, token refresh) that return a session token payload SHALL respond with `200 OK` and are not treated as REST resource creation.
   - The set of status codes documented for an operation SHALL equal the set of status codes the handler can actually emit; documented codes and runtime codes SHALL NOT diverge.

9. **Technology-Agnostic Operation Copy**:
   - Operation summaries and descriptions SHALL use current technology-agnostic domain language and SHALL NOT reference the retired fixed "30-day" curriculum program or a curriculum roadmap operation. The documented surface SHALL NOT include a `GET /api/v1/curriculum/roadmap` operation; roadmap data derives from the user's active `DocumentBook` and its ordered `DocumentChunk` slices.

#### Scenario: Developer accesses interactive API documentation in development
- **WHEN** a developer navigates to `/scalar/v1` in the development environment
- **THEN** the system serves the Scalar API explorer rendered with dark theme (`ScalarTheme.Moon`)
- **AND** the sidebar renders clean, concise endpoint titles for all Minimal API endpoints without multi-sentence text wrapping.

#### Scenario: Developer authorizes API requests via JWT Bearer in Scalar
- **WHEN** a developer provides a valid JWT token in Scalar's security definition dialog
- **THEN** subsequent test requests executed from the Scalar UI include the `Authorization: Bearer <token>` header.

#### Scenario: Legacy Swagger URL is requested
- **WHEN** a user or client requests `/swagger` or `/swagger/index.html`
- **THEN** the server responds with a redirect to `/scalar/v1`
- **AND** the redirect routes do not appear in the Scalar documentation index.

#### Scenario: Frontend developer generates OpenAPI TypeScript types
- **WHEN** a developer runs `npm run gen:api` in the `frontend` directory with the backend running
- **THEN** the CLI queries `http://localhost:5000/openapi/v1.json`
- **AND** generates a clean TypeScript type definition file at `frontend/types/api.generated.ts` containing all endpoint paths, request bodies, and response schemas.

#### Scenario: Developer inspects an authenticated read operation in Scalar
- **WHEN** a developer opens an authenticated read operation (e.g. `GET /api/v1/daily/today`) in Scalar
- **THEN** the operation documents a `200 OK` response whose body schema is the concrete response DTO with its fields
- **AND** the operation also documents a `401 Unauthorized` response using the RFC 7807 problem-details schema
- **AND** the response panel is no longer an empty "No Body".

#### Scenario: Developer inspects and calls a resource-creation operation
- **WHEN** a developer inspects a resource-creation operation that persists a new entity (e.g. `POST /api/v1/insights/generate`, `POST /api/v1/review/cards/from-highlight`, `POST /api/v1/review/cards/from-quiz-mistake`)
- **THEN** the operation documents a `201 Created` response with the created resource's body schema
- **AND** invoking the operation at runtime returns HTTP `201`, matching the documented code.

#### Scenario: Endpoint returns a domain error
- **WHEN** an endpoint returns a domain or validation failure
- **THEN** the response body is an RFC 7807 `application/problem+json` payload that includes the domain error `code`
- **AND** the returned status code is one of the codes documented for that operation.

#### Scenario: Curriculum roadmap operation copy is technology-agnostic
- **WHEN** a developer inspects the Scalar explorer and the `/openapi/v1.json` document
- **THEN** no `GET /api/v1/curriculum/roadmap` operation is present in the API surface
- **AND** no operation summary or description references a fixed "30-day" curriculum program.

### Requirement: Document Chunk Vector Completeness Invariant
Every document chunk in `DocumentChunks` SHALL possess a valid, non-null 768-dimensional float vector (`Embedding IS NOT NULL`) before being included in semantic vector similarity search or RAG retrieval pipelines. All `DocumentChunks` originate from user-imported library documents; there is no other chunk source.

#### Scenario: Verification of vector completeness post-maintenance
- **WHEN** post-maintenance integrity verification is executed
- **THEN** a query for `SELECT COUNT(*) FROM "DocumentChunks" WHERE "Embedding" IS NULL` returns exactly `0`
- **AND** an approximate nearest neighbor cosine similarity query using the `hnsw` index executes successfully without error.

## REMOVED Requirements

### Requirement: Curriculum Vector Backfill Pipeline
**Reason**: The fixed curriculum and `CurriculumSeeder` are retired in the pure bring-your-own-docs model; there is no seeded curriculum content to backfill.
**Migration**: Chunk vectorization now occurs on library ingestion (see Requirement: Document Chunk Vectorization on Ingestion); any residual unvectorized library chunks are handled by the existing `DatabaseMaintenanceRunner` maintenance path rather than a curriculum-specific seeder.

### Requirement: Technology-Agnostic Starter Handbook Content Invariant
**Reason**: There is no pre-built starter handbook content in a pure bring-your-own-docs model; the *Senior Engineering Craft Handbook* and its seed JSON are removed.
**Migration**: Users import their own technical documents (PDF upload, web crawler, or markdown series); the four universal pillar categories remain available for user documents but no canonical seeded handbook exists.

### Requirement: User-Centric Starter Handbook Provisioning on Registration
**Reason**: New accounts start with an empty library; no starter content is provisioned on registration.
**Migration**: Registration provisions nothing; users import documents themselves, and the empty-state guidance across `/library`, `/today`, and `/roadmap` is the initial state.

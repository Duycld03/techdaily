# Spec Delta: Core Platform

## MODIFIED Requirements

### Requirement: User Profile Management, Route Guards & Security
The user profile endpoints (`GET /api/v1/user/profile`, `PUT /api/v1/user/profile`, `PUT /api/v1/user/change-password`) SHALL support managing user study schedules, streak preservation alert preferences, IANA timezones, and browser push status alongside existing profile properties, protected with strict JWT Bearer authentication, reject unauthenticated requests with `HTTP 401 Unauthorized`, and enforce route middleware guards on protected frontend pages.

The User Profile interface (`frontend/pages/profile.vue`) and update action SHALL be strictly dedicated to personal identity (`name`), career role targets (`targetRole`), daily study pace (`dailyGoalMinutes`), and account credentials/password security.

The User Profile interface SHALL NOT display notification scheduling controls (`preferredStudyTime`, `streakAlertTime`) or timezone displays. The User Profile interface SHALL NOT include redirection links or navigational bridges to the system settings page, keeping the user experience clean and decluttered.

When submitting profile updates from the profile page, the client application SHALL dispatch only personal identity and pace fields (`name`, `targetRole`, `dailyGoalMinutes`) to `PUT /api/v1/user/profile`. The backend API SHALL support partial updates, preserving existing notification schedule and timezone database records when those fields are omitted.

The User Profile interface (`frontend/pages/profile.vue`) SHALL present an executive **Senior Engineer Career Portfolio & Passport** adhering to the **Dev-Learning Studio** visual standard:
1. **Root Container**: Renders on deep dark canvas (`dark:bg-canvas`, `dark:bg-canvas-subtle`) with `scrollbar-gutter: stable` and responsive spacing.
2. **Top Full-Width Engineer Identity Passport**: A prominent `.glass-card` banner spanning the top of the profile page featuring:
   - Large avatar with status indicator badge.
   - Engineer display name, email address, and membership tenure (`Member since: <date>`).
   - Badges for Target Role (e.g. `Senior Engineer`), Account Connection (Google Linked / Standard Email), and Personal Best Record (`Longest Streak: <days>` trophy badge).
3. **Left Column (Account & Security Hub)**:
   - Houses the settings and credentials forms inside a dedicated `.glass-card` with clean tab switching:
     - *Personal Info Tab*: Full Name input, Target Role selector, interactive Daily Goal Pace chips (`5m`, `10m`, `15m`, `30m`).
     - *Security Tab*: Current password verification, new password with dynamic strength bar, confirm password matching, and Google account hint banner.
4. **Right Column (Cumulative Milestones & Domain Mastery Portfolio)**:
   - **4-Cell Cumulative Achievement Bento Grid**: Replaces redundant active daily streak with four authentic long-term senior engineering telemetry cards:
     - *Architecture Drills*: Total senior scenario drills completed and average score (`{totalDrillsCompleted}` completed, `{averageScore}/10`).
     - *Interview Quiz Accuracy*: Accuracy percentage (`{accuracyRate}%`) and mastered concepts ratio (`{masteredCount}/{totalAnswered}`).
     - *Memory Vault (Spaced Repetition)*: Total technical concepts active in the SM-2 review deck (`{totalCardsInDeck}`).
     - *Architecture Source Highlights*: Total highlighted code and architectural quotes saved (`{totalHighlightsSaved}`).
   - **Domain Mastery Progress Bar (Goal Tracker)**: Categorized visual progress bars tracking curriculum domain coverage across four universal, framework-agnostic core engineering pillars, utilizing high-contrast gradient tracks:
     - **Pillar 1: Backend Runtime & Concurrency** (`profile.domain_backend_runtime`: "Nền Tảng Backend & Runtime" / "Backend Runtime & Concurrency") - tracking runtime mechanisms, memory allocation, async I/O, threads, and concurrency across .NET/CLR, Node.js/NestJS/Express, Go/Goroutines, Java/Spring/JVM, and Python.
     - **Pillar 2: Data Storage & Persistence** (`profile.domain_data_storage`: "Hệ Lưu Trữ & Cơ Sở Dữ Liệu" / "Data Storage & Persistence") - tracking storage engines, query execution, and persistence across PostgreSQL, MongoDB, Redis, MySQL, SQLite, Cassandra, ACID transactions, B-Trees, LSM-Trees, replication, WAL, and indexing.
     - **Pillar 3: Distributed Systems & Architecture** (`profile.domain_system_design`: "Hệ Thống Phân Tán & Thiết Kế" / "Distributed Systems & Architecture") - tracking distributed architecture patterns, microservices, message queues (Kafka, RabbitMQ), CAP theorem, transactional outbox, consensus, idempotency, rate limiting, and observability.
     - **Pillar 4: Frontend & Browser Engineering** (`profile.domain_frontend`: "Hiệu Năng Frontend & Trình Duyệt" / "Frontend & Browser Engineering") - tracking browser execution, rendering pipelines, critical rendering path, DOM, Vue, React, TypeScript, JavaScript, Web Vitals, and SSR/hydration.

The Domain Mastery Goal Tracker component SHALL evaluate and aggregate topic mastery dynamically across multi-stack keywords via `matchCategory(keyOrTopic: string)`, ensuring book chapters, drills, and quiz attempts in any modern stack map seamlessly into the appropriate universal pillar.

The Engineer Portfolio Dashboard SHALL adapt responsively across Desktop (≥ 1024px, top full-width passport + 2-column grid) and Mobile (< 1024px, vertically stacked layout) viewports without horizontal scrolling or layout overlap.

The Identity passport, milestone statistics, and domain goal tracker SHALL support bilingual rendering in both English and Vietnamese, ensuring that universal engineering pillar titles render cleanly without text truncation, badge clipping, or broken progress bar labels.

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
- **THEN** the layout renders a full-width top identity passport followed by a 2-column grid with Account & Security Hub on the left and Cumulative Milestones & Domain Mastery on the right
- **WHEN** user accesses `/profile` on a mobile viewport ($< 1024\text{px}$)
- **THEN** the layout stacks into a single vertical column with the Identity passport at the top followed by the Cumulative Milestones, Domain Mastery tracker, and Account settings form, maintaining touch-friendly targets and zero horizontal overflow.

#### Scenario: User inspects identity and learning milestones widget
- **WHEN** user views the profile page
- **THEN** UI renders the top identity passport with user avatar, account connection badge (Google / Email), membership tenure, and all-time longest streak badge
- **AND** the milestones bento renders 4 cumulative cards: total drills completed with average score, quiz accuracy rate, SM-2 cards in deck, and total highlights saved inside `.glass-card` components with hairline borders.

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
- **THEN** all copy across the Identity passport, cumulative milestone cards (drills completed, quiz accuracy, memory vault, highlights saved), and domain goal tracker updates dynamically
- **AND** universal pillar titles render with exact localized strings:
  - In English: "Backend Runtime & Concurrency", "Data Storage & Persistence", "Distributed Systems & Architecture", "Frontend & Browser Engineering"
  - In Vietnamese: "Nền Tảng Backend & Runtime", "Hệ Lưu Trữ & Cơ Sở Dữ Liệu", "Hệ Thống Phân Tán & Thiết Kế", "Hiệu Năng Frontend & Trình Duyệt"
- **AND** longer Vietnamese domain titles render without badge clipping, text truncation, or misalignment of progress bar percentages.

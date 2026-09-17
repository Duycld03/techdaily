## MODIFIED Requirements

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

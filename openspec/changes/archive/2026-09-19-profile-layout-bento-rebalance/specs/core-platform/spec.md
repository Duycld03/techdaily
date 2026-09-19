# Spec Delta: Core Platform

## MODIFIED Requirements

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

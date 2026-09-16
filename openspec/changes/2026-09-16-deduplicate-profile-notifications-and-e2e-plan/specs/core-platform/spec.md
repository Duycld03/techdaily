# Core Platform Capability Delta Specification

## Purpose
Defines delta specifications for separating personal user profile management from system notification scheduling and timezone configurations, enforcing single-source-of-truth semantics in the Settings domain, and establishing a zero-repo-footprint live production end-to-end (E2E) verification standard.

---

## MODIFIED Requirements

### Requirement: User Profile Management, Route Guards & Security
The User Profile interface (`frontend/pages/profile.vue`) and update action SHALL be strictly dedicated to personal identity (`name`), career role targets (`targetRole`), daily study pace (`dailyGoalMinutes`), and account credentials/password security. 

The User Profile interface SHALL NOT display notification scheduling controls (`preferredStudyTime`, `streakAlertTime`) or timezone displays. The User Profile interface SHALL NOT include redirection links or navigational bridges to the system settings page, keeping the user experience clean and decluttered.

When submitting profile updates from the profile page, the client application SHALL dispatch only personal identity and pace fields (`name`, `targetRole`, `dailyGoalMinutes`) to `PUT /api/v1/user/profile`. The backend API SHALL support partial updates, preserving existing notification schedule and timezone database records when those fields are omitted.

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

## NEW Requirements

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

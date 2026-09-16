# Proposal: Deduplicate Profile Notification Settings and Live Production E2E Verification Plan

## Title
Deduplicate Profile Notification Settings and Live Production E2E Verification Plan

## Why

TechDaily provides continuous daily learning and active recall retention for software engineers. User configuration is split across several management surfaces, primarily `/profile` (personal identity, target role, daily study pace, password security) and `/settings` (UI language, theme mode, Web Push notification subscription, study reminder schedule, and timezone).

However, examination of user workflows and codebase architecture reveals two critical areas requiring intervention:

### 1. Duplication of Notification Scheduling and Timezone Between `/profile` and `/settings`
Currently, `frontend/pages/profile.vue` renders a redundant card titled **"Study Schedule & Timezone"** containing:
- `preferredStudyTime` (HTML time input)
- `streakAlertTime` (HTML time input)
- `timeZone` (read-only detected timezone display with `Globe` icon)

Simultaneously, `frontend/pages/settings.vue` contains the dedicated **"Browser Web Push Notifications"** panel with interactive toggle controls, test push triggers, and the **Notification Schedule & Timezone Settings** section (`preferredStudyTime`, `streakAlertTime`, and a full interactive `timeZone` selector with common timezones).

#### Architectural & UX Problems:
- **Violated Single Source of Truth:** Having two separate pages editing the same user preferences creates state ambiguity and potential race conditions when a user changes settings in one tab while having the profile tab open.
- **Payload Overfetching & Overwriting:** When saving personal information (such as changing a display name or target role) via `handleProfileSave` in `/profile`, the client currently sends `{ name, targetRole, dailyGoalMinutes, preferredStudyTime, streakAlertTime, timeZone }`. This forces `/profile` to maintain reactive state and bindings for notifications, leading to unnecessary payload bloat and accidental overwriting of settings managed in `/settings`.
- **Interface Clutter:** The profile page is intended for personal identity, engineering seniority target, and account security. Interleaving notification dispatch times clutters the visual hierarchy.
- **Clear User Decision:** User feedback specifically requests decluttering `/profile` by removing the schedule card, reactive state, and unused icons, sending only `{ name, targetRole, dailyGoalMinutes }`. Crucially, user feedback mandates **NOT adding a link from profile to settings** ("không cần ở profile tạo 1 link sang setting làm gì đâu"), preserving a clean, distraction-free profile view without redundant navigational bridges.

### 2. Need for Live Production E2E Verification with Zero Repo Footprint
TechDaily has recently landed critical capabilities, including:
- Spaced repetition (SM-2) flashcard creation from reading highlights in `/notes` without Vue composition lifecycle crashes.
- AI Term Explainer modal in `/reader` with bilingual layout and responsive typography.
- Brave browser push service detection with localized user guidance in `/settings`.
- Timezone auto-synchronization during Web Push subscription.

To ensure regressions do not reach end users, we require a comprehensive end-to-end (E2E) verification against the live production deployment at `https://techdaily.duckdns.org`.
However, committing ad-hoc Playwright or Cypress test suites, fixtures, node modules, or browser cache configurations directly into the application repository introduces repository bloat, CI complexity, and maintenance overhead. 

We need a **Zero-Repo-Footprint Live E2E Verification Plan**:
- Executes entirely out of an ephemeral script located at `/tmp/techdaily-live-e2e.mjs`.
- Utilizes pre-cached headless Chromium engines already installed on the host machine (`~/.cache/ms-playwright/` or `~/.omp/puppeteer/`).
- Commits **zero test files, zero temporary scripts, and zero dependency locks** into the git repository.
- Systematically validates critical end-to-end user journeys against live production.

---

## Proposed Solution

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                               ARCHITECTURAL SEPARATION                                 │
│                                                                                        │
│   /profile (Personal Identity & Goals)            /settings (System & Notifications)   │
│   ┌──────────────────────────────────┐            ┌──────────────────────────────────┐ │
│   │ • Avatar & Display Name          │            │ • Interface Language & Theme     │ │
│   │ • Target Role (Career Track)     │            │ • Web Push Notification Toggle   │ │
│   │ • Daily Study Pace (5-30 mins)   │            │ • Preferred Study Time (08:00)   │ │
│   │ • Security / Password Change     │            │ • Streak Alert Time (20:00)      │ │
│   │                                  │            │ • Timezone Selector & Detection  │ │
│   │ [Payload: name, targetRole,      │            │ [Payload: isPushEnabled,         │ │
│   │  dailyGoalMinutes]               │            │  preferredStudyTime,             │ │
│   │                                  │            │  streakAlertTime, timeZone]      │ │
│   │ (NO link to /settings)           │            │                                  │ │
│   └──────────────────────────────────┘            └──────────────────────────────────┘ │
│                                                                                        │
│   LIVE PRODUCTION E2E VERIFICATION (Zero Repo Footprint)                               │
│   ┌──────────────────────────────────────────────────────────────────────────────────┐ │
│   │ Host: /tmp/techdaily-live-e2e.mjs                                                │ │
│   │ Engine: Headless Chromium (~/.cache/ms-playwright/ or ~/.omp/puppeteer/)         │ │
│   │ Target: https://techdaily.duckdns.org                                            │ │
│   │ Scenarios: S1 (Auth & Shell) -> S2 (Reader Modal) -> S3 (Notes SM-2 Flashcard)   │ │
│   │            -> S4 (Settings & Push) -> S5 (Decluttered Profile Save)              │ │
│   └──────────────────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

### 1. Declutter `frontend/pages/profile.vue`
- **Template Clean-up:** Completely excise the "Study Schedule & Timezone" card (lines containing `preferredStudyTime`, `streakAlertTime`, and `timeZone`).
- **Script Clean-up:**
  - Remove unused reactive variables: `preferredStudyTime`, `streakAlertTime`, and the `timeZone` ref / initialization block.
  - Remove unused icons from `lucide-vue-next` import: `Clock`, `Globe`.
  - Remove population logic in `onMounted()` for schedule and timezone fields.
- **Clean API Contract:** In `handleProfileSave()`, update the call to `profileStore.updateProfile()` to only send:
  ```typescript
  await profileStore.updateProfile({
    name: name.value.trim(),
    targetRole: targetRole.value,
    dailyGoalMinutes: dailyGoalMinutes.value
  })
  ```
- **Strict UI Cleanliness:** Do not render any banner, link, or button redirecting users from `/profile` to `/settings`. The profile page remains strictly dedicated to personal identity.

### 2. Preserve Single Source of Truth in `frontend/pages/settings.vue`
- Maintain `preferredStudyTime`, `streakAlertTime`, and `timeZone` exclusively in `settings.vue`.
- When users update schedule or timezone preferences, `handleSaveSchedule()` submits the schedule payload to `PUT /api/v1/user/profile`.
- When users enable Web Push, `handleTogglePush()` coordinates with `useWebPush.ts` and updates `isPushEnabled` and `timeZone`.
- No regression or modification to `settings.vue` logic: it remains the canonical, unified configuration dashboard.

### 3. Zero-Repo-Footprint Live E2E Verification Plan
Implement and execute a headless Chromium E2E test plan targeting production (`https://techdaily.duckdns.org`):
- **Runner Location:** Standalone ESM script written directly to `/tmp/techdaily-live-e2e.mjs`.
- **Browser Execution:** Launch Chromium using existing binaries located in `~/.cache/ms-playwright/` or `~/.omp/puppeteer/chrome`.
- **Zero Repo Footprint:** Absolutely no files created or modified within the git workspace (`frontend/`, `backend/`, root tests).
- **Comprehensive Scenario Coverage:**
  - **S1: Authentication & Navigation Shell:** Login flow, JWT acquisition, layout navigation checks across main tabs.
  - **S2: Reader & AI Term Explainer Modal:** Open daily slice, trigger term explanation, assert bilingual layout (EN definition + VI explanation), inspect bounding boxes to ensure zero horizontal/vertical text overflow.
  - **S3: Notes & SM-2 Flashcard Creation:** Open `/notes`, click "Flashcard SM-2" on an existing highlight, assert no `[vue-i18n]` runtime injection error, verify success toast, verify card appears in `/review` queue.
  - **S4: Settings & Web Push:** Navigate to `/settings`, verify schedule time inputs, verify timezone dropdown, test Brave push notification handling/guidance.
  - **S5: Decluttered Profile Save:** Navigate to `/profile`, assert complete absence of schedule/timezone inputs, update name and target role, save form, verify network payload contains only `{ name, targetRole, dailyGoalMinutes }`, verify success toast and persistence.

---

## What Changes

| Area | Component / Resource | Current Behavior | Proposed Behavior |
| :--- | :--- | :--- | :--- |
| **Profile UI** | `frontend/pages/profile.vue` | Displays "Study Schedule & Timezone" card with time inputs and timezone label. | Card completely removed; profile page contains only personal identity and security forms. |
| **Profile State** | `frontend/pages/profile.vue` | Declares `preferredStudyTime`, `streakAlertTime`, `timeZone`; imports `Clock`, `Globe`. | Removed unused state and unused icon imports. |
| **Profile Save** | `frontend/pages/profile.vue` | Sends `{ name, targetRole, dailyGoalMinutes, preferredStudyTime, streakAlertTime, timeZone }`. | Sends only `{ name, targetRole, dailyGoalMinutes }`. |
| **Profile Navigation** | `frontend/pages/profile.vue` | User asked whether to link to settings. | NO link to settings added; stays decluttered. |
| **Settings UI** | `frontend/pages/settings.vue` | Manages notifications, schedule, and timezone. | Remains the single source of truth without changes. |
| **E2E Testing** | Production live tests | Manual testing or local component unit tests only. | Automated Playwright script running from `/tmp/techdaily-live-e2e.mjs` against `https://techdaily.duckdns.org` with zero git footprint. |

---

## Impact & User Experience Benefits

1. **Clean Domain Separation:** Clear mental model for users: `/profile` = who I am and my learning pace; `/settings` = how the system interacts with me (language, theme, push notifications, reminder schedule, timezone).
2. **Elimination of State Divergence:** Eliminates the risk of saving stale or conflicting schedule times from `/profile` over values recently configured in `/settings`.
3. **Payload Optimization:** Reduces network payload size and prevents unnecessary database column rewrites when saving personal profile changes.
4. **Enhanced Code Maintainability:** Less dead code and fewer reactive variables in `profile.vue`.
5. **Verified Live Reliability:** Automated E2E verification of critical paths (authentication, reader modal, notes flashcards, push settings, profile save) against the live production environment without polluting the git tree.

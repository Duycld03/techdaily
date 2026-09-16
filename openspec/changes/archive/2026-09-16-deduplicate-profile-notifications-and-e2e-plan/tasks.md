# Tasks: Deduplicate Profile Notification Settings and Live Production E2E Verification Plan

## Phase 1: Profile Decluttering (`frontend/pages/profile.vue`)

- [x] 1.1 Clean up icon imports in `frontend/pages/profile.vue`:
  - Remove `Clock` and `Globe` from the `lucide-vue-next` import list.
  - Retain all other active icons (`User`, `Shield`, `Flame`, `CheckCircle2`, `Lock`, `Mail`, `Briefcase`, `Save`, `Eye`, `EyeOff`, `Target`, `Compass`).
- [x] 1.2 Remove redundant reactive state from `<script setup>` in `frontend/pages/profile.vue`:
  - Remove `preferredStudyTime = ref('08:00')`.
  - Remove `streakAlertTime = ref('20:00')`.
  - Remove `timeZone = ref(...)` and its IIFE auto-detection closure.
- [x] 1.3 Clean up data population in `onMounted()` in `frontend/pages/profile.vue`:
  - Remove lines reading `data.user.preferredStudyTime`.
  - Remove lines reading `data.user.streakAlertTime`.
  - Remove lines reading `data.user.timeZone`.
- [x] 1.4 Update profile submission in `handleProfileSave()` in `frontend/pages/profile.vue`:
  - Update `profileStore.updateProfile()` payload to pass only:
    ```typescript
    await profileStore.updateProfile({
      name: name.value.trim(),
      targetRole: targetRole.value,
      dailyGoalMinutes: dailyGoalMinutes.value
    })
    ```
  - Verify `preferredStudyTime`, `streakAlertTime`, and `timeZone` are not included in the payload.
- [x] 1.5 Remove the "Study Schedule & Timezone" card from `<template>` in `frontend/pages/profile.vue`:
  - Delete the card container (`<div class="p-4 rounded-xl bg-slate-50/70 ...">`) containing schedule title, study time input, streak alert time input, and detected timezone display.
- [x] 1.6 Verify strict UI decluttering:
  - Confirm that NO navigation button, link, or banner pointing to `/settings` is added to `profile.vue`.
  - Ensure the profile page remains focused exclusively on personal identity, role target, daily pace, and security.

---

## Phase 2: Live Production E2E Execution & Reporting (Zero Repo Footprint)

- [x] 2.1 Create standalone E2E runner at `/tmp/techdaily-live-e2e.mjs`:
  - Implement dynamic resolution for cached Chromium executables checking:
    - `~/.cache/ms-playwright/chromium-*/chrome-linux/chrome`
    - `~/.omp/puppeteer/chrome/linux-*/chrome-linux64/chrome`
  - Implement scenario execution harness with console error collection and screenshot capture on failure (`/tmp/techdaily-e2e-failure-*.png`).
- [x] 2.2 Execute Scenario 1: Authentication & Navigation Shell:
  - Launch headless browser pointing to `https://techdaily.duckdns.org`.
  - Perform login or verify active session.
  - Verify top-level navigation items (`Today`, `Library`, `Drills`, `Notes`, `Review`, `Settings`, `Profile`) are rendered and clickable.
  - Confirm zero uncaught client exceptions.
- [x] 2.3 Execute Scenario 2: Reader & AI Term Explainer Modal:
  - Navigate to `/reader` (active daily reading slice).
  - Open AI Term Explainer modal.
  - Assert bilingual content presentation: English technical term/definition alongside Vietnamese contextual explanation.
  - Verify modal bounding box does not overflow viewport dimensions and document has no horizontal scroll blowout.
  - Close modal cleanly via backdrop or escape key.
- [x] 2.4 Execute Scenario 3: Notes & SM-2 Flashcard Creation:
  - Navigate to `https://techdaily.duckdns.org/notes`.
  - Locate a highlight note and click "Flashcard SM-2".
  - Assert console receives zero `[vue-i18n] Not found injection "vue-i18n"` errors.
  - Assert successful toast notification is displayed.
  - Navigate to `/review` and assert that the flashcard queue reflects the newly generated card.
- [x] 2.5 Execute Scenario 4: Settings & Web Push:
  - Navigate to `https://techdaily.duckdns.org/settings`.
  - Assert that notification schedule inputs (`preferredStudyTime`, `streakAlertTime`) and `timeZone` dropdown are present and functional.
  - Execute "Save Schedule" and verify network request persists preferences with `HTTP 200`.
  - Verify Web Push controls and validate Brave browser push service guidance handling if applicable.
- [x] 2.6 Execute Scenario 5: Decluttered Profile Form Save:
  - Navigate to `https://techdaily.duckdns.org/profile`.
  - Assert that schedule time inputs and timezone indicators are completely absent.
  - Assert that no link to `/settings` is present in the profile container.
  - Intercept `PUT /api/v1/user/profile` and save profile with updated name and target role.
  - Verify request body contains only `{ name, targetRole, dailyGoalMinutes }` without schedule or timezone fields.
  - Verify success toast and confirm updated values persist upon page reload.
- [x] 2.7 Compile and output E2E Execution Report:
  - Write detailed JSON test execution report to `/tmp/techdaily-live-e2e-report.json`.
  - Log scenario status (`PASS`/`FAIL`), timing metrics, and screenshot paths.

---

## Phase 3: CI/CD & Production Verification

- [x] 3.1 Verify Zero Repo Footprint:
  - Execute `git status` to verify that no runner scripts, temporary files, or screenshots were created in the repository.
  - Confirm repository working directory is 100% clean of testing artifacts.
- [x] 3.2 Run frontend automated tests and type verification:
  - Run `npm test` in `frontend/` to confirm existing test suites pass.
  - Ensure zero TypeScript or Vue compiler warnings.
- [x] 3.3 Confirm backward compatibility:
  - Ensure backend `PUT /api/v1/user/profile` handles partial profile updates without altering existing `PreferredStudyTime`, `StreakAlertTime`, or `TimeZone` database values.

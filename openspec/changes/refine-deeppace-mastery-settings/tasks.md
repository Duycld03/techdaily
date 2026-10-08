# Tasks

## 1. Backend Domain & API Endpoints

- [ ] 1.1 Update `User.cs` default `TargetRole` to `"Deep Work & Focus Practitioner"`.
- [ ] 1.2 Update `AuthEndpoints.cs` to default new user `TargetRole` to `"Deep Work & Focus Practitioner"`.
- [ ] 1.3 Update `NotificationEndpoints.cs` test push notification payload to `"DeepPace Test Push 🚀"` with tag `"deeppace-test"`.

## 2. Frontend Settings & Localization

- [ ] 2.1 Update `frontend/app/i18n/locales/en.json` and `vi.json` to introduce `settings.mastery_track` and localized labels for all DeepPace mastery tracks while retaining backward compatibility for legacy roles.
- [ ] 2.2 Update `frontend/app/pages/settings.vue`: replace `difficultyOptions` with `masteryTrackOptions` binding to `AppSelect.vue`, updating the field label to `settings.mastery_track`.
- [ ] 2.3 Update unit test in `frontend/tests/pages/settings.spec.ts` to assert that mastery track options and profile submission contracts function as expected.

## 3. Micro-Branding Clean Cutover

- [ ] 3.1 Update `frontend/app/composables/useSliceAudio.ts` MediaSession default artist to `'DeepPace'` and album to `'DeepPace Reader'`.
- [ ] 3.2 Update `frontend/public/sw.js` default notification title to `'DeepPace'`.
- [ ] 3.3 Update `frontend/app/pages/showcase.vue` title to `'Design System — DeepPace'` and update Google Auth log warnings in `frontend/app/pages/login.vue` to `'[DeepPace Auth]'`.

## 4. Verification & Dual-Gate Validation

- [ ] 4.1 Run backend unit tests via `dotnet test` to verify 100% passing test suites across authentication, user profile, and notification endpoints.
- [ ] 4.2 Run frontend unit tests via `npm test` to verify 100% passing test suites across settings and audio composables.
- [ ] 4.3 Execute automated headless browser inspection on `/settings` for Desktop (1440x900) and Mobile (390x844) viewports, verifying the Mastery Track `AppSelect` dropdown renders cleanly in both English and Vietnamese locales without text collisions or clipping.

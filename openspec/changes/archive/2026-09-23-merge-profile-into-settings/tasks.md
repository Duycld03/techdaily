# Tasks

## 1. Frontend Layout & Tab Navigation Hardening

- [x] 1.1 Update `frontend/pages/settings.vue` navigation rail buttons with permanent 1px border baseline (`border border-transparent` inactive, `border-brand-500/20` active) and `transition-colors` to eliminate 1px border flashing and box-sizing shift.
- [x] 1.2 Implement bidirectional URL query synchronization (`?tab=<id>`) in `frontend/pages/settings.vue` using `router.replace` on tab switch, defaulting to `profile` if unspecified or invalid.

## 2. Frontend Unified Settings & Profile Integration

- [x] 2.1 Integrate Profile & Identity section (`activeTab === 'profile'`) in `frontend/pages/settings.vue` incorporating `EngineerIdentityPassport.vue` and the personal info form (Full Name, Target Role, Daily Goal minutes chips).
- [x] 2.2 Integrate Security & Password section (`activeTab === 'security'`) in `frontend/pages/settings.vue` incorporating current/new password inputs, password strength analysis bar, and Google OAuth linked badge.
- [x] 2.3 Integrate Milestones & Mastery section (`activeTab === 'mastery'`) in `frontend/pages/settings.vue` embedding `EngineerMilestonesCard.vue` and `DomainGoalTracker.vue`.
- [x] 2.4 Update navigation rail in `frontend/pages/settings.vue` to render all 6 categories (`profile`, `security`, `mastery`, `appearance`, `notifications`, `schedule`) with icons, active indicators, and badge counts.

## 3. Frontend Navigation & Route Migration

- [x] 3.1 Refactor `frontend/pages/profile.vue` into a redirect route targeting `/settings?tab=profile` to preserve backward compatibility.
- [x] 3.2 Update `frontend/components/layout/AppHeader.vue` user avatar link to `/settings?tab=profile`.
- [x] 3.3 Update `frontend/composables/useNavigationMenu.ts` and i18n locales (`en.json`, `vi.json`) to consolidate the account navigation group from separate entries into "Settings & Profile".

## 4. Frontend Verification & Automated Testing

- [x] 4.1 Update `frontend/tests/pages/settings.spec.ts` and `frontend/tests/composables/useNavigationMenu.spec.ts` to verify multi-tab switching, deep-link query synchronization, and settings updates.
- [x] 4.2 Verify full test suite passes (`npm test`) and capture headless 1080p visual screenshots of `/settings` across tabs demonstrating zero border flashing and balanced visual density.

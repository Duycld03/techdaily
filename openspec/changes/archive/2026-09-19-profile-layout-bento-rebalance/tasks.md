# Tasks

## 1. Localization & Typography Refinement

- [x] 1.1 Add dedicated `profile.subtitle` localized key ("Manage your senior engineer profile, career telemetry, and security credentials" / "Quản lý hồ sơ kỹ sư, năng lực chuyên môn và bảo mật tài khoản") to `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.
- [x] 1.2 Update `profile.quiz_accuracy` in `frontend/i18n/locales/vi.json` to `"Độ chính xác Quiz"` to eliminate badge text truncation.

## 2. Component Architecture & Density Tuning

- [x] 2.1 Refactor `frontend/components/profile/EngineerMilestonesCard.vue` into a responsive full-width 4-column bento strip (`grid-cols-2 lg:grid-cols-4 gap-4`), ensuring all cell titles have ample horizontal clearance without truncation.
- [x] 2.2 Optimize vertical density in `frontend/components/profile/DomainGoalTracker.vue` by tightening outer and inner padding to achieve height parity (~380px) with the Account & Security Hub.

## 3. Profile Page Layout & Integration

- [x] 3.1 Refactor `frontend/pages/profile.vue` to implement the 3-Tier Bento Dashboard: Tier 1 full-width Identity Passport, Tier 2 full-width 4-column Milestones Strip, and Tier 3 balanced 50/50 lower columns (Account Hub on the left, Domain Mastery on the right).
- [x] 3.2 Bind the page header subtitle in `frontend/pages/profile.vue` to `profile.subtitle`.

## 4. Automated Verification & Testing

- [x] 4.1 Update Vitest assertions in `frontend/tests/pages/profile.spec.ts` and `frontend/tests/components/profile.spec.ts` to reflect the 3-tier layout and new subtitle key.
- [x] 4.2 Run full frontend test suite (`npm --prefix frontend test`) to ensure 100% pass rate across all test files.
- [x] 4.3 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.

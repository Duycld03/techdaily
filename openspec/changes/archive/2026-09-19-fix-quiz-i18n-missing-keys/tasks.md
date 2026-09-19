# Tasks

## 1. Localization Dictionaries Refinement

- [x] 1.1 Add `quiz.tab_review_queue` to `frontend/i18n/locales/en.json` (`"Review Queue"`) and `frontend/i18n/locales/vi.json` (`"Ôn Tập"`).
- [x] 1.2 Add missing feedback keys (`quiz.incorrect_badge`, `quiz.incorrect_attempts`, `quiz.toast_push_sm2_failed`) to `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.

## 2. Quiz Page Template & Component Refactoring

- [x] 2.1 Refactor hardcoded English strings in `frontend/pages/quiz.vue` (accuracy score suffix, incorrect badge, and incorrect attempts counter) to use localized `$t` bindings.
- [x] 2.2 Update SM-2 push error handling in `frontend/pages/quiz.vue` to use localized toast fallback `t('quiz.toast_push_sm2_failed')`.

## 3. Automated Verification & Testing

- [x] 3.1 Update unit test assertions in `frontend/tests/pages/quiz.spec.ts` to assert `quiz.tab_review_queue`.
- [x] 3.2 Run full frontend test suite (`npm --prefix frontend test`) to ensure 100% pass rate across all test files.
- [x] 3.3 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.

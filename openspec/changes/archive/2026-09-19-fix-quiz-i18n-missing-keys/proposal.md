# Proposal: Fix Missing i18n Keys & Hardcoded Copy in Interview Quiz

## Why

In `frontend/pages/quiz.vue`, the review queue navigation button invokes `$t('quiz.tab_review_queue')`, but the localization dictionaries (`en.json` and `vi.json`) only declare `quiz.tab_review`, causing the raw untranslated key string `quiz.tab_review_queue` to render on screen.

Additionally, multiple feedback labels and counters in the quiz completion summary and review queue remain hardcoded in English (`Accuracy`, `Incorrect`, `{count} incorrect attempts`, and push to SM-2 error fallback), violating the platform's bilingual localization invariant.

## What Changes

- **Add Missing Navigation Key**:
  - Add `quiz.tab_review_queue` to `frontend/i18n/locales/en.json` (`"Review Queue"`) and `frontend/i18n/locales/vi.json` (`"Ôn Tập"`), while keeping `quiz.tab_review` for backwards compatibility.
- **Add Missing Feedback & Counter Keys**:
  - `quiz.incorrect_badge`: `"Incorrect"` (en) / `"Chưa chính xác"` (vi).
  - `quiz.incorrect_attempts`: `"{count} incorrect attempts"` (en) / `"{count} lần làm sai"` (vi).
  - `quiz.toast_push_sm2_failed`: `"Failed to push to SM-2 Deck."` (en) / `"Không thể đưa vào bộ ôn tập SM-2."` (vi).
- **Refactor Hardcoded UI Strings in `frontend/pages/quiz.vue`**:
  - Replace `{{ quizStore.sessionScore.percentage }}% Accuracy` with `{{ quizStore.sessionScore.percentage }}% {{ $t('quiz.stats_accuracy') }}`.
  - Replace `<span ...>Incorrect</span>` with `<span>{{ $t('quiz.incorrect_badge') }}</span>`.
  - Replace `<span ...>{{ q.incorrectCount }} incorrect attempts</span>` with `<span>{{ $t('quiz.incorrect_attempts', { count: q.incorrectCount }) }}</span>`.
  - Replace error toast fallback with `t('quiz.toast_push_sm2_failed')`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `quiz`: Update requirement `Interview Quiz Generation, Evaluation & Review Queue` to mandate 100% localized copy for review queue navigation tabs, mistake review badges, attempt counts, and session summary scores across both English and Vietnamese locales.

## Impact

- **Backend API & Database**: 0 breaking changes.
- **Frontend Components & Locales**: `frontend/pages/quiz.vue`, `frontend/i18n/locales/en.json`, `frontend/i18n/locales/vi.json`.
- **Testing**: `frontend/tests/pages/quiz.spec.ts` updated to assert the localized tab title and mistake attempt copy.

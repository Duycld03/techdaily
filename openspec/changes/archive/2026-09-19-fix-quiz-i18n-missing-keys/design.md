# Design: Fix Missing i18n Keys & Hardcoded Copy in Interview Quiz

## Context

See `proposal.md` for motivation. The Quiz view (`frontend/pages/quiz.vue`) invokes `$t('quiz.tab_review_queue')`, which is missing from both `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, rendering raw untranslated text `quiz.tab_review_queue`. Furthermore, several feedback labels in the summary and review queue remain hardcoded in English.

## Goals / Non-Goals

**Goals:**
- Eliminate untranslated key fallbacks by supplying `quiz.tab_review_queue` in both `en.json` and `vi.json`.
- Replace all hardcoded English strings in `frontend/pages/quiz.vue` with localized `$t` bindings:
  - Summary session score: `Accuracy` $\rightarrow$ `{{ $t('quiz.stats_accuracy') }}`.
  - Mistake indicator badge: `Incorrect` $\rightarrow$ `{{ $t('quiz.incorrect_badge') }}`.
  - Review queue mistake counter: `{count} incorrect attempts` $\rightarrow$ `{{ $t('quiz.incorrect_attempts', { count: q.incorrectCount }) }}`.
  - SM-2 push failure alert: fallback message $\rightarrow$ `t('quiz.toast_push_sm2_failed')`.
- Ensure 100% automated test pass rate across the Vitest suite.

**Non-Goals:**
- Modifying backend API contracts, database schemas, or store handlers.
- Changing visual layout structure or styling tokens.

## Decisions

### 1. Dictionary Alignment & Key Preservation
- **Decision**: Add `tab_review_queue` to both locales:
  - `en.json`: `"tab_review_queue": "Review Queue"`
  - `vi.json`: `"tab_review_queue": "Ôn Tập"`
  Retain existing `"tab_review"` in dictionaries to prevent any breaking changes for callers that might reference it.
- **Rationale**: Direct alignment with `quiz.vue`'s `$t('quiz.tab_review_queue')` call without breaking backwards compatibility.

### 2. Feedback & Counter Key Structure
- **Decision**: Introduce three dedicated keys under `quiz`:
  | Key | English (`en.json`) | Vietnamese (`vi.json`) | Usage |
  |---|---|---|---|
  | `incorrect_badge` | `"Incorrect"` | `"Chưa chính xác"` | Mistake summary card badge |
  | `incorrect_attempts` | `"{count} incorrect attempts"` | `"{count} lần làm sai"` | Queue item attempt count |
  | `toast_push_sm2_failed` | `"Failed to push to SM-2 Deck."` | `"Không thể đưa vào bộ ôn tập SM-2."` | Push failure toast fallback |

### 3. Template Refactoring in `frontend/pages/quiz.vue`
- **Decision**:
  - Line 817: Replace `{{ quizStore.sessionScore.percentage }}% Accuracy` with `{{ quizStore.sessionScore.percentage }}% {{ $t('quiz.stats_accuracy') }}`.
  - Line 866: Replace `<span class="text-rose-500 font-semibold">Incorrect</span>` with `<span class="text-rose-500 font-semibold">{{ $t('quiz.incorrect_badge') }}</span>`.
  - Line 938: Replace `<span class="text-rose-500 font-semibold">{{ q.incorrectCount }} incorrect attempts</span>` with `<span class="text-rose-500 font-semibold">{{ $t('quiz.incorrect_attempts', { count: q.incorrectCount }) }}</span>`.
  - Line 80: Replace `toast.error(err.message || 'Failed to push to SM-2 Deck.')` with `toast.error(err.message || t('quiz.toast_push_sm2_failed'))`.

## Risks / Trade-offs

- **Risk**: Potential unit test regressions in `quiz.spec.ts` if tests assert hardcoded strings.
  - *Mitigation*: Existing unit tests in `quiz.spec.ts` assert `$t` keys (e.g. `quiz.practice_current_batch`, `quiz.bento_hero_title`). We will update `quiz.spec.ts` to assert `quiz.tab_review_queue` as well.

# Proposal: Stabilize Quiz Generator Layout and Fix Topic Fallback

## Why

In the `/quiz` generation interface (`frontend/pages/quiz.vue`), toggling "Luyện đề theo sách" (`isGrounded`) reveals a book selector dropdown in the left Bento card (Topic & Context Hub). Because the desktop Bento grid uses `items-stretch` and the right Bento card (Seniority & Generation Controls) uses `flex flex-col justify-between`, the right card is forced to stretch to match the left card's expanded height, abruptly pushing the primary "Tạo Bộ Đề AI" button downward by ~75px. This causes a jarring visual layout shift (CLS) and moves the interactive click target.

Additionally, in `handleGenerateQuiz`, fallback topic resolution still references the obsolete `quickTopics` variable instead of `computedQuickTopics`, presenting a runtime `ReferenceError` risk when generating quizzes without manual topic input.

Stabilizing the generator container layout and correcting the topic fallback ensures a rock-solid, shift-free user experience and robust AI quiz initiation.

## What Changes

- **Bento Grid Alignment & Container Stability**:
  - Update the 2-column generator Bento grid alignment in `frontend/pages/quiz.vue` from `items-stretch` to `items-start` so each Bento card preserves its natural height without forcing sibling expansion.
  - Standardize the vertical spacing of the right Bento card (Seniority & Generation Controls) with predictable spacing (`space-y-6`), anchoring the primary "Tạo Bộ Đề AI" button at a constant position that does not jump when the book dropdown is toggled.
- **Topic Generation Fallback Resilience**:
  - Update `handleGenerateQuiz` in `frontend/pages/quiz.vue` to safely reference `computedQuickTopics.value?.[0] || 'Technical Architecture'` instead of the legacy `quickTopics[0]` reference.
- **Zero Breaking Changes**:
  - No backend changes, no database migrations, and no API contract modifications.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `quiz`: Update the Interview Quiz Studio Visual Layout & Interactive Tokens requirement to mandate shift-free Bento grid stability when toggling grounded book selection and resilient topic fallback resolution.

## Impact

- **Frontend**: `frontend/pages/quiz.vue` and `frontend/tests/pages/quiz.spec.ts`.
- **Specs**: `openspec/specs/quiz/spec.md`.
- **User Experience**: Toggling book-grounded mode expands only the context card with zero displacement or vertical jumping of the primary generation button.

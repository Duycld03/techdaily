# Tasks

## 1. Frontend: Generator Grid Alignment & Vertical Spacing

- [x] 1.1 In `frontend/pages/quiz.vue`, change the generator 2-column Bento grid container from `items-stretch` to `items-start` to decouple column heights and prevent sibling expansion.
- [x] 1.2 In `frontend/pages/quiz.vue`, simplify the right Bento card container from `flex flex-col justify-between` to standard vertical flow (`space-y-6`), anchoring the primary generate button at a constant relative position that does not jump when the book dropdown expands.

## 2. Frontend: Safe Topic Generation Fallback

- [x] 2.1 In `frontend/pages/quiz.vue`, update `handleGenerateQuiz` to safely fall back to `computedQuickTopics.value?.[0] || 'Technical Architecture'` instead of referencing undefined `quickTopics[0]`.

## 3. Verification & Automated Test Suite Execution

- [x] 3.1 Validate OpenSpec specifications and schema using `openspec validate --changes` and `openspec validate --specs`.
- [x] 3.2 Update and execute frontend unit tests (`npm test -- tests/pages/quiz.spec.ts` in `frontend/`) to assert top-aligned Bento layout (`items-start`) and safe topic fallback.
- [x] 3.3 Execute full frontend test suite (`npm test` in `frontend/`) and Nuxt production build (`npm run build` in `frontend/`) to ensure clean compilation and zero layout regression.

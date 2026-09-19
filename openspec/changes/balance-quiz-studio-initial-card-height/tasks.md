# Tasks

## 1. Frontend: Conditional Grid Alignment & Left Card Flex Distribution

- [x] 1.1 In `frontend/pages/quiz.vue`, bind conditional grid alignment `:class="['grid grid-cols-1 lg:grid-cols-2 gap-6', isGrounded ? 'items-start' : 'items-stretch']"` so both cards stretch equally when unselected and switch to top-alignment when book mode is toggled.
- [x] 1.2 In `frontend/pages/quiz.vue`, update the left Bento card container with `flex flex-col justify-between` so the topic section anchors at the top and the book toggle card anchors at the bottom matching the right card's height.

## 2. Verification & Automated Test Suite Execution

- [x] 2.1 Validate OpenSpec specifications and schema using `openspec validate --changes` and `openspec validate --specs`.
- [x] 2.2 Update and execute frontend unit tests (`npm test -- tests/pages/quiz.spec.ts` in `frontend/`) to assert conditional grid alignment and left card flex distribution.
- [x] 2.3 Execute full frontend test suite (`npm test` in `frontend/`) and Nuxt production build (`npm run build` in `frontend/`) to verify clean SSR compilation and zero layout regressions.

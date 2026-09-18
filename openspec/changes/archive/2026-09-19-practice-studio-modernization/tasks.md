# Tasks

## 1. Frontend - Interview Quiz Studio Modernization

- [x] 1.1 Modernize `/quiz` root container and top-level tab switcher (`generate`, `arena`, `review`, `stats`) with studio tokens.
- [x] 1.2 Modernize Quiz Generate Tab (topic inputs, book-grounded toggle, count chips, and level cards) with `.glass-card`.
- [x] 1.3 Modernize Quiz Arena Tab (question card, options A/B/C/D states, and explanation container) with refined dark glass.
- [x] 1.4 Modernize Quiz Summary, Mistake Review Queue, and Stats Bento Cards with hairline borders and unified badges.

## 2. Frontend - Spaced Repetition Review Studio Modernization

- [x] 2.1 Modernize `/review` root container and dual-mode tab switcher (`session`, `management`).
- [x] 2.2 Modernize Review Session Tab (interactive 3D flip card, ease rating buttons 1-4, and completion celebration card).
- [x] 2.3 Modernize Review Management Tab (3-card Bento overview, search & filter bar, and deck card list).
- [x] 2.4 Modernize Review Modals (Edit Card modal with edit/preview sub-tabs, Reset modal, and Delete modal) with `.glass-panel`.

## 3. Frontend - Error Boundary & Authentication Modernization

- [x] 3.1 Modernize `frontend/error.vue` (purging legacy emerald colors, using `.glass-panel` and Deep Iris CTA).
- [x] 3.2 Modernize `frontend/pages/login.vue` (clean glass paneling, mode switcher tabs, and refined submit actions).

## 4. Verification & Automated Tests

- [x] 4.1 Run frontend unit tests (`npm --prefix frontend test`) to verify zero regressions across all 52 test files.
- [x] 4.2 Run backend tests (`dotnet test backend/TechDaily.sln`) to ensure complete platform stability.
- [x] 4.3 Validate OpenSpec specifications (`openspec validate --changes` and `openspec validate --specs`).

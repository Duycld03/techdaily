# Tasks

## 1. Frontend - Studio Navigation & Top Control Bar

- [x] 1.1 Update `frontend/pages/today.vue` to implement Studio Control Bar with breadcrumb trail (`Book Title > Chapter Title > Slice N`), time-to-read badge, outline toggle, and scenario copilot dock toggle.
- [x] 1.2 Implement collapsible Left Rail (Outline Navigator) in `frontend/pages/today.vue` rendering book chapters/slices, progress checkmarks, and active slice styling.

## 2. Frontend - 3-Column Studio Workspace & Immersion Mode

- [x] 2.1 Update `frontend/pages/today.vue` layout to support responsive 3-column studio layout with collapsible right scenario dock and 100% width reader immersion mode.
- [x] 2.2 Modernize `frontend/components/today/DocReaderPane.vue` with elevated studio surfaces, hairline borders, and polished typography container.
- [x] 2.3 Modernize `frontend/components/today/InterviewChallengePane.vue` with glassmorphic option selector pills, violet active accents, and glowing trade-off evaluation cards.

## 3. Verification & Automated Tests

- [x] 3.1 Update and add unit test coverage for Studio Control Bar toggles and panel collapse states in `frontend/tests/`.
- [x] 3.2 Run frontend unit tests (`npm --prefix frontend test`) and backend tests (`dotnet test backend/TechDaily.sln`) to verify zero regressions.
- [x] 3.3 Validate OpenSpec specifications (`openspec validate --changes` and `openspec validate --specs`).

# Tasks: Today Reader Refinement & Architectural Trade-off Challenge

## 1. OpenSpec Planning & Branch Setup
- [x] Create feature branch `feat/today-reader-refinement-and-tradeoff-challenge` <!-- id: 1.1 -->
- [x] Write proposal, delta specs, and architectural design <!-- id: 1.2 -->

## 2. Frontend: Distraction-Free Reader Refinement
- [x] Remove `MicroQuizCard` import and component usage from `frontend/components/today/DocReaderPane.vue` <!-- id: 2.1 -->
- [x] Remove `.micro-quiz-container` DOM element and prune its selector from `handleMouseUp` <!-- id: 2.2 -->
- [x] Verify floating selection toolbar (Explain, Highlight, Copy) remains fully functional <!-- id: 2.3 -->

## 3. Frontend: Architectural Trade-off Challenge Styling & Feedback
- [x] Review `frontend/components/today/InterviewChallengePane.vue` to ensure robust rendering of Trade-off Scenario proposals and Principal Review explanations <!-- id: 3.1 -->
- [x] Verify i18n copy across English (`en.json`) and Vietnamese (`vi.json`) for Trade-off Challenge terminology <!-- id: 3.2 -->
- [x] Ensure responsive typography standards (`text-sm` on mobile, `text-base` / `text-lg` on desktop) are strictly observed <!-- id: 3.3 -->

## 4. Verification & Validation
- [x] Run full frontend test suite (`npm --prefix frontend test`) ensuring 100% pass rate <!-- id: 4.1 -->
- [x] Run backend test suite (`dotnet test backend/tests/TechDaily.Tests`) ensuring zero regressions <!-- id: 4.2 -->
- [x] Verify production build with `npm --prefix frontend run build` <!-- id: 4.3 -->

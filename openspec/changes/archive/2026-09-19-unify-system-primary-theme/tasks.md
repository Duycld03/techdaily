# Tasks

## 1. Frontend: Today Interview Challenge & Standalone Quiz

- [x] 1.1 In `frontend/components/today/InterviewChallengePane.vue`, replace emerald option border, ring, background, option badge, optimal choice badge, and feedback banner with system primary brand tokens (`brand-500`, `brand-600`, `bg-brand-50/80 dark:bg-brand-500/10`, `border-brand-500/60`, `text-brand-900 dark:text-brand-300`).
- [x] 1.2 In `frontend/pages/quiz.vue`, update option answered states, optimal choice badges, correct answer banner, readiness tier badges, accuracy progress bars, and empty review queue icons to use primary brand styling instead of emerald green.

## 2. Frontend: Spaced Repetition Flashcards & Deck Management

- [x] 2.1 In `frontend/components/review/Sm2GradingButtons.vue`, refactor ease rating button 4 ("Easy") from emerald styling to system primary brand styling (`border-brand-200/80 dark:border-brand-500/30`, `bg-brand-50/80 dark:bg-brand-500/10`, `hover:bg-brand-100/90 dark:hover:bg-brand-500/20`, `text-brand-700 dark:text-brand-300`).
- [x] 2.2 In `frontend/pages/review.vue`, update celebratory session completion hero card icon and status filter button for Mastered cards (`selectedStatus === 2`) to primary brand tokens.
- [x] 2.3 In `frontend/components/review/FlashcardBentoCard.vue` and `frontend/components/review/MasteryGaugeCard.vue`, update Mastered status badges and arc colors to system primary brand tokens.

## 3. Frontend: Roadmap Timeline & Hierarchical Mindmap

- [x] 3.1 In `frontend/pages/roadmap.vue`, update the overall progression bar gradient, completed chapter milestones, completed day badges, and completed slice card styling from emerald green to primary brand violet tokens (`bg-brand-600`, `text-brand-400`, `border-brand-500/40`, `from-brand-600 via-brand-500 to-brand-400`).
- [x] 3.2 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, refactor completed chapter branch nodes, slice leaf nodes, check indicators, and connecting SVG bezier edges from emerald to primary brand violet tokens (`stroke-brand-500 dark:stroke-brand-400`, `bg-brand-600 text-white`, `border-brand-400/60`, `text-brand-500`).

## 4. Frontend: Knowledge Graph 2D & 3D Mindmap
- [x] 4.1 In `frontend/components/graph/GraphCanvas.vue`, update Cytoscape 2D card node styling for Mastered status from `#10b981` (emerald) to `#7c3aed` (primary brand violet) and border color to `#c4b5fd`.
- [x] 4.2 In `frontend/components/graph/GraphCanvas3D.vue`, update WebGL 3D cosmos Mastered status node colors, glow materials, and tooltip interval telemetry to `#7c3aed`.
- [x] 4.3 In `frontend/components/graph/GraphMinimap.vue`, `frontend/components/graph/GraphLegend.vue`, `frontend/components/graph/GraphControlBar.vue`, and `frontend/components/graph/GraphDetailDrawer.vue`, update Mastered filter buttons, legend color indicators, and status badges from emerald green to primary brand violet tokens.

## 5. Frontend: Engineering Insights

- [x] 5.1 In `frontend/pages/insights.vue`, update the Solution code tab active button styling and benchmark telemetry stats badge to system primary brand violet tokens (`bg-brand-500/15 text-brand-700 dark:text-brand-300 border-brand-500/30`, `text-brand-500 fill-brand-500`).

## 6. Verification & Visual Smoke Testing

- [x] 6.1 Validate OpenSpec specifications and changes using `openspec validate --changes` and `openspec validate --specs`.
- [x] 6.2 Execute frontend test suite (`npm test` in `frontend/`) and verify Nuxt production build (`npm run build`).

# Proposal: Unify System Primary Theme Across Quiz, Review, Roadmap, Mindmap, and Insights

## Why

Multiple learning and analytics interfaces across TechDaily—including the Today interview challenge, standalone Quiz arena and review queue, Flashcard spaced repetition deck, Roadmap timeline, Mindmap canvas, Knowledge Graph cosmos, and Insights—currently render completed milestones, mastered nodes, optimal choices, and metric badges in mismatched emerald green (`#10b981`, `emerald-500`, `emerald-600`). This diverges from TechDaily's core Dev-Learning Studio brand identity centered on obsidian canvas tokens (`#09090b`) and primary violet brand accents (`brand-500` / `#7c3aed`, `brand-600`, `brand-400`). Unifying these surfaces to standard system primary brand tokens creates visual consistency, elevates aesthetic polish, and reinforces brand coherence across all core learning modes.

## What Changes

- **Today Interview Challenge & Daily Quiz**: Replace green/emerald option borders, selection rings, optimal choice badges, correct answer banners, and completed slice indicators with system primary brand tokens (`brand-500`, `brand-600`, `bg-brand-50 dark:bg-brand-950/40`, `border-brand-500/30`, `text-brand-600 dark:text-brand-400`).
- **Quiz Arena & Review Queue (`/quiz`)**: Migrate correct option highlights, readiness badges, mastered status indicators, mastery arc colors, accuracy progress bars, and review-queue empty state icons from emerald green to the system primary brand color palette.
- **Spaced Repetition Flashcards (`/review`)**: Align SM-2 grading button 4 ("Easy" / Perfect recall), mastered tier badges, mastery gauge arcs, and review completion celebration iconography to primary brand styling, replacing emerald green highlights.
- **Roadmap Timeline & Hierarchical Mindmap (`/roadmap`)**: Update progress bar gradient, completed chapter milestones, completed slice cards, pass badges, completed mindmap SVG connector edges, and completed mindmap chapter/slice nodes from emerald green to system primary brand tokens.
- **Knowledge Graph & 2D/3D Mindmap (`/graph`)**: Standardize mastered card and topic node background/glow colors (`#10b981` -> `#7c3aed`), legend indicators, mastery filter buttons, and detail drawer status badges to use primary brand styling.
- **Engineering Insights (`/insights`)**: Refactor solution code tab active pill, benchmark telemetry badge, and system design category badges from emerald green to primary brand tokens.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `quiz`: Update visual presentation requirements for correct answers, optimal choice indicators, mastered status badges, and today's interview challenge to use system primary brand tokens instead of emerald green.
- `roadmap`: Update timeline milestone indicators, completed slice badges, progress bars, and hierarchical mindmap nodes/edges to display completed learning states in system primary brand tokens instead of emerald green.
- `knowledge-graph`: Update 2D and 3D graph mastered node visual encoding, legend keys, mastery filters, and drawer badges to use system primary brand color tokens.
- `review`: Update SM-2 Grade 4 ("Easy") button, mastered tier indicators, and completion state icons to use system primary brand tokens.
- `insights`: Update solution tab active state and benchmark telemetry badges to use system primary brand tokens.

## Impact

- **Frontend Styling & UI Components**:
  - `frontend/pages/quiz.vue`, `frontend/components/today/InterviewChallengePane.vue`
  - `frontend/pages/review.vue`, `frontend/components/review/Sm2GradingButtons.vue`, `frontend/components/review/FlashcardBentoCard.vue`, `frontend/components/review/MasteryGaugeCard.vue`
  - `frontend/pages/roadmap.vue`, `frontend/components/roadmap/RoadmapMindmapCanvas.vue`
  - `frontend/components/graph/GraphCanvas.vue`, `frontend/components/graph/GraphCanvas3D.vue`, `frontend/components/graph/GraphLegend.vue`, `frontend/components/graph/GraphControlBar.vue`, `frontend/components/graph/GraphDetailDrawer.vue`, `frontend/components/graph/GraphMinimap.vue`
  - `frontend/pages/insights.vue`
- **Zero Breaking Changes**: No backend changes, no database migrations, and no API contract modifications.

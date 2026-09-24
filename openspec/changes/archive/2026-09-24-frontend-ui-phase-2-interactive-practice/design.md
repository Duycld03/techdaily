# Design: Frontend UI Phase 2 - Interactive Practice & Arena (StudioLayout & Preview Verification)

## Context

TechDaily's interactive practice surfaces (`quiz.vue` Arena mode and `review.vue` Tab 1 Flashcard Session) run active recall loops where cognitive load must remain focused on the technical content. Currently, these surfaces suffer from disproportionate stretching, cavernous dark voids, and layout shifts when answering questions or flipping flashcards.

The system layout architecture provides `StudioLayout.vue` (`frontend/components/layout/StudioLayout.vue`) featuring a two-column desktop cockpit:
- `#main` stage ($68\%$ width): Interactive focal surface (questions, code snippets, flashcard player).
- `#dock` companion ($32\%$ width): Session telemetry (timer, streak multipliers, SM-2 readouts, hotkeys).

Per `Requirement: Isolated Playground Sandbox Prototyping Invariant`, high-impact UI redesigns must be drafted and visually verified in `frontend/pages/playground/temp.vue` before production component cutover.

## Goals / Non-Goals

**Goals:**
- **Playground Preview Prototype**: Build an isolated dual-view prototype (`Quiz Arena` and `Flashcard 3D Session`) at `frontend/pages/playground/temp.vue` with mock data, capturing 1080p Light and Dark Obsidian mode screenshots for user approval.
- **Void Elimination in Flashcard Player**: Constrain flashcard height (`min-h-[320px] max-h-[520px]`), center card geometry (`max-w-2xl mx-auto`), and implement smooth 3D CSS perspective card flipping.
- **Zero-Shift OptionCard Component**: Refactor `OptionCard.vue` so that `default`, `hover`, `selected`, `correct`, and `incorrect` states maintain identical bounding box heights and border widths.
- **StudioLayout Standardization**: Wire `quiz.vue` and `review.vue` Tab 1 to `StudioLayout.vue` with consistent 68% / 32% proportions.
- **Complete i18n Telemetry**: Resolve raw unlocalized key `review.session_progress` in both English and Vietnamese locales, ensuring zero raw translation keys.

**Non-Goals:**
- No backend API modifications (all endpoints `/api/v1/quiz/*` and `/api/v1/review/*` remain intact).
- No domain SM-2 algorithm changes (mathematical formulas and intervals remain unchanged).
- No changes to Phase 3 (Reading Cockpit) or Phase 4 (Knowledge Graph / Roadmap).

## Decisions

### 1. Two-Stage Delivery: Playground Preview Prototyping Gate
```
+-------------------------------------------------------------------------+
| Stage 1: Playground Prototyping (frontend/pages/playground/temp.vue)   |
| - Mock Quiz Arena (68/32 StudioLayout) + Mock 3D Flashcard Player       |
| - Headless 1080p Screenshot capture (Light & Dark Obsidian)             |
| - User Inspection & Approval Gate                                       |
+-------------------------------------------------------------------------+
                                    │
                                    ▼ (Only upon user approval)
+-------------------------------------------------------------------------+
| Stage 2: Production Code Cutover & Store Wiring                         |
| - Port verified layout into quiz.vue, review.vue, OptionCard.vue        |
| - Pinia store binding (useQuizStore, useReviewStore)                    |
| - Vitest test suite execution & spec synchronization                    |
+-------------------------------------------------------------------------+
```
*Rationale*: Isolates visual experimentation from production state and prevents breaking test suites during design exploration.

### 2. Flashcard 3D Card Architecture & Void Elimination
```
+-------------------------------------------------------------------------+
| StudioLayout (#main stage: 68% width)                                   |
|                                                                         |
|   +-----------------------------------------------------------------+   |
|   | Flashcard Card Container (max-w-2xl mx-auto)                    |   |
|   | [perspective: 1000px]                                           |   |
|   |   +---------------------------------------------------------+   |   |
|   |   | 3D Flip Card (min-h-[340px] max-h-[500px])              |   |   |
|   |   | Front: Question, Category, Seniority, [Space] to flip   |   |   |
|   |   | Back: Explanation, Shiki Code, Source, SM-2 CTA Bar     |   |   |
|   |   +---------------------------------------------------------+   |   |
|   +-----------------------------------------------------------------+   |
|                                                                         |
|   CTA Bar: [1: Blackout] [2: Hard] [3: Good] [4: Easy]                  |
+-------------------------------------------------------------------------+
```
- Restrict excessive vertical height by setting `min-h-[340px] max-h-[500px]` with inner `overflow-y-auto` for long markdown explanations.
- Replace monolithic dark voids with subtle frosted surface styling: `glass-card p-6 border-slate-200/80 dark:border-white/[0.08] shadow-sm`.

### 3. OptionCard Zero-Layout-Shift State Hierarchy
```
State       | Background               | Border                        | Badge Fill
------------|--------------------------|-------------------------------|---------------------------
Default     | bg-white / bg-canvas-sub | border-slate-200 / border-w.. | bg-slate-100 / bg-white/5
Hover       | bg-slate-50 / bg-canvas  | border-slate-300 / border-w.. | bg-slate-200 / bg-white/10
Selected    | bg-brand-50/50 / brand.. | border-brand-500              | bg-brand-600 text-white
Correct     | bg-emerald-50/50 / em..  | border-emerald-500            | bg-emerald-600 text-white
Incorrect   | bg-rose-50/50 / rose..   | border-rose-500               | bg-rose-600 text-white
```
- All borders strictly use `border` ($1\text{px}$). Active and correct states adjust border color only, never border thickness.
- Font sizes and padding remain identical across states to guarantee zero layout shifts.

### 4. Telemetry Dock Component Layout
- **Quiz Arena Dock**: Live countdown ring/timer, current streak multiplier card (`Streak x3`), seniority badge, and horizontal question dot map.
- **Review Tab 1 Dock**: Session progress bar with localized title `$t('review.session_progress')`, review counts (`Due Today`, `New`, `Mastered`), next interval forecast preview, and keyboard shortcut legend.

## Risks / Trade-offs

- **Risk**: Keyboard shortcuts (`Space`, `1-4`) might accidentally trigger while typing in modal inputs or feedback dialogs.
  - **Mitigation**: Keyboard handlers verify `document.activeElement.tagName !== 'INPUT' && document.activeElement.tagName !== 'TEXTAREA'` before executing practice actions.
- **Risk**: Long markdown explanations or multi-line code snippets inside flashcards might push grading buttons off-screen.
  - **Mitigation**: Back face uses a flex column layout where the explanation area has `overflow-y-auto max-h-[320px]` and the SM-2 grading bar is pinned to the card bottom.

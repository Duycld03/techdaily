# Design: Modernize Spaced Repetition Flashcard Review UI

## Context

TechDaily's `/review` interface provides spaced repetition memory retention for software engineers. While deck management (`Tab 2`) recently received studio glass upgrades and advanced search filters, the active review session (`Tab 1`) suffers from an answer data leak (`card.topicSummary` is visible on the front of the card), an empty state template bug causing dual-card rendering, and outdated non-ergonomic interaction controls.

See `proposal.md` for motivation and background.

## Goals / Non-Goals

**Goals:**
- Enforce strict content separation between front (question/prompt only) and back (answer/code only) in `FlashcardDeck.vue`.
- Fix the missing `v-else` in `frontend/pages/review.vue` so the active review deck and completion hero card are strictly mutually exclusive.
- Implement Dev-Learning Studio 3D flip micro-interactions with Obsidian glass styling (`.glass-card`, dark canvas, hairline borders).
- Provide ergonomic keyboard navigation (`Space`/`Enter` to flip; `1`, `2`, `3`, `4` to grade) with automatic input shielding and unmount cleanup.
- Refine `Sm2GradingButtons.vue` with visual keyboard badges and fully localized bilingual descriptions.

**Non-Goals:**
- Changing backend SM-2 formulas, endpoints, or entity schemas (repetition count, ease factor, intervals, next review date).
- Modifying Deck Management (Tab 2) search filters, modals, or pagination logic.

## Decisions

### 1. Front vs Back Content Isolation & Layout Architecture
- **Problem**: `FlashcardDeck.vue` previously rendered `{{ card.topicSummary }}` right under `{{ card.topicTitle }}` before the user pressed "Show Answer", giving away the answer prematurely.
- **Decision**:
  - **Front Face**:
    - Top header: Category badge (`CategoryBadge`), Difficulty indicator, Repetition counter (`Repetition #X`), and SM-2 metadata pills (`EF: X.XX`, `Interval: Xd`).
    - Body: Question/Topic challenge (`card.topicTitle` or `card.frontMarkdown`).
    - Footer action: Large glass CTA button `[ 👁️ Xem Đáp Án / Show Answer ]` accompanied by a micro badge `[Space]`.
  - **Back Face**:
    - Top header: Same metadata context preserved for continuity.
    - Question reminder: Displayed as an elegant muted header to anchor context.
    - Core Insight Container: Highlighted glass panel (`dark:bg-canvas-subtle/80 border-white/[0.08]`) rendering `card.topicSummary` (or `card.backMarkdown`).
    - Deep Dive Section (if available): Rendered via `useMarkdownRenderer()` and `ShikiCodeBlock` for syntax-highlighted code walkthroughs (`card.topicDeepDiveMarkdown`).
    - Footer action: `Sm2GradingButtons` anchored at the bottom.

### 2. 3D Perspective Card Flip & CSS Animation
- **Decision**: Wrap the card surfaces in a 3D perspective container (`perspective: 1000px`) using CSS transforms (`transform-style: preserve-3d; transition: transform 0.4s cubic-bezier(0.4, 0, 0.2, 1); backface-visibility: hidden`).
- **Alternative Considered**: Instant boolean toggles (`v-if="!isFlipped"` vs `v-else`).
  - *Trade-off*: A 3D flip card feels tangible, engaging, and mirrors physical flashcards while preserving modern obsidian glass surfaces.

### 3. Ergonomic Keyboard Shortcuts
- **Decision**:
  - Register a `keydown` listener on `window` inside `onMounted` and remove it inside `onUnmounted`.
  - **Input Shielding**: If `e.target` is an `HTMLInputElement`, `HTMLTextAreaElement`, or has `isContentEditable`, keyboard events are ignored.
  - **Event Mapping**:
    - `isFlipped === false`: `Space` or `Enter` triggers flip to back face.
    - `isFlipped === true`:
      - Key `1` -> Grade 1 (Again: $Score = 1$)
      - Key `2` -> Grade 3 (Hard: $Score = 3$)
      - Key `3` -> Grade 4 (Good: $Score = 4$)
      - Key `4` -> Grade 5 (Easy: $Score = 5$)
  - Calling `gradeCard` automatically resets `isFlipped = false` for the incoming card.

### 4. SM-2 Grading Buttons Component Modernization (`Sm2GradingButtons.vue`)
- **Decision**:
  - Upgrade the 4-button grid to use semi-transparent glass borders and subtle glow accents (`border-rose-500/30`, `border-amber-500/30`, `border-sky-500/30`, `border-emerald-500/30`).
  - Add inline shortcut badges `[1]`, `[2]`, `[3]`, `[4]` next to or above the action labels.
  - Extract hardcoded strings ("Reset streak", "Interval x1.2", "Interval x2.5", "Bonus interval") into `en.json` and `vi.json`.

### 5. Template Fix in `frontend/pages/review.vue`
- **Decision**: Change the empty/completed state container condition from an unguarded `<div>` to `<div v-else class="w-full max-w-xl text-center ...">`. This guarantees clean mutual exclusion between the active card session and the celebratory completion hero card.

## Risks / Trade-offs

- **Markdown & Shiki Rendering on Back Face**:
  - *Risk*: Large code blocks could overflow or cause layout jank during 3D flip animation.
  - *Mitigation*: The back face container enforces `overflow-hidden` with responsive padding and max height constraints, ensuring code blocks inside `prose` scroll horizontally if needed without breaking card borders.
- **Keyboard Event Conflicts**:
  - *Risk*: Users navigating with screen readers or interacting with header controls might accidentally trigger grades.
  - *Mitigation*: Keys `1`-`4` only respond when `isFlipped` is active and no interactive input element holds focus.

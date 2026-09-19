# Proposal: Modernize Spaced Repetition Flashcard Review UI

## Why

During active flashcard review sessions (`/review`), the application suffers from two critical UI/UX flaws and an outdated visual presentation:
1. **Critical Flashcard Data Leak**: `frontend/components/review/FlashcardDeck.vue` renders `card.topicSummary` directly on the front face of the card before the card is flipped. This prematurely reveals the answer to the engineer, completely destroying active recall and defeating the purpose of spaced repetition memory training.
2. **Dual-Card Rendering Bug**: In `frontend/pages/review.vue`, the empty/completion state container lacks a `v-else` directive, causing the completion card (with a green `CheckCircle` icon) to render simultaneously beneath the active flashcard deck and peek out awkwardly at the bottom of the viewport.
3. **Outdated Flashcard Styling & Missing Keyboard Ergonomics**: The flashcard interface lacks the modern **Dev-Learning Studio** visual language (`.glass-card`, subtle glow, refined typography), lacks 3D flip card animations, lacks keyboard shortcuts for power users (`Space` to flip, `1`-`4` to grade), and contains hardcoded English subtext within the SM-2 grading buttons.

Addressing these issues now ensures engineers experience a seamless, immersive, and leak-free spaced repetition review workflow with studio-grade micro-interactions.

## What Changes

- **Fix Front-of-Card Content Isolation**: Restrict the front face of the flashcard strictly to the question/prompt (`card.topicTitle` or `card.frontMarkdown`), category badge, difficulty, repetition count, and SM-2 telemetry metadata. Move the core answer (`card.topicSummary` or `card.backMarkdown`) and technical deep-dive markdown (`card.topicDeepDiveMarkdown`) exclusively to the back face after flipping.
- **Fix Completion State Mutual Exclusivity**: Add a strict `v-else` guard to the completion state container in `frontend/pages/review.vue`, guaranteeing that the completion hero card is only rendered when `reviewStore.cards.length === 0` and no active card is present.
- **Dev-Learning Studio 3D Flip Card**: Redesign `FlashcardDeck.vue` with 3D perspective flip transitions (`transform-style: preserve-3d`), obsidian glass surfaces, standardized category pills, and responsive typography conforming to the platform's responsive typography invariant.
- **Full Keyboard Navigation Ergonomics**: Add global keyboard event listeners in `FlashcardDeck.vue`:
  - `Space` or `Enter`: Flip the card to inspect the answer.
  - Number keys `1` (Again), `2` (Hard), `3` (Good), `4` (Easy): Trigger immediate SM-2 grading when the card is flipped.
  - Automatically unbind keyboard listeners on component unmount or when typing inside form inputs.
- **Modernize SM-2 Grading Buttons (`Sm2GradingButtons.vue`)**:
  - Restyle grading buttons with semi-transparent glass borders and glowing accent tints (Rose for Again, Amber for Hard, Sky/Indigo for Good, Emerald for Easy).
  - Add visual keyboard shortcut badges (`[1]`, `[2]`, `[3]`, `[4]`).
  - Replace hardcoded English helper copy ("Reset streak", "Interval x1.2", "Interval x2.5", "Bonus interval") with dynamic i18n keys.
- **Bilingual Localization (i18n)**: Add comprehensive English and Vietnamese translation keys for flip instructions, answer section headings, and grading descriptions in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `review`: Update `Requirement: Spaced Repetition Dual-Mode Navigation` to enforce mutual exclusivity between the active flashcard and completion state, and update `Requirement: Spaced Repetition Studio Visual Layout & Modal Glass Surfaces` to specify front/back card content isolation, 3D flip perspective, keyboard shortcuts (`Space`, `1`-`4`), and bilingual SM-2 grading controls.

## Impact

- **API & Domain Contracts**: Zero breaking changes. `POST /api/v1/review/cards/{id}/grade` and `GET /api/v1/review/cards` remain untouched.
- **Frontend Architecture**: Improves `FlashcardDeck.vue`, `Sm2GradingButtons.vue`, and `review.vue` with pure Vue 3 composition API and keyboard lifecycle hygiene.
- **User Experience**: Completely eliminates the premature answer leak and dual-card visual collision while speeding up review throughput via keyboard controls.

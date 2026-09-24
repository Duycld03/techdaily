# Spec Delta: review (Phase 2 Interactive Practice)

## ADDED Requirements

### Requirement: Flashcard Active Practice Player 3D Flip & Void Elimination
The active review card player in `review.vue` Tab 1 and `FlashcardDeck.vue` SHALL eliminate excessive vertical stretching and dark voids on desktop displays ($W \ge 1280\text{px}$):
1. **Vertical Constraint & Centering**: The card container SHALL bound its height to `min-h-[320px] max-h-[520px]` centered horizontally (`max-w-2xl mx-auto`) with smooth 3D CSS perspective flip transformations (`perspective: 1000px`, `transform-style: preserve-3d`).
2. **Card Faces Organization**:
   - **Front Face**: Question/concept prompt, document category tag, seniority level badge, and clear flip affordance button (`[Space] Flip Card` / `[Phím cách] Lật thẻ`).
   - **Back Face**: Comprehensive explanation with Markdown rendering, Shiki-highlighted code excerpts, source book title badge, and SM-2 grading CTA bar.
3. **Zero Layout Shifts**: Card flipping and answer reveal MUST transition smoothly without expanding the outer container or clipping against the viewport fold.

#### Scenario: Reviewing a technical flashcard on desktop
- **WHEN** an engineer begins a review session on desktop ($1920\times1080$)
- **THEN** the active flashcard renders in a compact, centered card with zero empty dark voids and prominent front/back readability.

### Requirement: Flashcard Telemetry Dock Completeness & SM-2 Controls
The Tab 1 companion dock and grading controls SHALL provide complete localized session feedback:
1. **Session Progress Telemetry**: Localized header `$t('review.session_progress')` ("Session Progress" / "Tiến Độ Phiên Ôn Tập"), dynamic completion percentage, remaining review count, and visual progress bar.
2. **SM-2 Grading Button Bar**: When the card is flipped, the grading bar SHALL display 4 tactile buttons with explicit keyboard shortcuts:
   - `[1] Blackout` (Reset interval to 1 day)
   - `[2] Hard` (Slight interval decrease)
   - `[3] Good` (Standard SM-2 interval increase)
   - `[4] Easy` (Bonus interval increase)
3. **Keyboard Shortcut Legends**: Telemetry dock SHALL render persistent shortcut indicators (`Space` to flip, `1-4` to rate).

#### Scenario: Telemetry dock localization and grading
- **WHEN** viewing Tab 1 in Vietnamese locale
- **THEN** the telemetry dock renders "Tiến Độ Phiên Ôn Tập" without raw untranslated keys, and pressing key `3` records a "Good" rating.

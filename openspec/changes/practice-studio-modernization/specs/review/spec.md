# Spec Delta: review

## ADDED Requirements

### Requirement: Spaced Repetition Studio Visual Layout & Modal Glass Surfaces
The `/review` route SHALL implement the **Dev-Learning Studio** visual language across all interactive sessions, deck management views, and modal dialogs:

1. **Obsidian Canvas & Tab Bar:**
   - The root `/review` page SHALL render with `dark:bg-canvas` (`#09090b` obsidian base) with hairline divider borders (`border-white/[0.06]`).
   - The dual-mode tab switcher (`Review Session` / `Deck Management`) SHALL use refined glass styling consistent with the global studio standard.

2. **Interactive Flip Card & Rating Controls:**
   - The active SM-2 flip card container SHALL render with `.glass-card` styling, crisp font scaling, and smooth 3D flip animation.
   - Ease rating buttons (`Again`, `Hard`, `Good`, `Easy`) SHALL feature calibrated feedback colors without harsh opaque glare, indicating rating weights with subtle hairline borders.
   - The celebratory completion card SHALL use expanded padding and neutral obsidian glass.

3. **Deck Library & Modals Elevation:**
   - The 3-card Bento overview (`FlashcardHeroCard`, `MasteryGaugeCard`, `RetentionForecastCard`) and deck card library items SHALL use `.glass-card`.
   - Modals (Edit Card Modal with `edit`/`preview` tabs, Reset Progress Modal, Delete Card Modal) SHALL render with `.glass-panel` backdrop blur (`backdrop-blur-xl`), hairline borders (`border-white/[0.08]`), and dark inputs.

#### Scenario: User reviews flashcards in session mode
- **WHEN** user engages in an active flashcard review session
- **THEN** the flip card container renders with dark glass aesthetics and hairline borders
- **AND** ease grading buttons display clean, subtle color indicators without visual glare.

#### Scenario: User opens edit card modal in deck management
- **WHEN** user clicks edit on any card in the deck management table
- **THEN** the modal opens with a glass panel container
- **AND** the markdown edit/preview tabs toggle cleanly without opaque slate styling.

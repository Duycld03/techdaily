# Spec Delta: review

## ADDED Requirements

### Requirement: Flashcards Practice Studio Layout Integration
The `/review` page active review session (`activeTab === 'session'` with `currentCard !== null`) SHALL implement the `StudioLayout` archetype (`StudioLayout.vue`), replacing the solitary centered card layout with an integrated practice studio.
1. **Action Stage (Left Column)**:
   - The interactive flashcard player (`FlashcardDeck.vue`) SHALL render within the `#main` slot, centered within a maximum container width of `max-w-3xl` while filling the left 68% column.
2. **Telemetry Dock (Right Column)**:
   - The `#dock` slot SHALL render a companion telemetry dock containing:
     - **Daily Session Progress**: A compact card displaying completed cards versus total due cards with a percentage progress bar.
     - **SM-2 Scheduling Metrics**: Dynamic readouts of the current card's Ease Factor ($EF$), interval in days, repetition count, and mastery status tag.
     - **Keyboard Shortcuts Cheatsheet**: A compact guide showing `[Space]` to flip, `[1-4]` to grade (`Blackout`, `Hard`, `Good`, `Easy`), and `[E]` to edit.
     - **Source Architecture Context**: A reference block citing the original book title, chapter, and slice with a direct navigation link.

#### Scenario: Active Flashcard Practice on Desktop
- **WHEN** an engineer begins reviewing due flashcards on a desktop browser
- **THEN** the active flip-card renders in the main action stage on the left, while the session progress, SM-2 metrics, and hotkeys card render in the telemetry dock on the right.
- **AND** the entire viewport is balanced with zero wasted side margins and no vertical window scrolling required to access grading buttons.

#### Scenario: Reviewing on Mobile Devices
- **WHEN** an engineer reviews due flashcards on a mobile device ($< 1024\text{px}$)
- **THEN** the active flip-card occupies full viewport width, and dock telemetry widgets collapse cleanly below the card without obscuring flip or grading controls.

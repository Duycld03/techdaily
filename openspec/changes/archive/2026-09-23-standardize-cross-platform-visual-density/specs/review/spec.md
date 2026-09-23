# Spec Delta

## MODIFIED Requirements

### Requirement: Dev-Learning Studio Spaced Review & Flashcard Deck Layout Density
The `/review` route and interactive flashcard deck player (`FlashcardDeck.vue`) SHALL implement balanced layout density and compact geometry preventing bottom fold collisions:
1. **Flashcard Card Dimensions & Padding**: The interactive flashcard container SHALL use a minimum height of `min-h-[300px] sm:min-h-[320px]` and internal padding of `p-5 sm:p-6 rounded-2xl`, eliminating vertical padding bloat (`p-8`, `min-h-[400px]`).
2. **Challenge Heading Typography**: The flashcard challenge question text SHALL render at `text-lg sm:text-xl font-bold` with `leading-snug`, preventing oversized headings from pushing action controls offscreen.
3. **Action Row Spacing**: The flip action CTA and answer grading controls SHALL connect with a compact margin of `mt-5 pt-4`, keeping interactive buttons well above the viewport bottom.
4. **Page Container Padding**: The outer page container on `/review` SHALL use `p-4 sm:p-6 md:p-8` and `mb-4 sm:mb-6` for tab navigation, eliminating excessive `md:p-10` padding.

#### Scenario: Flashcard action button cleanly visible above viewport fold
- **WHEN** a user initiates a flashcard review session on a standard desktop viewport (1920x1080)
- **THEN** the active flashcard and the primary action button ("Show Answer Space" or SM-2 grading buttons) fit comfortably in the central viewport without crowding the browser bottom edge.

#### Scenario: Long technical challenge questions fit within card bounds
- **WHEN** a flashcard presents a multi-line technical architecture question
- **THEN** the card content scales gracefully without expanding beyond the screen fold or clipping action controls.

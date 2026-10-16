# Spec Delta

## MODIFIED Requirements

### Requirement: Flashcards Practice Studio Layout Integration
The active `/review` session SHALL retain a focused direct-canvas layout without redundant subheaders:
1. The flashcard player SHALL be the single hero in the left 68% desktop column, without outer truncated question snippets or repeated icon badges.
2. The right telemetry dock SHALL contain progress, SM-2 telemetry and hotkeys. Remaining-card counts SHALL appear only in the session tab and progress dock, not duplicate floating badges.
3. Deck/list and card mutation responses SHALL project authoritative category UUID/descriptor, difficulty and topic title from the source question, source chunk's book, or source highlight's book. No fixed Frontend or Senior fallback SHALL be used. Inactive assignments SHALL remain resolvable. A genuinely missing category provenance SHALL return nullable category metadata and a localized Unclassified state, never invent a replacement.
4. Category labels SHALL use current stored English/Vietnamese names after loading, grading, editing and resetting a card. Category metadata changes SHALL not alter SM-2 scheduling, repetition, ease, mastery or ownership.

#### Scenario: Active review session renders single hero card without outer sub-header
- **WHEN** the user opens the active session with due cards
- **THEN** the card appears directly below page tab navigation without a secondary title/icon header
- **AND** the complete question appears only inside the hero card, without prior truncation.

#### Scenario: Review deck accurately reflects question domain category and seniority
- **WHEN** the user reviews a card created from a question in any category, including an administrator-created subject
- **THEN** its category badge displays the authoritative descriptor in the current locale
- **AND** difficulty reflects the original question rather than a static default.

#### Scenario: Active Flashcard Practice on Desktop
- **WHEN** due cards are reviewed on desktop
- **THEN** the card occupies the main left action stage and progress, SM-2 metrics and hotkeys occupy the right dock
- **AND** the balanced viewport has no wasted side voids or required window scrolling to reach grading actions.

#### Scenario: Reviewing on Mobile Devices
- **WHEN** cards are reviewed below 1024px
- **THEN** the card uses full available width and telemetry stacks below without obscuring flip/grading controls.

#### Scenario: Card mutation retains authentic category
- **WHEN** an existing card is graded, edited or reset and the response replaces its client-side state
- **THEN** category UUID/descriptor still resolves from the same authoritative source rather than a hardcoded fallback
- **AND** reset changes only its documented scheduling state, not its category provenance.

#### Scenario: Inactive source category and orphaned source
- **WHEN** a card's source category is inactive
- **THEN** its authentic localized category remains visible and review remains available
- **WHEN** no surviving source can establish its category
- **THEN** the API returns nullable category fields and the UI shows localized Unclassified without assigning it to another subject.

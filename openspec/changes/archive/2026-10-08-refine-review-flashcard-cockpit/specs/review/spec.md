# Spec Delta: review

## MODIFIED Requirements

### Requirement: Flashcards Practice Studio Layout Integration
The `/review` page active review session (`activeTab === 'session'` with `currentCard !== null`) SHALL implement a focused, unboxed direct-canvas practice layout without redundant sub-headers:
1. **Direct-Canvas Action Stage (Left Column)**:
   - The interactive flashcard player (`FlashcardDeck.vue`) SHALL sit directly on the canvas as the single focal hero element in the left 68% column, without an enclosing outer sub-header row.
   - The application SHALL NOT render outer truncated title snippets (e.g. slicing the prompt markdown) or repeated icon badges above the flashcard container.
2. **Telemetry Dock (Right Column)**:
   - The right column companion dock SHALL house the session progress tracker, SM-2 algorithm telemetry, and keyboard shortcut legends.
   - Session remaining cards count SHALL be surfaced exclusively in the page navigation tab (`$t('review.tab_session')`) and the companion progress dock (`sessionProgress`), eliminating duplicate floating badges.
3. **Accurate Domain Category & Difficulty Metadata**:
   - The review deck endpoint (`GET /api/v1/review/deck`) and deck query endpoint (`GET /api/v1/review/cards`) SHALL accurately project `Category`, `Difficulty`, and `TopicTitle` from the card's underlying origin (such as `SourceQuizQuestion` or `SourceDocumentChunk`), prohibiting hardcoded fallback values (`FrontendWeb`, `Senior`).

#### Scenario: Active review session renders single hero card without outer sub-header
- **WHEN** the user navigates to `/review` with due review cards present in `activeTab === 'session'`
- **THEN** the active card is displayed directly under the page tab navigation without a secondary title/icon sub-header
- **AND** the question text appears solely within the flashcard hero container without prior truncation

#### Scenario: Review deck accurately reflects question domain category and seniority
- **WHEN** a user reviews a card created from an ASP.NET Core or Database quiz mistake
- **THEN** the card's category badge displays the authentic category (e.g., `BackendRuntime`, `DatabaseStorage`)
- **AND** the seniority level reflects the original question level rather than a static default

### Requirement: Flashcard Active Practice Player 3D Flip & Void Elimination
The interactive flashcard player (`FlashcardDeck.vue`) SHALL render clear provenance indicators directly on the front card face:
1. **Front Face Header**:
   - Along with Category and Difficulty chips, the card SHALL render a compact provenance badge identifying the card's origin:
     - `From Quiz Challenge` / `Từ Bài Trắc Nghiệm` when `sourceType === CardSourceType.QuizMistake`
     - `Reading Highlight` / `Từ Trích Đoạn` when `sourceType === CardSourceType.Highlight`
     - `Monograph Monograph` / `Tài Liệu Chuyên Khảo` when `sourceType === CardSourceType.DocumentChunk`
   - The repetition counter and SM-2 Ease Factor readout SHALL sit aligned on the opposite side of the card header.

#### Scenario: Card originating from quiz mistake displays quiz challenge provenance badge
- **WHEN** a flashcard originating from a quiz mistake is displayed on the front face
- **THEN** a provenance badge indicating its quiz challenge source is visible alongside the category badge

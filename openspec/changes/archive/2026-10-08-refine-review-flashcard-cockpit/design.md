# Design: Refined Review Flashcard Cockpit

## Context

See `proposal.md` for background and motivation.
Currently, `/review` renders an outer sub-header bar directly above `FlashcardDeck.vue` in `frontend/pages/review.vue`. This sub-header displays a third `<Layers>` icon badge (repeating the page header and tab switcher icons) and slices 50 characters of `frontMarkdown` as `cleanTopicTitle`, repeating the first words of the flashcard challenge text.
Furthermore, `GetReviewDeckHandler.cs` and `GetReviewCardsHandler.cs` hardcode `Category = Category.FrontendWeb` and `Difficulty = Difficulty.Senior` instead of reading from `c.SourceQuizQuestion` or `c.SourceDocumentChunk`.

## Goals / Non-Goals

**Goals:**
- Eliminate the redundant sub-header row in `pages/review.vue` to make `FlashcardDeck.vue` the single direct hero on canvas.
- Eliminate triple-icon rendering of `<Layers>` on `/review`.
- Accurately project `Category`, `Difficulty`, and `TopicTitle` from related entities in `GetReviewDeckHandler.cs` and `GetReviewCardsHandler.cs`.
- Add a compact, localized provenance badge inside `FlashcardDeck.vue` to indicate where the card originated (`From Quiz Challenge`, `Reading Highlight`, `Monograph Document`).

**Non-Goals:**
- Altering the SM-2 scheduling algorithm or repetition formula ($I_n = I_{n-1} \times \text{EF}$).
- Redesigning the right-column Telemetry Dock (`sessionProgress`, SM-2 readout, shortcuts).
- Modifying Deck Management (Tab 2) or Analytics & Stats (Tab 3) components beyond consuming accurate category/difficulty metadata.

## Decisions

### 1. Remove Outer Session Sub-Header Row in `pages/review.vue`
- **Decision**: In `frontend/pages/review.vue` (under `activeTab === 'session'`), delete the outer flex row containing `<Layers class="w-4 h-4" />`, `cleanTopicTitle`, `cleanSourceSubtitle`, and the duplicate "cards remaining" badge.
- **Rationale**: The remaining count is already shown on the navigation tab (`$t('review.tab_session')`) and inside the session progress dock. Slicing raw markdown at 50 chars produced broken titles (`Your team is architecting a new high-throughput, m`) that duplicated the question prompt. Removing this row yields a cleaner, distraction-free active recall environment.

### 2. Entity-Aware EF Core Projection in Review Handlers
- **Decision**: In `GetReviewDeckHandler.cs` and `GetReviewCardsHandler.cs`, project card metadata using conditional navigation properties:
  - `TopicTitle`: `c.SourceQuizQuestion != null ? c.SourceQuizQuestion.Topic : c.SourceDocumentChunk != null ? c.SourceDocumentChunk.ChapterTitle : (c.SourceHighlight != null && c.SourceHighlight.DocumentChunk != null ? c.SourceHighlight.DocumentChunk.ChapterTitle : "Technical Practice")`
  - `Category`: `c.SourceQuizQuestion != null ? c.SourceQuizQuestion.Category : (c.SourceDocumentChunk != null ? Category.SystemDesign : Category.FrontendWeb)`
  - `Difficulty`: `c.SourceQuizQuestion != null ? (Difficulty)c.SourceQuizQuestion.Level : Difficulty.Senior`
- **Rationale**: Prevents backend/database/system design questions from being tagged with `Frontend & Browser`. Enables authentic badge styling and filtering.

### 3. Surface Card Provenance Inside `FlashcardDeck.vue`
- **Decision**: Add a compact provenance chip in the top row of `FlashcardDeck.vue` next to the Category and Difficulty badges:
  - If `card.sourceType === 2` (`QuizMistake`): Badge with `<HelpCircle class="w-3 h-3" />` + `$t('review.source_quiz_mistake')` ("From Quiz Challenge" / "Từ Bài Trắc Nghiệm")
  - If `card.sourceType === 1` (`Highlight`): Badge with `<Highlighter class="w-3 h-3" />` + `$t('review.source_highlight')` ("Reading Highlight" / "Từ Trích Đoạn")
  - If `card.sourceType === 0` (`DocumentChunk`): Badge with `<BookOpen class="w-3 h-3" />` + `$t('review.source_monograph')` ("Monograph" / "Tài Liệu")
- **Rationale**: Gives context on card genesis directly inside the card boundaries without polluting the page canvas with a separate sub-header.

### 4. Align Frontend `ReviewCard` Interface
- **Decision**: Ensure `frontend/stores/useReviewStore.ts` defines `topicTitle?: string` on `ReviewCard` and handles both string and numeric `sourceType` representations.

## Risks / Trade-offs

- **Risk**: Null navigation references if EF Core queries don't include or project related tables.
  - **Mitigation**: EF Core `.Select(...)` translates null-safe member access directly into SQL `CASE WHEN ... THEN ... ELSE ... END` expressions without requiring `.Include()`. Verified via unit tests.
- **Risk**: Test breakage in existing `review.spec.ts` if tests assert the presence of `cleanTopicTitle` or `.truncate`.
  - **Mitigation**: Update frontend Vitest suites to test the direct presence of `FlashcardDeck` and absence of redundant headers.

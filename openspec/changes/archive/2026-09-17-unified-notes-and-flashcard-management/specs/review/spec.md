## Purpose
Provides a comprehensive spaced repetition review and flashcard deck management system (`/review`), featuring dual-mode review sessions (interactive 3D flip-card player for due cards) and complete deck library management (paginated filtering, card content editing, SM-2 progression reset, and soft deletion).

## ADDED Requirements

### Requirement: Spaced Repetition Dual-Mode Navigation
The `/review` page SHALL provide a top-level dual-mode tab switcher allowing software engineers to toggle seamlessly between the active review session ("Review Session" / "Ôn tập hôm nay") and the complete flashcard deck library ("Deck Management" / "Kho thẻ của tôi").

#### Scenario: User navigates between review session and deck management
- **WHEN** user loads `/review`
- **THEN** page defaults to the "Review Session" tab if due cards exist, or allows switching to "Deck Management"
- **WHEN** user clicks the "Deck Management" tab
- **THEN** client transitions to the deck management view and loads card statistics and the paginated card library without page reload.

#### Scenario: User completes review session
- **WHEN** user finishes grading all due cards in the "Review Session" tab
- **THEN** player displays a completion state with confetti celebration and provides a 1-click shortcut to inspect the full card deck in the "Deck Management" tab.

---

### Requirement: Flashcard Deck Library Querying & Metrics
The backend SHALL expose `GET /api/v1/review/cards` to retrieve a paginated list of flashcards belonging to the authenticated user, supporting optional keyword search across front and back markdown, status filtering (`Learning`, `Reviewing`, `Mastered`), and source type filtering (`Topic`, `Highlight`, `QuizMistake`). The response SHALL include deck statistics counting cards in each mastery status.

#### Scenario: User views deck statistics
- **WHEN** user views the "Deck Management" tab
- **THEN** UI displays counter summary cards for Total Cards, Learning, Reviewing, and Mastered calculated from the user's active flashcards.

#### Scenario: User searches and filters flashcard deck
- **WHEN** user inputs a search keyword (e.g. "PostgreSQL") and selects status filter "Learning" and source filter "Highlight"
- **THEN** client calls `GET /api/v1/review/cards?search=PostgreSQL&status=Learning&sourceType=Highlight&page=1&pageSize=20`
- **AND** table renders only matching flashcards with SM-2 metrics (Repetitions, Interval Days, Ease Factor, Next Review Date).

#### Scenario: User paginates through flashcard deck
- **WHEN** user clicks to advance to page 2 of the deck
- **THEN** client requests page 2 with existing search and filter criteria preserved, smoothly replacing table rows.

---

### Requirement: Flashcard Content Editing & Markdown Preview
The system SHALL expose `PUT /api/v1/review/cards/{id}` to update the `FrontMarkdown` and `BackMarkdown` of an existing flashcard. The frontend SHALL provide an edit modal with live split Markdown preview to verify question and answer formatting before saving.

#### Scenario: User edits flashcard markdown content
- **WHEN** user clicks "Edit" on a card in the deck table, updates front and back markdown in the modal, and clicks "Save Changes"
- **THEN** client sends `PUT /api/v1/review/cards/{id}` with new `frontMarkdown` and `backMarkdown`
- **AND** backend validates non-empty inputs, updates the card, and returns HTTP 200 OK with updated card DTO
- **AND** UI updates table row content and displays a success toast (`review.toast_update_success`).

#### Scenario: Validation fails on empty card content
- **WHEN** user submits the edit modal with empty front or back markdown
- **THEN** client or backend validation blocks submission and displays validation error feedback without updating database records.

---

### Requirement: Spaced Repetition Progression Reset
The system SHALL expose `POST /api/v1/review/cards/{id}/reset` to reset the SM-2 learning progression of a specific card ($RepetitionCount = 0, IntervalDays = 1, EaseFactor = 2.50, Status = Learning, NextReviewDate = Today$).

#### Scenario: User resets flashcard SM-2 progression
- **WHEN** user clicks "Reset Progress" on a card in the deck table and confirms the reset dialog
- **THEN** client sends `POST /api/v1/review/cards/{id}/reset`
- **AND** backend sets `RepetitionCount = 0`, `IntervalDays = 1`, `EaseFactor = 2.50m`, `Status = CardStatus.Learning`, and `NextReviewDate = Today`
- **AND** card immediately becomes due for today's review session, updating both deck stats and the review session badge.

---

### Requirement: Flashcard Soft Deletion
The system SHALL expose `DELETE /api/v1/review/cards/{id}` to soft-delete a flashcard by marking `IsDeleted = true` and `UpdatedAt = UtcNow`.

#### Scenario: User deletes a flashcard
- **WHEN** user clicks "Delete" on a card in the deck table and confirms the deletion modal
- **THEN** client sends `DELETE /api/v1/review/cards/{id}`
- **AND** backend calls `card.SoftDelete()`, updates database, and returns HTTP 200 OK or 204 No Content
- **AND** table removes the card row, updates deck statistics counters, and displays a success toast (`review.toast_delete_success`).
- **AND** the deleted card is filtered out of all subsequent review queries via EF Core global query filters.

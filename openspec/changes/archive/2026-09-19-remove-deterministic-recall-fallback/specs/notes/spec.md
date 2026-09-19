# Spec Delta: Notes

## MODIFIED Requirements

### Requirement: Deliberate Flashcard SM-2 Creation from Notes
Each highlight card in `/notes` SHALL feature a deliberate "Flashcard SM-2" action button to convert the excerpt and reflection into an active recall spaced repetition card (`POST /api/v1/review/cards/from-highlight`). Flashcard synthesis SHALL be strictly fail-fast: the backend SHALL invoke AI active recall synthesis and, if the external AI service fails (e.g. network timeout, rate limits, unconfigured key, or provider errors), the system SHALL immediately return an error result without persisting any synthetic fallback cards into the database, preserving data integrity and preventing review deck pollution.

Flashcard creation state SHALL be persistently reflected on the highlight card: upon card creation or initial page load where `HasFlashcard = true`, the action button transitions to a disabled badge displaying `<Check />` and localized text `notes.in_sm2` ("Đã Trong SM-2" / "In SM-2"), preserving state across browser refreshes. When card creation fails due to AI downtime or timeouts, the action button SHALL remain active in the unconverted state, allowing the user to retry when the service recovers.

#### Scenario: User creates SM-2 flashcard with successful AI synthesis
- **WHEN** user clicks "Flashcard SM-2" on a highlight card in `/notes` and the AI service returns successful card synthesis
- **THEN** client invokes `POST /api/v1/review/cards/from-highlight` with `highlightId` and current user `locale`
- **AND** backend creates a `SpacedRepetitionCard` populated with the synthesized front prompt and back answer
- **AND** persists the card linked to the user and highlight in the database
- **AND** frontend displays a success toast (`notes.toast_flashcard_success`) and marks the button as created with the `In SM-2` check badge.

#### Scenario: User attempts to create SM-2 flashcard when AI service fails or times out (Fail-Fast with No Database Writes)
- **WHEN** user clicks "Flashcard SM-2" on a highlight card in `/notes` and the AI service times out, exceeds rate limits (429), or returns an error
- **THEN** backend aborts execution and immediately returns an error failure result (HTTP 400 Bad Request)
- **AND** does NOT insert, create, or persist any `SpacedRepetitionCard` records in the database, ensuring zero junk data
- **AND** frontend catches the API failure and presents a localized error toast (`notes.toast_flashcard_error`) to inform the user
- **AND** the highlight card action button remains in the active `⚡ Flashcard SM-2` state without transitioning to the disabled `In SM-2` state, enabling the user to retry later.

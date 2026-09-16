# Reader Capability Delta Specification

## Purpose
Defines delta requirements for Vue 3 Composition API lifecycle conformity in the reading notes and highlights management view (`/notes`), ensuring that composables like `useI18n()` are initialized and destructured at top-level component setup rather than invoked inside asynchronous event handlers.

---

## ADDED Requirements

### Requirement: Highlight Notes System & Flashcard Generation
The Notes management interface (`/notes`) SHALL support converting reading highlights into active recall SM-2 flashcards via `POST /api/v1/review/cards/from-highlight`. Composable utilities (such as `useI18n`, `useToast`, `useReviewStore`, `useApiError`) SHALL be initialized and destructured exclusively at the synchronous top level of the `<script setup>` block in accordance with Vue 3 Composition API injection lifecycle constraints. Asynchronous event callbacks SHALL NOT invoke dependency-injecting composables inline.

#### Scenario: User generates flashcard from highlight in notes view
- **WHEN** an authenticated user clicks "Flashcard SM-2" on any saved reading highlight in `/notes`
- **THEN** the handler reads the synchronously captured `locale` ref without triggering a `[vue-i18n] Not found injection "vue-i18n"` runtime exception
- **AND** the client invokes `POST /api/v1/review/cards/from-highlight` passing the highlight ID and resolved locale string
- **AND** upon successful generation, displays the localized success toast (`notes.toast_flashcard_success`) and marks the highlight card as generated.

#### Scenario: Error handling during flashcard generation in notes view
- **WHEN** the backend returns an error or network connection fails during flashcard creation from `/notes`
- **THEN** the handler catches the error, releases the loading lock (`creatingCardHighlightId = null`), and displays a localized error toast (`notes.toast_flashcard_error`) without unhandled client crashes.

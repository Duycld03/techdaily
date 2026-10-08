# Proposal

## Why

Currently, once a book or document is imported into the library (`/library`), users cannot update its metadata (Title, Author/Source, or Category). If a user forgets to select or accidentally misselects the category during ingestion (for example, placing *Atomic Habits* into *Backend & Runtime* instead of *Habits & Deep Work*), their only recourse is to completely delete the book and re-upload the entire document from scratch.

Furthermore, an incorrect category selection cascades into the Knowledge Graph (`/graph`), anchoring nodes to the wrong knowledge pillar. Providing a first-class book management and editing capability solves this critical UX friction, preserves user reading history, and guarantees taxonomy integrity across the learning platform.

## What Changes

- **Backend Book Update Use Case & API (`PATCH /api/v1/library/books/{id}`)**:
  - Introduce `UpdateBookHandler`, `UpdateBookRequest`, and `UpdateBookValidator`.
  - Allow authorized owners (`CreatedByUserId == currentUserId`) to update `Title`, `AuthorOrSourceUrl`, `Category`, and `Language` (`"en"` | `"vi"`).
  - Validate that `Title` is non-empty ($\le 255$ chars), `Category` is a valid DeepPace enum ($0..6$), and `Language` is a supported locale code (`"en"` or `"vi"`).
  - Enforce strict ownership boundaries, returning `403 Forbidden` if the book belongs to another user and `404 Not Found` if deleted or non-existent.
  - Automatically cascade category updates to the book's associated domain artifacts (synchronizing chunk and drill classifications, ensuring immediate re-alignment in the Knowledge Graph projection) and cascade language updates to all `DocumentChunk` slices (ensuring AI curation and Audio narration align with the updated document language).
- **Frontend Book Management UI & Design System Alignment (`library.vue`)**:
  - Add an Edit action button (pencil icon) to book cards in the Library view alongside the Delete button.
  - Provide an accessible teleported edit modal (`EditBookModal` / studio modal dialog adhering to Pillar 2 design standards):
    - Use accessible `AppSelect.vue` for Category (all 7 DeepPace domains) and Document Language (`en` / `vi`), strictly prohibiting raw `<select><option>`.
    - Standard styled input fields for Title and Author/Source.
    - Information callout explaining that changing the category re-aligns the Knowledge Graph and flashcard taxonomy.
    - Submit and Cancel buttons with loading state (`isSaving`), error toasts, and bilingual localization (`en-US` and `vi-VN`).
    - Enforce responsive typography ($\ge 14\text{px}$ mobile, $\ge 16\text{px}$ desktop) and `whitespace-nowrap shrink-0` on action buttons to prevent layout breakage.

## Capabilities

### Modified Capabilities
- `library`: Introduce book metadata and category editing endpoint (`PATCH /api/v1/library/books/{id}`) and interactive edit modal on `/library`.

## Impact

- **Backend**:
  - Handler: `backend/src/TechDaily.Application/Features/Library/UpdateBook/UpdateBookHandler.cs`
  - Request / Validator: `UpdateBookRequest.cs`, `UpdateBookValidator.cs`
  - Endpoint: `backend/src/TechDaily.Api/Endpoints/LibraryEndpoints.cs`
  - Tests: `backend/tests/TechDaily.Tests/Application/UpdateBookHandlerTests.cs`
- **Frontend**:
  - Library view & modal: `frontend/pages/library.vue` (or `app/pages/library.vue`)
  - Localization: `frontend/i18n/locales/en.json`, `frontend/i18n/locales/vi.json`
  - Tests: `frontend/tests/pages/library.spec.ts`

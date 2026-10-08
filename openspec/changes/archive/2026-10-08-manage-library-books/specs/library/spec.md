# Spec Delta

## ADDED Requirements

### Requirement: Book Metadata and Category Update API

The system SHALL expose an authenticated HTTP PATCH endpoint `PATCH /api/v1/library/books/{id}` allowing users to update the metadata and domain category of books they have imported into their library.

1. **Authentication & Authorization**:
   - The endpoint SHALL require valid JWT Bearer authentication (`.RequireAuthorization()`) and return `HTTP 401 Unauthorized` for unauthenticated requests.
   - The endpoint SHALL verify that the requesting user is the owner of the book (`CreatedByUserId == currentUserId`). If the user does not own the book or `CreatedByUserId` is null, the endpoint SHALL return `HTTP 403 Forbidden` with RFC 7807 problem details and error code `LIBRARY_FORBIDDEN`.
2. **Resource Existence & Soft Delete**:
   - If the book ID does not exist or has `IsDeleted == true`, the endpoint SHALL return `HTTP 404 Not Found`.
3. **Payload Validation**:
   - The payload SHALL accept `Title` (string, required, length 1–255), `AuthorOrSourceUrl` (string, optional, max length 500), `Category` (integer enum, required, range 0–6), and `Language` (string, optional, allowed values: `"en"`, `"vi"`).
   - If `Title` is whitespace or exceeds 255 characters, `Category` is outside the defined enum range ($0..6$), or `Language` is specified with an unsupported locale, the system SHALL return `HTTP 400 Bad Request` with validation error details.
4. **Cascade Taxonomy and Language Alignment**:
   - Updating `Category` SHALL persist the new `Category` on `DocumentBook` and synchronize the book's slug and associated learning artifacts.
   - Subsequent calls to `GET /api/v1/graph` for the user SHALL immediately reflect the new pillar connection (`pillar-{Category}`), moving the book and its chunks to the updated DeepPace constellation without dangling edges.
   - Updating `Language` SHALL cascade and persist `chunk.Language = request.Language` on all surviving `DocumentChunk` records belonging to the book, ensuring subsequent AI curation (`CurateSliceHandler`) and TTS narration (`GetOrSynthesizeChunkAudioHandler`) operate under the updated language code.
5. **Response Contract**:
   - Upon successful update, the endpoint SHALL return `HTTP 200 OK` with the updated `BookDto`.

#### Scenario: Owner updates book title, author, and category
- **WHEN** an authenticated user who owns book `123` submits `PATCH /api/v1/library/books/123` with `Title = "Atomic Habits (Bản Đầy Đủ)"`, `AuthorOrSourceUrl = "James Clear"`, and `Category = 6` (`HabitsProductivity`)
- **THEN** the system returns `HTTP 200 OK` with updated `BookDto`
- **AND** `DocumentBook.Category` is updated to `Category.HabitsProductivity`
- **AND** the book's `Title` is updated in the database.

#### Scenario: Non-owner attempts to update book
- **WHEN** an authenticated user attempts to update a book created by a different account
- **THEN** the system returns `HTTP 403 Forbidden` with error code `LIBRARY_FORBIDDEN`
- **AND** no book data is modified.

#### Scenario: Update with invalid title or category
- **WHEN** user submits `PATCH /api/v1/library/books/{id}` with an empty title or invalid category integer `99`
- **THEN** the system returns `HTTP 400 Bad Request` with descriptive validation errors.


#### Scenario: Owner updates book language and cascades to document chunks
- **WHEN** an authenticated user who owns book `123` submits `PATCH /api/v1/library/books/123` with `Language = "vi"`
- **THEN** the system returns `HTTP 200 OK`
- **AND** all existing `DocumentChunk` records belonging to book `123` have their `Language` updated to `"vi"`
- **AND** subsequent TTS audio generation requests for chunks in this book select the Vietnamese neural voice.
---

### Requirement: Book Management and Edit Interface in Library

The Document Library interface (`pages/library.vue`) SHALL provide an accessible, responsive book editing modal dialog complying with the **Dev-Learning Studio** UI design system and Pillar 2 component governance.

1. **Card Action Trigger**:
   - Each book card owned by the user SHALL display an Edit button (`Pencil` icon, `library.edit_book`) alongside the Delete button.
   - The button SHALL have responsive padding and an accessible label/tooltip.
2. **Accessible Edit Modal (`EditBookModal`)**:
   - Clicking Edit SHALL open a teleported dialog adhering to `AppModal.vue` conventions (strictly prohibiting native browser `prompt` or `confirm` dialogs).
   - **Category & Language Dropdowns**: The modal MUST use custom accessible `AppSelect.vue` to render all 7 DeepPace categories and document language choices (`en` / `vi`), strictly prohibiting raw HTML `<select><option>` elements.
   - **Form Fields**: Accessible text inputs for Book Title and Author / Source URL.
   - **Taxonomy Callout**: A responsive informational banner explaining: *"Thay đổi chuyên mục sẽ cập nhật lại Cột trụ Đồ thị Tri thức và phân loại thẻ nhớ tương ứng"* (Vietnamese) / *"Updating the category re-aligns Knowledge Graph constellations and spaced repetition cards"* (English).
   - **Action Buttons**: "Hủy" (`library.cancel`) and "Lưu Thay Đổi" (`library.save_changes`) with `whitespace-nowrap shrink-0` and responsive gap, displaying a loading spinner during submission (`isSaving`).
3. **Optimistic / Immediate UI Synchronization**:
   - Upon successful save, the modal closes, a success toast notification appears (`toast.success`), and the book card on `/library` updates its title, category badge, and metadata immediately without requiring a full page refresh.

#### Scenario: User opens edit modal and updates category
- **WHEN** user clicks the Edit button on a book card in `/library`
- **THEN** the modal opens with current book Title, Author, and Category pre-populated in `AppSelect`
- **WHEN** user selects "Thói Quen & Tập Trung Sâu" (`Category.HabitsProductivity`) and clicks "Lưu Thay Đổi"
- **THEN** the modal shows a loading spinner
- **AND** upon API success, the modal closes, a success toast is displayed, and the book card badge updates to "Thói Quen & Tập Trung Sâu".

#### Scenario: Responsive and bilingual button layout
- **WHEN** the edit modal renders in both English and Vietnamese locales across Mobile ($390\text{px}$) and Desktop ($1440\text{px}$) viewports
- **THEN** all action buttons maintain `whitespace-nowrap shrink-0` with zero text truncation or button collision.

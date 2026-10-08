# Design

## Context

See `proposal.md` for motivation. In the current system:
- `DocumentBook` has `Title`, `Slug`, `Category`, `AuthorOrSourceUrl`, `CreatedByUserId`, `IsDeleted`.
- `GetBooksHandler` lists books for the authenticated user (`CreatedByUserId == currentUserId`).
- `DeleteBookHandler` verifies ownership (`CreatedByUserId == currentUserId`) and sets `IsDeleted = true`.
- However, there is no update endpoint or use-case handler.
- In `frontend/pages/library.vue`, book cards render the title, author/source, category badge, and a delete button, but no edit capability exists.
- In addition, users need to manage book categories because category choice directly controls how `GetKnowledgeGraphQueryHandler` links the book: `edges.Add(new GraphEdgeDto(..., Target: $"pillar-{book.Category}"))`.

## Goals / Non-Goals

**Goals:**
- Implement `UpdateBookHandler` following the project's Clean Architecture with Plain Use-Case Handlers (Pure DI) and Result Pattern.
- Expose `PATCH /api/v1/library/books/{id}` in `LibraryEndpoints.cs`.
- Validate that the caller is the owner (`CreatedByUserId == currentUserId`), returning `403 Forbidden` (`LIBRARY_FORBIDDEN`) otherwise.
- Validate payload via FluentValidation (`UpdateBookValidator`): `Title` (1..255 chars), `AuthorOrSourceUrl` ($\le 500$ chars), `Category` in enum range ($0..6$).
- When `Category` changes, update `DocumentBook.Category`.
- On frontend, integrate an Edit button on book cards in `/library`.
- Implement an edit modal dialog complying with AGENTS.md Pillar 2:
  - Custom accessible `AppSelect.vue` for all 7 DeepPace categories (strictly prohibiting raw `<select><option>`).
  - Teleported modal structure (strictly prohibiting native browser `prompt`/`confirm`).
  - Responsive typography ($\ge 14\text{px}$ mobile, $\ge 16\text{px}$ desktop).
  - Bilingual responsive buttons with `whitespace-nowrap shrink-0`.
- Optimistically and reactively update the book card in the Pinia `useLibraryStore` or local state.

**Non-Goals:**
- Editing individual chunk text or re-running the PDF extraction parser.
- Transferring book ownership between users.
- Re-generating vector embeddings (embeddings are tied to chunk text and summary, not the book title).

## Decisions

### 1. Handler & DTO Contracts
We define:
```csharp
public record UpdateBookRequest(
    Guid BookId,
    Guid UserId,
    string Title,
    string? AuthorOrSourceUrl,
    Category Category,
    string? Language = null);

public class UpdateBookResponse
{
    public BookDto Book { get; set; } = null!;
}
```

Validation via FluentValidation:
- `RuleFor(x => x.Title).NotEmpty().MaximumLength(255);`
- `RuleFor(x => x.AuthorOrSourceUrl).MaximumLength(500);`
- `RuleFor(x => x.Category).IsInEnum();`
- `RuleFor(x => x.Language).Must(l => l == null || l == "en" || l == "vi").WithMessage("Language must be either 'en' or 'vi'.");`

Logic in `UpdateBookHandler`:
1. Find book in DB: `await _dbContext.DocumentBooks.FirstOrDefaultAsync(b => b.Id == request.BookId && !b.IsDeleted, cancellationToken)`. If null, return `Error.NotFound`.
2. Check ownership: `if (book.CreatedByUserId != request.UserId) return Error.Custom("LIBRARY_FORBIDDEN", "You do not have permission to modify this book.");`
3. Update fields:
   - `book.Title = request.Title.Trim();`
   - `book.AuthorOrSourceUrl = string.IsNullOrWhiteSpace(request.AuthorOrSourceUrl) ? null : request.AuthorOrSourceUrl.Trim();`
   - `book.Category = request.Category;`
   - `book.UpdatedAt = DateTime.UtcNow;`
4. Cascade language if specified:
   - If `!string.IsNullOrWhiteSpace(request.Language)`:
     - Update all `DocumentChunks` belonging to the book: `chunk.Language = request.Language; chunk.UpdatedAt = DateTime.UtcNow;`
5. Commit: `await _dbContext.SaveChangesAsync(cancellationToken);`
6. Return updated `BookDto`.
### 2. Frontend Component Architecture & Design System Adherence
In `pages/library.vue`:
- **Action menu / trigger**:
  - Inside each book card's actions area, alongside the Delete icon button, add an Edit icon button with a pencil glyph (`Pencil` from `lucide-vue-next`).
  - Button styling: `p-2 rounded-xl text-slate-400 hover:text-brand-500 hover:bg-slate-100 dark:hover:bg-white/[0.06] transition-colors`.
- **Modal state**:
  - `isEditModalOpen = ref(false)`
  - `editingBook = ref<BookDto | null>(null)`
  - `editForm = reactive({ title: '', author: '', category: 0 })`
  - `isSaving = ref(false)`
- **Category & Language Dropdowns**:
  - Uses accessible `AppSelect.vue` with `categoryOptions` (all 7 DeepPace categories) and `languageOptions` (`en` / `vi`).
  - Strictly prohibits `<select><option>`.
- **Information Callout Banner**:
  - Soft amber/indigo callout informing the user about Knowledge Graph alignment.
- **Action Buttons**:
  - Cancel (`library.cancel`) and Save Changes (`library.save_changes`) with loading spinner and `whitespace-nowrap shrink-0`.

### 3. Internationalization
Add locale keys in `frontend/i18n/locales/en.json` and `vi.json`:
- `library.edit_book`: "Edit Book" / "Chỉnh Sửa Sách"
- `library.edit_book_title`: "Edit Book Details" / "Chỉnh Sửa Thông Tin Sách"
- `library.save_changes`: "Save Changes" / "Lưu Thay Đổi"
- `library.book_updated_success`: "Book details successfully updated" / "Đã cập nhật thông tin sách thành công"
- `library.edit_category_hint`: "Changing category re-aligns Knowledge Graph constellations and flashcard taxonomy." / "Thay đổi chuyên mục sẽ cập nhật lại Cột trụ Đồ thị Tri thức và phân loại thẻ nhớ tương ứng."

## Risks / Trade-offs

- **Risk: Nuxt 4 Directory Restructuring Conflict**:
  - *Mitigation*: We are currently in Planning Only (`openspec-propose`), writing planning files in `openspec/changes/manage-library-books/`. No frontend source code is touched until the Nuxt 4 directory migration in the parallel session finishes, avoiding any git merge conflicts.
- **Risk: Knowledge Graph Synchronization Latency**:
  - *Trade-off*: `GetKnowledgeGraphQueryHandler` computes edges dynamically on each query from `DocumentBook.Category`. Once `book.Category` is updated in Postgres, the next `GET /api/v1/graph` request immediately pulls the updated category and links to `pillar-{Category}` with zero cache invalidation lag.

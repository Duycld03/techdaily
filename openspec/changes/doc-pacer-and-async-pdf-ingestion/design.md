# Design: Doc Pacer Engine & Asynchronous Large PDF Ingestion

## 1. Architectural Overview & Component Topology

```
┌───────────────────────────────────────────────────────────────────────────────────────────┐
│                                   FRONTEND (Nuxt 4 / Vue 3)                              │
│                                                                                           │
│     /today (Pacer Bar)          /today (Dual-Pane)                    /roadmap            │
│   ┌─────────────────────┐    ┌──────────────┬──────────────────┐    ┌───────────────────┐ │
│   │ [DDIA ▼] Ch 3: S4/12│    │ DocReader    │ Interview        │    │ Active Book       │ │
│   │ Prev [Next] (18%)   │    │ (Zero-Wait)  │ Challenge /      │    │ Chapter Milestone │ │
│   └──────────┬──────────┘    │              │ AI Synthesis Card│    │ Progression       │ │
│              │               └──────────────┴──────────────────┘    └───────────────────┘ │
└──────────────┼────────────────────────────────────────────────────────────────────────────┘
               │ HTTP REST
┌──────────────▼────────────────────────────────────────────────────────────────────────────┐
│                             BACKEND (.NET 10 / ASP.NET Core)                              │
│                                                                                           │
│   Library Endpoints              Daily Pacer Endpoints            Background Worker       │
│   ┌────────────────────────┐     ┌───────────────────────┐       ┌──────────────────────┐ │
│   │ POST /upload-pdf (202) │     │ GET /today-pacer      │       │ BackgroundService    │ │
│   │ GET /books/{id}/status │     │ POST /switch-book     │       │ Channels<IngestTask> │ │
│   └──────────┬─────────────┘     └──────────┬────────────┘       │ Channels<PriorityJIT>│ │
│              │                              │                    └──────────┬───────────┘ │
│              ▼                              ▼                               ▼             │
│   ┌────────────────────────┐     ┌───────────────────────┐       ┌──────────────────────┐ │
│   │ Disk Spooling Stream   │     │ PostgreSQL 17         │◄──────┤Gemini 3.5 Flash-Lite │ │
│   │ (80KB buffer, Zero-LOH)│     │ (Books, Chunks, Pacer)│       │ (Look-Ahead Buffer)  │ │
│   └────────────────────────┘     └───────────────────────┘       └──────────────────────┘ │
└───────────────────────────────────────────────────────────────────────────────────────────┘
```

## 2. Ingestion Pipeline & PDF Bookmarks Engine
1. **Zero-LOH Disk Spooling:**
   - Upload requests stream directly to `/tmp/techdaily-uploads/{bookId}.pdf` via 80KB chunks.
   - Endpoint sets `[RequestSizeLimit(314_572_800)]` (300 MB) and returns HTTP 202 immediately with initial book record.
2. **Asynchronous Channel Processing:**
   - `System.Threading.Channels.Channel<PdfIngestJob>` processes uploads sequentially in the background.
   - Worker extracts bookmarks tree via `PdfPig.Structure` / `GetOutline()`.
   - Chapters are segmented by bookmark headings; pages prior to the first chapter bookmark (front-matter) and trailing index pages are automatically skipped.
   - Progress percentage is updated every $N$ pages: `(processedPages / totalPages) * 100`.
   - Upon completion, the temporary disk file is deleted and `ProcessingStatus` is marked `Ready`.

## 3. Look-Ahead JIT Pre-Generation Engine
1. **Sliding Buffer Architecture:**
   - On initial book readiness, Gemini synthesizes scenarios for Chunks 1, 2, and 3.
   - As the user reads Chunk $N$, a background service ensures Chunks $N+1$, $N+2$, and $N+3$ exist in the database.
2. **Priority Queue Promotion on Rapid Navigation:**
   - If user skips to Chunk $K$ that has no generated challenge:
     - The reader loads Chunk $K$ immediately from PostgreSQL (<10ms).
     - The challenge pane displays the `AISynthesisSkeletonCard` component.
     - The backend promotes Chunk $K$ to the head of `PriorityChannel<ScenarioGenTask>`.
     - Gemini 3.5 Flash-Lite (`gemini-3.5-flash-lite`) completes synthesis in ~1.5s, sending the result to the client via polling/SSE to smoothly fade into the interactive scenario.

## 4. Database Schema Modifications

### Updated: `DocumentBook` Entity
```csharp
public class DocumentBook : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public Category Category { get; set; }
    public int TotalChunks { get; set; }
    public string? AuthorOrSourceUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; } = false; // Primary sample book for new visitors
    
    // Ingestion status
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public int ProgressPercentage { get; set; } = 0;
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
```

### New: `UserBookPacer` Entity
```csharp
public class UserBookPacer : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid DocumentBookId { get; set; }
    public int CurrentChunkOrder { get; set; } = 1;
    public int DailyPaceChunks { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateOnly? LastReadDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
    public DocumentBook DocumentBook { get; set; } = null!;
}
```

## 5. UI/UX Specifications

### 5.1. The Pacer Bar (`frontend/pages/today.vue`)
- **Center Control:**
  - Dropdown button displaying active book title with chevron: `📖 Designing Data-Intensive Applications ▼`.
  - Clicking displays a sleek menu with:
    - Currently reading books with mini progress bars.
    - Quick link: `+ Chọn Sách Khác Từ Thư Viện` / `+ Thêm Sách Mới`.
- **Progress Pill:**
  - `Chương 3: Storage Engines • Lát cắt 4 / 12 (28%)`.
- **Navigation Actions:**
  - Previous Slice button & Next Slice button with disabled states at boundaries.

### 5.2. AI Synthesis Skeleton Card (`frontend/components/today/AISynthesisCard.vue`)
- Displays shimmering skeleton placeholders for question text and 4 option buttons.
- Pulsating brand icon with copy:
  - English: *"Gemini is analyzing this architectural slice... Your trade-off challenge will be ready in a moment."*
  - Vietnamese: *"Gemini đang phân tích lát cắt kiến trúc... Thử thách Trade-off sẽ sẵn sàng trong giây lát."*
- Resilient recovery: If generation takes >6s, shows a graceful retry button.

## 6. Project Invariants Compliance
- **100% English Codebase:** All classes, methods, DTOs, tables, and test cases in English.
- **Source Language Preservation:** English technical documents remain English; Vietnamese remains Vietnamese.
- **Responsive Typography:** Minimum `text-sm` (14px) mobile, `text-base` (16px) or `text-lg` (18px) desktop.
- **Bilingual Layout:** All badges and action buttons equipped with `whitespace-nowrap shrink-0` with flex gaps.
- **Balanced Bracket Parsing:** Depth-balanced bracket parsing (`ExtractJsonObject`) for Gemini scenario outputs.

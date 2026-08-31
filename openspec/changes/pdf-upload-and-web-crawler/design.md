# Design: PDF Upload & Web URL Document Crawler

## Architecture & Data Flow

```
Frontend (/library)
├── ImportModal.vue (Tabbed: Markdown | PDF | URL)
│   ├── Tab 1 (Markdown): Direct POST /api/v1/library/import
│   ├── Tab 2 (PDF Upload): POST /api/v1/library/upload-pdf (multipart/form-data)
│   └── Tab 3 (URL Crawler): POST /api/v1/library/crawl-url -> Preview -> POST /api/v1/library/import
│
Backend (ASP.NET Core)
├── Endpoints: LibraryEndpoints.cs
│   ├── POST /api/v1/library/upload-pdf
│   └── POST /api/v1/library/crawl-url
├── Application Layer:
│   ├── Features/Library/UploadPdf/UploadPdfHandler.cs
│   ├── Features/Library/CrawlUrl/CrawlUrlHandler.cs
│   └── Interfaces/IPdfExtractor.cs & IWebArticleCrawler.cs
└── Infrastructure Layer:
    ├── Services/PdfPigExtractor.cs (UglyToad.PdfPig library)
    └── Services/WebArticleCrawler.cs (HttpClient + HtmlAgilityPack + ReverseMarkdown)
```

## PDF Processing Strategy & Large File Memory Architecture
1. **Extraction Engine:** Use `UglyToad.PdfPig` (.NET 10 compatible, zero native dependency, fast managed memory).
2. **Streaming Pipeline (Zero-LOH Buffering):**
   - Stream directly from `IFormFile.OpenReadStream()` to `PdfDocument.Open(stream)`.
   - Never load the entire 500MB PDF as a single contiguous `byte[]` in memory. This eliminates Gen 2 GC pauses and avoids Large Object Heap (LOH) OutOfMemory fragmentation.
3. **Kestrel & FormOptions Configuration:**
   - Configure `[RequestSizeLimit(524_288_000)]` (500 MB) on `POST /api/v1/library/upload-pdf`.
   - Set `FormOptions.MultipartBodyLengthLimit = 524_288_000` in endpoint DI wiring.
4. **Text Normalization & Chunk Segmentation:**
   - Strip repeating running headers and footers (page numbers).
   - Detect chapter titles via font size / uppercase headings or group by contiguous 500-word page blocks.
   - Each `DocumentChunk` is tagged with `ChapterTitle = "Part {N}: ..."` or actual heading.
   - Generate initial `MicroQuizVo` and `KeyTakeaways` from slice summary.

## Web Crawling Strategy
1. **GitHub URL Detection:**
   - Detect `github.com/{owner}/{repo}/blob/{branch}/{path}.md` and automatically translate to raw URL `raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}.md`.
2. **Standard HTML Pages:**
   - Use `HttpClient` with standard browser `User-Agent`.
   - Parse with `HtmlAgilityPack` to select candidate article containers: `article`, `main`, `.markdown-body`, `#content`, `.post-content`, or fallback to `body`.
   - Strip `<script>`, `<style>`, `<nav>`, `<footer>`, `<aside>`, `<svg>`, and ads.
   - Convert cleaned HTML to Markdown using `ReverseMarkdown.Net`.
   - Extract OpenGraph title `meta[property='og:title']` or `<title>`.

## UI Components & Flow
1. **Upload Modal Tab Selector:** 3 distinct pill tabs with icons (`FileText`, `FileUp`, `Globe`).
2. **PDF Dropzone:**
   - Drag-and-drop box with file picker.
   - Displays selected file name, size (e.g. `12.4 MB`), and format validation.
   - Uploading state with spinner and "Extracting PDF & Slicing Chapters..." animation.
3. **URL Crawler:**
   - Input box for URL.
   - "Fetch Article" button with loading spinner.
   - Once fetched, auto-populates Title and Markdown preview textarea for quick edits before saving.

# Proposal: Smart PDF Curation & Reader Formatting

## Executive Summary
This proposal resolves two intertwined critical defects in TechDaily's document ingestion and reading pipeline:
1. **Severe Code-Block Markdown Corruption & Text Swallowing:** The PDF markdown formatter in `PdfPigExtractor.cs` uses a brittle line-by-line regex heuristic that traps prose explanations (such as instructional steps, file paths, and notes) inside code blocks (```` ```csharp ````), rendering narrative prose in red monospace code formatting.
2. **Chaotic Slicing & Roadmap Grouping Collapse:** Ingesting large monolithic technical PDFs (such as Microsoft Learn's 3,000+ page `aspnet-core-aspnetcore-10.0.pdf`) flattens multi-level bookmark trees, blindly slicing text every 700 words into arbitrary fragments (`Part 1 ... Part 10`), producing 2,672 incoherent slices. On `/roadmap`, a naive title-based grouping collapses all slices named "Overview" across hundreds of unrelated chapters into a single distorted milestone with 47 fragmented, out-of-order cards (`#2`, `#23`, `#63`, `#85`).

We introduce a **Smart Auto-Curation Engine** for PDF ingestion and a **Resilient Markdown Tokenizer**, accompanied by **Parent-Module-Aware Sequential Grouping** on `/roadmap` and clean typography on the Gitbook Reader and `/today`.

---

## Problem Statement & Motivation

### 1. The Code Block Trapping Bug (Reader & Today Pane)
When `FormatAsMarkdown` detects a keyword like `using` or a semicolon/brace, it enters `inCodeBlock = true`. The exit condition relies on `trimmed.EndsWith('.')` and forbids parentheses `()`, colons, and uppercase starts without punctuation. As a result:
- Section titles (`Change the app`), filenames (`Components/Pages/Counter.razor :`), framework identifiers (`razor`), and sentences containing parentheses fail to trigger the exit condition.
- Entire paragraphs of crucial architectural explanation are swallowed into the code fence.
- On both `/today` (DocReaderPane) and `/read/[bookId]` (Gitbook Reader), users see pages of red monospace code instead of formatted articles.

### 2. The 2,672 Fragmented Slices & "Part 1..10" Churn
`PdfPigExtractor` currently collects every bookmark at every depth level (Level 0, 1, 2, 3, 4, 5) without discrimination. A 1-page subsection or a leaf bookmark becomes its own slice. If text exceeds 900 words, it enforces a hard 700-word cut with `(Part X)`, severing code blocks mid-function and cutting sentences in half.

### 3. Roadmap Data Jungle & Title Collisions
In `roadmap.vue`, milestones are constructed via `new Map<string, typeof chunks>()` keyed strictly by `chunk.chapterTitle`. In comprehensive technical documentation, every major section contains an "Overview". Because parent chapter context is discarded, every "Overview" slice across 3,000 pages collapses into a single milestone with 47 non-contiguous slices jumping from `#2` to `#126`. Furthermore, raw markdown hashtags (`# Overview`, `### 07/30/2025`) and pre-release disclaimers bleed directly into card summaries.

---

## Proposed Solution & Key Capabilities

### 1. Smart PDF Bookmark Hierarchy Curation (Backend)
- Analyze the outline tree to detect root volumes, primary chapters (Level 1/2), and leaf sub-sections (Level 3+).
- Group leaf sub-sections into their parent topic rather than creating independent slices. A comprehensive topic (e.g. *Dependency Injection in ASP.NET Core*) remains a coherent 1,500–3,500 word masterclass.
- Eliminate arbitrary 700-word cuts. Only split if a single standalone article exceeds 5,000 words, and only split at natural `##` section headings outside code blocks.

### 2. Resilient Code Block Detection State Machine (Backend)
- Require strong multi-line code indicators (indentation, multiple code tokens, method signatures, class declarations) to open a code fence.
- Automatically and aggressively close code fences when encountering normal English prose sentences (capitalized start, natural punctuation, high dictionary-word density, double blank line, or explicit headings).
- Strip print-layout noise: publication dates (`### 07/30/2025`), header/footer numbering, and legal disclaimers (*"Important: This information relates to a pre-release product..."*).

### 3. Parent-Module Context & Sequential Roadmap Grouping (Backend & Frontend)
- Preserve parent chapter context in `DocumentChunk.ChapterTitle` (e.g. `"Fundamentals: Dependency Injection"`, `"Blazor: Overview"`).
- Refactor `roadmap.vue` to group milestones sequentially along the reading order instead of an unordered global title map, guaranteeing contiguous slice order (`1, 2, 3...`).
- Clean raw markdown headers (`#`, `##`, `###`) and disclaimers from card summaries.

---

## Scope & Boundaries
- **Included:**
  - `PdfPigExtractor.cs` refactoring (bookmark tree curation, code block tokenizer, disclaimer stripping).
  - `PdfIngestionWorker.cs` clean summary text generation.
  - `roadmap.vue` sequential milestone grouping and sanitized card previews.
  - Comprehensive unit tests for PDF extraction heuristics and roadmap grouping.
- **Excluded:**
  - Changes to the vector embedding schema (pgvector columns and HNSW indexes remain intact).
  - Changes to user authentication or drill generation endpoints.

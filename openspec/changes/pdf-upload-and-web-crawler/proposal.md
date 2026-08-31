# Proposal: PDF File Upload & Web URL Document Crawler

## Summary
Add support for importing technical books and articles directly via **PDF file upload** (with server-side text extraction & slice chunking) and **Web URL crawling/fetching** (with HTML-to-Markdown conversion for GitHub, Microsoft Learn, and tech blogs).

## Problem Statement
Currently, importing a document in TechDaily requires users to manually copy and paste raw Markdown text into a textarea. However:
1. Many standard senior technical books (e.g. O'Reilly, Manning, RFCs) are stored locally as PDF files.
2. Many high-value engineering articles and documentation pages are published on web URLs (e.g. GitHub repos, Microsoft Learn, Martin Fowler blogs, Substack).
3. Manually opening, copying, and converting these resources into Markdown is tedious and causes high friction for daily learning.

## Proposed Solution
- **PDF Upload & Ingestion (`POST /api/v1/library/upload-pdf`):**
  - Accept `.pdf` multipart file upload bounded to **50–60% of Gemini Free context window**: maximum **200 MB** or **800 pages** (~500,000 tokens).
  - Stream directly from the HTTP request stream into `UglyToad.PdfPig` without full in-memory byte array buffering to avoid Large Object Heap (LOH) fragmentation.
  - Extract text and bookmarks page-by-page, automatically segmenting pages into 3–5 minute reading slices (`DocumentChunk`) with chapter titles.
- **Web URL Document Crawler (`POST /api/v1/library/crawl-url`):**
  - Accept a target URL (e.g. GitHub raw/blob, Microsoft Learn, Dev.to, Medium, personal engineering blogs).
  - Fetch content via `HttpClient`, clean and extract main article body (`<article>`, `<main>`, markdown container), and convert HTML to structured Markdown using `ReverseMarkdown`.
- **Enhanced Import Modal in `/library`:**
  - Modern 3-tab import interface:
    1. **📝 Paste Markdown** (Fast direct input).
    2. **📄 Upload PDF** (Drag & drop zone supporting up to 200 MB / 800 pages, live upload progress bar and page extraction indicator).
    3. **🌐 Import from URL** (URL input with "Fetch & Preview" button to review and edit markdown before saving).

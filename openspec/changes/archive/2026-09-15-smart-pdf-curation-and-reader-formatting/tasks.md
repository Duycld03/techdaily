# Tasks: Smart PDF Curation & Reader Formatting

## 1. Backend Infrastructure & Extraction Engine
- [x] 1.1 Refactor `PdfPigExtractor.cs` to extract hierarchical bookmarks with `ParentTitle` and apply depth-aware curation (curate at Topic level, aggregating leaf subsections into coherent chapters).
- [x] 1.2 Deprecate arbitrary 700-word partition cuts (`(Part X)`) in `PdfPigExtractor.cs`, keeping complete articles intact and splitting only at natural `##` headings if exceeding 5,000 words.
- [x] 1.3 Re-implement `FormatAsMarkdown` code block detection state machine with strict entry conditions and aggressive prose escape rules to prevent explanatory text from being trapped in code fences.
- [x] 1.4 Implement pre-release disclaimer, date, and print-layout artifact stripping in `PdfPigExtractor.cs`.
- [x] 1.5 Update `PdfIngestionWorker.cs` to ensure `SummaryMarkdown` is sanitized of leading `#` heading tokens before database persistence.

## 2. Frontend Roadmap & Reader Refinements
- [x] 2.1 Refactor `chapterMilestones` computation in `frontend/pages/roadmap.vue` to enforce sequential module grouping along chronological `chunkOrder` without title collision.
- [x] 2.2 Add `sanitizeSummary` helper in `frontend/pages/roadmap.vue` to strip leading Markdown headings (`#`, `##`) and dates from milestone cards.
- [x] 2.3 Verify typography and code block styling in `frontend/components/today/DocReaderPane.vue` and `frontend/pages/read/[bookId].vue`.

## 3. Verification & Automated Testing
- [x] 3.1 Update and expand `PdfPigExtractorTests.cs` to verify code block isolation, hierarchical bookmark curation, and disclaimer removal.
- [x] 3.2 Execute backend test suite: `dotnet test backend/tests/TechDaily.Tests`.
- [x] 3.3 Execute frontend test suite: `npm test` in `frontend/`.

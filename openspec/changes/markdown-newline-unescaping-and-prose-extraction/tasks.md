# Tasks: Markdown Newline Unescaping & Prose Code-Block Extraction Refinement

## 1. Backend AI Markdown Newline Normalization
- [x] 1.1 Implement `NormalizeEscapedNewlines` in `backend/src/TechDaily.Infrastructure/Services/GeminiAiService.cs`.
- [x] 1.2 Apply normalization to `formattedMarkdown`, `summaryMarkdown`, and `scenarioDrill.ExplanationMarkdown` in `ParseSliceResponse`.
- [x] 1.3 Refactor system instruction JSON schema example in `GeminiAiService.cs` to eliminate literal `\n` in prompt templates.

## 2. Raw PDF Extraction Prose & Code Block Heuristics
- [x] 2.1 Update `IsObviousProse` in `backend/src/TechDaily.Infrastructure/Services/PdfPigExtractor.cs` to recognize technical documentation headers (`New behavior`, `Previous behavior`, `Type of breaking change`, `Reason for change`, `Recommended action`, `Affected APIs`).
- [x] 2.2 Add closing delimiter detection (`}`, `};`, `});`) in `PdfPigExtractor.FormatAsMarkdown` to terminate code blocks when followed by blank lines or prose.
- [x] 2.3 Add unit test in `PdfPigExtractorTests.cs` verifying that Breaking Changes prose headers are never trapped inside code blocks.

## 3. Frontend Markdown Renderer Resilience & DB Repair
- [x] 3.1 Update `frontend/composables/useMarkdownRenderer.ts` `sanitizeScraperArtifacts` to normalize literal `\n` and `\r\n` to real linebreaks before rendering.
- [x] 3.2 Execute database update on `DocumentChunks` in `techdaily_db` to repair existing records containing literal `\n` sequences (e.g. Slice 15).

## 4. Local-First Automated Verification
- [x] 4.1 Run `dotnet test backend/tests/TechDaily.Tests` to verify all backend extraction and curation tests pass.
- [x] 4.2 Run `npm test` to verify all frontend tests pass.
- [x] 4.3 Verify Slice 15 and Slice 11 in Playwright, ensuring callout alerts, headers, code blocks, and prose render cleanly without literal `\n` or trapped text.

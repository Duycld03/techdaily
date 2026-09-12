## 1. Frontend Markdown Callout Renderer

- [x] 1.1 In `frontend/composables/useMarkdownRenderer.ts`, implement alert callout detection and custom rendering for `NOTE`, `TIP`, `IMPORTANT`, `WARNING`, `CAUTION` with semantic colors and SVG icons.
- [x] 1.2 In `frontend/composables/useMarkdownRenderer.ts`, strip redundant duplicate titles (`[WARNING]` followed immediately by `Warning`).
- [x] 1.3 In `frontend/pages/read/[bookId].vue`, add `prose-blockquote:not-italic prose-blockquote:before:content-none prose-blockquote:after:content-none` and paragraph quote reset to eliminate quotation marks.

## 2. Backend Crawler Callout Preprocessing

- [x] 2.1 In `backend/src/TechDaily.Infrastructure/Services/WebArticleCrawler.cs`, update `PreprocessAlertBoxes` to strip redundant title elements and output standard GitHub Alert format `> [!TYPE]`.
- [x] 2.2 Run backend unit tests (`dotnet test backend/TechDaily.sln`) to ensure crawler logic and security tests pass.

## 3. Verification & Testing

- [x] 3.1 Write unit tests in `frontend/tests/composables/useMarkdownRenderer.spec.ts` covering alert callout rendering and quote suppression.
- [x] 3.2 Run frontend unit tests (`npm test`) and verify production build (`npm run build`).

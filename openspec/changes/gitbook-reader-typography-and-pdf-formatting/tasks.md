# Tasks: GitBook Reader Typography Controls, PDF Paragraph Segmentation, and Verbatim Preservation Hints

## 1. Backend PDF Paragraph & Heading Segmentation (`PdfPigExtractor.cs`)

- [x] 1.1 In `backend/src/TechDaily.Infrastructure/Services/PdfPigExtractor.cs`, refactor `ExtractPageLines` to capture word and line geometry (`Top`, `Bottom`, `Left`, `Right`) and calculate the median vertical line spacing ($\Delta Y_{\text{median}}$) across lines on each page. Verify line geometry calculations with unit tests.
- [x] 1.2 In `PdfPigExtractor.cs`, implement line spacing gap detection: when vertical spacing between consecutive lines $\Delta Y_i \ge 1.35 \times \Delta Y_{\text{median}}$, emit double newlines (`\n\n`) to mark a paragraph boundary. Verify paragraph boundary emission with unit tests.
- [x] 1.3 In `PdfPigExtractor.cs`, implement first-line indentation detection: determine text column left margin $X_{\min}$ and emit double newlines (`\n\n`) when a line starts with indentation $\Delta X \ge 12\text{pt}$ relative to $X_{\min}$. Verify indentation detection with unit tests.
- [x] 1.4 In `PdfPigExtractor.cs` (`FormatAsMarkdown`), implement heading and section title detection: identify standalone short, uppercase, centered lines or lines matching chapter/preface regex patterns (`^LỜI NÓI ĐẦU`, `^Phần GIỚI THIỆU`, `^Câu chuyện của chính tôi\.?`, `^Chương\s+\d+`, `^Chapter\s+\d+`), formatting them as `\n\n## {Title}\n\n`. Verify heading markdown generation with unit tests.
- [x] 1.5 In `PdfPigExtractor.cs`, implement dialogue and quotation line break preservation: preserve line breaks for spoken lines beginning with quotes (`"`, `“`, `”`) or dialogue dashes (`-`, `—`, `–`). Verify dialogue lines retain distinct breaks with unit tests.
- [x] 1.6 Add comprehensive unit tests in `backend/tests/TechDaily.Tests/Infrastructure/PdfPigExtractorTests.cs` covering gap detection, first-line indent, dialogue, and heading segmentation. Verify with `dotnet test backend/tests/TechDaily.Tests/TechDaily.Tests.csproj --filter FullyQualifiedName~PdfPigExtractorTests`.

---

## 2. Frontend Markdown Paragraph Rendering (`useMarkdownRenderer.ts`)

- [x] 2.1 In `frontend/composables/useMarkdownRenderer.ts`, verify Markdown-it parses double newlines (`\n\n`) into separate `<p>` tags without collapsing. Verify with unit test assertions in `frontend/tests/composables/useMarkdownRenderer.spec.ts`.
- [x] 2.2 In `frontend/pages/read/[bookId].vue`, refine Tailwind prose classes on the `<article>` tag to ensure paragraph elements receive balanced vertical margins (`prose-p:my-4 prose-p:leading-inherit`). Verify with visual inspection in browser.

---

## 3. Verbatim Category Helper Hint in Library Modals (`frontend/pages/library.vue`)

- [x] 3.1 In `frontend/pages/library.vue`, add the verbatim category helper callout beneath the Category selector in Tab 0 (Markdown Series Tab). Verify in browser that the amber helper box displays under the dropdown.
- [x] 3.2 In `frontend/pages/library.vue`, add the verbatim category helper callout beneath the Category selector in Tab 1 (PDF Upload Tab). Verify in browser that the amber helper box displays under the dropdown.
- [x] 3.3 In `frontend/pages/library.vue`, add the verbatim category helper callout beneath the Category selector in Tab 2 (URL Crawler Tab). Verify in browser that the amber helper box displays under the dropdown.
- [x] 3.4 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, add localized string keys `library.verbatim_category_hint` for English and Vietnamese:
  - **VI:** `"💡 Mẹo: Chọn danh mục \"Tư Duy Kỹ Sư & Năng Suất\" nếu bạn muốn giữ nguyên văn 100% từng câu chữ của sách (không dùng AI tóm tắt ngắn lại)."`
  - **EN:** `"💡 Tip: Select \"Engineering Craft & Mindset\" if you want 100% verbatim author text preserved without AI condensing."`
  Verify by toggling language switcher in `/library`.

---

## 4. Novel-Style Reader Typography Controls (`frontend/pages/read/[bookId].vue`)

- [x] 4.1 In `frontend/pages/read/[bookId].vue`, declare reactive state `typography` with default values (`fontSize: 'base'`, `fontFamily: 'sans'`, `lineSpacing: 'relaxed'`, `readingWidth: 'standard'`) and `isTypographyOpen = ref(false)`. Verify reactive state initializes with defaults.
- [x] 4.2 In `frontend/pages/read/[bookId].vue`, implement `localStorage` hydration and persistence under key `techdaily_reader_typography` with SSR safety (`import.meta.client`). Verify settings persist across page reloads in browser devtools.
- [x] 4.3 In `frontend/pages/read/[bookId].vue`, add the `Aa` button in the reader topbar directly adjacent to `ThemeToggle` with active ring indicator. Verify clicking toggles `isTypographyOpen`.
- [x] 4.4 In `frontend/pages/read/[bookId].vue`, implement the typography settings popover card with:
  - Font size controls: `A-` / `A+` stepped buttons across `sm` (14px), `base` (16px), `lg` (18px), `xl` (20px), `2xl` (22px).
  - Font family toggle: Sans-Serif (Inter), Serif (Merriweather/Georgia), Monospace (JetBrains Mono).
  - Line spacing toggle: Standard (`leading-normal`), Relaxed (`leading-relaxed`), Loose (`leading-loose`).
  - Reading width toggle: Standard (`max-w-3xl`), Wide (`max-w-4xl`), Full (`max-w-full`).
  - Click-outside dismiss handler.
  Verify all options update reactive state on click.
- [x] 4.5 In `frontend/pages/read/[bookId].vue`, bind dynamic classes to the reading container (`readingWidth`) and `<article>` tag (`fontSize`, `fontFamily`, `lineSpacing`). Verify in browser that font size, font family, line height, and container width change dynamically in real time.
- [x] 4.6 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, add localized keys for typography controls (`reader.typography_settings`, font sizes, font families, line spacing, reading widths). Verify bilingual rendering in typography popover.

---

## 5. Local Testing & Verification

- [x] 5.1 Execute backend test suite: `dotnet test backend/TechDaily.sln` to verify all extractor and pipeline tests pass.
- [x] 5.2 Execute frontend test suite: `npm --prefix frontend run test` to verify component and composable tests pass.
- [x] 5.3 Test end-to-end extraction and reading locally using Vietnamese *Atomic Habits* fixture (`/home/duycld03/Downloads/827-thoi-quen-nguyen-tu-thuviensach.vn.pdf`): verify in local browser (`http://localhost:3000/read/[bookId]`) that paragraphs are properly separated and headings are isolated.

---

## 6. Production Deployment & Live Verification via MCP Tools

- [x] 6.1 Commit all implementation changes to git and push to `origin main`. Verify `git status` is clean and commit is pushed to remote.
- [ ] 6.2 Monitor GitHub Actions CI/CD deployment pipeline and wait 4–5 minutes until workflow run completes successfully. Verify deployment status on remote server.
- [ ] 6.3 Execute live browser verification on production `https://techdaily.duckdns.org` via MCP tools:
  - Navigate to `/library` and open the import modal: verify verbatim category helper callout displays under Category dropdown across Markdown, PDF, and URL Crawler tabs.
  - Navigate to `/read/[bookId]` for an imported book: verify paragraphs render with clean line breaks and distinct `<p>` tags instead of a solid block of text.
  - In `/read/[bookId]`, open the `Aa` typography popover: verify font size changes dynamically with `A-` / `A+`, font family switches between Sans and Serif, line height adjusts, and preferences persist upon page refresh.

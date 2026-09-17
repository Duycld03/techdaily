# Proposal: GitBook Reader Typography Controls, PDF Paragraph Segmentation, and Verbatim Preservation Hints

## Why

TechDaily aims to provide an exceptional reading and learning experience for software engineers, bridging rigorous technical manuals and impactful engineering craft literature. However, three critical formatting and user-experience issues in the reader (`/read/[bookId]`) and library (`/library`) currently degrade readability:

1. **PDF Text Squashing in GitBook Reader:**
   In `PdfPigExtractor.cs`, `ExtractPageLines` groups extracted words strictly by baseline Y coordinate and joins all lines on each page with a single newline (`\n`). In Markdown and CommonMark specifications (and in TechDaily's `useMarkdownRenderer.ts` where `breaks: false`), single newlines inside text blocks are treated as soft breaks (spaces). Consequently, entire chapters and multi-page books (such as *Atomic Habits* / *Thói quen nguyên tử*) collapse into a single monolithic, unformatted solid block of text. Readers are confronted with an unreadable wall of text devoid of paragraph indents, vertical line spacing rhythm, dialogue breaks, or visual section headings.

2. **Hidden Verbatim Preservation Workflow in Library Ingestion:**
   While the ingestion engine supports 100% verbatim text preservation for books under Category 4 ("Engineering Craft & Mindset" / "Tư Duy Kỹ Sư & Năng Suất"), users uploading PDFs or crawling web documentation have no visual indication of this policy. Without an explicit in-context hint beneath the Category selector in the Markdown, PDF Upload, and URL Crawler tabs, users cannot make an informed choice to preserve the author's original narrative prose versus enabling AI-condensed summaries.

3. **Rigid Reader Typography & Layout:**
   The reader view (`/read/[bookId].vue`) hardcodes typography styles: a static font size (`text-sm md:text-lg`), default sans-serif font family, fixed line spacing, and a rigid container width (`max-w-3xl`). Unlike modern e-readers (such as Kindle, Apple Books, or GitBook), readers cannot customize font size for varying display distances, toggle to a serif typeface for narrative immersion or monospace for technical code analysis, adjust line height, or expand reading width on wide monitors.

Resolving these issues transforms TechDaily's reader into a publication-grade reading environment with automated paragraph segmentation, clear user guidance on text preservation, and personalized novel-style typography controls.

---

## What Changes

We propose a cohesive three-part enhancement spanning backend extraction heuristics, library ingestion UX, and frontend reader typography:

1. **Intelligent PDF Paragraph & Heading Segmentation (`PdfPigExtractor.cs`):**
   - **Line Spacing Gap Detection:** Calculate the median vertical line spacing ($\Delta Y_{\text{median}}$) across lines on a page. When vertical spacing between consecutive lines exceeds $1.35 \times \Delta Y_{\text{median}}$, recognize a paragraph boundary and emit double newlines (`\n\n`).
   - **First-Line Indentation Detection:** Determine the left margin ($X_{\min}$) of the text column. When a line starts with an indentation $\Delta X \ge 12\text{pt}$ relative to $X_{\min}$, emit double newlines (`\n\n`) to initiate a new paragraph.
   - **Heading & Section Title Detection:** Detect standalone short, centered, uppercase lines, or lines matching chapter and preface patterns (e.g. "LỜI NÓI ĐẦU", "Phần GIỚI THIỆU", "Câu chuyện của chính tôi.", "Chương \d+", "Chapter \d+"), formatting them with Markdown heading tags (`\n\n## {Title}\n\n`).
   - **Dialogue & Quotation Detection:** Preserve independent line breaks for spoken lines and quotes beginning with quotation marks (`"`, `“`, `”`) or dialogue dashes (`-`).
   - **Clean Markdown Rendering:** Ensure `useMarkdownRenderer.ts` preserves paragraph breaks cleanly into distinct semantic `<p>` tags with balanced vertical margins.

2. **Verbatim Category Helper Hint in Library Modals (`frontend/pages/library.vue`):**
   - Under the Category selector across all 3 modal tabs (Markdown Series, PDF Upload, and URL Crawler), add an informative visual notice:
     - **VI:** `"💡 Mẹo: Chọn danh mục \"Tư Duy Kỹ Sư & Năng Suất\" nếu bạn muốn giữ nguyên văn 100% từng câu chữ của sách (không dùng AI tóm tắt ngắn lại)."`
     - **EN:** `"💡 Tip: Select \"Engineering Craft & Mindset\" if you want 100% verbatim author text preserved without AI condensing."`

3. **Novel-Style Reader Typography Controls (`frontend/pages/read/[bookId].vue`):**
   - In the reader topbar next to `ThemeToggle`, add an `Aa` (Typography Settings) button that triggers a Kindle/Apple Books-style popover.
   - **Font Size Scale:** 5 presets (`sm`: 14px, `base`: 16px [default], `lg`: 18px, `xl`: 20px, `2xl`: 22px) with stepped `A-` / `A+` controls.
   - **Font Family Options:**
     - Sans-Serif (Default: Inter, system-ui)
     - Serif (Novel standard: Merriweather, Georgia, Lora, serif)
     - Monospace (Technical books: JetBrains Mono, monospace)
   - **Line Spacing Options:** Standard (`leading-normal`: 1.5), Relaxed (`leading-relaxed`: 1.625 [default]), Loose (`leading-loose`: 2.0).
   - **Reading Width Options:** Standard (`max-w-3xl`), Wide (`max-w-4xl`), Full (`max-w-full`).
   - **Persistence:** Persist typography preferences in browser `localStorage` under key `techdaily_reader_typography` across books and sessions.

4. **Production Deployment & Live Verification Workflow:**
   - Commit and push changes to `origin main`.
   - Monitor GitHub Actions CI/CD to completion (4–5 minutes).
   - Execute live browser verification on production (`https://techdaily.duckdns.org`) verifying paragraph segmentation, typography popover controls, and verbatim category notices.

---

## Capabilities

### Modified Capabilities

- `pdf-extraction`: Enhanced paragraph and heading segmentation heuristics in `PdfPigExtractor.ExtractPageLines` and `FormatAsMarkdown` (line spacing gap detection, first-line indentation, heading/section titles, dialogue/quotation line breaks).
- `reader`: Novel-style reader typography controls (font size scale, font family, line spacing, reading width, localStorage persistence) and clean paragraph break preservation in `useMarkdownRenderer.ts`.
- `library`: Verbatim category helper hint under the Category selector in all 3 tabs (Markdown, PDF, URL) in `frontend/pages/library.vue`.

---

## Impact

- **Backend:**
  - `backend/src/TechDaily.Infrastructure/Services/PdfPigExtractor.cs`: Line spacing calculations, indentation detection, and heading formatters.
  - `backend/tests/TechDaily.Tests/Infrastructure/PdfPigExtractorTests.cs`: Unit tests for gap detection, indentation, dialogue, and heading segmentation.
- **Frontend:**
  - `frontend/composables/useMarkdownRenderer.ts`: Paragraph break handling and `<p>` tag spacing.
  - `frontend/pages/library.vue`: Verbatim category helper callout across Markdown, PDF, and URL Crawler tabs.
  - `frontend/pages/read/[bookId].vue`: `Aa` typography button in topbar, typography popover, dynamic font size, family, leading, and width classes.
  - `frontend/i18n/locales/en.json` & `frontend/i18n/locales/vi.json`: Localized strings for typography controls and library helper hints.
- **Deployment & Operations:**
  - Automated deployment via GitHub Actions CI/CD to production host `https://techdaily.duckdns.org`.
  - Zero database schema migrations required (pure client-side state and backend parser heuristics).

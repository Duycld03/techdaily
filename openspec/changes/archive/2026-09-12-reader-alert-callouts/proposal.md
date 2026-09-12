## Why

Technical documentation frequently relies on alert callouts (e.g. Note, Tip, Important, Warning, Caution) to highlight critical caveats, performance tips, and security warnings. Currently, these render as generic green blockquotes wrapped in Tailwind Typography's literary quotation marks (`“... ”`), with unparsed bracket text (`"[WARNING]"`) and duplicate title headers (`"[WARNING] Warning"`). Transforming them into first-class GitHub-style alert callouts with semantic color-coding, icons, and clean typography brings the reader to modern technical documentation standards.

## What Changes

- **GitHub Alert Rendering in Frontend**: Enhance `useMarkdownRenderer.ts` to recognize both GitHub-style (`[!NOTE]`, `[!TIP]`, `[!IMPORTANT]`, `[!WARNING]`, `[!CAUTION]`) and legacy bracketed callout markers (`[WARNING]`, `[NOTE]`, etc.), rendering them into styled alert boxes with custom icons and titles.
- **Color & Icon Hierarchy**:
  - `NOTE`: Blue / Sky accent with Info icon.
  - `TIP`: Emerald accent with Lightbulb icon.
  - `IMPORTANT`: Indigo / Purple accent with AlertCircle icon.
  - `WARNING`: Amber accent with AlertTriangle icon.
  - `CAUTION` / `DANGER`: Rose / Red accent with OctagonAlert icon.
- **Quote & Duplicate Header Sanitization**:
  - Disable Tailwind Typography pseudo-element quotation marks on blockquotes (`prose-blockquote:before:content-none prose-blockquote:after:content-none`).
  - Strip redundant duplicate headers (e.g., `[WARNING]` followed by a `Warning` paragraph).
- **Backend Crawler Standardization**: Update `WebArticleCrawler.cs` to output clean GitHub Alert syntax `> [!TYPE]` while stripping redundant title tags from scraped HTML (e.g., Microsoft Learn `<div class="WARNING"><p>Warning</p>...</div>`).

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `reader`: Enhance markdown rendering specifications to require semantic alert callout parsing (Note, Tip, Important, Warning, Caution) with distinct color themes and icons, and elimination of pseudo-element quotation marks.

## Impact

- Frontend: `frontend/composables/useMarkdownRenderer.ts`, `frontend/pages/read/[bookId].vue`.
- Backend: `backend/src/TechDaily.Infrastructure/Services/WebArticleCrawler.cs`.
- Specs: Delta spec in `openspec/changes/reader-alert-callouts/specs/reader/spec.md`.

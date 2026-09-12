## Context

Document readers encountering callout boxes (e.g. from Microsoft Learn or GitHub documentation) currently see:
1. Generic green borders (`border-emerald-500`) regardless of severity.
2. Unparsed bracket labels (`"[WARNING]"`).
3. Duplicated titles (`"[WARNING] Warning"`).
4. Intrusive typographical curly quotes (`“` and `”`) injected by `@tailwindcss/typography` default styling for blockquotes.

See `proposal.md` for motivation.

## Goals / Non-Goals

**Goals:**
- Detect and render technical alert callouts (`NOTE`, `TIP`, `IMPORTANT`, `WARNING`, `CAUTION`) with distinct semantic color themes (Sky, Emerald, Indigo, Amber, Rose) and dedicated SVG icons.
- Strip redundant repeated titles (such as `[WARNING]` followed immediately by `Warning`).
- Suppress Tailwind Typography quotation mark pseudo-elements (`“... ”`) on blockquotes and callout paragraphs.
- Keep `html: false` security in MarkdownIt to maintain SSRF/XSS defense.
- Clean up backend `WebArticleCrawler.cs` alert box preprocessing to output clean GitHub Alert syntax without redundant title paragraphs.

**Non-Goals:**
- Custom user-authored callouts editor in frontend (focus is purely on reader rendering).
- Modifying database schemas or migrations.

## Decisions

### 1. MarkdownIt Token Stream Processing for Alerts
- **Choice:** Inspect tokens between `blockquote_open` and `blockquote_close` in a MarkdownIt core rule / renderer.
- **Rationale:** Keeps `html: false` active and safe, while allowing rich HTML container markup (`<div class="callout callout-warning ...">`) with embedded SVG icons and semantic headers.
- **Matching Patterns:**
  - Matches `[!NOTE]`, `[!TIP]`, `[!IMPORTANT]`, `[!WARNING]`, `[!CAUTION]` (GitHub standard).
  - Also matches legacy `**[NOTE]**`, `[NOTE]`, `**[WARNING]**`, `[WARNING]`, `**[TIP]**`, etc. (crawler legacy formats).
  - Case-insensitive.

### 2. Semantic Color & Icon Mapping
- **NOTE**: Border `border-sky-500`, background `bg-sky-50/70 dark:bg-sky-950/20`, text `text-sky-700 dark:text-sky-300`, icon: Info (i in circle).
- **TIP**: Border `border-emerald-500`, background `bg-emerald-50/70 dark:bg-emerald-950/20`, text `text-emerald-700 dark:text-emerald-300`, icon: Lightbulb.
- **IMPORTANT**: Border `border-indigo-500`, background `bg-indigo-50/70 dark:bg-indigo-950/20`, text `text-indigo-700 dark:text-indigo-300`, icon: AlertCircle.
- **WARNING**: Border `border-amber-500`, background `bg-amber-50/70 dark:bg-amber-950/20`, text `text-amber-700 dark:text-amber-300`, icon: AlertTriangle.
- **CAUTION / DANGER**: Border `border-rose-500`, background `bg-rose-50/70 dark:bg-rose-950/20`, text `text-rose-700 dark:text-rose-300`, icon: OctagonAlert.
- **STANDARD (Fallback)**: Border `border-slate-300 dark:border-slate-700`, background `bg-slate-50/50 dark:bg-slate-900/40`, text `text-slate-700 dark:text-slate-300`.

### 3. Eliminating Typographic Quotes
- **Choice:** Add `prose-blockquote:not-italic prose-blockquote:before:content-none prose-blockquote:after:content-none prose-p:before:content-none prose-p:after:content-none` to the prose article container in `frontend/pages/read/[bookId].vue`.
- **Rationale:** Technical documents contain code, warnings, and citations where literary curly quotes `“` and `”` distort formatting and confuse readers.

### 4. Backend Crawler Preprocessing Cleanup
- In `WebArticleCrawler.cs`, check if the alert node contains an alert title element (`.alert-title` or first paragraph text matching the alert type name). If so, remove the redundant title element before wrapping with `<blockquote><p>[!TYPE] ...</p></blockquote>`.

## Risks / Trade-offs

- **[Risk]** Regular blockquotes (intended as actual literary quotes) will lose italicization and decorative quotes.
  - **Mitigation:** In technical software architecture documentation, 99.9% of blockquotes are either callouts or standard references. Clear borders and indentation are much more readable than cursive italics with giant curly quotes.

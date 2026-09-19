# Design: Fix Reader i18n Keys, Key Takeaways Deduplication, and Brand Palette

## Context

The dedicated reader view (`frontend/pages/read/[bookId].vue`) provides distraction-free technical document reading. Slices contain both an AI-curated markdown body (`originalTextMarkdown`) and extracted structural metadata (`keyTakeaways: string[]`).

When the slice renders:
1. `renderedMarkdown` suppresses the leading chapter heading and renders the article body via `useMarkdownRenderer.ts`.
2. A separate Vue template container `<div v-if="hasValidTakeaways">` renders the structured takeaways below the article body.
3. Because Gemini appends `### Key Takeaways\n- item 1...` to the markdown body, both the markdown renderer and the template callout display the takeaways, leaving an untranslated English `Key Takeaways` heading in the article body.
4. The callout box was styled with legacy warning amber tokens instead of Dev-Learning Studio primary brand violet tokens.
5. A missing `{{` delimiter on the slice badge at line 1221 causes raw template code to render, and several controls use hardcoded English strings.

See `proposal.md` for complete user motivation and problem statement.

## Goals / Non-Goals

**Goals:**
- Fix the missing `{{` template interpolation syntax on the reader slice badge in `frontend/pages/read/[bookId].vue`.
- Suppress redundant `### Key Takeaways` and `## Key Takeaways` markdown sections from `renderedMarkdown` when `hasValidTakeaways` is true, ensuring takeaways appear exclusively within the localized callout box.
- Migrate the Key Takeaways callout box from legacy warning amber to Dev-Learning Studio system primary brand violet tokens (`bg-brand-50/50 dark:bg-brand-500/10`, `border-brand-200/80 dark:border-brand-500/20`, `text-brand-900 dark:text-brand-300`, `Sparkles text-brand-600 dark:text-brand-400`, `bg-brand-500` bullet indicators).
- Add missing translation keys (`font_smaller`, `font_larger`, `prev_slice_hint`, `next_slice_hint`, `loading_chapter`) to `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, wiring all hardcoded English strings to i18n `$t` calls.
- Update test coverage in `frontend/tests/pages/read.spec.ts` to verify syntax interpolation, takeaway deduplication, and brand token rendering.

**Non-Goals:**
- No backend modifications: `GeminiAiService` prompt and ingestion contracts remain unchanged.
- No changes to other views (`/today`, `/library`, `/quiz`).
- No changes to Markdown-it core alert parsing or Shiki highlighter utilities.

## Decisions

### Decision 1: Trailing Key Takeaways Markdown Suppression in `renderedMarkdown`
- **Approach**: In the `renderedMarkdown` computed property of `[bookId].vue`, after heading deduplication and prior to calling `renderMarkdown(text, ...)`, apply a regex pattern to strip trailing Key Takeaways sections when `hasValidTakeaways.value` is true:
  ```typescript
  if (hasValidTakeaways.value) {
    text = text.replace(
      /\s*#{1,4}\s+Key\s+Takeaways\s*(?:\r?\n\s*[-*+]\s+[^\r\n]+)*\s*$/i,
      ""
    );
  }
  ```
- **Rationale**: The Gemini formatter prompt mandates that key takeaways be placed at the very conclusion of the markdown text (`'### Key Takeaways' containing exactly 3 bullet points`). When structured takeaways are available in `currentChunk.keyTakeaways`, stripping them from the raw markdown prevents duplication and eliminates the untranslated English H3 heading from the Vietnamese locale. If a chunk has no structured takeaways, the markdown content is preserved as-is.

### Decision 2: Brand Violet Token Standardization for Takeaways Callout
- **Approach**: Replace amber color utilities in `[bookId].vue` with system primary brand tokens:
  - Container: `p-4 sm:p-6 rounded-3xl bg-brand-50/50 dark:bg-brand-500/10 border border-brand-200/80 dark:border-brand-500/20 space-y-3`
  - Header: `flex items-center gap-2 text-xs sm:text-sm font-bold text-brand-900 dark:text-brand-300 uppercase tracking-wider`
  - Sparkles icon: `w-4 h-4 text-brand-600 dark:text-brand-400`
  - Bullet indicators: `w-1.5 h-1.5 rounded-full bg-brand-500 mt-2 shrink-0`
- **Rationale**: Amber is reserved for warning alerts (`[!WARNING]`) and streak flames (`streak-amber`). Primary learning insights and takeaways belong to the system primary brand violet palette (`brand-500` / `#7c3aed`), matching the obsidian canvas standard established in `unify-system-primary-theme` and `modernize-reader-studio-tokens`.

### Decision 3: Localization Catalog Expansion & Template Wiring
- **Approach**:
  - Add the following keys to `frontend/i18n/locales/en.json`:
    - `"font_smaller": "Smaller Font"`
    - `"font_larger": "Larger Font"`
    - `"prev_slice_hint": "Previous Slice (Shift + ←)"`
    - `"next_slice_hint": "Next Slice (Shift + →)"`
    - `"loading_chapter": "Loading document chapter..."`
  - Add corresponding keys to `frontend/i18n/locales/vi.json`:
    - `"font_smaller": "Cỡ chữ nhỏ hơn"`
    - `"font_larger": "Cỡ chữ lớn hơn"`
    - `"prev_slice_hint": "Lát cắt trước (Shift + ←)"`
    - `"next_slice_hint": "Lát cắt tiếp theo (Shift + →)"`
    - `"loading_chapter": "Đang tải nội dung chương..."`
  - Wire buttons and attributes in `[bookId].vue`:
    - Font size buttons: `:title="$t('reader.font_smaller')"` and `:title="$t('reader.font_larger')"`
    - Font family buttons: `{{ $t('reader.font_sans') }}`, `{{ $t('reader.font_serif') }}`, `{{ $t('reader.font_mono') }}`
    - Navigation tooltips: `:title="$t('reader.prev_slice_hint')"` and `:title="$t('reader.next_slice_hint')"`
    - Drawer close button: `:aria-label="$t('reader.close_toc')"`
    - Loading placeholder: `{{ $t('reader.loading_chapter') }}`
- **Rationale**: Enforces 100% localization coverage, eliminating hardcoded English literals throughout the reader interface.

## Risks / Trade-offs

- **Risk**: Edge cases where an author writes "Key Takeaways" in the middle of an article rather than at the end.
- **Mitigation**: The regex requires the section to match at the end of the text (`\s*$`), ensuring middle-of-article text is never stripped.
- **Risk**: Missing translation keys causing runtime i18n warnings.
- **Mitigation**: Verified symmetrical key additions in both `en.json` and `vi.json` with strict parity checks.

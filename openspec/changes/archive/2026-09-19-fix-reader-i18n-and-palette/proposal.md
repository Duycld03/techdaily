# Proposal: Fix Reader i18n Keys, Key Takeaways Deduplication, and Brand Palette

## Why

In the dedicated reading view (`/read/[bookId]`, as captured in user inspection [Image #1]), multiple localization, syntax, and color token discrepancies undermine reader polish and visual cohesion:
1. **Broken Template Interpolation**: Line 1221 in `frontend/pages/read/[bookId].vue` is missing opening curly braces (`$t("reader.slice_badge", { ... }) }}`), outputting raw unparsed JavaScript syntax instead of the translated chapter slice badge.
2. **Untranslated & Duplicated Key Takeaways**: When reading an AI-curated chapter, the markdown body renders a trailing English `### Key Takeaways` section directly above the localized `✨ Ý CHÍNH CỐT LÕI` callout box. This displays duplicate bullet points and leaves an untranslated English heading in the Vietnamese locale.
3. **Mismatched Amber Palette on Takeaways Box**: The Key Takeaways callout box renders in amber warning tokens (`bg-amber-50/60 dark:bg-amber-950/20`, `border-amber-200/80 dark:border-amber-500/20`, `text-amber-900 dark:text-amber-300`, `text-amber-600 dark:text-amber-400`), which clashes with TechDaily's obsidian canvas (`#09090b`) and system primary brand violet identity (`brand-500` / `#7c3aed`) unified across the rest of the application.
4. **Hardcoded Strings in Reader Controls**: Typography popover options (`Sans`, `Serif`, `Mono`, `Smaller Font`, `Larger Font`), slice navigation tooltips (`Previous Slice (Shift + ←)`, `Next Slice (Shift + →)`), and loading states contain hardcoded English strings that bypass the i18n localization dictionary.

Addressing these issues restores 100% localization coverage, eliminates redundant content rendering, and unifies the reader callout palette with Dev-Learning Studio brand tokens.

## What Changes

- **Fix Template Syntax**: Add missing `{{` interpolation delimiters to line 1221 of `frontend/pages/read/[bookId].vue` so `$t("reader.slice_badge")` interpolates properly.
- **Key Takeaways Markdown Deduplication**: In `renderedMarkdown` computed property of `[bookId].vue`, strip trailing `### Key Takeaways` (and `## Key Takeaways`) sections along with their bullet points whenever `hasValidTakeaways` is true, ensuring takeaways render solely in the dedicated, localized callout card.
- **Brand Palette Unification for Takeaways Callout**: Replace amber styling on the Key Takeaways container with system primary brand violet tokens:
  - Background: `bg-brand-50/50 dark:bg-brand-500/10`
  - Border: `border-brand-200/80 dark:border-brand-500/20`
  - Header & Sparkles icon: `text-brand-900 dark:text-brand-300` and `text-brand-600 dark:text-brand-400`
  - Bullet indicators: `bg-brand-500`
- **Comprehensive Reader i18n Localization**:
  - Add missing translation keys (`font_smaller`, `font_larger`, `prev_slice_hint`, `next_slice_hint`, `loading_chapter`) to both `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.
  - Wire font family buttons to existing keys (`reader.font_sans`, `reader.font_serif`, `reader.font_mono`).
  - Wire navigation tooltips and loading spinners to localized `$t()` expressions.
- **Zero Breaking Changes**: Purely frontend presentation and localization fixes; no API, database, or schema modifications.

## Capabilities

### New Capabilities
<!-- None: Pure refinement of existing reader capability -->

### Modified Capabilities
- `reader`: Update requirements and scenarios for `Dedicated Reading Route` and `Distraction-Free Daily Reader Pane` to mandate deduplication of Key Takeaways in the reading article body, system primary brand violet token styling for takeaway callouts, and 100% localization coverage across all reader interface controls.

## Impact

- Affected files:
  - `frontend/pages/read/[bookId].vue`
  - `frontend/i18n/locales/en.json`
  - `frontend/i18n/locales/vi.json`
- Test suites:
  - `frontend/tests/pages/read.spec.ts`

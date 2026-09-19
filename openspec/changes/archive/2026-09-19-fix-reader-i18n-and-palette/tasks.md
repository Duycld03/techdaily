# Tasks

## 1. Frontend Localization & Dictionary Expansion

- [x] 1.1 Add missing reader keys (`font_smaller`, `font_larger`, `prev_slice_hint`, `next_slice_hint`, `loading_chapter`) to `frontend/i18n/locales/en.json`
- [x] 1.2 Add corresponding Vietnamese translations for the new reader keys to `frontend/i18n/locales/vi.json`
- [x] 1.3 Verify bilingual parity across all reader localization keys in both English and Vietnamese catalogs

## 2. Frontend Reader Syntax & Deduplication

- [x] 2.1 Fix missing `{{` template interpolation syntax on slice badge at line 1221 of `frontend/pages/read/[bookId].vue`
- [x] 2.2 Update `renderedMarkdown` computed property in `frontend/pages/read/[bookId].vue` to strip trailing `### Key Takeaways` blocks when `hasValidTakeaways` is true
- [x] 2.3 Wire typography family buttons (`Sans`, `Serif`, `Mono`) to `$t('reader.font_sans')`, `$t('reader.font_serif')`, and `$t('reader.font_mono')` in `frontend/pages/read/[bookId].vue`
- [x] 2.4 Wire font size adjusters, navigation hints, TOC close buttons, and loading spinner to localized i18n keys in `frontend/pages/read/[bookId].vue`

## 3. Frontend Brand Palette Styling

- [x] 3.1 Migrate Key Takeaways container in `frontend/pages/read/[bookId].vue` from amber tokens to Dev-Learning Studio brand violet tokens (`bg-brand-50/50 dark:bg-brand-500/10`, `border-brand-200/80 dark:border-brand-500/20`)
- [x] 3.2 Update Key Takeaways title, icon, and bullet point colors to primary brand violet tokens (`text-brand-900 dark:text-brand-300`, `text-brand-600 dark:text-brand-400`, `bg-brand-500`)

## 4. Automated Verification & Regression Testing

- [x] 4.1 Update `frontend/tests/pages/read.spec.ts` to assert slice badge template interpolation, trailing Key Takeaways suppression in markdown, and brand token styling on the callout container
- [x] 4.2 Execute frontend unit tests (`npm test`) and verify 100% passing test suites

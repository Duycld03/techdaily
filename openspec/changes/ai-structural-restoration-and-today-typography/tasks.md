# Tasks: AI Structural Restoration and Today Reader Typography Controls

## 1. Backend AI Structural Restoration & Curation Pipeline

- [x] 1.1 Update `backend/src/TechDaily.Infrastructure/Services/GeminiAiService.cs` system instruction for `Category.EngineeringCraft` to mandate 100% verbatim author text retention (zero summarization), detect run-in headings and promote them to `### {SectionTitle}`, eliminate duplicate header echoes, format double-newline paragraph breaks (`\n\n`), and preserve spoken dialogue and quotes. Verify by reviewing unit test assertions in `GeminiAiServiceTests.cs`.
- [x] 1.2 Update `backend/src/TechDaily.Application/Features/Library/CurateSlice/CurateSliceHandler.cs` to set `chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown` when `FormattedMarkdown` is present, allowing clean AI-restored verbatim markdown to be persisted and rendered across all reader views. Verify by executing `dotnet test --filter FullyQualifiedName~CurateSliceHandlerTests`.
- [x] 1.3 Add backend unit test cases in `backend/tests/TechDaily.Tests` asserting that `EngineeringCraft` slices retain verbatim prose while promoting run-in headings and that `CurateSliceHandler` correctly updates `OriginalTextMarkdown`. Verify by running `dotnet test`.

## 2. Frontend Library Guidance & Localization

- [x] 2.1 Update `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` for key `library.verbatim_category_hint` with streamlined copy:
  - VI: `"Mẹo: Chọn chuyên mục \"Tư Duy Kỹ Sư & Năng Suất\" để giữ nguyên văn 100% nội dung sách (AI khôi phục tiêu đề & ngắt dòng)."`
  - EN: `"Tip: Select \"Engineering Craft & Mindset\" to preserve 100% verbatim book text (AI restores headings & paragraphs)."`
  Verify by checking both JSON locale files for valid syntax and correct key paths.
- [x] 2.2 Verify in `frontend/pages/library.vue` that the full-width amber callout beneath the Category selector renders the updated localized strings across all three import modal tabs (Markdown Series, PDF Upload, and URL Crawler). Verify by running `npx nuxi typecheck`.

## 3. Shared Typography Composable & Dedicated Reader Refactoring

- [x] 3.1 Create `frontend/composables/useReaderTypography.ts` managing reactive typography state (`fontSize`, `fontFamily`, `lineSpacing`, `readingWidth`) synchronized via `localStorage` under key `techdaily_reader_typography`, including stepped size helpers (`increaseFontSize`, `decreaseFontSize`), boundary flags (`canDecreaseFontSize`, `canIncreaseFontSize`), and computed style/class mappings (`fontSizePx`, `lineHeightValue`, `fontFamilyClass`, `readingWidthClass`). Verify by running `npx nuxi typecheck`.
- [x] 3.2 Refactor `frontend/pages/read/[bookId].vue` to import and consume `useReaderTypography()`, removing duplicate typography state, local maps, and redundant storage watchers. Verify by running `npx nuxi typecheck` and ensuring reader page compiles without errors.

## 4. Today Reader Pane Typography Controls & Styling

- [x] 4.1 In `frontend/components/today/DocReaderPane.vue`, add a sleek `Aa` typography button and floating popover dropdown to the header with controls for Font Size (`A-` / `A+` and percentage scale), Font Family (Sans, Serif, Mono), and Line Spacing (Normal, Relaxed, Loose). Verify by checking template markup and visual layout.
- [x] 4.2 Bind dynamic inline styles `:style="{ fontSize: fontSizePx, lineHeight: lineHeightValue }"` and classes `:class="[fontFamilyClass]"` to `.doc-reader-content` in `DocReaderPane.vue`. Verify by inspecting DOM element styles during reactive updates.
- [x] 4.3 Add scoped CSS inheritance in `DocReaderPane.vue`:
  ```css
  :deep(.markdown-body p),
  :deep(.markdown-body li) {
    font-size: inherit !important;
    line-height: inherit !important;
  }
  ```
  Verify rendered markdown paragraphs and list items inherit the active font size and line spacing.
- [x] 4.4 Add click-outside and `Escape` key event handling to dismiss the typography popover in `DocReaderPane.vue`. Verify by testing popover open and close interactions.

## 5. End-to-End Verification & Validation

- [x] 5.1 Run OpenSpec strict validation using `openspec validate --strict ai-structural-restoration-and-today-typography` to confirm all planning artifacts strictly conform to OpenSpec schemas. Verify command outputs validation success with zero errors.
- [x] 5.2 Validate cross-view typography synchronization between `/today` and `/read/[bookId]` to ensure preferences chosen on one view immediately apply and persist across both views.

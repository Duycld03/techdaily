# Design: Smart Slicing, AI Fallback Guard & Mobile UI

## 1. Natural Paragraph & Page Slicing (`PdfPigExtractor.cs`)
- Replace the failed regex `(?m)(?=^#{2,3}\s+)` with a two-phase splitter:
  1. Page-level grouping: accumulate pages up to ~1,500 - 2,000 words.
  2. If an accumulated chunk reaches 2,000 words, break at the nearest double newline (`\n\n`) or page boundary.
  3. Ensure every slice is strictly <= 2,500 words.
  4. Label subdivided parts with clear ordinal naming: `"{Title} (Section 1)"`, `"{Title} (Section 2)"`.

## 2. Strict AI Success Flag (`IAiMarkdownFormatter.cs`, `GeminiAiService.cs`, `CurateSliceHandler.cs`)
- Add `bool IsSuccess` to `AiFormattedSliceResult`.
- `GeminiAiService.FormatSliceAsync`:
  - On genuine AI JSON response: `IsSuccess = true`.
  - In `FallbackFormatSlice`: `IsSuccess = false`.
- `CurateSliceHandler.cs`:
  - If `!aiResult.IsSuccess`: do NOT set `chunk.IsAiFormatted = true`, do NOT persist `InterviewQuestion`, and return `Error("CurateSlice.AiUnavailable", "AI formatting temporarily unavailable")`.

## 3. Responsive Layout in `today.vue`
- Change outer container to `h-full flex flex-col overflow-hidden` (relying on layout's `main` flex-1).
- Encapsulate loading state in `p-6 sm:p-8 flex flex-col items-center justify-center text-center max-w-sm sm:max-w-md mx-auto my-auto`.
